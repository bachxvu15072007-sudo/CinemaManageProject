using CinemaManageProject.Helpers;
using Dapper;
using Microsoft.Data.Sqlite;

namespace CinemaManageProject.Data;

public sealed record CheckInTicketRow
{
    public int TicketId { get; init; }
    public string TicketCode { get; init; } = string.Empty;
    public int OrderId { get; init; }
    public string OrderCode { get; init; } = string.Empty;
    public string OrderStatus { get; init; } = string.Empty;
    public int ShowtimeId { get; init; }
    public string ShowtimeStatus { get; init; } = string.Empty;
    public string MovieTitle { get; init; } = string.Empty;
    public string AgeRating { get; init; } = string.Empty;
    public string RoomName { get; init; } = string.Empty;
    public string Format { get; init; } = string.Empty;
    public string SeatCode { get; init; } = string.Empty;
    public string SeatType { get; init; } = string.Empty;
    public string StartTimeSql { get; init; } = string.Empty;
    public string EndTimeSql { get; init; } = string.Empty;
    public decimal UnitPrice { get; init; }
    public string CustomerName { get; init; } = string.Empty;
    public string CustomerPhone { get; init; } = string.Empty;
    public string TicketStatus { get; init; } = string.Empty;
    public string? CheckedInAtSql { get; init; }
    public string? CheckedInByName { get; init; }

    public string RoomAndSeat => $"{RoomName} • Ghế {SeatCode}";
    public string SeatTypeDisplay => FormatHelper.FormatSeatType(SeatType);
    public string StartTimeDisplay => FormatHelper.FormatSqlDateTime(StartTimeSql, "dd/MM/yyyy HH:mm");
    public string TimeRangeDisplay =>
        $"{FormatHelper.FormatSqlDateTime(StartTimeSql, "HH:mm")} - {FormatHelper.FormatSqlDateTime(EndTimeSql, "HH:mm")}";
    public string UnitPriceText => FormatHelper.FormatVnd(UnitPrice);
    public string StatusDisplay => FormatHelper.FormatStatusLabel(TicketStatus);
    public string CheckedInAtDisplay => string.IsNullOrWhiteSpace(CheckedInAtSql)
        ? "Chưa soát"
        : FormatHelper.FormatSqlDateTime(CheckedInAtSql, "dd/MM/yyyy HH:mm");
    public string CheckedInByDisplay => string.IsNullOrWhiteSpace(CheckedInByName) ? "—" : CheckedInByName;

    public bool IsPending => TicketStatus == "Booked";
    public bool IsCheckedIn => TicketStatus == "CheckedIn";
    public bool IsCancelled => TicketStatus == "Cancelled";

    public bool CanCheckIn =>
        TicketStatus == "Booked"
        && OrderStatus != "Cancelled"
        && ShowtimeStatus != "Cancelled";

    public string ValidationState => CanCheckIn
        ? "Valid"
        : IsCheckedIn
            ? "AlreadyCheckedIn"
            : "Blocked";

    public string ValidationSummary => ValidationState switch
    {
        "Valid" => "Vé hợp lệ — sẵn sàng xác nhận Check-in cho khách vào phòng chiếu.",
        "AlreadyCheckedIn" => $"Vé đã được check-in lúc {CheckedInAtDisplay} (nhân viên: {CheckedInByDisplay}).",
        _ when IsCancelled || OrderStatus == "Cancelled" => "Vé hoặc hóa đơn đã bị hủy, không được phép vào phòng chiếu.",
        _ when ShowtimeStatus == "Cancelled" => "Suất chiếu của vé này đã bị hủy.",
        _ => "Vé không đủ điều kiện check-in."
    };
}

public sealed record CheckInActionResult(bool Succeeded, CheckInTicketRow? UpdatedTicket, string? ErrorMessage);

/// <summary>
/// Truy vấn và thao tác SQLite bằng Dapper cho màn hình Soát vé Check-in của Staff.
/// </summary>
public static class CheckInData
{
    private const string BaseSelectSql = """
        SELECT t.TicketId,
               t.TicketCode,
               o.OrderId,
               o.OrderCode,
               o.Status AS OrderStatus,
               s.ShowtimeId,
               s.Status AS ShowtimeStatus,
               m.Title AS MovieTitle,
               m.AgeRating,
               r.Name AS RoomName,
               s.Format,
               se.SeatCode,
               se.SeatType,
               s.StartTime AS StartTimeSql,
               s.EndTime AS EndTimeSql,
               t.UnitPrice,
               COALESCE(c.FullName, 'Khách vãng lai') AS CustomerName,
               COALESCE(c.Phone, 'Không có SĐT') AS CustomerPhone,
               t.Status AS TicketStatus,
               t.CheckedInAt AS CheckedInAtSql,
               u.FullName AS CheckedInByName
        FROM Tickets t
        INNER JOIN Orders o ON o.OrderId = t.OrderId
        INNER JOIN Showtimes s ON s.ShowtimeId = t.ShowtimeId
        INNER JOIN Movies m ON m.MovieId = s.MovieId
        INNER JOIN Rooms r ON r.RoomId = s.RoomId
        INNER JOIN Seats se ON se.SeatId = t.SeatId
        LEFT JOIN Customers c ON c.CustomerId = o.CustomerId
        LEFT JOIN Users u ON u.UserId = t.CheckedInByUserId
        """;

    public static CheckInTicketRow? LookupTicketByCode(string ticketCode)
    {
        string cleanCode = (ticketCode ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(cleanCode))
        {
            return null;
        }

        using SqliteConnection conn = DatabaseHelper.OpenConnection();
        string sql = $"""
            {BaseSelectSql}
            WHERE LOWER(t.TicketCode) = LOWER(@code)
            LIMIT 1;
            """;
        return conn.QueryFirstOrDefault<CheckInTicketRow>(sql, new { code = cleanCode });
    }

    public static List<CheckInTicketRow> LoadAllTicketsForCheckIn()
    {
        using SqliteConnection conn = DatabaseHelper.OpenConnection();
        string sql = $"""
            {BaseSelectSql}
            ORDER BY CASE t.Status WHEN 'Booked' THEN 0 WHEN 'CheckedIn' THEN 1 ELSE 2 END,
                     CASE WHEN t.Status = 'Booked' THEN s.StartTime END ASC,
                     COALESCE(t.CheckedInAt, s.StartTime) DESC,
                     t.TicketId DESC;
            """;
        return conn.Query<CheckInTicketRow>(sql).AsList();
    }

    public static CheckInActionResult ConfirmCheckIn(int ticketId, int staffUserId)
    {
        if (staffUserId <= 0)
        {
            return new CheckInActionResult(false, null, "Không xác định được tài khoản nhân viên thực hiện soát vé.");
        }

        try
        {
            using SqliteConnection conn = DatabaseHelper.OpenConnection();
            using SqliteTransaction tx = conn.BeginTransaction();

            string byIdSql = $"""
                {BaseSelectSql}
                WHERE t.TicketId = @tid
                LIMIT 1;
                """;
            CheckInTicketRow? ticket = conn.QueryFirstOrDefault<CheckInTicketRow>(byIdSql, new { tid = ticketId }, tx);
            if (ticket is null)
            {
                return new CheckInActionResult(false, null, "Không tìm thấy vé cần soát.");
            }

            if (ticket.IsCheckedIn)
            {
                return new CheckInActionResult(
                    false,
                    ticket,
                    $"Vé {ticket.TicketCode} đã được check-in trước đó lúc {ticket.CheckedInAtDisplay} ({ticket.CheckedInByDisplay}).");
            }

            if (ticket.IsCancelled || ticket.OrderStatus == "Cancelled")
            {
                return new CheckInActionResult(false, ticket, $"Vé {ticket.TicketCode} thuộc trạng thái đã hủy, không thể check-in.");
            }

            if (ticket.ShowtimeStatus == "Cancelled")
            {
                return new CheckInActionResult(false, ticket, $"Suất chiếu của vé {ticket.TicketCode} đã bị hủy.");
            }

            int affected = conn.Execute(
                """
                UPDATE Tickets
                SET Status = 'CheckedIn',
                    CheckedInAt = datetime('now', 'localtime'),
                    CheckedInByUserId = @staffId
                WHERE TicketId = @tid
                  AND Status = 'Booked';
                """,
                new { staffId = staffUserId, tid = ticketId },
                tx);

            if (affected != 1)
            {
                return new CheckInActionResult(false, null, "Trạng thái vé vừa thay đổi, vui lòng tải lại danh sách.");
            }

            CheckInTicketRow? updated = conn.QueryFirstOrDefault<CheckInTicketRow>(byIdSql, new { tid = ticketId }, tx);
            tx.Commit();
            return new CheckInActionResult(true, updated, null);
        }
        catch (SqliteException ex)
        {
            return new CheckInActionResult(false, null, $"Không thể xác nhận check-in: {ex.Message}");
        }
    }
}
