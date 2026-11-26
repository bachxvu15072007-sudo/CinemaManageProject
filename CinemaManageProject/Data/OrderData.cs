using CinemaManageProject.Helpers;
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

public sealed record OrderTicketDetailRow(
    int TicketId,
    int OrderId,
    string TicketCode,
    int MovieId,
    string MovieTitle,
    string RoomName,
    string SeatCode,
    string ShowtimeStartSql,
    decimal UnitPrice,
    string Status)
{
    public string RoomAndSeat => $"{RoomName} • Ghế {SeatCode}";
    public string ShowtimeDisplay => FormatHelper.FormatSqlDateTime(ShowtimeStartSql, "dd/MM/yyyy HH:mm");
    public string UnitPriceText => FormatHelper.FormatVnd(UnitPrice);
    public string StatusDisplay => FormatHelper.FormatStatusLabel(Status);
    public bool IsEffective => Status != "Cancelled";
}

/// <summary>
/// Truy vấn SQLite chỉ đọc cho quản lý Hóa đơn và thống kê Doanh thu Admin.
/// </summary>
public static class OrderData
{
    public static List<AdminOrderSummaryRow> LoadAllOrders()
    {
        var orders = new List<AdminOrderSummaryRow>();
        using SqliteConnection conn = DatabaseHelper.OpenConnection();
        using SqliteCommand cmd = conn.CreateCommand();
        cmd.CommandText = """
            SELECT o.OrderId,
                   o.OrderCode,
                   o.StaffUserId,
                   u.FullName,
                   u.Username,
                   o.CustomerId,
                   c.FullName,
                   c.Phone,
                   o.BookingChannel,
                   o.TotalTickets,
                   (SELECT COUNT(*) FROM Tickets t WHERE t.OrderId = o.OrderId AND t.Status != 'Cancelled'),
                   o.SubTotalAmount,
                   o.PointsUsed,
                   o.DiscountAmount,
                   o.TotalAmount,
                   o.PaymentMethod,
                   o.CashReceived,
                   o.ChangeAmount,
                   o.Status,
                   o.Note,
                   o.CreatedAt,
                   o.PaidAt
            FROM Orders o
            LEFT JOIN Customers c ON c.CustomerId = o.CustomerId
            LEFT JOIN Users u ON u.UserId = o.StaffUserId
            ORDER BY o.CreatedAt DESC, o.OrderId DESC;
            """;

        using SqliteDataReader r = cmd.ExecuteReader();
        while (r.Read())
        {
            int? staffId = r.IsDBNull(2) ? null : r.GetInt32(2);
            int? customerId = r.IsDBNull(5) ? null : r.GetInt32(5);
            string channel = r.GetString(8);
            string paymentMethod = r.GetString(15);
            string status = r.GetString(18);
            string createdAtSql = r.GetString(20);

            string staffDisplay = channel == "SelfService"
                ? "Khách tự đặt (App)"
                : staffId is null
                    ? "Nhân viên quầy"
                    : FormatStaffLabel(ReadOrFallback(r, 3, "Nhân viên quầy"), ReadOrNull(r, 4));

            string customerName = customerId is null
                ? "Khách vãng lai"
                : ReadOrFallback(r, 6, "Hội viên");
            string customerPhone = customerId is null
                ? "Không có tài khoản"
                : ReadOrFallback(r, 7, "Chưa cập nhật");

            string paidAtDisplay = status == "Cancelled"
                ? "Chưa thu tiền / Đã hủy"
                : r.IsDBNull(21)
                    ? "Chưa ghi nhận"
                    : FormatHelper.FormatSqlDateTime(r.GetString(21), "dd/MM/yyyy HH:mm");

            orders.Add(new AdminOrderSummaryRow(
                r.GetInt32(0),
                r.GetString(1),
                staffId,
                staffDisplay,
                customerId,
                customerName,
                customerPhone,
                channel,
                channel == "SelfService" ? "Tự đặt" : "Tại quầy",
                r.GetInt32(9),
                r.GetInt32(10),
                DatabaseHelper.ReadDecimal(r, 11),
                r.GetInt32(12),
                DatabaseHelper.ReadDecimal(r, 13),
                DatabaseHelper.ReadDecimal(r, 14),
                paymentMethod,
                FormatHelper.FormatPaymentMethod(paymentMethod),
                DatabaseHelper.ReadDecimal(r, 16),
                DatabaseHelper.ReadDecimal(r, 17),
                status,
                FormatHelper.FormatStatusLabel(status),
                ReadOrFallback(r, 19, "Không có ghi chú"),
                createdAtSql,
                FormatHelper.FormatSqlDateTime(createdAtSql, "dd/MM/yyyy HH:mm"),
                paidAtDisplay));
        }

        return orders;
    }

    public static Dictionary<int, IReadOnlyList<OrderTicketDetailRow>> LoadAllOrderTickets()
    {
        var grouped = new Dictionary<int, List<OrderTicketDetailRow>>();
        using SqliteConnection conn = DatabaseHelper.OpenConnection();
        using SqliteCommand cmd = conn.CreateCommand();
        cmd.CommandText = """
            SELECT t.TicketId,
                   t.OrderId,
                   t.TicketCode,
                   s.MovieId,
                   m.Title,
                   r.Name,
                   se.SeatCode,
                   s.StartTime,
                   t.UnitPrice,
                   t.Status
            FROM Tickets t
            INNER JOIN Showtimes s ON s.ShowtimeId = t.ShowtimeId
            INNER JOIN Movies m ON m.MovieId = s.MovieId
            INNER JOIN Rooms r ON r.RoomId = s.RoomId
            INNER JOIN Seats se ON se.SeatId = t.SeatId
            ORDER BY t.OrderId DESC, t.TicketId ASC;
            """;

        using SqliteDataReader r = cmd.ExecuteReader();
        while (r.Read())
        {
            int orderId = r.GetInt32(1);
            if (!grouped.TryGetValue(orderId, out List<OrderTicketDetailRow>? list))
            {
                list = [];
                grouped[orderId] = list;
            }

            list.Add(new OrderTicketDetailRow(
                r.GetInt32(0),
                orderId,
                r.GetString(2),
                r.GetInt32(3),
                r.GetString(4),
                r.GetString(5),
                r.GetString(6),
                r.GetString(7),
                DatabaseHelper.ReadDecimal(r, 8),
                r.GetString(9)));
        }

        return grouped.ToDictionary(kv => kv.Key, kv => (IReadOnlyList<OrderTicketDetailRow>)kv.Value);
    }

    private static string FormatStaffLabel(string fullName, string? username)
        => string.IsNullOrWhiteSpace(username) ? fullName : $"{fullName} (@{username})";

    private static string? ReadOrNull(SqliteDataReader reader, int ordinal)
    {
        if (reader.IsDBNull(ordinal))
        {
            return null;
        }

        string value = reader.GetString(ordinal).Trim();
        return string.IsNullOrWhiteSpace(value) ? null : value;
    }

    private static string ReadOrFallback(SqliteDataReader reader, int ordinal, string fallback)
        => ReadOrNull(reader, ordinal) ?? fallback;
}
