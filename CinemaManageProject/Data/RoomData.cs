using CinemaManageProject.Helpers;
using Dapper;
using Microsoft.Data.Sqlite;

namespace CinemaManageProject.Data;

public sealed record AdminRoomRow
{
    public int RoomId { get; init; }
    public string Name { get; init; } = string.Empty;
    public string RoomType { get; init; } = string.Empty;
    public int TotalRows { get; init; }
    public int TotalCols { get; init; }
    public decimal BasePriceSurcharge { get; init; }
    public bool IsActive { get; init; }
    public int TotalSeats { get; init; }
    public int ActiveSeats { get; init; }
    public int MaintenanceSeats { get; init; }
    public int StandardSeats { get; init; }
    public int VipSeats { get; init; }
    public int CoupleSeats { get; init; }

    public string DimensionsText => $"{TotalRows} hàng × {TotalCols} ghế đơn";
    public string RoomTypeDisplay => FormatHelper.FormatRoomType(RoomType);
    public string SurchargeText => BasePriceSurcharge > 0 ? $"+{FormatHelper.FormatVnd(BasePriceSurcharge)}" : "0đ";
    public string StatusDisplay => IsActive ? "Hoạt động" : "Tạm đóng";
    public string SeatSummaryText => MaintenanceSeats > 0
        ? $"{ActiveSeats}/{TotalSeats} khả dụng ({MaintenanceSeats} bảo trì)"
        : $"{TotalSeats}/{TotalSeats} khả dụng";
    public string SeatBreakdownText => $"{StandardSeats} Thường • {VipSeats} VIP • {CoupleSeats} Đôi";
    public string TotalSeatsText => $"{TotalSeats} ghế";
}

public sealed record RoomSeatRow
{
    public int SeatId { get; init; }
    public int RoomId { get; init; }
    public string RowLabel { get; init; } = string.Empty;
    public int SeatNumber { get; init; }
    public string SeatCode { get; init; } = string.Empty;
    public string SeatType { get; init; } = string.Empty;
    public decimal PriceMultiplier { get; init; }
    public int RowIndex { get; init; }
    public int ColIndex { get; init; }
    public bool IsActive { get; init; }
}

public sealed record SeatOrderRow(
    string OrderCode,
    string TicketCode,
    string MovieTitle,
    string ShowtimeStart,
    string CustomerName,
    string TicketStatus);

/// <summary>
/// Truy vấn SQLite bằng Dapper cho phòng chiếu, sơ đồ ghế, đơn hàng theo ghế và cập nhật bảo trì ghế.
/// </summary>
public static class RoomData
{
    private sealed class SeatOrderSqlRow
    {
        public string OrderCode { get; init; } = string.Empty;
        public string TicketCode { get; init; } = string.Empty;
        public string MovieTitle { get; init; } = string.Empty;
        public string StartTimeSql { get; init; } = string.Empty;
        public string CustomerName { get; init; } = string.Empty;
        public string Status { get; init; } = string.Empty;
    }

    public static List<AdminRoomRow> LoadAllRooms()
    {
        using SqliteConnection conn = DatabaseHelper.OpenConnection();
        const string sql = """
            SELECT r.RoomId, r.Name, r.RoomType, r.TotalRows, r.TotalCols, r.BasePriceSurcharge, r.IsActive,
                   (SELECT COUNT(*) FROM Seats s WHERE s.RoomId = r.RoomId) AS TotalSeats,
                   (SELECT COUNT(*) FROM Seats s WHERE s.RoomId = r.RoomId AND s.IsActive = 1) AS ActiveSeats,
                   (SELECT COUNT(*) FROM Seats s WHERE s.RoomId = r.RoomId AND s.IsActive = 0) AS MaintenanceSeats,
                   (SELECT COUNT(*) FROM Seats s WHERE s.RoomId = r.RoomId AND s.SeatType = 'Standard') AS StandardSeats,
                   (SELECT COUNT(*) FROM Seats s WHERE s.RoomId = r.RoomId AND s.SeatType = 'VIP') AS VipSeats,
                   (SELECT COUNT(*) FROM Seats s WHERE s.RoomId = r.RoomId AND s.SeatType = 'Couple') AS CoupleSeats
            FROM Rooms r
            ORDER BY r.RoomId ASC;
            """;
        return conn.Query<AdminRoomRow>(sql).AsList();
    }

    public static List<RoomSeatRow> LoadSeatsByRoom(int roomId)
    {
        using SqliteConnection conn = DatabaseHelper.OpenConnection();
        const string sql = """
            SELECT SeatId, RoomId, RowLabel, SeatNumber, SeatCode, SeatType, PriceMultiplier, RowIndex, ColIndex, IsActive
            FROM Seats
            WHERE RoomId = @rid
            ORDER BY RowIndex ASC, ColIndex ASC;
            """;
        return conn.Query<RoomSeatRow>(sql, new { rid = roomId }).AsList();
    }

    public static List<SeatOrderRow> LoadSeatOrders(int seatId)
    {
        using SqliteConnection conn = DatabaseHelper.OpenConnection();
        const string sql = """
            SELECT o.OrderCode,
                   t.TicketCode,
                   m.Title AS MovieTitle,
                   s.StartTime AS StartTimeSql,
                   COALESCE(c.FullName, 'Khách vãng lai') AS CustomerName,
                   t.Status
            FROM Tickets t
            INNER JOIN Orders o ON o.OrderId = t.OrderId
            INNER JOIN Showtimes s ON s.ShowtimeId = t.ShowtimeId
            INNER JOIN Movies m ON m.MovieId = s.MovieId
            LEFT JOIN Customers c ON c.CustomerId = o.CustomerId
            WHERE t.SeatId = @sid
            ORDER BY s.StartTime DESC, t.TicketId DESC
            LIMIT 20;
            """;

        return conn.Query<SeatOrderSqlRow>(sql, new { sid = seatId })
            .Select(r => new SeatOrderRow(
                r.OrderCode,
                r.TicketCode,
                r.MovieTitle,
                FormatHelper.FormatSqlDateTime(r.StartTimeSql, "dd/MM/yyyy HH:mm"),
                r.CustomerName,
                FormatHelper.FormatStatusLabel(r.Status)))
            .ToList();
    }

    public static int CountActiveTicketsForSeat(int seatId)
    {
        using SqliteConnection conn = DatabaseHelper.OpenConnection();
        const string sql = """
            SELECT COUNT(*)
            FROM Tickets t
            INNER JOIN Showtimes s ON s.ShowtimeId = t.ShowtimeId
            WHERE t.SeatId = @sid
              AND t.Status IN ('Booked', 'CheckedIn')
              AND s.Status IN ('Scheduled', 'OpenForSale');
            """;
        return conn.ExecuteScalar<int>(sql, new { sid = seatId });
    }

    public static void SetSeatActive(int seatId, bool isActive)
    {
        using SqliteConnection conn = DatabaseHelper.OpenConnection();
        conn.Execute(
            "UPDATE Seats SET IsActive = @active WHERE SeatId = @id;",
            new { active = isActive ? 1 : 0, id = seatId });
    }
}
