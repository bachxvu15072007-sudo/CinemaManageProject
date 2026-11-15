using System.ComponentModel.DataAnnotations;

namespace CinemaManageProject.Models;

/// <summary>
/// Hóa đơn đặt/bán vé (hỗ trợ cả bán tại quầy và khách hàng tự đặt).
/// </summary>
public class Order
{
    [Key]
    public int OrderId { get; set; }

    [Required]
    [MaxLength(30)]
    public string OrderCode { get; set; } = string.Empty;

    /// <summary>
    /// Null nếu khách hàng tự đặt vé; có giá trị nếu nhân viên bán tại quầy.
    /// </summary>
    public int? StaffUserId { get; set; }
    public User? StaffUser { get; set; }

    /// <summary>
    /// Null nếu là khách vãng lai tại quầy không đăng ký thông tin thành viên.
    /// </summary>
    public int? CustomerId { get; set; }
    public Customer? Customer { get; set; }

    public BookingChannel BookingChannel { get; set; } = BookingChannel.Counter;

    public int TotalTickets { get; set; }

    public decimal SubTotalAmount { get; set; }

    public int PointsUsed { get; set; }

    public decimal DiscountAmount { get; set; }

    public decimal TotalAmount { get; set; }

    public PaymentMethod PaymentMethod { get; set; } = PaymentMethod.Cash;

    public decimal CashReceived { get; set; }

    public decimal ChangeAmount { get; set; }

    public OrderStatus Status { get; set; } = OrderStatus.Paid;

    [MaxLength(300)]
    public string? Note { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.Now;

    public DateTime? PaidAt { get; set; }

    public ICollection<Ticket> Tickets { get; set; } = [];
}
