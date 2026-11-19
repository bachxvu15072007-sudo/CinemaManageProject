# Nguyên tắc Dự án dành cho AI Agents (`AGENTS.md`)

Tài liệu này quy định các nguyên tắc bắt buộc khi làm việc trên repository **CinemaManageProject (CINEVERSE CINEMA)**. Mọi AI agent hoặc lập trình viên đóng góp code đều phải tuân thủ để giữ kiến trúc nhất quán, gọn nhẹ và không phá vỡ giao diện Zinc.

---

## 1. Công nghệ & Truy cập Dữ liệu
* **Nền tảng:** `.NET 10 Windows` (`net10.0-windows`), **WPF** (`UseWPF=true`), `Nullable=enable`, `ImplicitUsings=enable`.
* **CSDL:** Dùng trực tiếp **SQLite (`Microsoft.Data.Sqlite`)** thông qua `App.OpenDbConnection()` làm việc với `Database/cinema.db`.
* **Không tự ý thêm package / hạ tầng ngoài phạm vi:** Không thêm Entity Framework Core, `appsettings.json`, hoặc thư viện MVVM bên thứ ba trừ khi người dùng yêu cầu rõ ràng.
* **An toàn SQL:** Luôn dùng parameterized query (`@param`), bật `PRAGMA foreign_keys = ON;` (đã có trong `App.OpenDbConnection()`), tuân thủ đúng tên bảng, cột và ràng buộc `CHECK (...)` trong `CinemaManageProject/Database/CinemaManageDB.sql`.

## 2. Kiến trúc MVVM Gọn nhẹ & Tổ chức Thư mục
* **Vị trí file:**
  * Toàn bộ màn hình Window và UserControl đặt trong `CinemaManageProject/Views/` (không đặt file `.xaml` màn hình ở thư mục gốc project).
  * Toàn bộ ViewModel đặt trong `CinemaManageProject/ViewModels/`, kế thừa `ViewModelBase`.
  * Entity và Enum dùng lại trực tiếp từ `CinemaManageProject/Models/` (`CinemaEnums.cs` và 10 class model).
* **Tránh over-engineering:**
  * Không tạo thêm các lớp trung gian, framework MVVM rườm rà, converter thừa hoặc service tách nhỏ không cần thiết khi code hiện tại xử lý gọn ngay trong ViewModel/helper nhẹ.
  * Khi người dùng yêu cầu làm **phần giao diện** hoặc **1 màn hình cụ thể**, chỉ viết đúng phần đó; không tự ý code lan sang các module nghiệp vụ chưa được yêu cầu.
* **Code-behind tối giản (`.xaml.cs`):** Chỉ chứa `InitializeComponent()`, `switch` chọn màn hình con trong `MainWindow.xaml.cs`, chuyển tiếp sự kiện `Click` gọi method public trên ViewModel (không dùng `RelayCommand`/`ICommand`) và chuyển tiếp `PasswordBox.Password` (vì WPF không hỗ trợ binding 2 chiều `Password`).

## 3. Quy tắc Giao diện & Dark Theme (`Themes/Controls.xaml`)
* **Tập trung toàn bộ tài nguyên dùng chung về `Themes/Controls.xaml`:** Mọi `Style` control (`PrimaryButtonStyle`, `SecondaryButtonStyle`, `GhostButtonStyle`, `NavMenuButtonStyle`, `BadgeBorderStyle`, `CardBorderStyle`, ...) và converter dùng chung (`BoolToVis`) đặt hết trong một file `Themes/Controls.xaml` (nạp tại `App.xaml`). Các trang View chỉ viết giao diện XAML và logic sự kiện, giữ đủ tên `x:Key` rõ ràng.
* **Ghi mã màu trực tiếp trong `Controls.xaml` và View:**
  * Dùng trực tiếp bảng màu Dark Theme (nền `#09090B` / `#0F1011` / `#141516` / `#18181B`, chữ `#FAFAFA` / `#A1A1AA`, nhấn Indigo `#5E6AD2`, viền `#23252A` / `#3F3F46`) ngay trong từng `Style`/View và gọi `Style` dùng chung qua `{StaticResource ...Style}`.
* **Viết style chuẩn WPF, dễ đọc và dễ bảo trì:** Giữ style tối giản, không dùng `{DynamicResource}`, không thêm hiệu ứng gradient/glow hay `ControlTemplate` phức tạp không cần thiết.

## 4. Quy tắc Vòng đời & Nghiệp vụ Cốt lõi
* **Phiên làm việc:** Quản lý qua `App.CurrentUser` (`UserRole`: `Admin`, `Staff`, `Customer`; hồ sơ hội viên qua `User.CustomerProfile`).
* **Vòng đời cửa sổ (`App.xaml` / `App.xaml.cs`):** Sử dụng `ShutdownMode="OnExplicitShutdown"` để `ShowLoginWindow()`, `CompleteLogin()` và `Logout()` chuyển đổi mượt giữa `LoginWindow` và `Views.MainWindow` mà không thoát app nhầm hoặc để tiến trình chạy ngầm khi đóng cửa sổ.
* **Quy ước nghiệp vụ (cho các bước tiếp theo):**
  * Suất chiếu (`Showtimes`): `EndTime = StartTime + Movie.DurationMinutes + 15 phút` dọn phòng; kiểm tra trùng lịch trong cùng `RoomId`.
  * Sơ đồ ghế (`Seats` & `Tickets`): Chặn chọn ghế bảo trì (`IsActive = 0`) và ghế đã có vé trạng thái `Booked` hoặc `CheckedIn` trong cùng suất chiếu; kiểm tra lại trong `SqliteTransaction` trước khi ghi `Orders` và `Tickets`.
  * Giá vé cơ bản khớp dữ liệu mẫu: `UnitPrice = (Showtime.BaseTicketPrice + Room.BasePriceSurcharge) * Seat.PriceMultiplier`.

## 5. Kiểm chứng & Quy tắc Git
* **Bắt buộc xác minh sau khi sửa code:** Chạy `dotnet build CinemaManageProject.slnx -p:UseSharedCompilation=false` đạt `0 Warning(s)`, `0 Error(s)` và kiểm tra khởi chạy thực tế không phát sinh lỗi binding XAML hay ngoại lệ SQLite.
* **Git workflow:**
  * Dùng **Conventional Commits** (`feat(...)`, `fix(...)`, `docs(...)`, `refactor(...)`, `chore(...)`), tách mỗi đầu việc thành một commit gọn sạch.
  * Không bao giờ stage/commit thư mục build hay file tạm (`bin/`, `obj/`, `.vs/`, `*_wpftmp.csproj`).
  * Không ghi hoặc commit các file nháp từ thư mục scratch vào repository.
