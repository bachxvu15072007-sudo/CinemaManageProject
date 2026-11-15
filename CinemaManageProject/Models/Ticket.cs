using System.ComponentModel.DataAnnotations;

namespace CinemaManageProject.Models;

/// <summary>
/// Vé xem phim gắn với một suất chiếu và một ghế ngồi cụ thể.
/// </summary>
public class Ticket
{
    [Key]
    public int TicketId { get; set; }

    [Required]
    [MaxLength(30)]
    public string TicketCode { get; set; } = string.Empty;

    public int OrderId { get; set; }
    public Order Order { get; set; } = null!;

    public int ShowtimeId { get; set; }
    public Showtime Showtime { get; set; } = null!;

    public int SeatId { get; set; }
    public Seat Seat { get; set; } = null!;

    public decimal UnitPrice { get; set; }

    public TicketStatus Status { get; set; } = TicketStatus.Booked;

    public DateTime? CheckedInAt { get; set; }

    public int? CheckedInByUserId { get; set; }
    public User? CheckedInByUser { get; set; }
}
