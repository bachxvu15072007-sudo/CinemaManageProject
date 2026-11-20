# Thiết kế Màn hình Quản lý Lịch chiếu (`Admin`)

## 1. Mục tiêu & Phạm vi
* **Mục tiêu:** Kích hoạt mục **Quản lý Lịch chiếu** (`showtimes`) trên Sidebar của vai trò `Admin` để tạo, chỉnh sửa, lọc, đổi trạng thái và xóa an toàn các suất chiếu trong `Database/cinema.db`.
* **Ngoài phạm vi (Non-goals):** Không thay đổi thông số phòng/ghế (`Rooms`/`Seats`), không làm bán vé tại quầy (`POS`) hay chọn ghế khách hàng (`Booking`).

## 2. Phương án kiến trúc
* **Bố cục 2 cột trên 1 màn hình (`AdminShowtimesView`):** Thanh tiêu đề (`+ Thêm suất chiếu`) + thanh tìm kiếm & bộ lọc (từ khóa, ngày chiếu, phòng, trạng thái) ở trên; cột trái là `DataGrid` danh sách suất chiếu kèm tỷ lệ vé đã bán; cột phải là form chi tiết thêm/sửa suất chiếu và xem trước giờ kết thúc / giá vé theo hạng ghế.
* **Lý do chọn:** Đồng bộ 100% với phong cách Dark Zinc và cách thao tác của `AdminMoviesView`, gọn nhẹ không cần tạo `Window` phụ.

## 3. Cấu trúc File & Điều hướng
1. `CinemaManageProject/Data/ShowtimeData.cs`: Truy vấn SQLite cho bảng `Showtimes` (nạp tùy chọn Phim/Phòng, nạp danh sách suất kèm số vé hiệu lực và ghế khả dụng của phòng, kiểm tra trùng khung giờ trong `SqliteTransaction`, lưu và xóa an toàn).
2. `CinemaManageProject/ViewModels/Admin/AdminShowtimesViewModel.cs`: Kế thừa `ViewModelBase`, xử lý tìm kiếm & bộ lọc, tự gợi ý định dạng chiếu theo loại phòng + ngôn ngữ phim, xem trước `EndTime = StartTime + DurationMinutes + 15 phút` và bảng giá quy đổi theo phụ thu phòng (`UnitPrice = (BaseTicketPrice + BasePriceSurcharge) * Multiplier`).
3. `CinemaManageProject/Views/Admin/AdminShowtimesView.xaml` & `.xaml.cs`: Giao diện WPF Dark Theme, code-behind tối giản chuyển tiếp `Click` và hộp thoại xác nhận xóa.
4. `CinemaManageProject/Themes/Controls.xaml`: Bổ sung `IsEnabled="False"` (`Opacity="0.55"`) cho `ModernComboBoxStyle` để thể hiện rõ trạng thái khóa lõi suất chiếu khi đã có vé đặt.
5. `CinemaManageProject/ViewModels/MainViewModel.cs` & `CinemaManageProject/Views/MainWindow.xaml.cs`: Mở khóa item `"showtimes"` (`isAvailable: true`) và ánh xạ `AdminShowtimesViewModel` → `AdminShowtimesView`.

## 4. Quy tắc Nghiệp vụ & Toàn vẹn Dữ liệu
* **Tính giờ kết thúc:** `EndTime = StartTime + Movie.DurationMinutes + 15` phút dọn phòng; lưu xuống SQLite chuẩn `yyyy-MM-dd HH:mm:ss`.
* **Chống trùng lịch theo phòng (`SqliteTransaction`):** Với mọi suất chiếu có `Status != 'Cancelled'`, không được chồng lấn thời gian với suất khác trong cùng `RoomId` (`existing.StartTime < newEndTime AND existing.EndTime > newStartTime`). Hai suất chạm mốc (`EndTime == StartTime` kế tiếp) được phép vì 15 phút dọn phòng đã nằm trong `EndTime`.
* **Ràng buộc khi đã có vé hiệu lực (`Booked` / `CheckedIn`):**
  * Không cho phép đổi Phim, Phòng hoặc Ngày/Giờ bắt đầu để bảo toàn vé đã xuất; chỉ cho phép đổi `Status` (ví dụ chuyển sang `Completed` hoặc `Cancelled`).
  * Không cho xóa cứng bất kỳ suất nào đã phát sinh dòng trong `Tickets` (`ON DELETE RESTRICT`), thay vào đó hướng dẫn chuyển trạng thái sang `Cancelled`.
* **Quy tắc giá vé thống nhất theo Loại phòng & Hạng ghế:** Giá gốc chuẩn toàn rạp cố định **`80.000đ`** (`ShowtimeData.StandardBaseTicketPrice`), không đổi giá theo từng phim; giá bán thực tế của từng ghế chỉ phụ thuộc vào phụ thu loại phòng và hệ số hạng ghế: `(80.000đ + Room.BasePriceSurcharge) × Seat.PriceMultiplier`.
* **Ràng buộc tạo mới:** Chỉ chọn phim chưa `Ended` và phòng đang hoạt động (`IsActive = 1`). `CreatedByUserId` lấy từ `App.CurrentUser?.UserId ?? 1`.

## 5. Tiêu chí Nghiệm thu
* `dotnet build` đạt `0 Warning(s)`, `0 Error(s)`.
* Kiểm thử STA WPF + SQLite: nạp 12 suất mẫu, lọc theo ngày/phòng/trạng thái/từ khóa, kiểm tra công thức `EndTime = StartTime + Duration + 15p`, chặn trùng lịch cùng phòng, cho phép nối tiếp ngay sát mốc `EndTime`, chặn đổi lõi/xóa suất đã có vé (`ShowtimeId = 7`), tạo + sửa + xóa sạch suất test mới, và điều hướng từ `MainViewModel`.
