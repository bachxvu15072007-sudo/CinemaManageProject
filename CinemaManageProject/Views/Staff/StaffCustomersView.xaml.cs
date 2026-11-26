using System.Windows;
using System.Windows.Controls;
using CinemaManageProject.ViewModels.Staff;

namespace CinemaManageProject.Views.Staff;

public partial class StaffCustomersView : UserControl
{
    public StaffCustomersView()
    {
        InitializeComponent();
    }

    private StaffCustomersViewModel? ViewModel => DataContext as StaffCustomersViewModel;

    private void OnFilterAllClick(object sender, RoutedEventArgs e) => ViewModel?.SetTierFilter("All");
    private void OnFilterMemberClick(object sender, RoutedEventArgs e) => ViewModel?.SetTierFilter("Member");
    private void OnFilterSilverClick(object sender, RoutedEventArgs e) => ViewModel?.SetTierFilter("Silver");
    private void OnFilterGoldClick(object sender, RoutedEventArgs e) => ViewModel?.SetTierFilter("Gold");
    private void OnFilterPlatinumClick(object sender, RoutedEventArgs e) => ViewModel?.SetTierFilter("Platinum");
    private void OnNewCustomerClick(object sender, RoutedEventArgs e) => ViewModel?.StartNewCustomer();
    private void OnSaveCustomerClick(object sender, RoutedEventArgs e) => ViewModel?.SaveCustomer();
}
