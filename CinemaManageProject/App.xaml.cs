using System.IO;
using System.Windows;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Configuration;

namespace CinemaManageProject;

/// <summary>
/// Điểm khởi chạy ứng dụng WPF, đọc cấu hình và chuỗi kết nối SQLite tới Database/cinema.db.
/// </summary>
public partial class App : Application
{
    public static IConfiguration Configuration { get; private set; } = null!;
    public static string ConnectionString { get; private set; } = "Data Source=Database/cinema.db";

    public static SqliteConnection OpenDbConnection()
    {
        var conn = new SqliteConnection(ConnectionString);
        conn.Open();

        using var pragma = conn.CreateCommand();
        pragma.CommandText = "PRAGMA foreign_keys = ON;";
        pragma.ExecuteNonQuery();

        return conn;
    }

    protected override void OnStartup(StartupEventArgs e)
    {
        Configuration = new ConfigurationBuilder()
            .SetBasePath(AppContext.BaseDirectory)
            .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
            .Build();

        var rawConnection = Configuration.GetConnectionString("DefaultConnection") ?? "Data Source=Database/cinema.db";
        ConnectionString = ResolveSqliteConnectionString(rawConnection, e.Args);

        if (e.Args.Contains("--check-db", StringComparer.OrdinalIgnoreCase))
        {
            using var conn = OpenDbConnection();
            using var cmd = conn.CreateCommand();
            cmd.CommandText = "SELECT COUNT(*) FROM Users;";
            _ = Convert.ToInt32(cmd.ExecuteScalar());
            Shutdown(0);
            return;
        }

        base.OnStartup(e);
    }

    private static string ResolveSqliteConnectionString(string rawConnection, string[] args)
    {
        for (int i = 0; i < args.Length - 1; i++)
        {
            if (args[i].Equals("--db", StringComparison.OrdinalIgnoreCase))
            {
                return $"Data Source={Path.GetFullPath(args[i + 1])}";
            }
        }

        const string prefix = "Data Source=";
        if (rawConnection.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
        {
            var dataSource = rawConnection[prefix.Length..].Trim();
            if (!Path.IsPathRooted(dataSource))
            {
                return $"{prefix}{Path.Combine(AppContext.BaseDirectory, dataSource)}";
            }
        }

        return rawConnection;
    }
}
