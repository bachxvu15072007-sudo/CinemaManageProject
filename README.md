# CINEVERSE CINEMA (`CinemaManageProject`)

Ứng dụng desktop quản lý rạp chiếu phim và đặt vé xây dựng bằng **WPF (.NET 10 Windows)**, **SQLite (`Microsoft.Data.Sqlite`)**, kiến trúc **MVVM gọn nhẹ** và giao diện **Dark Theme** tối giản.

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
* **`Controls.xaml`:** Chứa toàn bộ tài nguyên dùng chung (`BoolToVis` converter và các `Style` chuẩn với bảng màu Dark Theme ghi trực tiếp: `CinemaWindowStyle`, `CardBorderStyle`, `BadgeBorderStyle`, `PrimaryButtonStyle`, `SecondaryButtonStyle`, `DangerButtonStyle`, `GhostButtonStyle`, `NavMenuButtonStyle`, `ModernTextBoxStyle`, `ModernPasswordBoxStyle`, `ModernDataGridStyle`). Các màn hình View giữ gọn chỉ viết bố cục giao diện và xử lý sự kiện `Click` gọi method ViewModel (không dùng `Command`/`RelayCommand`).

### 2.4. Đăng nhập, Khung Shell & 3 Dashboard Theo Vai trò (`Views/`, `ViewModels/`)
* **Vòng đời cửa sổ (`App.xaml` / `App.xaml.cs`):**
  * Dùng `ShutdownMode="OnExplicitShutdown"`, khởi chạy với `LoginWindow`, chuyển sang `Views.MainWindow` sau khi xác thực (`CompleteLogin()`) và quay lại `LoginWindow` khi đăng xuất (`Logout()`).
  * Mở kết nối tới `Database/cinema.db` qua `DatabaseHelper.OpenConnection()`.
* **Màn hình Đăng nhập (`Views/LoginWindow.xaml`, `ViewModels/LoginViewModel.cs`):**
  * Xác thực trực tiếp với bảng `Users` (nạp thêm `CustomerProfile` nếu vai trò là `Customer`), hỗ trợ chuyển sang form **Đăng ký hội viên mới** và 3 nút điền nhanh tài khoản mẫu `Admin` / `Staff` / `Customer`.
* **Khung chính (`Views/MainWindow.xaml`, `ViewModels/MainViewModel.cs`):**
  * Sidebar điều hướng phân quyền tự động theo `UserRole`, Topbar làm mới dữ liệu, nút Đăng xuất và vùng `ContentControl` nạp động Dashboard theo vai trò.
* **3 Dashboard riêng biệt đặt theo từng thư mục vai trò (`Admin/`, `Staff/`, `Customer/` trong `Views/` và `ViewModels/`):**
  * **`AdminDashboardView`:** 4 thẻ KPI toàn rạp (tổng doanh thu & hôm nay, vé phát hành & đã check-in, số phim đang/sắp chiếu, phòng hoạt động & ghế bảo trì), bảng *Lịch chiếu & Tỷ lệ lấp đầy* và *Hóa đơn gần đây*.
  * **`StaffDashboardView`:** 4 thẻ KPI ca quầy cá nhân (vé đã bán, doanh thu quầy của nhân viên, suất đang mở bán, vé chờ soát check-in), bảng *Suất chiếu mở bán & Ghế trống* và *Giao dịch quầy gần đây*.
  * **`CustomerDashboardView`:** Thẻ hội viên (`Tier`, điểm tích lũy, tổng chi tiêu, vé chờ chiếu), danh sách phim với bộ lọc `Tất cả` / `Đang chiếu` / `Sắp chiếu`, và bảng *Vé của tôi*.

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

Các mục điều hướng còn lại trên Sidebar đang ở trạng thái chờ để triển khai lần lượt ở các bước tiếp theo:

1. **Quản lý Phim & Thể loại (`Admin`):** Thêm/sửa/xóa phim (`Movies`), gán nhiều thể loại (`MovieGenres`), chọn ảnh poster và chuyển trạng thái `ComingSoon` / `NowShowing` / `Ended`.
2. **Quản lý Phòng chiếu & Sơ đồ Ghế (`Admin`):** Thêm/sửa phòng (`Rooms`), chỉnh loại ghế (`Standard`, `VIP`, `Couple`), `PriceMultiplier` và bật/tắt bảo trì từng ghế (`Seats.IsActive`).
3. **Quản lý & Xem Lịch chiếu (`Admin` / `Staff` / `Customer`):** Lên lịch `Showtimes`, tự tính `EndTime = StartTime + DurationMinutes + 15 phút` dọn phòng, chặn trùng giờ theo phòng.
4. **Quy trình Chọn ghế & Đặt vé (`Booking`):** Giao diện chọn ghế trực quan theo `RowIndex`/`ColIndex`, khóa ghế đã bán (`Booked`/`CheckedIn`) và ghế bảo trì, tính giá vé, áp dụng điểm thưởng `LoyaltyPoints`, tính tiền thối khi thanh toán `Cash` (`Counter`) và lưu `Order` + `Ticket` (`TicketCode`) trong SQLite transaction.
5. **Soát vé Check-in & Hóa đơn (`Staff` / `Admin` / `Customer`):** Nhập mã `TicketCode` xác nhận `CheckedIn`, xem chi tiết hóa đơn, lịch sử vé của khách và xử lý hủy/hoàn vé hợp lệ.
6. **Khách hàng & Nhân sự (`Staff` / `Admin`):** Tra cứu hội viên theo SĐT, quản lý tài khoản nhân viên (`IsActive`).
