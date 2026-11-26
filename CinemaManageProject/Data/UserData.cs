using System.Globalization;
using CinemaManageProject.Helpers;
using Dapper;
using Microsoft.Data.Sqlite;

namespace CinemaManageProject.Data;

public sealed record AdminUserRow
{
    private static readonly CultureInfo ViCulture = CultureInfo.GetCultureInfo("vi-VN");

    public int UserId { get; init; }
    public string Username { get; init; } = string.Empty;
    public string FullName { get; init; } = string.Empty;
    public string Email { get; init; } = string.Empty;
    public string Phone { get; init; } = string.Empty;
    public string Role { get; init; } = string.Empty;
    public bool IsActive { get; init; }
    public string CreatedAtSql { get; init; } = string.Empty;
    public string? LastLoginAtSql { get; init; }
    public int? CustomerId { get; init; }
    public string? DateOfBirthSql { get; init; }
    public int LoyaltyPoints { get; init; }
    public decimal TotalSpent { get; init; }
    public string? Tier { get; init; }
    public int StaffActivityCount { get; init; }
    public int CustomerOrdersCount { get; init; }

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
/// Truy vấn và thao tác SQLite bằng Dapper cho quản trị Nhân sự &amp; Tài khoản (Users + đồng bộ Customers).
/// </summary>
public static class UserData
{
    private sealed class UserRoleCustomerRow
    {
        public string Role { get; init; } = string.Empty;
        public int? CustomerId { get; init; }
    }

    public static List<AdminUserRow> LoadAllUsers()
    {
        using SqliteConnection conn = DatabaseHelper.OpenConnection();
        const string sql = """
            SELECT u.UserId,
                   u.Username,
                   u.FullName,
                   COALESCE(u.Email, '') AS Email,
                   COALESCE(u.Phone, '') AS Phone,
                   u.Role,
                   u.IsActive,
                   u.CreatedAt AS CreatedAtSql,
                   u.LastLoginAt AS LastLoginAtSql,
                   c.CustomerId,
                   c.DateOfBirth AS DateOfBirthSql,
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

        return conn.Query<AdminUserRow>(sql).AsList();
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
            int activeFlag = input.IsActive ? 1 : 0;
            string? hashedPassword = cleanPassword is not null
                ? PasswordHelper.HashPassword(cleanPassword)
                : null;

            if (!input.UserId.HasValue)
            {
                savedUserId = conn.ExecuteScalar<int>(
                    """
                    INSERT INTO Users (Username, Password, FullName, Email, Phone, Role, IsActive)
                    VALUES (@u, @p, @n, @e, @ph, @r, @act);
                    SELECT last_insert_rowid();
                    """,
                    new { u = cleanUsername, p = hashedPassword!, n = cleanFullName, e = cleanEmail, ph = cleanPhone, r = cleanRole, act = activeFlag },
                    tx);
            }
            else
            {
                savedUserId = input.UserId.Value;
                string updateSql = hashedPassword is not null
                    ? """
                      UPDATE Users
                      SET Username = @u, Password = @p, FullName = @n, Email = @e, Phone = @ph, IsActive = @act
                      WHERE UserId = @id;
                      """
                    : """
                      UPDATE Users
                      SET Username = @u, FullName = @n, Email = @e, Phone = @ph, IsActive = @act
                      WHERE UserId = @id;
                      """;

                conn.Execute(
                    updateSql,
                    new { id = savedUserId, u = cleanUsername, p = hashedPassword, n = cleanFullName, e = cleanEmail, ph = cleanPhone, act = activeFlag },
                    tx);
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

            conn.Execute(
                "UPDATE Users SET IsActive = @act WHERE UserId = @id;",
                new { act = isActive ? 1 : 0, id = userId },
                tx);

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

            UserRoleCustomerRow? info = conn.QueryFirstOrDefault<UserRoleCustomerRow>(
                """
                SELECT u.Role, c.CustomerId
                FROM Users u
                LEFT JOIN Customers c ON c.UserId = u.UserId
                WHERE u.UserId = @id
                LIMIT 1;
                """,
                new { id = userId },
                tx);

            if (info is null)
            {
                return new UserActionResult(false, "Không tìm thấy tài khoản cần xóa.");
            }

            if (info.Role == "Admin")
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

            if (info.CustomerId.HasValue)
            {
                int custOrders = CountCustomerOrders(conn, tx, info.CustomerId.Value);
                if (custOrders > 0)
                {
                    return new UserActionResult(
                        false,
                        $"Khách hàng này đã phát sinh {custOrders} hóa đơn nên không thể xóa. Hãy dùng nút Khóa tài khoản.");
                }

                conn.Execute("DELETE FROM Customers WHERE CustomerId = @cid;", new { cid = info.CustomerId.Value }, tx);
            }

            conn.Execute("DELETE FROM Users WHERE UserId = @id;", new { id = userId }, tx);

            tx.Commit();
            return new UserActionResult(true, null);
        }
        catch (SqliteException ex)
        {
            return new UserActionResult(false, $"Không thể xóa tài khoản: {ex.Message}");
        }
    }

    private static string? ReadUserRole(SqliteConnection conn, SqliteTransaction tx, int userId)
        => conn.QueryFirstOrDefault<string>(
            "SELECT Role FROM Users WHERE UserId = @id LIMIT 1;",
            new { id = userId },
            tx);

    private static bool UsernameExists(SqliteConnection conn, SqliteTransaction tx, string username, int? excludeUserId)
        => conn.ExecuteScalar<int?>(
            """
            SELECT 1
            FROM Users
            WHERE LOWER(Username) = LOWER(@u)
              AND (@id IS NULL OR UserId != @id)
            LIMIT 1;
            """,
            new { u = username, id = excludeUserId },
            tx).HasValue;

    private static bool CustomerPhoneExists(SqliteConnection conn, SqliteTransaction tx, string phone, int? excludeUserId)
        => conn.ExecuteScalar<int?>(
            """
            SELECT 1
            FROM Customers
            WHERE Phone = @ph
              AND (@uid IS NULL OR UserId IS NULL OR UserId != @uid)
            LIMIT 1;
            """,
            new { ph = phone, uid = excludeUserId },
            tx).HasValue;

    private static void SyncCustomerProfile(
        SqliteConnection conn,
        SqliteTransaction tx,
        int userId,
        string fullName,
        string phone,
        string? email,
        string? dobSql)
    {
        bool exists = conn.ExecuteScalar<int?>(
            "SELECT CustomerId FROM Customers WHERE UserId = @uid LIMIT 1;",
            new { uid = userId },
            tx).HasValue;

        string sql = exists
            ? """
              UPDATE Customers
              SET FullName = @n, Phone = @ph, Email = @e, DateOfBirth = @dob
              WHERE UserId = @uid;
              """
            : """
              INSERT INTO Customers (UserId, FullName, Phone, Email, DateOfBirth, LoyaltyPoints, TotalSpent, Tier)
              VALUES (@uid, @n, @ph, @e, @dob, 0, 0, 'Member');
              """;

        conn.Execute(sql, new { uid = userId, n = fullName, ph = phone, e = email, dob = dobSql }, tx);
    }

    private static int CountStaffReferences(SqliteConnection conn, SqliteTransaction tx, int userId)
        => conn.ExecuteScalar<int>(
            """
            SELECT
                (SELECT COUNT(*) FROM Showtimes WHERE CreatedByUserId = @id)
                + (SELECT COUNT(*) FROM Orders WHERE StaffUserId = @id)
                + (SELECT COUNT(*) FROM Tickets WHERE CheckedInByUserId = @id);
            """,
            new { id = userId },
            tx);

    private static int CountCustomerOrders(SqliteConnection conn, SqliteTransaction tx, int customerId)
        => conn.ExecuteScalar<int>(
            "SELECT COUNT(*) FROM Orders WHERE CustomerId = @cid;",
            new { cid = customerId },
            tx);
}
