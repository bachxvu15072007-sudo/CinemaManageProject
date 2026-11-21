# Thiết kế Màn hình Hóa đơn & Doanh thu (`AdminOrdersView`)

## 1. Mục tiêu & Phạm vi (Phương án 1)
* **Mục tiêu:** Kích hoạt mục **Hóa đơn & Doanh thu** (`orders`) trên Sidebar của vai trò `Admin` để theo dõi tổng quan doanh thu, phân tích cơ cấu doanh thu theo **kênh bán / phương thức thanh toán / phim**, lọc tra cứu hóa đơn đa điều kiện và xem chi tiết từng hóa đơn kèm danh sách vé phát sinh.
* **Ngoài phạm vi (Non-goals):**
  * Chưa thực hiện thao tác ghi/sửa/hủy hóa đơn hay hoàn vé (`Cancelled` / `PartiallyRefunded`) — sẽ triển khai ở bước POS & Soát vé/Hoàn vé riêng.
  * Không thêm biểu đồ bên thứ ba hoặc thư viện chart/EF Core; dùng trực tiếp bảng và thanh tiến độ/tỷ lệ chuẩn WPF.
  * Không thay đổi Dashboard của `Staff` và `Customer`.

## 2. Phương án Kiến trúc & Dữ liệu
### 2.1. Tách 2 câu truy vấn SQLite gọn nhẹ (tránh N+1 query)
Trong `CinemaManageProject/Data/OrderData.cs`, nạp dữ liệu từ `Database/cinema.db` bằng `DatabaseHelper.OpenConnection()`:
1. **`LoadAllOrders()`:** Đọc toàn bộ `Orders` (`LEFT JOIN Customers c`, `LEFT JOIN Users u ON u.UserId = o.StaffUserId`), sắp xếp `o.CreatedAt DESC, o.OrderId DESC`, bao gồm cả hóa đơn của khách vãng lai (`CustomerId IS NULL`) và hóa đơn đã hủy (`Cancelled`).
2. **`LoadAllOrderTickets()`:** Đọc toàn bộ `Tickets` (`INNER JOIN Showtimes`, `Movies`, `Rooms`, `Seats`), sắp xếp `t.OrderId DESC, t.TicketId ASC` rồi gom nhóm theo `OrderId` trong bộ nhớ (`Dictionary<int, IReadOnlyList<OrderTicketDetailRow>>`). Cách này vừa giúp bấm chọn hóa đơn nào là hiện danh sách vé tức thì (không truy vấn lặp lại), vừa giúp tính ngay **Doanh thu theo Phim** khớp với tập hóa đơn đang lọc.

### 2.2. Quy tắc tính Doanh thu & Phân bổ theo Phim
* **Hóa đơn tính doanh thu (`IsRevenueEffective`):** Mọi hóa đơn có `Status != 'Cancelled'` (gồm `Paid` và `PartiallyRefunded`). Đơn `Cancelled` có đóng góp doanh thu = `0đ`, chỉ thống kê ở phần đếm đơn hủy và tổng tiền bị hủy.
* **Ngày tham chiếu:** Thống kê theo `CreatedAt` (đồng bộ 100% với `AdminDashboardViewModel.cs` đang dùng `date(CreatedAt) = date('now', 'localtime')`).
* **Thẻ KPI:**
  1. **Doanh thu hiệu lực (Lọc):** Tổng `TotalAmount` các đơn hiệu lực đang hiển thị + dòng phụ cố định `Toàn rạp: {Toàn rạp} • Hôm nay: {Hôm nay}` tính trên toàn bộ dữ liệu gốc.
  2. **Kênh Tại quầy (`Counter`):** Tổng doanh thu & số **hóa đơn hiệu lực** (`Status != 'Cancelled'`) tại quầy theo bộ lọc *(giữ thống nhất với `PaymentBreakdownRow`, ví dụ `32 đơn hiệu lực`)*.
  3. **Kênh Tự đặt (`SelfService`):** Tổng doanh thu & số **hóa đơn hiệu lực** (`Status != 'Cancelled'`) khách tự đặt theo bộ lọc *(ví dụ `65 đơn hiệu lực` trên tập seed, không tính đơn hủy `HD-...-0028`)*.
  4. **Tổng đơn & Vé hiệu lực (Lọc):** Tổng số hóa đơn khớp bộ lọc + số vé thực tế có `Ticket.Status != 'Cancelled'` (khớp logic `AdminDashboardViewModel.cs#L104`) + dòng phụ số đơn `Status == 'Cancelled'` và tổng `TotalAmount` của các đơn hủy đó.
* **Cơ cấu Thanh toán (`PaymentBreakdownRow`):** Luôn liệt kê đủ 4 phương thức (`Tiền mặt`, `Chuyển khoản`, `Thẻ`, `Ví điện tử`) với số đơn hiệu lực (`Status != 'Cancelled'`), doanh thu và tỷ lệ `%` (do đó tổng số đơn của 4 dòng luôn khớp đúng tổng đơn hiệu lực trên KPI #2 + #3).
* **Doanh thu theo Phim (`MovieRevenueRow`):** Tính trên các vé có `Ticket.Status != 'Cancelled'` thuộc hóa đơn hiệu lực đang lọc; phân bổ theo tỷ lệ thực thu dựa trên tổng giá gốc của **các vé còn hiệu lực trong đơn** (`activeSub = SUM(UnitPrice của vé != 'Cancelled')`; nếu `activeSub > 0` thì mỗi vé đóng góp `UnitPrice * (Orders.TotalAmount / activeSub)`, fallback `UnitPrice`). Cách tính này giữ đúng tổng doanh thu theo phim bằng tổng `Orders.TotalAmount` ngay cả khi hóa đơn vừa có `DiscountAmount > 0` vừa có `PartiallyRefunded` (hủy bớt vé trong khi `Orders.SubTotalAmount` vẫn lưu giá gốc ban đầu). Sắp xếp giảm dần theo doanh thu, rồi số vé giảm dần, rồi tên phim tăng dần.

### 2.3. Trường hợp ngoại lệ dữ liệu & Hiển thị chi tiết
* **Khách vãng lai (`CustomerId == NULL`):** Hiển thị `"Khách vãng lai"` (SĐT `"Không có tài khoản"`), vẫn cho tìm kiếm theo chuỗi `"vãng lai"`.
* **Đơn khách tự đặt (`StaffUserId == NULL`):** Hiển thị người xử lý là `"Khách tự đặt (App)"`.
* **Thanh toán tiền mặt vs điện tử (`HasCashDetails`):** Chỉ hiển thị hai dòng `Khách đưa` (`CashReceived`) và `Tiền thối` (`ChangeAmount`) khi `PaymentMethod == "Cash"`.
* **Hóa đơn `Cancelled` (`PaidAt == NULL`) & `PartiallyRefunded`:**
  * Nếu `PaidAt == NULL`: hiển thị `"Chưa thu tiền / Đã hủy"` ở mục thời gian thanh toán.
  * Trên bảng danh sách hóa đơn: cột `Số vé` hiển thị `{ActiveTicketCount}/{TotalTickets} vé` nếu có vé hủy lẻ (hoặc `{TotalTickets} vé` khi toàn vẹn).
  * Trong phiếu tính tiền chi tiết: nếu `SubTotalAmount - DiscountAmount > TotalAmount` (đơn hoàn một phần), tự hiển thị thêm dòng suy diễn `Đã hoàn / điều chỉnh: -{(SubTotalAmount - DiscountAmount - TotalAmount)}` để tổng cộng trên phiếu luôn khớp `TotalAmount`.
* **Khoảng ngày lọc (`OrderDateRangeFilterOption`, theo lịch địa phương):**
  * `All` (`Tất cả` — mặc định)
  * `Today` (`Hôm nay`: `CreatedAt.Date == DateTime.Today`)
  * `Last7Days` (`7 ngày gần đây` — 7 ngày lịch tính cả hôm nay: `CreatedAt.Date >= DateTime.Today.AddDays(-6)`)
  * `Last30Days` (`30 ngày gần đây` — 30 ngày lịch tính cả hôm nay: `CreatedAt.Date >= DateTime.Today.AddDays(-29)`)
* **Danh sách rỗng sau lọc:** `SelectedOrder = null`, `HasSelectedOrder = false`, hiện thông báo `"Không tìm thấy hóa đơn nào phù hợp với bộ lọc."`.

## 3. Thiết kế Giao diện (`AdminOrdersView.xaml`) & Điều hướng
Bám sát phong cách Dark Zinc trong `Themes/Controls.xaml` và vừa khung `MinWidth="1080"` của `MainWindow`:
1. **Header:** Icon `ReceiptIconGeometry` + tiêu đề `"Hóa đơn & Doanh thu"` + mô tả ngắn + badge `"TRA CỨU & THỐNG KÊ"`.
2. **4 thẻ KPI:** Chia đều 4 cột bằng `CardBorderStyle`.
3. **Thanh tìm kiếm & bộ lọc (2 hàng cân đối):**
   * Hàng 1: Ô `Tìm kiếm` (`OrderCode`, tên/SĐT khách, nhân viên, ghi chú) + `Khoảng ngày` (`170px`) + nút `Đặt lại` (`Height="38"`).
   * Hàng 2: 3 bộ lọc đều nhau (`Kênh đặt`, `Phương thức thanh toán`, `Trạng thái`).
4. **Bảng Danh sách Hóa đơn (Full chiều ngang):** `DataGrid` hiển thị `Mã hóa đơn`, `Thời gian`, `Khách hàng`, `Kênh`, `Thu ngân`, `Số vé`, `Thanh toán`, `Tổng tiền`, `Trạng thái`.
5. **Vùng chi tiết & Phân tích (2 cột bên dưới bảng):**
   * **Cột trái (`*`):** Khung **Chi tiết Hóa đơn đang chọn** (badges mã đơn/trạng thái/kênh, thông tin khách & thu ngân, phiếu tính tiền dùng đường rãnh vé `<Line StrokeDashArray="4 3" />`, bảng **Vé trong hóa đơn** với `TicketCode`, `MovieTitle`, `RoomAndSeat`, `ShowtimeDisplay`, `UnitPriceText`, `StatusDisplay`).
   * **Cột phải (`370px`):** Hai thẻ thống kê gọn theo bộ lọc: **Cơ cấu Phương thức thanh toán** và **Doanh thu theo Phim**.
6. **Điều hướng Shell (`MainViewModel.cs` & `MainWindow.xaml.cs`):** Mở khóa mục `"orders"` (`isAvailable: true`), khởi tạo/nạp lại `AdminOrdersViewModel` trong `SelectMenu`, và ánh xạ `AdminOrdersViewModel => new AdminOrdersView()` trong `ShowCurrentDashboard()`.

## 4. Cấu trúc File sẽ tạo / chỉnh sửa
1. Create: `CinemaManageProject/Data/OrderData.cs`
2. Create: `CinemaManageProject/ViewModels/Admin/AdminOrdersViewModel.cs`
3. Create: `CinemaManageProject/Views/Admin/AdminOrdersView.xaml` & `AdminOrdersView.xaml.cs`
4. Modify: `CinemaManageProject/ViewModels/MainViewModel.cs` & `CinemaManageProject/Views/MainWindow.xaml.cs`

## 5. Tiêu chí Nghiệm thu
* `dotnet build CinemaManageProject.slnx -p:UseSharedCompilation=false` đạt `0 Warning(s)`, `0 Error(s)`.
* Kiểm thử STA WPF + SQLite xác nhận: nạp đủ `98` hóa đơn và `160` vé, tổng doanh thu toàn rạp khớp `AdminDashboardViewModel` (`15.670.000đ`), tổng doanh thu theo phim & thanh toán khớp tổng đơn hiệu lực, nhận diện đúng khách vãng lai (`HD-...-0027`) và đơn hủy (`HD-...-0028`), lọc đúng theo từ khóa/ngày/kênh/PTTT/trạng thái, xử lý trạng thái rỗng an toàn, đo layout sạch ở `988×860` và `788×660`, điều hướng qua `MainViewModel`.
