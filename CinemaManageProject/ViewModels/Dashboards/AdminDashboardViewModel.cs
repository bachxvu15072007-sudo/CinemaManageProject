using System.Collections.ObjectModel;
using System.Globalization;
using Microsoft.Data.Sqlite;

namespace CinemaManageProject.ViewModels.Dashboards;

public sealed record AdminShowtimeRow(
    string MovieTitle,
    string RoomName,
    string StartTime,
    string Format,
    string Occupancy,
    string Price,
    string StatusDisplay);

public sealed record AdminOrderRow(
    string OrderCode,
    string CustomerName,
    string Channel,
    string Payment,
    string TotalAmount,
    string StatusDisplay);

/// <summary>
/// ViewModel cho Dashboard vai trò Admin: KPI toàn rạp, suất chiếu và hóa đơn gần đây.
/// </summary>
public sealed class AdminDashboardViewModel : ViewModelBase
{
    private string _totalRevenueText = "0đ";
    private string _todayRevenueText = "Hôm nay: 0đ";
    private string _totalTicketsText = "0 vé";
    private string _checkedInSubText = "0 đã check-in";
    private string _moviesCountText = "0 phim";
    private string _comingSoonSubText = "0 sắp chiếu";
    private string _roomsText = "0 / 0";
    private string _maintenanceSeatsText = "0 ghế bảo trì";

    public AdminDashboardViewModel()
    {
        LoadData();
    }

    public string TotalRevenueText { get => _totalRevenueText; private set => SetProperty(ref _totalRevenueText, value); }
    public string TodayRevenueText { get => _todayRevenueText; private set => SetProperty(ref _todayRevenueText, value); }
    public string TotalTicketsText { get => _totalTicketsText; private set => SetProperty(ref _totalTicketsText, value); }
    public string CheckedInSubText { get => _checkedInSubText; private set => SetProperty(ref _checkedInSubText, value); }
    public string MoviesCountText { get => _moviesCountText; private set => SetProperty(ref _moviesCountText, value); }
    public string ComingSoonSubText { get => _comingSoonSubText; private set => SetProperty(ref _comingSoonSubText, value); }
    public string RoomsText { get => _roomsText; private set => SetProperty(ref _roomsText, value); }
    public string MaintenanceSeatsText { get => _maintenanceSeatsText; private set => SetProperty(ref _maintenanceSeatsText, value); }

    public ObservableCollection<AdminShowtimeRow> Showtimes { get; } = [];
    public ObservableCollection<AdminOrderRow> RecentOrders { get; } = [];

    public void LoadData()
    {
        using SqliteConnection conn = App.OpenDbConnection();

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
                TotalRevenueText = FormatVnd(Convert.ToDecimal(r.GetValue(0), CultureInfo.InvariantCulture));
                TodayRevenueText = $"Hôm nay: {FormatVnd(Convert.ToDecimal(r.GetValue(1), CultureInfo.InvariantCulture))}";
                TotalTicketsText = $"{r.GetInt32(2)} vé";
                CheckedInSubText = $"{r.GetInt32(3)} vé đã check-in";
                MoviesCountText = $"{r.GetInt32(4)} đang chiếu";
                ComingSoonSubText = $"{r.GetInt32(5)} phim sắp chiếu";
                RoomsText = $"{r.GetInt32(6)} / {r.GetInt32(7)}";
                MaintenanceSeatsText = $"{r.GetInt32(8)} ghế đang bảo trì";
            }
        }

        Showtimes.Clear();
        using (SqliteCommand st = conn.CreateCommand())
        {
            st.CommandText = """
                SELECT m.Title, r.Name, s.StartTime, s.Format,
                       (SELECT COUNT(*) FROM Tickets t WHERE t.ShowtimeId = s.ShowtimeId AND t.Status IN ('Booked', 'CheckedIn')),
                       (SELECT COUNT(*) FROM Seats se WHERE se.RoomId = s.RoomId AND se.IsActive = 1),
                       s.BaseTicketPrice, s.Status
                FROM Showtimes s
                INNER JOIN Movies m ON m.MovieId = s.MovieId
                INNER JOIN Rooms r ON r.RoomId = s.RoomId
                ORDER BY s.StartTime ASC
                LIMIT 12;
                """;
            using SqliteDataReader r = st.ExecuteReader();
            while (r.Read())
            {
                string timeText = DateTime.TryParse(r.GetString(2), CultureInfo.InvariantCulture, DateTimeStyles.AssumeLocal, out DateTime dt)
                    ? dt.ToString("dd/MM HH:mm")
                    : r.GetString(2);
                decimal price = Convert.ToDecimal(r.GetValue(6), CultureInfo.InvariantCulture);
                string statusDisplay = r.GetString(7) switch
                {
                    "OpenForSale" => "Đang mở bán",
                    "Scheduled" => "Đã lên lịch",
                    "Completed" => "Đã chiếu",
                    "Cancelled" => "Đã hủy",
                    var s => s
                };

                Showtimes.Add(new AdminShowtimeRow(
                    r.GetString(0),
                    r.GetString(1),
                    timeText,
                    r.GetString(3),
                    $"{r.GetInt32(4)} / {r.GetInt32(5)}",
                    FormatVnd(price),
                    statusDisplay));
            }
        }

        RecentOrders.Clear();
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
                string channel = r.GetString(2) == "SelfService" ? "Tự đặt" : "Tại quầy";
                string payment = r.GetString(3) switch
                {
                    "Cash" => "Tiền mặt",
                    "BankTransfer" => "Chuyển khoản",
                    "Card" => "Thẻ",
                    "EWallet" => "Ví điện tử",
                    var p => p
                };
                string status = r.GetString(5) switch
                {
                    "Paid" => "Đã thanh toán",
                    "Cancelled" => "Đã hủy",
                    "PartiallyRefunded" => "Hoàn một phần",
                    var s => s
                };

                RecentOrders.Add(new AdminOrderRow(
                    r.GetString(0),
                    r.GetString(1),
                    channel,
                    payment,
                    FormatVnd(Convert.ToDecimal(r.GetValue(4), CultureInfo.InvariantCulture)),
                    status));
            }
        }
    }
}
