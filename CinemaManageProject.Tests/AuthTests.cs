using CinemaManageProject.Data;
using CinemaManageProject.Helpers;
using CinemaManageProject.Models;
using Dapper;
using Microsoft.Data.Sqlite;
using Xunit;

namespace CinemaManageProject.Tests;

[Collection("CinemaDatabase")]
public sealed class AuthTests
{
    private readonly CinemaDatabaseFixture _db;

    public AuthTests(CinemaDatabaseFixture db)
    {
        _db = db;
        _db.ResetDatabase();
    }

    [Theory]
    [InlineData("admin", "Admin@123", UserRole.Admin, false)]
    [InlineData("staff1", "Staff@123", UserRole.Staff, false)]
    [InlineData("khach1", "Khach@123", UserRole.Customer, true)]
    public void Authenticate_ValidSeededAccounts_SucceedsAndClearsPasswordInMemory(
        string username,
        string password,
        UserRole expectedRole,
        bool expectCustomerProfile)
    {
        AuthResult result = AuthData.Authenticate(username, password);

        Assert.True(result.Succeeded);
        Assert.NotNull(result.User);
        Assert.Equal(expectedRole, result.User!.Role);
        Assert.Equal(string.Empty, result.User.Password);
        Assert.Equal(expectCustomerProfile, result.User.CustomerProfile is not null);
    }

    [Fact]
    public void Authenticate_WrongPassword_ReturnsFailure()
    {
        AuthResult result = AuthData.Authenticate("khach1", "SaiMatKhau@123");

        Assert.False(result.Succeeded);
        Assert.Null(result.User);
        Assert.False(string.IsNullOrWhiteSpace(result.ErrorMessage));
    }

    [Fact]
    public void Authenticate_LockedAccount_ReturnsFailure()
    {
        UserActionResult lockRes = UserData.SetUserActive(userId: 4, isActive: false, currentAdminUserId: 1);
        Assert.True(lockRes.Succeeded);

        AuthResult loginRes = AuthData.Authenticate("staff3", "Staff@123");

        Assert.False(loginRes.Succeeded);
        Assert.Null(loginRes.User);
        Assert.Contains("khóa", loginRes.ErrorMessage ?? string.Empty, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void RegisterCustomer_ValidInput_HashesPasswordAndAllowsLogin()
    {
        AuthResult reg = AuthData.RegisterCustomer(
            fullName: "Nguyễn Kiểm Thử",
            phone: "0977888999",
            username: "test_new_customer",
            email: "test.new@cineverse.vn",
            password: "MyPassword@2026");

        Assert.True(reg.Succeeded);
        Assert.NotNull(reg.User);
        Assert.NotNull(reg.User!.CustomerProfile);
        Assert.Equal(UserRole.Customer, reg.User.Role);
        Assert.Equal(string.Empty, reg.User.Password);

        using (SqliteConnection conn = DatabaseHelper.OpenConnection())
        {
            string storedHash = conn.QuerySingle<string>(
                "SELECT Password FROM Users WHERE UserId = @id;",
                new { id = reg.User.UserId });

            Assert.True(PasswordHelper.IsHashed(storedHash));
            Assert.NotEqual("MyPassword@2026", storedHash);
        }

        AuthResult login = AuthData.Authenticate("test_new_customer", "MyPassword@2026");
        Assert.True(login.Succeeded);
        Assert.Equal(reg.User.UserId, login.User!.UserId);
    }

    [Fact]
    public void RegisterCustomer_DuplicateUsernameOrPhone_ReturnsFailure()
    {
        AuthResult dupUsername = AuthData.RegisterCustomer("Trùng User", "0977000111", "khach1", null, "Pass@123");
        Assert.False(dupUsername.Succeeded);

        AuthResult dupPhone = AuthData.RegisterCustomer("Trùng Phone", "0901000001", "unique_user_99", null, "Pass@123");
        Assert.False(dupPhone.Succeeded);
    }

    [Fact]
    public void Authenticate_LegacyPlainTextPassword_AutoUpgradesToPbkdf2()
    {
        int legacyUserId;
        using (SqliteConnection conn = DatabaseHelper.OpenConnection())
        {
            legacyUserId = conn.ExecuteScalar<int>(
                """
                INSERT INTO Users (Username, Password, FullName, Role, IsActive)
                VALUES ('legacy_unit_user', 'Legacy@123', 'Tài khoản cũ', 'Staff', 1);
                SELECT last_insert_rowid();
                """);
        }

        // Sai mật khẩu không được nâng cấp hash
        Assert.False(AuthData.Authenticate("legacy_unit_user", "WrongPass").Succeeded);
        using (SqliteConnection conn = DatabaseHelper.OpenConnection())
        {
            string stillPlain = conn.QuerySingle<string>(
                "SELECT Password FROM Users WHERE UserId = @id;",
                new { id = legacyUserId });
            Assert.Equal("Legacy@123", stillPlain);
        }

        // Đăng nhập đúng phải thành công và tự nâng cấp lên PBKDF2
        AuthResult ok = AuthData.Authenticate("legacy_unit_user", "Legacy@123");
        Assert.True(ok.Succeeded);

        using (SqliteConnection conn = DatabaseHelper.OpenConnection())
        {
            string upgraded = conn.QuerySingle<string>(
                "SELECT Password FROM Users WHERE UserId = @id;",
                new { id = legacyUserId });
            Assert.True(PasswordHelper.IsHashed(upgraded));
        }

        // Đăng nhập lại lần 2 vẫn hoạt động bình thường với chuỗi hash mới
        Assert.True(AuthData.Authenticate("legacy_unit_user", "Legacy@123").Succeeded);
    }
}
