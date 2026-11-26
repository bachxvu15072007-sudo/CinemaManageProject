using System.Collections.ObjectModel;
using CinemaManageProject.Data;
using CinemaManageProject.Helpers;

namespace CinemaManageProject.ViewModels.Staff;

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

    public string StaffGreeting
    {
        get => _staffGreeting;
        private set { _staffGreeting = value; OnPropertyChanged(nameof(StaffGreeting)); }
    }

    public string MyTicketsText
    {
        get => _myTicketsText;
        private set { _myTicketsText = value; OnPropertyChanged(nameof(MyTicketsText)); }
    }

    public string MyRevenueText
    {
        get => _myRevenueText;
        private set { _myRevenueText = value; OnPropertyChanged(nameof(MyRevenueText)); }
    }

    public string OpenShowtimesText
    {
        get => _openShowtimesText;
        private set { _openShowtimesText = value; OnPropertyChanged(nameof(OpenShowtimesText)); }
    }

    public string PendingCheckInText
    {
        get => _pendingCheckInText;
        private set { _pendingCheckInText = value; OnPropertyChanged(nameof(PendingCheckInText)); }
    }

    public ObservableCollection<StaffShowtimeRow> OpenShowtimes { get; } = [];
    public ObservableCollection<StaffOrderRow> MyRecentOrders { get; } = [];

    public override void LoadData()
    {
        int staffId = App.CurrentUser?.UserId ?? 0;
        StaffGreeting = $"Ca làm việc của {App.CurrentUser?.FullName ?? "Nhân viên"}";

        StaffDashboardSnapshot data = DashboardData.LoadStaffDashboard(staffId);

        MyTicketsText = $"{data.MyTickets} vé";
        MyRevenueText = FormatHelper.FormatVnd(data.MyRevenue);
        OpenShowtimesText = $"{data.OpenShowtimesCount} suất";
        PendingCheckInText = $"{data.PendingCheckInCount} vé chờ soát";

        OpenShowtimes.Clear();
        foreach (StaffDashboardShowtimeDto st in data.OpenShowtimes)
        {
            OpenShowtimes.Add(new StaffShowtimeRow(
                st.MovieTitle,
                st.RoomName,
                st.AgeRating,
                FormatHelper.FormatSqlDateTime(st.StartTimeSql),
                st.Format,
                $"{st.SeatsLeft} / {st.TotalSeats} trống",
                FormatHelper.FormatVnd(st.Price)));
        }

        MyRecentOrders.Clear();
        foreach (StaffDashboardOrderDto ord in data.MyRecentOrders)
        {
            MyRecentOrders.Add(new StaffOrderRow(
                ord.OrderCode,
                ord.CustomerName,
                ord.TotalTickets,
                FormatHelper.FormatPaymentMethod(ord.PaymentMethod),
                FormatHelper.FormatVnd(ord.TotalAmount),
                FormatHelper.FormatSqlDateTime(ord.CreatedAtSql)));
        }
    }
}
