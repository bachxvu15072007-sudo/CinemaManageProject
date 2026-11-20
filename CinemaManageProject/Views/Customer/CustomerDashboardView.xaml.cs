using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using CinemaManageProject.ViewModels.Customer;

namespace CinemaManageProject.Views.Customer;

public partial class CustomerDashboardView : UserControl
{
    private const double StripScrollStep = 360d;

    public CustomerDashboardView()
    {
        InitializeComponent();
    }

    private void OnFilterClick(object sender, RoutedEventArgs e)
    {
        if (sender is not Button btn || DataContext is not CustomerDashboardViewModel vm)
        {
            return;
        }

        string filter = btn.Name switch
        {
            nameof(FilterNowShowingButton) => "NowShowing",
            nameof(FilterComingSoonButton) => "ComingSoon",
            _ => "All"
        };

        vm.SetFilter(filter);
        PosterStripScrollViewer.ScrollToHorizontalOffset(0);
    }

    private void OnMovieCardClick(object sender, RoutedEventArgs e)
    {
        if (sender is FrameworkElement { DataContext: CustomerMovieRow movie }
            && DataContext is CustomerDashboardViewModel vm)
        {
            vm.SelectMovie(movie);
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

        // Chuyển tiếp cuộn dọc lên ScrollViewer cha của MainWindow thay vì bị nuốt bởi strip ngang.
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
}
