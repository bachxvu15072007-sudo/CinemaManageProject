using CinemaManageProject.Data;
using CinemaManageProject.Helpers;
using Dapper;
using Microsoft.Data.Sqlite;
using Xunit;

namespace CinemaManageProject.Tests;

[Collection("CinemaDatabase")]
public sealed class MovieCrudTests
{
    private readonly CinemaDatabaseFixture _db;

    public MovieCrudTests(CinemaDatabaseFixture db)
    {
        _db = db;
        _db.ResetDatabase();
    }

    [Fact]
    public void LoadAllMoviesAndGenres_ReturnsSeededCatalog()
    {
        List<AdminMovieRow> movies = MovieData.LoadAllMovies();
        List<(int GenreId, string Name)> genres = MovieData.LoadAllGenres();

        Assert.Equal(15, movies.Count);
        Assert.Equal(10, genres.Count);
        Assert.All(movies, m => Assert.NotEmpty(m.GenreIds));
    }

    [Fact]
    public void InsertGenre_NewNameSucceedsAndDuplicateNameThrowsUniqueConstraint()
    {
        int newId = MovieData.InsertGenre("Tài liệu IMAX");

        Assert.True(newId > 10);
        Assert.Contains(MovieData.LoadAllGenres(), g => g.GenreId == newId && g.Name == "Tài liệu IMAX");
        Assert.Throws<SqliteException>(() => MovieData.InsertGenre("Tài liệu IMAX"));
    }

    [Fact]
    public void SaveMovie_CreateUpdateSyncGenresAndDelete_WorksEndToEnd()
    {
        var createInput = new MovieSaveInput(
            Title: "Phim Test Tự Động",
            OriginalTitle: "Automated Test Movie",
            Director: "Đạo diễn Test",
            Cast: "Diễn viên A, B",
            DurationMinutes: 120,
            ReleaseDateSql: "2026-12-01",
            EndDateSql: "2027-01-15",
            AgeRating: "T13",
            Language: "Phụ đề Tiếng Việt",
            Description: "Mô tả kiểm thử CRUD phim.",
            PosterPath: null,
            Status: "ComingSoon",
            GenreIds: [1, 5]);

        int movieId = MovieData.SaveMovie(null, createInput);
        Assert.True(movieId > 15);

        AdminMovieRow created = MovieData.LoadAllMovies().Single(m => m.MovieId == movieId);
        Assert.Equal("Phim Test Tự Động", created.Title);
        Assert.Equal("ComingSoon", created.Status);
        Assert.Equal([1, 5], created.GenreIds.OrderBy(id => id).ToArray());

        // Cập nhật tên phim, trạng thái và đồng bộ lại thể loại (bỏ 1 & 5, đổi sang 2 & 9)
        var updateInput = createInput with
        {
            Title = "Phim Test Đã Cập Nhật",
            Status = "NowShowing",
            DurationMinutes = 135,
            GenreIds = [2, 9]
        };

        int updatedId = MovieData.SaveMovie(movieId, updateInput);
        Assert.Equal(movieId, updatedId);

        AdminMovieRow updated = MovieData.LoadAllMovies().Single(m => m.MovieId == movieId);
        Assert.Equal("Phim Test Đã Cập Nhật", updated.Title);
        Assert.Equal("NowShowing", updated.Status);
        Assert.Equal(135, updated.DurationMinutes);
        Assert.Equal([2, 9], updated.GenreIds.OrderBy(id => id).ToArray());

        // Xóa phim chưa có suất chiếu phải xóa cả bảng nối MovieGenres (ON DELETE CASCADE)
        Assert.Equal(0, MovieData.CountMovieShowtimes(movieId));
        MovieData.DeleteMovie(movieId);

        Assert.DoesNotContain(MovieData.LoadAllMovies(), m => m.MovieId == movieId);
        using SqliteConnection conn = DatabaseHelper.OpenConnection();
        int remainingLinks = conn.ExecuteScalar<int>("SELECT COUNT(*) FROM MovieGenres WHERE MovieId = @id;", new { id = movieId });
        Assert.Equal(0, remainingLinks);
    }

    [Fact]
    public void DeleteMovie_WithExistingShowtimes_IsBlockedByForeignKeyRestrict()
    {
        Assert.True(MovieData.CountMovieShowtimes(movieId: 1) > 0);
        Assert.Throws<SqliteException>(() => MovieData.DeleteMovie(movieId: 1));
    }

    [Theory]
    [InlineData(0, "T13", "NowShowing")]
    [InlineData(-10, "T13", "NowShowing")]
    [InlineData(110, "R21", "NowShowing")]
    [InlineData(110, "T13", "UnknownStatus")]
    public void SaveMovie_ViolatingCheckConstraints_ThrowsSqliteException(int duration, string ageRating, string status)
    {
        var invalidInput = new MovieSaveInput(
            Title: "Phim Lỗi Ràng Buộc",
            OriginalTitle: null,
            Director: null,
            Cast: null,
            DurationMinutes: duration,
            ReleaseDateSql: "2026-12-01",
            EndDateSql: null,
            AgeRating: ageRating,
            Language: "Tiếng Việt",
            Description: null,
            PosterPath: null,
            Status: status,
            GenreIds: [1]);

        Assert.Throws<SqliteException>(() => MovieData.SaveMovie(null, invalidInput));
        Assert.Equal(15, MovieData.LoadAllMovies().Count);
    }
}
