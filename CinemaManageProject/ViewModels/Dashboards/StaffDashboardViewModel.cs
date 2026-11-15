using System.Collections.ObjectModel;
using Microsoft.Data.Sqlite;

namespace CinemaManageProject.ViewModels.Dashboards;

public sealed record StaffShowtimeRow(
    string MovieTitle,
    string RoomName,
    string AgeRating,
    string StartTime,
    string Format,
    string SeatsLeft,
    string Price);

public sealed record StaffOrderRow(
    string OrderCode,
    string CustomerName,
    int TotalTickets,
    string Payment,
    string Amount,
    string CreatedAt);

/// <summary>
/// ViewModel cho Dashboard vai trò Staff: doanh số quầy cá nhân, suất chiếu mở bán và vé chờ soát.
/// </summary>
public sealed class StaffDashboardViewModel : ViewModelBase
{
    private string _staffGreeting = string.Empty;
    private string _myTicketsText = "0 vé";
    private string _myRevenueText = "0đ";
    private string _openShowtimesText = "0 suất";
    private string _pendingCheckInText = "0 vé";

    public StaffDashboardViewModel()
    {
        LoadData();
    }

    public string StaffGreeting { get => _staffGreeting; private set => SetProperty(ref _staffGreeting, value); }
    public string MyTicketsText { get => _myTicketsText; private set => SetProperty(ref _myTicketsText, value); }
    public string MyRevenueText { get => _myRevenueText; private set => SetProperty(ref _myRevenueText, value); }
    public string OpenShowtimesText { get => _openShowtimesText; private set => SetProperty(ref _openShowtimesText, value); }
    public string PendingCheckInText { get => _pendingCheckInText; private set => SetProperty(ref _pendingCheckInText, value); }

    public ObservableCollection<StaffShowtimeRow> OpenShowtimes { get; } = [];
    public ObservableCollection<StaffOrderRow> MyRecentOrders { get; } = [];

    public override void LoadData()
    {
        int staffId = App.CurrentUser?.UserId ?? 0;
        StaffGreeting = $"Ca làm việc của {App.CurrentUser?.FullName ?? "Nhân viên"}";

        using SqliteConnection conn = App.OpenDbConnection();

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
                MyTicketsText = $"{r.GetInt32(0)} vé";
                MyRevenueText = FormatVnd(ReadDecimal(r, 1));
                OpenShowtimesText = $"{r.GetInt32(2)} suất";
                PendingCheckInText = $"{r.GetInt32(3)} vé chờ soát";
            }
        }

        OpenShowtimes.Clear();
        using (SqliteCommand st = conn.CreateCommand())
        {
            st.CommandText = """
                SELECT m.Title, r.Name, m.AgeRating, s.StartTime, s.Format,
                       (SELECT COUNT(*) FROM Seats se WHERE se.RoomId = s.RoomId AND se.IsActive = 1) -
                       (SELECT COUNT(*) FROM Tickets t WHERE t.ShowtimeId = s.ShowtimeId AND t.Status IN ('Booked', 'CheckedIn')),
                       (SELECT COUNT(*) FROM Seats se WHERE se.RoomId = s.RoomId AND se.IsActive = 1),
                       s.BaseTicketPrice
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
                int avail = Math.Max(0, r.GetInt32(5));
                OpenShowtimes.Add(new StaffShowtimeRow(
                    r.GetString(0),
                    r.GetString(1),
                    r.GetString(2),
                    FormatSqlDateTime(r.GetString(3)),
                    r.GetString(4),
                    $"{avail} / {r.GetInt32(6)} trống",
                    FormatVnd(ReadDecimal(r, 7))));
            }
        }

        MyRecentOrders.Clear();
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
                MyRecentOrders.Add(new StaffOrderRow(
                    r.GetString(0),
                    r.GetString(1),
                    r.GetInt32(2),
                    FormatPaymentMethod(r.GetString(3)),
                    FormatVnd(ReadDecimal(r, 4)),
                    FormatSqlDateTime(r.GetString(5))));
            }
        }
    }
}
