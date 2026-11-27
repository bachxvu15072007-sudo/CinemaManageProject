using CinemaManageProject.Data;
using Microsoft.Data.Sqlite;
using Xunit;

namespace CinemaManageProject.Tests;

[Collection("CinemaDatabase")]
public sealed class ShowtimeTests
{
    private readonly CinemaDatabaseFixture _db;

    public ShowtimeTests(CinemaDatabaseFixture db)
    {
        _db = db;
        _db.ResetDatabase();
    }

    [Fact]
    public void SaveShowtime_CreateUpdateAndDeleteWithoutTickets_Succeeds()
    {
        var input = new ShowtimeSaveInput(
            MovieId: 1,
            RoomId: 2,
            StartTimeSql: "2027-02-10 10:00:00",
            EndTimeSql: "2027-02-10 13:01:00",
            BaseTicketPrice: ShowtimeData.StandardBaseTicketPrice,
            Format: "2D Phụ đề",
            Status: "Scheduled",
            CreatedByUserId: 1);

        ShowtimeSaveResult created = ShowtimeData.SaveShowtime(null, input);
        Assert.True(created.Succeeded);
        Assert.True(created.ShowtimeId > 0);

        ShowtimeSaveResult updated = ShowtimeData.SaveShowtime(
            created.ShowtimeId,
            input with { Status = "OpenForSale", Format = "2D Lồng tiếng" });
        Assert.True(updated.Succeeded);

        AdminShowtimeItem saved = ShowtimeData.LoadAllShowtimes().Single(s => s.ShowtimeId == created.ShowtimeId);
        Assert.Equal("OpenForSale", saved.Status);
        Assert.Equal("2D Lồng tiếng", saved.Format);

        Assert.Equal(0, ShowtimeData.CountShowtimeTickets(created.ShowtimeId));
        ShowtimeData.DeleteShowtime(created.ShowtimeId);
        Assert.DoesNotContain(ShowtimeData.LoadAllShowtimes(), s => s.ShowtimeId == created.ShowtimeId);
    }

    [Fact]
    public void SaveShowtime_OverlappingSameRoom_IsRejected()
    {
        // Tạo suất gốc 14:00 - 16:30 tại Phòng 2 ngày 2027-02-15
        ShowtimeSaveResult baseRes = ShowtimeData.SaveShowtime(null, new ShowtimeSaveInput(
            MovieId: 2, RoomId: 2,
            StartTimeSql: "2027-02-15 14:00:00", EndTimeSql: "2027-02-15 16:30:00",
            BaseTicketPrice: 80000m, Format: "2D Tiếng Việt", Status: "OpenForSale", CreatedByUserId: 1));
        Assert.True(baseRes.Succeeded);

        // 1. Trùng phần đầu (13:00 - 14:30)
        ShowtimeSaveResult overlapStart = ShowtimeData.SaveShowtime(null, new ShowtimeSaveInput(
            MovieId: 3, RoomId: 2,
            StartTimeSql: "2027-02-15 13:00:00", EndTimeSql: "2027-02-15 14:30:00",
            BaseTicketPrice: 80000m, Format: "2D Lồng tiếng", Status: "OpenForSale", CreatedByUserId: 1));
        Assert.False(overlapStart.Succeeded);
        Assert.Contains("trùng lịch", overlapStart.ErrorMessage ?? string.Empty, StringComparison.OrdinalIgnoreCase);

        // 2. Trùng phần cuối (16:00 - 18:00)
        ShowtimeSaveResult overlapEnd = ShowtimeData.SaveShowtime(null, new ShowtimeSaveInput(
            MovieId: 3, RoomId: 2,
            StartTimeSql: "2027-02-15 16:00:00", EndTimeSql: "2027-02-15 18:00:00",
            BaseTicketPrice: 80000m, Format: "2D Lồng tiếng", Status: "OpenForSale", CreatedByUserId: 1));
        Assert.False(overlapEnd.Succeeded);

        // 3. Nằm lọt bên trong (14:30 - 15:30)
        ShowtimeSaveResult overlapInside = ShowtimeData.SaveShowtime(null, new ShowtimeSaveInput(
            MovieId: 3, RoomId: 2,
            StartTimeSql: "2027-02-15 14:30:00", EndTimeSql: "2027-02-15 15:30:00",
            BaseTicketPrice: 80000m, Format: "2D Lồng tiếng", Status: "OpenForSale", CreatedByUserId: 1));
        Assert.False(overlapInside.Succeeded);
    }

    [Fact]
    public void SaveShowtime_AdjacentBoundaryOrDifferentRoomOrCancelledSlot_IsAllowed()
    {
        ShowtimeSaveResult baseRes = ShowtimeData.SaveShowtime(null, new ShowtimeSaveInput(
            MovieId: 2, RoomId: 2,
            StartTimeSql: "2027-02-16 14:00:00", EndTimeSql: "2027-02-16 16:30:00",
            BaseTicketPrice: 80000m, Format: "2D Tiếng Việt", Status: "OpenForSale", CreatedByUserId: 1));
        Assert.True(baseRes.Succeeded);

        // Sát biên (bắt đầu đúng 16:30:00 ngay sau EndTime đã gồm +15p dọn phòng) -> Hợp lệ
        ShowtimeSaveResult adjacent = ShowtimeData.SaveShowtime(null, new ShowtimeSaveInput(
            MovieId: 3, RoomId: 2,
            StartTimeSql: "2027-02-16 16:30:00", EndTimeSql: "2027-02-16 18:19:00",
            BaseTicketPrice: 80000m, Format: "2D Lồng tiếng", Status: "OpenForSale", CreatedByUserId: 1));
        Assert.True(adjacent.Succeeded);

        // Cùng khung giờ 14:00 - 16:30 nhưng khác phòng (Phòng 3) -> Hợp lệ
        ShowtimeSaveResult otherRoom = ShowtimeData.SaveShowtime(null, new ShowtimeSaveInput(
            MovieId: 2, RoomId: 3,
            StartTimeSql: "2027-02-16 14:00:00", EndTimeSql: "2027-02-16 16:30:00",
            BaseTicketPrice: 80000m, Format: "3D Tiếng Việt", Status: "OpenForSale", CreatedByUserId: 1));
        Assert.True(otherRoom.Succeeded);

        // Hủy suất gốc ở Phòng 2 rồi tạo suất thay thế đúng khung giờ đó -> Hợp lệ
        ShowtimeSaveResult cancelBase = ShowtimeData.SaveShowtime(baseRes.ShowtimeId, new ShowtimeSaveInput(
            MovieId: 2, RoomId: 2,
            StartTimeSql: "2027-02-16 14:00:00", EndTimeSql: "2027-02-16 16:30:00",
            BaseTicketPrice: 80000m, Format: "2D Tiếng Việt", Status: "Cancelled", CreatedByUserId: 1));
        Assert.True(cancelBase.Succeeded);

        ShowtimeSaveResult replacement = ShowtimeData.SaveShowtime(null, new ShowtimeSaveInput(
            MovieId: 4, RoomId: 2,
            StartTimeSql: "2027-02-16 14:00:00", EndTimeSql: "2027-02-16 16:29:00",
            BaseTicketPrice: 80000m, Format: "2D Phụ đề", Status: "OpenForSale", CreatedByUserId: 1));
        Assert.True(replacement.Succeeded);
    }

    [Fact]
    public void SaveShowtime_WithActiveTickets_BlocksCoreFieldChangesButAllowsStatusUpdate()
    {
        AdminShowtimeItem showtime7 = ShowtimeData.LoadAllShowtimes().Single(s => s.ShowtimeId == 7);
        Assert.True(showtime7.ActiveTicketsCount > 0);

        // Đổi giá vé của suất đã có vé hiệu lực -> bị khóa
        ShowtimeSaveResult priceChange = ShowtimeData.SaveShowtime(7, new ShowtimeSaveInput(
            showtime7.MovieId, showtime7.RoomId, showtime7.StartTimeSql, showtime7.EndTimeSql,
            BaseTicketPrice: 95000m, showtime7.Format, showtime7.Status, CreatedByUserId: 1));
        Assert.False(priceChange.Succeeded);
        Assert.Contains("vé hiệu lực", priceChange.ErrorMessage ?? string.Empty, StringComparison.OrdinalIgnoreCase);

        // Đổi phòng của suất đã có vé -> bị khóa
        ShowtimeSaveResult roomChange = ShowtimeData.SaveShowtime(7, new ShowtimeSaveInput(
            showtime7.MovieId, RoomId: 2, showtime7.StartTimeSql, showtime7.EndTimeSql,
            showtime7.BaseTicketPrice, showtime7.Format, showtime7.Status, CreatedByUserId: 1));
        Assert.False(roomChange.Succeeded);

        // Giữ nguyên thông số cốt lõi, chỉ đổi trạng thái -> cho phép
        ShowtimeSaveResult statusOnly = ShowtimeData.SaveShowtime(7, new ShowtimeSaveInput(
            showtime7.MovieId, showtime7.RoomId, showtime7.StartTimeSql, showtime7.EndTimeSql,
            showtime7.BaseTicketPrice, showtime7.Format, Status: "Scheduled", CreatedByUserId: 1));
        Assert.True(statusOnly.Succeeded);
    }

    [Fact]
    public void DeleteShowtime_WithExistingTickets_IsBlockedByForeignKeyRestrict()
    {
        Assert.True(ShowtimeData.CountShowtimeTickets(showtimeId: 7) > 0);
        Assert.Throws<SqliteException>(() => ShowtimeData.DeleteShowtime(showtimeId: 7));
    }

    [Theory]
    [InlineData("IMAX", "Phụ đề Tiếng Việt", "IMAX Phụ đề")]
    [InlineData("Standard3D", "Lồng tiếng Việt", "3D Lồng tiếng")]
    [InlineData("Standard2D", "Tiếng Việt", "2D Tiếng Việt")]
    [InlineData("VIP", "Phụ đề Anh - Việt", "VIP Phụ đề")]
    public void SuggestFormat_MapsRoomTypeAndLanguageAccurately(string roomType, string language, string expected)
    {
        Assert.Equal(expected, ShowtimeData.SuggestFormat(roomType, language));
    }
}
