using System.Collections.ObjectModel;
using System.Windows;
using CinemaManageProject.Models;
using CinemaManageProject.ViewModels.Admin;
using CinemaManageProject.ViewModels.Customer;
using CinemaManageProject.ViewModels.Staff;
using CommunityToolkit.Mvvm.ComponentModel;
using Wpf.Ui.Controls;

namespace CinemaManageProject.ViewModels;

public sealed partial class SidebarMenuItem : ViewModelBase
{
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ActiveTag))]
    private bool _isSelected;

    public SidebarMenuItem(string key, string label, bool isAvailable, SymbolRegular iconSymbol = SymbolRegular.Board24)
    {
        Key = key;
        Label = label;
        IsAvailable = isAvailable;
        IconSymbol = iconSymbol;
    }

    public string Key { get; }
    public string Label { get; }
    public bool IsAvailable { get; }
    public SymbolRegular IconSymbol { get; }
    public string? ActiveTag => IsSelected ? "Active" : null;
}

/// <summary>
/// ViewModel Shell của MainWindow: quản lý Sidebar theo vai trò và nạp màn hình tương ứng.
/// </summary>
public sealed partial class MainViewModel : ViewModelBase
{
    [ObservableProperty]
    private ViewModelBase? _currentDashboard;

    [ObservableProperty]
    private string _pageTitle = string.Empty;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(SidebarToggleLabel))]
    [NotifyPropertyChangedFor(nameof(SidebarToggleToolTip))]
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

    public string SidebarToggleLabel => IsSidebarOpen ? "‹" : "☰";
    public string SidebarToggleToolTip => IsSidebarOpen ? "Thu gọn thanh bên" : "Mở thanh bên";

    public ObservableCollection<SidebarMenuItem> MenuItems { get; } = [];

    public void ToggleSidebar() => IsSidebarOpen = !IsSidebarOpen;
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
            System.Windows.MessageBox.Show(
                $"Màn hình \"{item.Label}\" sẽ được viết ở bước tiếp theo.",
                "CINEVERSE CINEMA",
                System.Windows.MessageBoxButton.OK,
                System.Windows.MessageBoxImage.Information);
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

            case "movies": SwitchOrReload(() => new AdminMoviesViewModel()); break;
            case "rooms": SwitchOrReload(() => new AdminRoomsViewModel()); break;
            case "showtimes": SwitchOrReload(() => new AdminShowtimesViewModel()); break;
            case "orders": SwitchOrReload(() => new AdminOrdersViewModel()); break;
            case "users": SwitchOrReload(() => new AdminUsersViewModel()); break;
            case "pos": SwitchOrReload(() => new StaffPosViewModel()); break;
            case "checkin": SwitchOrReload(() => new StaffCheckInViewModel()); break;
            case "customers": SwitchOrReload(() => new StaffCustomersViewModel()); break;
            case "browse": SwitchOrReload(() => new CustomerBrowseViewModel()); break;
            case "mytickets": SwitchOrReload(() => new CustomerMyTicketsViewModel()); break;
            default: CurrentDashboard?.LoadData(); break;
        }
    }

    private void SwitchOrReload<T>(Func<T> factory) where T : ViewModelBase
    {
        if (CurrentDashboard is T)
        {
            CurrentDashboard.LoadData();
        }
        else
        {
            CurrentDashboard = factory();
        }
    }

    private void BuildSidebar(UserRole role)
    {
        MenuItems.Clear();

        switch (role)
        {
            case UserRole.Admin:
                PageTitle = "Dashboard Quản trị";
                MenuItems.Add(new SidebarMenuItem("dashboard", "Dashboard Quản trị", true, SymbolRegular.Board24) { IsSelected = true });
                MenuItems.Add(new SidebarMenuItem("movies", "Quản lý Phim", true, SymbolRegular.MoviesAndTv24));
                MenuItems.Add(new SidebarMenuItem("rooms", "Phòng & Sơ đồ ghế", true, SymbolRegular.Desktop24));
                MenuItems.Add(new SidebarMenuItem("showtimes", "Quản lý Lịch chiếu", true, SymbolRegular.Clock24));
                MenuItems.Add(new SidebarMenuItem("orders", "Hóa đơn & Doanh thu", true, SymbolRegular.Receipt24));
                MenuItems.Add(new SidebarMenuItem("users", "Nhân sự & Tài khoản", true, SymbolRegular.People24));
                break;

            case UserRole.Staff:
                PageTitle = "Dashboard Quầy vé";
                MenuItems.Add(new SidebarMenuItem("dashboard", "Dashboard Quầy vé", true, SymbolRegular.Board24) { IsSelected = true });
                MenuItems.Add(new SidebarMenuItem("pos", "Bán vé tại quầy", true, SymbolRegular.TicketDiagonal24));
                MenuItems.Add(new SidebarMenuItem("checkin", "Soát vé Check-in", true, SymbolRegular.CheckmarkCircle24));
                MenuItems.Add(new SidebarMenuItem("customers", "Tra cứu Khách hàng", true, SymbolRegular.People24));
                break;

            default:
                PageTitle = "Trang chủ Hội viên";
                MenuItems.Add(new SidebarMenuItem("dashboard", "Trang chủ Hội viên", true, SymbolRegular.Board24) { IsSelected = true });
                MenuItems.Add(new SidebarMenuItem("browse", "Phim & Lịch chiếu", true, SymbolRegular.Filmstrip24));
                MenuItems.Add(new SidebarMenuItem("mytickets", "Vé của tôi", true, SymbolRegular.TicketDiagonal24));
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
}
