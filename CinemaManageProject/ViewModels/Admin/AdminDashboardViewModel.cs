using System.Collections.ObjectModel;
using CinemaManageProject.Data;
using CinemaManageProject.Helpers;
using CommunityToolkit.Mvvm.ComponentModel;

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
public sealed partial class AdminDashboardViewModel : ViewModelBase
{
    [ObservableProperty] private string _totalRevenueText = "0đ";
    [ObservableProperty] private string _todayRevenueText = "Hôm nay: 0đ";
    [ObservableProperty] private string _totalTicketsText = "0 vé";
    [ObservableProperty] private string _checkedInSubText = "0 đã check-in";
    [ObservableProperty] private string _moviesCountText = "0 phim";
    [ObservableProperty] private string _comingSoonSubText = "0 sắp chiếu";
    [ObservableProperty] private string _roomsText = "0 / 0";
    [ObservableProperty] private string _maintenanceSeatsText = "0 ghế bảo trì";

    public AdminDashboardViewModel()
    {
        LoadData();
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
