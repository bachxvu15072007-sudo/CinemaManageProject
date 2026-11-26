using CinemaManageProject.Models;
using Dapper;
using Microsoft.Data.Sqlite;

namespace CinemaManageProject.Data;

/// <summary>
/// Truy vấn SQLite bằng Dapper cho bảng Customers.
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
}
