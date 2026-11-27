using CinemaManageProject.Data;
using Xunit;

namespace CinemaManageProject.Tests;

[Collection("CinemaDatabase")]
public sealed class CancelTicketTests
{
    private readonly CinemaDatabaseFixture _db;

    public CancelTicketTests(CinemaDatabaseFixture db)
    {
        _db = db;
        _db.ResetDatabase();
    }

    [Fact]
    public void CancelCustomerTicket_PartialThenFullCancel_UpdatesOrderStatusReleasesSeatAndRestoresSpent()
    {
        const int customerId = 1;
        BookingCustomerSummary before = BookingData.FindCustomerById(customerId)!;

        List<ShowtimeSeatRow> freeSeats = BookingData.LoadShowtimeSeatMap(showtimeId: 7)
            .Where(s => s.IsActive && !s.IsBooked)
            .Take(2)
            .ToList();
        Assert.Equal(2, freeSeats.Count);

        BookingCreateResult order = BookingData.CreateOrder(new BookingCreateInput(
            ShowtimeId: 7,
            SeatIds: freeSeats.Select(s => s.SeatId).ToList(),
            StaffUserId: null,
            CustomerId: customerId,
            BookingChannel: "SelfService",
            PointsUsed: 20,
            PaymentMethod: "BankTransfer",
            CashReceived: 0m,
            Note: "Unit test hủy vé một phần và toàn phần"));
        Assert.True(order.Succeeded);

        List<CustomerMyTicketRow> tickets = BookingData.LoadMyTickets(customerId)
            .Where(t => t.OrderId == order.OrderId)
            .ToList();
        Assert.Equal(2, tickets.Count);

        // 1. Hủy vé đầu tiên -> Hóa đơn chuyển PartiallyRefunded và ghế lập tức trống trở lại
        CancelTicketResult cancelFirst = BookingData.CancelCustomerTicket(tickets[0].TicketId, customerId);
        Assert.True(cancelFirst.Succeeded);
        Assert.Equal("PartiallyRefunded", cancelFirst.NewOrderStatus);
        Assert.True(cancelFirst.RefundedAmount > 0m);

        ShowtimeSeatRow releasedSeat = BookingData.LoadShowtimeSeatMap(showtimeId: 7)
            .Single(s => s.SeatCode == tickets[0].SeatCode);
        Assert.False(releasedSeat.IsBooked);

        // 2. Hủy vé còn lại -> Hóa đơn chuyển Cancelled và tổng chi tiêu trở về mức ban đầu
        CancelTicketResult cancelSecond = BookingData.CancelCustomerTicket(tickets[1].TicketId, customerId);
        Assert.True(cancelSecond.Succeeded);
        Assert.Equal("Cancelled", cancelSecond.NewOrderStatus);

        BookingCustomerSummary afterFullCancel = BookingData.FindCustomerById(customerId)!;
        Assert.Equal(before.TotalSpent, afterFullCancel.TotalSpent);
        Assert.Equal(before.Tier, afterFullCancel.Tier);
    }

    [Fact]
    public void CancelCustomerTicket_CheckedInTicket_IsRejected()
    {
        CustomerMyTicketRow checkedInTicket = BookingData.LoadMyTickets(customerId: 1)
            .First(t => t.IsCheckedIn);

        CancelTicketResult result = BookingData.CancelCustomerTicket(checkedInTicket.TicketId, customerId: 1);

        Assert.False(result.Succeeded);
        Assert.False(string.IsNullOrWhiteSpace(result.ErrorMessage));
    }

    [Fact]
    public void CancelCustomerTicket_TicketOwnedByAnotherCustomer_IsRejected()
    {
        CustomerMyTicketRow khach1BookedTicket = BookingData.LoadMyTickets(customerId: 1)
            .First(t => t.IsPending);

        // Khách #2 cố hủy vé thuộc Khách #1 phải bị chặn
        CancelTicketResult result = BookingData.CancelCustomerTicket(khach1BookedTicket.TicketId, customerId: 2);

        Assert.False(result.Succeeded);
        Assert.Contains("không tìm thấy", result.ErrorMessage ?? string.Empty, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void CancelCustomerTicket_AlreadyCancelledTicket_IsRejected()
    {
        CustomerMyTicketRow booked = BookingData.LoadMyTickets(customerId: 1).First(t => t.IsPending);

        CancelTicketResult first = BookingData.CancelCustomerTicket(booked.TicketId, customerId: 1);
        Assert.True(first.Succeeded);

        CancelTicketResult second = BookingData.CancelCustomerTicket(booked.TicketId, customerId: 1);
        Assert.False(second.Succeeded);
    }
}
