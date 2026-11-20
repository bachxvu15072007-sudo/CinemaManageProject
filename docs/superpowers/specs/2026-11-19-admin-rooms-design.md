# Thiết kế Màn hình Quản lý Phòng & Sơ đồ Ghế (`Admin`)

## 1. Mục tiêu & Phạm vi
* **Mục tiêu:** Kích hoạt mục **Phòng & Sơ đồ ghế** (`rooms`) trên Sidebar của vai trò `Admin` để:
  1. Xem thông số cố định của từng phòng chiếu (`Phòng 1 - 2D`, `Phòng 2 - 2D`, `Phòng 3 - 3D`, `Phòng 4 - IMAX`) và sơ đồ ghế trực quan theo từng hàng.
  2. Thể hiện ghế đôi (`Couple`) là một cặp dài (`Width="86"`, bằng chiều ngang 2 ghế đơn `Width="40" + Margin`) nên hàng ghế đôi chỉ có một nửa số ghế (`5` hoặc `6` ghế đôi).
  3. Khi bấm vào một ghế bất kỳ: xem thông tin cố định của ghế, kiểm tra xem ghế đó đã có đơn hàng/vé (`Orders` & `Tickets`) nào đặt vào chưa, và bật/tắt trạng thái bảo trì (`Available` ↔ `Bảo trì - Không cho đặt`).
* **Ngoài phạm vi (Non-goals):** Không cho phép thêm, sửa, xóa phòng chiếu hay chỉnh sửa bản chất ghế (`RoomType`, kích thước hàng/cột, `SeatType`, `PriceMultiplier`), vì thông số phòng và phân hạng ghế thuộc bản chất vật lý cố định của rạp.

## 2. Cơ sở Dữ liệu (`SeedData.sql` & `cinema.db`)
* **4 Phòng chiếu (bao gồm 2 phòng thường 2D):**
  * `Phòng 1 - 2D` (`Standard2D`, `8 hàng × 10 ghế đơn`, phụ thu `0đ`): `A–D` Thường (`40`), `E–G` VIP (`30`), `H` Đôi (`5` ghế đôi `H1..H5`, hệ số `2.00`) → **75 ghế** (`B1` đang bảo trì `IsActive = 0`).
  * `Phòng 2 - 2D` (`Standard2D`, `8 hàng × 10 ghế đơn`, phụ thu `0đ`): `A–D` Thường (`40`), `E–G` VIP (`30`), `H` Đôi (`5` ghế đôi `H1..H5`) → **75 ghế**.
  * `Phòng 3 - 3D` (`Standard3D`, `8 hàng × 12 ghế đơn`, phụ thu `+20.000đ`): `A–D` Thường (`48`), `E–G` VIP (`36`), `H` Đôi (`6` ghế đôi `H1..H6`) → **90 ghế**.
  * `Phòng 4 - IMAX` (`IMAX`, `10 hàng × 12 ghế đơn`, phụ thu `+40.000đ`): `A–F` Thường (`72`), `G–I` VIP (`36`), `J` Đôi (`6` ghế đôi `J1..J6`) → **114 ghế**.
* Tổng cộng: **354 ghế** trên 4 phòng. Các suất chiếu và vé mẫu trong `SeedData.sql` vẫn giữ nguyên toàn vẹn khóa ngoại.

## 3. Cấu trúc File & Điều hướng
1. `CinemaManageProject/Helpers/FormatHelper.cs`: Bổ sung `FormatRoomType` và `FormatSeatType`.
2. `CinemaManageProject/Data/RoomData.cs`: Truy vấn SQLite chỉ đọc cho phòng (`LoadAllRooms`) và ghế (`LoadSeatsByRoom`), truy vấn lịch sử đơn hàng đặt vào ghế (`LoadSeatOrders` JOIN `Tickets`, `Orders`, `Showtimes`, `Movies`, `Users`), kiểm tra vé hiệu lực (`CountActiveTicketsForSeat`) và cập nhật `Seats.IsActive` (`SetSeatActive`).
3. `CinemaManageProject/ViewModels/Admin/AdminRoomsViewModel.cs`: Kế thừa `ViewModelBase`, chứa danh sách `Rooms`, danh sách hàng ghế `SeatRows` (`SeatRowGroup` gồm `RowLabel` + `Seats`), danh sách `SeatOrders`, thông tin `SelectedRoom`/`SelectedSeat`, và các method `SelectSeat`, `ToggleSelectedSeatAvailable`.
4. `CinemaManageProject/Views/Admin/AdminRoomsView.xaml` & `.xaml.cs`: Bố cục 2 cột Dark Theme dùng style trong `Themes/Controls.xaml` (không có form Thêm/Sửa/Xóa phòng).
5. `CinemaManageProject/ViewModels/MainViewModel.cs` & `CinemaManageProject/Views/MainWindow.xaml.cs`: Mở khóa mục `"rooms"` (`isAvailable: true`) và map `AdminRoomsViewModel` → `AdminRoomsView`.

## 4. Giao diện & Nghiệp vụ chi tiết (`AdminRoomsView`)
* **Cột trái (`370px`) — Danh sách Phòng & Thông số Cố định:**
  * Bảng danh sách phòng (`Tên phòng`, `Loại`, `Sức chứa`, `Trạng thái`).
  * Thẻ **Thông tin Phòng chiếu** (chế độ chỉ xem với nhãn `CỐ ĐỊNH`): `Loại phòng`, `Kích thước`, `Tình trạng ghế` (số khả dụng / bảo trì), `Phân bổ ghế` (Thường • VIP • Đôi), `Phụ thu phòng`, `Trạng thái phòng`.
* **Cột phải (`*`) — Sơ đồ Ghế theo Hàng & Kiểm tra Đơn hàng / Bảo trì Ghế:**
  * Chú thích màu sắc: `Thường (×1.00)`, `VIP (×1.25)`, `Ghế Đôi (×2.00)` (ô minh họa dài gấp đôi), `Bảo trì (Không cho đặt)`.
  * Dải `"MÀN HÌNH CHIẾU"` ở trên + sơ đồ ghế nhóm theo từng hàng (`SeatRows` với nhãn `A`, `B`... bên trái). Nút ghế đơn rộng `40px` (`Margin="3"` → `46px`), nút ghế đôi (`IsCouple == True`) rộng `86px` (`92px = 2 × 46px`), giúp hàng `5`/`6` ghế đôi căn thẳng hàng với hàng `10`/`12` ghế đơn.
  * **Panel Chi tiết Ghế & Kiểm tra Đơn hàng** (khi bấm vào 1 ghế):
    * Thông tin cố định của ghế: `SeatCode`, badge `SeatTypeDisplay`, `PositionText`, `Hệ số giá`, `Trạng thái`.
    * Nút hành động duy nhất: **`Đặt bảo trì (Không cho đặt)`** hoặc **`Mở lại khả dụng (Available)`**.
    * *Ràng buộc an toàn:* Nếu ghế đang có vé hiệu lực (`Booked`/`CheckedIn`) thuộc suất chiếu đang mở (`Scheduled`/`OpenForSale`), chặn chuyển sang bảo trì và báo rõ số vé đang giữ ghế đó.
    * Bảng **`SeatOrders`** hiển thị danh sách đơn hàng/vé đã từng đặt vào ghế đó (`Mã đơn`, `Mã vé`, `Phim`, `Suất chiếu`, `Khách hàng`, `Trạng thái`), hoặc thông báo `"Chưa có đơn hàng nào đặt vào ghế này"`.

## 5. Tiêu chí Nghiệm thu
* `dotnet build --no-restore CinemaManageProject.slnx -p:UseSharedCompilation=false` đạt `0 Warning(s)`, `0 Error(s)`.
* Kiểm thử runtime: đủ 4 phòng mẫu (`75, 75, 90, 114` ghế; `5, 5, 6, 6` ghế đôi), ghế `B1` Phòng 1 đang bảo trì và toggle được ON/OFF, ghế `E5` Phòng 1 hiển thị đơn đặt vé và bị chặn bảo trì do có vé đang hiệu lực, mở điều hướng `"rooms"` từ `MainViewModel` hoạt động bình thường.
