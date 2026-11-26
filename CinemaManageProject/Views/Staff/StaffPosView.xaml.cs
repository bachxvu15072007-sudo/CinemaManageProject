using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using CinemaManageProject.ViewModels;
using CinemaManageProject.ViewModels.Staff;

namespace CinemaManageProject.Views.Staff;

public partial class StaffPosView : UserControl
{
    private const double StripScrollStep = 340d;

    public StaffPosView()
    {
        InitializeComponent();
    }

    private StaffPosViewModel? ViewModel => DataContext as StaffPosViewModel;

    private void OnSelectMovieClick(object sender, RoutedEventArgs e)
    {
        if (sender is Button { DataContext: PosMoviePosterItem movie })
        {
            ViewModel?.SelectMovie(movie);
        }
    }

    private void OnShowAllMoviesClick(object sender, RoutedEventArgs e)
        => ViewModel?.ShowAllMovies();

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

    private void OnClearSeatsClick(object sender, RoutedEventArgs e)
        => ViewModel?.ClearSelectedSeats();

    private void OnLookupCustomerClick(object sender, RoutedEventArgs e)
        => ViewModel?.LookupCustomer();

    private void OnClearCustomerClick(object sender, RoutedEventArgs e)
        => ViewModel?.ClearCustomer();

    private void OnApplyMaxPointsClick(object sender, RoutedEventArgs e)
        => ViewModel?.ApplyMaxPoints();

    private void OnExactCashClick(object sender, RoutedEventArgs e)
        => ViewModel?.SetExactCash();

    private void OnQuickCashClick(object sender, RoutedEventArgs e)
    {
        if (sender is Button { Tag: string raw } &&
            decimal.TryParse(raw, NumberStyles.Number, CultureInfo.InvariantCulture, out decimal amount))
        {
            ViewModel?.SetQuickCash(amount);
        }
    }

    private void OnCompleteSaleClick(object sender, RoutedEventArgs e)
        => ViewModel?.CompleteSale();
}
