using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using CinemaManageProject.ViewModels;

namespace CinemaManageProject.Views;

/// <summary>
/// Cửa sổ đăng nhập tách rời trước khi vào MainWindow Shell.
/// </summary>
public partial class LoginWindow : Wpf.Ui.Controls.FluentWindow
{
    private readonly LoginViewModel _viewModel;

    public LoginWindow()
    {
        InitializeComponent();

        _viewModel = new LoginViewModel();
        _viewModel.LoginSucceeded += OnLoginSucceeded;
        _viewModel.DemoPasswordRequested += pass => LoginPasswordBox.Password = pass;
        _viewModel.ModeSwitched += ClearPasswords;
        DataContext = _viewModel;

        Loaded += (_, _) => UsernameTextBox.Focus();
    }

    private void OnLoginClick(object sender, RoutedEventArgs e)
        => _viewModel.Login(LoginPasswordBox.Password);

    private void OnRegisterClick(object sender, RoutedEventArgs e)
        => _viewModel.Register(RegPasswordBox.Password, RegConfirmPasswordBox.Password);

    private void OnToggleModeClick(object sender, RoutedEventArgs e)
        => _viewModel.ToggleMode();

    private void OnFillDemoClick(object sender, RoutedEventArgs e)
    {
        if (sender is Button btn)
        {
            _viewModel.FillDemo(btn.Tag?.ToString());
        }
    }

    private void OnLoginPasswordKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter)
        {
            _viewModel.Login(LoginPasswordBox.Password);
        }
    }

    private void OnRegisterPasswordKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter)
        {
            _viewModel.Register(RegPasswordBox.Password, RegConfirmPasswordBox.Password);
        }
    }

    private void ClearPasswords()
    {
        LoginPasswordBox.Clear();
        RegPasswordBox.Clear();
        RegConfirmPasswordBox.Clear();
    }

    private void OnLoginSucceeded()
        => ((App)Application.Current).CompleteLogin();
}
