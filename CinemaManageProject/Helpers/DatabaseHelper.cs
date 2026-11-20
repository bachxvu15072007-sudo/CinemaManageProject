using System.Globalization;
using System.IO;
using Microsoft.Data.Sqlite;

namespace CinemaManageProject.Helpers;

/// <summary>
/// Quản lý chuỗi kết nối, mở kết nối SQLite tới cinema.db và đọc dữ liệu cơ bản.
/// </summary>
public static class DatabaseHelper
{
    public static string ConnectionString { get; } =
        $"Data Source={Path.Combine(AppContext.BaseDirectory, "Database", "cinema.db")}";

    /// <summary>
    /// Mở kết nối SQLite và bật kiểm tra khóa ngoại cho phiên làm việc.
    /// </summary>
    public static SqliteConnection OpenConnection()
    {
        var conn = new SqliteConnection(ConnectionString);
        conn.Open();

        using var pragma = conn.CreateCommand();
        pragma.CommandText = "PRAGMA foreign_keys = ON;";
        pragma.ExecuteNonQuery();

        return conn;
    }

    /// <summary>
    /// Đọc giá trị số thập phân từ SqliteDataReader, trả về 0 nếu NULL.
    /// </summary>
    public static decimal ReadDecimal(SqliteDataReader reader, int ordinal)
        => reader.IsDBNull(ordinal) ? 0m : Convert.ToDecimal(reader.GetValue(ordinal), CultureInfo.InvariantCulture);
}
