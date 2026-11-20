# AdminRooms (Phòng & Sơ đồ Ghế) Implementation Plan

> **For agentic workers:** Use inline execution (`executing-plans`) cleanly across these 3 tasks.

**Goal:** Xây dựng chức năng **Quản lý Phòng & Sơ đồ ghế (`rooms`)** cho `Admin` theo đúng bản chất cố định của phòng/ghế: 4 phòng mẫu (gồm 2 phòng thường 2D) với hàng ghế đôi ít hơn một nửa và hiển thị dài gấp đôi; bấm vào ghế xem thông tin cố định, kiểm tra đơn hàng đã đặt vào ghế và bật/tắt trạng thái bảo trì (`IsActive`).

**Architecture:** Cập nhật `SeedData.sql` & tạo lại `cinema.db` (4 phòng, 354 ghế), tách truy vấn SQLite đọc phòng/ghế/đơn đặt vé vào `CinemaManageProject/Data/RoomData.cs`, bổ sung nhãn vào `FormatHelper.cs`, viết `AdminRoomsViewModel.cs` và giao diện 2 cột Dark Theme trong `Views/Admin/AdminRoomsView.xaml`, nối vào `MainViewModel` + `MainWindow.xaml.cs`.

**Tech Stack:** .NET 10 Windows (`net10.0-windows`), WPF, SQLite (`Microsoft.Data.Sqlite`).

**Spec:** [2026-11-19-admin-rooms-design.md](../specs/2026-11-19-admin-rooms-design.md)

---

### Task 1: Dữ liệu Mẫu 4 Phòng & Tầng Truy cập `RoomData`

**Files:**
- Modify: `CinemaManageProject/Database/SeedData.sql`
- Regenerate: `CinemaManageProject/Database/cinema.db`
- Modify: `README.md`
- Modify: `CinemaManageProject/Helpers/FormatHelper.cs`
- Create: `CinemaManageProject/Data/RoomData.cs`

**Interfaces:**
- `FormatHelper.FormatRoomType(string raw)`, `FormatHelper.FormatSeatType(string raw)`
- `RoomData`: `LoadAllRooms()`, `LoadSeatsByRoom(int roomId)`, `LoadSeatOrders(int seatId)`, `CountActiveTicketsForSeat(int seatId)`, `SetSeatActive(int seatId, bool isActive)`.

### Task 2: `AdminRoomsViewModel`

**Files:**
- Create: `CinemaManageProject/ViewModels/Admin/AdminRoomsViewModel.cs`

**Interfaces:**
- `SeatRowGroup` (`RowLabel`, `Seats`), `SeatCellItem` (`IsCouple`, `VisualState`, `IsAvailable`, `IsSelected`, thông tin cố định của ghế).
- `AdminRoomsViewModel`: `Rooms`, `SelectedRoom`, `SeatRows`, `SeatOrders`, `SelectedSeat`, `HasSelectedSeat`, `HasNoSeatOrders`, `SeatActionLabel`, `SeatMapHeader`, `SeatMapSubtitle`, `SeatOrdersSummaryText`, `ErrorMessage`/`HasError`, `SuccessMessage`/`HasSuccess`, `SelectSeat(SeatCellItem? seat)`, `ToggleSelectedSeatAvailable()`.

### Task 3: `AdminRoomsView` & Điều hướng Shell

**Files:**
- Create: `CinemaManageProject/Views/Admin/AdminRoomsView.xaml`
- Create: `CinemaManageProject/Views/Admin/AdminRoomsView.xaml.cs`
- Modify: `CinemaManageProject/ViewModels/MainViewModel.cs`
- Modify: `CinemaManageProject/Views/MainWindow.xaml.cs`

**Verification:**
- `dotnet build --no-restore CinemaManageProject.slnx -p:UseSharedCompilation=false` đạt `0 Warning(s)`, `0 Error(s)`.
- Smoke test STA WPF + SQLite kiểm tra 4 phòng mẫu (`75, 75, 90, 114` ghế; `5, 5, 6, 6` ghế đôi; `IsCouple == true`), `B1` toggle ON/OFF, `E5` hiển thị đơn đặt vé và chặn bảo trì khi có vé hiệu lực, cùng điều hướng từ `MainViewModel`.
