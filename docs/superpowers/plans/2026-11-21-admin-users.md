# AdminUsers (Nhân sự & Tài khoản) Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Hoàn thiện màn hình **Nhân sự & Tài khoản (`users`)** cho vai trò `Admin` với quản lý `Staff` và `Customer` (thêm, sửa, đổi mật khẩu, khóa/mở khóa, xóa có kiểm tra ràng buộc lịch sử), đồng thời bảo vệ nghiêm ngặt tài khoản `Admin` gốc (không tạo thêm `Admin`, không sửa quyền/khóa/xóa `Admin` hoặc chính người đang đăng nhập).

**Architecture:** Tách lớp truy cập SQLite an toàn giao dịch vào `CinemaManageProject/Data/UserData.cs` (`LoadAllUsers`, `SaveUser`, `SetUserActive`, `DeleteUser`) để đồng bộ 1-1 giữa `Users` và `Customers` và kiểm tra ràng buộc `Showtimes`/`Orders`/`Tickets`; đóng gói state lọc, 4 KPI, phân quyền form và sự kiện xóa trắng `PasswordBox` (`PasswordClearRequested`) trong `AdminUsersViewModel.cs`; dựng giao diện Dark Zinc bảng danh sách full chiều ngang kết hợp form 2 cột phía dưới (đồng bộ với `AdminShowtimesView.xaml`/`AdminOrdersView.xaml`) trong `AdminUsersView.xaml` và nối vào `MainViewModel` + `MainWindow.xaml.cs`.

**Tech Stack:** .NET 10 Windows (`net10.0-windows`), WPF, SQLite (`Microsoft.Data.Sqlite`), `AGENTS.md` Dark Zinc Theme.

**Spec:** [2026-11-21-admin-users-design.md](../specs/2026-11-21-admin-users-design.md)

## Global Constraints
* Dùng `Microsoft.Data.Sqlite` qua `DatabaseHelper.OpenConnection()`, `PRAGMA foreign_keys = ON;`, parameterized query; không thêm EF Core hay thư viện MVVM bên thứ ba.
* Tái sử dụng `FormatHelper.*`, `DatabaseHelper.*`, các `Style` trong `Themes/Controls.xaml` (`CardBorderStyle`, `BadgeBorderStyle`, `ModernTextBoxStyle`, `ModernPasswordBoxStyle`, `ModernComboBoxStyle`, `ModernCheckBoxStyle`, `ModernDataGridStyle`, `PrimaryButtonStyle`, `SecondaryButtonStyle`, `DangerButtonStyle`, `CinemaVectorIconStyle`, `UsersGroupIconGeometry`).
* Mật khẩu lưu tương thích trực tiếp với `LoginViewModel.Login`/`Register` (tối thiểu 6 ký tự khi tạo mới hoặc khi nhập mật khẩu mới lúc chỉnh sửa; bỏ trống khi sửa = giữ nguyên).
* Code-behind `.xaml.cs` tối giản: chỉ `InitializeComponent()`, chuyển tiếp sự kiện `Click`, đọc/xóa `PasswordBox` qua `PasswordClearRequested`, và hộp thoại `MessageBox` xác nhận trước khi `DeleteSelectedUser()`.
* Mọi kiểm thử sửa/xóa trên DB phải chạy trên **bản sao SQLite cô lập**, không làm bẩn `CinemaManageProject/Database/cinema.db`.

## Review Focus
* **Bảo vệ Admin ở cả UI lẫn Data layer:** Không cho tạo mới `Role = 'Admin'`, không cho nâng/hạ quyền hoặc sửa bản ghi `Admin` (kể cả khi gọi trực tiếp `UserData.SaveUser`), không cho khóa (`SetUserActive`) hay xóa (`DeleteUser`) `Admin` hoặc chính `currentAdminUserId`.
* **Xung đột `Username` (không phân biệt hoa thường) & `Customers.Phone` (`UNIQUE`):** Kiểm tra `LOWER(Username) = LOWER(@u)` để chặn tạo tài khoản trùng tên kiểu `Admin` / `STAFF1`, và kiểm tra trùng `Phone` trên `Customers` (loại trừ chính `Customer` của user đang sửa).
* **Đồng bộ `Users` + `Customers` & Ngày sinh `DateOfBirth`:** Tạo `Customer` phải chèn đủ cả `Users` và `Customers` (`Member`, `0` điểm, `0đ`); cập nhật `Customer` phải đồng bộ `FullName`, `Phone`, `Email`, `DateOfBirth` (`dd/MM/yyyy` → `yyyy-MM-dd`, chặn ngày sinh tương lai) và không làm thay đổi `LoyaltyPoints`/`TotalSpent`/`Tier`.
* **Chặn xóa cứng tài khoản đã có giao dịch lịch sử (`ON DELETE RESTRICT` & `Orders`):** `staff1` (`10` đơn) và `khach1` (đã có hóa đơn) phải bị chặn `DeleteUser` kèm thông báo hướng dẫn dùng Khóa tài khoản; tài khoản `Staff`/`Customer` mới chưa có giao dịch thì xóa sạch khỏi `Customers` và `Users`.
* **Bảo toàn `PasswordBox` & Layout ở `MinWidth="1080"`:** Khi chuyển từ chọn dòng A sang dòng B, bấm `+ Thêm tài khoản` hoặc Lưu thành công, `PasswordClearRequested` phải xóa sạch mật khẩu đang gõ dở; bảng danh sách phải giữ highlight `IsSelected` `#265E6AD2`, `FontSize="12"`, cột `MinWidth` an toàn và không tràn ngang ở `788×660`.

---

### Task 1: Tầng Truy cập Dữ liệu `UserData.cs`

**Files:**
- Create: `CinemaManageProject/Data/UserData.cs`
- Test: `scratch/CheckUsers.cs` (via `scratch/UiProbe/UiProbe.csproj`)

**Interfaces:**
- Records (Produces):
  - `AdminUserRow(int UserId, string Username, string FullName, string Email, string Phone, string Role, bool IsActive, string CreatedAtSql, string? LastLoginAtSql, int? CustomerId, string? DateOfBirthSql, int LoyaltyPoints, decimal TotalSpent, string? Tier, int StaffActivityCount, int CustomerOrdersCount)`
    - Computed properties: `RoleDisplay`, `StatusDisplay`, `IsAdminRole`, `IsCustomerRole`, `CreatedAtDisplay`, `LastLoginDisplay`, `DateOfBirthDisplay`, `TierDisplay`, `LoyaltyPointsText`, `TotalSpentText`, `ActivitySummaryText`, `HasBlockingHistory`, `CanHardDelete`.
  - `UserSaveInput(int? UserId, string Username, string? NewPassword, string FullName, string? Email, string? Phone, string Role, bool IsActive, string? DateOfBirthSql)`
  - `UserSaveResult(bool Succeeded, int UserId, string? ErrorMessage)`
  - `UserActionResult(bool Succeeded, string? ErrorMessage)`
- Methods (Produces):
  - `public static List<AdminUserRow> LoadAllUsers()`
  - `public static UserSaveResult SaveUser(UserSaveInput input, int currentAdminUserId)`
  - `public static UserActionResult SetUserActive(int userId, bool isActive, int currentAdminUserId)`
  - `public static UserActionResult DeleteUser(int userId, int currentAdminUserId)`

- [ ] **Step 1: Viết kiểm thử RED trong `scratch/CheckUsers.cs`** (chạy trên bản sao cô lập của `cinema.db`) kiểm tra `UserData.LoadAllUsers()` trả về `29` tài khoản (`1 Admin`, `3 Staff`, `25 Customer`), xác nhận `SaveUser` chặn tạo/sửa `Admin`, chặn trùng `Username` khác hoa-thường & trùng `Phone` khách hàng, tạo/sửa/giữ nguyên mật khẩu/cập nhật mật khẩu mới + khóa (`SetUserActive`) cho `Staff` và `Customer` đồng bộ sang `Customers`, kiểm tra đăng nhập qua `LoginViewModel`, và kiểm tra `DeleteUser` chặn `admin`/`staff1`/`khach1` nhưng xóa sạch được `Staff` & `Customer` mới không có giao dịch.
- [ ] **Step 2: Chạy kiểm thử RED** xác nhận lỗi biên dịch vì `UserData` chưa tồn tại.
- [ ] **Step 3: Tạo `CinemaManageProject/Data/UserData.cs`** cài đặt các record và `LoadAllUsers`, `SaveUser`, `SetUserActive`, `DeleteUser`.
- [ ] **Step 4: Chạy build + kiểm thử Task 1** xác nhận PASS (`TASK1_USER_DATA_OK`).

---

### Task 2: `AdminUsersViewModel.cs`

**Files:**
- Create: `CinemaManageProject/ViewModels/Admin/AdminUsersViewModel.cs`
- Test: `scratch/CheckUsers.cs`

**Interfaces:**
- Consumes: `UserData.LoadAllUsers`, `UserData.SaveUser`, `UserData.SetUserActive`, `UserData.DeleteUser`, `App.CurrentUser`
- Records (Produces):
  - `UserFilterOption(string Value, string Label)`
- Events & Properties (Produces):
  - `public event Action? PasswordClearRequested;`
  - Filters: `SearchKeyword`, `RoleFilterOptions`, `SelectedRoleFilterOption`, `StatusFilterOptions`, `SelectedStatusFilterOption`, `ResetFilters()`
  - KPI: `TotalUsersCountText`, `StaffCountText`, `CustomerCountText`, `LockedUsersCountText`, `ResultCountText`
  - Selection & Form: `Users`, `SelectedUser`, `FormHeaderText`, `FormModeBadge`, `IsEditMode`, `IsProtectedAdmin`, `IsEditableUser`, `CanSelectRole`, `CanToggleActiveQuick`, `QuickLockButtonLabel`, `RoleFormOptions`, `SelectedFormRoleOption`, `Username`, `FullName`, `Phone`, `Email`, `DateOfBirthText`, `IsActiveAccount`, `IsCustomerFormMode`, `ShowMemberReadOnlyCard`, `MemberTierText`, `MemberPointsText`, `MemberTotalSpentText`, `PasswordFieldLabel`, `ProtectedAdminNotice`, `ErrorMessage`, `HasError`, `SuccessMessage`, `HasSuccess`
  - Public methods: `LoadData()`, `StartNewUser()`, `SaveUser(string password)`, `ResetForm()`, `ToggleSelectedUserActive()`, `CanDeleteSelectedUser(out string summary)`, `DeleteSelectedUser()`

- [ ] **Step 1: Mở rộng `scratch/CheckUsers.cs` với kiểm thử RED cho Task 2** phủ KPI (`29` / `3` / `25` / `0`), lọc từ khóa/vai trò/trạng thái, `ResetFilters()`, chọn `admin` đặt `IsProtectedAdmin = true` và khóa chỉnh sửa, `StartNewUser()` chỉ có `Staff` & `Customer` và bắn `PasswordClearRequested`, tạo mới + sửa giữ nguyên mật khẩu + khóa/mở khóa nhanh + kiểm tra `DateOfBirth` tương lai báo lỗi + chặn tự khóa `App.CurrentUser`.
- [ ] **Step 2: Chạy kiểm thử RED** xác nhận lỗi biên dịch vì `AdminUsersViewModel` chưa tồn tại.
- [ ] **Step 3: Tạo `CinemaManageProject/ViewModels/Admin/AdminUsersViewModel.cs`** cài đặt đầy đủ filtering, KPI, CRUD form validation và quản lý `PasswordClearRequested`.
- [ ] **Step 4: Chạy build + kiểm thử Task 2** xác nhận PASS (`TASK2_USERS_VM_OK`).

---

### Task 3: Giao diện `AdminUsersView`, Nối Shell & Kiểm thử Tổng thể

**Files:**
- Create: `CinemaManageProject/Views/Admin/AdminUsersView.xaml`
- Create: `CinemaManageProject/Views/Admin/AdminUsersView.xaml.cs`
- Modify: `CinemaManageProject/ViewModels/MainViewModel.cs:123,225-238`
- Modify: `CinemaManageProject/Views/MainWindow.xaml.cs:40-47`
- Test: `scratch/CheckUsers.cs`

**Interfaces:**
- Consumes: `AdminUsersViewModel` (tất cả properties, `StartNewUser()`, `SaveUser(string)`, `ResetForm()`, `ToggleSelectedUserActive()`, `CanDeleteSelectedUser(out string)`, `DeleteSelectedUser()`, `ResetFilters()`, `PasswordClearRequested`) và styles/icons trong `Themes/Controls.xaml`
- Code-behind: chuyển tiếp click các nút `+ Thêm tài khoản`, `Đặt lại`, `Lưu tài khoản` (truyền `UserPasswordBox.Password`), `Khóa/Mở khóa`, `Làm mới`, `Xóa` (kèm `MessageBox` xác nhận), đồng bộ `PasswordClearRequested += ClearPasswordBox`.

- [ ] **Step 1: Mở rộng `scratch/CheckUsers.cs` với kiểm thử RED cho Task 3** kiểm tra điều hướng `MainViewModel.SelectMenu("users")` mở `AdminUsersViewModel` (`PageTitle == "Nhân sự & Tài khoản"`), dựng `AdminUsersView` trong `Window` mang `CinemaWindowStyle`, kiểm tra highlight `IsSelected` `#265E6AD2`, `FontSize="12"` trên `UsersDataGrid`, kiểm tra bố cục không tràn ở `988px` và `788px` (`NarrowW <= 790`, mọi cột `>= 30px`), kiểm tra nút `NewUserButton` / `UserPasswordBox` được xóa trắng khi đổi chọn dòng, và xuất ảnh `users_render.png`.
- [ ] **Step 2: Chạy kiểm thử RED** xác nhận thất bại vì `"users"` chưa mở khóa và thiếu `AdminUsersView`.
- [ ] **Step 3: Tạo `AdminUsersView.xaml`, `AdminUsersView.xaml.cs` và cập nhật `MainViewModel.cs` + `MainWindow.xaml.cs`** (bật `isAvailable: true` cho `"users"`, thêm `case "users":` và mapping `AdminUsersViewModel => new AdminUsersView()`).
- [ ] **Step 4: Chạy build sạch (`0 Warning(s)`, `0 Error(s)`), kiểm tra ảnh `users_render.png`, chạy `CheckUsers` + toàn bộ regression (`CheckOrders`, `CheckDashboards`, `CheckShowtimes`, `CheckRooms`, `CheckFilters`, `SmokeTest`, `CheckCustomerPosterStrip`), rồi thực hiện Final Code Review.**
