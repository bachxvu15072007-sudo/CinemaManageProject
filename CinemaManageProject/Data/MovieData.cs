using System.Globalization;
using CinemaManageProject.Helpers;
using Microsoft.Data.Sqlite;

namespace CinemaManageProject.Data;

public sealed record AdminMovieRow(
    int MovieId,
    string Title,
    string? OriginalTitle,
    string? Director,
    string? Cast,
    int DurationMinutes,
    string ReleaseDateSql,
    string? EndDateSql,
    string AgeRatingText,
    string Language,
    string? Description,
    string? PosterPath,
    string Status,
    string GenresText,
    IReadOnlyList<int> GenreIds)
{
    public string DurationText => $"{DurationMinutes} phút";
    public string StatusDisplay => FormatHelper.FormatStatusLabel(Status);
}

public sealed record MovieSaveInput(
    string Title,
    string? OriginalTitle,
    string? Director,
    string? Cast,
    int DurationMinutes,
    string ReleaseDateSql,
    string? EndDateSql,
    string AgeRating,
    string Language,
    string? Description,
    string? PosterPath,
    string Status,
    IReadOnlyList<int> GenreIds);

/// <summary>
/// Truy vấn SQLite cho các bảng Movies, Genres và MovieGenres.
/// </summary>
public static class MovieData
{
    public static List<(int GenreId, string Name)> LoadAllGenres()
    {
        var result = new List<(int, string)>();
        using SqliteConnection conn = DatabaseHelper.OpenConnection();
        using SqliteCommand cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT GenreId, Name FROM Genres ORDER BY Name COLLATE NOCASE ASC;";

        using SqliteDataReader r = cmd.ExecuteReader();
        while (r.Read())
        {
            result.Add((r.GetInt32(0), r.GetString(1)));
        }

        return result;
    }

    public static int InsertGenre(string name)
    {
        using SqliteConnection conn = DatabaseHelper.OpenConnection();
        using SqliteCommand cmd = conn.CreateCommand();
        cmd.CommandText = """
            INSERT INTO Genres (Name) VALUES (@name);
            SELECT last_insert_rowid();
            """;
        cmd.Parameters.AddWithValue("@name", name);
        return Convert.ToInt32(cmd.ExecuteScalar(), CultureInfo.InvariantCulture);
    }

    public static List<AdminMovieRow> LoadAllMovies()
    {
        using SqliteConnection conn = DatabaseHelper.OpenConnection();
        Dictionary<int, List<int>> movieGenres = LoadMovieGenreMap(conn);

        var movies = new List<AdminMovieRow>();
        using SqliteCommand cmd = conn.CreateCommand();
        cmd.CommandText = """
            SELECT m.MovieId, m.Title, m.OriginalTitle, m.Director, m.Cast,
                   m.DurationMinutes, m.ReleaseDate, m.EndDate, m.AgeRating,
                   m.Language, m.Description, m.PosterPath, m.Status,
                   COALESCE((SELECT GROUP_CONCAT(g.Name, ', ')
                             FROM MovieGenres mg INNER JOIN Genres g ON g.GenreId = mg.GenreId
                             WHERE mg.MovieId = m.MovieId), 'Chưa gán')
            FROM Movies m
            ORDER BY CASE m.Status WHEN 'NowShowing' THEN 0 WHEN 'ComingSoon' THEN 1 ELSE 2 END,
                     m.ReleaseDate DESC, m.MovieId DESC;
            """;

        using SqliteDataReader r = cmd.ExecuteReader();
        while (r.Read())
        {
            int movieId = r.GetInt32(0);
            IReadOnlyList<int> ids = movieGenres.TryGetValue(movieId, out List<int>? list) ? list : [];

            movies.Add(new AdminMovieRow(
                movieId,
                r.GetString(1),
                r.IsDBNull(2) ? null : r.GetString(2),
                r.IsDBNull(3) ? null : r.GetString(3),
                r.IsDBNull(4) ? null : r.GetString(4),
                r.GetInt32(5),
                r.GetString(6),
                r.IsDBNull(7) ? null : r.GetString(7),
                r.GetString(8),
                r.GetString(9),
                r.IsDBNull(10) ? null : r.GetString(10),
                r.IsDBNull(11) ? null : r.GetString(11),
                r.GetString(12),
                r.GetString(13),
                ids));
        }

        return movies;
    }

    public static int SaveMovie(int? movieId, MovieSaveInput input)
    {
        using SqliteConnection conn = DatabaseHelper.OpenConnection();
        using SqliteTransaction tx = conn.BeginTransaction();

        int savedId;
        if (movieId.HasValue)
        {
            savedId = movieId.Value;
            using SqliteCommand update = conn.CreateCommand();
            update.Transaction = tx;
            update.CommandText = """
                UPDATE Movies
                SET Title = @title, OriginalTitle = @orig, Director = @dir, Cast = @cast,
                    DurationMinutes = @dur, ReleaseDate = @rel, EndDate = @end, AgeRating = @age,
                    Language = @lang, Description = @desc, PosterPath = @poster, Status = @status
                WHERE MovieId = @id;
                """;
            BindMovieParameters(update, input);
            update.Parameters.AddWithValue("@id", savedId);
            update.ExecuteNonQuery();
        }
        else
        {
            using SqliteCommand insert = conn.CreateCommand();
            insert.Transaction = tx;
            insert.CommandText = """
                INSERT INTO Movies (
                    Title, OriginalTitle, Director, Cast, DurationMinutes,
                    ReleaseDate, EndDate, AgeRating, Language, Description, PosterPath, Status)
                VALUES (
                    @title, @orig, @dir, @cast, @dur,
                    @rel, @end, @age, @lang, @desc, @poster, @status);
                SELECT last_insert_rowid();
                """;
            BindMovieParameters(insert, input);
            savedId = Convert.ToInt32(insert.ExecuteScalar(), CultureInfo.InvariantCulture);
        }

        SyncMovieGenres(conn, tx, savedId, input.GenreIds);
        tx.Commit();
        return savedId;
    }

    public static int CountMovieShowtimes(int movieId)
    {
        using SqliteConnection conn = DatabaseHelper.OpenConnection();
        using SqliteCommand cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT COUNT(*) FROM Showtimes WHERE MovieId = @id;";
        cmd.Parameters.AddWithValue("@id", movieId);
        return Convert.ToInt32(cmd.ExecuteScalar(), CultureInfo.InvariantCulture);
    }

    public static void DeleteMovie(int movieId)
    {
        using SqliteConnection conn = DatabaseHelper.OpenConnection();
        using SqliteCommand cmd = conn.CreateCommand();
        cmd.CommandText = "DELETE FROM Movies WHERE MovieId = @id;";
        cmd.Parameters.AddWithValue("@id", movieId);
        cmd.ExecuteNonQuery();
    }

    private static Dictionary<int, List<int>> LoadMovieGenreMap(SqliteConnection conn)
    {
        var map = new Dictionary<int, List<int>>();
        using SqliteCommand cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT MovieId, GenreId FROM MovieGenres;";

        using SqliteDataReader r = cmd.ExecuteReader();
        while (r.Read())
        {
            int mid = r.GetInt32(0);
            if (!map.TryGetValue(mid, out List<int>? list))
            {
                list = [];
                map[mid] = list;
            }
            list.Add(r.GetInt32(1));
        }

        return map;
    }

    private static void BindMovieParameters(SqliteCommand cmd, MovieSaveInput input)
    {
        cmd.Parameters.AddWithValue("@title", input.Title);
        cmd.Parameters.AddWithValue("@orig", (object?)input.OriginalTitle ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@dir", (object?)input.Director ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@cast", (object?)input.Cast ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@dur", input.DurationMinutes);
        cmd.Parameters.AddWithValue("@rel", input.ReleaseDateSql);
        cmd.Parameters.AddWithValue("@end", (object?)input.EndDateSql ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@age", input.AgeRating);
        cmd.Parameters.AddWithValue("@lang", input.Language);
        cmd.Parameters.AddWithValue("@desc", (object?)input.Description ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@poster", string.IsNullOrWhiteSpace(input.PosterPath) ? DBNull.Value : input.PosterPath);
        cmd.Parameters.AddWithValue("@status", input.Status);
    }

    private static void SyncMovieGenres(SqliteConnection conn, SqliteTransaction tx, int movieId, IReadOnlyList<int> genreIds)
    {
        using (SqliteCommand del = conn.CreateCommand())
        {
            del.Transaction = tx;
            del.CommandText = "DELETE FROM MovieGenres WHERE MovieId = @mid;";
            del.Parameters.AddWithValue("@mid", movieId);
            del.ExecuteNonQuery();
        }

        foreach (int genreId in genreIds)
        {
            using SqliteCommand ins = conn.CreateCommand();
            ins.Transaction = tx;
            ins.CommandText = "INSERT INTO MovieGenres (MovieId, GenreId) VALUES (@mid, @gid);";
            ins.Parameters.AddWithValue("@mid", movieId);
            ins.Parameters.AddWithValue("@gid", genreId);
            ins.ExecuteNonQuery();
        }
    }
}
