using System.ComponentModel.DataAnnotations;

namespace CinemaManageProject.Models;

/// <summary>
/// Thông tin khách hàng mua vé và tích điểm thành viên.
/// </summary>
public class Customer
{
    [Key]
    public int CustomerId { get; set; }

    /// <summary>
    /// Null nếu khách chỉ đăng ký thông tin tại quầy chưa tạo tài khoản đăng nhập.
    /// </summary>
    public int? UserId { get; set; }
    public User? User { get; set; }

    [Required]
    [MaxLength(100)]
    public string FullName { get; set; } = string.Empty;

    [Required]
    [MaxLength(20)]
    public string Phone { get; set; } = string.Empty;

    [MaxLength(100)]
    public string? Email { get; set; }

    public DateOnly? DateOfBirth { get; set; }

    public int LoyaltyPoints { get; set; }

    public decimal TotalSpent { get; set; }

    public CustomerTier Tier { get; set; } = CustomerTier.Member;

    public DateTime CreatedAt { get; set; } = DateTime.Now;

    public ICollection<Order> Orders { get; set; } = [];
}
