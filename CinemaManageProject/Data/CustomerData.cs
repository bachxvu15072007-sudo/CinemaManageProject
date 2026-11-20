using CinemaManageProject.Helpers;
using CinemaManageProject.Models;
using Microsoft.Data.Sqlite;

namespace CinemaManageProject.Data;

/// <summary>
/// Truy vấn SQLite cho bảng Customers.
/// </summary>
public static class CustomerData
{
    /// <summary>
    /// Đọc hồ sơ khách hàng từ bảng Customers theo UserId.
    /// </summary>
    public static Customer? LoadCustomerProfile(SqliteConnection conn, int userId)
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
            TotalSpent = DatabaseHelper.ReadDecimal(r, 5),
            Tier = Enum.TryParse(r.GetString(6), true, out CustomerTier tier) ? tier : CustomerTier.Member
        };
    }
}
