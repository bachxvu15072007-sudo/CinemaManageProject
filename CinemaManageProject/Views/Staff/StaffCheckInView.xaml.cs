using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using CinemaManageProject.ViewModels.Staff;

namespace CinemaManageProject.Views.Staff;

public partial class StaffCheckInView : UserControl
{
    public StaffCheckInView()
    {
        InitializeComponent();
    }

    private StaffCheckInViewModel? ViewModel => DataContext as StaffCheckInViewModel;

    private void OnTicketCodeKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter)
        {
            ViewModel?.LookupTicket();
        }
    }

    private void OnLookupClick(object sender, RoutedEventArgs e) => ViewModel?.LookupTicket();
    private void OnConfirmCheckInClick(object sender, RoutedEventArgs e) => ViewModel?.ConfirmCurrentTicketCheckIn();
    private void OnClearClick(object sender, RoutedEventArgs e) => ViewModel?.ClearLookup();

    private void OnFilterBookedClick(object sender, RoutedEventArgs e) => ViewModel?.SetStatusFilter("Booked");
    private void OnFilterCheckedInClick(object sender, RoutedEventArgs e) => ViewModel?.SetStatusFilter("CheckedIn");
    private void OnFilterCancelledClick(object sender, RoutedEventArgs e) => ViewModel?.SetStatusFilter("Cancelled");
    private void OnFilterAllClick(object sender, RoutedEventArgs e) => ViewModel?.SetStatusFilter("All");
}
