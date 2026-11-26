using CinemaManageProject.Helpers;
using Dapper;
using Microsoft.Data.Sqlite;

namespace CinemaManageProject.Data;

public sealed record AdminOrderSummaryRow(
    int OrderId,
    string OrderCode,
    int? StaffUserId,
    string StaffDisplay,
    int? CustomerId,
    string CustomerName,
    string CustomerPhone,
    string BookingChannel,
    string BookingChannelDisplay,
    int TotalTickets,
    int ActiveTicketsCount,
    decimal SubTotalAmount,
    int PointsUsed,
    decimal DiscountAmount,
    decimal TotalAmount,
    string PaymentMethod,
    string PaymentDisplay,
    decimal CashReceived,
    decimal ChangeAmount,
    string Status,
    string StatusDisplay,
    string NoteText,
    string CreatedAtSql,
    string CreatedAtDisplay,
    string PaidAtDisplay)
{
    public bool IsRevenueEffective => Status != "Cancelled";
    public bool IsCancelled => Status == "Cancelled";
    public bool HasCashDetails => PaymentMethod == "Cash";
    public decimal RefundAdjustmentAmount => Math.Max(0m, SubTotalAmount - DiscountAmount - TotalAmount);
    public bool HasRefundAdjustment => RefundAdjustmentAmount > 0m;
    public bool HasDiscountOrPoints => PointsUsed > 0 || DiscountAmount > 0m;

    public string TotalTicketsText => ActiveTicketsCount != TotalTickets
        ? $"{ActiveTicketsCount}/{TotalTickets} vé"
        : $"{TotalTickets} vé";

    public string TotalAmountText => FormatHelper.FormatVnd(TotalAmount);
    public string SubTotalAmountText => FormatHelper.FormatVnd(SubTotalAmount);
    public string DiscountSummaryText => PointsUsed > 0
        ? $"-{FormatHelper.FormatVnd(DiscountAmount)} ({PointsUsed} điểm)"
        : $"-{FormatHelper.FormatVnd(DiscountAmount)}";
    public string RefundAdjustmentText => $"-{FormatHelper.FormatVnd(RefundAdjustmentAmount)}";
    public string CashReceivedText => FormatHelper.FormatVnd(CashReceived);
    public string ChangeAmountText => FormatHelper.FormatVnd(ChangeAmount);
}

public sealed record OrderTicketDetailRow
{
    public int TicketId { get; init; }
    public int OrderId { get; init; }
    public string TicketCode { get; init; } = string.Empty;
    public int MovieId { get; init; }
    public string MovieTitle { get; init; } = string.Empty;
    public string RoomName { get; init; } = string.Empty;
    public string SeatCode { get; init; } = string.Empty;
    public string ShowtimeStartSql { get; init; } = string.Empty;
    public decimal UnitPrice { get; init; }
    public string Status { get; init; } = string.Empty;

    public string RoomAndSeat => $"{RoomName} • Ghế {SeatCode}";
    public string ShowtimeDisplay => FormatHelper.FormatSqlDateTime(ShowtimeStartSql, "dd/MM/yyyy HH:mm");
    public string UnitPriceText => FormatHelper.FormatVnd(UnitPrice);
    public string StatusDisplay => FormatHelper.FormatStatusLabel(Status);
    public bool IsEffective => Status != "Cancelled";
}

/// <summary>
/// Truy vấn SQLite bằng Dapper cho quản lý Hóa đơn và thống kê Doanh thu Admin.
/// </summary>
public static class OrderData
{
    private sealed class OrderSqlRow
    {
        public int OrderId { get; init; }
        public string OrderCode { get; init; } = string.Empty;
        public int? StaffUserId { get; init; }
        public string? StaffFullName { get; init; }
        public string? StaffUsername { get; init; }
        public int? CustomerId { get; init; }
        public string? CustomerFullName { get; init; }
        public string? CustomerPhone { get; init; }
        public string BookingChannel { get; init; } = string.Empty;
        public int TotalTickets { get; init; }
        public int ActiveTicketsCount { get; init; }
        public decimal SubTotalAmount { get; init; }
        public int PointsUsed { get; init; }
        public decimal DiscountAmount { get; init; }
        public decimal TotalAmount { get; init; }
        public string PaymentMethod { get; init; } = string.Empty;
        public decimal CashReceived { get; init; }
        public decimal ChangeAmount { get; init; }
        public string Status { get; init; } = string.Empty;
        public string? Note { get; init; }
        public string CreatedAtSql { get; init; } = string.Empty;
        public string? PaidAtSql { get; init; }
    }

    public static List<AdminOrderSummaryRow> LoadAllOrders()
    {
        using SqliteConnection conn = DatabaseHelper.OpenConnection();
        const string sql = """
            SELECT o.OrderId,
                   o.OrderCode,
                   o.StaffUserId,
                   u.FullName AS StaffFullName,
                   u.Username AS StaffUsername,
                   o.CustomerId,
                   c.FullName AS CustomerFullName,
                   c.Phone AS CustomerPhone,
                   o.BookingChannel,
                   o.TotalTickets,
                   (SELECT COUNT(*) FROM Tickets t WHERE t.OrderId = o.OrderId AND t.Status != 'Cancelled') AS ActiveTicketsCount,
                   o.SubTotalAmount,
                   o.PointsUsed,
                   o.DiscountAmount,
                   o.TotalAmount,
                   o.PaymentMethod,
                   o.CashReceived,
                   o.ChangeAmount,
                   o.Status,
                   o.Note,
                   o.CreatedAt AS CreatedAtSql,
                   o.PaidAt AS PaidAtSql
            FROM Orders o
            LEFT JOIN Customers c ON c.CustomerId = o.CustomerId
            LEFT JOIN Users u ON u.UserId = o.StaffUserId
            ORDER BY o.CreatedAt DESC, o.OrderId DESC;
            """;

        return conn.Query<OrderSqlRow>(sql)
            .Select(r =>
            {
                string staffDisplay = r.BookingChannel == "SelfService"
                    ? "Khách tự đặt (App)"
                    : r.StaffUserId is null
                        ? "Nhân viên quầy"
                        : FormatStaffLabel(CleanOrFallback(r.StaffFullName, "Nhân viên quầy"), CleanOrNull(r.StaffUsername));

                string customerName = r.CustomerId is null
                    ? "Khách vãng lai"
                    : CleanOrFallback(r.CustomerFullName, "Hội viên");
                string customerPhone = r.CustomerId is null
                    ? "Không có tài khoản"
                    : CleanOrFallback(r.CustomerPhone, "Chưa cập nhật");

                string? cleanPaidAt = CleanOrNull(r.PaidAtSql);
                string paidAtDisplay = r.Status == "Cancelled"
                    ? "Chưa thu tiền / Đã hủy"
                    : cleanPaidAt is null
                        ? "Chưa ghi nhận"
                        : FormatHelper.FormatSqlDateTime(cleanPaidAt, "dd/MM/yyyy HH:mm");

                return new AdminOrderSummaryRow(
                    r.OrderId,
                    r.OrderCode,
                    r.StaffUserId,
                    staffDisplay,
                    r.CustomerId,
                    customerName,
                    customerPhone,
                    r.BookingChannel,
                    r.BookingChannel == "SelfService" ? "Tự đặt" : "Tại quầy",
                    r.TotalTickets,
                    r.ActiveTicketsCount,
                    r.SubTotalAmount,
                    r.PointsUsed,
                    r.DiscountAmount,
                    r.TotalAmount,
                    r.PaymentMethod,
                    FormatHelper.FormatPaymentMethod(r.PaymentMethod),
                    r.CashReceived,
                    r.ChangeAmount,
                    r.Status,
                    FormatHelper.FormatStatusLabel(r.Status),
                    CleanOrFallback(r.Note, "Không có ghi chú"),
                    r.CreatedAtSql,
                    FormatHelper.FormatSqlDateTime(r.CreatedAtSql, "dd/MM/yyyy HH:mm"),
                    paidAtDisplay);
            })
            .ToList();
    }

    public static Dictionary<int, IReadOnlyList<OrderTicketDetailRow>> LoadAllOrderTickets()
    {
        using SqliteConnection conn = DatabaseHelper.OpenConnection();
        const string sql = """
            SELECT t.TicketId,
                   t.OrderId,
                   t.TicketCode,
                   s.MovieId,
                   m.Title AS MovieTitle,
                   r.Name AS RoomName,
                   se.SeatCode,
                   s.StartTime AS ShowtimeStartSql,
                   t.UnitPrice,
                   t.Status
            FROM Tickets t
            INNER JOIN Showtimes s ON s.ShowtimeId = t.ShowtimeId
            INNER JOIN Movies m ON m.MovieId = s.MovieId
            INNER JOIN Rooms r ON r.RoomId = s.RoomId
            INNER JOIN Seats se ON se.SeatId = t.SeatId
            ORDER BY t.OrderId DESC, t.TicketId ASC;
            """;

        return conn.Query<OrderTicketDetailRow>(sql)
            .GroupBy(t => t.OrderId)
            .ToDictionary(g => g.Key, g => (IReadOnlyList<OrderTicketDetailRow>)g.ToList());
    }

    private static string FormatStaffLabel(string fullName, string? username)
        => string.IsNullOrWhiteSpace(username) ? fullName : $"{fullName} (@{username})";

    private static string? CleanOrNull(string? value)
    {
        string trimmed = (value ?? string.Empty).Trim();
        return string.IsNullOrWhiteSpace(trimmed) ? null : trimmed;
    }

    private static string CleanOrFallback(string? value, string fallback)
        => CleanOrNull(value) ?? fallback;
}
