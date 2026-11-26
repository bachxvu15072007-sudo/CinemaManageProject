using CinemaManageProject.Helpers;
using Dapper;
using Microsoft.Data.Sqlite;

namespace CinemaManageProject.Data;

public sealed record ShowtimeMovieOption
{
    public int MovieId { get; init; }
    public string Title { get; init; } = string.Empty;
    public int DurationMinutes { get; init; }
    public string Language { get; init; } = string.Empty;
    public string Status { get; init; } = string.Empty;

    public string DisplayLabel => Status switch
    {
        "Ended" => $"{Title} ({DurationMinutes}p - Ngừng chiếu)",
        "ComingSoon" => $"{Title} ({DurationMinutes}p - Sắp chiếu)",
        _ => $"{Title} ({DurationMinutes}p)"
    };

    public override string ToString() => DisplayLabel;
}

public sealed record ShowtimeRoomOption
{
    public int RoomId { get; init; }
    public string Name { get; init; } = string.Empty;
    public string RoomType { get; init; } = string.Empty;
    public decimal BasePriceSurcharge { get; init; }
    public bool IsActive { get; init; }
    public int ActiveSeatCount { get; init; }

    public string RoomTypeDisplay => FormatHelper.FormatRoomType(RoomType);
    public string DisplayLabel => IsActive
        ? $"{Name} ({ActiveSeatCount} ghế)"
        : $"{Name} (Tạm đóng)";

    public override string ToString() => DisplayLabel;
}

public sealed record AdminShowtimeItem
{
    public int ShowtimeId { get; init; }
    public int MovieId { get; init; }
    public string MovieTitle { get; init; } = string.Empty;
    public int MovieDurationMinutes { get; init; }
    public int RoomId { get; init; }
    public string RoomName { get; init; } = string.Empty;
    public decimal RoomSurcharge { get; init; }
    public string StartTimeSql { get; init; } = string.Empty;
    public string EndTimeSql { get; init; } = string.Empty;
    public decimal BaseTicketPrice { get; init; }
    public string Format { get; init; } = string.Empty;
    public string Status { get; init; } = string.Empty;
    public int ActiveTicketsCount { get; init; }
    public int TotalTicketsCount { get; init; }
    public int AvailableSeatsCount { get; init; }

    public string ShowDateSql => StartTimeSql.Length >= 10 ? StartTimeSql[..10] : StartTimeSql;
    public string ShowDateDisplay => FormatHelper.FormatSqlDateTime(StartTimeSql, "dd/MM/yyyy");
    public string TimeRangeDisplay =>
        $"{FormatHelper.FormatSqlDateTime(StartTimeSql, "HH:mm")} - {FormatHelper.FormatSqlDateTime(EndTimeSql, "HH:mm")}";
    public string BasePriceText => FormatHelper.FormatVnd(BaseTicketPrice);
    public decimal StandardSeatPrice => BaseTicketPrice + RoomSurcharge;
    public string StandardSeatPriceText => FormatHelper.FormatVnd(StandardSeatPrice);
    public string OccupancyDisplay => $"{ActiveTicketsCount}/{AvailableSeatsCount}";
    public string StatusDisplay => FormatHelper.FormatStatusLabel(Status);
}

public sealed record ShowtimeSaveInput(
    int MovieId,
    int RoomId,
    string StartTimeSql,
    string EndTimeSql,
    decimal BaseTicketPrice,
    string Format,
    string Status,
    int CreatedByUserId);

public sealed record ShowtimeSaveResult(bool Succeeded, int ShowtimeId, string? ErrorMessage);

/// <summary>
/// Truy vấn SQLite bằng Dapper và kiểm tra ràng buộc nghiệp vụ cho bảng Showtimes.
/// </summary>
public static class ShowtimeData
{
    public const decimal StandardBaseTicketPrice = 80000m;

    private sealed class ShowtimeLockRow
    {
        public int MovieId { get; init; }
        public int RoomId { get; init; }
        public string StartTime { get; init; } = string.Empty;
        public decimal BaseTicketPrice { get; init; }
        public int ActiveTickets { get; init; }
    }

    private sealed class RoomConflictRow
    {
        public string MovieTitle { get; init; } = string.Empty;
        public string RoomName { get; init; } = string.Empty;
        public string StartTime { get; init; } = string.Empty;
        public string EndTime { get; init; } = string.Empty;
    }

    public static List<ShowtimeMovieOption> LoadMovieOptions()
    {
        using SqliteConnection conn = DatabaseHelper.OpenConnection();
        const string sql = """
            SELECT MovieId, Title, DurationMinutes, Language, Status
            FROM Movies
            ORDER BY CASE Status WHEN 'NowShowing' THEN 0 WHEN 'ComingSoon' THEN 1 ELSE 2 END,
                     Title COLLATE NOCASE ASC;
            """;
        return conn.Query<ShowtimeMovieOption>(sql).AsList();
    }

    public static List<ShowtimeRoomOption> LoadRoomOptions()
    {
        using SqliteConnection conn = DatabaseHelper.OpenConnection();
        const string sql = """
            SELECT r.RoomId, r.Name, r.RoomType, r.BasePriceSurcharge, r.IsActive,
                   (SELECT COUNT(*) FROM Seats s WHERE s.RoomId = r.RoomId AND s.IsActive = 1) AS ActiveSeatCount
            FROM Rooms r
            ORDER BY r.IsActive DESC, r.RoomId ASC;
            """;
        return conn.Query<ShowtimeRoomOption>(sql).AsList();
    }

    public static List<AdminShowtimeItem> LoadAllShowtimes()
    {
        using SqliteConnection conn = DatabaseHelper.OpenConnection();
        const string sql = """
            SELECT s.ShowtimeId,
                   s.MovieId,
                   m.Title AS MovieTitle,
                   m.DurationMinutes AS MovieDurationMinutes,
                   s.RoomId,
                   r.Name AS RoomName,
                   r.BasePriceSurcharge AS RoomSurcharge,
                   s.StartTime AS StartTimeSql,
                   s.EndTime AS EndTimeSql,
                   s.BaseTicketPrice,
                   s.Format,
                   s.Status,
                   (SELECT COUNT(*) FROM Tickets t WHERE t.ShowtimeId = s.ShowtimeId AND t.Status IN ('Booked', 'CheckedIn')) AS ActiveTicketsCount,
                   (SELECT COUNT(*) FROM Tickets t WHERE t.ShowtimeId = s.ShowtimeId) AS TotalTicketsCount,
                   (SELECT COUNT(*) FROM Seats se WHERE se.RoomId = s.RoomId AND se.IsActive = 1) AS AvailableSeatsCount
            FROM Showtimes s
            INNER JOIN Movies m ON m.MovieId = s.MovieId
            INNER JOIN Rooms r ON r.RoomId = s.RoomId
            ORDER BY CASE WHEN s.Status IN ('OpenForSale', 'Scheduled') THEN 0 ELSE 1 END,
                     CASE WHEN s.Status IN ('OpenForSale', 'Scheduled') THEN s.StartTime END ASC,
                     s.StartTime DESC,
                     s.RoomId ASC;
            """;
        return conn.Query<AdminShowtimeItem>(sql).AsList();
    }

    public static ShowtimeSaveResult SaveShowtime(int? showtimeId, ShowtimeSaveInput input)
    {
        using SqliteConnection conn = DatabaseHelper.OpenConnection();
        using SqliteTransaction tx = conn.BeginTransaction();

        if (showtimeId.HasValue)
        {
            string? coreGuardError = CheckActiveTicketCoreLock(conn, tx, showtimeId.Value, input);
            if (coreGuardError is not null)
            {
                return new ShowtimeSaveResult(false, showtimeId.Value, coreGuardError);
            }
        }

        if (input.Status != "Cancelled")
        {
            string? conflictError = FindRoomConflictError(conn, tx, showtimeId, input.RoomId, input.StartTimeSql, input.EndTimeSql);
            if (conflictError is not null)
            {
                return new ShowtimeSaveResult(false, showtimeId ?? 0, conflictError);
            }
        }

        int savedId;
        if (showtimeId.HasValue)
        {
            savedId = showtimeId.Value;
            conn.Execute(
                """
                UPDATE Showtimes
                SET MovieId = @MovieId,
                    RoomId = @RoomId,
                    StartTime = @StartTimeSql,
                    EndTime = @EndTimeSql,
                    BaseTicketPrice = @BaseTicketPrice,
                    Format = @Format,
                    Status = @Status
                WHERE ShowtimeId = @ShowtimeId;
                """,
                new
                {
                    ShowtimeId = savedId,
                    input.MovieId,
                    input.RoomId,
                    input.StartTimeSql,
                    input.EndTimeSql,
                    input.BaseTicketPrice,
                    input.Format,
                    input.Status
                },
                tx);
        }
        else
        {
            savedId = conn.ExecuteScalar<int>(
                """
                INSERT INTO Showtimes (MovieId, RoomId, StartTime, EndTime, BaseTicketPrice, Format, Status, CreatedByUserId)
                VALUES (@MovieId, @RoomId, @StartTimeSql, @EndTimeSql, @BaseTicketPrice, @Format, @Status, @CreatedByUserId);
                SELECT last_insert_rowid();
                """,
                input,
                tx);
        }

        tx.Commit();
        return new ShowtimeSaveResult(true, savedId, null);
    }

    public static int CountShowtimeTickets(int showtimeId)
    {
        using SqliteConnection conn = DatabaseHelper.OpenConnection();
        return conn.ExecuteScalar<int>("SELECT COUNT(*) FROM Tickets WHERE ShowtimeId = @id;", new { id = showtimeId });
    }

    public static void DeleteShowtime(int showtimeId)
    {
        using SqliteConnection conn = DatabaseHelper.OpenConnection();
        conn.Execute("DELETE FROM Showtimes WHERE ShowtimeId = @id;", new { id = showtimeId });
    }

    public static string SuggestFormat(string roomType, string movieLanguage)
    {
        string prefix = roomType switch
        {
            "IMAX" => "IMAX",
            "Standard3D" => "3D",
            "VIP" => "VIP",
            _ => "2D"
        };

        string suffix = movieLanguage.Contains("Lồng tiếng", StringComparison.OrdinalIgnoreCase)
            ? "Lồng tiếng"
            : movieLanguage.Equals("Tiếng Việt", StringComparison.OrdinalIgnoreCase)
                ? "Tiếng Việt"
                : "Phụ đề";

        return $"{prefix} {suffix}";
    }

    private static string? CheckActiveTicketCoreLock(
        SqliteConnection conn,
        SqliteTransaction tx,
        int showtimeId,
        ShowtimeSaveInput input)
    {
        const string sql = """
            SELECT MovieId, RoomId, StartTime, BaseTicketPrice,
                   (SELECT COUNT(*) FROM Tickets t WHERE t.ShowtimeId = s.ShowtimeId AND t.Status IN ('Booked', 'CheckedIn')) AS ActiveTickets
            FROM Showtimes s
            WHERE s.ShowtimeId = @id;
            """;

        ShowtimeLockRow? existing = conn.QueryFirstOrDefault<ShowtimeLockRow>(sql, new { id = showtimeId }, tx);
        if (existing is null)
        {
            return "Không tìm thấy suất chiếu cần cập nhật.";
        }

        if (existing.ActiveTickets <= 0)
        {
            return null;
        }

        if (existing.MovieId != input.MovieId
            || existing.RoomId != input.RoomId
            || !string.Equals(existing.StartTime, input.StartTimeSql, StringComparison.Ordinal)
            || existing.BaseTicketPrice != input.BaseTicketPrice)
        {
            return $"Suất chiếu đang có {existing.ActiveTickets} vé hiệu lực, không thể đổi phim, phòng, giờ chiếu hoặc giá vé (chỉ được cập nhật trạng thái).";
        }

        return null;
    }

    private static string? FindRoomConflictError(
        SqliteConnection conn,
        SqliteTransaction tx,
        int? excludeShowtimeId,
        int roomId,
        string startSql,
        string endSql)
    {
        const string sql = """
            SELECT m.Title AS MovieTitle, r.Name AS RoomName, s.StartTime, s.EndTime
            FROM Showtimes s
            INNER JOIN Movies m ON m.MovieId = s.MovieId
            INNER JOIN Rooms r ON r.RoomId = s.RoomId
            WHERE s.RoomId = @rid
              AND s.Status != 'Cancelled'
              AND (@sid IS NULL OR s.ShowtimeId != @sid)
              AND s.StartTime < @end
              AND s.EndTime > @start
            ORDER BY s.StartTime ASC
            LIMIT 1;
            """;

        RoomConflictRow? conflict = conn.QueryFirstOrDefault<RoomConflictRow>(
            sql,
            new { rid = roomId, sid = excludeShowtimeId, start = startSql, end = endSql },
            tx);

        if (conflict is null)
        {
            return null;
        }

        string startDisplay = FormatHelper.FormatSqlDateTime(conflict.StartTime, "HH:mm");
        string endDisplay = FormatHelper.FormatSqlDateTime(conflict.EndTime, "HH:mm dd/MM/yyyy");
        return $"{conflict.RoomName} bị trùng lịch với suất \"{conflict.MovieTitle}\" ({startDisplay} – {endDisplay}, đã tính +15 phút dọn phòng).";
    }
}
