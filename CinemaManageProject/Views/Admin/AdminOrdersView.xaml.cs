using System.Windows;
using System.Windows.Controls;
using CinemaManageProject.ViewModels.Admin;

namespace CinemaManageProject.Views.Admin;

/// <summary>
/// Màn hình tra cứu Hóa đơn &amp; Doanh thu dành cho Admin.
/// </summary>
public partial class AdminOrdersView : UserControl
{
    public AdminOrdersView()
    {
        InitializeComponent();
    }

    private void OnResetFiltersClick(object sender, RoutedEventArgs e)
    {
        if (DataContext is AdminOrdersViewModel viewModel)
        {
            viewModel.ResetFilters();
        }
    }
}
