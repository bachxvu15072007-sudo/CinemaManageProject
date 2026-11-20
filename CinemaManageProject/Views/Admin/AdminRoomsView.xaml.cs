using System.Windows;
using System.Windows.Controls;
using CinemaManageProject.ViewModels.Admin;

namespace CinemaManageProject.Views.Admin;

/// <summary>
/// Màn hình Quản lý Phòng chiếu &amp; Sơ đồ Ghế cho vai trò Admin.
/// </summary>
public partial class AdminRoomsView : UserControl
{
    public AdminRoomsView()
    {
        InitializeComponent();
    }

    private AdminRoomsViewModel? ViewModel => DataContext as AdminRoomsViewModel;

    private void OnSeatClick(object sender, RoutedEventArgs e)
    {
        if (sender is Button { DataContext: SeatCellItem seat })
        {
            ViewModel?.SelectSeat(seat);
        }
    }

    private void OnToggleSeatAvailableClick(object sender, RoutedEventArgs e)
        => ViewModel?.ToggleSelectedSeatAvailable();
}
