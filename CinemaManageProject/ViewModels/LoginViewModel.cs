using CinemaManageProject.Data;
using CommunityToolkit.Mvvm.ComponentModel;

namespace CinemaManageProject.ViewModels;

/// <summary>
/// ViewModel cho LoginWindow: đăng nhập theo Users và đăng ký tài khoản Customer mới.
/// </summary>
public sealed partial class LoginViewModel : ViewModelBase
{
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsLoginMode))]
    private bool _isRegisterMode;

    [ObservableProperty]
    private string _username = string.Empty;

    [ObservableProperty]
    private string _regFullName = string.Empty;

    [ObservableProperty]
    private string _regPhone = string.Empty;

    [ObservableProperty]
    private string _regUsername = string.Empty;

    [ObservableProperty]
    private string _regEmail = string.Empty;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasError))]
    private string? _errorMessage;

    public event Action? LoginSucceeded;
    public event Action<string>? DemoPasswordRequested;
    public event Action? ModeSwitched;

    public bool IsLoginMode => !IsRegisterMode;
    public bool HasError => !string.IsNullOrWhiteSpace(ErrorMessage);

    public void ToggleMode()
    {
        ErrorMessage = null;
        IsRegisterMode = !IsRegisterMode;
        ModeSwitched?.Invoke();
    }

    public void Login(string password)
    {
        ErrorMessage = null;
        string cleanUser = (Username ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(cleanUser) || string.IsNullOrEmpty(password))
        {
            ErrorMessage = "Vui lòng nhập đầy đủ tên đăng nhập và mật khẩu.";
            return;
        }

        AuthResult result = AuthData.Authenticate(cleanUser, password);
        if (!result.Succeeded || result.User is null)
        {
            ErrorMessage = result.ErrorMessage ?? "Tên đăng nhập hoặc mật khẩu không đúng.";
            return;
        }

        App.CurrentUser = result.User;
        LoginSucceeded?.Invoke();
    }

    public void Register(string password, string confirmPassword)
    {
        ErrorMessage = null;
        string cleanName = (RegFullName ?? string.Empty).Trim();
        string cleanPhone = (RegPhone ?? string.Empty).Trim();
        string cleanUser = (RegUsername ?? string.Empty).Trim();
        string? cleanEmail = string.IsNullOrWhiteSpace(RegEmail) ? null : RegEmail.Trim();

        if (string.IsNullOrWhiteSpace(cleanName) || string.IsNullOrWhiteSpace(cleanPhone) ||
            string.IsNullOrWhiteSpace(cleanUser) || string.IsNullOrEmpty(password))
        {
            ErrorMessage = "Vui lòng điền đầy đủ Họ tên, Số điện thoại, Tên đăng nhập và Mật khẩu.";
            return;
        }

        if (password.Length < 6)
        {
            ErrorMessage = "Mật khẩu phải từ 6 ký tự trở lên.";
            return;
        }

        if (!string.Equals(password, confirmPassword, StringComparison.Ordinal))
        {
            ErrorMessage = "Mật khẩu xác nhận không khớp.";
            return;
        }

        AuthResult result = AuthData.RegisterCustomer(cleanName, cleanPhone, cleanUser, cleanEmail, password);
        if (!result.Succeeded || result.User is null)
        {
            ErrorMessage = result.ErrorMessage ?? "Không thể đăng ký tài khoản.";
            return;
        }

        App.CurrentUser = result.User;
        LoginSucceeded?.Invoke();
    }

    public void FillDemo(string? role)
    {
        if (IsRegisterMode)
        {
            IsRegisterMode = false;
            ModeSwitched?.Invoke();
        }

        ErrorMessage = null;
        (string user, string pass) = role switch
        {
            "Admin" => ("admin", "Admin@123"),
            "Staff" => ("staff1", "Staff@123"),
            _ => ("khach1", "Khach@123")
        };

        Username = user;
        DemoPasswordRequested?.Invoke(pass);
    }
}
