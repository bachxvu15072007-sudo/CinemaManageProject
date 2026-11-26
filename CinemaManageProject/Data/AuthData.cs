using System.Globalization;
using CinemaManageProject.Helpers;
using CinemaManageProject.Models;
using Microsoft.Data.Sqlite;

namespace CinemaManageProject.Data;

public sealed record AuthResult(bool Succeeded, User? User, string? ErrorMessage);

/// <summary>
/// Truy vấn và thao tác SQLite cho đăng nhập và đăng ký tài khoản Customer.
/// </summary>
public static class AuthData
{
    public static AuthResult Authenticate(string username, string password)
    {
        try
        {
            using SqliteConnection conn = DatabaseHelper.OpenConnection();
            using SqliteCommand cmd = conn.CreateCommand();
            cmd.CommandText = """
                SELECT UserId, Username, Password, FullName, Email, Phone, Role, IsActive
                FROM Users
                WHERE Username = @username
                LIMIT 1;
                """;
            cmd.Parameters.AddWithValue("@username", username);

            using SqliteDataReader r = cmd.ExecuteReader();
            if (!r.Read() || !string.Equals(r.GetString(2), password, StringComparison.Ordinal))
            {
                return new AuthResult(false, null, "Tên đăng nhập hoặc mật khẩu không đúng.");
            }

            if (r.GetInt32(7) == 0)
            {
                return new AuthResult(false, null, "Tài khoản này đang bị khóa.");
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

            if (ScalarExists(conn, tx, "SELECT 1 FROM Users WHERE LOWER(Username) = LOWER(@v) LIMIT 1;", cleanUser))
            {
                return new AuthResult(false, null, "Tên đăng nhập đã tồn tại.");
            }

            if (ScalarExists(conn, tx, "SELECT 1 FROM Customers WHERE Phone = @v LIMIT 1;", cleanPhone))
            {
                return new AuthResult(false, null, "Số điện thoại đã được đăng ký.");
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
                userId = Convert.ToInt32(cmd.ExecuteScalar(), CultureInfo.InvariantCulture);
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
                customerId = Convert.ToInt32(cmd.ExecuteScalar(), CultureInfo.InvariantCulture);
            }

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

    private static bool ScalarExists(SqliteConnection conn, SqliteTransaction tx, string sql, string val)
    {
        using SqliteCommand cmd = conn.CreateCommand();
        cmd.Transaction = tx;
        cmd.CommandText = sql;
        cmd.Parameters.AddWithValue("@v", val);
        return cmd.ExecuteScalar() is not null;
    }
}
