using CinemaManageProject.Helpers;
using CinemaManageProject.Models;
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
            if(value == _isRegisterMode) return;
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
                user.CustomerProfile = CinemaHelper.LoadCustomerProfile(conn, user.UserId);
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

    private static bool ScalarExists(SqliteConnection conn, SqliteTransaction tx, string sql, string val)
    {
        using SqliteCommand cmd = conn.CreateCommand();
        cmd.Transaction = tx;
        cmd.CommandText = sql;
        cmd.Parameters.AddWithValue("@v", val);
        return cmd.ExecuteScalar() is not null;
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
