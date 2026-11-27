using CinemaManageProject.Data;
using CinemaManageProject.Helpers;
using Dapper;
using Microsoft.Data.Sqlite;
using Xunit;

namespace CinemaManageProject.Tests;

[Collection("CinemaDatabase")]
public sealed class BookingTests
{
    private readonly CinemaDatabaseFixture _db;

    public BookingTests(CinemaDatabaseFixture db)
    {
        _db = db;
        _db.ResetDatabase();
    }

    [Fact]
    public void CreateOrder_CounterCashWithPoints_CreatesOrderTicketsAndUpdatesCustomer()
    {
        // Suất 7: Phòng 1 - 2D (Base=80.000, Surcharge=0) -> Standard=80.000, VIP=100.000
        List<ShowtimeSeatRow> seats = BookingData.LoadShowtimeSeatMap(showtimeId: 7);
        ShowtimeSeatRow freeStd = seats.First(s => s.IsActive && !s.IsBooked && s.SeatType == "Standard");
        ShowtimeSeatRow freeVip = seats.First(s => s.IsActive && !s.IsBooked && s.SeatType == "VIP");
        BookingCustomerSummary khachBefore = BookingData.FindCustomerByPhone("0901000001")!;

        BookingCreateResult result = BookingData.CreateOrder(new BookingCreateInput(
            ShowtimeId: 7,
            SeatIds: [freeStd.SeatId, freeVip.SeatId],
            StaffUserId: 2,
            CustomerId: khachBefore.CustomerId,
            BookingChannel: "Counter",
            PointsUsed: 20,
            PaymentMethod: "Cash",
            CashReceived: 200000m,
            Note: "Unit test đặt vé tại quầy"));

        Assert.True(result.Succeeded);
        Assert.True(result.OrderId > 0);
        Assert.StartsWith("HD-", result.OrderCode);
        Assert.Equal(2, result.TicketCodes.Count);
        Assert.Equal(180000m, result.SubTotalAmount);
        Assert.Equal(20000m, result.DiscountAmount);
        Assert.Equal(160000m, result.TotalAmount);
        Assert.Equal(40000m, result.ChangeAmount);
        Assert.Equal(16, result.EarnedPoints);

        BookingCustomerSummary khachAfter = BookingData.FindCustomerById(khachBefore.CustomerId)!;
        Assert.Equal(khachBefore.LoyaltyPoints - 20 + 16, khachAfter.LoyaltyPoints);
        Assert.Equal(khachBefore.TotalSpent + 160000m, khachAfter.TotalSpent);

        // Kiểm tra ghế vừa đặt đã chuyển sang IsBooked
        List<ShowtimeSeatRow> seatsAfter = BookingData.LoadShowtimeSeatMap(showtimeId: 7);
        Assert.True(seatsAfter.Single(s => s.SeatId == freeStd.SeatId).IsBooked);
        Assert.True(seatsAfter.Single(s => s.SeatId == freeVip.SeatId).IsBooked);
    }

    [Fact]
    public void CreateOrder_SelfServiceOnline_CreatesOrderWithNullStaff()
    {
        List<ShowtimeSeatRow> seats = BookingData.LoadShowtimeSeatMap(showtimeId: 7);
        ShowtimeSeatRow freeSeat = seats.First(s => s.IsActive && !s.IsBooked && s.SeatType == "Standard");

        BookingCreateResult result = BookingData.CreateOrder(new BookingCreateInput(
            ShowtimeId: 7,
            SeatIds: [freeSeat.SeatId],
            StaffUserId: null,
            CustomerId: 1,
            BookingChannel: "SelfService",
            PointsUsed: 0,
            PaymentMethod: "EWallet",
            CashReceived: 0m,
            Note: "Unit test tự đặt online"));

        Assert.True(result.Succeeded);
        Assert.Single(result.TicketCodes);
        Assert.Equal(80000m, result.TotalAmount);

        using SqliteConnection conn = DatabaseHelper.OpenConnection();
        string channel = conn.QuerySingle<string>("SELECT BookingChannel FROM Orders WHERE OrderId = @id;", new { id = result.OrderId });
        int? staffId = conn.QuerySingle<int?>("SELECT StaffUserId FROM Orders WHERE OrderId = @id;", new { id = result.OrderId });

        Assert.Equal("SelfService", channel);
        Assert.Null(staffId);
    }

    [Fact]
    public void CreateOrder_MaintenanceSeat_IsRejected()
    {
        // Phòng 1 có ghế B1 đang bảo trì (IsActive = 0)
        ShowtimeSeatRow maintSeat = BookingData.LoadShowtimeSeatMap(showtimeId: 7).First(s => !s.IsActive);

        BookingCreateResult result = BookingData.CreateOrder(new BookingCreateInput(
            ShowtimeId: 7,
            SeatIds: [maintSeat.SeatId],
            StaffUserId: 2,
            CustomerId: null,
            BookingChannel: "Counter",
            PointsUsed: 0,
            PaymentMethod: "Cash",
            CashReceived: 200000m,
            Note: null));

        Assert.False(result.Succeeded);
        Assert.Contains("bảo trì", result.ErrorMessage ?? string.Empty, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void CreateOrder_AlreadyBookedSeat_IsRejected()
    {
        ShowtimeSeatRow freeSeat = BookingData.LoadShowtimeSeatMap(showtimeId: 7).First(s => s.IsActive && !s.IsBooked);

        BookingCreateResult first = BookingData.CreateOrder(new BookingCreateInput(
            ShowtimeId: 7,
            SeatIds: [freeSeat.SeatId],
            StaffUserId: 2,
            CustomerId: null,
            BookingChannel: "Counter",
            PointsUsed: 0,
            PaymentMethod: "Cash",
            CashReceived: 100000m,
            Note: null));
        Assert.True(first.Succeeded);

        // Đặt lại chính ghế đó lần thứ 2 phải bị chặn trong SqliteTransaction
        BookingCreateResult second = BookingData.CreateOrder(new BookingCreateInput(
            ShowtimeId: 7,
            SeatIds: [freeSeat.SeatId],
            StaffUserId: 2,
            CustomerId: null,
            BookingChannel: "Counter",
            PointsUsed: 0,
            PaymentMethod: "Cash",
            CashReceived: 100000m,
            Note: null));

        Assert.False(second.Succeeded);
        Assert.False(string.IsNullOrWhiteSpace(second.ErrorMessage));
    }

    [Fact]
    public void CreateOrder_SelfServiceWithCash_IsRejected()
    {
        ShowtimeSeatRow freeSeat = BookingData.LoadShowtimeSeatMap(showtimeId: 7).First(s => s.IsActive && !s.IsBooked);

        BookingCreateResult result = BookingData.CreateOrder(new BookingCreateInput(
            ShowtimeId: 7,
            SeatIds: [freeSeat.SeatId],
            StaffUserId: null,
            CustomerId: 1,
            BookingChannel: "SelfService",
            PointsUsed: 0,
            PaymentMethod: "Cash",
            CashReceived: 100000m,
            Note: null));

        Assert.False(result.Succeeded);
    }

    [Fact]
    public void CreateOrder_ExceedingMaxSeatsPerOrder_IsRejected()
    {
        List<int> elevenSeatIds = BookingData.LoadShowtimeSeatMap(showtimeId: 7)
            .Where(s => s.IsActive && !s.IsBooked)
            .Take(BookingData.MaxSeatsPerOrder + 1)
            .Select(s => s.SeatId)
            .ToList();

        Assert.Equal(11, elevenSeatIds.Count);

        BookingCreateResult result = BookingData.CreateOrder(new BookingCreateInput(
            ShowtimeId: 7,
            SeatIds: elevenSeatIds,
            StaffUserId: 2,
            CustomerId: null,
            BookingChannel: "Counter",
            PointsUsed: 0,
            PaymentMethod: "BankTransfer",
            CashReceived: 0m,
            Note: null));

        Assert.False(result.Succeeded);
        Assert.Contains("tối đa", result.ErrorMessage ?? string.Empty, StringComparison.OrdinalIgnoreCase);
    }
}
