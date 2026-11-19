using System.Windows;
using CinemaManageProject.Models;
using CinemaManageProject.Views;

namespace CinemaManageProject;

/// <summary>
/// Điểm khởi chạy ứng dụng WPF, lưu phiên người dùng và điều phối luồng LoginWindow &lt;-&gt; MainWindow.
/// </summary>
public partial class App : Application
{
    private LoginWindow? _loginWindow;
    private Views.MainWindow? _mainWindow;
    private bool _isSwitching;

    public static User? CurrentUser { get; set; }

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        ShowLoginWindow();
    }

    public void ShowLoginWindow()
    {
        var login = new LoginWindow();
        login.Closed += (_, _) =>
        {
            if (!_isSwitching)
            {
                Shutdown();
            }
        };

        _loginWindow = login;
        MainWindow = login;
        login.Show();
    }

    public void CompleteLogin()
    {
        _isSwitching = true;

        var main = new Views.MainWindow();
        main.Closed += (_, _) =>
        {
            if (!_isSwitching)
            {
                Shutdown();
            }
        };

        _mainWindow = main;
        MainWindow = main;
        main.Show();

        LoginWindow? oldLogin = _loginWindow;
        _loginWindow = null;
        oldLogin?.Close();

        _isSwitching = false;
    }

    public void Logout()
    {
        _isSwitching = true;
        CurrentUser = null;

        ShowLoginWindow();

        Views.MainWindow? oldMain = _mainWindow;
        _mainWindow = null;
        oldMain?.Close();

        _isSwitching = false;
    }
}
