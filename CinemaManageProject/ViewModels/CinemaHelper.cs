using System.Globalization;
using CinemaManageProject.Models;
using Microsoft.Data.Sqlite;

namespace CinemaManageProject.ViewModels;

/// <summary>
/// Các hàm tĩnh phụ trợ đọc SQLite và định dạng dữ liệu hiển thị cho toàn bộ ViewModel.
/// </summary>
public static class CinemaHelper
{
    private static readonly CultureInfo ViCulture = CultureInfo.GetCultureInfo("vi-VN");

    /// <summary>
    /// Định dạng số tiền sang chuẩn VNĐ (ví dụ: 120.000đ).
    /// </summary>
    public static string FormatVnd(decimal amount) => $"{amount.ToString("N0", ViCulture)}đ";

    /// <summary>
    /// Đọc giá trị số thập phân từ SqliteDataReader, trả về 0 nếu NULL.
    /// </summary>
    public static decimal ReadDecimal(SqliteDataReader reader, int ordinal)
        => reader.IsDBNull(ordinal) ? 0m : Convert.ToDecimal(reader.GetValue(ordinal), CultureInfo.InvariantCulture);

    /// <summary>
    /// Chuyển chuỗi ngày giờ lưu trong SQLite sang định dạng dễ đọc.
    /// </summary>
    public static string FormatSqlDateTime(string raw, string format = "dd/MM HH:mm")
        => DateTime.TryParse(raw, CultureInfo.InvariantCulture, DateTimeStyles.AssumeLocal, out DateTime dt)
            ? dt.ToString(format)
            : raw;

    /// <summary>
    /// Dịch phương thức thanh toán từ mã lưu trong DB sang tiếng Việt.
    /// </summary>
    public static string FormatPaymentMethod(string raw) => raw switch
    {
        "Cash" => "Tiền mặt",
        "BankTransfer" => "Chuyển khoản",
        "Card" => "Thẻ",
        "EWallet" => "Ví điện tử",
        _ => raw
    };

    /// <summary>
    /// Dịch trạng thái suất chiếu, hóa đơn và vé sang tiếng Việt.
    /// </summary>
    public static string FormatStatusLabel(string raw) => raw switch
    {
        "OpenForSale" => "Đang mở bán",
        "Scheduled" => "Đã lên lịch",
        "Completed" => "Đã chiếu",
        "Paid" => "Đã thanh toán",
        "PartiallyRefunded" => "Hoàn một phần",
        "Booked" => "Chờ soát vé",
        "CheckedIn" => "Đã check-in",
        "Cancelled" => "Đã hủy",
        _ => raw
    };

    /// <summary>
    /// Đọc hồ sơ khách hàng từ bảng Customers theo UserId.
    /// </summary>
    public static Customer? LoadCustomerProfile(SqliteConnection conn, int userId)
    {
        using SqliteCommand cmd = conn.CreateCommand();
        cmd.CommandText = """
            SELECT CustomerId, FullName, Phone, Email, LoyaltyPoints, TotalSpent, Tier
            FROM Customers
            WHERE UserId = @uid
            LIMIT 1;
            """;
        cmd.Parameters.AddWithValue("@uid", userId);

        using SqliteDataReader r = cmd.ExecuteReader();
        if (!r.Read())
        {
            return null;
        }

        return new Customer
        {
            CustomerId = r.GetInt32(0),
            UserId = userId,
            FullName = r.GetString(1),
            Phone = r.GetString(2),
            Email = r.IsDBNull(3) ? null : r.GetString(3),
            LoyaltyPoints = r.GetInt32(4),
            TotalSpent = ReadDecimal(r, 5),
            Tier = Enum.TryParse(r.GetString(6), true, out CustomerTier tier) ? tier : CustomerTier.Member
        };
    }
}
