using CinemaManageProject.Data;

namespace CinemaManageProject.ViewModels;

/// <summary>
/// ViewModel cho LoginWindow: đăng nhập theo Users và đăng ký tài khoản Customer mới.
/// </summary>
public sealed class LoginViewModel : ViewModelBase
{
    private bool _isRegisterMode;
    private string _username = string.Empty;
    private string _regFullName = string.Empty;
    private string _regPhone = string.Empty;
    private string _regUsername = string.Empty;
    private string _regEmail = string.Empty;
    private string? _errorMessage;

    public LoginViewModel()
    {
    }

    public event Action? LoginSucceeded;
    public event Action<string>? DemoPasswordRequested;
    public event Action? ModeSwitched;

    public bool IsRegisterMode
    {
        get => _isRegisterMode;
        private set
        {
            if (value == _isRegisterMode) return;
            _isRegisterMode = value;
            OnPropertyChanged(nameof(IsRegisterMode));
            OnPropertyChanged(nameof(IsLoginMode));
        }
    }

    public bool IsLoginMode => !IsRegisterMode;

    public string Username
    {
        get => _username;
        set { _username = value; OnPropertyChanged(nameof(Username)); }
    }

    public string RegFullName
    {
        get => _regFullName;
        set { _regFullName = value; OnPropertyChanged(nameof(RegFullName)); }
    }

    public string RegPhone
    {
        get => _regPhone;
        set { _regPhone = value; OnPropertyChanged(nameof(RegPhone)); }
    }

    public string RegUsername
    {
        get => _regUsername;
        set { _regUsername = value; OnPropertyChanged(nameof(RegUsername)); }
    }

    public string RegEmail
    {
        get => _regEmail;
        set { _regEmail = value; OnPropertyChanged(nameof(RegEmail)); }
    }

    public string? ErrorMessage
    {
        get => _errorMessage;
        private set
        {
            _errorMessage = value;
            OnPropertyChanged(nameof(ErrorMessage));
            OnPropertyChanged(nameof(HasError));
        }
    }

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
        string cleanUser = Username.Trim();
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
        string cleanName = RegFullName.Trim();
        string cleanPhone = RegPhone.Trim();
        string cleanUser = RegUsername.Trim();
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
