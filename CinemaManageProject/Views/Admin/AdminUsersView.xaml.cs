using System.Windows;
using System.Windows.Controls;
using CinemaManageProject.ViewModels.Admin;

namespace CinemaManageProject.Views.Admin;

/// <summary>
/// Màn hình Quản lý Nhân sự &amp; Tài khoản dành cho Admin.
/// </summary>
public partial class AdminUsersView : UserControl
{
    private AdminUsersViewModel? _subscribedViewModel;

    public AdminUsersView()
    {
        InitializeComponent();
        DataContextChanged += OnDataContextChanged;
    }

    private AdminUsersViewModel? ViewModel => DataContext as AdminUsersViewModel;

    private void OnDataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
    {
        if (_subscribedViewModel is not null)
        {
            _subscribedViewModel.PasswordClearRequested -= ClearPasswordInput;
        }

        _subscribedViewModel = e.NewValue as AdminUsersViewModel;
        if (_subscribedViewModel is not null)
        {
            _subscribedViewModel.PasswordClearRequested += ClearPasswordInput;
        }

        ClearPasswordInput();
    }

    private void ClearPasswordInput() => UserPasswordBox.Clear();

    private void OnNewUserClick(object sender, RoutedEventArgs e)
        => ViewModel?.StartNewUser();

    private void OnResetFiltersClick(object sender, RoutedEventArgs e)
        => ViewModel?.ResetFilters();

    private void OnSaveUserClick(object sender, RoutedEventArgs e)
        => ViewModel?.SaveUser(UserPasswordBox.Password);

    private void OnResetFormClick(object sender, RoutedEventArgs e)
        => ViewModel?.ResetForm();

    private void OnToggleActiveClick(object sender, RoutedEventArgs e)
        => ViewModel?.ToggleSelectedUserActive();

    private void OnDeleteUserClick(object sender, RoutedEventArgs e)
    {
        if (ViewModel is null || !ViewModel.CanDeleteSelectedUser(out string summary))
        {
            return;
        }

        MessageBoxResult confirm = MessageBox.Show(
            $"Bạn có chắc chắn muốn xóa tài khoản \"{summary}\"?\nHành động này không thể hoàn tác.",
            "Xác nhận xóa tài khoản",
            MessageBoxButton.YesNo,
            MessageBoxImage.Warning);

        if (confirm == MessageBoxResult.Yes)
        {
            ViewModel.DeleteSelectedUser();
        }
    }
}
