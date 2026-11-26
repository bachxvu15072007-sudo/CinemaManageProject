using CinemaManageProject.Helpers;
using CinemaManageProject.Models;
using Microsoft.Data.Sqlite;

namespace CinemaManageProject.Data;

public sealed record AdminDashboardShowtimeDto(
    string MovieTitle,
    string RoomName,
    string StartTimeSql,
    string Format,
    int BookedCount,
    int ActiveSeats,
    decimal BasePlusSurcharge,
    string Status);

public sealed record AdminDashboardOrderDto(
    string OrderCode,
    string CustomerName,
    string BookingChannel,
    string PaymentMethod,
    decimal TotalAmount,
    string Status);

public sealed record AdminDashboardSnapshot(
    decimal TotalRevenue,
    decimal TodayRevenue,
    int TotalTickets,
    int CheckedInTickets,
    int NowShowingCount,
    int ComingSoonCount,
    int ActiveRooms,
    int TotalRooms,
    int MaintenanceSeats,
    IReadOnlyList<AdminDashboardShowtimeDto> Showtimes,
    IReadOnlyList<AdminDashboardOrderDto> RecentOrders);

public sealed record StaffDashboardShowtimeDto(
    string MovieTitle,
    string RoomName,
    string AgeRating,
    string StartTimeSql,
    string Format,
    int SeatsLeft,
    int TotalSeats,
    decimal Price);

public sealed record StaffDashboardOrderDto(
    string OrderCode,
    string CustomerName,
    int TotalTickets,
    string PaymentMethod,
    decimal TotalAmount,
    string CreatedAtSql);

public sealed record StaffDashboardSnapshot(
    int MyTickets,
    decimal MyRevenue,
    int OpenShowtimesCount,
    int PendingCheckInCount,
    IReadOnlyList<StaffDashboardShowtimeDto> OpenShowtimes,
    IReadOnlyList<StaffDashboardOrderDto> MyRecentOrders);

public sealed record CustomerDashboardMovieDto(
    int MovieId,
    string Title,
    string? OriginalTitle,
    string Director,
    string Cast,
    int DurationMinutes,
    string Language,
    string ReleaseDateSql,
    string AgeRating,
    string Description,
    string? PosterPath,
    string Status,
    string Genres,
    decimal? MinOpenPrice);

public sealed record CustomerDashboardTicketDto(
    string TicketCode,
    string MovieTitle,
    string RoomName,
    string SeatCode,
    string StartTimeSql,
    decimal UnitPrice,
    string Status);

public sealed record CustomerDashboardSnapshot(
    Customer? Profile,
    IReadOnlyList<CustomerDashboardMovieDto> Movies,
    IReadOnlyList<CustomerDashboardTicketDto> MyTickets);

/// <summary>
/// Truy vấn tổng hợp SQLite cho các màn hình Dashboard (Admin, Staff, Customer).
/// </summary>
public static class DashboardData
{
    public static AdminDashboardSnapshot LoadAdminDashboard()
    {
        using SqliteConnection conn = DatabaseHelper.OpenConnection();

        decimal totalRevenue = 0m;
        decimal todayRevenue = 0m;
        int totalTickets = 0;
        int checkedInTickets = 0;
        int nowShowingCount = 0;
        int comingSoonCount = 0;
        int activeRooms = 0;
        int totalRooms = 0;
        int maintenanceSeats = 0;

        using (SqliteCommand kpi = conn.CreateCommand())
        {
            kpi.CommandText = """
                SELECT
                    (SELECT COALESCE(SUM(TotalAmount), 0) FROM Orders WHERE Status != 'Cancelled'),
                    (SELECT COALESCE(SUM(TotalAmount), 0) FROM Orders WHERE Status != 'Cancelled' AND date(CreatedAt) = date('now', 'localtime')),
                    (SELECT COUNT(*) FROM Tickets WHERE Status != 'Cancelled'),
                    (SELECT COUNT(*) FROM Tickets WHERE Status = 'CheckedIn'),
                    (SELECT COUNT(*) FROM Movies WHERE Status = 'NowShowing'),
                    (SELECT COUNT(*) FROM Movies WHERE Status = 'ComingSoon'),
                    (SELECT COUNT(*) FROM Rooms WHERE IsActive = 1),
                    (SELECT COUNT(*) FROM Rooms),
                    (SELECT COUNT(*) FROM Seats WHERE IsActive = 0);
                """;
            using SqliteDataReader r = kpi.ExecuteReader();
            if (r.Read())
            {
                totalRevenue = DatabaseHelper.ReadDecimal(r, 0);
                todayRevenue = DatabaseHelper.ReadDecimal(r, 1);
                totalTickets = r.GetInt32(2);
                checkedInTickets = r.GetInt32(3);
                nowShowingCount = r.GetInt32(4);
                comingSoonCount = r.GetInt32(5);
                activeRooms = r.GetInt32(6);
                totalRooms = r.GetInt32(7);
                maintenanceSeats = r.GetInt32(8);
            }
        }

        List<AdminDashboardShowtimeDto> showtimes = [];
        using (SqliteCommand st = conn.CreateCommand())
        {
            st.CommandText = """
                SELECT m.Title, r.Name, s.StartTime, s.Format,
                       (SELECT COUNT(*) FROM Tickets t WHERE t.ShowtimeId = s.ShowtimeId AND t.Status IN ('Booked', 'CheckedIn')),
                       (SELECT COUNT(*) FROM Seats se WHERE se.RoomId = s.RoomId AND se.IsActive = 1),
                       s.BaseTicketPrice + r.BasePriceSurcharge, s.Status
                FROM Showtimes s
                INNER JOIN Movies m ON m.MovieId = s.MovieId
                INNER JOIN Rooms r ON r.RoomId = s.RoomId
                ORDER BY CASE WHEN s.Status IN ('OpenForSale', 'Scheduled') THEN 0 ELSE 1 END,
                         s.StartTime ASC
                LIMIT 12;
                """;
            using SqliteDataReader r = st.ExecuteReader();
            while (r.Read())
            {
                showtimes.Add(new AdminDashboardShowtimeDto(
                    r.GetString(0),
                    r.GetString(1),
                    r.GetString(2),
                    r.GetString(3),
                    r.GetInt32(4),
                    r.GetInt32(5),
                    DatabaseHelper.ReadDecimal(r, 6),
                    r.GetString(7)));
            }
        }

        List<AdminDashboardOrderDto> orders = [];
        using (SqliteCommand ord = conn.CreateCommand())
        {
            ord.CommandText = """
                SELECT o.OrderCode, COALESCE(c.FullName, 'Khách vãng lai'),
                       o.BookingChannel, o.PaymentMethod, o.TotalAmount, o.Status
                FROM Orders o
                LEFT JOIN Customers c ON c.CustomerId = o.CustomerId
                ORDER BY o.CreatedAt DESC
                LIMIT 8;
                """;
            using SqliteDataReader r = ord.ExecuteReader();
            while (r.Read())
            {
                orders.Add(new AdminDashboardOrderDto(
                    r.GetString(0),
                    r.GetString(1),
                    r.GetString(2),
                    r.GetString(3),
                    DatabaseHelper.ReadDecimal(r, 4),
                    r.GetString(5)));
            }
        }

        return new AdminDashboardSnapshot(
            totalRevenue,
            todayRevenue,
            totalTickets,
            checkedInTickets,
            nowShowingCount,
            comingSoonCount,
            activeRooms,
            totalRooms,
            maintenanceSeats,
            showtimes,
            orders);
    }

    public static StaffDashboardSnapshot LoadStaffDashboard(int staffId)
    {
        using SqliteConnection conn = DatabaseHelper.OpenConnection();

        int myTickets = 0;
        decimal myRevenue = 0m;
        int openShowtimesCount = 0;
        int pendingCheckInCount = 0;

        using (SqliteCommand kpi = conn.CreateCommand())
        {
            kpi.CommandText = """
                SELECT
                    (SELECT COALESCE(SUM(TotalTickets), 0) FROM Orders WHERE StaffUserId = @sid AND Status != 'Cancelled'),
                    (SELECT COALESCE(SUM(TotalAmount), 0) FROM Orders WHERE StaffUserId = @sid AND Status != 'Cancelled'),
                    (SELECT COUNT(*) FROM Showtimes WHERE Status = 'OpenForSale'),
                    (SELECT COUNT(*) FROM Tickets WHERE Status = 'Booked');
                """;
            kpi.Parameters.AddWithValue("@sid", staffId);

            using SqliteDataReader r = kpi.ExecuteReader();
            if (r.Read())
            {
                myTickets = r.GetInt32(0);
                myRevenue = DatabaseHelper.ReadDecimal(r, 1);
                openShowtimesCount = r.GetInt32(2);
                pendingCheckInCount = r.GetInt32(3);
            }
        }

        List<StaffDashboardShowtimeDto> openShowtimes = [];
        using (SqliteCommand st = conn.CreateCommand())
        {
            st.CommandText = """
                SELECT m.Title, r.Name, m.AgeRating, s.StartTime, s.Format,
                       (SELECT COUNT(*) FROM Seats se WHERE se.RoomId = s.RoomId AND se.IsActive = 1) -
                       (SELECT COUNT(*) FROM Tickets t WHERE t.ShowtimeId = s.ShowtimeId AND t.Status IN ('Booked', 'CheckedIn')),
                       (SELECT COUNT(*) FROM Seats se WHERE se.RoomId = s.RoomId AND se.IsActive = 1),
                       s.BaseTicketPrice + r.BasePriceSurcharge
                FROM Showtimes s
                INNER JOIN Movies m ON m.MovieId = s.MovieId
                INNER JOIN Rooms r ON r.RoomId = s.RoomId
                WHERE s.Status = 'OpenForSale'
                ORDER BY s.StartTime ASC
                LIMIT 12;
                """;
            using SqliteDataReader r = st.ExecuteReader();
            while (r.Read())
            {
                openShowtimes.Add(new StaffDashboardShowtimeDto(
                    r.GetString(0),
                    r.GetString(1),
                    r.GetString(2),
                    r.GetString(3),
                    r.GetString(4),
                    Math.Max(0, r.GetInt32(5)),
                    r.GetInt32(6),
                    DatabaseHelper.ReadDecimal(r, 7)));
            }
        }

        List<StaffDashboardOrderDto> myOrders = [];
        using (SqliteCommand ord = conn.CreateCommand())
        {
            ord.CommandText = """
                SELECT o.OrderCode, COALESCE(c.FullName, 'Khách vãng lai'),
                       o.TotalTickets, o.PaymentMethod, o.TotalAmount, o.CreatedAt
                FROM Orders o
                LEFT JOIN Customers c ON c.CustomerId = o.CustomerId
                WHERE o.StaffUserId = @sid
                ORDER BY o.CreatedAt DESC
                LIMIT 8;
                """;
            ord.Parameters.AddWithValue("@sid", staffId);

            using SqliteDataReader r = ord.ExecuteReader();
            while (r.Read())
            {
                myOrders.Add(new StaffDashboardOrderDto(
                    r.GetString(0),
                    r.GetString(1),
                    r.GetInt32(2),
                    r.GetString(3),
                    DatabaseHelper.ReadDecimal(r, 4),
                    r.GetString(5)));
            }
        }

        return new StaffDashboardSnapshot(
            myTickets,
            myRevenue,
            openShowtimesCount,
            pendingCheckInCount,
            openShowtimes,
            myOrders);
    }

    public static CustomerDashboardSnapshot LoadCustomerDashboard(int? userId, Customer? fallbackProfile)
    {
        using SqliteConnection conn = DatabaseHelper.OpenConnection();

        Customer? profile = userId.HasValue
            ? CustomerData.LoadCustomerProfile(conn, userId.Value) ?? fallbackProfile
            : fallbackProfile;

        List<CustomerDashboardMovieDto> movies = [];
        using (SqliteCommand mov = conn.CreateCommand())
        {
            mov.CommandText = """
                SELECT m.MovieId, m.Title, m.OriginalTitle, m.Director, m.Cast,
                       m.DurationMinutes, m.Language, m.ReleaseDate, m.AgeRating,
                       m.Description, m.PosterPath, m.Status,
                       COALESCE((SELECT GROUP_CONCAT(g.Name, ', ')
                                 FROM MovieGenres mg INNER JOIN Genres g ON g.GenreId = mg.GenreId
                                 WHERE mg.MovieId = m.MovieId), 'Tổng hợp'),
                       (SELECT MIN(s.BaseTicketPrice + r.BasePriceSurcharge)
                        FROM Showtimes s
                        INNER JOIN Rooms r ON r.RoomId = s.RoomId
                        WHERE s.MovieId = m.MovieId AND s.Status = 'OpenForSale')
                FROM Movies m
                WHERE m.Status IN ('NowShowing', 'ComingSoon')
                ORDER BY CASE m.Status WHEN 'NowShowing' THEN 0 ELSE 1 END, m.ReleaseDate DESC;
                """;
            using SqliteDataReader r = mov.ExecuteReader();
            while (r.Read())
            {
                movies.Add(new CustomerDashboardMovieDto(
                    MovieId: r.GetInt32(0),
                    Title: r.GetString(1),
                    OriginalTitle: r.IsDBNull(2) ? null : r.GetString(2),
                    Director: ReadOptionalText(r, 3, "Đang cập nhật"),
                    Cast: ReadOptionalText(r, 4, "Đang cập nhật"),
                    DurationMinutes: r.GetInt32(5),
                    Language: r.GetString(6),
                    ReleaseDateSql: r.GetString(7),
                    AgeRating: r.GetString(8),
                    Description: ReadOptionalText(r, 9, "Chưa có mô tả giới thiệu cho phim này."),
                    PosterPath: r.IsDBNull(10) ? null : r.GetString(10),
                    Status: r.GetString(11),
                    Genres: r.GetString(12),
                    MinOpenPrice: r.IsDBNull(13) ? null : DatabaseHelper.ReadDecimal(r, 13)));
            }
        }

        List<CustomerDashboardTicketDto> tickets = [];
        if (profile is not null)
        {
            using SqliteCommand tk = conn.CreateCommand();
            tk.CommandText = """
                SELECT t.TicketCode, m.Title, r.Name, se.SeatCode, s.StartTime, t.UnitPrice, t.Status
                FROM Tickets t
                INNER JOIN Orders o ON o.OrderId = t.OrderId
                INNER JOIN Showtimes s ON s.ShowtimeId = t.ShowtimeId
                INNER JOIN Movies m ON m.MovieId = s.MovieId
                INNER JOIN Rooms r ON r.RoomId = s.RoomId
                INNER JOIN Seats se ON se.SeatId = t.SeatId
                WHERE o.CustomerId = @cid
                ORDER BY s.StartTime DESC
                LIMIT 15;
                """;
            tk.Parameters.AddWithValue("@cid", profile.CustomerId);

            using SqliteDataReader r = tk.ExecuteReader();
            while (r.Read())
            {
                tickets.Add(new CustomerDashboardTicketDto(
                    r.GetString(0),
                    r.GetString(1),
                    r.GetString(2),
                    r.GetString(3),
                    r.GetString(4),
                    DatabaseHelper.ReadDecimal(r, 5),
                    r.GetString(6)));
            }
        }

        return new CustomerDashboardSnapshot(profile, movies, tickets);
    }

    private static string ReadOptionalText(SqliteDataReader reader, int ordinal, string fallback)
    {
        if (reader.IsDBNull(ordinal))
        {
            return fallback;
        }

        string text = reader.GetString(ordinal).Trim();
        return string.IsNullOrWhiteSpace(text) ? fallback : text;
    }
}
