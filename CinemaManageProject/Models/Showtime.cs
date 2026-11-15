using System.ComponentModel.DataAnnotations;

namespace CinemaManageProject.Models;

/// <summary>
/// Suất chiếu kết nối một bộ phim với một phòng chiếu trong khung giờ xác định.
/// </summary>
public class Showtime
{
    [Key]
    public int ShowtimeId { get; set; }

    public int MovieId { get; set; }
    public Movie Movie { get; set; } = null!;

    public int RoomId { get; set; }
    public Room Room { get; set; } = null!;

    public DateTime StartTime { get; set; }

    /// <summary>
    /// Thời điểm kết thúc phòng (bao gồm thời lượng phim và thời gian dọn phòng).
    /// </summary>
    public DateTime EndTime { get; set; }

    public decimal BaseTicketPrice { get; set; }

    [MaxLength(50)]
    public string Format { get; set; } = "2D Phụ đề";

    public ShowtimeStatus Status { get; set; } = ShowtimeStatus.OpenForSale;

    public int CreatedByUserId { get; set; }
    public User CreatedByUser { get; set; } = null!;

    public DateTime CreatedAt { get; set; } = DateTime.Now;

    public ICollection<Ticket> Tickets { get; set; } = [];
}
