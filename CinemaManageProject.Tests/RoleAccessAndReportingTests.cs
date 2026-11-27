using CinemaManageProject.Data;
using CinemaManageProject.Helpers;
using CinemaManageProject.Models;
using CinemaManageProject.ViewModels;
using Xunit;

namespace CinemaManageProject.Tests;

[Collection("CinemaDatabase")]
public sealed class RoleAccessAndReportingTests
{
    private readonly CinemaDatabaseFixture _db;

    public RoleAccessAndReportingTests(CinemaDatabaseFixture db)
    {
        _db = db;
        _db.ResetDatabase();
    }

    [Theory]
    [InlineData("admin", "Admin@123", new[] { "dashboard", "movies", "rooms", "showtimes", "orders", "users" })]
    [InlineData("staff1", "Staff@123", new[] { "dashboard", "pos", "checkin", "customers" })]
    [InlineData("khach1", "Khach@123", new[] { "dashboard", "browse", "mytickets" })]
    public void MainViewModel_SidebarNavigation_ExposesOnlyMenusAllowedForRole(
        string username,
        string password,
        string[] expectedMenuKeys)
    {
        AuthResult auth = AuthData.Authenticate(username, password);
        Assert.True(auth.Succeeded);

        string[] actualKeys = [];
        RunOnStaThread(() =>
        {
            User? previous = App.CurrentUser;
            try
            {
                App.CurrentUser = auth.User;
                var mainVm = new MainViewModel();
                actualKeys = mainVm.MenuItems.Select(m => m.Key).ToArray();
            }
            finally
            {
                App.CurrentUser = previous;
            }
        });

        Assert.Equal(expectedMenuKeys, actualKeys);
    }

    [Theory]
    [InlineData("Counter", null, 1, "Cash")]            // Quầy thiếu StaffUserId
    [InlineData("SelfService", null, null, "EWallet")]  // Online thiếu CustomerId
    [InlineData("SelfService", null, 1, "Cash")]        // Online cấm thanh toán tiền mặt
    [InlineData("UnknownChannel", 2, 1, "Cash")]        // Kênh đặt không hợp lệ
    [InlineData("Counter", 2, 1, "Crypto")]             // PTTT không hợp lệ
    public void CreateOrder_EnforcesChannelAndPaymentRoleRules(
        string channel,
        int? staffUserId,
        int? customerId,
        string paymentMethod)
    {
        ShowtimeSeatRow freeSeat = BookingData.LoadShowtimeSeatMap(showtimeId: 7).First(s => s.IsActive && !s.IsBooked);

        BookingCreateResult result = BookingData.CreateOrder(new BookingCreateInput(
            ShowtimeId: 7,
            SeatIds: [freeSeat.SeatId],
            StaffUserId: staffUserId,
            CustomerId: customerId,
            BookingChannel: channel,
            PointsUsed: 0,
            PaymentMethod: paymentMethod,
            CashReceived: 200000m,
            Note: null));

        Assert.False(result.Succeeded);
        Assert.False(string.IsNullOrWhiteSpace(result.ErrorMessage));
    }

    [Fact]
    public void DashboardData_AllThreeRoleDashboards_ReturnConsistentSnapshots()
    {
        AdminDashboardSnapshot admin = DashboardData.LoadAdminDashboard();
        Assert.True(admin.TotalRevenue > 0m);
        Assert.Equal(10, admin.NowShowingCount);
        Assert.Equal(5, admin.ComingSoonCount);
        Assert.Equal(4, admin.TotalRooms);
        Assert.Equal(1, admin.MaintenanceSeats);
        Assert.Equal(12, admin.Showtimes.Count);
        Assert.Equal(8, admin.RecentOrders.Count);

        StaffDashboardSnapshot staff = DashboardData.LoadStaffDashboard(staffId: 2);
        Assert.True(staff.MyTickets > 0);
        Assert.True(staff.MyRevenue > 0m);
        Assert.Equal(18, staff.OpenShowtimesCount);
        Assert.Equal(12, staff.OpenShowtimes.Count);

        CustomerDashboardSnapshot cust = DashboardData.LoadCustomerDashboard(userId: 5, fallbackProfile: null);
        Assert.NotNull(cust.Profile);
        Assert.Equal(1, cust.Profile!.CustomerId);
        Assert.Equal(15, cust.Movies.Count);
        Assert.NotEmpty(cust.MyTickets);
    }

    [Fact]
    public void OrderData_LoadAllOrdersAndTickets_MatchesSeededCountsAndStatusRules()
    {
        List<AdminOrderSummaryRow> orders = OrderData.LoadAllOrders();
        Dictionary<int, IReadOnlyList<OrderTicketDetailRow>> ticketsByOrder = OrderData.LoadAllOrderTickets();

        Assert.Equal(98, orders.Count);
        Assert.Equal(160, ticketsByOrder.Values.Sum(list => list.Count));
        Assert.All(orders, o =>
        {
            Assert.True(o.ActiveTicketsCount <= o.TotalTickets);
            Assert.Equal(o.Status != "Cancelled", o.IsRevenueEffective);
        });
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("PBKDF2$100000$not-valid-base64!$bad")]
    [InlineData("PBKDF2$500$AAAA$BBBB")]
    [InlineData("PBKDF2$100000$only_three_parts")]
    public void PasswordHelper_MalformedOrEmptyStoredHash_ReturnsFalseWithoutThrowing(string? badHash)
    {
        bool ok = PasswordHelper.VerifyPassword("AnyPassword@123", badHash, out bool needsUpgrade);

        Assert.False(ok);
        Assert.False(needsUpgrade);
    }

    private static void RunOnStaThread(Action action)
    {
        Exception? captured = null;
        var thread = new Thread(() =>
        {
            try
            {
                action();
            }
            catch (Exception ex)
            {
                captured = ex;
            }
        });
        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();
        thread.Join();

        if (captured is not null)
        {
            throw captured;
        }
    }
}
