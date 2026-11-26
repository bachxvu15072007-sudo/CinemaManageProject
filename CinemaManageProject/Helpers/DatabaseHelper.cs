using System.Data;
using System.Globalization;
using System.IO;
using Dapper;
using Microsoft.Data.Sqlite;

namespace CinemaManageProject.Helpers;

/// <summary>
/// Quản lý chuỗi kết nối và mở kết nối SQLite tới cinema.db.
/// </summary>
public static class DatabaseHelper
{
    static DatabaseHelper()
    {
        // SQLite trả về Int64 cho số tròn (1.0 -> 1) và Double cho số lẻ (1.25) trên cùng một cột NUMERIC;
        // TypeHandler giúp Dapper luôn chuyển đổi an toàn sang decimal bất kể kiểu động từng dòng.
        SqlMapper.AddTypeHandler(new SqliteDecimalHandler());
    }

    public static string ConnectionString { get; } =
        $"Data Source={Path.Combine(AppContext.BaseDirectory, "Database", "cinema.db")}";

    /// <summary>
    /// Mở kết nối SQLite và bật kiểm tra khóa ngoại cho phiên làm việc.
    /// </summary>
    public static SqliteConnection OpenConnection()
    {
        var conn = new SqliteConnection(ConnectionString);
        conn.Open();

        using SqliteCommand pragma = conn.CreateCommand();
        pragma.CommandText = "PRAGMA foreign_keys = ON;";
        pragma.ExecuteNonQuery();

        return conn;
    }

    private sealed class SqliteDecimalHandler : SqlMapper.TypeHandler<decimal>
    {
        public override decimal Parse(object value)
            => Convert.ToDecimal(value, CultureInfo.InvariantCulture);

        public override void SetValue(IDbDataParameter parameter, decimal value)
            => parameter.Value = value;
    }
}
