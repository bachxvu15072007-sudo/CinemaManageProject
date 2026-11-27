using System.IO;
using Microsoft.Data.Sqlite;
using Xunit;

namespace CinemaManageProject.Tests;

/// <summary>
/// Fixture cô lập dữ liệu SQLite cho toàn bộ bộ test: khôi phục sạch cinema.db từ bản gốc bất biến cinema.seed.db
/// trước mỗi test để không làm thay đổi dữ liệu nguồn của dự án và đảm bảo mọi lần chạy lặp lại đều ổn định.
/// </summary>
public sealed class CinemaDatabaseFixture : IDisposable
{
    private readonly string _runtimeDbPath;
    private readonly string _seedDbPath;

    public CinemaDatabaseFixture()
    {
        var dbDirectory = Path.Combine(AppContext.BaseDirectory, "Database");
        Directory.CreateDirectory(dbDirectory);

        _runtimeDbPath = Path.Combine(dbDirectory, "cinema.db");
        _seedDbPath = Path.Combine(dbDirectory, "cinema.seed.db");

        if (!File.Exists(_seedDbPath))
        {
            throw new FileNotFoundException("Không tìm thấy bản gốc bất biến cinema.seed.db trong thư mục chạy kiểm thử.", _seedDbPath);
        }

        ResetDatabase();
    }

    public void ResetDatabase()
    {
        SqliteConnection.ClearAllPools();

        foreach (var suffix in new[] { "-wal", "-shm", "-journal" })
        {
            var sidecarPath = _runtimeDbPath + suffix;
            if (File.Exists(sidecarPath))
            {
                File.Delete(sidecarPath);
            }
        }

        File.Copy(_seedDbPath, _runtimeDbPath, overwrite: true);
    }

    public void Dispose()
    {
        ResetDatabase();
    }
}

[CollectionDefinition("CinemaDatabase")]
public sealed class CinemaDatabaseCollection : ICollectionFixture<CinemaDatabaseFixture>;
