using System.ComponentModel.DataAnnotations;

namespace CinemaManageProject.Models;

/// <summary>
/// Thông tin bộ phim được quản lý và lên lịch chiếu tại rạp.
/// </summary>
public class Movie
{
    [Key]
    public int MovieId { get; set; }

    [Required]
    [MaxLength(200)]
    public string Title { get; set; } = string.Empty;

    [MaxLength(200)]
    public string? OriginalTitle { get; set; }

    [MaxLength(100)]
    public string? Director { get; set; }

    [MaxLength(300)]
    public string? Cast { get; set; }

    public int DurationMinutes { get; set; }

    public DateOnly ReleaseDate { get; set; }

    public DateOnly? EndDate { get; set; }

    public AgeRating AgeRating { get; set; } = AgeRating.P;

    [MaxLength(50)]
    public string Language { get; set; } = "Phụ đề Tiếng Việt";

    [MaxLength(1500)]
    public string? Description { get; set; }

    [MaxLength(500)]
    public string? PosterPath { get; set; }

    public MovieStatus Status { get; set; } = MovieStatus.NowShowing;

    public DateTime CreatedAt { get; set; } = DateTime.Now;

    public ICollection<MovieGenre> MovieGenres { get; set; } = [];
    public ICollection<Showtime> Showtimes { get; set; } = [];
}
