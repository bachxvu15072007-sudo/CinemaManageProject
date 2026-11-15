using System.ComponentModel.DataAnnotations;

namespace CinemaManageProject.Models;

/// <summary>
/// Phòng chiếu phim chứa sơ đồ ghế và phụ thu theo định dạng phòng.
/// </summary>
public class Room
{
    [Key]
    public int RoomId { get; set; }

    [Required]
    [MaxLength(50)]
    public string Name { get; set; } = string.Empty;

    public RoomType RoomType { get; set; } = RoomType.Standard2D;

    public int TotalRows { get; set; }

    public int TotalCols { get; set; }

    public decimal BasePriceSurcharge { get; set; }

    public bool IsActive { get; set; } = true;

    public DateTime CreatedAt { get; set; } = DateTime.Now;

    public ICollection<Seat> Seats { get; set; } = [];
    public ICollection<Showtime> Showtimes { get; set; } = [];
}
