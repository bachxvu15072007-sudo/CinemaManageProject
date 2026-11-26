using CinemaManageProject.Helpers;
using CinemaManageProject.Models;
using Dapper;
using Microsoft.Data.Sqlite;

namespace CinemaManageProject.Data;

public sealed record AuthResult(bool Succeeded, User? User, string? ErrorMessage);

/// <summary>
/// Truy vấn và thao tác SQLite bằng Dapper cho đăng nhập và đăng ký tài khoản Customer.
/// </summary>
public static class AuthData
{
    public static AuthResult Authenticate(string username, string password)
    {
        try
        {
            using SqliteConnection conn = DatabaseHelper.OpenConnection();
            const string sql = """
                SELECT UserId, Username, Password, FullName, Email, Phone, Role, IsActive
                FROM Users
                WHERE Username = @username
                LIMIT 1;
                """;

            User? user = conn.QueryFirstOrDefault<User>(sql, new { username });
            if (user is null || !PasswordHelper.VerifyPassword(password, user.Password, out bool needsUpgrade))
            {
                return new AuthResult(false, null, "Tên đăng nhập hoặc mật khẩu không đúng.");
            }

            if (!user.IsActive)
            {
                return new AuthResult(false, null, "Tài khoản này đang bị khóa.");
            }

            user.Password = string.Empty;
            user.LastLoginAt = DateTime.Now;

            if (needsUpgrade)
            {
                conn.Execute(
                    "UPDATE Users SET Password = @hash, LastLoginAt = datetime('now', 'localtime') WHERE UserId = @id;",
                    new { hash = PasswordHelper.HashPassword(password), id = user.UserId });
            }
            else
            {
                conn.Execute(
                    "UPDATE Users SET LastLoginAt = datetime('now', 'localtime') WHERE UserId = @id;",
                    new { id = user.UserId });
            }

            if (user.Role == UserRole.Customer)
            {
                user.CustomerProfile = CustomerData.LoadCustomerProfile(conn, user.UserId);
            }

            return new AuthResult(true, user, null);
        }
        catch (Exception ex)
        {
            return new AuthResult(false, null, $"Lỗi đăng nhập: {ex.Message}");
        }
    }

    public static AuthResult RegisterCustomer(
        string fullName,
        string phone,
        string username,
        string? email,
        string password)
    {
        string cleanName = (fullName ?? string.Empty).Trim();
        string cleanPhone = (phone ?? string.Empty).Trim();
        string cleanUser = (username ?? string.Empty).Trim();
        string? cleanEmail = string.IsNullOrWhiteSpace(email) ? null : email.Trim();

        if (string.IsNullOrWhiteSpace(cleanName) || string.IsNullOrWhiteSpace(cleanPhone) ||
            string.IsNullOrWhiteSpace(cleanUser) || string.IsNullOrEmpty(password))
        {
            return new AuthResult(false, null, "Vui lòng điền đầy đủ Họ tên, Số điện thoại, Tên đăng nhập và Mật khẩu.");
        }

        if (string.IsNullOrWhiteSpace(password) || password.Length < 6)
        {
            return new AuthResult(false, null, "Mật khẩu phải từ 6 ký tự trở lên.");
        }

        try
        {
            using SqliteConnection conn = DatabaseHelper.OpenConnection();
            using SqliteTransaction tx = conn.BeginTransaction();

            bool usernameExists = conn.ExecuteScalar<int?>(
                "SELECT 1 FROM Users WHERE LOWER(Username) = LOWER(@v) LIMIT 1;",
                new { v = cleanUser },
                tx).HasValue;
            if (usernameExists)
            {
                return new AuthResult(false, null, "Tên đăng nhập đã tồn tại.");
            }

            bool phoneExists = conn.ExecuteScalar<int?>(
                "SELECT 1 FROM Customers WHERE Phone = @v LIMIT 1;",
                new { v = cleanPhone },
                tx).HasValue;
            if (phoneExists)
            {
                return new AuthResult(false, null, "Số điện thoại đã được đăng ký.");
            }

            string passwordHash = PasswordHelper.HashPassword(password);
            int userId = conn.ExecuteScalar<int>(
                """
                INSERT INTO Users (Username, Password, FullName, Email, Phone, Role, IsActive)
                VALUES (@u, @p, @n, @e, @ph, 'Customer', 1);
                SELECT last_insert_rowid();
                """,
                new { u = cleanUser, p = passwordHash, n = cleanName, e = cleanEmail, ph = cleanPhone },
                tx);

            int customerId = conn.ExecuteScalar<int>(
                """
                INSERT INTO Customers (UserId, FullName, Phone, Email, LoyaltyPoints, TotalSpent, Tier)
                VALUES (@uid, @n, @ph, @e, 0, 0, 'Member');
                SELECT last_insert_rowid();
                """,
                new { uid = userId, n = cleanName, ph = cleanPhone, e = cleanEmail },
                tx);

            tx.Commit();

            var user = new User
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

            return new AuthResult(true, user, null);
        }
        catch (Exception ex)
        {
            return new AuthResult(false, null, $"Không thể đăng ký: {ex.Message}");
        }
    }
}
