using System.Globalization;
using CinemaManageProject.Helpers;
using Microsoft.Data.Sqlite;

namespace CinemaManageProject.Data;

public sealed record AdminUserRow(
    int UserId,
    string Username,
    string FullName,
    string Email,
    string Phone,
    string Role,
    bool IsActive,
    string CreatedAtSql,
    string? LastLoginAtSql,
    int? CustomerId,
    string? DateOfBirthSql,
    int LoyaltyPoints,
    decimal TotalSpent,
    string? Tier,
    int StaffActivityCount,
    int CustomerOrdersCount)
{
    private static readonly CultureInfo ViCulture = CultureInfo.GetCultureInfo("vi-VN");

    public bool IsAdminRole => Role == "Admin";
    public bool IsCustomerRole => Role == "Customer";

    public string RoleDisplay => Role switch
    {
        "Admin" => "Quản trị viên",
        "Staff" => "Nhân viên",
        _ => "Khách hàng"
    };

    public string StatusDisplay => IsActive ? "Hoạt động" : "Đã khóa";

    public string CreatedAtDisplay => FormatHelper.FormatSqlDateTime(CreatedAtSql, "dd/MM/yyyy HH:mm");

    public string LastLoginDisplay => string.IsNullOrWhiteSpace(LastLoginAtSql)
        ? "Chưa đăng nhập"
        : FormatHelper.FormatSqlDateTime(LastLoginAtSql, "dd/MM/yyyy HH:mm");

    public string DateOfBirthDisplay => string.IsNullOrWhiteSpace(DateOfBirthSql)
        ? string.Empty
        : FormatHelper.FormatSqlDateTime(DateOfBirthSql, "dd/MM/yyyy");

    public string TierDisplay => string.IsNullOrWhiteSpace(Tier) ? "Member" : Tier;

    public string LoyaltyPointsText => $"{LoyaltyPoints.ToString("N0", ViCulture)} điểm";

    public string TotalSpentText => FormatHelper.FormatVnd(TotalSpent);

    public string ActivitySummaryText => Role switch
    {
        "Admin" => $"Quản trị gốc • {StaffActivityCount} suất/lịch",
        "Staff" => StaffActivityCount > 0 ? $"{StaffActivityCount} đơn / soát vé" : "Chưa có giao dịch",
        _ => $"{TierDisplay} • {CustomerOrdersCount} đơn"
    };

    public bool HasBlockingHistory => Role == "Customer"
        ? CustomerOrdersCount > 0 || StaffActivityCount > 0
        : StaffActivityCount > 0;

    public bool CanHardDelete => !IsAdminRole && !HasBlockingHistory;
}

public sealed record UserSaveInput(
    int? UserId,
    string Username,
    string? NewPassword,
    string FullName,
    string? Email,
    string? Phone,
    string Role,
    bool IsActive,
    string? DateOfBirthSql);

public sealed record UserSaveResult(bool Succeeded, int UserId, string? ErrorMessage);

public sealed record UserActionResult(bool Succeeded, string? ErrorMessage);

/// <summary>
/// Truy vấn và thao tác SQLite cho quản trị Nhân sự &amp; Tài khoản (Users + đồng bộ Customers).
/// </summary>
public static class UserData
{
    public static List<AdminUserRow> LoadAllUsers()
    {
        List<AdminUserRow> list = [];
        using SqliteConnection conn = DatabaseHelper.OpenConnection();
        using SqliteCommand cmd = conn.CreateCommand();
        cmd.CommandText = """
            SELECT u.UserId,
                   u.Username,
                   u.FullName,
                   u.Email,
                   u.Phone,
                   u.Role,
                   u.IsActive,
                   u.CreatedAt,
                   u.LastLoginAt,
                   c.CustomerId,
                   c.DateOfBirth,
                   COALESCE(c.LoyaltyPoints, 0) AS LoyaltyPoints,
                   COALESCE(c.TotalSpent, 0) AS TotalSpent,
                   c.Tier,
                   (SELECT COUNT(*) FROM Showtimes s WHERE s.CreatedByUserId = u.UserId)
                     + (SELECT COUNT(*) FROM Orders o WHERE o.StaffUserId = u.UserId)
                     + (SELECT COUNT(*) FROM Tickets t WHERE t.CheckedInByUserId = u.UserId) AS StaffActivityCount,
                   CASE
                       WHEN c.CustomerId IS NULL THEN 0
                       ELSE (SELECT COUNT(*) FROM Orders oc WHERE oc.CustomerId = c.CustomerId)
                   END AS CustomerOrdersCount
            FROM Users u
            LEFT JOIN Customers c ON c.UserId = u.UserId
            ORDER BY CASE u.Role
                         WHEN 'Admin' THEN 0
                         WHEN 'Staff' THEN 1
                         ELSE 2
                     END,
                     u.IsActive DESC,
                     u.UserId ASC;
            """;

        using SqliteDataReader r = cmd.ExecuteReader();
        while (r.Read())
        {
            list.Add(new AdminUserRow(
                UserId: r.GetInt32(0),
                Username: r.GetString(1),
                FullName: r.GetString(2),
                Email: r.IsDBNull(3) ? string.Empty : r.GetString(3),
                Phone: r.IsDBNull(4) ? string.Empty : r.GetString(4),
                Role: r.GetString(5),
                IsActive: r.GetInt32(6) != 0,
                CreatedAtSql: r.GetString(7),
                LastLoginAtSql: r.IsDBNull(8) ? null : r.GetString(8),
                CustomerId: r.IsDBNull(9) ? null : r.GetInt32(9),
                DateOfBirthSql: r.IsDBNull(10) ? null : r.GetString(10),
                LoyaltyPoints: r.GetInt32(11),
                TotalSpent: DatabaseHelper.ReadDecimal(r, 12),
                Tier: r.IsDBNull(13) ? null : r.GetString(13),
                StaffActivityCount: r.GetInt32(14),
                CustomerOrdersCount: r.GetInt32(15)));
        }

        return list;
    }

    public static UserSaveResult SaveUser(UserSaveInput input, int currentAdminUserId)
    {
        string cleanUsername = (input.Username ?? string.Empty).Trim();
        string cleanFullName = (input.FullName ?? string.Empty).Trim();
        string? cleanEmail = string.IsNullOrWhiteSpace(input.Email) ? null : input.Email.Trim();
        string? cleanPhone = string.IsNullOrWhiteSpace(input.Phone) ? null : input.Phone.Trim();
        string cleanRole = (input.Role ?? string.Empty).Trim();
        string? cleanDob = string.IsNullOrWhiteSpace(input.DateOfBirthSql) ? null : input.DateOfBirthSql.Trim();
        string? cleanPassword = string.IsNullOrEmpty(input.NewPassword) ? null : input.NewPassword;

        if (cleanRole != "Staff" && cleanRole != "Customer")
        {
            return new UserSaveResult(
                false,
                0,
                "Chỉ được phép tạo hoặc quản lý tài khoản Nhân viên (Staff) và Khách hàng (Customer); không thể thêm hoặc gán vai trò Quản trị viên.");
        }

        if (cleanUsername.Length < 3 || cleanUsername.Length > 50 || cleanUsername.Any(char.IsWhiteSpace))
        {
            return new UserSaveResult(false, 0, "Tên đăng nhập phải từ 3 đến 50 ký tự và không chứa khoảng trắng.");
        }

        if (string.IsNullOrWhiteSpace(cleanFullName) || cleanFullName.Length > 100)
        {
            return new UserSaveResult(false, 0, "Vui lòng nhập Họ và tên hợp lệ (tối đa 100 ký tự).");
        }

        if (cleanRole == "Customer" && string.IsNullOrWhiteSpace(cleanPhone))
        {
            return new UserSaveResult(false, 0, "Tài khoản Khách hàng bắt buộc phải có Số điện thoại để đồng bộ hồ sơ hội viên.");
        }

        if (!input.UserId.HasValue)
        {
            if (string.IsNullOrWhiteSpace(cleanPassword) || cleanPassword.Length < 6)
            {
                return new UserSaveResult(false, 0, "Vui lòng nhập Mật khẩu từ 6 ký tự trở lên (không chỉ gồm khoảng trắng) khi tạo tài khoản mới.");
            }
        }
        else if (cleanPassword is not null && (string.IsNullOrWhiteSpace(cleanPassword) || cleanPassword.Length < 6))
        {
            return new UserSaveResult(false, 0, "Mật khẩu mới phải từ 6 ký tự trở lên và không chỉ gồm khoảng trắng (hoặc để trống để giữ mật khẩu hiện tại).");
        }

        if (cleanRole == "Customer" && cleanDob is not null)
        {
            if (!DateTime.TryParseExact(cleanDob, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime parsedDob) ||
                parsedDob.Date > DateTime.Today)
            {
                return new UserSaveResult(false, 0, "Ngày sinh không hợp lệ (chuẩn yyyy-MM-dd và không vượt quá ngày hiện tại).");
            }
        }

        try
        {
            using SqliteConnection conn = DatabaseHelper.OpenConnection();
            using SqliteTransaction tx = conn.BeginTransaction();

            if (input.UserId.HasValue)
            {
                string? existingRole = ReadUserRole(conn, tx, input.UserId.Value);
                if (existingRole is null)
                {
                    return new UserSaveResult(false, 0, "Không tìm thấy tài khoản cần cập nhật.");
                }

                if (existingRole == "Admin")
                {
                    return new UserSaveResult(false, 0, "Tài khoản Quản trị viên gốc được bảo vệ, không thể chỉnh sửa hoặc đổi quyền.");
                }

                if (!string.Equals(existingRole, cleanRole, StringComparison.Ordinal))
                {
                    return new UserSaveResult(false, 0, "Không thể đổi vai trò của tài khoản đã tạo.");
                }

                if (input.UserId.Value == currentAdminUserId && !input.IsActive)
                {
                    return new UserSaveResult(false, 0, "Không thể tự khóa tài khoản đang đăng nhập.");
                }
            }

            if (UsernameExists(conn, tx, cleanUsername, input.UserId))
            {
                return new UserSaveResult(false, 0, "Tên đăng nhập đã tồn tại trong hệ thống.");
            }

            if (cleanRole == "Customer" && cleanPhone is not null && CustomerPhoneExists(conn, tx, cleanPhone, input.UserId))
            {
                return new UserSaveResult(false, 0, "Số điện thoại này đã được đăng ký cho hồ sơ khách hàng khác.");
            }

            int savedUserId;
            if (!input.UserId.HasValue)
            {
                using SqliteCommand ins = conn.CreateCommand();
                ins.Transaction = tx;
                ins.CommandText = """
                    INSERT INTO Users (Username, Password, FullName, Email, Phone, Role, IsActive)
                    VALUES (@u, @p, @n, @e, @ph, @r, @act);
                    SELECT last_insert_rowid();
                    """;
                ins.Parameters.AddWithValue("@u", cleanUsername);
                ins.Parameters.AddWithValue("@p", cleanPassword!);
                ins.Parameters.AddWithValue("@n", cleanFullName);
                ins.Parameters.AddWithValue("@e", (object?)cleanEmail ?? DBNull.Value);
                ins.Parameters.AddWithValue("@ph", (object?)cleanPhone ?? DBNull.Value);
                ins.Parameters.AddWithValue("@r", cleanRole);
                ins.Parameters.AddWithValue("@act", input.IsActive ? 1 : 0);
                savedUserId = Convert.ToInt32(ins.ExecuteScalar(), CultureInfo.InvariantCulture);
            }
            else
            {
                savedUserId = input.UserId.Value;
                using SqliteCommand upd = conn.CreateCommand();
                upd.Transaction = tx;
                upd.CommandText = cleanPassword is not null
                    ? """
                      UPDATE Users
                      SET Username = @u,
                          Password = @p,
                          FullName = @n,
                          Email = @e,
                          Phone = @ph,
                          IsActive = @act
                      WHERE UserId = @id;
                      """
                    : """
                      UPDATE Users
                      SET Username = @u,
                          FullName = @n,
                          Email = @e,
                          Phone = @ph,
                          IsActive = @act
                      WHERE UserId = @id;
                      """;
                upd.Parameters.AddWithValue("@id", savedUserId);
                upd.Parameters.AddWithValue("@u", cleanUsername);
                if (cleanPassword is not null)
                {
                    upd.Parameters.AddWithValue("@p", cleanPassword);
                }
                upd.Parameters.AddWithValue("@n", cleanFullName);
                upd.Parameters.AddWithValue("@e", (object?)cleanEmail ?? DBNull.Value);
                upd.Parameters.AddWithValue("@ph", (object?)cleanPhone ?? DBNull.Value);
                upd.Parameters.AddWithValue("@act", input.IsActive ? 1 : 0);
                upd.ExecuteNonQuery();
            }

            if (cleanRole == "Customer")
            {
                SyncCustomerProfile(conn, tx, savedUserId, cleanFullName, cleanPhone!, cleanEmail, cleanDob);
            }

            tx.Commit();
            return new UserSaveResult(true, savedUserId, null);
        }
        catch (SqliteException ex)
        {
            return new UserSaveResult(false, 0, $"Không thể lưu tài khoản: {ex.Message}");
        }
    }

    public static UserActionResult SetUserActive(int userId, bool isActive, int currentAdminUserId)
    {
        try
        {
            using SqliteConnection conn = DatabaseHelper.OpenConnection();
            using SqliteTransaction tx = conn.BeginTransaction();

            string? role = ReadUserRole(conn, tx, userId);
            if (role is null)
            {
                return new UserActionResult(false, "Không tìm thấy tài khoản.");
            }

            if (role == "Admin")
            {
                return new UserActionResult(false, "Tài khoản Quản trị viên gốc được bảo vệ, không thể khóa hoặc vô hiệu hóa.");
            }

            if (userId == currentAdminUserId && !isActive)
            {
                return new UserActionResult(false, "Không thể tự khóa tài khoản đang đăng nhập.");
            }

            using SqliteCommand cmd = conn.CreateCommand();
            cmd.Transaction = tx;
            cmd.CommandText = "UPDATE Users SET IsActive = @act WHERE UserId = @id;";
            cmd.Parameters.AddWithValue("@act", isActive ? 1 : 0);
            cmd.Parameters.AddWithValue("@id", userId);
            cmd.ExecuteNonQuery();

            tx.Commit();
            return new UserActionResult(true, null);
        }
        catch (SqliteException ex)
        {
            return new UserActionResult(false, $"Không thể cập nhật trạng thái tài khoản: {ex.Message}");
        }
    }

    public static UserActionResult DeleteUser(int userId, int currentAdminUserId)
    {
        try
        {
            using SqliteConnection conn = DatabaseHelper.OpenConnection();
            using SqliteTransaction tx = conn.BeginTransaction();

            (string? role, int? customerId) = ReadUserRoleAndCustomer(conn, tx, userId);
            if (role is null)
            {
                return new UserActionResult(false, "Không tìm thấy tài khoản cần xóa.");
            }

            if (role == "Admin")
            {
                return new UserActionResult(false, "Tài khoản Quản trị viên gốc được bảo vệ, không thể xóa.");
            }

            if (userId == currentAdminUserId)
            {
                return new UserActionResult(false, "Không thể tự xóa tài khoản đang đăng nhập.");
            }

            int staffRefs = CountStaffReferences(conn, tx, userId);
            if (staffRefs > 0)
            {
                return new UserActionResult(
                    false,
                    $"Tài khoản nhân sự này đã phát sinh {staffRefs} suất chiếu/đơn hàng/lượt soát vé nên không thể xóa. Hãy dùng nút Khóa tài khoản.");
            }

            if (customerId.HasValue)
            {
                int custOrders = CountCustomerOrders(conn, tx, customerId.Value);
                if (custOrders > 0)
                {
                    return new UserActionResult(
                        false,
                        $"Khách hàng này đã phát sinh {custOrders} hóa đơn nên không thể xóa. Hãy dùng nút Khóa tài khoản.");
                }

                using SqliteCommand delCust = conn.CreateCommand();
                delCust.Transaction = tx;
                delCust.CommandText = "DELETE FROM Customers WHERE CustomerId = @cid;";
                delCust.Parameters.AddWithValue("@cid", customerId.Value);
                delCust.ExecuteNonQuery();
            }

            using SqliteCommand delUser = conn.CreateCommand();
            delUser.Transaction = tx;
            delUser.CommandText = "DELETE FROM Users WHERE UserId = @id;";
            delUser.Parameters.AddWithValue("@id", userId);
            delUser.ExecuteNonQuery();

            tx.Commit();
            return new UserActionResult(true, null);
        }
        catch (SqliteException ex)
        {
            return new UserActionResult(false, $"Không thể xóa tài khoản: {ex.Message}");
        }
    }

    private static string? ReadUserRole(SqliteConnection conn, SqliteTransaction tx, int userId)
    {
        using SqliteCommand cmd = conn.CreateCommand();
        cmd.Transaction = tx;
        cmd.CommandText = "SELECT Role FROM Users WHERE UserId = @id LIMIT 1;";
        cmd.Parameters.AddWithValue("@id", userId);
        return cmd.ExecuteScalar()?.ToString();
    }

    private static (string? Role, int? CustomerId) ReadUserRoleAndCustomer(SqliteConnection conn, SqliteTransaction tx, int userId)
    {
        using SqliteCommand cmd = conn.CreateCommand();
        cmd.Transaction = tx;
        cmd.CommandText = """
            SELECT u.Role, c.CustomerId
            FROM Users u
            LEFT JOIN Customers c ON c.UserId = u.UserId
            WHERE u.UserId = @id
            LIMIT 1;
            """;
        cmd.Parameters.AddWithValue("@id", userId);
        using SqliteDataReader r = cmd.ExecuteReader();
        if (!r.Read())
        {
            return (null, null);
        }

        return (r.GetString(0), r.IsDBNull(1) ? null : r.GetInt32(1));
    }

    private static bool UsernameExists(SqliteConnection conn, SqliteTransaction tx, string username, int? excludeUserId)
    {
        using SqliteCommand cmd = conn.CreateCommand();
        cmd.Transaction = tx;
        cmd.CommandText = """
            SELECT 1
            FROM Users
            WHERE LOWER(Username) = LOWER(@u)
              AND (@id IS NULL OR UserId != @id)
            LIMIT 1;
            """;
        cmd.Parameters.AddWithValue("@u", username);
        cmd.Parameters.AddWithValue("@id", excludeUserId.HasValue ? excludeUserId.Value : DBNull.Value);
        return cmd.ExecuteScalar() is not null;
    }

    private static bool CustomerPhoneExists(SqliteConnection conn, SqliteTransaction tx, string phone, int? excludeUserId)
    {
        using SqliteCommand cmd = conn.CreateCommand();
        cmd.Transaction = tx;
        cmd.CommandText = """
            SELECT 1
            FROM Customers
            WHERE Phone = @ph
              AND (@uid IS NULL OR UserId IS NULL OR UserId != @uid)
            LIMIT 1;
            """;
        cmd.Parameters.AddWithValue("@ph", phone);
        cmd.Parameters.AddWithValue("@uid", excludeUserId.HasValue ? excludeUserId.Value : DBNull.Value);
        return cmd.ExecuteScalar() is not null;
    }

    private static void SyncCustomerProfile(
        SqliteConnection conn,
        SqliteTransaction tx,
        int userId,
        string fullName,
        string phone,
        string? email,
        string? dobSql)
    {
        using SqliteCommand check = conn.CreateCommand();
        check.Transaction = tx;
        check.CommandText = "SELECT CustomerId FROM Customers WHERE UserId = @uid LIMIT 1;";
        check.Parameters.AddWithValue("@uid", userId);
        object? existing = check.ExecuteScalar();

        if (existing is null)
        {
            using SqliteCommand ins = conn.CreateCommand();
            ins.Transaction = tx;
            ins.CommandText = """
                INSERT INTO Customers (UserId, FullName, Phone, Email, DateOfBirth, LoyaltyPoints, TotalSpent, Tier)
                VALUES (@uid, @n, @ph, @e, @dob, 0, 0, 'Member');
                """;
            ins.Parameters.AddWithValue("@uid", userId);
            ins.Parameters.AddWithValue("@n", fullName);
            ins.Parameters.AddWithValue("@ph", phone);
            ins.Parameters.AddWithValue("@e", (object?)email ?? DBNull.Value);
            ins.Parameters.AddWithValue("@dob", (object?)dobSql ?? DBNull.Value);
            ins.ExecuteNonQuery();
            return;
        }

        using SqliteCommand upd = conn.CreateCommand();
        upd.Transaction = tx;
        upd.CommandText = """
            UPDATE Customers
            SET FullName = @n,
                Phone = @ph,
                Email = @e,
                DateOfBirth = @dob
            WHERE UserId = @uid;
            """;
        upd.Parameters.AddWithValue("@uid", userId);
        upd.Parameters.AddWithValue("@n", fullName);
        upd.Parameters.AddWithValue("@ph", phone);
        upd.Parameters.AddWithValue("@e", (object?)email ?? DBNull.Value);
        upd.Parameters.AddWithValue("@dob", (object?)dobSql ?? DBNull.Value);
        upd.ExecuteNonQuery();
    }

    private static int CountStaffReferences(SqliteConnection conn, SqliteTransaction tx, int userId)
    {
        using SqliteCommand cmd = conn.CreateCommand();
        cmd.Transaction = tx;
        cmd.CommandText = """
            SELECT
                (SELECT COUNT(*) FROM Showtimes WHERE CreatedByUserId = @id)
                + (SELECT COUNT(*) FROM Orders WHERE StaffUserId = @id)
                + (SELECT COUNT(*) FROM Tickets WHERE CheckedInByUserId = @id);
            """;
        cmd.Parameters.AddWithValue("@id", userId);
        return Convert.ToInt32(cmd.ExecuteScalar(), CultureInfo.InvariantCulture);
    }

    private static int CountCustomerOrders(SqliteConnection conn, SqliteTransaction tx, int customerId)
    {
        using SqliteCommand cmd = conn.CreateCommand();
        cmd.Transaction = tx;
        cmd.CommandText = "SELECT COUNT(*) FROM Orders WHERE CustomerId = @cid;";
        cmd.Parameters.AddWithValue("@cid", customerId);
        return Convert.ToInt32(cmd.ExecuteScalar(), CultureInfo.InvariantCulture);
    }
}
