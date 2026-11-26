using System.Windows;
using System.Windows.Controls;
using CinemaManageProject.ViewModels.Customer;

namespace CinemaManageProject.Views.Customer;

public partial class CustomerMyTicketsView : UserControl
{
    public CustomerMyTicketsView()
    {
        InitializeComponent();
    }

    private CustomerMyTicketsViewModel? ViewModel => DataContext as CustomerMyTicketsViewModel;

    private void OnFilterAllClick(object sender, RoutedEventArgs e) => ViewModel?.SetStatusFilter("All");
    private void OnFilterBookedClick(object sender, RoutedEventArgs e) => ViewModel?.SetStatusFilter("Booked");
    private void OnFilterCheckedInClick(object sender, RoutedEventArgs e) => ViewModel?.SetStatusFilter("CheckedIn");
    private void OnFilterCancelledClick(object sender, RoutedEventArgs e) => ViewModel?.SetStatusFilter("Cancelled");

    private void OnCancelTicketClick(object sender, RoutedEventArgs e)
    {
        if (ViewModel?.SelectedTicket is null)
        {
            return;
        }

        MessageBoxResult confirm = MessageBox.Show(
            $"Bạn có chắc chắn muốn hủy vé {ViewModel.SelectedTicket.TicketCode} ({ViewModel.SelectedTicket.MovieTitle} - {ViewModel.SelectedTicket.RoomAndSeat}) không?",
            "Xác nhận hủy vé",
            MessageBoxButton.YesNo,
            MessageBoxImage.Warning);

        if (confirm == MessageBoxResult.Yes)
        {
            ViewModel.CancelSelectedTicket();
        }
    }
}
