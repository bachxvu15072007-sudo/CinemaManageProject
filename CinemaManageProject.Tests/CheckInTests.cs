using CinemaManageProject.Data;
using CinemaManageProject.Helpers;
using Dapper;
using Microsoft.Data.Sqlite;
using Xunit;

namespace CinemaManageProject.Tests;

[Collection("CinemaDatabase")]
public sealed class CheckInTests
{
    private readonly CinemaDatabaseFixture _db;

    public CheckInTests(CinemaDatabaseFixture db)
    {
        _db = db;
        _db.ResetDatabase();
    }

    [Fact]
    public void LookupTicketByCode_CaseInsensitiveAndTrimmed_FindsValidTicket()
    {
        CheckInTicketRow? row = CheckInData.LookupTicketByCode("  ve-khach101  ");

        Assert.NotNull(row);
        Assert.Equal("VE-KHACH101", row!.TicketCode);
        Assert.True(row.IsPending);
        Assert.True(row.CanCheckIn);
        Assert.Equal("Valid", row.ValidationState);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("VE-KHONG-TON-TAI")]
    public void LookupTicketByCode_BlankOrUnknownCode_ReturnsNull(string code)
    {
        Assert.Null(CheckInData.LookupTicketByCode(code));
    }

    [Fact]
    public void LoadAllTicketsForCheckIn_OrdersPendingTicketsFirst()
    {
        List<CheckInTicketRow> rows = CheckInData.LoadAllTicketsForCheckIn();

        Assert.Equal(160, rows.Count);
        Assert.True(rows[0].IsPending);
    }

    [Fact]
    public void ConfirmCheckIn_ValidBookedTicket_UpdatesStatusAndBlocksSecondCheckIn()
    {
        CheckInTicketRow ticket = CheckInData.LookupTicketByCode("VE-KHACH101")!;

        CheckInActionResult first = CheckInData.ConfirmCheckIn(ticket.TicketId, staffUserId: 2);

        Assert.True(first.Succeeded);
        Assert.NotNull(first.UpdatedTicket);
        Assert.True(first.UpdatedTicket!.IsCheckedIn);
        Assert.False(first.UpdatedTicket.CanCheckIn);
        Assert.Equal("AlreadyCheckedIn", first.UpdatedTicket.ValidationState);
        Assert.Equal("Nguyễn Minh Khôi", first.UpdatedTicket.CheckedInByName);
        Assert.False(string.IsNullOrWhiteSpace(first.UpdatedTicket.CheckedInAtSql));

        // Soát lại lần 2 phải bị chặn
        CheckInActionResult second = CheckInData.ConfirmCheckIn(ticket.TicketId, staffUserId: 2);
        Assert.False(second.Succeeded);
        Assert.Contains("đã được check-in", second.ErrorMessage ?? string.Empty, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void ConfirmCheckIn_AlreadyCheckedInOrCancelledTicket_IsRejected()
    {
        CheckInTicketRow checkedTicket = CheckInData.LookupTicketByCode("VE-HIST003A")!;
        CheckInActionResult checkedRes = CheckInData.ConfirmCheckIn(checkedTicket.TicketId, staffUserId: 2);
        Assert.False(checkedRes.Succeeded);

        CheckInTicketRow cancelledTicket = CheckInData.LookupTicketByCode("VE-CANC028A")!;
        CheckInActionResult cancelledRes = CheckInData.ConfirmCheckIn(cancelledTicket.TicketId, staffUserId: 2);
        Assert.False(cancelledRes.Succeeded);
        Assert.Contains("hủy", cancelledRes.ErrorMessage ?? string.Empty, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void ConfirmCheckIn_TicketOfCancelledShowtime_IsRejected()
    {
        CheckInTicketRow ticket = CheckInData.LookupTicketByCode("VE-KHACH101")!;

        using (SqliteConnection conn = DatabaseHelper.OpenConnection())
        {
            conn.Execute("UPDATE Showtimes SET Status = 'Cancelled' WHERE ShowtimeId = @sid;", new { sid = ticket.ShowtimeId });
        }

        CheckInActionResult result = CheckInData.ConfirmCheckIn(ticket.TicketId, staffUserId: 2);

        Assert.False(result.Succeeded);
        Assert.Contains("suất chiếu", result.ErrorMessage ?? string.Empty, StringComparison.OrdinalIgnoreCase);
    }

    [Theory]
    [InlineData(1, 0)]
    [InlineData(1, -5)]
    [InlineData(999999, 2)]
    public void ConfirmCheckIn_InvalidStaffOrUnknownTicket_IsRejected(int ticketId, int staffUserId)
    {
        CheckInActionResult result = CheckInData.ConfirmCheckIn(ticketId, staffUserId);

        Assert.False(result.Succeeded);
        Assert.False(string.IsNullOrWhiteSpace(result.ErrorMessage));
    }
}
