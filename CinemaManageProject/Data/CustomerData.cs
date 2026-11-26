using System.Globalization;
using CinemaManageProject.Helpers;
using CinemaManageProject.Models;
using Dapper;
using Microsoft.Data.Sqlite;

namespace CinemaManageProject.Data;

public sealed record StaffCustomerRow
{
    private static readonly CultureInfo ViCulture = CultureInfo.GetCultureInfo("vi-VN");

    public int CustomerId { get; init; }
    public int? UserId { get; init; }
    public string? Username { get; init; }
    public string FullName { get; init; } = string.Empty;
    public string Phone { get; init; } = string.Empty;
    public string Email { get; init; } = string.Empty;
    public string? DateOfBirthSql { get; init; }
    public int LoyaltyPoints { get; init; }
    public decimal TotalSpent { get; init; }
    public string Tier { get; init; } = "Member";
    public string CreatedAtSql { get; init; } = string.Empty;
    public int OrderCount { get; init; }
    public int ActiveTicketCount { get; init; }

    public bool HasAppAccount => UserId.HasValue;

    public string AccountDisplay => UserId.HasValue && !string.IsNullOrWhiteSpace(Username)
        ? $"App (@{Username})"
        : "Hội viên quầy";

    public string TierDisplay => Tier switch
    {
        "Silver" => "Silver",
        "Gold" => "Gold",
        "Platinum" => "Platinum",
        _ => "Member"
    };

    public string LoyaltyPointsText => $"{LoyaltyPoints.ToString("N0", ViCulture)} điểm";
    public string TotalSpentText => FormatHelper.FormatVnd(TotalSpent);
    public string DateOfBirthDisplay => string.IsNullOrWhiteSpace(DateOfBirthSql)
        ? "Chưa cập nhật"
        : FormatHelper.FormatSqlDateTime(DateOfBirthSql, "dd/MM/yyyy");
    public string CreatedAtDisplay => FormatHelper.FormatSqlDateTime(CreatedAtSql, "dd/MM/yyyy");
    public string OrderSummaryText => $"{OrderCount} đơn • {ActiveTicketCount} chờ soát";
}

public sealed record CustomerOrderHistoryRow
{
    public int OrderId { get; init; }
    public string OrderCode { get; init; } = string.Empty;
    public string BookingChannel { get; init; } = string.Empty;
    public int TotalTickets { get; init; }
    public decimal TotalAmount { get; init; }
    public string PaymentMethod { get; init; } = string.Empty;
    public string Status { get; init; } = string.Empty;
    public string CreatedAtSql { get; init; } = string.Empty;
    public string MoviesSummary { get; init; } = string.Empty;

    public string ChannelDisplay => BookingChannel == "SelfService" ? "Tự đặt" : "Tại quầy";
    public string TicketsText => $"{TotalTickets} vé";
    public string TotalAmountText => FormatHelper.FormatVnd(TotalAmount);
    public string PaymentDisplay => FormatHelper.FormatPaymentMethod(PaymentMethod);
    public string StatusDisplay => FormatHelper.FormatStatusLabel(Status);
    public string CreatedAtDisplay => FormatHelper.FormatSqlDateTime(CreatedAtSql, "dd/MM/yyyy HH:mm");
}

public sealed record CounterCustomerSaveInput(
    int? CustomerId,
    string FullName,
    string Phone,
    string? Email,
    string? DateOfBirthSql);

public sealed record CounterCustomerSaveResult(bool Succeeded, int CustomerId, string? ErrorMessage);

/// <summary>
/// Truy vấn và thao tác SQLite bằng Dapper cho bảng Customers (hồ sơ hội viên, tra cứu &amp; đăng ký tại quầy).
/// </summary>
public static class CustomerData
{
    /// <summary>
    /// Đọc hồ sơ khách hàng từ bảng Customers theo UserId.
    /// </summary>
    public static Customer? LoadCustomerProfile(SqliteConnection conn, int userId)
    {
        const string sql = """
            SELECT CustomerId, UserId, FullName, Phone, Email, LoyaltyPoints, TotalSpent, Tier
            FROM Customers
            WHERE UserId = @uid
            LIMIT 1;
            """;
        return conn.QueryFirstOrDefault<Customer>(sql, new { uid = userId });
    }

    public static List<StaffCustomerRow> LoadAllCustomers()
    {
        using SqliteConnection conn = DatabaseHelper.OpenConnection();
        const string sql = """
            SELECT c.CustomerId,
                   c.UserId,
                   u.Username,
                   c.FullName,
                   c.Phone,
                   COALESCE(c.Email, '') AS Email,
                   c.DateOfBirth AS DateOfBirthSql,
                   c.LoyaltyPoints,
                   c.TotalSpent,
                   c.Tier,
                   c.CreatedAt AS CreatedAtSql,
                   (SELECT COUNT(*) FROM Orders o WHERE o.CustomerId = c.CustomerId AND o.Status != 'Cancelled') AS OrderCount,
                   (SELECT COUNT(*)
                    FROM Tickets t
                    INNER JOIN Orders o ON o.OrderId = t.OrderId
                    WHERE o.CustomerId = c.CustomerId AND t.Status = 'Booked') AS ActiveTicketCount
            FROM Customers c
            LEFT JOIN Users u ON u.UserId = c.UserId
            ORDER BY CASE c.Tier
                         WHEN 'Platinum' THEN 0
                         WHEN 'Gold' THEN 1
                         WHEN 'Silver' THEN 2
                         ELSE 3
                     END,
                     c.TotalSpent DESC,
                     c.CustomerId ASC;
            """;
        return conn.Query<StaffCustomerRow>(sql).AsList();
    }

    public static List<CustomerOrderHistoryRow> LoadCustomerOrderHistory(int customerId)
    {
        using SqliteConnection conn = DatabaseHelper.OpenConnection();
        const string sql = """
            SELECT o.OrderId,
                   o.OrderCode,
                   o.BookingChannel,
                   o.TotalTickets,
                   o.TotalAmount,
                   o.PaymentMethod,
                   o.Status,
                   o.CreatedAt AS CreatedAtSql,
                   COALESCE((SELECT GROUP_CONCAT(DISTINCT m.Title)
                             FROM Tickets t
                             INNER JOIN Showtimes s ON s.ShowtimeId = t.ShowtimeId
                             INNER JOIN Movies m ON m.MovieId = s.MovieId
                             WHERE t.OrderId = o.OrderId), '—') AS MoviesSummary
            FROM Orders o
            WHERE o.CustomerId = @cid
            ORDER BY o.CreatedAt DESC, o.OrderId DESC
            LIMIT 25;
            """;
        return conn.Query<CustomerOrderHistoryRow>(sql, new { cid = customerId }).AsList();
    }

    public static CounterCustomerSaveResult SaveCounterCustomer(CounterCustomerSaveInput input)
    {
        string cleanName = (input.FullName ?? string.Empty).Trim();
        string cleanPhone = (input.Phone ?? string.Empty).Trim();
        string? cleanEmail = string.IsNullOrWhiteSpace(input.Email) ? null : input.Email.Trim();
        string? cleanDob = string.IsNullOrWhiteSpace(input.DateOfBirthSql) ? null : input.DateOfBirthSql.Trim();

        if (string.IsNullOrWhiteSpace(cleanName) || cleanName.Length > 100)
        {
            return new CounterCustomerSaveResult(false, 0, "Vui lòng nhập Họ tên khách hàng hợp lệ (tối đa 100 ký tự).");
        }

        if (cleanPhone.Length < 8 || cleanPhone.Length > 20 || cleanPhone.Any(char.IsWhiteSpace))
        {
            return new CounterCustomerSaveResult(false, 0, "Số điện thoại phải từ 8 đến 20 ký tự và không chứa khoảng trắng.");
        }

        if (cleanDob is not null)
        {
            if (!DateTime.TryParseExact(cleanDob, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime parsedDob) ||
                parsedDob.Date > DateTime.Today)
            {
                return new CounterCustomerSaveResult(false, 0, "Ngày sinh phải đúng định dạng yyyy-MM-dd và không vượt quá ngày hiện tại.");
            }
        }

        try
        {
            using SqliteConnection conn = DatabaseHelper.OpenConnection();
            using SqliteTransaction tx = conn.BeginTransaction();

            bool phoneExists = conn.ExecuteScalar<int?>(
                """
                SELECT 1
                FROM Customers
                WHERE Phone = @ph
                  AND (@cid IS NULL OR CustomerId != @cid)
                LIMIT 1;
                """,
                new { ph = cleanPhone, cid = input.CustomerId },
                tx).HasValue;

            if (phoneExists)
            {
                return new CounterCustomerSaveResult(false, 0, "Số điện thoại này đã tồn tại trong hệ thống hội viên.");
            }

            int savedId;
            if (!input.CustomerId.HasValue)
            {
                savedId = conn.ExecuteScalar<int>(
                    """
                    INSERT INTO Customers (UserId, FullName, Phone, Email, DateOfBirth, LoyaltyPoints, TotalSpent, Tier)
                    VALUES (NULL, @n, @ph, @e, @dob, 0, 0, 'Member');
                    SELECT last_insert_rowid();
                    """,
                    new { n = cleanName, ph = cleanPhone, e = cleanEmail, dob = cleanDob },
                    tx);
            }
            else
            {
                savedId = input.CustomerId.Value;
                int? linkedUserId = conn.QueryFirstOrDefault<int?>(
                    "SELECT UserId FROM Customers WHERE CustomerId = @cid LIMIT 1;",
                    new { cid = savedId },
                    tx);

                int updated = conn.Execute(
                    """
                    UPDATE Customers
                    SET FullName = @n, Phone = @ph, Email = @e, DateOfBirth = @dob
                    WHERE CustomerId = @cid;
                    """,
                    new { cid = savedId, n = cleanName, ph = cleanPhone, e = cleanEmail, dob = cleanDob },
                    tx);

                if (updated != 1)
                {
                    return new CounterCustomerSaveResult(false, 0, "Không tìm thấy hồ sơ khách hàng cần cập nhật.");
                }

                if (linkedUserId.HasValue)
                {
                    conn.Execute(
                        "UPDATE Users SET FullName = @n, Phone = @ph, Email = @e WHERE UserId = @uid;",
                        new { uid = linkedUserId.Value, n = cleanName, ph = cleanPhone, e = cleanEmail },
                        tx);
                }
            }

            tx.Commit();
            return new CounterCustomerSaveResult(true, savedId, null);
        }
        catch (SqliteException ex)
        {
            return new CounterCustomerSaveResult(false, 0, $"Không thể lưu hồ sơ hội viên: {ex.Message}");
        }
    }
}
