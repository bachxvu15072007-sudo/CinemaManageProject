using System.ComponentModel.DataAnnotations;

namespace CinemaManageProject.Models;

/// <summary>
/// Thể loại phim (Hành động, Hoạt hình, Kinh dị, Tình cảm...).
/// </summary>
public class Genre
{
    [Key]
    public int GenreId { get; set; }

    [Required]
    [MaxLength(50)]
    public string Name { get; set; } = string.Empty;

    public ICollection<MovieGenre> MovieGenres { get; set; } = [];
}
