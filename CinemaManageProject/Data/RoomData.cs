using System.Globalization;
using CinemaManageProject.Helpers;
using Microsoft.Data.Sqlite;

namespace CinemaManageProject.Data;

public sealed record AdminRoomRow(
    int RoomId,
    string Name,
    string RoomType,
    int TotalRows,
    int TotalCols,
    decimal BasePriceSurcharge,
    bool IsActive,
    int TotalSeats,
    int ActiveSeats,
    int MaintenanceSeats,
    int StandardSeats,
    int VipSeats,
    int CoupleSeats)
{
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

public sealed record RoomSeatRow(
    int SeatId,
    int RoomId,
    string RowLabel,
    int SeatNumber,
    string SeatCode,
    string SeatType,
    decimal PriceMultiplier,
    int RowIndex,
    int ColIndex,
    bool IsActive);

public sealed record SeatOrderRow(
    string OrderCode,
    string TicketCode,
    string MovieTitle,
    string ShowtimeStart,
    string CustomerName,
    string TicketStatus);

/// <summary>
/// Truy vấn SQLite chỉ đọc thông tin cố định của phòng chiếu, sơ đồ ghế, đơn hàng theo ghế và cập nhật bảo trì ghế.
/// </summary>
public static class RoomData
{
    public static List<AdminRoomRow> LoadAllRooms()
    {
        var rooms = new List<AdminRoomRow>();
        using SqliteConnection conn = DatabaseHelper.OpenConnection();
        using SqliteCommand cmd = conn.CreateCommand();
        cmd.CommandText = """
            SELECT r.RoomId, r.Name, r.RoomType, r.TotalRows, r.TotalCols, r.BasePriceSurcharge, r.IsActive,
                   (SELECT COUNT(*) FROM Seats s WHERE s.RoomId = r.RoomId),
                   (SELECT COUNT(*) FROM Seats s WHERE s.RoomId = r.RoomId AND s.IsActive = 1),
                   (SELECT COUNT(*) FROM Seats s WHERE s.RoomId = r.RoomId AND s.IsActive = 0),
                   (SELECT COUNT(*) FROM Seats s WHERE s.RoomId = r.RoomId AND s.SeatType = 'Standard'),
                   (SELECT COUNT(*) FROM Seats s WHERE s.RoomId = r.RoomId AND s.SeatType = 'VIP'),
                   (SELECT COUNT(*) FROM Seats s WHERE s.RoomId = r.RoomId AND s.SeatType = 'Couple')
            FROM Rooms r
            ORDER BY r.RoomId ASC;
            """;

        using SqliteDataReader r = cmd.ExecuteReader();
        while (r.Read())
        {
            rooms.Add(new AdminRoomRow(
                r.GetInt32(0),
                r.GetString(1),
                r.GetString(2),
                r.GetInt32(3),
                r.GetInt32(4),
                DatabaseHelper.ReadDecimal(r, 5),
                r.GetInt32(6) == 1,
                r.GetInt32(7),
                r.GetInt32(8),
                r.GetInt32(9),
                r.GetInt32(10),
                r.GetInt32(11),
                r.GetInt32(12)));
        }

        return rooms;
    }

    public static List<RoomSeatRow> LoadSeatsByRoom(int roomId)
    {
        var seats = new List<RoomSeatRow>();
        using SqliteConnection conn = DatabaseHelper.OpenConnection();
        using SqliteCommand cmd = conn.CreateCommand();
        cmd.CommandText = """
            SELECT SeatId, RoomId, RowLabel, SeatNumber, SeatCode, SeatType, PriceMultiplier, RowIndex, ColIndex, IsActive
            FROM Seats
            WHERE RoomId = @rid
            ORDER BY RowIndex ASC, ColIndex ASC;
            """;
        cmd.Parameters.AddWithValue("@rid", roomId);

        using SqliteDataReader r = cmd.ExecuteReader();
        while (r.Read())
        {
            seats.Add(new RoomSeatRow(
                r.GetInt32(0),
                r.GetInt32(1),
                r.GetString(2),
                r.GetInt32(3),
                r.GetString(4),
                r.GetString(5),
                DatabaseHelper.ReadDecimal(r, 6),
                r.GetInt32(7),
                r.GetInt32(8),
                r.GetInt32(9) == 1));
        }

        return seats;
    }

    public static List<SeatOrderRow> LoadSeatOrders(int seatId)
    {
        var rows = new List<SeatOrderRow>();
        using SqliteConnection conn = DatabaseHelper.OpenConnection();
        using SqliteCommand cmd = conn.CreateCommand();
        cmd.CommandText = """
            SELECT o.OrderCode,
                   t.TicketCode,
                   m.Title,
                   s.StartTime,
                   COALESCE(c.FullName, 'Khách vãng lai'),
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
        cmd.Parameters.AddWithValue("@sid", seatId);

        using SqliteDataReader r = cmd.ExecuteReader();
        while (r.Read())
        {
            rows.Add(new SeatOrderRow(
                r.GetString(0),
                r.GetString(1),
                r.GetString(2),
                FormatHelper.FormatSqlDateTime(r.GetString(3), "dd/MM/yyyy HH:mm"),
                r.GetString(4),
                FormatHelper.FormatStatusLabel(r.GetString(5))));
        }

        return rows;
    }

    public static int CountActiveTicketsForSeat(int seatId)
    {
        using SqliteConnection conn = DatabaseHelper.OpenConnection();
        using SqliteCommand cmd = conn.CreateCommand();
        cmd.CommandText = """
            SELECT COUNT(*)
            FROM Tickets t
            INNER JOIN Showtimes s ON s.ShowtimeId = t.ShowtimeId
            WHERE t.SeatId = @sid
              AND t.Status IN ('Booked', 'CheckedIn')
              AND s.Status IN ('Scheduled', 'OpenForSale');
            """;
        cmd.Parameters.AddWithValue("@sid", seatId);
        return Convert.ToInt32(cmd.ExecuteScalar(), CultureInfo.InvariantCulture);
    }

    public static void SetSeatActive(int seatId, bool isActive)
    {
        using SqliteConnection conn = DatabaseHelper.OpenConnection();
        using SqliteCommand cmd = conn.CreateCommand();
        cmd.CommandText = "UPDATE Seats SET IsActive = @active WHERE SeatId = @id;";
        cmd.Parameters.AddWithValue("@active", isActive ? 1 : 0);
        cmd.Parameters.AddWithValue("@id", seatId);
        cmd.ExecuteNonQuery();
    }
}
