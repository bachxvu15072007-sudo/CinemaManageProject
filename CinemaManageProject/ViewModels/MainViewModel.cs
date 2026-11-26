using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Media;
using CinemaManageProject.Models;
using CinemaManageProject.ViewModels.Admin;
using CinemaManageProject.ViewModels.Customer;
using CinemaManageProject.ViewModels.Staff;

namespace CinemaManageProject.ViewModels;

public sealed class SidebarMenuItem : ViewModelBase
{
    private bool _isSelected;

    public SidebarMenuItem(string key, string label, bool isAvailable, string iconResourceKey = "DashboardIconGeometry")
    {
        Key = key;
        Label = label;
        IsAvailable = isAvailable;
        IconResourceKey = iconResourceKey;
    }

    public string Key { get; }
    public string Label { get; }
    public bool IsAvailable { get; }
    public string IconResourceKey { get; }
    public Geometry? IconGeometry => Application.Current?.TryFindResource(IconResourceKey) as Geometry;

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
    private bool _isSidebarOpen = true;

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

    public bool IsSidebarOpen
    {
        get => _isSidebarOpen;
        private set
        {
            if (_isSidebarOpen == value)
            {
                return;
            }

            _isSidebarOpen = value;
            OnPropertyChanged(nameof(IsSidebarOpen));
            OnPropertyChanged(nameof(SidebarToggleLabel));
            OnPropertyChanged(nameof(SidebarToggleToolTip));
        }
    }

    public string SidebarToggleLabel => IsSidebarOpen ? "‹" : "☰";
    public string SidebarToggleToolTip => IsSidebarOpen ? "Thu gọn thanh bên" : "Mở thanh bên";

    public void ToggleSidebar() => IsSidebarOpen = !IsSidebarOpen;

    public ObservableCollection<SidebarMenuItem> MenuItems { get; } = [];

    private void BuildSidebar(UserRole role)
    {
        MenuItems.Clear();

        switch (role)
        {
            case UserRole.Admin:
                PageTitle = "Dashboard Quản trị";
                MenuItems.Add(new SidebarMenuItem("dashboard", "Dashboard Quản trị", true, "DashboardIconGeometry") { IsSelected = true });
                MenuItems.Add(new SidebarMenuItem("movies", "Quản lý Phim", true, "FilmClapperIconGeometry"));
                MenuItems.Add(new SidebarMenuItem("rooms", "Phòng & Sơ đồ ghế", true, "TheaterScreenIconGeometry"));
                MenuItems.Add(new SidebarMenuItem("showtimes", "Quản lý Lịch chiếu", true, "ShowtimeClockIconGeometry"));
                MenuItems.Add(new SidebarMenuItem("orders", "Hóa đơn & Doanh thu", true, "ReceiptIconGeometry"));
                MenuItems.Add(new SidebarMenuItem("users", "Nhân sự & Tài khoản", true, "UsersGroupIconGeometry"));
                break;

            case UserRole.Staff:
                PageTitle = "Dashboard Quầy vé";
                MenuItems.Add(new SidebarMenuItem("dashboard", "Dashboard Quầy vé", true, "DashboardIconGeometry") { IsSelected = true });
                MenuItems.Add(new SidebarMenuItem("pos", "Bán vé tại quầy", true, "CinemaTicketIconGeometry"));
                MenuItems.Add(new SidebarMenuItem("checkin", "Soát vé Check-in", true, "CheckTicketIconGeometry"));
                MenuItems.Add(new SidebarMenuItem("customers", "Tra cứu Khách hàng", true, "UsersGroupIconGeometry"));
                break;

            default:
                PageTitle = "Trang chủ Hội viên";
                MenuItems.Add(new SidebarMenuItem("dashboard", "Trang chủ Hội viên", true, "DashboardIconGeometry") { IsSelected = true });
                MenuItems.Add(new SidebarMenuItem("browse", "Phim & Lịch chiếu", true, "FilmStripIconGeometry"));
                MenuItems.Add(new SidebarMenuItem("mytickets", "Vé của tôi", true, "CinemaTicketIconGeometry"));
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

            case "orders":
                if (CurrentDashboard is AdminOrdersViewModel)
                {
                    CurrentDashboard.LoadData();
                }
                else
                {
                    CurrentDashboard = new AdminOrdersViewModel();
                }
                break;

            case "users":
                if (CurrentDashboard is AdminUsersViewModel)
                {
                    CurrentDashboard.LoadData();
                }
                else
                {
                    CurrentDashboard = new AdminUsersViewModel();
                }
                break;

            case "pos":
                if (CurrentDashboard is StaffPosViewModel)
                {
                    CurrentDashboard.LoadData();
                }
                else
                {
                    CurrentDashboard = new StaffPosViewModel();
                }
                break;

            case "checkin":
                if (CurrentDashboard is StaffCheckInViewModel)
                {
                    CurrentDashboard.LoadData();
                }
                else
                {
                    CurrentDashboard = new StaffCheckInViewModel();
                }
                break;

            case "customers":
                if (CurrentDashboard is StaffCustomersViewModel)
                {
                    CurrentDashboard.LoadData();
                }
                else
                {
                    CurrentDashboard = new StaffCustomersViewModel();
                }
                break;

            case "browse":
                if (CurrentDashboard is CustomerBrowseViewModel)
                {
                    CurrentDashboard.LoadData();
                }
                else
                {
                    CurrentDashboard = new CustomerBrowseViewModel();
                }
                break;

            case "mytickets":
                if (CurrentDashboard is CustomerMyTicketsViewModel)
                {
                    CurrentDashboard.LoadData();
                }
                else
                {
                    CurrentDashboard = new CustomerMyTicketsViewModel();
                }
                break;

            default:
                CurrentDashboard?.LoadData();
                break;
        }
    }
}
