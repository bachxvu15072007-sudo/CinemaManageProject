using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
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

    /// <summary>
    /// Bật thanh tiêu đề tối thuần Windows 10/11 (DWM) cho Window chuẩn mà không dùng WindowChrome/FluentWindow.
    /// </summary>
    public static void ApplyDarkTitleBar(Window window)
    {
        window.SourceInitialized += (_, _) =>
        {
            nint hwnd = new WindowInteropHelper(window).Handle;
            if (hwnd == nint.Zero)
            {
                return;
            }

            int useDarkMode = 1;
            _ = DwmSetWindowAttribute(hwnd, 20, ref useDarkMode, sizeof(int));

            // COLORREF (0x00BBGGRR) cho màu nền #0F1011 và viền #23252A trên Windows 11
            int captionColor = 0x0011100F;
            _ = DwmSetWindowAttribute(hwnd, 35, ref captionColor, sizeof(int));

            int borderColor = 0x002A2523;
            _ = DwmSetWindowAttribute(hwnd, 34, ref borderColor, sizeof(int));
        };
    }

    [DllImport("dwmapi.dll")]
    private static extern int DwmSetWindowAttribute(nint hwnd, int attr, ref int attrValue, int attrSize);
}
