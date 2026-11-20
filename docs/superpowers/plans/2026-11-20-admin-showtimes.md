# AdminShowtimes (Quản lý Lịch chiếu) Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task.

**Goal:** Xây dựng hoàn chỉnh màn hình **Quản lý Lịch chiếu (`showtimes`)** cho vai trò `Admin` với tính năng xem, lọc đa điều kiện, tạo/cập nhật suất chiếu tự tính `+15 phút` dọn phòng, chặn trùng lịch cùng phòng trong `SqliteTransaction` và bảo vệ suất đã phát sinh vé.

**Architecture:** Tách toàn bộ truy vấn & transaction SQLite vào `CinemaManageProject/Data/ShowtimeData.cs`, quản lý bộ lọc/validation/tính toán xem trước trong `AdminShowtimesViewModel.cs`, dựng giao diện 2 cột Dark Zinc trong `Views/Admin/AdminShowtimesView.xaml` và nối vào `MainViewModel` + `MainWindow.xaml.cs`.

**Tech Stack:** .NET 10 Windows (`net10.0-windows`), WPF, SQLite (`Microsoft.Data.Sqlite`).

**Spec:** [2026-11-20-admin-showtimes-design.md](../specs/2026-11-20-admin-showtimes-design.md)

## Global Constraints
* Chỉ dùng `Microsoft.Data.Sqlite` qua `DatabaseHelper.OpenConnection()`, `PRAGMA foreign_keys = ON;`, parameterized query, không thêm EF Core hay thư viện MVVM ngoài.
* `EndTime = StartTime + Movie.DurationMinutes + 15` phút; kiểm tra trùng lịch trên cùng `RoomId` với mọi suất có `Status != 'Cancelled'`.
* Không để warning hay error khi build; kiểm thử qua harness STA WPF + SQLite trên bản sao CSDL cô lập.

## Review Focus
* Suất nối tiếp chạm biên đúng `EndTime` của suất trước trong cùng phòng phải hợp lệ (`StartTime < @end AND EndTime > @start`).
* Đổi `Status` của chính suất đang sửa thành `Cancelled` hoặc chỉnh giờ của chính nó không được tự báo trùng với chính nó (`ShowtimeId != @sid`).
* Suất đã có vé `Booked`/`CheckedIn` (ví dụ suất #7) phải khóa đổi Phim/Phòng/Giờ/Giá trong cả UI và transaction `SaveShowtime`, nhưng vẫn cho phép chuyển trạng thái.
* Xóa suất đã có vé (kể cả vé `Cancelled`) phải bị chặn an toàn, tránh lỗi `FOREIGN KEY constraint failed`.
* Phim ở trạng thái `Ended` hoặc phòng tạm đóng nếu đã có trong lịch sử suất cũ vẫn hiển thị đúng khi chọn dòng cũ, nhưng không cho tạo suất mới.

---

### Task 1: Tầng Truy cập Dữ liệu `ShowtimeData`

**Files:**
- Create: `CinemaManageProject/Data/ShowtimeData.cs`

**Interfaces:**
- Records: `ShowtimeMovieOption`, `ShowtimeRoomOption`, `AdminShowtimeItem`, `ShowtimeSaveInput`, `ShowtimeSaveResult`.
- Methods: `LoadMovieOptions()`, `LoadRoomOptions()`, `LoadAllShowtimes()`, `SaveShowtime(int? showtimeId, ShowtimeSaveInput input)`, `CountShowtimeTickets(int showtimeId)`, `DeleteShowtime(int showtimeId)`, `SuggestFormat(string roomType, string movieLanguage)`.

- [ ] **Step 1:** Viết `CinemaManageProject/Data/ShowtimeData.cs` với đầy đủ parameterized query, transaction kiểm tra khóa ngoại/vé hiệu lực và kiểm tra trùng lịch `StartTime < @end AND EndTime > @start`.
- [ ] **Step 2:** Xác minh biên dịch dự án sạch không lỗi/cảnh báo.

### Task 2: `AdminShowtimesViewModel`

**Files:**
- Create: `CinemaManageProject/ViewModels/Admin/AdminShowtimesViewModel.cs`

**Interfaces:**
- Filter properties: `SearchKeyword`, `DateFilterOptions`, `SelectedDateFilter`, `RoomFilterOptions`, `SelectedRoomFilter`, `StatusFilterOptions`, `SelectedStatusFilterOption`, `ResultCountText`.
- Form & Preview properties: `Showtimes`, `SelectedShowtime`, `MovieOptions`, `RoomOptions`, `StatusOptions`, `SelectedMovieOption`, `SelectedRoomOption`, `ShowDateText`, `StartTimeText`, `FormatText`, `StandardBasePriceDisplay`, `SelectedStatusOption`, `IsEditMode`, `FormHeaderText`, `FormModeBadge`, `SelectedHasActiveTickets`, `CanEditScheduleCore`, `ActiveTicketsNoticeText`, `PreviewEndTimeText`, `PreviewPriceSummaryText`, `ErrorMessage`/`HasError`, `SuccessMessage`/`HasSuccess`.
- Methods: `LoadData()`, `ResetFilters()`, `StartNewShowtime()`, `ResetForm()`, `SaveShowtime()`, `CanDeleteSelectedShowtime(out string summary)`, `DeleteSelectedShowtime()`.

- [ ] **Step 1:** Viết `AdminShowtimesViewModel.cs` kế thừa `ViewModelBase`, tự cập nhật xem trước `EndTime` và giá quy đổi theo `Room.BasePriceSurcharge`, gợi ý định dạng khi tạo mới, và giữ lọc sau khi lưu/xóa.
- [ ] **Step 2:** Xác minh biên dịch sạch.

### Task 3: `AdminShowtimesView`, Style ComboBox Disabled & Điều hướng Shell

**Files:**
- Modify: `CinemaManageProject/Themes/Controls.xaml`
- Create: `CinemaManageProject/Views/Admin/AdminShowtimesView.xaml`
- Create: `CinemaManageProject/Views/Admin/AdminShowtimesView.xaml.cs`
- Modify: `CinemaManageProject/ViewModels/MainViewModel.cs`
- Modify: `CinemaManageProject/Views/MainWindow.xaml.cs`

- [ ] **Step 1:** Thêm trigger `IsEnabled="False"` cho `ModernComboBoxStyle` trong `Themes/Controls.xaml`.
- [ ] **Step 2:** Dựng giao diện 2 cột Dark Theme trong `Views/Admin/AdminShowtimesView.xaml` và code-behind tối giản trong `AdminShowtimesView.xaml.cs`.
- [ ] **Step 3:** Mở khóa item `"showtimes"` trong `MainViewModel.cs` và ánh xạ `AdminShowtimesViewModel => new AdminShowtimesView()` trong `MainWindow.xaml.cs`.
- [ ] **Step 4:** Chạy build sạch (`0 Warning(s)`, `0 Error(s)`) và kiểm thử STA WPF + SQLite (`CheckShowtimes.cs`) phủ toàn bộ mục Review Focus + hồi quy toàn bộ ứng dụng.
