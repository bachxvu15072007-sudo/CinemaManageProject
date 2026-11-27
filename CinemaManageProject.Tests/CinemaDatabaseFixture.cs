using System.IO;
using Microsoft.Data.Sqlite;
using Xunit;

namespace CinemaManageProject.Tests;

/// <summary>
/// Fixture cô lập dữ liệu SQLite cho toàn bộ bộ test: lưu bản gốc cinema.db và khôi phục sạch trước mỗi test
/// để không làm thay đổi dữ liệu nguồn của dự án và tránh xung đột song song.
/// </summary>
public sealed class CinemaDatabaseFixture : IDisposable
{
    private readonly string _runtimeDbPath;
    private readonly string _seedBackupPath;

    public CinemaDatabaseFixture()
    {
        _runtimeDbPath = Path.Combine(AppContext.BaseDirectory, "Database", "cinema.db");
        _seedBackupPath = Path.Combine(AppContext.BaseDirectory, "Database", "cinema.seed.bak");

        if (!File.Exists(_runtimeDbPath))
        {
            throw new FileNotFoundException("Không tìm thấy tệp cinema.db trong thư mục chạy kiểm thử.", _runtimeDbPath);
        }

        SqliteConnection.ClearAllPools();
        File.Copy(_runtimeDbPath, _seedBackupPath, overwrite: true);
    }

    public void ResetDatabase()
    {
        SqliteConnection.ClearAllPools();
        File.Copy(_seedBackupPath, _runtimeDbPath, overwrite: true);
    }

    public void Dispose()
    {
        ResetDatabase();
        if (File.Exists(_seedBackupPath))
        {
            File.Delete(_seedBackupPath);
        }
    }
}

[CollectionDefinition("CinemaDatabase")]
public sealed class CinemaDatabaseCollection : ICollectionFixture<CinemaDatabaseFixture>;
