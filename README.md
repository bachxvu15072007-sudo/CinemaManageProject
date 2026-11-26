# CINEVERSE CINEMA (`CinemaManageProject`)

Ứng dụng desktop quản lý rạp chiếu phim và đặt vé xây dựng bằng **WPF (.NET 10 Windows)**, **SQLite (`Microsoft.Data.Sqlite` + `Dapper`)**, kiến trúc **MVVM gọn nhẹ** và giao diện **Dark Theme** tối giản.

---

## 1. Giới thiệu Dự án

Hệ thống hỗ trợ quản lý vận hành rạp phim và đặt vé tập trung theo 3 vai trò (`UserRole`):
* **Admin (Quản trị viên):** Giám sát doanh thu toàn rạp, tỷ lệ lấp đầy suất chiếu, quản lý phim, phòng chiếu, sơ đồ ghế, lịch chiếu, hóa đơn và nhân sự.
* **Staff (Nhân viên quầy):** Bán vé trực tiếp tại quầy (`Counter`), soát vé check-in (`CheckedIn`), theo dõi suất chiếu mở bán và tra cứu khách hàng.
* **Customer (Khách hàng):** Theo dõi hạng thành viên và điểm tích lũy (`LoyaltyPoints`), duyệt phim/lịch chiếu, đặt vé trực tuyến (`SelfService`) và quản lý vé đã đặt.

---

## 2. Những thứ Đã làm & Mô tả Chi tiết

### 2.1. Cơ sở dữ liệu SQLite (`CinemaManageProject/Database/`)
* **`CinemaManageDB.sql`:** Thiết kế 10 bảng chuẩn hóa với `PRAGMA foreign_keys = ON;`:
  * `Users`, `Customers`, `Genres`, `Movies`, `MovieGenres`, `Rooms`, `Seats`, `Showtimes`, `Orders`, `Tickets`.
  * Các giá trị enum được ràng buộc trực tiếp bằng `CHECK (...)` dạng chuỗi.
* **`SeedData.sql` & `cinema.db`:** Dữ liệu mẫu sẵn sàng kiểm thử:
  * **29 tài khoản:** `admin`, `staff1..3`, `khach1..25`.
  * **25 hồ sơ khách hàng:** rải theo nhiều nhóm sở thích xem phim (`95` cặp khách–phim riêng biệt) để phục vụ hệ khuyến nghị.
  * **15 phim & 10 thể loại:** 10 phim đang chiếu (`NowShowing`), 5 phim sắp chiếu (`ComingSoon`), kèm đầy đủ ảnh poster thực tế (`CinemaManageProject/Posters/`).
  * **4 phòng chiếu & 354 ghế (hàng cuối là ghế đôi nên bằng 1/2 số ghế hàng thường):** `Phòng 1 - 2D` (75 ghế, `B1` bảo trì), `Phòng 2 - 2D` (75 ghế), `Phòng 3 - 3D` (90 ghế), `Phòng 4 - IMAX` (114 ghế).
  * **28 suất chiếu** (`18` mở bán + `10` đã chiếu quá khứ, đồng nhất giá gốc chuẩn `80.000đ` và đã tính `+15` phút dọn phòng), **98 hóa đơn** và **160 vé mẫu** (`Booked`, `CheckedIn`, `Cancelled`).

### 2.2. Models & Enums (`CinemaManageProject/Models/`)
* **`CinemaEnums.cs`:** Định nghĩa 11 enum (`UserRole`, `CustomerTier`, `MovieStatus`, `AgeRating`, `RoomType`, `SeatType`, `ShowtimeStatus`, `OrderStatus`, `PaymentMethod`, `BookingChannel`, `TicketStatus`).
* **10 lớp Entity:** Ánh xạ đầy đủ các bảng SQLite (`User`, `Customer`, `Movie`, `Genre`, `MovieGenre`, `Room`, `Seat`, `Showtime`, `Order`, `Ticket`).

### 2.3. Giao diện Dark Theme & Resource Tập trung (`CinemaManageProject/Themes/Controls.xaml`)
* **`Controls.xaml`:** Chứa toàn bộ tài nguyên dùng chung (`BoolToVis` converter, 10 icon vector `<Geometry>`, nền điện ảnh `CinemaWindowBackgroundBrush` và các `Style` chuẩn với bảng màu Dark Theme ghi trực tiếp: `CinemaWindowStyle`, `CardBorderStyle`, `BadgeBorderStyle`, `PrimaryButtonStyle`, `SecondaryButtonStyle`, `DangerButtonStyle`, `GhostButtonStyle`, `NavMenuButtonStyle`, `ModernTextBoxStyle`, `ModernPasswordBoxStyle`, `ModernDataGridStyle`, `CompactDataGridStyle`, `EllipsisCellTextStyle`, `WrappingCellTextStyle`, `BookingSeatButtonStyle`). Các màn hình View giữ gọn chỉ viết bố cục giao diện và xử lý sự kiện `Click` gọi method ViewModel (không dùng `Command`/`RelayCommand`).

### 2.4. Tầng Dữ liệu (`Data/`) & Phụ trợ (`Helpers/`)
* **`CinemaManageProject/Helpers/`:** `DatabaseHelper` (mở kết nối SQLite kèm `PRAGMA foreign_keys = ON;` và `SqliteDecimalHandler` cho Dapper), `FormatHelper` (định dạng tiền VNĐ, ngày giờ, trạng thái), `PosterHelper` (nạp/lưu ảnh poster).
* **`CinemaManageProject/Data/`:** Tập trung toàn bộ truy vấn SQLite thuần có tham số hóa qua **Dapper** vào 10 lớp tĩnh (`AuthData`, `BookingData`, `CheckInData`, `CustomerData`, `DashboardData`, `MovieData`, `OrderData`, `RoomData`, `ShowtimeData`, `UserData`) — không còn câu SQL trực tiếp trong `ViewModels/`.

### 2.5. Đăng nhập, Khung Shell & Trọn bộ 13 Màn hình theo 3 Vai trò (`Views/`, `ViewModels/`)
* **Vòng đời cửa sổ (`App.xaml` / `App.xaml.cs`):**
  * Dùng `ShutdownMode="OnExplicitShutdown"`, khởi chạy với `LoginWindow`, chuyển sang `Views.MainWindow` sau khi xác thực (`CompleteLogin()`) và quay lại `LoginWindow` khi đăng xuất (`Logout()`).
* **Màn hình Đăng nhập (`Views/LoginWindow.xaml`, `ViewModels/LoginViewModel.cs`):**
  * Xác thực qua `AuthData` (nạp thêm `CustomerProfile` nếu vai trò là `Customer`), hỗ trợ chuyển sang form **Đăng ký hội viên mới** và 3 nút điền nhanh tài khoản mẫu `Admin` / `Staff` / `Customer`.
* **Khung chính (`Views/MainWindow.xaml`, `ViewModels/MainViewModel.cs`):**
  * Sidebar điều hướng phân quyền tự động theo `UserRole` có hỗ trợ đóng/mở gọn (`ToggleSidebar`), Topbar làm mới dữ liệu, nút Đăng xuất và vùng `ContentControl` nạp động toàn bộ màn hình con.
* **Phân hệ Admin (6/6 màn hình hoàn chỉnh):**
  * **`AdminDashboardView`:** 4 thẻ KPI toàn rạp, bảng *Lịch chiếu & Tỷ lệ lấp đầy* và *Hóa đơn gần đây*.
  * **`AdminMoviesView`:** Thêm/sửa/xóa phim, gán nhiều thể loại (`MovieGenres`), nạp/lưu ảnh poster và lọc theo thể loại/độ tuổi/trạng thái.
  * **`AdminRoomsView`:** Xem thông số cố định của 4 phòng chiếu, trực quan hóa sơ đồ ghế (Thường / VIP / Ghế đôi rộng gấp đôi), kiểm tra đơn đặt theo ghế và bật/tắt bảo trì có chặn ghế đang có vé hiệu lực.
  * **`AdminShowtimesView`:** Lên lịch chiếu tự cộng `+15` phút dọn phòng, chặn trùng giờ cùng phòng, xem trước giá vé theo hạng ghế và khóa sửa thông số cốt lõi khi suất đã phát sinh vé.
  * **`AdminOrdersView`:** Tra cứu hóa đơn & phân tích doanh thu (4 KPI, bộ lọc ngày/kênh/PTTT/trạng thái/từ khóa, cơ cấu 4 phương thức thanh toán, phân bổ doanh thu theo phim theo tỷ lệ `activeSub`, chi tiết phiếu tính tiền & danh sách vé).
  * **`AdminUsersView`:** Quản trị tài khoản `Staff` & `Customer` (thêm, sửa, đổi mật khẩu, khóa/mở khóa, xóa có kiểm tra ràng buộc lịch sử), đồng bộ 1-1 `Users` ↔ `Customers`, và bảo vệ nghiêm ngặt tài khoản `Admin` gốc.
* **Phân hệ Staff (4/4 màn hình hoàn chỉnh):**
  * **`StaffDashboardView`:** 4 thẻ KPI ca quầy cá nhân, bảng *Suất chiếu mở bán & Ghế trống* và *Giao dịch quầy gần đây*.
  * **`StaffPosView`:** Bán vé tại quầy (`Counter`) — chọn suất mở bán, sơ đồ chọn nhiều ghế (chặn ghế bảo trì & ghế đã đặt, khóa kiểm tra trong `SqliteTransaction`), tra cứu hội viên theo SĐT hoặc khách vãng lai, đổi điểm thưởng (`1 điểm = 1.000đ`, tối đa 50%), tính tiền thối (`Cash`) và phát hành hóa đơn + mã vé.
  * **`StaffCheckInView`:** Soát vé Check-in — tra cứu theo `TicketCode` hoặc chọn từ hàng chờ `Booked`, đối chiếu thông tin phòng/ghế/suất chiếu và xác nhận `CheckedIn` (`CheckedInAt`, `CheckedInByUserId`).
  * **`StaffCustomersView`:** Tra cứu hội viên theo SĐT/tên/hạng thẻ, xem điểm tích lũy & lịch sử hóa đơn, cập nhật liên hệ (đồng bộ `Users`) và đăng ký hội viên mới tại quầy (`UserId = NULL`).
* **Phân hệ Customer (3/3 màn hình hoàn chỉnh):**
  * **`CustomerDashboardView`:** Thẻ hội viên (`Tier`, điểm tích lũy, tổng chi tiêu, vé chờ chiếu), dải trượt ngang poster phim với khung spotlight chi tiết & bộ lọc, và bảng *Vé của tôi*.
  * **`CustomerBrowseView`:** Duyệt phim Đang chiếu / Sắp chiếu theo thể loại/từ khóa, chọn suất chiếu mở bán, tự chọn ghế ngồi dùng chung `BookingSeatButtonStyle`, đổi điểm thưởng và thanh toán trực tuyến (`SelfService` — Chuyển khoản / Ví điện tử / Thẻ).
  * **`CustomerMyTicketsView`:** Quản lý toàn bộ vé & lịch sử hóa đơn của hội viên, xem cuống vé điện tử có `TicketCode` check-in và hỗ trợ hủy vé `Booked` (cập nhật `PartiallyRefunded` / `Cancelled` và hoàn điều chỉnh điểm/hạng thẻ trong transaction).

---

## 3. Hướng dẫn Chạy & Tài khoản Demo

### Yêu cầu môi trường
* Windows + **.NET 10 SDK (`net10.0-windows`)**

### Chạy ứng dụng
```powershell
dotnet build CinemaManageProject.slnx
dotnet run --project CinemaManageProject\CinemaManageProject.csproj
```

### Tài khoản mẫu (`Database/SeedData.sql`)
| Vai trò | Tên đăng nhập | Mật khẩu | Ghi chú |
| :--- | :--- | :--- | :--- |
| **Admin** | `admin` | `Admin@123` | Toàn quyền xem KPI & quản trị rạp |
| **Staff** | `staff1` | `Staff@123` | Nhân viên quầy (ngoài ra có `staff2`, `staff3`) |
| **Customer** | `khach1` | `Khach@123` | Hội viên Silver, 180 điểm (ngoài ra có `khach2`, `khach3`) |

---

## 4. Những thứ Cần làm Tiếp tục

Toàn bộ 13 màn hình vận hành chính (Admin 6/6, Staff 4/4, Customer 3/3) đã hoàn thiện và kích hoạt đầy đủ trên Sidebar. Các hạng mục mở rộng tiếp theo gồm:

1. **Hệ khuyến nghị phim cá nhân hóa:** Gợi ý phim theo lịch sử xem và độ tương đồng sở thích trên bộ dữ liệu 25 hội viên (`95` cặp khách–phim trong `SeedData.sql`).
2. **Nâng cấp kỹ thuật (nếu triển khai thực tế):** Băm mật khẩu (`PBKDF2`/`SHA256`), chuyển DB runtime & poster sang `%LocalAppData%` kèm `SqliteConnectionStringBuilder` + `WAL`.
