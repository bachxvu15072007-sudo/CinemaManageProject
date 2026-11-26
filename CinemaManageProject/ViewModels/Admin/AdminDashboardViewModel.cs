using System.Collections.ObjectModel;
using CinemaManageProject.Data;
using CinemaManageProject.Helpers;

namespace CinemaManageProject.ViewModels.Admin;

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

    public string TotalRevenueText
    {
        get => _totalRevenueText;
        private set { _totalRevenueText = value; OnPropertyChanged(nameof(TotalRevenueText)); }
    }

    public string TodayRevenueText
    {
        get => _todayRevenueText;
        private set { _todayRevenueText = value; OnPropertyChanged(nameof(TodayRevenueText)); }
    }

    public string TotalTicketsText
    {
        get => _totalTicketsText;
        private set { _totalTicketsText = value; OnPropertyChanged(nameof(TotalTicketsText)); }
    }

    public string CheckedInSubText
    {
        get => _checkedInSubText;
        private set { _checkedInSubText = value; OnPropertyChanged(nameof(CheckedInSubText)); }
    }

    public string MoviesCountText
    {
        get => _moviesCountText;
        private set { _moviesCountText = value; OnPropertyChanged(nameof(MoviesCountText)); }
    }

    public string ComingSoonSubText
    {
        get => _comingSoonSubText;
        private set { _comingSoonSubText = value; OnPropertyChanged(nameof(ComingSoonSubText)); }
    }

    public string RoomsText
    {
        get => _roomsText;
        private set { _roomsText = value; OnPropertyChanged(nameof(RoomsText)); }
    }

    public string MaintenanceSeatsText
    {
        get => _maintenanceSeatsText;
        private set { _maintenanceSeatsText = value; OnPropertyChanged(nameof(MaintenanceSeatsText)); }
    }

    public ObservableCollection<AdminShowtimeRow> Showtimes { get; } = [];
    public ObservableCollection<AdminOrderRow> RecentOrders { get; } = [];

    public override void LoadData()
    {
        AdminDashboardSnapshot data = DashboardData.LoadAdminDashboard();

        TotalRevenueText = FormatHelper.FormatVnd(data.TotalRevenue);
        TodayRevenueText = $"Hôm nay: {FormatHelper.FormatVnd(data.TodayRevenue)}";
        TotalTicketsText = $"{data.TotalTickets} vé";
        CheckedInSubText = $"{data.CheckedInTickets} vé đã check-in";
        MoviesCountText = $"{data.NowShowingCount} đang chiếu";
        ComingSoonSubText = $"{data.ComingSoonCount} phim sắp chiếu";
        RoomsText = $"{data.ActiveRooms} / {data.TotalRooms}";
        MaintenanceSeatsText = $"{data.MaintenanceSeats} ghế đang bảo trì";

        Showtimes.Clear();
        foreach (AdminDashboardShowtimeDto st in data.Showtimes)
        {
            Showtimes.Add(new AdminShowtimeRow(
                st.MovieTitle,
                st.RoomName,
                FormatHelper.FormatSqlDateTime(st.StartTimeSql),
                st.Format,
                $"{st.BookedCount} / {st.ActiveSeats}",
                FormatHelper.FormatVnd(st.BasePlusSurcharge),
                FormatHelper.FormatStatusLabel(st.Status)));
        }

        RecentOrders.Clear();
        foreach (AdminDashboardOrderDto ord in data.RecentOrders)
        {
            string channel = ord.BookingChannel == "SelfService" ? "Tự đặt" : "Tại quầy";
            RecentOrders.Add(new AdminOrderRow(
                ord.OrderCode,
                ord.CustomerName,
                channel,
                FormatHelper.FormatPaymentMethod(ord.PaymentMethod),
                FormatHelper.FormatVnd(ord.TotalAmount),
                FormatHelper.FormatStatusLabel(ord.Status)));
        }
    }
}
