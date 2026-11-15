using System.ComponentModel.DataAnnotations;

namespace CinemaManageProject.Models;

/// <summary>
/// Tài khoản người dùng trong hệ thống (Admin, Staff, Customer).
/// </summary>
public class User
{
    [Key]
    public int UserId { get; set; }

    [Required]
    [MaxLength(50)]
    public string Username { get; set; } = string.Empty;

    [Required]
    [MaxLength(100)]
    public string Password { get; set; } = string.Empty;

    [Required]
    [MaxLength(100)]
    public string FullName { get; set; } = string.Empty;

    [MaxLength(100)]
    public string? Email { get; set; }

    [MaxLength(20)]
    public string? Phone { get; set; }

    public UserRole Role { get; set; } = UserRole.Customer;

    public bool IsActive { get; set; } = true;

    public DateTime CreatedAt { get; set; } = DateTime.Now;

    public DateTime? LastLoginAt { get; set; }

    /// <summary>
    /// Hồ sơ khách hàng liên kết 1-0..1 khi tài khoản có vai trò Customer.
    /// </summary>
    public Customer? CustomerProfile { get; set; }

    public ICollection<Showtime> CreatedShowtimes { get; set; } = [];
    public ICollection<Order> SoldOrders { get; set; } = [];
    public ICollection<Ticket> CheckedInTickets { get; set; } = [];
}
