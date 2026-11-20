using System.Windows;
using System.Windows.Controls;
using CinemaManageProject.ViewModels;
using CinemaManageProject.ViewModels.Admin;
using CinemaManageProject.ViewModels.Customer;
using CinemaManageProject.ViewModels.Staff;
using CinemaManageProject.Views.Admin;
using CinemaManageProject.Views.Customer;
using CinemaManageProject.Views.Staff;

namespace CinemaManageProject.Views;

/// <summary>
/// Cửa sổ khung chính (Shell) hiển thị Sidebar phân quyền và Dashboard theo từng vai trò.
/// </summary>
public partial class MainWindow : Window
{
    private readonly MainViewModel _viewModel;

    public MainWindow()
    {
        InitializeComponent();
        _viewModel = new MainViewModel();
        _viewModel.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == nameof(MainViewModel.CurrentDashboard))
            {
                ShowCurrentDashboard();
            }
        };
        DataContext = _viewModel;
        ShowCurrentDashboard();
    }

    private void ShowCurrentDashboard()
    {
        UserControl view = _viewModel.CurrentDashboard switch
        {
            AdminDashboardViewModel => new AdminDashboardView(),
            AdminMoviesViewModel => new AdminMoviesView(),
            AdminRoomsViewModel => new AdminRoomsView(),
            AdminShowtimesViewModel => new AdminShowtimesView(),
            StaffDashboardViewModel => new StaffDashboardView(),
            _ => new CustomerDashboardView()
        };
        view.DataContext = _viewModel.CurrentDashboard;
        DashboardHost.Content = view;
    }

    private void OnSelectMenuClick(object sender, RoutedEventArgs e)
    {
        if (sender is Button { DataContext: SidebarMenuItem item })
        {
            _viewModel.SelectMenu(item);
        }
    }

    private void OnRefreshClick(object sender, RoutedEventArgs e)
        => _viewModel.RefreshCurrentDashboard();

    private void OnLogoutClick(object sender, RoutedEventArgs e)
        => _viewModel.Logout();
}
