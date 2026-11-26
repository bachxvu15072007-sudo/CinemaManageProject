using System.Windows;
using System.Windows.Controls;
using CinemaManageProject.ViewModels;
using CinemaManageProject.ViewModels.Customer;

namespace CinemaManageProject.Views.Customer;

public partial class CustomerBrowseView : UserControl
{
    public CustomerBrowseView()
    {
        InitializeComponent();
    }

    private CustomerBrowseViewModel? ViewModel => DataContext as CustomerBrowseViewModel;

    private void OnFilterNowShowingClick(object sender, RoutedEventArgs e) => ViewModel?.SetStatusFilter("NowShowing");
    private void OnFilterComingSoonClick(object sender, RoutedEventArgs e) => ViewModel?.SetStatusFilter("ComingSoon");
    private void OnFilterAllClick(object sender, RoutedEventArgs e) => ViewModel?.SetStatusFilter("All");

    private void OnSelectMovieClick(object sender, RoutedEventArgs e)
    {
        if (sender is Button { DataContext: BrowseMovieCardItem movie })
        {
            ViewModel?.SelectMovie(movie);
        }
    }

    private void OnSeatClick(object sender, RoutedEventArgs e)
    {
        if (sender is Button { DataContext: BookingSeatCellItem seat })
        {
            ViewModel?.ToggleSeat(seat);
        }
    }

    private void OnClearSeatsClick(object sender, RoutedEventArgs e) => ViewModel?.ClearSelectedSeats();
    private void OnApplyMaxPointsClick(object sender, RoutedEventArgs e) => ViewModel?.ApplyMaxPoints();
    private void OnConfirmBookingClick(object sender, RoutedEventArgs e) => ViewModel?.ConfirmBooking();
}
