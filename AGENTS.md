# Nguyên tắc Dự án dành cho AI Agents (`AGENTS.md`)

Tài liệu này quy định các nguyên tắc bắt buộc khi làm việc trên repository **CinemaManageProject (CINEVERSE CINEMA)**. Mọi AI agent hoặc lập trình viên đóng góp code đều phải tuân thủ để giữ kiến trúc nhất quán, gọn nhẹ và không phá vỡ giao diện Zinc.

---

## 1. Công nghệ, Thư viện & Truy cập Dữ liệu
* **Nền tảng:** `.NET 10 Windows` (`net10.0-windows`), **WPF** (`UseWPF=true`), `Nullable=enable`, `ImplicitUsings=enable`.
* **CSDL:** Dùng **SQLite (`Microsoft.Data.Sqlite` + micro-ORM `Dapper`)** thông qua `DatabaseHelper.OpenConnection()` làm việc với `Database/cinema.db`.
* **Các thư viện đã được duyệt & đang dùng trong dự án:**
  * `Microsoft.Data.Sqlite` + `Dapper`: truy vấn SQLite thuần có tham số hóa và ánh xạ DTO gọn nhẹ.
  * `CommunityToolkit.Mvvm`: dùng `ObservableObject`, `[ObservableProperty]`, `[NotifyPropertyChangedFor]` (Source Generator) để loại bỏ code lặp `INotifyPropertyChanged`.
  * `WPF-UI`: cung cấp Dark Theme, style control hiện đại (`ThemesDictionary`, `ControlsDictionary`) và bộ icon `ui:SymbolIcon` (`SymbolRegular`).
* **Quy tắc chủ động đề xuất thư viện khi lên ý tưởng (Bắt buộc):**
  * **Mỗi lần lên ý tưởng, thiết kế hoặc chuẩn bị viết tính năng/module mới**, AI agent phải **chủ động đề xuất các thư viện phù hợp** (nêu rõ công dụng, ưu/nhược điểm, và mức độ rút gọn code so với tự viết tay) để **người dùng xem xét và lựa chọn trước** khi triển khai.
  * Không tự ý cài thêm package mới (như EF Core, thư viện chart, PDF, barcode...) khi chưa được người dùng chọn/đồng ý.
* **An toàn SQL:** Luôn dùng SQL thuần có tham số hóa (`@param` qua Dapper anonymous/typed parameters), bật `PRAGMA foreign_keys = ON;` (đã có trong `DatabaseHelper.OpenConnection()`), tuân thủ đúng tên bảng, cột và ràng buộc `CHECK (...)` trong `CinemaManageProject/Database/CinemaManageDB.sql`.

## 2. Kiến trúc MVVM Gọn nhẹ & Tổ chức Thư mục
* **Vị trí file:**
  * Toàn bộ màn hình Window và UserControl đặt trong `CinemaManageProject/Views/` (không đặt file `.xaml` màn hình ở thư mục gốc project).
  * Toàn bộ ViewModel đặt trong `CinemaManageProject/ViewModels/`, khai báo `partial class` kế thừa `ViewModelBase` (`ObservableObject` của `CommunityToolkit.Mvvm`) và ưu tiên `[ObservableProperty]` / `[NotifyPropertyChangedFor]` / `partial void OnXxxChanged(...)` thay cho property boilerplate thủ công.
  * Các lớp truy cập dữ liệu SQLite dùng lại đặt trong `CinemaManageProject/Data/` (`MovieData`, `CustomerData`...), giữ gọn bằng static method + parameterized query qua Dapper.
  * Các hàm phụ trợ dùng chung đặt trong `CinemaManageProject/Helpers/` theo đúng chức năng (`DatabaseHelper` mở kết nối SQLite, `PasswordHelper` băm/xác thực `PBKDF2-HMAC-SHA256`, `FormatHelper` định dạng tiền/ngày/trạng thái, `PosterHelper` xử lý ảnh poster...).
  * Entity và Enum dùng lại trực tiếp từ `CinemaManageProject/Models/` (`CinemaEnums.cs` và 10 class model).
* **Tránh over-engineering:**
  * Không tạo thêm các lớp trung gian, container DI rườm rà, converter thừa hoặc service tách nhỏ không cần thiết khi code hiện tại xử lý gọn ngay trong ViewModel/Data/Helper.
  * Khi người dùng yêu cầu làm **phần giao diện** hoặc **1 màn hình cụ thể**, chỉ viết đúng phần đó; không tự ý code lan sang các module nghiệp vụ chưa được yêu cầu.
* **Code-behind tối giản (`.xaml.cs`):** Chỉ chứa `InitializeComponent()`, `switch` chọn màn hình con trong `MainWindow.xaml.cs`, chuyển tiếp sự kiện `Click` gọi method public trên ViewModel và chuyển tiếp `PasswordBox.Password` (vì WPF không hỗ trợ binding 2 chiều `Password`).

## 3. Quy tắc Giao diện & Dark Theme (`Themes/Controls.xaml`)
* **Tập trung toàn bộ tài nguyên dùng chung về `Themes/Controls.xaml`:** Nạp `<ui:ThemesDictionary Theme="Dark" />` và `<ui:ControlsDictionary />` của **WPF-UI** ngay trong `Themes/Controls.xaml` (nạp tại `App.xaml`), kế thừa style control mặc định của WPF-UI bằng `BasedOn` và giữ toàn bộ style dùng chung (`PrimaryButtonStyle`, `SecondaryButtonStyle`, `GhostButtonStyle`, `NavMenuButtonStyle`, `BadgeBorderStyle`, `CardBorderStyle`, `ModernDataGridStyle`, `CompactDataGridStyle`, `BookingSeatButtonStyle`, ...) cùng converter `BoolToVis` tại đây.
* **Icon giao diện dùng `WPF-UI` (`ui:SymbolIcon`):** Ưu tiên `<ui:SymbolIcon Symbol="..." />` (`Wpf.Ui.Controls.SymbolRegular`) thay cho tự viết chuỗi SVG `<Geometry>` thủ công.
* **Ghi mã màu trực tiếp trong `Controls.xaml` và View:**
  * Dùng trực tiếp bảng màu Dark Theme Zinc (nền `#09090B` / `#0F1011` / `#141516` / `#18181B`, chữ `#FAFAFA` / `#A1A1AA`, nhấn Indigo `#5E6AD2`, viền `#23252A` / `#3F3F46`) ngay trong từng `Style`/View và gọi `Style` dùng chung qua `{StaticResource ...Style}`.
* **Viết style chuẩn WPF, dễ đọc và dễ bảo trì:** Giữ style tối giản, không dùng `{DynamicResource}` và không viết lại `ControlTemplate` tay khi control WPF-UI đã đáp ứng sẵn.

## 4. Quy tắc Vòng đời & Nghiệp vụ Cốt lõi
* **Phiên làm việc & Bảo mật:** Quản lý qua `App.CurrentUser` (`UserRole`: `Admin`, `Staff`, `Customer`; hồ sơ hội viên qua `User.CustomerProfile`). Mọi mật khẩu lưu DB đều phải băm qua `PasswordHelper.HashPassword(...)` (`PBKDF2-HMAC-SHA256` kèm salt ngẫu nhiên) và xác thực bằng `PasswordHelper.VerifyPassword(...)`.
* **Vòng đời cửa sổ (`App.xaml` / `App.xaml.cs`):** Sử dụng `ShutdownMode="OnExplicitShutdown"` để `ShowLoginWindow()`, `CompleteLogin()` và `Logout()` chuyển đổi mượt giữa `LoginWindow` và `Views.MainWindow` mà không thoát app nhầm hoặc để tiến trình chạy ngầm khi đóng cửa sổ.
* **Quy ước nghiệp vụ cốt lõi:**
  * Suất chiếu (`Showtimes`): `EndTime = StartTime + Movie.DurationMinutes + 15 phút` dọn phòng; kiểm tra trùng lịch trong cùng `RoomId`.
  * Sơ đồ ghế (`Seats` & `Tickets`): Chặn chọn ghế bảo trì (`IsActive = 0`) và ghế đã có vé trạng thái `Booked` hoặc `CheckedIn` trong cùng suất chiếu; kiểm tra lại trong `SqliteTransaction` trước khi ghi `Orders` và `Tickets`.
  * Giá vé cơ bản khớp dữ liệu mẫu: `UnitPrice = (Showtime.BaseTicketPrice + Room.BasePriceSurcharge) * Seat.PriceMultiplier`.

## 5. Kiểm chứng & Quy tắc Git
* **Bắt buộc xác minh sau khi sửa code:** Chạy `dotnet build CinemaManageProject.slnx -p:UseSharedCompilation=false` đạt `0 Warning(s)`, `0 Error(s)` và kiểm tra khởi chạy thực tế không phát sinh lỗi binding XAML hay ngoại lệ SQLite.
* **Git workflow:**
  * Dùng **Conventional Commits** (`feat(...)`, `fix(...)`, `docs(...)`, `refactor(...)`, `chore(...)`), tách mỗi đầu việc thành một commit gọn sạch.
  * Không bao giờ stage/commit thư mục build hay file tạm (`bin/`, `obj/`, `.vs/`, `*_wpftmp.csproj`).
  * Không ghi hoặc commit các file nháp từ thư mục scratch vào repository.
