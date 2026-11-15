using System.IO;
using System.Windows;
using Microsoft.Data.Sqlite;

namespace CinemaManageProject;

/// <summary>
/// Điểm khởi chạy ứng dụng WPF và cung cấp kết nối tới Database/cinema.db.
/// </summary>
public partial class App : Application
{
    public static string ConnectionString { get; } =
        $"Data Source={Path.Combine(AppContext.BaseDirectory, "Database", "cinema.db")}";

    public static SqliteConnection OpenDbConnection()
    {
        var conn = new SqliteConnection(ConnectionString);
        conn.Open();

        using var pragma = conn.CreateCommand();
        pragma.CommandText = "PRAGMA foreign_keys = ON;";
        pragma.ExecuteNonQuery();

        return conn;
    }
}
