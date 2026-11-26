using CinemaManageProject.Helpers;
using Dapper;
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
/// Truy vấn SQLite bằng Dapper cho các bảng Movies, Genres và MovieGenres.
/// </summary>
public static class MovieData
{
    private sealed class GenreRow
    {
        public int GenreId { get; init; }
        public string Name { get; init; } = string.Empty;
    }

    private sealed class MovieGenrePair
    {
        public int MovieId { get; init; }
        public int GenreId { get; init; }
    }

    private sealed class MovieSqlRow
    {
        public int MovieId { get; init; }
        public string Title { get; init; } = string.Empty;
        public string? OriginalTitle { get; init; }
        public string? Director { get; init; }
        public string? Cast { get; init; }
        public int DurationMinutes { get; init; }
        public string ReleaseDateSql { get; init; } = string.Empty;
        public string? EndDateSql { get; init; }
        public string AgeRatingText { get; init; } = string.Empty;
        public string Language { get; init; } = string.Empty;
        public string? Description { get; init; }
        public string? PosterPath { get; init; }
        public string Status { get; init; } = string.Empty;
        public string GenresText { get; init; } = string.Empty;
    }

    public static List<(int GenreId, string Name)> LoadAllGenres()
    {
        using SqliteConnection conn = DatabaseHelper.OpenConnection();
        return conn.Query<GenreRow>("SELECT GenreId, Name FROM Genres ORDER BY Name COLLATE NOCASE ASC;")
            .Select(g => (g.GenreId, g.Name))
            .ToList();
    }

    public static int InsertGenre(string name)
    {
        using SqliteConnection conn = DatabaseHelper.OpenConnection();
        return conn.ExecuteScalar<int>(
            """
            INSERT INTO Genres (Name) VALUES (@name);
            SELECT last_insert_rowid();
            """,
            new { name });
    }

    public static List<AdminMovieRow> LoadAllMovies()
    {
        using SqliteConnection conn = DatabaseHelper.OpenConnection();

        Dictionary<int, List<int>> movieGenres = conn
            .Query<MovieGenrePair>("SELECT MovieId, GenreId FROM MovieGenres;")
            .GroupBy(p => p.MovieId)
            .ToDictionary(g => g.Key, g => g.Select(x => x.GenreId).ToList());

        const string sql = """
            SELECT m.MovieId, m.Title, m.OriginalTitle, m.Director, m.Cast,
                   m.DurationMinutes, m.ReleaseDate AS ReleaseDateSql, m.EndDate AS EndDateSql,
                   m.AgeRating AS AgeRatingText, m.Language, m.Description, m.PosterPath, m.Status,
                   COALESCE((SELECT GROUP_CONCAT(g.Name, ', ')
                             FROM MovieGenres mg INNER JOIN Genres g ON g.GenreId = mg.GenreId
                             WHERE mg.MovieId = m.MovieId), 'Chưa gán') AS GenresText
            FROM Movies m
            ORDER BY CASE m.Status WHEN 'NowShowing' THEN 0 WHEN 'ComingSoon' THEN 1 ELSE 2 END,
                     m.ReleaseDate DESC, m.MovieId DESC;
            """;

        return conn.Query<MovieSqlRow>(sql)
            .Select(m => new AdminMovieRow(
                m.MovieId,
                m.Title,
                m.OriginalTitle,
                m.Director,
                m.Cast,
                m.DurationMinutes,
                m.ReleaseDateSql,
                m.EndDateSql,
                m.AgeRatingText,
                m.Language,
                m.Description,
                m.PosterPath,
                m.Status,
                m.GenresText,
                movieGenres.TryGetValue(m.MovieId, out List<int>? ids) ? ids : []))
            .ToList();
    }

    public static int SaveMovie(int? movieId, MovieSaveInput input)
    {
        using SqliteConnection conn = DatabaseHelper.OpenConnection();
        using SqliteTransaction tx = conn.BeginTransaction();

        string? cleanPoster = string.IsNullOrWhiteSpace(input.PosterPath) ? null : input.PosterPath;
        int savedId;

        if (movieId.HasValue)
        {
            savedId = movieId.Value;
            conn.Execute(
                """
                UPDATE Movies
                SET Title = @Title, OriginalTitle = @OriginalTitle, Director = @Director, Cast = @Cast,
                    DurationMinutes = @DurationMinutes, ReleaseDate = @ReleaseDateSql, EndDate = @EndDateSql,
                    AgeRating = @AgeRating, Language = @Language, Description = @Description,
                    PosterPath = @PosterPath, Status = @Status
                WHERE MovieId = @MovieId;
                """,
                new
                {
                    MovieId = savedId,
                    input.Title,
                    input.OriginalTitle,
                    input.Director,
                    input.Cast,
                    input.DurationMinutes,
                    input.ReleaseDateSql,
                    input.EndDateSql,
                    input.AgeRating,
                    input.Language,
                    input.Description,
                    PosterPath = cleanPoster,
                    input.Status
                },
                tx);
        }
        else
        {
            savedId = conn.ExecuteScalar<int>(
                """
                INSERT INTO Movies (
                    Title, OriginalTitle, Director, Cast, DurationMinutes,
                    ReleaseDate, EndDate, AgeRating, Language, Description, PosterPath, Status)
                VALUES (
                    @Title, @OriginalTitle, @Director, @Cast, @DurationMinutes,
                    @ReleaseDateSql, @EndDateSql, @AgeRating, @Language, @Description, @PosterPath, @Status);
                SELECT last_insert_rowid();
                """,
                new
                {
                    input.Title,
                    input.OriginalTitle,
                    input.Director,
                    input.Cast,
                    input.DurationMinutes,
                    input.ReleaseDateSql,
                    input.EndDateSql,
                    input.AgeRating,
                    input.Language,
                    input.Description,
                    PosterPath = cleanPoster,
                    input.Status
                },
                tx);
        }

        conn.Execute("DELETE FROM MovieGenres WHERE MovieId = @mid;", new { mid = savedId }, tx);
        if (input.GenreIds.Count > 0)
        {
            conn.Execute(
                "INSERT INTO MovieGenres (MovieId, GenreId) VALUES (@mid, @gid);",
                input.GenreIds.Select(gid => new { mid = savedId, gid }),
                tx);
        }

        tx.Commit();
        return savedId;
    }

    public static int CountMovieShowtimes(int movieId)
    {
        using SqliteConnection conn = DatabaseHelper.OpenConnection();
        return conn.ExecuteScalar<int>("SELECT COUNT(*) FROM Showtimes WHERE MovieId = @id;", new { id = movieId });
    }

    public static void DeleteMovie(int movieId)
    {
        using SqliteConnection conn = DatabaseHelper.OpenConnection();
        conn.Execute("DELETE FROM Movies WHERE MovieId = @id;", new { id = movieId });
    }
}
