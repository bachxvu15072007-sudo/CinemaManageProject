# AdminOrders (Hóa đơn & Doanh thu) Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Hoàn thiện màn hình **Hóa đơn & Doanh thu (`orders`)** cho vai trò `Admin` với thống kê doanh thu tổng quan/theo kênh/theo phương thức thanh toán/theo phim, bộ lọc đa điều kiện và xem chi tiết hóa đơn kèm danh sách vé phát sinh.

**Architecture:** Tách 2 truy vấn SQLite đọc trước toàn bộ đơn và vé nhóm theo `OrderId` vào `CinemaManageProject/Data/OrderData.cs` để tránh N+1 query; gom logic lọc ngày, tính 4 KPI, tổng hợp cơ cấu thanh toán & doanh thu theo phim vào `AdminOrdersViewModel.cs`; dựng giao diện Dark Zinc 2 hàng lọc + bảng `DataGrid` full chiều ngang + 2 cột chi tiết/phân tích trong `AdminOrdersView.xaml` và nối vào `MainViewModel` + `MainWindow.xaml.cs`.

**Tech Stack:** .NET 10 Windows (`net10.0-windows`), WPF, SQLite (`Microsoft.Data.Sqlite`), `AGENTS.md` Dark Zinc Theme.

**Spec:** [2026-11-21-admin-orders-design.md](../specs/2026-11-21-admin-orders-design.md)

## Global Constraints
* Dùng `Microsoft.Data.Sqlite` qua `DatabaseHelper.OpenConnection()`, `PRAGMA foreign_keys = ON;`, parameterized query, không thêm EF Core hay thư viện chart/MVVM bên thứ ba.
* Tái sử dụng `FormatHelper.*`, `DatabaseHelper.*`, các `Style` trong `Themes/Controls.xaml` (`CardBorderStyle`, `BadgeBorderStyle`, `ModernTextBoxStyle`, `ModernComboBoxStyle`, `ModernDataGridStyle`, `SecondaryButtonStyle`, `CinemaVectorIconStyle`, `ReceiptIconGeometry`, `CinemaTicketIconGeometry`).
* Màn hình chỉ đọc (`Method = Phương án 1`): không chỉnh sửa dữ liệu `Orders`/`Tickets`/`Customers`.
* Code-behind `.xaml.cs` tối giản: chỉ `InitializeComponent()` và chuyển tiếp `Click` sang method public trên ViewModel.
* Build đạt `0 Warning(s)`, `0 Error(s)` và kiểm thử qua harness STA WPF + SQLite trên bản sao DB cô lập.

## Review Focus
* **Hóa đơn `PartiallyRefunded` / có `DiscountAmount > 0`:** Phải chia tỷ lệ doanh thu phim theo tổng `UnitPrice` của **các vé chưa hủy (`Ticket.Status != 'Cancelled'`)** thuộc đơn đó (`activeSub`), không chia theo `Orders.SubTotalAmount` ban đầu; đồng thời phiếu chi tiết phải hiện thêm dòng `Đã hoàn / điều chỉnh` khi `SubTotalAmount - DiscountAmount > TotalAmount`.
* **Đồng bộ KPI & Bảng Cơ cấu Thanh toán:** KPI Kênh Tại quầy / Tự đặt và bảng `PaymentBreakdownRow` đều chỉ đếm số **hóa đơn hiệu lực (`Status != 'Cancelled'`)** để tổng khớp chính xác (`32 + 65 = 97` đơn = `15.670.000đ`), còn đơn `Cancelled` (`HD-...-0028`, `120.000đ`) chỉ nằm ở KPI tổng đơn/đơn hủy.
* **Mốc lọc ngày theo lịch địa phương:** `Today` (`== Today`), `Last7Days` (`>= Today.AddDays(-6)`), `Last30Days` (`>= Today.AddDays(-29)`), dựa trên `CreatedAt` khớp `AdminDashboardViewModel`.
* **Khách vãng lai & Đơn tự đặt (`NULL` foreign keys):** Đơn `CustomerId IS NULL` (`#27`) hiển thị `"Khách vãng lai"` / `"Không có tài khoản"` và tìm được qua từ khóa; đơn `SelfService` (`StaffUserId IS NULL`) hiển thị `"Khách tự đặt (App)"`; dòng tiền mặt (`CashReceived` / `ChangeAmount`) chỉ hiện khi `PaymentMethod == "Cash"`; đơn hủy có `PaidAt IS NULL` hiển thị `"Chưa thu tiền / Đã hủy"`.
* **Trạng thái rỗng & Độ rộng tối thiểu (`MinWidth="1080"`):** Khi bộ lọc trả về `0` đơn, `SelectedOrder = null`, ẩn chi tiết hóa đơn sạch không lỗi binding, hiển thị thông báo rỗng và giữ bố cục 2 hàng lọc không tràn ngang ở viewport `788×660`.

---

### Task 1: Tầng Truy cập Dữ liệu `OrderData.cs`

**Files:**
- Create: `CinemaManageProject/Data/OrderData.cs`

**Interfaces:**
- Records (Produces):
  - `AdminOrderSummaryRow(int OrderId, string OrderCode, int? StaffUserId, string StaffDisplay, int? CustomerId, string CustomerName, string CustomerPhone, string BookingChannel, string BookingChannelDisplay, int TotalTickets, int ActiveTicketsCount, decimal SubTotalAmount, int PointsUsed, decimal DiscountAmount, decimal TotalAmount, string PaymentMethod, string PaymentDisplay, decimal CashReceived, decimal ChangeAmount, string Status, string StatusDisplay, string NoteText, string CreatedAtSql, string CreatedAtDisplay, string PaidAtDisplay)`
    - Computed properties: `IsRevenueEffective`, `IsCancelled`, `HasCashDetails`, `RefundAdjustmentAmount`, `HasRefundAdjustment`, `HasDiscountOrPoints`, `TotalTicketsText`, `TotalAmountText`, `SubTotalAmountText`, `DiscountSummaryText`, `RefundAdjustmentText`, `CashReceivedText`, `ChangeAmountText`, `CustomerSearchLabel`.
  - `OrderTicketDetailRow(int TicketId, int OrderId, string TicketCode, int MovieId, string MovieTitle, string RoomName, string SeatCode, string ShowtimeStartSql, decimal UnitPrice, string Status)`
    - Computed properties: `RoomAndSeat`, `ShowtimeDisplay`, `UnitPriceText`, `StatusDisplay`, `IsEffective`.
- Methods (Produces):
  - `public static List<AdminOrderSummaryRow> LoadAllOrders()`
  - `public static Dictionary<int, IReadOnlyList<OrderTicketDetailRow>> LoadAllOrderTickets()`

- [ ] **Step 1: Viết kiểm thử RED trong `scratch/CheckOrders.cs`** kiểm tra `OrderData.LoadAllOrders()` trả về `98` đơn (gồm đơn vãng lai `HD-...-0027` và đơn hủy `HD-...-0028`) và `OrderData.LoadAllOrderTickets()` trả về tổng `160` vé khớp `ActiveTicketsCount`.
- [ ] **Step 2: Chạy kiểm thử RED** xác nhận lỗi biên dịch vì `OrderData` chưa tồn tại.
- [ ] **Step 3: Tạo `CinemaManageProject/Data/OrderData.cs`** cài đặt `AdminOrderSummaryRow`, `OrderTicketDetailRow`, `LoadAllOrders()` và `LoadAllOrderTickets()`.
- [ ] **Step 4: Chạy build + kiểm thử Task 1** xác nhận PASS (`TASK1_ORDER_DATA_OK`).

---

### Task 2: `AdminOrdersViewModel.cs`

**Files:**
- Create: `CinemaManageProject/ViewModels/Admin/AdminOrdersViewModel.cs`

**Interfaces:**
- Consumes: `OrderData.LoadAllOrders`, `OrderData.LoadAllOrderTickets`, `FormatHelper.FormatVnd`
- Records (Produces):
  - `OrderFilterOption(string Value, string Label)`
  - `PaymentBreakdownRow(string MethodKey, string MethodLabel, int OrderCount, decimal RevenueAmount, string RevenueText, string ShareText, double ShareRatio)`
  - `MovieRevenueRow(int MovieId, string MovieTitle, int TicketCount, decimal RevenueAmount, string RevenueText, string TicketCountText, string ShareText)`
- Properties & Methods (Produces):
  - Filters: `SearchKeyword`, `DateRangeOptions`, `SelectedDateRangeOption`, `ChannelFilterOptions`, `SelectedChannelFilterOption`, `PaymentFilterOptions`, `SelectedPaymentFilterOption`, `StatusFilterOptions`, `SelectedStatusFilterOption`
  - KPI & Counters: `FilteredRevenueText`, `GlobalRevenueSubText`, `CounterRevenueText`, `CounterOrdersSubText`, `SelfServiceRevenueText`, `SelfServiceOrdersSubText`, `FilteredOrdersTicketsText`, `CancelledOrdersSubText`, `ResultCountText`
  - Selection & Breakdowns: `Orders`, `SelectedOrder`, `HasSelectedOrder`, `HasNoOrders`, `SelectedOrderTickets`, `HasNoSelectedOrderTickets`, `PaymentBreakdowns`, `MovieRevenues`, `HasNoMovieRevenues`
  - Public methods: `LoadData()`, `ResetFilters()`

- [ ] **Step 1: Mở rộng `scratch/CheckOrders.cs` với kiểm thử RED cho Task 2** phủ toàn bộ KPI (`15.670.000đ`), tổng hợp kênh (`32 + 65 = 97`), cơ cấu thanh toán, phân bổ doanh thu phim (bao gồm test mô phỏng `PartiallyRefunded` trên DB cô lập), lọc ngày (`Today`, `Last7Days`, `Last30Days`), lọc từ khóa `"vãng lai"`, trạng thái `Cancelled`, trạng thái rỗng và `ResetFilters()`.
- [ ] **Step 2: Chạy kiểm thử RED** xác nhận lỗi biên dịch vì `AdminOrdersViewModel` chưa tồn tại.
- [ ] **Step 3: Tạo `CinemaManageProject/ViewModels/Admin/AdminOrdersViewModel.cs`** cài đặt lọc, 4 KPI, phân tích thanh toán 4 kênh, phân bổ doanh thu phim theo `activeSub` và chọn/cập nhật vé chi tiết.
- [ ] **Step 4: Chạy build + kiểm thử Task 2** xác nhận PASS (`TASK2_ORDERS_VM_OK`).

---

### Task 3: Giao diện `AdminOrdersView`, Nối Shell & Kiểm thử Tổng thể

**Files:**
- Create: `CinemaManageProject/Views/Admin/AdminOrdersView.xaml`
- Create: `CinemaManageProject/Views/Admin/AdminOrdersView.xaml.cs`
- Modify: `CinemaManageProject/ViewModels/MainViewModel.cs:122,190-203`
- Modify: `CinemaManageProject/Views/MainWindow.xaml.cs:40-46`

**Interfaces:**
- Consumes: `AdminOrdersViewModel` (`Orders`, `SelectedOrder`, `HasSelectedOrder`, `HasNoOrders`, `SelectedOrderTickets`, `PaymentBreakdowns`, `MovieRevenues`, `HasNoMovieRevenues`, các filter & KPI properties, `ResetFilters()`) và styles/icons trong `Themes/Controls.xaml`
- Code-behind: `OnResetFiltersClick` gọi `_viewModel.ResetFilters()` (nếu `DataContext is AdminOrdersViewModel vm`).

- [ ] **Step 1: Mở rộng `scratch/CheckOrders.cs` với kiểm thử RED cho Task 3** kiểm tra điều hướng `MainViewModel.SelectMenu("orders")` trỏ tới `AdminOrdersViewModel`, render `AdminOrdersView` tại `988×860` và `788×660`, xác nhận hiển thị `OrderDetailTicketsGrid` / phiếu tính tiền, và xuất ảnh `orders_render.png`.
- [ ] **Step 2: Chạy kiểm thử RED** xác nhận thất bại ở `"orders"` chưa mở khóa / thiếu `AdminOrdersView`.
- [ ] **Step 3: Tạo `AdminOrdersView.xaml`, `AdminOrdersView.xaml.cs` và cập nhật `MainViewModel.cs` + `MainWindow.xaml.cs`** (bật `isAvailable: true` cho `"orders"`, thêm `case "orders"` trong `SelectMenu` và mapping `AdminOrdersViewModel => new AdminOrdersView()`).
- [ ] **Step 4: Chạy build sạch (`0 Warning(s)`, `0 Error(s)`), kiểm tra ảnh `orders_render.png`, chạy `CheckOrders` + toàn bộ regression (`CheckCustomerPosterStrip`, `CheckDashboards`, `CheckShowtimes`, `CheckRooms`, `CheckFilters`, `SmokeTest`) và commit.**
