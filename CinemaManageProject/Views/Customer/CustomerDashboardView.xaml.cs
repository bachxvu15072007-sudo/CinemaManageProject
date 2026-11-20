using System.Windows;
using System.Windows.Controls;
using CinemaManageProject.ViewModels.Customer;

namespace CinemaManageProject.Views.Customer;

public partial class CustomerDashboardView : UserControl
{
    public CustomerDashboardView()
    {
        InitializeComponent();
    }

    private void OnFilterClick(object sender, RoutedEventArgs e)
    {
        if (sender is Button btn && DataContext is CustomerDashboardViewModel vm)
        {
            vm.SetFilter(btn.Tag?.ToString());
        }
    }
}
