using CinemaManageProject.Data;
using CinemaManageProject.Helpers;
using Dapper;
using Microsoft.Data.Sqlite;
using Xunit;

namespace CinemaManageProject.Tests;

[Collection("CinemaDatabase")]
public sealed class UserAndCustomerTests
{
    private readonly CinemaDatabaseFixture _db;

    public UserAndCustomerTests(CinemaDatabaseFixture db)
    {
        _db = db;
        _db.ResetDatabase();
    }

    [Fact]
    public void SaveUser_CreateAndUpdateStaff_HashesPasswordAndSupportsOptionalReset()
    {
        UserSaveResult created = UserData.SaveUser(
            new UserSaveInput(null, "staff_new_01", "StaffNew@123", "Lê Nhân Viên Mới", "new@cineverse.vn", "0912345678", "Staff", true, null),
            currentAdminUserId: 1);

        Assert.True(created.Succeeded);
        Assert.True(AuthData.Authenticate("staff_new_01", "StaffNew@123").Succeeded);

        // Cập nhật không truyền NewPassword -> giữ nguyên mật khẩu cũ
        UserSaveResult updatedKeepPass = UserData.SaveUser(
            new UserSaveInput(created.UserId, "staff_new_01", null, "Lê Nhân Viên Đổi Tên", "new2@cineverse.vn", "0912345679", "Staff", true, null),
            currentAdminUserId: 1);
        Assert.True(updatedKeepPass.Succeeded);
        Assert.True(AuthData.Authenticate("staff_new_01", "StaffNew@123").Succeeded);

        // Đặt lại mật khẩu mới -> mật khẩu cũ không còn dùng được, mật khẩu mới hợp lệ
        UserSaveResult resetPass = UserData.SaveUser(
            new UserSaveInput(created.UserId, "staff_new_01", "ResetPass@999", "Lê Nhân Viên Đổi Tên", "new2@cineverse.vn", "0912345679", "Staff", true, null),
            currentAdminUserId: 1);
        Assert.True(resetPass.Succeeded);
        Assert.False(AuthData.Authenticate("staff_new_01", "StaffNew@123").Succeeded);
        Assert.True(AuthData.Authenticate("staff_new_01", "ResetPass@999").Succeeded);
    }

    [Fact]
    public void SaveUser_CreateAndUpdateCustomer_SyncsCustomerProfile1To1()
    {
        UserSaveResult created = UserData.SaveUser(
            new UserSaveInput(null, "khach_sync_01", "KhachSync@123", "Phạm Đồng Bộ", "sync@test.vn", "0981112223", "Customer", true, "2002-08-15"),
            currentAdminUserId: 1);

        Assert.True(created.Succeeded);
        AdminUserRow createdRow = UserData.LoadAllUsers().Single(u => u.UserId == created.UserId);
        Assert.NotNull(createdRow.CustomerId);
        Assert.Equal("Phạm Đồng Bộ", createdRow.FullName);
        Assert.Equal("0981112223", createdRow.Phone);
        Assert.Equal("2002-08-15", createdRow.DateOfBirthSql);

        // Cập nhật tên/SĐT/ngày sinh từ UserData phải đồng bộ ngay sang bảng Customers
        UserSaveResult updated = UserData.SaveUser(
            new UserSaveInput(created.UserId, "khach_sync_01", null, "Phạm Đồng Bộ Mới", "sync2@test.vn", "0981112224", "Customer", true, "2001-01-20"),
            currentAdminUserId: 1);

        Assert.True(updated.Succeeded);
        BookingCustomerSummary? syncedProfile = BookingData.FindCustomerByPhone("0981112224");
        Assert.NotNull(syncedProfile);
        Assert.Equal("Phạm Đồng Bộ Mới", syncedProfile!.FullName);
        Assert.Equal(createdRow.CustomerId, syncedProfile.CustomerId);
    }

    [Theory]
    [InlineData("Admin", "newadmin", "Pass@123", "Admin Giả", "0911222333", null)]       // Không cho gán vai trò Admin
    [InlineData("Staff", "ab", "Pass@123", "Quá Ngắn", "0911222333", null)]              // Username < 3 ký tự
    [InlineData("Staff", "bad user", "Pass@123", "Có Khoảng Trắng", "0911222333", null)] // Username chứa khoảng trắng
    [InlineData("Staff", "valid_u1", "Pass@123", "   ", "0911222333", null)]             // Họ tên rỗng
    [InlineData("Customer", "valid_c1", "Pass@123", "Khách Thiếu SĐT", "", null)]        // Customer thiếu SĐT
    [InlineData("Staff", "valid_u2", "12345", "Mật Khẩu Ngắn", "0911222333", null)]      // Mật khẩu < 6 ký tự
    [InlineData("Staff", "valid_u3", "      ", "Mật Khẩu Trắng", "0911222333", null)]    // Mật khẩu toàn khoảng trắng
    [InlineData("Customer", "valid_c2", "Pass@123", "DOB Tương Lai", "0911222334", "2099-01-01")] // Ngày sinh tương lai
    [InlineData("Customer", "valid_c3", "Pass@123", "DOB Sai Format", "0911222335", "15/08/2002")] // Ngày sinh sai định dạng
    public void SaveUser_InvalidInputs_AreRejected(
        string role,
        string username,
        string password,
        string fullName,
        string phone,
        string? dob)
    {
        UserSaveResult result = UserData.SaveUser(
            new UserSaveInput(null, username, password, fullName, null, phone, role, true, dob),
            currentAdminUserId: 1);

        Assert.False(result.Succeeded);
        Assert.False(string.IsNullOrWhiteSpace(result.ErrorMessage));
    }

    [Fact]
    public void UserData_ProtectsRootAdminRoleChangeAndSelfLockOrDelete()
    {
        // 1. Không thể chỉnh sửa, khóa hay xóa tài khoản Admin gốc (#1)
        Assert.False(UserData.SaveUser(
            new UserSaveInput(1, "admin", null, "Đổi tên Admin", null, "0900000001", "Staff", true, null),
            currentAdminUserId: 1).Succeeded);
        Assert.False(UserData.SetUserActive(userId: 1, isActive: false, currentAdminUserId: 1).Succeeded);
        Assert.False(UserData.DeleteUser(userId: 1, currentAdminUserId: 1).Succeeded);

        // 2. Không thể đổi vai trò của tài khoản đã tạo (Staff #2 -> Customer)
        UserSaveResult roleChange = UserData.SaveUser(
            new UserSaveInput(2, "staff1", null, "Nguyễn Minh Khôi", "staff1@cineverse.vn", "0901999888", "Customer", true, null),
            currentAdminUserId: 1);
        Assert.False(roleChange.Succeeded);
        Assert.Contains("vai trò", roleChange.ErrorMessage ?? string.Empty, StringComparison.OrdinalIgnoreCase);

        // 3. Không thể tự khóa hoặc tự xóa chính tài khoản đang đăng nhập
        Assert.False(UserData.SetUserActive(userId: 2, isActive: false, currentAdminUserId: 2).Succeeded);
        Assert.False(UserData.DeleteUser(userId: 2, currentAdminUserId: 2).Succeeded);
    }

    [Fact]
    public void DeleteUser_BlocksAccountsWithHistoryAndHardDeletesFreshCustomer()
    {
        // staff1 (#2) có đơn hàng & check-in -> chặn xóa
        Assert.False(UserData.DeleteUser(userId: 2, currentAdminUserId: 1).Succeeded);
        // khach1 (#5) có hóa đơn -> chặn xóa
        Assert.False(UserData.DeleteUser(userId: 5, currentAdminUserId: 1).Succeeded);

        // Tài khoản Customer mới chưa có giao dịch -> xóa cứng sạch cả Users & Customers
        UserSaveResult fresh = UserData.SaveUser(
            new UserSaveInput(null, "fresh_del_cust", "Fresh@123", "Khách Xóa Được", null, "0987654321", "Customer", true, null),
            currentAdminUserId: 1);
        Assert.True(fresh.Succeeded);

        UserActionResult deleted = UserData.DeleteUser(fresh.UserId, currentAdminUserId: 1);
        Assert.True(deleted.Succeeded);
        Assert.DoesNotContain(UserData.LoadAllUsers(), u => u.UserId == fresh.UserId);
        Assert.Null(BookingData.FindCustomerByPhone("0987654321"));
    }

    [Fact]
    public void SaveCounterCustomer_CreateWalkInAndSyncAppLinkedCustomer_Works()
    {
        // 1. Tạo hội viên quầy (UserId = NULL, Tier = Member)
        CounterCustomerSaveResult created = CustomerData.SaveCounterCustomer(
            new CounterCustomerSaveInput(null, "Nguyễn Khách Quầy", "0988777111", "quay@test.vn", "1999-04-12"));
        Assert.True(created.Succeeded);

        StaffCustomerRow row = CustomerData.LoadAllCustomers().Single(c => c.CustomerId == created.CustomerId);
        Assert.False(row.HasAppAccount);
        Assert.Equal("Member", row.Tier);
        Assert.Equal("1999-04-12", row.DateOfBirthSql);

        // 2. Cập nhật khách có tài khoản App (khach1, CustomerId = 1, UserId = 5) -> đồng bộ ngược sang Users
        CounterCustomerSaveResult synced = CustomerData.SaveCounterCustomer(
            new CounterCustomerSaveInput(1, "Phạm Anh Tuấn VIP", "0901000001", "tuan.vip@gmail.com", "2002-05-14"));
        Assert.True(synced.Succeeded);

        using SqliteConnection conn = DatabaseHelper.OpenConnection();
        string userFullName = conn.QuerySingle<string>("SELECT FullName FROM Users WHERE UserId = 5;");
        string userEmail = conn.QuerySingle<string>("SELECT Email FROM Users WHERE UserId = 5;");
        Assert.Equal("Phạm Anh Tuấn VIP", userFullName);
        Assert.Equal("tuan.vip@gmail.com", userEmail);
    }

    [Theory]
    [InlineData("", "0988777222", null)]                 // Tên trống
    [InlineData("Ngắn SĐT", "0988", null)]               // SĐT < 8 ký tự
    [InlineData("SĐT Trắng", "0988 777 222", null)]      // SĐT chứa khoảng trắng
    [InlineData("DOB Tương Lai", "0988777223", "2099-12-31")]
    [InlineData("DOB Sai", "0988777224", "31-12-2000")]
    [InlineData("Trùng SĐT", "0901000001", null)]        // Trùng SĐT của khach1
    public void SaveCounterCustomer_InvalidInputsAndDuplicatePhone_AreRejected(string name, string phone, string? dob)
    {
        CounterCustomerSaveResult result = CustomerData.SaveCounterCustomer(
            new CounterCustomerSaveInput(null, name, phone, null, dob));

        Assert.False(result.Succeeded);
        Assert.False(string.IsNullOrWhiteSpace(result.ErrorMessage));
    }

    [Fact]
    public void LoadAllCustomersAndOrderHistory_ReturnsSeededRecords()
    {
        List<StaffCustomerRow> customers = CustomerData.LoadAllCustomers();
        List<CustomerOrderHistoryRow> history = CustomerData.LoadCustomerOrderHistory(customerId: 1);

        Assert.Equal(25, customers.Count);
        Assert.NotEmpty(history);
        Assert.All(history, o => Assert.False(string.IsNullOrWhiteSpace(o.MoviesSummary)));
    }
}
