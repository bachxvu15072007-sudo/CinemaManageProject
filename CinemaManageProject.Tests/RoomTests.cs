using CinemaManageProject.Data;
using CinemaManageProject.ViewModels.Admin;
using Xunit;

namespace CinemaManageProject.Tests;

[Collection("CinemaDatabase")]
public sealed class RoomTests
{
    private readonly CinemaDatabaseFixture _db;

    public RoomTests(CinemaDatabaseFixture db)
    {
        _db = db;
        _db.ResetDatabase();
    }

    [Fact]
    public void LoadAllRooms_Returns4SeededRoomsWithAccurateSeatTotals()
    {
        List<AdminRoomRow> rooms = RoomData.LoadAllRooms();

        Assert.Equal(4, rooms.Count);
        Assert.Equal(354, rooms.Sum(r => r.TotalSeats));

        AdminRoomRow room1 = rooms.Single(r => r.RoomId == 1);
        Assert.Equal(75, room1.TotalSeats);
        Assert.Equal(74, room1.ActiveSeats);
        Assert.Equal(1, room1.MaintenanceSeats);
        Assert.Equal(40, room1.StandardSeats);
        Assert.Equal(30, room1.VipSeats);
        Assert.Equal(5, room1.CoupleSeats);
    }

    [Fact]
    public void LoadSeatsByRoom_ReturnsOrderedSeatsWithMaintenanceB1()
    {
        List<RoomSeatRow> seats = RoomData.LoadSeatsByRoom(roomId: 1);

        Assert.Equal(75, seats.Count);
        Assert.Equal("A1", seats[0].SeatCode);
        Assert.False(seats.Single(s => s.SeatCode == "B1").IsActive);
        Assert.Equal("Couple", seats.Single(s => s.SeatCode == "H1").SeatType);
        Assert.Equal(2.00m, seats.Single(s => s.SeatCode == "H1").PriceMultiplier);
    }

    [Fact]
    public void SetSeatActive_FreeSeat_TogglesMaintenanceAndRestores()
    {
        RoomSeatRow seatA2 = RoomData.LoadSeatsByRoom(roomId: 1).Single(s => s.SeatCode == "A2");
        Assert.True(seatA2.IsActive);

        RoomData.SetSeatActive(seatA2.SeatId, isActive: false);
        Assert.False(RoomData.LoadSeatsByRoom(roomId: 1).Single(s => s.SeatId == seatA2.SeatId).IsActive);
        Assert.Equal(2, RoomData.LoadAllRooms().Single(r => r.RoomId == 1).MaintenanceSeats);

        RoomData.SetSeatActive(seatA2.SeatId, isActive: true);
        Assert.True(RoomData.LoadSeatsByRoom(roomId: 1).Single(s => s.SeatId == seatA2.SeatId).IsActive);
        Assert.Equal(1, RoomData.LoadAllRooms().Single(r => r.RoomId == 1).MaintenanceSeats);
    }

    [Fact]
    public void CountActiveTicketsAndSeatOrders_BookedSeatE5_ReturnsActiveBookingAndHistory()
    {
        RoomSeatRow seatE5 = RoomData.LoadSeatsByRoom(roomId: 1).Single(s => s.SeatCode == "E5");

        int activeCount = RoomData.CountActiveTicketsForSeat(seatE5.SeatId);
        List<SeatOrderRow> orders = RoomData.LoadSeatOrders(seatE5.SeatId);

        Assert.True(activeCount > 0);
        Assert.NotEmpty(orders);
    }

    [Fact]
    public void AdminRoomsViewModel_ToggleMaintenanceOnBookedSeat_IsBlocked()
    {
        var vm = new AdminRoomsViewModel();
        vm.SelectedRoom = vm.Rooms.Single(r => r.RoomId == 1);

        SeatCellItem bookedCell = vm.SeatRows
            .SelectMany(r => r.Seats)
            .Single(s => s.SeatCode == "E5");

        vm.SelectSeat(bookedCell);
        vm.ToggleSelectedSeatAvailable();

        Assert.True(vm.HasError);
        Assert.Contains("bảo trì", vm.ErrorMessage ?? string.Empty, StringComparison.OrdinalIgnoreCase);
        Assert.True(RoomData.LoadSeatsByRoom(roomId: 1).Single(s => s.SeatCode == "E5").IsActive);
    }
}
