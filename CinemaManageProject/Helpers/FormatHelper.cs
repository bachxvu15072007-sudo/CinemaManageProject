using System.Globalization;

namespace CinemaManageProject.Helpers;

/// <summary>
/// Các hàm định dạng chuỗi hiển thị dùng chung (tiền tệ VNĐ, ngày giờ, phương thức thanh toán, trạng thái).
/// </summary>
public static class FormatHelper
{
    private static readonly CultureInfo ViCulture = CultureInfo.GetCultureInfo("vi-VN");

    /// <summary>
    /// Định dạng số tiền sang chuẩn VNĐ (ví dụ: 120.000đ).
    /// </summary>
    public static string FormatVnd(decimal amount) => $"{amount.ToString("N0", ViCulture)}đ";

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
    /// Dịch trạng thái phim, suất chiếu, hóa đơn và vé sang tiếng Việt.
    /// </summary>
    public static string FormatStatusLabel(string raw) => raw switch
    {
        "ComingSoon" => "Sắp chiếu",
        "NowShowing" => "Đang chiếu",
        "Ended" => "Ngừng chiếu",
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
    /// Dịch loại phòng chiếu từ mã lưu trong DB sang nhãn dễ đọc.
    /// </summary>
    public static string FormatRoomType(string raw) => raw switch
    {
        "Standard2D" => "2D Tiêu chuẩn",
        "Standard3D" => "3D",
        "IMAX" => "IMAX",
        "VIP" => "VIP",
        _ => raw
    };

    /// <summary>
    /// Dịch loại ghế ngồi sang nhãn tiếng Việt.
    /// </summary>
    public static string FormatSeatType(string raw) => raw switch
    {
        "Standard" => "Ghế Thường",
        "VIP" => "Ghế VIP",
        "Couple" => "Ghế Đôi",
        _ => raw
    };
}
