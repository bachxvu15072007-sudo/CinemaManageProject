using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using CinemaManageProject.ViewModels;
using CinemaManageProject.ViewModels.Customer;

namespace CinemaManageProject.Views.Customer;

public partial class CustomerBrowseView : UserControl
{
    private const double StripScrollStep = 340d;

    public CustomerBrowseView()
    {
        InitializeComponent();
    }

    private CustomerBrowseViewModel? ViewModel => DataContext as CustomerBrowseViewModel;

    private void OnFilterNowShowingClick(object sender, RoutedEventArgs e)
    {
        ViewModel?.SetStatusFilter("NowShowing");
        PosterStripScrollViewer.ScrollToHorizontalOffset(0);
    }

    private void OnFilterComingSoonClick(object sender, RoutedEventArgs e)
    {
        ViewModel?.SetStatusFilter("ComingSoon");
        PosterStripScrollViewer.ScrollToHorizontalOffset(0);
    }

    private void OnFilterAllClick(object sender, RoutedEventArgs e)
    {
        ViewModel?.SetStatusFilter("All");
        PosterStripScrollViewer.ScrollToHorizontalOffset(0);
    }

    private void OnSelectMovieClick(object sender, RoutedEventArgs e)
    {
        if (sender is Button { DataContext: BrowseMovieCardItem movie })
        {
            ViewModel?.SelectMovie(movie);
        }
    }

    private void OnScrollLeftClick(object sender, RoutedEventArgs e) => ScrollStripBy(-StripScrollStep);
    private void OnScrollRightClick(object sender, RoutedEventArgs e) => ScrollStripBy(StripScrollStep);
    private void OnPosterStripScrollChanged(object sender, ScrollChangedEventArgs e) => UpdateScrollButtons();

    private void OnPosterStripPreviewMouseWheel(object sender, MouseWheelEventArgs e)
    {
        if (Keyboard.Modifiers == ModifierKeys.Shift)
        {
            ScrollStripBy(-e.Delta);
            e.Handled = true;
            return;
        }

        if (VisualTreeHelper.GetParent(PosterStripScrollViewer) is UIElement parent)
        {
            e.Handled = true;
            var forwarded = new MouseWheelEventArgs(e.MouseDevice, e.Timestamp, e.Delta)
            {
                RoutedEvent = UIElement.MouseWheelEvent,
                Source = sender
            };
            parent.RaiseEvent(forwarded);
        }
    }

    private void ScrollStripBy(double delta)
    {
        double target = Math.Clamp(
            PosterStripScrollViewer.HorizontalOffset + delta,
            0d,
            PosterStripScrollViewer.ScrollableWidth);
        PosterStripScrollViewer.ScrollToHorizontalOffset(target);
    }

    private void UpdateScrollButtons()
    {
        ScrollLeftButton.IsEnabled = PosterStripScrollViewer.HorizontalOffset > 0.5d;
        ScrollRightButton.IsEnabled = PosterStripScrollViewer.HorizontalOffset + 0.5d < PosterStripScrollViewer.ScrollableWidth;
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
