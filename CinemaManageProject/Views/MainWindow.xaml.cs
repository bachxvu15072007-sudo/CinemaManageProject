using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
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
        App.ApplyDarkTitleBar(this);
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
            AdminOrdersViewModel => new AdminOrdersView(),
            AdminUsersViewModel => new AdminUsersView(),
            StaffDashboardViewModel => new StaffDashboardView(),
            StaffPosViewModel => new StaffPosView(),
            StaffCheckInViewModel => new StaffCheckInView(),
            StaffCustomersViewModel => new StaffCustomersView(),
            CustomerBrowseViewModel => new CustomerBrowseView(),
            CustomerMyTicketsViewModel => new CustomerMyTicketsView(),
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

    private void OnToggleSidebarClick(object sender, RoutedEventArgs e)
        => _viewModel.ToggleSidebar();

    private void OnRefreshClick(object sender, RoutedEventArgs e)
        => _viewModel.RefreshCurrentDashboard();

    private void OnLogoutClick(object sender, RoutedEventArgs e)
        => _viewModel.Logout();

    private void OnMainScrollPreviewMouseWheel(object sender, MouseWheelEventArgs e)
    {
        if (Keyboard.Modifiers == ModifierKeys.Shift)
        {
            return;
        }

        ScrollViewer? inner = FindInnerScrollViewer(e.OriginalSource as DependencyObject);
        if (inner is null)
        {
            return;
        }

        bool canScrollUp = e.Delta > 0 && inner.VerticalOffset > 0.001d;
        bool canScrollDown = e.Delta < 0 && inner.VerticalOffset + 0.001d < inner.ScrollableHeight;
        if (inner.ScrollableHeight > 0d && (canScrollUp || canScrollDown))
        {
            return;
        }

        e.Handled = true;
        double nextOffset = Math.Clamp(
            MainScrollViewer.VerticalOffset - (e.Delta * 0.45d),
            0d,
            MainScrollViewer.ScrollableHeight);
        MainScrollViewer.ScrollToVerticalOffset(nextOffset);
    }

    private ScrollViewer? FindInnerScrollViewer(DependencyObject? current)
    {
        while (current is not null && !ReferenceEquals(current, MainScrollViewer))
        {
            if (current is ScrollViewer sv)
            {
                return sv;
            }

            if (current is ListBox or DataGrid)
            {
                return FindChildScrollViewer(current);
            }

            current = current is Visual
                ? VisualTreeHelper.GetParent(current)
                : LogicalTreeHelper.GetParent(current);
        }

        return null;
    }

    private static ScrollViewer? FindChildScrollViewer(DependencyObject root)
    {
        int count = VisualTreeHelper.GetChildrenCount(root);
        for (int i = 0; i < count; i++)
        {
            DependencyObject child = VisualTreeHelper.GetChild(root, i);
            if (child is ScrollViewer sv)
            {
                return sv;
            }

            if (FindChildScrollViewer(child) is ScrollViewer nested)
            {
                return nested;
            }
        }

        return null;
    }
}
