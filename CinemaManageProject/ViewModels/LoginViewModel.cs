using System.Globalization;
using CinemaManageProject.Models;
using CinemaManageProject.Themes;
using Microsoft.Data.Sqlite;

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
    private string _themeButtonText = string.Empty;

    public LoginViewModel()
    {
        ToggleModeCommand = new RelayCommand(() =>
        {
            ErrorMessage = null;
            IsRegisterMode = !IsRegisterMode;
            ModeSwitched?.Invoke();
        });
        ToggleThemeCommand = new RelayCommand(() => ThemeManager.ToggleTheme());
        FillDemoCommand = new RelayCommand(FillDemo);

        ThemeManager.ThemeChanged += OnThemeChanged;
        UpdateThemeText();
    }

    public event Action? LoginSucceeded;
    public event Action<string>? DemoPasswordRequested;
    public event Action? ModeSwitched;

    public bool IsRegisterMode
    {
        get => _isRegisterMode;
        private set
        {
            if (SetProperty(ref _isRegisterMode, value))
            {
                OnPropertyChanged(nameof(IsLoginMode));
            }
        }
    }

    public bool IsLoginMode => !IsRegisterMode;

    public string Username { get => _username; set => SetProperty(ref _username, value); }
    public string RegFullName { get => _regFullName; set => SetProperty(ref _regFullName, value); }
    public string RegPhone { get => _regPhone; set => SetProperty(ref _regPhone, value); }
    public string RegUsername { get => _regUsername; set => SetProperty(ref _regUsername, value); }
    public string RegEmail { get => _regEmail; set => SetProperty(ref _regEmail, value); }

    public string? ErrorMessage
    {
        get => _errorMessage;
        private set
        {
            if (SetProperty(ref _errorMessage, value))
            {
                OnPropertyChanged(nameof(HasError));
            }
        }
    }

    public bool HasError => !string.IsNullOrWhiteSpace(ErrorMessage);

    public string ThemeButtonText { get => _themeButtonText; private set => SetProperty(ref _themeButtonText, value); }

    public RelayCommand ToggleModeCommand { get; }
    public RelayCommand ToggleThemeCommand { get; }
    public RelayCommand FillDemoCommand { get; }

    public void Login(string password)
    {
        ErrorMessage = null;
        string cleanUser = Username.Trim();
        if (string.IsNullOrWhiteSpace(cleanUser) || string.IsNullOrEmpty(password))
        {
            ErrorMessage = "Vui lòng nhập đầy đủ tên đăng nhập và mật khẩu.";
            return;
        }

        try
        {
            using SqliteConnection conn = App.OpenDbConnection();
            using SqliteCommand cmd = conn.CreateCommand();
            cmd.CommandText = """
                SELECT UserId, Username, Password, FullName, Email, Phone, Role, IsActive
                FROM Users
                WHERE Username = @username
                LIMIT 1;
                """;
            cmd.Parameters.AddWithValue("@username", cleanUser);

            using SqliteDataReader r = cmd.ExecuteReader();
            if (!r.Read() || !string.Equals(r.GetString(2), password, StringComparison.Ordinal))
            {
                ErrorMessage = "Tên đăng nhập hoặc mật khẩu không đúng.";
                return;
            }

            if (r.GetInt32(7) == 0)
            {
                ErrorMessage = "Tài khoản này đang bị khóa.";
                return;
            }

            var user = new User
            {
                UserId = r.GetInt32(0),
                Username = r.GetString(1),
                Password = r.GetString(2),
                FullName = r.GetString(3),
                Email = r.IsDBNull(4) ? null : r.GetString(4),
                Phone = r.IsDBNull(5) ? null : r.GetString(5),
                Role = Enum.TryParse(r.GetString(6), true, out UserRole role) ? role : UserRole.Customer,
                IsActive = true,
                LastLoginAt = DateTime.Now
            };
            r.Close();

            using SqliteCommand upd = conn.CreateCommand();
            upd.CommandText = "UPDATE Users SET LastLoginAt = datetime('now', 'localtime') WHERE UserId = @id;";
            upd.Parameters.AddWithValue("@id", user.UserId);
            upd.ExecuteNonQuery();

            if (user.Role == UserRole.Customer)
            {
                user.CustomerProfile = LoadCustomerProfile(conn, user.UserId);
            }

            App.CurrentUser = user;
            LoginSucceeded?.Invoke();
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Lỗi đăng nhập: {ex.Message}";
        }
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

        try
        {
            using SqliteConnection conn = App.OpenDbConnection();
            using SqliteTransaction tx = conn.BeginTransaction();

            if (ScalarExists(conn, tx, "SELECT 1 FROM Users WHERE Username = @v LIMIT 1;", cleanUser))
            {
                ErrorMessage = "Tên đăng nhập đã tồn tại.";
                return;
            }

            if (ScalarExists(conn, tx, "SELECT 1 FROM Customers WHERE Phone = @v LIMIT 1;", cleanPhone))
            {
                ErrorMessage = "Số điện thoại đã được đăng ký.";
                return;
            }

            int userId;
            using (SqliteCommand cmd = conn.CreateCommand())
            {
                cmd.Transaction = tx;
                cmd.CommandText = """
                    INSERT INTO Users (Username, Password, FullName, Email, Phone, Role, IsActive)
                    VALUES (@u, @p, @n, @e, @ph, 'Customer', 1);
                    SELECT last_insert_rowid();
                    """;
                cmd.Parameters.AddWithValue("@u", cleanUser);
                cmd.Parameters.AddWithValue("@p", password);
                cmd.Parameters.AddWithValue("@n", cleanName);
                cmd.Parameters.AddWithValue("@e", (object?)cleanEmail ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@ph", cleanPhone);
                userId = Convert.ToInt32(cmd.ExecuteScalar());
            }

            int customerId;
            using (SqliteCommand cmd = conn.CreateCommand())
            {
                cmd.Transaction = tx;
                cmd.CommandText = """
                    INSERT INTO Customers (UserId, FullName, Phone, Email, LoyaltyPoints, TotalSpent, Tier)
                    VALUES (@uid, @n, @ph, @e, 0, 0, 'Member');
                    SELECT last_insert_rowid();
                    """;
                cmd.Parameters.AddWithValue("@uid", userId);
                cmd.Parameters.AddWithValue("@n", cleanName);
                cmd.Parameters.AddWithValue("@ph", cleanPhone);
                cmd.Parameters.AddWithValue("@e", (object?)cleanEmail ?? DBNull.Value);
                customerId = Convert.ToInt32(cmd.ExecuteScalar());
            }

            tx.Commit();

            App.CurrentUser = new User
            {
                UserId = userId,
                Username = cleanUser,
                FullName = cleanName,
                Email = cleanEmail,
                Phone = cleanPhone,
                Role = UserRole.Customer,
                IsActive = true,
                CustomerProfile = new Customer
                {
                    CustomerId = customerId,
                    UserId = userId,
                    FullName = cleanName,
                    Phone = cleanPhone,
                    Email = cleanEmail,
                    Tier = CustomerTier.Member
                }
            };

            LoginSucceeded?.Invoke();
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Không thể đăng ký: {ex.Message}";
        }
    }

    public void Detach() => ThemeManager.ThemeChanged -= OnThemeChanged;

    private static bool ScalarExists(SqliteConnection conn, SqliteTransaction tx, string sql, string val)
    {
        using SqliteCommand cmd = conn.CreateCommand();
        cmd.Transaction = tx;
        cmd.CommandText = sql;
        cmd.Parameters.AddWithValue("@v", val);
        return cmd.ExecuteScalar() is not null;
    }

    private static Customer? LoadCustomerProfile(SqliteConnection conn, int userId)
    {
        using SqliteCommand cmd = conn.CreateCommand();
        cmd.CommandText = """
            SELECT CustomerId, FullName, Phone, Email, LoyaltyPoints, TotalSpent, Tier
            FROM Customers
            WHERE UserId = @uid
            LIMIT 1;
            """;
        cmd.Parameters.AddWithValue("@uid", userId);

        using SqliteDataReader r = cmd.ExecuteReader();
        if (!r.Read())
        {
            return null;
        }

        return new Customer
        {
            CustomerId = r.GetInt32(0),
            UserId = userId,
            FullName = r.GetString(1),
            Phone = r.GetString(2),
            Email = r.IsDBNull(3) ? null : r.GetString(3),
            LoyaltyPoints = r.GetInt32(4),
            TotalSpent = Convert.ToDecimal(r.GetValue(5), CultureInfo.InvariantCulture),
            Tier = Enum.TryParse(r.GetString(6), true, out CustomerTier tier) ? tier : CustomerTier.Member
        };
    }

    private void FillDemo(object? param)
    {
        if (IsRegisterMode)
        {
            IsRegisterMode = false;
            ModeSwitched?.Invoke();
        }

        ErrorMessage = null;
        (string user, string pass) = param?.ToString() switch
        {
            "Admin" => ("admin", "Admin@123"),
            "Staff" => ("staff1", "Staff@123"),
            _ => ("khach1", "Khach@123")
        };

        Username = user;
        DemoPasswordRequested?.Invoke(pass);
    }

    private void OnThemeChanged(object? sender, AppTheme e) => UpdateThemeText();

    private void UpdateThemeText() =>
        ThemeButtonText = ThemeManager.CurrentTheme == AppTheme.Dark ? "☀ Light Mode" : "🌙 Dark Mode";
}
