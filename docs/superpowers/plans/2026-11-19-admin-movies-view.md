# AdminMoviesView (UI-Only) Implementation Plan

> **For agentic workers:** Use inline execution (`executing-plans`) for these 2 tightly coupled UI tasks.

**Goal:** Tạo giao diện 2 cột `AdminMoviesView` (chưa làm `AdminMoviesViewModel` hay đụng `ViewModels/`) theo đúng thiết kế Dark Theme trong `Controls.xaml`.

**Architecture:** Mở rộng `CinemaManageProject/Themes/Controls.xaml` thêm `FieldLabelTextStyle`, `ModernCheckBoxStyle`, `ModernComboBoxStyle` và hỗ trợ `VerticalContentAlignment` cho `ModernTextBoxStyle`. Tạo `CinemaManageProject/Views/Admin/AdminMoviesView.xaml` + `.xaml.cs` với bố cục 2 cột (trái: tìm kiếm + lọc trạng thái + `DataGrid`; phải: form chi tiết phim, khung xem trước poster, danh sách checkbox thể loại + ô thêm thể loại, cụm nút hành động) dùng `FallbackValue` để xem trước chuẩn trên XAML Designer và sẵn sàng nối binding với ViewModel ở bước sau.

**Tech Stack:** .NET 10 Windows (`net10.0-windows`), WPF, XAML.

**Spec:** [2026-11-19-admin-movies-design.md](../specs/2026-11-19-admin-movies-design.md) (giới hạn phạm vi đợt này chỉ ở tầng View theo yêu cầu người dùng).

---

### Task 1: Bổ sung Style Dark Theme dùng chung trong `Controls.xaml`

**Files:**
- Modify: `CinemaManageProject/Themes/Controls.xaml`

**Step 1:** Thêm `FieldLabelTextStyle`, hỗ trợ `VerticalContentAlignment` trên `ModernTextBoxStyle`, và viết `ModernCheckBoxStyle`, `ModernComboBoxStyle` (mã màu hex trực tiếp, không `DynamicResource`, không gradient/glow).
**Step 2:** Verify build không lỗi XAML (`dotnet build --no-restore CinemaManageProject.slnx -p:UseSharedCompilation=false`).

### Task 2: Tạo màn hình `AdminMoviesView`

**Files:**
- Create: `CinemaManageProject/Views/Admin/AdminMoviesView.xaml`
- Create: `CinemaManageProject/Views/Admin/AdminMoviesView.xaml.cs`

**Step 1:** Tạo `AdminMoviesView.xaml` theo bố cục 2 cột đã chốt, tái sử dụng toàn bộ style trong `Controls.xaml`, chuẩn bị sẵn đường dẫn `{Binding ...}` và `Click` handlers đặt tên rõ ràng.
**Step 2:** Tạo `AdminMoviesView.xaml.cs` tối giản (`InitializeComponent()` + các handler `Click` chờ nối ViewModel ở bước sau).
**Step 3:** Chạy `dotnet build --no-restore CinemaManageProject.slnx -p:UseSharedCompilation=false` xác nhận `0 Warning(s)`, `0 Error(s)`.
