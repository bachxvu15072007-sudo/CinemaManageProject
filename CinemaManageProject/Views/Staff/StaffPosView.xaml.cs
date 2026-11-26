using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using CinemaManageProject.ViewModels;
using CinemaManageProject.ViewModels.Staff;

namespace CinemaManageProject.Views.Staff;

public partial class StaffPosView : UserControl
{
    public StaffPosView()
    {
        InitializeComponent();
    }

    private StaffPosViewModel? ViewModel => DataContext as StaffPosViewModel;

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
