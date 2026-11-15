namespace CinemaManageProject.Models;

/// <summary>
/// Bảng liên kết nhiều-nhiều giữa Phim và Thể loại.
/// </summary>
public class MovieGenre
{
    public int MovieId { get; set; }
    public Movie Movie { get; set; } = null!;

    public int GenreId { get; set; }
    public Genre Genre { get; set; } = null!;
}
