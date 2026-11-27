using CinemaManageProject.Data;
using CinemaManageProject.Helpers;
using Dapper;
using Microsoft.Data.Sqlite;
using Xunit;

namespace CinemaManageProject.Tests;

[Collection("CinemaDatabase")]
public sealed class TransactionFailureTests
{
    private readonly CinemaDatabaseFixture _db;

    public TransactionFailureTests(CinemaDatabaseFixture db)
    {
        _db = db;
        _db.ResetDatabase();
    }

    [Fact]
    public void CreateOrder_InsufficientCash_RollsBackWithoutCreatingOrderOrChangingLoyalty()
    {
        ShowtimeSeatRow freeSeat = BookingData.LoadShowtimeSeatMap(showtimeId: 7).First(s => s.IsActive && !s.IsBooked && s.SeatType == "Standard");
        BookingCustomerSummary khachBefore = BookingData.FindCustomerById(1)!;
        (int ordersBefore, int ticketsBefore) = CountOrdersAndTickets();

        BookingCreateResult result = BookingData.CreateOrder(new BookingCreateInput(
            ShowtimeId: 7,
            SeatIds: [freeSeat.SeatId],
            StaffUserId: 2,
            CustomerId: 1,
            BookingChannel: "Counter",
            PointsUsed: 0,
            PaymentMethod: "Cash",
            CashReceived: 50000m, // Thiếu so với 80.000đ
            Note: "Test thiếu tiền mặt"));

        Assert.False(result.Succeeded);
        Assert.Equal((ordersBefore, ticketsBefore), CountOrdersAndTickets());

        BookingCustomerSummary khachAfter = BookingData.FindCustomerById(1)!;
        Assert.Equal(khachBefore.LoyaltyPoints, khachAfter.LoyaltyPoints);
        Assert.Equal(khachBefore.TotalSpent, khachAfter.TotalSpent);
    }

    [Fact]
    public void CreateOrder_PointsExceedingBalanceOrHalfCap_RollsBackCleanly()
    {
        ShowtimeSeatRow freeSeat = BookingData.LoadShowtimeSeatMap(showtimeId: 7).First(s => s.IsActive && !s.IsBooked && s.SeatType == "Standard");
        (int ordersBefore, int ticketsBefore) = CountOrdersAndTickets();

        // Ghế 80.000đ -> tối đa đổi 50% = 40 điểm; truyền 60 điểm phải bị chặn
        BookingCreateResult overCap = BookingData.CreateOrder(new BookingCreateInput(
            ShowtimeId: 7,
            SeatIds: [freeSeat.SeatId],
            StaffUserId: null,
            CustomerId: 1,
            BookingChannel: "SelfService",
            PointsUsed: 60,
            PaymentMethod: "EWallet",
            CashReceived: 0m,
            Note: null));

        Assert.False(overCap.Succeeded);
        Assert.Equal((ordersBefore, ticketsBefore), CountOrdersAndTickets());
    }

    [Fact]
    public void CreateOrder_MixedSeatFromDifferentRoom_RollsBackWithoutPartialWrites()
    {
        // Suất 7 ở Phòng 1; ghép 1 ghế hợp lệ Phòng 1 + 1 ghế thuộc Phòng 2
        ShowtimeSeatRow room1Seat = BookingData.LoadShowtimeSeatMap(showtimeId: 7).First(s => s.IsActive && !s.IsBooked);
        RoomSeatRow room2Seat = RoomData.LoadSeatsByRoom(roomId: 2).First(s => s.IsActive);
        (int ordersBefore, int ticketsBefore) = CountOrdersAndTickets();

        BookingCreateResult result = BookingData.CreateOrder(new BookingCreateInput(
            ShowtimeId: 7,
            SeatIds: [room1Seat.SeatId, room2Seat.SeatId],
            StaffUserId: 2,
            CustomerId: null,
            BookingChannel: "Counter",
            PointsUsed: 0,
            PaymentMethod: "BankTransfer",
            CashReceived: 0m,
            Note: null));

        Assert.False(result.Succeeded);
        Assert.Equal((ordersBefore, ticketsBefore), CountOrdersAndTickets());
        Assert.False(BookingData.LoadShowtimeSeatMap(showtimeId: 7).Single(s => s.SeatId == room1Seat.SeatId).IsBooked);
    }

    [Fact]
    public void CreateOrder_CancelledShowtimeOrClosedRoom_RollsBackCleanly()
    {
        ShowtimeSeatRow freeSeat = BookingData.LoadShowtimeSeatMap(showtimeId: 7).First(s => s.IsActive && !s.IsBooked);
        (int ordersBefore, int ticketsBefore) = CountOrdersAndTickets();

        using (SqliteConnection conn = DatabaseHelper.OpenConnection())
        {
            conn.Execute("UPDATE Showtimes SET Status = 'Cancelled' WHERE ShowtimeId = 7;");
        }

        BookingCreateResult cancelledShowtimeRes = BookingData.CreateOrder(new BookingCreateInput(
            ShowtimeId: 7,
            SeatIds: [freeSeat.SeatId],
            StaffUserId: 2,
            CustomerId: null,
            BookingChannel: "Counter",
            PointsUsed: 0,
            PaymentMethod: "Cash",
            CashReceived: 200000m,
            Note: null));

        Assert.False(cancelledShowtimeRes.Succeeded);
        Assert.Equal((ordersBefore, ticketsBefore), CountOrdersAndTickets());

        // Khôi phục suất chiếu nhưng đóng phòng chiếu (Rooms.IsActive = 0)
        using (SqliteConnection conn = DatabaseHelper.OpenConnection())
        {
            conn.Execute("UPDATE Showtimes SET Status = 'OpenForSale' WHERE ShowtimeId = 7;");
            conn.Execute("UPDATE Rooms SET IsActive = 0 WHERE RoomId = 1;");
        }

        BookingCreateResult closedRoomRes = BookingData.CreateOrder(new BookingCreateInput(
            ShowtimeId: 7,
            SeatIds: [freeSeat.SeatId],
            StaffUserId: 2,
            CustomerId: null,
            BookingChannel: "Counter",
            PointsUsed: 0,
            PaymentMethod: "Cash",
            CashReceived: 200000m,
            Note: null));

        Assert.False(closedRoomRes.Succeeded);
        Assert.Equal((ordersBefore, ticketsBefore), CountOrdersAndTickets());
    }

    [Fact]
    public void CreateOrder_InvalidStaffForeignKey_TriggersSqliteFkRollbackAndReturnsFailure()
    {
        ShowtimeSeatRow freeSeat = BookingData.LoadShowtimeSeatMap(showtimeId: 7).First(s => s.IsActive && !s.IsBooked);
        (int ordersBefore, int ticketsBefore) = CountOrdersAndTickets();

        // StaffUserId = 999999 không tồn tại -> vi phạm FK Orders.StaffUserId trong SqliteTransaction
        BookingCreateResult result = BookingData.CreateOrder(new BookingCreateInput(
            ShowtimeId: 7,
            SeatIds: [freeSeat.SeatId],
            StaffUserId: 999999,
            CustomerId: null,
            BookingChannel: "Counter",
            PointsUsed: 0,
            PaymentMethod: "Cash",
            CashReceived: 200000m,
            Note: null));

        Assert.False(result.Succeeded);
        Assert.Equal((ordersBefore, ticketsBefore), CountOrdersAndTickets());
        Assert.False(BookingData.LoadShowtimeSeatMap(showtimeId: 7).Single(s => s.SeatId == freeSeat.SeatId).IsBooked);
    }

    [Fact]
    public void SaveMovie_InvalidGenreForeignKeyMidTransaction_RollsBackMovieInsertAtomically()
    {
        int movieCountBefore = MovieData.LoadAllMovies().Count;

        var inputWithBadGenre = new MovieSaveInput(
            Title: "Phim Phải Bị Rollback",
            OriginalTitle: null,
            Director: null,
            Cast: null,
            DurationMinutes: 100,
            ReleaseDateSql: "2026-12-10",
            EndDateSql: null,
            AgeRating: "P",
            Language: "Tiếng Việt",
            Description: null,
            PosterPath: null,
            Status: "ComingSoon",
            GenreIds: [999999]); // GenreId không tồn tại -> lỗi FK ở bước 2 của transaction

        Assert.Throws<SqliteException>(() => MovieData.SaveMovie(null, inputWithBadGenre));

        // Giao dịch phải rollback hoàn toàn: không để lại bản ghi mồ côi trong bảng Movies
        Assert.Equal(movieCountBefore, MovieData.LoadAllMovies().Count);
        Assert.DoesNotContain(MovieData.LoadAllMovies(), m => m.Title == "Phim Phải Bị Rollback");
    }

    private static (int Orders, int Tickets) CountOrdersAndTickets()
    {
        using SqliteConnection conn = DatabaseHelper.OpenConnection();
        int orders = conn.ExecuteScalar<int>("SELECT COUNT(*) FROM Orders;");
        int tickets = conn.ExecuteScalar<int>("SELECT COUNT(*) FROM Tickets;");
        return (orders, tickets);
    }
}
