using System.Globalization;
using CinemaManageProject.Helpers;
using Microsoft.Data.Sqlite;

namespace CinemaManageProject.Data;

public sealed record ShowtimeMovieOption(
    int MovieId,
    string Title,
    int DurationMinutes,
    string Language,
    string Status)
{
    public string DisplayLabel => Status switch
    {
        "Ended" => $"{Title} ({DurationMinutes}p - Ngừng chiếu)",
        "ComingSoon" => $"{Title} ({DurationMinutes}p - Sắp chiếu)",
        _ => $"{Title} ({DurationMinutes}p)"
    };

    public override string ToString() => DisplayLabel;
}

public sealed record ShowtimeRoomOption(
    int RoomId,
    string Name,
    string RoomType,
    decimal BasePriceSurcharge,
    bool IsActive,
    int ActiveSeatCount)
{
    public string RoomTypeDisplay => FormatHelper.FormatRoomType(RoomType);
    public string DisplayLabel => IsActive
        ? $"{Name} ({ActiveSeatCount} ghế)"
        : $"{Name} (Tạm đóng)";

    public override string ToString() => DisplayLabel;
}

public sealed record AdminShowtimeItem(
    int ShowtimeId,
    int MovieId,
    string MovieTitle,
    int MovieDurationMinutes,
    int RoomId,
    string RoomName,
    decimal RoomSurcharge,
    string StartTimeSql,
    string EndTimeSql,
    decimal BaseTicketPrice,
    string Format,
    string Status,
    int ActiveTicketsCount,
    int TotalTicketsCount,
    int AvailableSeatsCount)
{
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
/// Truy vấn SQLite và kiểm tra ràng buộc nghiệp vụ cho bảng Showtimes.
/// </summary>
public static class ShowtimeData
{
    public const decimal StandardBaseTicketPrice = 80000m;

    public static List<ShowtimeMovieOption> LoadMovieOptions()
    {
        var result = new List<ShowtimeMovieOption>();
        using SqliteConnection conn = DatabaseHelper.OpenConnection();
        using SqliteCommand cmd = conn.CreateCommand();
        cmd.CommandText = """
            SELECT MovieId, Title, DurationMinutes, Language, Status
            FROM Movies
            ORDER BY CASE Status WHEN 'NowShowing' THEN 0 WHEN 'ComingSoon' THEN 1 ELSE 2 END,
                     Title COLLATE NOCASE ASC;
            """;

        using SqliteDataReader r = cmd.ExecuteReader();
        while (r.Read())
        {
            result.Add(new ShowtimeMovieOption(
                r.GetInt32(0),
                r.GetString(1),
                r.GetInt32(2),
                r.GetString(3),
                r.GetString(4)));
        }

        return result;
    }

    public static List<ShowtimeRoomOption> LoadRoomOptions()
    {
        var result = new List<ShowtimeRoomOption>();
        using SqliteConnection conn = DatabaseHelper.OpenConnection();
        using SqliteCommand cmd = conn.CreateCommand();
        cmd.CommandText = """
            SELECT r.RoomId, r.Name, r.RoomType, r.BasePriceSurcharge, r.IsActive,
                   (SELECT COUNT(*) FROM Seats s WHERE s.RoomId = r.RoomId AND s.IsActive = 1)
            FROM Rooms r
            ORDER BY r.IsActive DESC, r.RoomId ASC;
            """;

        using SqliteDataReader r = cmd.ExecuteReader();
        while (r.Read())
        {
            result.Add(new ShowtimeRoomOption(
                r.GetInt32(0),
                r.GetString(1),
                r.GetString(2),
                DatabaseHelper.ReadDecimal(r, 3),
                r.GetInt32(4) == 1,
                r.GetInt32(5)));
        }

        return result;
    }

    public static List<AdminShowtimeItem> LoadAllShowtimes()
    {
        var items = new List<AdminShowtimeItem>();
        using SqliteConnection conn = DatabaseHelper.OpenConnection();
        using SqliteCommand cmd = conn.CreateCommand();
        cmd.CommandText = """
            SELECT s.ShowtimeId,
                   s.MovieId,
                   m.Title,
                   m.DurationMinutes,
                   s.RoomId,
                   r.Name,
                   r.BasePriceSurcharge,
                   s.StartTime,
                   s.EndTime,
                   s.BaseTicketPrice,
                   s.Format,
                   s.Status,
                   (SELECT COUNT(*) FROM Tickets t WHERE t.ShowtimeId = s.ShowtimeId AND t.Status IN ('Booked', 'CheckedIn')),
                   (SELECT COUNT(*) FROM Tickets t WHERE t.ShowtimeId = s.ShowtimeId),
                   (SELECT COUNT(*) FROM Seats se WHERE se.RoomId = s.RoomId AND se.IsActive = 1)
            FROM Showtimes s
            INNER JOIN Movies m ON m.MovieId = s.MovieId
            INNER JOIN Rooms r ON r.RoomId = s.RoomId
            ORDER BY CASE WHEN s.Status IN ('OpenForSale', 'Scheduled') THEN 0 ELSE 1 END,
                     CASE WHEN s.Status IN ('OpenForSale', 'Scheduled') THEN s.StartTime END ASC,
                     s.StartTime DESC,
                     s.RoomId ASC;
            """;

        using SqliteDataReader r = cmd.ExecuteReader();
        while (r.Read())
        {
            items.Add(new AdminShowtimeItem(
                r.GetInt32(0),
                r.GetInt32(1),
                r.GetString(2),
                r.GetInt32(3),
                r.GetInt32(4),
                r.GetString(5),
                DatabaseHelper.ReadDecimal(r, 6),
                r.GetString(7),
                r.GetString(8),
                DatabaseHelper.ReadDecimal(r, 9),
                r.GetString(10),
                r.GetString(11),
                r.GetInt32(12),
                r.GetInt32(13),
                r.GetInt32(14)));
        }

        return items;
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
            using SqliteCommand update = conn.CreateCommand();
            update.Transaction = tx;
            update.CommandText = """
                UPDATE Showtimes
                SET MovieId = @mid,
                    RoomId = @rid,
                    StartTime = @start,
                    EndTime = @end,
                    BaseTicketPrice = @price,
                    Format = @fmt,
                    Status = @status
                WHERE ShowtimeId = @id;
                """;
            BindParameters(update, input);
            update.Parameters.AddWithValue("@id", savedId);
            update.ExecuteNonQuery();
        }
        else
        {
            using SqliteCommand insert = conn.CreateCommand();
            insert.Transaction = tx;
            insert.CommandText = """
                INSERT INTO Showtimes (MovieId, RoomId, StartTime, EndTime, BaseTicketPrice, Format, Status, CreatedByUserId)
                VALUES (@mid, @rid, @start, @end, @price, @fmt, @status, @uid);
                SELECT last_insert_rowid();
                """;
            BindParameters(insert, input);
            insert.Parameters.AddWithValue("@uid", input.CreatedByUserId);
            savedId = Convert.ToInt32(insert.ExecuteScalar(), CultureInfo.InvariantCulture);
        }

        tx.Commit();
        return new ShowtimeSaveResult(true, savedId, null);
    }

    public static int CountShowtimeTickets(int showtimeId)
    {
        using SqliteConnection conn = DatabaseHelper.OpenConnection();
        using SqliteCommand cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT COUNT(*) FROM Tickets WHERE ShowtimeId = @id;";
        cmd.Parameters.AddWithValue("@id", showtimeId);
        return Convert.ToInt32(cmd.ExecuteScalar(), CultureInfo.InvariantCulture);
    }

    public static void DeleteShowtime(int showtimeId)
    {
        using SqliteConnection conn = DatabaseHelper.OpenConnection();
        using SqliteCommand cmd = conn.CreateCommand();
        cmd.CommandText = "DELETE FROM Showtimes WHERE ShowtimeId = @id;";
        cmd.Parameters.AddWithValue("@id", showtimeId);
        cmd.ExecuteNonQuery();
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
        using SqliteCommand cmd = conn.CreateCommand();
        cmd.Transaction = tx;
        cmd.CommandText = """
            SELECT MovieId, RoomId, StartTime, BaseTicketPrice,
                   (SELECT COUNT(*) FROM Tickets t WHERE t.ShowtimeId = s.ShowtimeId AND t.Status IN ('Booked', 'CheckedIn'))
            FROM Showtimes s
            WHERE s.ShowtimeId = @id;
            """;
        cmd.Parameters.AddWithValue("@id", showtimeId);

        using SqliteDataReader r = cmd.ExecuteReader();
        if (!r.Read())
        {
            return "Không tìm thấy suất chiếu cần cập nhật.";
        }

        int activeTickets = r.GetInt32(4);
        if (activeTickets <= 0)
        {
            return null;
        }

        int oldMovieId = r.GetInt32(0);
        int oldRoomId = r.GetInt32(1);
        string oldStart = r.GetString(2);
        decimal oldPrice = DatabaseHelper.ReadDecimal(r, 3);

        if (oldMovieId != input.MovieId
            || oldRoomId != input.RoomId
            || !string.Equals(oldStart, input.StartTimeSql, StringComparison.Ordinal)
            || oldPrice != input.BaseTicketPrice)
        {
            return $"Suất chiếu đang có {activeTickets} vé hiệu lực, không thể đổi phim, phòng, giờ chiếu hoặc giá vé (chỉ được cập nhật trạng thái).";
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
        using SqliteCommand cmd = conn.CreateCommand();
        cmd.Transaction = tx;
        cmd.CommandText = """
            SELECT m.Title, r.Name, s.StartTime, s.EndTime
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
        cmd.Parameters.AddWithValue("@rid", roomId);
        cmd.Parameters.AddWithValue("@sid", excludeShowtimeId.HasValue ? excludeShowtimeId.Value : DBNull.Value);
        cmd.Parameters.AddWithValue("@start", startSql);
        cmd.Parameters.AddWithValue("@end", endSql);

        using SqliteDataReader r = cmd.ExecuteReader();
        if (!r.Read())
        {
            return null;
        }

        string movieTitle = r.GetString(0);
        string roomName = r.GetString(1);
        string startDisplay = FormatHelper.FormatSqlDateTime(r.GetString(2), "HH:mm");
        string endDisplay = FormatHelper.FormatSqlDateTime(r.GetString(3), "HH:mm dd/MM/yyyy");
        return $"{roomName} bị trùng lịch với suất \"{movieTitle}\" ({startDisplay} – {endDisplay}, đã tính +15 phút dọn phòng).";
    }

    private static void BindParameters(SqliteCommand cmd, ShowtimeSaveInput input)
    {
        cmd.Parameters.AddWithValue("@mid", input.MovieId);
        cmd.Parameters.AddWithValue("@rid", input.RoomId);
        cmd.Parameters.AddWithValue("@start", input.StartTimeSql);
        cmd.Parameters.AddWithValue("@end", input.EndTimeSql);
        cmd.Parameters.AddWithValue("@price", input.BaseTicketPrice);
        cmd.Parameters.AddWithValue("@fmt", input.Format);
        cmd.Parameters.AddWithValue("@status", input.Status);
    }
}
