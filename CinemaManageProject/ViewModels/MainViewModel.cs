using System.Collections.ObjectModel;
using System.Windows;
using CinemaManageProject.Models;
using CinemaManageProject.ViewModels.Admin;
using CinemaManageProject.ViewModels.Customer;
using CinemaManageProject.ViewModels.Staff;

namespace CinemaManageProject.ViewModels;

public sealed class SidebarMenuItem : ViewModelBase
{
    private bool _isSelected;

    public SidebarMenuItem(string key, string label, bool isAvailable)
    {
        Key = key;
        Label = label;
        IsAvailable = isAvailable;
    }

    public string Key { get; }
    public string Label { get; }
    public bool IsAvailable { get; }

    public string? ActiveTag => IsSelected ? "Active" : null;

    public bool IsSelected
    {
        get => _isSelected;
        set
        {
            _isSelected = value;
            OnPropertyChanged(nameof(IsSelected));
            OnPropertyChanged(nameof(ActiveTag));
        }
    }
}

/// <summary>
/// ViewModel Shell của MainWindow: quản lý Sidebar theo vai trò và nạp Dashboard tương ứng.
/// </summary>
public sealed class MainViewModel : ViewModelBase
{
    private ViewModelBase? _currentDashboard;
    private string _pageTitle = string.Empty;

    public MainViewModel()
    {
        User user = App.CurrentUser
            ?? throw new InvalidOperationException("Chưa đăng nhập.");

        UserFullName = user.FullName;
        UsernameDisplay = $"@{user.Username}";
        RoleDisplay = user.Role switch
        {
            UserRole.Admin => "ADMIN",
            UserRole.Staff => "STAFF",
            _ => "CUSTOMER"
        };

        BuildSidebar(user.Role);
        LoadRoleDashboard(user.Role);
    }

    public string UserFullName { get; }
    public string UsernameDisplay { get; }
    public string RoleDisplay { get; }

    public string PageTitle
    {
        get => _pageTitle;
        private set { _pageTitle = value; OnPropertyChanged(nameof(PageTitle)); }
    }

    public ViewModelBase? CurrentDashboard
    {
        get => _currentDashboard;
        private set { _currentDashboard = value; OnPropertyChanged(nameof(CurrentDashboard)); }
    }

    public ObservableCollection<SidebarMenuItem> MenuItems { get; } = [];

    private void BuildSidebar(UserRole role)
    {
        MenuItems.Clear();

        switch (role)
        {
            case UserRole.Admin:
                PageTitle = "Dashboard Quản trị";
                MenuItems.Add(new SidebarMenuItem("dashboard", "Dashboard Quản trị", true) { IsSelected = true });
                MenuItems.Add(new SidebarMenuItem("movies", "Quản lý Phim", true));
                MenuItems.Add(new SidebarMenuItem("rooms", "Phòng & Sơ đồ ghế", true));
                MenuItems.Add(new SidebarMenuItem("showtimes", "Quản lý Lịch chiếu", true));
                MenuItems.Add(new SidebarMenuItem("orders", "Hóa đơn & Doanh thu", false));
                MenuItems.Add(new SidebarMenuItem("users", "Nhân sự & Tài khoản", false));
                break;

            case UserRole.Staff:
                PageTitle = "Dashboard Quầy vé";
                MenuItems.Add(new SidebarMenuItem("dashboard", "Dashboard Quầy vé", true) { IsSelected = true });
                MenuItems.Add(new SidebarMenuItem("pos", "Bán vé tại quầy", false));
                MenuItems.Add(new SidebarMenuItem("checkin", "Soát vé Check-in", false));
                MenuItems.Add(new SidebarMenuItem("customers", "Tra cứu Khách hàng", false));
                break;

            default:
                PageTitle = "Trang chủ Hội viên";
                MenuItems.Add(new SidebarMenuItem("dashboard", "Trang chủ Hội viên", true) { IsSelected = true });
                MenuItems.Add(new SidebarMenuItem("browse", "Phim & Lịch chiếu", false));
                MenuItems.Add(new SidebarMenuItem("mytickets", "Vé của tôi", false));
                break;
        }
    }

    private void LoadRoleDashboard(UserRole role)
    {
        CurrentDashboard = role switch
        {
            UserRole.Admin => new AdminDashboardViewModel(),
            UserRole.Staff => new StaffDashboardViewModel(),
            _ => new CustomerDashboardViewModel()
        };
    }

    public void RefreshCurrentDashboard() => CurrentDashboard?.LoadData();

    public void Logout() => ((App)Application.Current).Logout();

    public void SelectMenu(SidebarMenuItem? item)
    {
        if (item is null)
        {
            return;
        }

        if (!item.IsAvailable)
        {
            MessageBox.Show(
                $"Màn hình \"{item.Label}\" sẽ được viết ở bước tiếp theo.",
                "CINEVERSE CINEMA",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
            return;
        }

        foreach (SidebarMenuItem m in MenuItems)
        {
            m.IsSelected = ReferenceEquals(m, item);
        }

        PageTitle = item.Label;
        switch (item.Key)
        {
            case "dashboard":
                if (CurrentDashboard is AdminDashboardViewModel or StaffDashboardViewModel or CustomerDashboardViewModel)
                {
                    CurrentDashboard.LoadData();
                }
                else
                {
                    LoadRoleDashboard(App.CurrentUser?.Role ?? UserRole.Customer);
                }
                break;

            case "movies":
                if (CurrentDashboard is AdminMoviesViewModel)
                {
                    CurrentDashboard.LoadData();
                }
                else
                {
                    CurrentDashboard = new AdminMoviesViewModel();
                }
                break;

            case "rooms":
                if (CurrentDashboard is AdminRoomsViewModel)
                {
                    CurrentDashboard.LoadData();
                }
                else
                {
                    CurrentDashboard = new AdminRoomsViewModel();
                }
                break;

            case "showtimes":
                if (CurrentDashboard is AdminShowtimesViewModel)
                {
                    CurrentDashboard.LoadData();
                }
                else
                {
                    CurrentDashboard = new AdminShowtimesViewModel();
                }
                break;

            default:
                CurrentDashboard?.LoadData();
                break;
        }
    }
}
