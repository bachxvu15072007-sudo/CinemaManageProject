using CinemaManageProject.Helpers;
using CinemaManageProject.Models;
using Dapper;
using Microsoft.Data.Sqlite;

namespace CinemaManageProject.Data;

public sealed record AdminDashboardShowtimeDto
{
    public string MovieTitle { get; init; } = string.Empty;
    public string RoomName { get; init; } = string.Empty;
    public string StartTimeSql { get; init; } = string.Empty;
    public string Format { get; init; } = string.Empty;
    public int BookedCount { get; init; }
    public int ActiveSeats { get; init; }
    public decimal BasePlusSurcharge { get; init; }
    public string Status { get; init; } = string.Empty;
}

public sealed record AdminDashboardOrderDto
{
    public string OrderCode { get; init; } = string.Empty;
    public string CustomerName { get; init; } = string.Empty;
    public string BookingChannel { get; init; } = string.Empty;
    public string PaymentMethod { get; init; } = string.Empty;
    public decimal TotalAmount { get; init; }
    public string Status { get; init; } = string.Empty;
}

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

public sealed record StaffDashboardShowtimeDto
{
    public string MovieTitle { get; init; } = string.Empty;
    public string RoomName { get; init; } = string.Empty;
    public string AgeRating { get; init; } = string.Empty;
    public string StartTimeSql { get; init; } = string.Empty;
    public string Format { get; init; } = string.Empty;
    public int SeatsLeft { get; init; }
    public int TotalSeats { get; init; }
    public decimal Price { get; init; }
}

public sealed record StaffDashboardOrderDto
{
    public string OrderCode { get; init; } = string.Empty;
    public string CustomerName { get; init; } = string.Empty;
    public int TotalTickets { get; init; }
    public string PaymentMethod { get; init; } = string.Empty;
    public decimal TotalAmount { get; init; }
    public string CreatedAtSql { get; init; } = string.Empty;
}

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

public sealed record CustomerDashboardTicketDto
{
    public string TicketCode { get; init; } = string.Empty;
    public string MovieTitle { get; init; } = string.Empty;
    public string RoomName { get; init; } = string.Empty;
    public string SeatCode { get; init; } = string.Empty;
    public string StartTimeSql { get; init; } = string.Empty;
    public decimal UnitPrice { get; init; }
    public string Status { get; init; } = string.Empty;
}

public sealed record CustomerDashboardSnapshot(
    Customer? Profile,
    IReadOnlyList<CustomerDashboardMovieDto> Movies,
    IReadOnlyList<CustomerDashboardTicketDto> MyTickets);

/// <summary>
/// Truy vấn tổng hợp SQLite bằng Dapper cho các màn hình Dashboard (Admin, Staff, Customer).
/// </summary>
public static class DashboardData
{
    private sealed class AdminKpiRow
    {
        public decimal TotalRevenue { get; init; }
        public decimal TodayRevenue { get; init; }
        public int TotalTickets { get; init; }
        public int CheckedInTickets { get; init; }
        public int NowShowingCount { get; init; }
        public int ComingSoonCount { get; init; }
        public int ActiveRooms { get; init; }
        public int TotalRooms { get; init; }
        public int MaintenanceSeats { get; init; }
    }

    private sealed class StaffKpiRow
    {
        public int MyTickets { get; init; }
        public decimal MyRevenue { get; init; }
        public int OpenShowtimesCount { get; init; }
        public int PendingCheckInCount { get; init; }
    }

    private sealed class CustomerMovieSqlRow
    {
        public int MovieId { get; init; }
        public string Title { get; init; } = string.Empty;
        public string? OriginalTitle { get; init; }
        public string? Director { get; init; }
        public string? Cast { get; init; }
        public int DurationMinutes { get; init; }
        public string Language { get; init; } = string.Empty;
        public string ReleaseDateSql { get; init; } = string.Empty;
        public string AgeRating { get; init; } = string.Empty;
        public string? Description { get; init; }
        public string? PosterPath { get; init; }
        public string Status { get; init; } = string.Empty;
        public string Genres { get; init; } = string.Empty;
        public decimal? MinOpenPrice { get; init; }
    }

    public static AdminDashboardSnapshot LoadAdminDashboard()
    {
        using SqliteConnection conn = DatabaseHelper.OpenConnection();

        const string kpiSql = """
            SELECT
                (SELECT COALESCE(SUM(TotalAmount), 0) FROM Orders WHERE Status != 'Cancelled') AS TotalRevenue,
                (SELECT COALESCE(SUM(TotalAmount), 0) FROM Orders WHERE Status != 'Cancelled' AND date(CreatedAt) = date('now', 'localtime')) AS TodayRevenue,
                (SELECT COUNT(*) FROM Tickets WHERE Status != 'Cancelled') AS TotalTickets,
                (SELECT COUNT(*) FROM Tickets WHERE Status = 'CheckedIn') AS CheckedInTickets,
                (SELECT COUNT(*) FROM Movies WHERE Status = 'NowShowing') AS NowShowingCount,
                (SELECT COUNT(*) FROM Movies WHERE Status = 'ComingSoon') AS ComingSoonCount,
                (SELECT COUNT(*) FROM Rooms WHERE IsActive = 1) AS ActiveRooms,
                (SELECT COUNT(*) FROM Rooms) AS TotalRooms,
                (SELECT COUNT(*) FROM Seats WHERE IsActive = 0) AS MaintenanceSeats;
            """;
        AdminKpiRow kpi = conn.QuerySingle<AdminKpiRow>(kpiSql);

        const string showtimeSql = """
            SELECT m.Title AS MovieTitle,
                   r.Name AS RoomName,
                   s.StartTime AS StartTimeSql,
                   s.Format,
                   (SELECT COUNT(*) FROM Tickets t WHERE t.ShowtimeId = s.ShowtimeId AND t.Status IN ('Booked', 'CheckedIn')) AS BookedCount,
                   (SELECT COUNT(*) FROM Seats se WHERE se.RoomId = s.RoomId AND se.IsActive = 1) AS ActiveSeats,
                   s.BaseTicketPrice + r.BasePriceSurcharge AS BasePlusSurcharge,
                   s.Status
            FROM Showtimes s
            INNER JOIN Movies m ON m.MovieId = s.MovieId
            INNER JOIN Rooms r ON r.RoomId = s.RoomId
            ORDER BY CASE WHEN s.Status IN ('OpenForSale', 'Scheduled') THEN 0 ELSE 1 END,
                     s.StartTime ASC
            LIMIT 12;
            """;
        List<AdminDashboardShowtimeDto> showtimes = conn.Query<AdminDashboardShowtimeDto>(showtimeSql).AsList();

        const string orderSql = """
            SELECT o.OrderCode,
                   COALESCE(c.FullName, 'Khách vãng lai') AS CustomerName,
                   o.BookingChannel,
                   o.PaymentMethod,
                   o.TotalAmount,
                   o.Status
            FROM Orders o
            LEFT JOIN Customers c ON c.CustomerId = o.CustomerId
            ORDER BY o.CreatedAt DESC
            LIMIT 8;
            """;
        List<AdminDashboardOrderDto> orders = conn.Query<AdminDashboardOrderDto>(orderSql).AsList();

        return new AdminDashboardSnapshot(
            kpi.TotalRevenue,
            kpi.TodayRevenue,
            kpi.TotalTickets,
            kpi.CheckedInTickets,
            kpi.NowShowingCount,
            kpi.ComingSoonCount,
            kpi.ActiveRooms,
            kpi.TotalRooms,
            kpi.MaintenanceSeats,
            showtimes,
            orders);
    }

    public static StaffDashboardSnapshot LoadStaffDashboard(int staffId)
    {
        using SqliteConnection conn = DatabaseHelper.OpenConnection();

        const string kpiSql = """
            SELECT
                (SELECT COALESCE(SUM(TotalTickets), 0) FROM Orders WHERE StaffUserId = @sid AND Status != 'Cancelled') AS MyTickets,
                (SELECT COALESCE(SUM(TotalAmount), 0) FROM Orders WHERE StaffUserId = @sid AND Status != 'Cancelled') AS MyRevenue,
                (SELECT COUNT(*) FROM Showtimes WHERE Status = 'OpenForSale') AS OpenShowtimesCount,
                (SELECT COUNT(*) FROM Tickets WHERE Status = 'Booked') AS PendingCheckInCount;
            """;
        StaffKpiRow kpi = conn.QuerySingle<StaffKpiRow>(kpiSql, new { sid = staffId });

        const string showtimeSql = """
            SELECT m.Title AS MovieTitle,
                   r.Name AS RoomName,
                   m.AgeRating,
                   s.StartTime AS StartTimeSql,
                   s.Format,
                   MAX(0, (SELECT COUNT(*) FROM Seats se WHERE se.RoomId = s.RoomId AND se.IsActive = 1) -
                          (SELECT COUNT(*) FROM Tickets t WHERE t.ShowtimeId = s.ShowtimeId AND t.Status IN ('Booked', 'CheckedIn'))) AS SeatsLeft,
                   (SELECT COUNT(*) FROM Seats se WHERE se.RoomId = s.RoomId AND se.IsActive = 1) AS TotalSeats,
                   s.BaseTicketPrice + r.BasePriceSurcharge AS Price
            FROM Showtimes s
            INNER JOIN Movies m ON m.MovieId = s.MovieId
            INNER JOIN Rooms r ON r.RoomId = s.RoomId
            WHERE s.Status = 'OpenForSale'
            ORDER BY s.StartTime ASC
            LIMIT 12;
            """;
        List<StaffDashboardShowtimeDto> openShowtimes = conn.Query<StaffDashboardShowtimeDto>(showtimeSql).AsList();

        const string orderSql = """
            SELECT o.OrderCode,
                   COALESCE(c.FullName, 'Khách vãng lai') AS CustomerName,
                   o.TotalTickets,
                   o.PaymentMethod,
                   o.TotalAmount,
                   o.CreatedAt AS CreatedAtSql
            FROM Orders o
            LEFT JOIN Customers c ON c.CustomerId = o.CustomerId
            WHERE o.StaffUserId = @sid
            ORDER BY o.CreatedAt DESC
            LIMIT 8;
            """;
        List<StaffDashboardOrderDto> myOrders = conn.Query<StaffDashboardOrderDto>(orderSql, new { sid = staffId }).AsList();

        return new StaffDashboardSnapshot(
            kpi.MyTickets,
            kpi.MyRevenue,
            kpi.OpenShowtimesCount,
            kpi.PendingCheckInCount,
            openShowtimes,
            myOrders);
    }

    public static CustomerDashboardSnapshot LoadCustomerDashboard(int? userId, Customer? fallbackProfile)
    {
        using SqliteConnection conn = DatabaseHelper.OpenConnection();

        Customer? profile = userId.HasValue
            ? CustomerData.LoadCustomerProfile(conn, userId.Value) ?? fallbackProfile
            : fallbackProfile;

        const string movieSql = """
            SELECT m.MovieId, m.Title, m.OriginalTitle, m.Director, m.Cast,
                   m.DurationMinutes, m.Language, m.ReleaseDate AS ReleaseDateSql, m.AgeRating,
                   m.Description, m.PosterPath, m.Status,
                   COALESCE((SELECT GROUP_CONCAT(g.Name, ', ')
                             FROM MovieGenres mg INNER JOIN Genres g ON g.GenreId = mg.GenreId
                             WHERE mg.MovieId = m.MovieId), 'Tổng hợp') AS Genres,
                   (SELECT MIN(s.BaseTicketPrice + r.BasePriceSurcharge)
                    FROM Showtimes s
                    INNER JOIN Rooms r ON r.RoomId = s.RoomId
                    WHERE s.MovieId = m.MovieId AND s.Status = 'OpenForSale') AS MinOpenPrice
            FROM Movies m
            WHERE m.Status IN ('NowShowing', 'ComingSoon')
            ORDER BY CASE m.Status WHEN 'NowShowing' THEN 0 ELSE 1 END, m.ReleaseDate DESC;
            """;

        List<CustomerDashboardMovieDto> movies = conn.Query<CustomerMovieSqlRow>(movieSql)
            .Select(m => new CustomerDashboardMovieDto(
                m.MovieId,
                m.Title,
                m.OriginalTitle,
                FallbackText(m.Director, "Đang cập nhật"),
                FallbackText(m.Cast, "Đang cập nhật"),
                m.DurationMinutes,
                m.Language,
                m.ReleaseDateSql,
                m.AgeRating,
                FallbackText(m.Description, "Chưa có mô tả giới thiệu cho phim này."),
                m.PosterPath,
                m.Status,
                m.Genres,
                m.MinOpenPrice))
            .ToList();

        List<CustomerDashboardTicketDto> tickets = [];
        if (profile is not null)
        {
            const string ticketSql = """
                SELECT t.TicketCode,
                       m.Title AS MovieTitle,
                       r.Name AS RoomName,
                       se.SeatCode,
                       s.StartTime AS StartTimeSql,
                       t.UnitPrice,
                       t.Status
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
            tickets = conn.Query<CustomerDashboardTicketDto>(ticketSql, new { cid = profile.CustomerId }).AsList();
        }

        return new CustomerDashboardSnapshot(profile, movies, tickets);
    }

    private static string FallbackText(string? value, string fallback)
    {
        string trimmed = (value ?? string.Empty).Trim();
        return string.IsNullOrWhiteSpace(trimmed) ? fallback : trimmed;
    }
}
