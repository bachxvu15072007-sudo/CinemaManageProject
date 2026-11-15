using System.ComponentModel.DataAnnotations;

namespace CinemaManageProject.Models;

/// <summary>
/// Ghế ngồi thuộc một phòng chiếu cụ thể (hỗ trợ loại ghế và trạng thái bảo trì).
/// </summary>
public class Seat
{
    [Key]
    public int SeatId { get; set; }

    public int RoomId { get; set; }
    public Room Room { get; set; } = null!;

    [Required]
    [MaxLength(5)]
    public string RowLabel { get; set; } = string.Empty;

    public int SeatNumber { get; set; }

    [Required]
    [MaxLength(10)]
    public string SeatCode { get; set; } = string.Empty;

    public SeatType SeatType { get; set; } = SeatType.Standard;

    public decimal PriceMultiplier { get; set; } = 1.00m;

    public int RowIndex { get; set; }

    public int ColIndex { get; set; }

    /// <summary>
    /// true = ghế hoạt động bình thường; false = ghế hỏng/bảo trì không thể bán.
    /// </summary>
    public bool IsActive { get; set; } = true;

    public ICollection<Ticket> Tickets { get; set; } = [];
}
