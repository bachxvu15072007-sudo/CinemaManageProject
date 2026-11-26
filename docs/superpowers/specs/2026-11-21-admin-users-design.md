# Thiết kế Màn hình Quản lý Nhân sự & Tài khoản (`AdminUsersView`)

## 1. Mục tiêu & Phạm vi
* **Mục tiêu:** Kích hoạt mục **Nhân sự & Tài khoản (`users`)** trên Sidebar của vai trò `Admin` để quản trị tập trung tài khoản nhân viên (`Staff`) và tài khoản hội viên (`Customer`) với đầy đủ thao tác **Thêm / Sửa / Đặt lại mật khẩu / Khóa-Mở khóa / Xóa** theo ràng buộc an toàn dữ liệu.
* **Quy tắc phân quyền Admin (Chủ rạp):**
  * **Không được phép tạo thêm tài khoản `Admin` mới** từ giao diện quản trị và **không cho nâng/hạ quyền** lên/xuống `Admin`.
  * Tài khoản `Admin` hiện có (`admin`) hiển thị trong danh sách ở trạng thái **chỉ xem bảo vệ**: không sửa vai trò, không khóa/vô hiệu hóa và không xóa được bất kỳ `Admin` nào cũng như chính tài khoản đang đăng nhập (`App.CurrentUser.UserId`).
* **Cách tiếp cận (hướng gọn chuẩn nhất):**
  * Giao diện dùng một màn hình CRUD thống nhất cho cả `Staff` và `Customer` (đọc trước từ bảng `Users LEFT JOIN Customers`), giữ các chỉ số hội viên (`Tier`, `LoyaltyPoints`, `TotalSpent`) ở chế độ **chỉ đọc** để không làm sai lệch đối soát hóa đơn; khi tạo/sửa `Customer` luôn đồng bộ 1-1 giữa `Users` và `Customers` trong cùng `SqliteTransaction`.

## 2. Nghiệp vụ & Dữ liệu (`CinemaManageProject/Data/UserData.cs`)
1. **Nạp danh sách (`LoadAllUsers()`):**
   * Truy vấn `Users u LEFT JOIN Customers c ON c.UserId = u.UserId` kèm các bộ đếm ràng buộc tham chiếu:
     * `Staff/Admin`: tổng `Showtimes(CreatedByUserId)` + `Orders(StaffUserId)` + `Tickets(CheckedInByUserId)`.
     * `Customer`: tổng `Orders(CustomerId)`.
   * Sắp xếp theo thứ tự vai trò (`Admin` → `Staff` → `Customer`), rồi `IsActive DESC, UserId ASC`.
2. **Thêm mới & Cập nhật (`SaveUser(UserSaveInput input, int currentAdminUserId)`):**
   * Chạy trong `SqliteTransaction`:
     * Chặn nếu `input.Role == "Admin"` hoặc đang sửa bản ghi có `Users.Role == 'Admin'` (kể cả khi cố gửi tham số trực tiếp qua lớp Data).
     * Kiểm tra `Username` bắt buộc, không chứa khoảng trắng, tối thiểu 3 ký tự và không trùng trong `Users`.
     * Kiểm tra `FullName` bắt buộc; nếu `Role == "Customer"`, bắt buộc nhập `Phone` (`Customers.Phone` là `NOT NULL UNIQUE`) và kiểm tra trùng `Phone` trên `Customers` (loại trừ `Customer` gắn với `UserId` đang sửa).
     * Kiểm tra mật khẩu: bắt buộc `>= 6` ký tự khi tạo mới; khi cập nhật, bỏ trống nghĩa là **giữ nguyên mật khẩu cũ**, có nhập thì phải `>= 6` ký tự mới ghi đè `Users.Password`. (Giữ plain-text khớp hoàn toàn với `LoginViewModel.Login` hiện tại, không phá vỡ đăng nhập seed.)
     * Ngày sinh (`DateOfBirth`): tùy chọn khi là `Customer`, chấp nhận `dd/MM/yyyy` / `d/M/yyyy` / `yyyy-MM-dd` và lưu xuống SQLite chuẩn `yyyy-MM-dd` (hoặc `NULL`).
     * Khi chỉnh sửa không đổi `Role` của tài khoản đã tồn tại để tránh dữ liệu mồ côi giữa hóa đơn quầy và hồ sơ hội viên.
     * Đồng bộ `Customers`: tạo mới `Customer` sẽ `INSERT` thêm dòng `Customers (LoyaltyPoints=0, TotalSpent=0, Tier='Member')`; cập nhật `Customer` sẽ `UPDATE Customers` (hoặc tự chèn bổ sung nếu `Users` role `Customer` chưa có bản ghi `Customers`).
3. **Khóa / Mở khóa tài khoản (`SetUserActive(int userId, bool isActive, int currentAdminUserId)`):**
   * Chặn khóa `Admin` và chặn tự khóa chính `currentAdminUserId`.
   * Cập nhật `Users.IsActive` (`0`/`1`), phối hợp trực tiếp với kiểm tra khóa tài khoản tại `LoginViewModel.cs`.
4. **Xóa tài khoản (`DeleteUser(int userId, int currentAdminUserId)`):**
   * Chạy trong `SqliteTransaction`:
     * Chặn xóa `Admin` và chặn tự xóa `currentAdminUserId`.
     * Nếu tài khoản là `Staff` và đã có lịch sử tạo suất chiếu / lập đơn hàng / check-in vé (`ReferenceActivityCount > 0`): chặn xóa cứng và trả lỗi hướng dẫn chuyển sang **Khóa tài khoản** (`ON DELETE RESTRICT`).
     * Nếu tài khoản là `Customer` và đã có hóa đơn (`CustomerOrdersCount > 0`): chặn xóa cứng để bảo toàn liên kết hóa đơn và hạng thẻ, hướng dẫn dùng **Khóa tài khoản**.
     * Chỉ khi chưa phát sinh giao dịch lịch sử (`0`): xóa `Customers` gắn với `UserId` (nếu có) rồi xóa `Users`.

## 3. Giao diện (`AdminUsersView.xaml`) & ViewModel (`AdminUsersViewModel.cs`)
* **Bố cục Dark Zinc khớp `AdminShowtimesView.xaml` / `AdminOrdersView.xaml` & `MinWidth="1080"`:**
  * **Header:** Icon `UsersGroupIconGeometry` + tiêu đề `"Nhân sự & Tài khoản"` + mô tả ngắn + nút `+ Thêm tài khoản` (`PrimaryButtonStyle`, đặt lại form sang tạo `Staff` mới).
  * **4 thẻ KPI gọn:** `TỔNG TÀI KHOẢN`, `NHÂN SỰ (STAFF)`, `HỘI VIÊN (CUSTOMER)`, `TÀI KHOẢN ĐÃ KHÓA`.
  * **Thanh tìm kiếm & bộ lọc:** Ô `Tìm kiếm` (Username, Họ tên, SĐT, Email) + `Vai trò` (`All`/`Staff`/`Customer`/`Admin`) + `Trạng thái` (`All`/`Active`/`Locked`) + nút `Đặt lại`.
  * **Bảng Danh sách Tài khoản (full chiều ngang) & Form 2 cột phía dưới:**
    * **Phía trên:** Bảng `DataGrid` danh sách tài khoản (`Tên đăng nhập`, `Họ và tên`, `Vai trò`, `Số điện thoại`, `Trạng thái`, `Hoạt động / Hạng`, `Đăng nhập cuối`). Dùng `EllipsisCellTextStyle`, `FontSize="12"`, giữ trigger highlight `IsSelected` `#265E6AD2` và `MinWidth` an toàn.
    * **Phía dưới (2 cột cân đối):** Form chi tiết & cập nhật (`THÊM MỚI` / `CHỈNH SỬA` / `CHỈ XEM (ADMIN)`):
      * Các banner lỗi / thành công / cảnh báo bảo vệ Admin (`IsProtectedAdmin`).
      * ComboBox `Vai trò *` (chỉ có `Staff` / `Customer`, chỉ bật ở chế độ Thêm mới).
      * `Tên đăng nhập *`, `Mật khẩu` (`PasswordBox` chuẩn `ModernPasswordBoxStyle` với nhãn `PasswordFieldLabel`, chuyển tiếp mật khẩu qua code-behind khi nhấn Lưu), `Họ và tên *`, `Số điện thoại`, `Email`.
      * Khối **Hồ sơ Hội viên** (hiển thị khi chọn hoặc tạo `Customer`): `Ngày sinh (dd/MM/yyyy)` + hộp tóm tắt chỉ đọc `Hạng thẻ`, `Điểm tích lũy`, `Tổng chi tiêu` khi chỉnh sửa `Customer`.
      * CheckBox `Tài khoản đang hoạt động` + thanh nút hành động: `Lưu tài khoản`, `Khóa tài khoản` / `Mở khóa`, `Làm mới`, `Xóa tài khoản` (có xác nhận `MessageBox`).
* **Điều hướng Shell (`MainViewModel.cs` & `MainWindow.xaml.cs`):**
  * Mở khóa `"users"` (`isAvailable: true`), xử lý `case "users":` -> `AdminUsersViewModel`, ánh xạ `AdminUsersViewModel => new AdminUsersView()`.

## 4. Tiêu chí Nghiệm thu
* `CinemaManageProject.csproj` build sạch `0 Warning(s), 0 Error(s)`.
* Kiểm thử STA WPF + SQLite trên bản sao cô lập (`CheckUsers.cs`) xác nhận: nạp đủ `29` tài khoản seed (`1 Admin`, `3 Staff`, `25 Customer`), lọc từ khóa/vai trò/trạng thái, chặn tạo/sửa/khóa/xóa `Admin`, chặn tự khóa chính mình, thêm/sửa/đổi mật khẩu/khóa/xóa `Staff` & `Customer` đồng bộ 2 bảng `Users` + `Customers`, chặn xóa tài khoản đã phát sinh hóa đơn/vé (`staff1`, `khach1`), kiểm tra giao diện & chụp `users_render.png` ở `988px` và hẹp `MinWidth="1080"` (`788px`).
* Không hồi quy toàn bộ các bài kiểm thử hiện có (`CheckOrders`, `CheckDashboards`, `CheckShowtimes`, `CheckRooms`, `CheckFilters`, `SmokeTest`, `CheckCustomerPosterStrip`).
