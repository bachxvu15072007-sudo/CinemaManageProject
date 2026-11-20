using System.Windows;
using System.Windows.Controls;
using CinemaManageProject.ViewModels.Admin;

namespace CinemaManageProject.Views.Admin;

/// <summary>
/// Màn hình Quản lý Lịch chiếu cho vai trò Admin.
/// </summary>
public partial class AdminShowtimesView : UserControl
{
    public AdminShowtimesView()
    {
        InitializeComponent();
    }

    private AdminShowtimesViewModel? ViewModel => DataContext as AdminShowtimesViewModel;

    private void OnNewShowtimeClick(object sender, RoutedEventArgs e)
        => ViewModel?.StartNewShowtime();

    private void OnResetFiltersClick(object sender, RoutedEventArgs e)
        => ViewModel?.ResetFilters();

    private void OnSaveShowtimeClick(object sender, RoutedEventArgs e)
        => ViewModel?.SaveShowtime();

    private void OnResetFormClick(object sender, RoutedEventArgs e)
        => ViewModel?.ResetForm();

    private void OnDeleteShowtimeClick(object sender, RoutedEventArgs e)
    {
        if (ViewModel is null || !ViewModel.CanDeleteSelectedShowtime(out string summary))
        {
            return;
        }

        MessageBoxResult confirm = MessageBox.Show(
            $"Bạn có chắc chắn muốn xóa suất chiếu \"{summary}\"?\nHành động này không thể hoàn tác.",
            "Xác nhận xóa suất chiếu",
            MessageBoxButton.YesNo,
            MessageBoxImage.Warning);

        if (confirm == MessageBoxResult.Yes)
        {
            ViewModel.DeleteSelectedShowtime();
        }
    }
}
