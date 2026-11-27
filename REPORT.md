# Báo cáo Kiến trúc & Mã nguồn theo Thư mục — CINEVERSE CINEMA (`CinemaManageProject`)

Tài liệu này mô tả toàn bộ mã nguồn của dự án **CINEVERSE CINEMA** theo cấu trúc **từng thư mục thực tế** trên nhánh `main`: mỗi phần trình bày **công nghệ & thư viện dùng chung**, **tổng quát kiến trúc thư mục**, rồi đi xuống **chi tiết từng tệp tin**.

### Sơ đồ tổng thể 4 tầng & Công nghệ cốt lõi
```text
[Views (.xaml / .xaml.cs)]  <── Data Binding / Click ──>  [ViewModels (CommunityToolkit.Mvvm)]
        │                                                              │
   Styles/Icons                                                  Static methods
        ▼                                                              ▼
[Themes/Controls.xaml (WPF-UI)]                     [Data (Dapper) + Helpers (PBKDF2/Format)]
                                                                       │
                                                              SQL tham số hóa
                                                                       ▼
                                                       [Database/cinema.db (SQLite 3)]
```

| Thành phần | Công nghệ / Thư viện | Phiên bản | Vai trò chính |
| :--- | :--- | :---: | :--- |
| Nền tảng & UI | `.NET 10 SDK` (`net10.0-windows`, **chỉ hỗ trợ Windows 10/11**), `WPF` | `10.0` | Ứng dụng desktop Windows, giao diện XAML, `Nullable=enable`, `ImplicitUsings=enable` |
| Giao diện & Icon | `WPF-UI` (`Wpf.Ui`) | `4.3.0` | Dark Theme, style control hiện đại (`ThemesDictionary`, `ControlsDictionary`), icon `ui:SymbolIcon` |
| Kiến trúc MVVM | `CommunityToolkit.Mvvm` | `8.4.2` | `ObservableObject`, Source Generator `[ObservableProperty]` & `[NotifyPropertyChangedFor]` |
| CSDL & Micro-ORM | `Microsoft.Data.Sqlite` + `Dapper` | `10.0.15` / `2.1.100` | Lưu trữ cục bộ SQLite, bật `PRAGMA foreign_keys = ON`, SQL tham số hóa & ánh xạ DTO gọn nhẹ |
| Kiểm thử tự động | `xUnit` + `Microsoft.NET.Test.Sdk` | `2.9.3` / `17.14.1` | Kiểm thử tích hợp đăng nhập, đặt vé, chống đặt trùng ghế và hủy vé (`dotnet test`) |
| Bảo mật & Hệ thống | `PBKDF2-HMAC-SHA256` + `dwmapi.dll` (DWM) | .NET BCL / Win32 | Băm mật khẩu kèm salt ngẫu nhiên 16 byte; bật thanh tiêu đề tối thuần Windows 10/11 |

---

## Mục lục theo Thư mục
1. [Thư mục Gốc Repository & Khởi chạy Ứng dụng (`/` và `CinemaManageProject/`)](#1-thư-mục-gốc-repository--khởi-chạy-ứng-dụng--và-cinemamanageproject)
2. [Thư mục `CinemaManageProject/Database/` — Cơ sở dữ liệu SQLite](#2-thư-mục-cinemamanageprojectdatabase--cơ-sở-dữ-liệu-sqlite)
3. [Thư mục `CinemaManageProject/Models/` — Thực thể Dữ liệu & Enum](#3-thư-mục-cinemamanageprojectmodels--thực-thể-dữ-liệu--enum)
4. [Thư mục `CinemaManageProject/Helpers/` — Các lớp Phụ trợ Dùng chung](#4-thư-mục-cinemamanageprojecthelpers--các-lớp-phụ-trợ-dùng-chung)
5. [Thư mục `CinemaManageProject/Data/` — Tầng Truy cập Dữ liệu SQLite (Dapper)](#5-thư-mục-cinemamanageprojectdata--tầng-truy-cập-dữ-liệu-sqlite-dapper)
6. [Thư mục `CinemaManageProject/Themes/` — Hệ thống Giao diện Dark Theme Zinc](#6-thư-mục-cinemamanageprojectthemes--hệ-thống-giao-diện-dark-theme-zinc)
7. [Thư mục `CinemaManageProject/Posters/` — Tài nguyên Ảnh Poster Phim](#7-thư-mục-cinemamanageprojectposters--tài-nguyên-ảnh-poster-phim)
8. [Thư mục `CinemaManageProject/ViewModels/` — Tầng Logic Giao diện (MVVM)](#8-thư-mục-cinemamanageprojectviewmodels--tầng-logic-giao-diện-mvvm)
9. [Thư mục `CinemaManageProject/Views/` — Tầng Màn hình XAML & Code-behind](#9-thư-mục-cinemamanageprojectviews--tầng-màn-hình-xaml--code-behind)
10. [Thư mục `CinemaManageProject.Tests/` — Kiểm thử Tự động (`xUnit`)](#10-thư-mục-cinemamanageprojecttests--kiểm-thử-tự-động-xunit)
11. [Thư mục `docs/` — Sơ đồ Use Case, ERD, Ảnh Demo & Đặc tả Thiết kế](#11-thư-mục-docs--sơ-đồ-use-case-erd-ảnh-demo--đặc-tả-thiết-kế)
12. [Phân tích Chuyên sâu: Giao dịch Đặt ghế, Ràng buộc Khóa ngoại & Phân quyền](#12-phân-tích-chuyên-sâu-giao-dịch-đặt-ghế-ràng-buộc-khóa-ngoại--phân-quyền)

---

## 1. Thư mục Gốc Repository & Khởi chạy Ứng dụng (`/` và `CinemaManageProject/`)

### Công nghệ & Thư viện dùng chung
- **`.NET 10 SDK` & `MSBuild` (`slnx`, `.csproj`):** Cấu hình solution hiện đại định dạng XML rút gọn `.slnx`, biên dịch `WinExe` WPF trên `net10.0-windows`, quản lý 4 gói NuGet và quy tắc sao chép tài nguyên (`PreserveNewest`).
- **WPF `Application` (`ShutdownMode="OnExplicitShutdown"`):** Kiểm soát vòng đời cửa sổ chủ động khi chuyển đổi giữa `LoginWindow` và `MainWindow`.
- **Win32 P/Invoke (`dwmapi.dll` — `DwmSetWindowAttribute`):** Kích hoạt thanh tiêu đề tối gốc Windows 10/11 (attribute `20`, màu caption `35`, màu viền `34`) trên `Window` chuẩn mà không gặp lỗi vẽ lại nền trắng của `WindowChrome`.

### Tổng quát thư mục
Khu vực gốc chịu trách nhiệm khai báo cấu hình build, danh sách gói phụ thuộc, tài liệu vận hành/quy chuẩn phát triển và điểm vào khởi chạy ứng dụng (`App`). Mọi màn hình giao diện đều đã được gom vào `Views/`, giúp thư mục gốc gọn và rõ vai trò điều phối.

### Chi tiết từng file
- **[CinemaManageProject.slnx](CinemaManageProject.slnx)** *(3 dòng)*: Tệp solution định dạng mới của .NET 10, liên kết trực tiếp tới `CinemaManageProject/CinemaManageProject.csproj`.
- **[CinemaManageProject/CinemaManageProject.csproj](CinemaManageProject/CinemaManageProject.csproj)** *(33 dòng)*: Khai báo `TargetFramework=net10.0-windows`, `UseWPF=true`, `Nullable=enable`, `ImplicitUsings=enable`; tham chiếu 4 gói NuGet (`CommunityToolkit.Mvvm 8.4.2`, `Dapper 2.1.100`, `Microsoft.Data.Sqlite 10.0.15`, `WPF-UI 4.3.0`) và cấu hình `CopyToOutputDirectory=PreserveNewest` cho `Database\cinema.db`, `CinemaManageDB.sql`, `SeedData.sql` cùng toàn bộ `Posters\*`.
- **[CinemaManageProject/App.xaml](CinemaManageProject/App.xaml)** *(8 dòng)*: Khai báo `ShutdownMode="OnExplicitShutdown"` (không tự tắt ứng dụng khi đóng cửa sổ tạm) và nạp duy nhất một từ điển tài nguyên dùng chung `<ResourceDictionary Source="Themes/Controls.xaml" />`.
- **[CinemaManageProject/App.xaml.cs](CinemaManageProject/App.xaml.cs)** *(107 dòng)*:
  - Giữ phiên người dùng đang đăng nhập qua `public static User? CurrentUser`.
  - Điều phối vòng đời 2 cửa sổ bằng `OnStartup()`, `ShowLoginWindow()`, `CompleteLogin()`, `Logout()` kèm cờ `_isSwitching` để đóng cửa sổ cũ mà không thoát nhầm tiến trình.
  - Cung cấp `ApplyDarkTitleBar(Window)` gọi `DwmSetWindowAttribute` (`#0F1011` nền caption, `#23252A` viền) cho cả `LoginWindow` và `MainWindow`.
- **[CinemaManageProject/AssemblyInfo.cs](CinemaManageProject/AssemblyInfo.cs)** *(10 dòng)*: Khai báo `[assembly: ThemeInfo(ResourceDictionaryLocation.None, ResourceDictionaryLocation.SourceAssembly)]` chuẩn của WPF.
- **[.gitignore](.gitignore)** *(59 dòng)*: Loại trừ thư mục build (`bin/`, `obj/`, `.vs/`), file tạm `*_wpftmp.csproj` và dữ liệu phát sinh không đưa lên Git.
- **[AGENTS.md](AGENTS.md)** *(51 dòng)*: Quy tắc kiến trúc bắt buộc cho lập trình viên và AI agent (công nghệ đã duyệt, quy tắc chủ động đề xuất thư viện trước khi làm tính năng mới, chuẩn MVVM gọn nhẹ, quy tắc Dark Theme Zinc, quy ước nghiệp vụ và kiểm chứng build/Git).
- **[README.md](README.md)** *(92 dòng)*: Tài liệu giới thiệu dự án, tóm tắt 13 màn hình theo 3 vai trò, hướng dẫn build/chạy, bảng tài khoản demo và lộ trình mở rộng (hệ khuyến nghị phim).
- **[REPORT.md](REPORT.md)**: Tài liệu báo cáo tổng quan và chi tiết kiến trúc mã nguồn theo từng thư mục (chính tệp này).

---

## 2. Thư mục `CinemaManageProject/Database/` — Cơ sở dữ liệu SQLite

### Công nghệ & Thư viện dùng chung
- **SQLite 3** (`PRAGMA foreign_keys = ON;`): Cơ sở dữ liệu quan hệ nhúng dạng tệp đơn, không cần cài server ngoài.
- **Ràng buộc toàn vẹn mức CSDL:** `PRIMARY KEY AUTOINCREMENT`, khóa ghép `PRIMARY KEY (MovieId, GenreId)`, `UNIQUE`, `CHECK (...)` cho toàn bộ giá trị enum dạng chuỗi, khóa ngoại với đủ 3 chính sách `ON DELETE CASCADE`, `ON DELETE RESTRICT` và `ON DELETE SET NULL`.
- **Biểu thức thời gian SQLite:** `datetime('now', 'localtime', ...)` và `date('now', 'localtime', ...)` đảm bảo dữ liệu mẫu luôn khớp ngày hiện tại khi khởi tạo.
- **Mật khẩu băm `PBKDF2-HMAC-SHA256`:** Lưu sẵn chuỗi băm có salt riêng cho từng tài khoản trong dữ liệu mẫu.

### Tổng quát thư mục
Chứa kịch bản tạo lược đồ 10 bảng chuẩn hóa cho 6 miền nghiệp vụ cốt lõi (Tài khoản/Khách hàng, Phim/Thể loại, Phòng/Ghế, Suất chiếu, Hóa đơn, Vé), kịch bản nạp dữ liệu mẫu phong phú (đã thiết kế sẵn cụm sở thích xem phim cho bài toán khuyến nghị) và tệp CSDL chạy thực tế `cinema.db`.

### Chi tiết từng file
- **[CinemaManageDB.sql](CinemaManageProject/Database/CinemaManageDB.sql)** *(129 dòng)*: Định nghĩa trọn bộ 10 bảng:
  1. `Users`: tài khoản đăng nhập (`Admin`, `Staff`, `Customer`), trạng thái khóa `IsActive`, `LastLoginAt`.
  2. `Customers`: hồ sơ hội viên (`UserId` nullable unique cho phép cả khách App lẫn hội viên mở tại quầy), `Phone` unique, `LoyaltyPoints`, `TotalSpent`, `Tier` (`Member`/`Silver`/`Gold`/`Platinum`).
  3. `Genres` & 4. `Movies` & 5. `MovieGenres`: danh mục phim (`AgeRating` `P`/`K`/`T13`/`T16`/`T18`, `Status` `ComingSoon`/`NowShowing`/`Ended`) và quan hệ nhiều-nhiều với thể loại (`ON DELETE CASCADE`).
  6. `Rooms` & 7. `Seats`: 4 loại phòng (`Standard2D`/`Standard3D`/`IMAX`/`VIP`), phụ thu `BasePriceSurcharge`, sơ đồ ghế (`Standard`/`VIP`/`Couple`, hệ số `PriceMultiplier`, tọa độ `RowIndex`/`ColIndex`, cờ bảo trì `IsActive`).
  8. `Showtimes`: lịch chiếu (`StartTime`, `EndTime`, `BaseTicketPrice`, `Format`, `Status` `Scheduled`/`OpenForSale`/`Completed`/`Cancelled`, `CreatedByUserId`).
  9. `Orders`: hóa đơn (`OrderCode` unique, `BookingChannel` `Counter`/`SelfService`, `PaymentMethod` `Cash`/`BankTransfer`/`Card`/`EWallet`, điểm đổi thưởng, tiền khách đưa/tiền thối, trạng thái `Paid`/`Cancelled`/`PartiallyRefunded`).
  10. `Tickets`: từng vé (`TicketCode` unique, `UnitPrice`, `Status` `Booked`/`CheckedIn`/`Cancelled`, `CheckedInAt`, `CheckedInByUserId`).
- **[SeedData.sql](CinemaManageProject/Database/SeedData.sql)** *(515 dòng)*: Nạp bộ dữ liệu mẫu đồng nhất trong 1 transaction:
  - **29 tài khoản** (`admin`, `staff1..3`, `khach1..25`) đều đã băm `PBKDF2$100000$...` với salt riêng biệt (mật khẩu demo: `Admin@123`, `Staff@123`, `Khach@123`).
  - **25 hồ sơ hội viên** với lịch sử chi tiêu và cụm sở thích xem phim rõ rệt (`95` cặp khách–phim riêng biệt).
  - **10 thể loại & 15 phim** (`10` phim `NowShowing`, `5` phim `ComingSoon`), gán đầy đủ poster trong `Posters/`.
  - **4 phòng chiếu & 354 ghế** (hàng cuối là ghế đôi `Couple` hệ số `2.00` nên số lượng bằng 1/2 hàng thường; ghế `B1` Phòng 1 đặt bảo trì `IsActive = 0` để kiểm thử).
  - **28 suất chiếu** (`18` mở bán + `10` đã chiếu quá khứ, giá gốc đồng nhất `80.000đ`, đã cộng `+15` phút dọn phòng), **98 hóa đơn** và **160 vé mẫu** đủ 3 trạng thái `Booked`, `CheckedIn`, `Cancelled`.
- **[cinema.db](CinemaManageProject/Database/cinema.db)** *(168 KB)*: Tệp SQLite nhị phân dựng sẵn từ `CinemaManageDB.sql` + `SeedData.sql`, tự động copy vào thư mục chạy để ứng dụng hoạt động ngay.

---

## 3. Thư mục `CinemaManageProject/Models/` — Thực thể Dữ liệu & Enum

### Công nghệ & Thư viện dùng chung
- **C# thuần (.NET 10, `Nullable=enable`):** Các POCO class gọn nhẹ, khởi tạo giá trị mặc định rõ ràng (`string.Empty`, `DateTime.Now`, danh sách khởi tạo `[]`).
- **`System.ComponentModel.DataAnnotations`:** Sử dụng `[Key]`, `[Required]`, `[MaxLength]` mô tả khóa chính và ràng buộc độ dài khớp lược đồ SQLite.
- **Navigation Properties:** Khai báo đầy đủ tham chiếu quan hệ (`1-1`, `1-N`, `N-N`) phục vụ cả lưu phiên làm việc (`User.CustomerProfile`) lẫn mở rộng ORM sau này.

### Tổng quát thư mục
Định nghĩa ngôn ngữ miền (Domain Model) của toàn hệ thống rạp phim: 1 tệp chứa 11 `enum` chuẩn hóa trạng thái/phân loại và 10 lớp Entity ánh xạ 1-1 với 10 bảng trong `CinemaManageDB.sql`.

### Chi tiết từng file
- **[CinemaEnums.cs](CinemaManageProject/Models/CinemaEnums.cs)** *(116 dòng)*: Định nghĩa toàn bộ **11 enum** khớp tuyệt đối ràng buộc `CHECK` trong SQLite: `UserRole`, `CustomerTier`, `MovieStatus`, `AgeRating`, `RoomType`, `SeatType`, `ShowtimeStatus`, `OrderStatus`, `PaymentMethod`, `BookingChannel`, `TicketStatus`.
- **[User.cs](CinemaManageProject/Models/User.cs)** *(47 dòng)*: Thực thể tài khoản hệ thống (`UserId`, `Username`, `Password`, `FullName`, `Email`, `Phone`, `Role`, `IsActive`, `CreatedAt`, `LastLoginAt`) cùng navigation tới `CustomerProfile`, `CreatedShowtimes`, `SoldOrders`, `CheckedInTickets`.
- **[Customer.cs](CinemaManageProject/Models/Customer.cs)** *(41 dòng)*: Thực thể hội viên (`CustomerId`, `UserId?`, `FullName`, `Phone`, `Email`, `DateOfBirth`, `LoyaltyPoints`, `TotalSpent`, `Tier`, `CreatedAt`) và danh sách `Orders`.
- **[Genre.cs](CinemaManageProject/Models/Genre.cs)** *(18 dòng)*: Thực thể thể loại phim (`GenreId`, `Name`) và tập `MovieGenres`.
- **[Movie.cs](CinemaManageProject/Models/Movie.cs)** *(49 dòng)*: Thực thể phim (`MovieId`, `Title`, `OriginalTitle`, `Director`, `Cast`, `DurationMinutes`, `ReleaseDate`, `EndDate`, `AgeRating`, `Language`, `Description`, `PosterPath`, `Status`, `CreatedAt`) cùng `MovieGenres` và `Showtimes`.
- **[MovieGenre.cs](CinemaManageProject/Models/MovieGenre.cs)** *(13 dòng)*: Bảng nối nhiều-nhiều giữa `Movie` và `Genre` (`MovieId`, `GenreId`, navigation `Movie`, `Genre`).
- **[Room.cs](CinemaManageProject/Models/Room.cs)** *(31 dòng)*: Thực thể phòng chiếu (`RoomId`, `Name`, `RoomType`, `TotalRows`, `TotalCols`, `BasePriceSurcharge`, `IsActive`, `CreatedAt`) kèm `Seats` và `Showtimes`.
- **[Seat.cs](CinemaManageProject/Models/Seat.cs)** *(40 dòng)*: Thực thể ghế ngồi (`SeatId`, `RoomId`, `RowLabel`, `SeatNumber`, `SeatCode`, `SeatType`, `PriceMultiplier`, `RowIndex`, `ColIndex`, `IsActive`) kèm `Room` và `Tickets`.
- **[Showtime.cs](CinemaManageProject/Models/Showtime.cs)** *(39 dòng)*: Thực thể suất chiếu (`ShowtimeId`, `MovieId`, `RoomId`, `StartTime`, `EndTime`, `BaseTicketPrice`, `Format`, `Status`, `CreatedByUserId`, `CreatedAt`) kèm `Movie`, `Room`, `CreatedByUser`, `Tickets`.
- **[Order.cs](CinemaManageProject/Models/Order.cs)** *(57 dòng)*: Thực thể hóa đơn (`OrderId`, `OrderCode`, `StaffUserId?`, `CustomerId?`, `BookingChannel`, `TotalTickets`, `SubTotalAmount`, `PointsUsed`, `DiscountAmount`, `TotalAmount`, `PaymentMethod`, `CashReceived`, `ChangeAmount`, `Status`, `Note`, `CreatedAt`, `PaidAt`) kèm `Tickets`.
- **[Ticket.cs](CinemaManageProject/Models/Ticket.cs)** *(34 dòng)*: Thực thể vé (`TicketId`, `TicketCode`, `OrderId`, `ShowtimeId`, `SeatId`, `UnitPrice`, `Status`, `CheckedInAt`, `CheckedInByUserId`) kèm navigation tới `Order`, `Showtime`, `Seat`, `CheckedInByUser`.

---

## 4. Thư mục `CinemaManageProject/Helpers/` — Các lớp Phụ trợ Dùng chung

### Công nghệ & Thư viện dùng chung
- **`Microsoft.Data.Sqlite` & `Dapper` (`SqlMapper.TypeHandler<decimal>`):** Mở kết nối SQLite chuẩn hóa và xử lý an toàn kiểu động `NUMERIC` của SQLite khi ánh xạ sang `decimal`.
- **`System.Security.Cryptography`:** `RandomNumberGenerator`, `Rfc2898DeriveBytes.Pbkdf2` (`SHA-256`, `100.000` vòng lặp) và `CryptographicOperations.FixedTimeEquals` (so sánh hằng thời gian chống timing attack).
- **`System.Globalization` (`vi-VN` / `InvariantCulture`):** Định dạng tiền tệ VNĐ, ngày giờ và dịch nhãn enum tiếng Việt.
- **`System.Windows.Media.Imaging` (`BitmapImage`):** Nạp ảnh poster với `BitmapCacheOption.OnLoad` và `Freeze()` để không khóa tệp ảnh trên đĩa.

### Tổng quát thư mục
Tập hợp 4 lớp tiện ích tĩnh không lưu trạng thái (stateless static helpers), tách rạch ròi theo đúng trách nhiệm kỹ thuật: kết nối CSDL, bảo mật mật khẩu, định dạng hiển thị và xử lý tệp ảnh poster.

### Chi tiết từng file
- **[DatabaseHelper.cs](CinemaManageProject/Helpers/DatabaseHelper.cs)** *(47 dòng)*:
  - Lưu `ConnectionString` trỏ tới `Database/cinema.db` trong `AppContext.BaseDirectory`.
  - Đăng ký `SqliteDecimalHandler` trong static constructor để Dapper chuyển đổi an toàn mọi giá trị `Int64`/`Double` từ cột `NUMERIC` sang `decimal`.
  - Cung cấp `OpenConnection()` luôn thực thi `PRAGMA foreign_keys = ON;` trên mỗi kết nối mở ra.
- **[PasswordHelper.cs](CinemaManageProject/Helpers/PasswordHelper.cs)** *(84 dòng)*:
  - `HashPassword(plainPassword)`: tạo salt ngẫu nhiên 16 byte, băm 100.000 vòng PBKDF2-HMAC-SHA256 ra khóa 32 byte, trả về chuỗi `PBKDF2$100000$<saltBase64>$<hashBase64>`.
  - `VerifyPassword(plainPassword, storedPassword, out needsUpgrade)`: kiểm tra mật khẩu bằng `FixedTimeEquals`; hỗ trợ nhận diện chuỗi plain-text cũ hoặc số vòng lặp thấp để bật cờ `needsUpgrade` tự nâng cấp khi đăng nhập đúng.
  - `IsHashed(storedPassword)`: kiểm tra nhanh tiền tố `PBKDF2$`.
- **[FormatHelper.cs](CinemaManageProject/Helpers/FormatHelper.cs)** *(78 dòng)*:
  - `FormatVnd(decimal)`: định dạng tiền Việt có dấu chấm phân cách nghìn (VD: `120.000đ`).
  - `FormatSqlDateTime(string, format)`: chuyển chuỗi thời gian lưu trong SQLite sang `dd/MM HH:mm`, `dd/MM/yyyy HH:mm`...
  - `FormatPaymentMethod`, `FormatStatusLabel`, `FormatRoomType`, `FormatSeatType`: dịch mã enum DB sang tiếng Việt thống nhất toàn ứng dụng.
- **[PosterHelper.cs](CinemaManageProject/Helpers/PosterHelper.cs)** *(39 dòng)*:
  - `ResolvePosterFullPath(storedPath)`: ghép đường dẫn tương đối `Posters/...` thành đường dẫn tuyệt đối.
  - `LoadPosterBitmap(fullPath)`: nạp `BitmapImage` với `CacheOption = OnLoad` và `Freeze()` (an toàn khi thay/xóa ảnh lúc app đang chạy).
  - `SavePosterFile(sourcePath)`: sao chép ảnh người dùng chọn vào `Posters/poster_<timestamp>.<ext>` và trả về đường dẫn tương đối để lưu DB.

---

## 5. Thư mục `CinemaManageProject/Data/` — Tầng Truy cập Dữ liệu SQLite (Dapper)

### Công nghệ & Thư viện dùng chung
- **`Dapper 2.1.100` + `Microsoft.Data.Sqlite 10.0.15`:** Truy vấn SQL thuần tham số hóa (`@param`), kết hợp `conn.Query<T>`, `conn.QueryFirstOrDefault<T>`, `conn.QuerySingle<T>`, `conn.Execute`, `conn.ExecuteScalar<T>`.
- **`SqliteTransaction`:** Bảo vệ toàn vẹn dữ liệu cho các thao tác nhiều bước (đăng ký tài khoản, lưu phim + thể loại, kiểm tra trùng lịch chiếu, đặt vé chống trùng ghế, soát vé check-in, hủy vé hoàn điểm).
- **C# `sealed record` DTO:** Định nghĩa ngay cùng file nghiệp vụ các kiểu dữ liệu vào/ra (`*Input`, `*Result`, `*Row`, `*Dto`) gọn nhẹ, bất biến và có sẵn thuộc tính định dạng hiển thị.

### Tổng quát thư mục
Cô lập **100% câu lệnh SQL** khỏi `ViewModels/` vào 10 lớp tĩnh theo từng phân hệ nghiệp vụ. Toàn bộ tầng này chia sẻ chung quy ước:
- Giá vé: `UnitPrice = Math.Round((Showtime.BaseTicketPrice + Room.BasePriceSurcharge) * Seat.PriceMultiplier, 0)`.
- Lịch chiếu: `EndTime = StartTime + Movie.DurationMinutes + 15 phút` dọn phòng; kiểm tra trùng trong cùng `RoomId`.
- Điểm thưởng & Hạng thẻ: đổi `1 điểm = 1.000đ` (tối đa `50%` tạm tính), tích `1 điểm / 10.000đ` thực trả; mốc hạng `Silver` ≥ 1 triệu, `Gold` ≥ 5 triệu, `Platinum` ≥ 10 triệu.

### Chi tiết từng file
- **[AuthData.cs](CinemaManageProject/Data/AuthData.cs)** *(161 dòng)*:
  - DTO: `AuthResult`.
  - `Authenticate(username, password)`: xác thực qua `PasswordHelper.VerifyPassword`, chặn tài khoản bị khóa (`IsActive = 0`), tự nâng cấp mật khẩu cũ sang `PBKDF2`, cập nhật `LastLoginAt`, xóa chuỗi hash trong RAM (`user.Password = string.Empty`) và nạp `CustomerProfile` nếu là `Customer`.
  - `RegisterCustomer(...)`: tạo đồng thời bản ghi `Users` (đã băm mật khẩu) và `Customers` trong 1 `SqliteTransaction`, kiểm tra trùng `Username`/`Phone`.
- **[DashboardData.cs](CinemaManageProject/Data/DashboardData.cs)** *(343 dòng)*:
  - DTO: `AdminDashboardSnapshot`, `StaffDashboardSnapshot`, `CustomerDashboardSnapshot` cùng các `*ShowtimeDto`, `*OrderDto`, `*MovieDto`, `*TicketDto`.
  - `LoadAdminDashboard()`: tổng hợp 9 chỉ số KPI toàn rạp, top 12 suất chiếu (tỷ lệ lấp đầy) và 8 hóa đơn gần nhất.
  - `LoadStaffDashboard(staffId)`: KPI ca cá nhân của thu ngân, 12 suất mở bán còn ghế trống và 8 giao dịch quầy gần nhất.
  - `LoadCustomerDashboard(userId, fallbackProfile)`: làm mới hồ sơ hội viên (qua `CustomerData.LoadCustomerProfile`), nạp danh sách phim `NowShowing`/`ComingSoon` kèm thể loại (`GROUP_CONCAT`) & giá thấp nhất, và 15 vé gần nhất.
- **[MovieData.cs](CinemaManageProject/Data/MovieData.cs)** *(230 dòng)*:
  - DTO: `AdminMovieRow`, `MovieSaveInput`.
  - `LoadAllGenres`, `InsertGenre`: quản trị danh mục thể loại.
  - `LoadAllMovies`: đọc danh sách phim kèm chuỗi thể loại và danh sách `GenreIds`, sắp xếp ưu tiên `NowShowing` → `ComingSoon` → `Ended`.
  - `SaveMovie`, `CountMovieShowtimes`, `DeleteMovie`: thêm/sửa phim và đồng bộ bảng nối `MovieGenres` trong transaction; kiểm tra ràng buộc suất chiếu trước khi xóa.
- **[RoomData.cs](CinemaManageProject/Data/RoomData.cs)** *(152 dòng)*:
  - DTO: `AdminRoomRow`, `RoomSeatRow`, `SeatOrderRow`.
  - `LoadAllRooms`, `LoadSeatsByRoom`: thống kê tổng ghế/khả dụng/bảo trì và phân loại Thường/VIP/Đôi của từng phòng.
  - `LoadSeatOrders`: truy vết 20 lượt đặt gần nhất của một ghế cụ thể qua 5 bảng.
  - `CountActiveTicketsForSeat`, `SetSeatActive`: chặn chuyển sang bảo trì nếu ghế đang có vé `Booked`/`CheckedIn` ở suất `Scheduled`/`OpenForSale`.
- **[ShowtimeData.cs](CinemaManageProject/Data/ShowtimeData.cs)** *(331 dòng)*:
  - Hằng số `StandardBaseTicketPrice = 80000m`; DTO: `ShowtimeMovieOption`, `ShowtimeRoomOption`, `AdminShowtimeItem`, `ShowtimeSaveInput`, `ShowtimeSaveResult`.
  - `LoadMovieOptions`, `LoadRoomOptions`, `LoadAllShowtimes`: nạp combobox phim/phòng và danh sách lịch chiếu kèm số vé hoạt động / ghế khả dụng.
  - `SaveShowtime`: khóa chỉnh sửa phim/phòng/giờ/giá khi suất đã có vé hiệu lực, chặn trùng khung giờ trong cùng phòng chiếu trong `SqliteTransaction`.
  - `CountShowtimeTickets`, `DeleteShowtime`, `SuggestFormat`: đếm vé trước khi xóa và gợi ý tự động định dạng chiếu (`IMAX Phụ đề`, `2D Tiếng Việt`...).
- **[OrderData.cs](CinemaManageProject/Data/OrderData.cs)** *(229 dòng)*:
  - DTO: `AdminOrderSummaryRow`, `OrderTicketDetailRow`.
  - `LoadAllOrders`: đọc toàn bộ hóa đơn ghép thông tin thu ngân, khách hàng, số vé hiệu lực (`ActiveTicketsCount`) và trạng thái thu tiền.
  - `LoadAllOrderTickets`: tải toàn bộ chi tiết vé và gom thành `Dictionary<int, IReadOnlyList<OrderTicketDetailRow>>` theo `OrderId` phục vụ xem chi tiết phiếu và tính phân bổ doanh thu theo phim.
- **[UserData.cs](CinemaManageProject/Data/UserData.cs)** *(457 dòng)*:
  - DTO: `AdminUserRow`, `UserSaveInput`, `UserSaveResult`, `UserActionResult`.
  - `LoadAllUsers`: nạp danh sách tài khoản kèm thông tin `Customers` và tổng lượt hoạt động (`StaffActivityCount`, `CustomerOrdersCount`).
  - `SaveUser`: thêm/sửa `Staff` hoặc `Customer`, băm mật khẩu mới qua `PasswordHelper`, đồng bộ 1-1 `Users` ↔ `Customers`, chặn sửa/gán quyền `Admin`.
  - `SetUserActive`, `DeleteUser`: bảo vệ tuyệt đối tài khoản `Admin` gốc và chống tự khóa/tự xóa; chỉ cho xóa cứng khi không phát sinh lịch sử suất chiếu/hóa đơn/soát vé.
- **[BookingData.cs](CinemaManageProject/Data/BookingData.cs)** *(920 dòng)*:
  - Lõi đặt vé dùng chung cho cả **Staff POS (`Counter`)**, **Customer Browse (`SelfService`)** và **Customer MyTickets**.
  - DTO: `OpenShowtimeOption`, `ShowtimeSeatRow`, `BookingCustomerSummary`, `BrowseMovieRow`, `BookingCreateInput`, `BookingCreateResult`, `CustomerMyTicketRow`, `CustomerMyOrderRow`, `CancelTicketResult`.
  - Hàm tính toán dùng chung: `CalculateSeatUnitPrice`, `CalculateMaxRedeemablePoints`, `CalculateEarnedPoints`, `ComputeTier`.
  - Truy vấn đọc: `LoadOpenShowtimes`, `LoadBrowseMovies`, `LoadGenreNames`, `LoadShowtimeSeatMap`, `FindCustomerByUserId/Phone/Id`, `LoadMyTickets`, `LoadMyOrders`.
  - Giao dịch cốt lõi: `CreateOrder` (khóa kiểm tra suất chiếu/ghế bảo trì/ghế trùng trong `SqliteTransaction`, sinh mã `HD-yyyyMMdd-xxxx` & `VE-yyMMdd-xxxx-SeatCode`, cập nhật điểm và hạng thẻ) và `CancelCustomerTicket` (hủy vé `Booked`, chuyển hóa đơn sang `PartiallyRefunded` hoặc `Cancelled`, giải phóng ghế và hoàn điều chỉnh điểm/chi tiêu/hạng).
- **[CheckInData.cs](CinemaManageProject/Data/CheckInData.cs)** *(204 dòng)*:
  - DTO: `CheckInTicketRow`, `CheckInActionResult`.
  - `LookupTicketByCode` (không phân biệt hoa/thường), `LoadAllTicketsForCheckIn` (ghép 8 bảng, ưu tiên vé `Booked` chờ soát lên đầu).
  - `ConfirmCheckIn(ticketId, staffUserId)`: kiểm tra trạng thái vé/hóa đơn/suất chiếu trong transaction, cập nhật `Status = 'CheckedIn'`, `CheckedInAt` và `CheckedInByUserId`.
- **[CustomerData.cs](CinemaManageProject/Data/CustomerData.cs)** *(255 dòng)*:
  - DTO: `StaffCustomerRow`, `CustomerOrderHistoryRow`, `CounterCustomerSaveInput`, `CounterCustomerSaveResult`.
  - `LoadCustomerProfile(conn, userId)`: nạp hồ sơ hội viên trong kết nối dùng chung của `DashboardData`.
  - `LoadAllCustomers`, `LoadCustomerOrderHistory`: danh sách hội viên sắp theo hạng thẻ & chi tiêu kèm lịch sử 25 hóa đơn gần nhất.
  - `SaveCounterCustomer`: mở hội viên mới tại quầy (`UserId = NULL`) hoặc cập nhật liên hệ kèm đồng bộ ngược sang `Users` nếu hội viên có liên kết tài khoản App.

---

## 6. Thư mục `CinemaManageProject/Themes/` — Hệ thống Giao diện Dark Theme Zinc

### Công nghệ & Thư viện dùng chung
- **`WPF-UI 4.3.0` (`Wpf.Ui`):** Nạp `<ui:ThemesDictionary Theme="Dark" />` và `<ui:ControlsDictionary />` ngay trong `MergedDictionaries`, kế thừa style control hiện đại bằng `BasedOn="{StaticResource {x:Type ...}}"` thay cho các `ControlTemplate` vẽ tay.
- **WPF `ResourceDictionary` & `{StaticResource ...}`:** Không dùng `DynamicResource`, viết mã màu Dark Theme Zinc trực tiếp (`#09090B`, `#0F1011`, `#141516`, `#18181B`, chữ `#FAFAFA` / `#A1A1AA`, nhấn Indigo `#5E6AD2`, viền `#23252A` / `#3F3F46`) để dễ đọc và tải nhanh.
- **`DrawingBrush` & `RadialGradientBrush`:** Tạo phông nền điện ảnh đa lớp hoàn toàn bằng vector, không tốn tài nguyên ảnh bitmap.

### Tổng quát thư mục
Tập trung 100% tài nguyên giao diện dùng chung về duy nhất 1 tệp `Controls.xaml` (được nạp tại `App.xaml`), bảo đảm cả 15 màn hình đồng nhất tuyệt đối về màu sắc, kiểu chữ, nút bấm, bảng dữ liệu và ô ghế.

### Chi tiết từng file
- **[Controls.xaml](CinemaManageProject/Themes/Controls.xaml)** *(498 dòng, 27 resource key)*:
  - **Converter (1 key):** `BoolToVis` (`BooleanToVisibilityConverter`).
  - **Nền điện ảnh & Cửa sổ (2 key):** `CinemaWindowBackgroundBrush` (nền tối phối ánh sáng Indigo & Crimson nhẹ), `CinemaWindowStyle` (`Segoe UI`, `14px`, `#FAFAFA`).
  - **Typography (4 key):** `PageTitleTextStyle`, `SectionTitleTextStyle`, `MutedCaptionTextStyle`, `FieldLabelTextStyle`.
  - **Bề mặt & Huy hiệu (2 key):** `CardBorderStyle` (thẻ bo góc `12px`, nền `#141516`, viền `#23252A`), `BadgeBorderStyle` (nhãn bo tròn `999px`).
  - **Nút bấm & Thẻ tương tác (9 key):** `BaseButtonStyle` (kế thừa WPF-UI), `PrimaryButtonStyle` (Indigo `#5E6AD2`), `SecondaryButtonStyle`, `DangerButtonStyle`, `GhostButtonStyle`, `NavMenuButtonStyle` (Sidebar với trạng thái `Tag="Active"`), `FilterChipButtonStyle`, `PosterCardButtonStyle` (thẻ poster có viền sáng khi chọn), `ShowtimeCardItemStyle` (`ListBoxItem` chọn suất chiếu ở POS và Browse).
  - **Ô nhập liệu (4 key kế thừa WPF-UI):** `ModernTextBoxStyle`, `ModernPasswordBoxStyle`, `ModernCheckBoxStyle`, `ModernComboBoxStyle`.
  - **Bảng dữ liệu (4 key):** `ModernDataGridStyle`, `CompactDataGridStyle` (bảng gọn không tràn cột), `EllipsisCellTextStyle`, `WrappingCellTextStyle`.
  - **Sơ đồ ghế dùng chung (1 key):** `BookingSeatButtonStyle` với `DataTrigger` cho đủ 6 trạng thái trực quan (`Standard`, `VIP`, `Couple` rộng `86px`, `Selected`, `Booked`, `Maintenance`), dùng chung giữa `StaffPosView` và `CustomerBrowseView`.

---

## 7. Thư mục `CinemaManageProject/Posters/` — Tài nguyên Ảnh Poster Phim

### Công nghệ & Cơ chế sử dụng
- **Định dạng JPEG tĩnh (`CopyToOutputDirectory=PreserveNewest`):** Được đóng gói tự động vào thư mục chạy của ứng dụng theo cấu hình trong `.csproj`.
- **Ánh xạ mềm qua DB:** Cột `Movies.PosterPath` lưu đường dẫn tương đối `Posters/<ten-file>.jpg`; mọi ViewModel đều nạp thông qua `PosterHelper.LoadPosterBitmap(...)` (`BitmapCacheOption.OnLoad` + `Freeze()`) và có khung fallback `"Chưa có poster"` nếu tệp không tồn tại.

### Tổng quát thư mục
Chứa trọn bộ **15 tệp ảnh poster thực tế** (tổng dung lượng ~1,5 MB) tương ứng chính xác 15 phim mẫu trong `SeedData.sql` (`10` phim đang chiếu và `5` phim sắp chiếu).

### Chi tiết từng file
| STT | Tệp tin trong `CinemaManageProject/Posters/` | Dung lượng | Phim tương ứng (`Movies.MovieId`) | Trạng thái mẫu |
| :-: | :--- | :---: | :--- | :---: |
| 1 | [dune-part-two.jpg](CinemaManageProject/Posters/dune-part-two.jpg) | 92,1 KB | Dune: Hành Tinh Cát - Phần 2 (`#1`) | Đang chiếu |
| 2 | [mai.jpg](CinemaManageProject/Posters/mai.jpg) | 47,4 KB | Mai (`#2`) | Đang chiếu |
| 3 | [kung-fu-panda-4.jpg](CinemaManageProject/Posters/kung-fu-panda-4.jpg) | 80,1 KB | Kung Fu Panda 4 (`#3`) | Đang chiếu |
| 4 | [exhuma.jpg](CinemaManageProject/Posters/exhuma.jpg) | 80,2 KB | Quật Mộ Trùng Ma (`#4`) | Đang chiếu |
| 5 | [godzilla-x-kong.jpg](CinemaManageProject/Posters/godzilla-x-kong.jpg) | 70,5 KB | Godzilla x Kong: Đế Chế Mới (`#5`) | Đang chiếu |
| 6 | [lat-mat-7.jpg](CinemaManageProject/Posters/lat-mat-7.jpg) | 71,7 KB | Lật Mặt 7: Một Điều Ước (`#6`) | Đang chiếu |
| 7 | [the-fall-guy.jpg](CinemaManageProject/Posters/the-fall-guy.jpg) | 105,5 KB | Kẻ Thế Thân (`#7`) | Đang chiếu |
| 8 | [kingdom-planet-apes.jpg](CinemaManageProject/Posters/kingdom-planet-apes.jpg) | 161,6 KB | Hành Tinh Khỉ: Vương Quốc Mới (`#8`) | Đang chiếu |
| 9 | [thanh-xuan-18x2.jpg](CinemaManageProject/Posters/thanh-xuan-18x2.jpg) | 46,8 KB | Thanh Xuân 18x2 (`#9`) | Đang chiếu |
| 10 | [pee-nak-4.jpg](CinemaManageProject/Posters/pee-nak-4.jpg) | 462,0 KB | Ngôi Đền Kỳ Quái 4 (`#10`) | Đang chiếu |
| 11 | [spider-man-spider-verse.jpg](CinemaManageProject/Posters/spider-man-spider-verse.jpg) | 107,2 KB | Người Nhện: Du Hành Vũ Trụ Mới (`#11`) | Sắp chiếu |
| 12 | [ghostbusters-frozen.jpg](CinemaManageProject/Posters/ghostbusters-frozen.jpg) | 30,7 KB | Biệt Đội Săn Ma: Kỷ Nguyên Băng Giá (`#12`) | Sắp chiếu |
| 13 | [a-quiet-place-day-one.jpg](CinemaManageProject/Posters/a-quiet-place-day-one.jpg) | 50,7 KB | Vùng Đất Câm Lặng: Ngày Một (`#13`) | Sắp chiếu |
| 14 | [inside-out-2.jpg](CinemaManageProject/Posters/inside-out-2.jpg) | 48,9 KB | Những Mảnh Ghép Cảm Xúc 2 (`#14`) | Sắp chiếu |
| 15 | [ngay-xua-chuyen-tinh.jpg](CinemaManageProject/Posters/ngay-xua-chuyen-tinh.jpg) | 80,1 KB | Ngày Xưa Có Một Chuyện Tình (`#15`) | Sắp chiếu |

---

## 8. Thư mục `CinemaManageProject/ViewModels/` — Tầng Logic Giao diện (MVVM)

### Công nghệ & Thư viện dùng chung
- **`CommunityToolkit.Mvvm 8.4.2`:** Toàn bộ 17 tệp ViewModel kế thừa `ViewModelBase` (`ObservableObject`), khai báo `partial class` và dùng Source Generator lúc biên dịch (`[ObservableProperty]`, `[NotifyPropertyChangedFor]`, `partial void OnXxxChanged/Changing`) để tự động hóa `INotifyPropertyChanged`.
- **`ObservableCollection<T>` & LINQ:** Quản lý danh sách ràng buộc dữ liệu hai chiều lên `DataGrid`, `ListBox`, `ItemsControl`, `ComboBox`.
- **`WPF-UI` (`Wpf.Ui.Controls.SymbolRegular`):** Khai báo biểu tượng Sidebar kiểu an toàn trực tiếp trong `MainViewModel`.
- **Tách biệt sạch với CSDL:** `0` câu SQL trực tiếp trong toàn bộ thư mục `ViewModels/` — mọi truy cập DB đều đi qua các lớp tĩnh trong `Data/`. Không dùng `ICommand`/`RelayCommand` rườm rà mà cung cấp các method `public` gọn rõ để code-behind gọi trực tiếp.

### Tổng quát thư mục
Gồm **17 tệp tin** chia làm 4 nhóm: 4 tệp lõi dùng chung tại gốc `ViewModels/` và 3 thư mục con theo đúng 3 vai trò (`Admin/` 6 tệp, `Staff/` 4 tệp, `Customer/` 3 tệp).

### 8.1. Nhóm Dùng chung (`CinemaManageProject/ViewModels/`)
- **[ViewModelBase.cs](CinemaManageProject/ViewModels/ViewModelBase.cs)** *(11 dòng)*: Lớp trừu tượng kế thừa `ObservableObject`, định nghĩa cổng `public virtual void LoadData()` giúp `MainViewModel` làm mới đồng nhất mọi màn hình con.
- **[LoginViewModel.cs](CinemaManageProject/ViewModels/LoginViewModel.cs)** *(126 dòng)*: Quản lý trạng thái chuyển đổi Đăng nhập ↔ Đăng ký hội viên (`ToggleMode`), xác thực (`Login`), tạo tài khoản mới (`Register`), điền nhanh 3 tài khoản mẫu (`FillDemo`) và phát các sự kiện `LoginSucceeded`, `DemoPasswordRequested`, `ModeSwitched`.
- **[MainViewModel.cs](CinemaManageProject/ViewModels/MainViewModel.cs)** *(188 dòng)*: Chứa `SidebarMenuItem` (kèm `SymbolRegular IconSymbol`) và điều phối Shell chính: xây dựng menu Sidebar tự động theo `App.CurrentUser.Role` (`BuildSidebar`), đóng/mở Sidebar (`ToggleSidebar`), nạp/chuyển màn hình (`SelectMenu`, `SwitchOrReload`), làm mới (`RefreshCurrentDashboard`) và đăng xuất (`Logout`).
- **[BookingSeatItems.cs](CinemaManageProject/ViewModels/BookingSeatItems.cs)** *(76 dòng)*: Chứa `BookingSeatRowGroup` và `BookingSeatCellItem` (với thuộc tính `VisualState`, `IsCouple`, `IsSelectable`, `ToolTipText`) dùng chung 100% cho sơ đồ chọn ghế của `StaffPosViewModel` và `CustomerBrowseViewModel`.

### 8.2. Phân hệ Admin (`CinemaManageProject/ViewModels/Admin/`)
- **[AdminDashboardViewModel.cs](CinemaManageProject/ViewModels/Admin/AdminDashboardViewModel.cs)** *(86 dòng)*: Nạp 4 thẻ KPI toàn rạp (doanh thu, vé bán/check-in, phim đang/sắp chiếu, phòng/ghế bảo trì), bảng *Lịch chiếu & Tỷ lệ lấp đầy* (`AdminShowtimeRow`) và *Hóa đơn gần đây* (`AdminOrderRow`).
- **[AdminMoviesViewModel.cs](CinemaManageProject/ViewModels/Admin/AdminMoviesViewModel.cs)** *(534 dòng)*: Quản trị phim trọn vòng đời: bộ lọc đa tiêu chí (trạng thái, thể loại, độ tuổi, từ khóa + `ResetFilters`), chọn nhiều thể loại (`GenreSelectionItem`), thêm nhanh thể loại mới (`AddGenre`), chọn/xóa ảnh poster (`SetPosterFromFile`, `ClearPoster`), lưu (`SaveMovie`) và kiểm tra/xóa (`CanDeleteSelectedMovie`, `DeleteSelectedMovie`).
- **[AdminRoomsViewModel.cs](CinemaManageProject/ViewModels/Admin/AdminRoomsViewModel.cs)** *(255 dòng)*: Quản lý 4 phòng chiếu cố định và sơ đồ ghế trực quan (`SeatRowGroup`, `SeatCellItem`), xem lịch sử đơn đặt khi chọn từng ghế (`SelectSeat`) và bật/tắt trạng thái bảo trì có kiểm tra vé hiệu lực (`ToggleSelectedSeatAvailable`).
- **[AdminShowtimesViewModel.cs](CinemaManageProject/ViewModels/Admin/AdminShowtimesViewModel.cs)** *(560 dòng)*: Lên lịch chiếu tự động tính giờ kết thúc `+15 phút` dọn phòng, xem trước bảng giá vé 3 hạng ghế (Thường/VIP/Đôi), bộ lọc theo ngày/phòng/trạng thái/từ khóa, khóa sửa thông số cốt lõi khi suất đã có vé, lưu (`SaveShowtime`) và xóa (`DeleteSelectedShowtime`).
- **[AdminOrdersViewModel.cs](CinemaManageProject/ViewModels/Admin/AdminOrdersViewModel.cs)** *(537 dòng)*: Phân tích hóa đơn & doanh thu: 4 thẻ KPI động theo bộ lọc (ngày, kênh, PTTT, trạng thái, từ khóa), bảng cơ cấu 4 phương thức thanh toán (`PaymentBreakdownRow`), bảng phân bổ doanh thu theo từng phim theo tỷ lệ `activeSub` (`MovieRevenueRow`) và chi tiết phiếu tính tiền/danh sách vé.
- **[AdminUsersViewModel.cs](CinemaManageProject/ViewModels/Admin/AdminUsersViewModel.cs)** *(576 dòng)*: Quản trị tài khoản `Staff` & `Customer`: lọc theo vai trò/trạng thái/từ khóa, thêm/sửa/đặt lại mật khẩu (`SaveUser`), khóa/mở khóa (`ToggleSelectedUserActive`), kiểm tra điều kiện xóa cứng (`CanDeleteSelectedUser`, `DeleteSelectedUser`), hiển thị huy hiệu khóa bảo vệ đối với tài khoản `Admin` gốc.

### 8.3. Phân hệ Staff (`CinemaManageProject/ViewModels/Staff/`)
- **[StaffDashboardViewModel.cs](CinemaManageProject/ViewModels/Staff/StaffDashboardViewModel.cs)** *(81 dòng)*: Nạp 4 KPI ca quầy cá nhân (vé đã bán, doanh thu ca, suất mở bán, vé chờ check-in), danh sách suất chiếu mở bán (`StaffShowtimeRow`) và giao dịch quầy gần nhất (`StaffOrderRow`).
- **[StaffPosViewModel.cs](CinemaManageProject/ViewModels/Staff/StaffPosViewModel.cs)** *(649 dòng)*: Bán vé tại quầy (`Counter`): dải poster phim lọc suất (`PosMoviePosterItem`, `SelectMovie`, `ShowAllMovies`), chọn suất & chọn nhiều ghế (`ToggleSeat`, `ClearSelectedSeats`), tra cứu/bỏ gắn hội viên theo SĐT (`LookupCustomer`, `ClearCustomer`), đổi điểm thưởng (`ApplyMaxPoints`), chọn 4 PTTT + tính tiền thối nhanh (`SetExactCash`, `SetQuickCash`) và phát hành hóa đơn (`CompleteSale`).
- **[StaffCheckInViewModel.cs](CinemaManageProject/ViewModels/Staff/StaffCheckInViewModel.cs)** *(209 dòng)*: Soát vé Check-in: tra cứu mã vé (`LookupTicket`), lọc hàng chờ theo trạng thái (`SetStatusFilter`) và từ khóa, hiển thị trạng thái hợp lệ (`Valid`/`AlreadyCheckedIn`/`Blocked`), xác nhận check-in (`ConfirmCurrentTicketCheckIn`) và làm sạch khung quét (`ClearLookup`).
- **[StaffCustomersViewModel.cs](CinemaManageProject/ViewModels/Staff/StaffCustomersViewModel.cs)** *(240 dòng)*: Tra cứu hội viên theo hạng thẻ (`SetTierFilter`) và từ khóa, xem lịch sử hóa đơn của khách đang chọn, cập nhật liên hệ và đăng ký hội viên mới tại quầy (`StartNewCustomer`, `SaveCustomer`).

### 8.4. Phân hệ Customer (`CinemaManageProject/ViewModels/Customer/`)
- **[CustomerDashboardViewModel.cs](CinemaManageProject/ViewModels/Customer/CustomerDashboardViewModel.cs)** *(247 dòng)*: Thẻ hội viên (hạng thẻ, điểm, tổng chi tiêu, số vé chờ chiếu), dải poster trượt ngang kèm khung spotlight chi tiết (`CustomerMovieRow`, `SetFilter`, `SelectMovie`) và bảng *Vé của tôi* (`CustomerTicketRow`).
- **[CustomerBrowseViewModel.cs](CinemaManageProject/ViewModels/Customer/CustomerBrowseViewModel.cs)** *(596 dòng)*: Duyệt phim Đang chiếu/Sắp chiếu (`BrowseMovieCardItem`) theo trạng thái/thể loại/từ khóa, chọn suất mở bán, chọn ghế trên sơ đồ dùng chung (`ToggleSeat`, `ClearSelectedSeats`), đổi điểm thưởng (`ApplyMaxPoints`), thanh toán trực tuyến 3 kênh (`OnlinePaymentOption`: Chuyển khoản/Ví điện tử/Thẻ) và xác nhận đặt vé (`ConfirmBooking`) kèm đồng bộ `App.CurrentUser.CustomerProfile`.
- **[CustomerMyTicketsViewModel.cs](CinemaManageProject/ViewModels/Customer/CustomerMyTicketsViewModel.cs)** *(202 dòng)*: Quản lý toàn bộ vé và lịch sử hóa đơn của hội viên, lọc vé theo trạng thái (`SetStatusFilter`), xem cuống vé điện tử có `TicketCode` check-in và hủy vé `Booked` của suất chưa chiếu (`CancelSelectedTicket`).

---

## 9. Thư mục `CinemaManageProject/Views/` — Tầng Màn hình XAML & Code-behind

### Công nghệ & Thư viện dùng chung
- **WPF XAML + Data Binding (`{Binding ...}`, `Mode=TwoWay`, `UpdateSourceTrigger=PropertyChanged`):** Kết nối trực tiếp giao diện với ViewModel, ẩn/hiện động bằng `{StaticResource BoolToVis}` và `DataTrigger`.
- **`WPF-UI 4.3.0` (`xmlns:ui="http://schemas.lepo.co/wpfui/2022/xaml"`):** Dùng `<ui:SymbolIcon Symbol="..." />` thay hoàn toàn icon SVG vẽ tay và dùng các `Style` kế thừa từ `Themes/Controls.xaml`.
- **Code-behind tối giản (`.xaml.cs`):** Chỉ làm 4 việc chuẩn mực: gọi `InitializeComponent()`, chuyển tiếp sự kiện `Click`/`KeyDown` sang method ViewModel, chuyển tiếp `PasswordBox.Password` / `OpenFileDialog` / hộp thoại xác nhận `MessageBox`, và xử lý cuộn ngang/dọc mượt mà (`PreviewMouseWheel`).

### Tổng quát thư mục
Chứa trọn bộ **15 màn hình hoàn chỉnh** (30 tệp `.xaml` và `.xaml.cs`): 2 cửa sổ khung (`LoginWindow`, `MainWindow`) và 13 `UserControl` phân đúng 3 thư mục con (`Admin/` 6 màn hình, `Staff/` 4 màn hình, `Customer/` 3 màn hình).

### 9.1. Cửa sổ Khung chính (`CinemaManageProject/Views/`)
- **[LoginWindow.xaml](CinemaManageProject/Views/LoginWindow.xaml) & [LoginWindow.xaml.cs](CinemaManageProject/Views/LoginWindow.xaml.cs)** *(223 / 71 dòng)*: Cửa sổ đăng nhập & đăng ký hội viên (áp dụng `App.ApplyDarkTitleBar(this)`), hỗ trợ nhấn `Enter` ở `PasswordBox`, 3 nút điền nhanh tài khoản mẫu `Admin`/`Staff`/`Customer` và gọi `App.CompleteLogin()` khi xác thực thành công.
- **[MainWindow.xaml](CinemaManageProject/Views/MainWindow.xaml) & [MainWindow.xaml.cs](CinemaManageProject/Views/MainWindow.xaml.cs)** *(194 / 146 dòng)*: Khung Shell gồm Sidebar đóng/mở gọn (`PanelLeftContract24` / `PanelLeftExpand24`), Topbar làm mới & đăng xuất, `ScrollViewer` chính (`MainScrollViewer`) và `ContentControl` (`DashboardHost`) chọn động 13 màn hình con bằng `switch`. Tích hợp cơ chế `OnMainScrollPreviewMouseWheel` chuyển tiếp cuộn chuột mượt mà khi con trỏ nằm trên `DataGrid`, `ListBox` hoặc `ScrollViewer` lồng nhau.

### 9.2. Phân hệ Admin (`CinemaManageProject/Views/Admin/`)
- **[AdminDashboardView.xaml](CinemaManageProject/Views/Admin/AdminDashboardView.xaml) & [.xaml.cs](CinemaManageProject/Views/Admin/AdminDashboardView.xaml.cs)** *(148 / 11 dòng)*: 4 thẻ KPI toàn rạp và 2 bảng *Lịch chiếu & Tỷ lệ lấp đầy*, *Hóa đơn gần đây* (binding thuần, code-behind chỉ gọi `InitializeComponent()`).
- **[AdminMoviesView.xaml](CinemaManageProject/Views/Admin/AdminMoviesView.xaml) & [.xaml.cs](CinemaManageProject/Views/Admin/AdminMoviesView.xaml.cs)** *(408 / 70 dòng)*: Thanh lọc phim, bảng danh mục phim bên trái và form thêm/sửa phim bên phải (chọn ảnh poster qua `OpenFileDialog`, gán nhiều thể loại, thêm thể loại mới, xác nhận `MessageBox` trước khi xóa).
- **[AdminRoomsView.xaml](CinemaManageProject/Views/Admin/AdminRoomsView.xaml) & [.xaml.cs](CinemaManageProject/Views/Admin/AdminRoomsView.xaml.cs)** *(410 / 29 dòng)*: Danh sách 4 phòng chiếu, bản đồ ghế trực quan (ghế đôi rộng gấp đôi), bảng thông tin ghế đang chọn + lịch sử đơn đặt và nút bật/tắt bảo trì ghế.
- **[AdminShowtimesView.xaml](CinemaManageProject/Views/Admin/AdminShowtimesView.xaml) & [.xaml.cs](CinemaManageProject/Views/Admin/AdminShowtimesView.xaml.cs)** *(339 / 49 dòng)*: Bộ lọc ngày/phòng/trạng thái, danh sách suất chiếu, form lập/cập nhật suất hiển thị xem trước giờ kết thúc (`+15p`) cùng bảng giá 3 hạng ghế và nút xóa có xác nhận.
- **[AdminOrdersView.xaml](CinemaManageProject/Views/Admin/AdminOrdersView.xaml) & [.xaml.cs](CinemaManageProject/Views/Admin/AdminOrdersView.xaml.cs)** *(652 / 24 dòng)*: 4 thẻ KPI doanh thu, thanh lọc 5 tiêu chí, bảng hóa đơn, 2 thẻ phân tích cơ cấu PTTT & doanh thu theo phim, cùng panel chi tiết phiếu tính tiền và danh sách vé của hóa đơn đang chọn.
- **[AdminUsersView.xaml](CinemaManageProject/Views/Admin/AdminUsersView.xaml) & [.xaml.cs](CinemaManageProject/Views/Admin/AdminUsersView.xaml.cs)** *(449 / 73 dòng)*: Bộ lọc tài khoản, bảng nhân sự & hội viên, form thêm/sửa tài khoản kèm `PasswordBox` đặt/đổi mật khẩu (tự xóa trống qua sự kiện `PasswordResetRequested`), nút khóa/mở khóa và xóa có xác nhận.

### 9.3. Phân hệ Staff (`CinemaManageProject/Views/Staff/`)
- **[StaffDashboardView.xaml](CinemaManageProject/Views/Staff/StaffDashboardView.xaml) & [.xaml.cs](CinemaManageProject/Views/Staff/StaffDashboardView.xaml.cs)** *(144 / 11 dòng)*: 4 thẻ KPI ca làm việc và 2 bảng *Suất chiếu mở bán & Ghế trống*, *Giao dịch quầy gần đây*.
- **[StaffPosView.xaml](CinemaManageProject/Views/Staff/StaffPosView.xaml) & [.xaml.cs](CinemaManageProject/Views/Staff/StaffPosView.xaml.cs)** *(515 / 107 dòng)*: Bố cục 2 tầng thống nhất với Customer Browse — phía trên là dải poster phim trượt ngang có nút chevron trái/phải; phía dưới chia cột trái danh sách suất mở bán (`ShowtimeCardItemStyle`) và cột phải gồm sơ đồ ghế (`BookingSeatButtonStyle`) ở trên + khung thanh toán 2 cột (tra cứu hội viên, đổi điểm, nút chọn tiền mặt nhanh, xuất phiếu) ở dưới.
- **[StaffCheckInView.xaml](CinemaManageProject/Views/Staff/StaffCheckInView.xaml) & [.xaml.cs](CinemaManageProject/Views/Staff/StaffCheckInView.xaml.cs)** *(248 / 33 dòng)*: Ô quét/nhập mã vé hỗ trợ phím `Enter`, thẻ đối chiếu thông tin vé/ghế/suất chiếu có mã màu trạng thái, nút xác nhận `CheckedIn` và bảng hàng chờ vé bên dưới.
- **[StaffCustomersView.xaml](CinemaManageProject/Views/Staff/StaffCustomersView.xaml) & [.xaml.cs](CinemaManageProject/Views/Staff/StaffCustomersView.xaml.cs)** *(212 / 23 dòng)*: Chip lọc 4 hạng thẻ + ô tìm kiếm, danh sách hội viên, bảng lịch sử hóa đơn và form đăng ký hội viên tại quầy / cập nhật thông tin liên hệ.

### 9.4. Phân hệ Customer (`CinemaManageProject/Views/Customer/`)
- **[CustomerDashboardView.xaml](CinemaManageProject/Views/Customer/CustomerDashboardView.xaml) & [.xaml.cs](CinemaManageProject/Views/Customer/CustomerDashboardView.xaml.cs)** *(409 / 87 dòng)*: Thẻ hội viên đầu trang, dải poster phim trượt ngang (hỗ trợ nút chevron & `Shift + Cuộn chuột`), khung spotlight giới thiệu phim chi tiết và bảng *Vé của tôi*.
- **[CustomerBrowseView.xaml](CinemaManageProject/Views/Customer/CustomerBrowseView.xaml) & [.xaml.cs](CinemaManageProject/Views/Customer/CustomerBrowseView.xaml.cs)** *(441 / 98 dòng)*: Bộ lọc trạng thái/thể loại/từ khóa + dải poster phim trượt ngang, tóm tắt phim đang chọn, danh sách suất mở bán bên trái, sơ đồ chọn ghế và khung thanh toán trực tuyến (`SelfService` — đổi điểm thưởng, 3 PTTT online) bên phải.
- **[CustomerMyTicketsView.xaml](CinemaManageProject/Views/Customer/CustomerMyTicketsView.xaml) & [.xaml.cs](CinemaManageProject/Views/Customer/CustomerMyTicketsView.xaml.cs)** *(233 / 39 dòng)*: Bộ lọc trạng thái vé, danh sách vé kèm cuống vé điện tử hiển thị `TicketCode` check-in, nút hủy vé có hộp thoại xác nhận `MessageBox` và bảng lịch sử toàn bộ hóa đơn.

---

## 10. Thư mục `CinemaManageProject.Tests/` — Kiểm thử Tự động (`xUnit`)

### Công nghệ & Thư viện dùng chung
- **`xUnit 2.9.3` + `Microsoft.NET.Test.Sdk 17.14.1` + `xunit.runner.visualstudio 3.1.1`:** Bộ khung kiểm thử chuẩn .NET chạy trực tiếp qua `dotnet test CinemaManageProject.slnx` và Visual Studio Test Explorer.
- **Cô lập dữ liệu bằng `ICollectionFixture<CinemaDatabaseFixture>`:** Lưu bản sao `cinema.seed.bak` trong thư mục chạy của project test, xả connection pool (`SqliteConnection.ClearAllPools()`) và khôi phục nguyên trạng `cinema.db` trước mỗi test — đảm bảo `dotnet test` chạy lặp lại ổn định và không làm bẩn CSDL nguồn của ứng dụng.

### Tổng quát thư mục
Chứa **18 ca kiểm thử tích hợp thực tế trên SQLite** (tập trung vào 3 luồng rủi ro cao nhất: xác thực/đăng ký, giao dịch đặt ghế và hủy vé hoàn điểm).

### Chi tiết từng file
- **[CinemaManageProject.Tests.csproj](CinemaManageProject.Tests/CinemaManageProject.Tests.csproj)**: Khai báo project test `net10.0-windows` (`UseWPF=true`), tham chiếu `CinemaManageProject` và 3 gói `xUnit`.
- **[CinemaDatabaseFixture.cs](CinemaManageProject.Tests/CinemaDatabaseFixture.cs)**: Định nghĩa `[CollectionDefinition("CinemaDatabase")]` chạy tuần tự tránh xung đột ghi SQLite và cung cấp `ResetDatabase()`.
- **[AuthTests.cs](CinemaManageProject.Tests/AuthTests.cs)** *(8 ca kiểm thử)*: Đăng nhập thành công cho cả 3 vai trò `admin`/`staff1`/`khach1` kèm kiểm tra xóa hash khỏi `User.Password` & nạp `CustomerProfile`; chặn sai mật khẩu; chặn tài khoản bị khóa; đăng ký khách mới lưu hash `PBKDF2`; chặn trùng `Username`/`Phone`; kiểm tra cơ chế tự nâng cấp mật khẩu plain-text cũ lên `PBKDF2`.
- **[BookingTests.cs](CinemaManageProject.Tests/BookingTests.cs)** *(6 ca kiểm thử)*: Đặt vé quầy `Counter` kèm đổi điểm/tiền thối & kiểm chứng cộng điểm/chi tiêu; đặt vé `SelfService` (`StaffUserId = NULL`); chặn ghế bảo trì (`B1`); chặn đặt trùng ghế đã `Booked`; chặn `SelfService` trả tiền mặt; chặn vượt quá giới hạn `10` ghế/đơn.
- **[CancelTicketTests.cs](CinemaManageProject.Tests/CancelTicketTests.cs)** *(4 ca kiểm thử)*: Hủy một phần (`PartiallyRefunded`, giải phóng ghế ngay lập tức) rồi hủy hết (`Cancelled`, khôi phục `TotalSpent` & `Tier`); chặn hủy vé đã `CheckedIn`; chặn khách khác hủy nhầm vé không thuộc quyền sở hữu; chặn hủy lặp lại vé đã hủy.

---

## 11. Thư mục `docs/` — Sơ đồ Use Case, ERD, Ảnh Demo & Đặc tả Thiết kế

### Công nghệ & Quy ước dùng chung
- **Markdown + Mermaid (`flowchart LR`, `erDiagram`):** Biểu diễn trực quan sơ đồ Use Case và ERD hiển thị trực tiếp trên GitHub.
- **PNG Screenshots:** Ảnh chụp kết xuất trực tiếp từ giao diện WPF thực tế chạy trên `cinema.db` mẫu.
- **Quy trình Design Spec → Implementation Plan:** Lưu lại hồ sơ thiết kế và kế hoạch triển khai theo từng mốc ngày.

### Chi tiết từng thư mục con & file
- **Thư mục con `docs/diagrams/` (2 sơ đồ kiến trúc):**
  - [use-case.md](docs/diagrams/use-case.md): Sơ đồ Use Case đầy đủ cho 3 tác nhân (`Admin`, `Staff`, `Customer`) + khách chưa đăng nhập và bảng ánh xạ màn hình.
  - [erd.md](docs/diagrams/erd.md): Sơ đồ ERD trọn bộ 10 bảng SQLite và bảng mô tả 13 ràng buộc khóa ngoại (`CASCADE` / `RESTRICT` / `SET NULL`).
- **Thư mục con `docs/screenshots/` (6 ảnh demo màn hình thực tế):**
  - [01-login.png](docs/screenshots/01-login.png), [02-admin-dashboard.png](docs/screenshots/02-admin-dashboard.png), [03-admin-rooms.png](docs/screenshots/03-admin-rooms.png), [04-staff-pos.png](docs/screenshots/04-staff-pos.png), [05-customer-dashboard.png](docs/screenshots/05-customer-dashboard.png), [06-customer-browse.png](docs/screenshots/06-customer-browse.png).
- **Thư mục con `docs/superpowers/specs/` (6 tệp đặc tả thiết kế):**
  - [2026-11-19-admin-movies-design.md](docs/superpowers/specs/2026-11-19-admin-movies-design.md) *(54 dòng)*: Đặc tả màn hình Quản lý Phim, thể loại và tải poster.
  - [2026-11-19-admin-rooms-design.md](docs/superpowers/specs/2026-11-19-admin-rooms-design.md) *(40 dòng)*: Đặc tả màn hình Phòng chiếu cố định & bảo trì sơ đồ ghế.
  - [2026-11-20-admin-showtimes-design.md](docs/superpowers/specs/2026-11-20-admin-showtimes-design.md) *(29 dòng)*: Đặc tả màn hình Quản lý Lịch chiếu (`+15 phút`, kiểm tra trùng phòng, giá thống nhất).
  - [2026-11-20-customer-poster-strip-design.md](docs/superpowers/specs/2026-11-20-customer-poster-strip-design.md) *(31 dòng)*: Đặc tả dải trượt ngang poster & khung spotlight chi tiết trên Dashboard Khách hàng.
  - [2026-11-21-admin-orders-design.md](docs/superpowers/specs/2026-11-21-admin-orders-design.md) *(63 dòng)*: Đặc tả màn hình Hóa đơn & phân tích Doanh thu Admin.
  - [2026-11-21-admin-users-design.md](docs/superpowers/specs/2026-11-21-admin-users-design.md) *(55 dòng)*: Đặc tả màn hình Quản trị Nhân sự & Tài khoản, đồng bộ `Users` ↔ `Customers` và bảo vệ `Admin`.
- **Thư mục con `docs/superpowers/plans/` (6 tệp kế hoạch triển khai):**
  - [2026-11-19-admin-movies-view.md](docs/superpowers/plans/2026-11-19-admin-movies-view.md) *(31 dòng)*: Kế hoạch từng bước xây dựng `AdminMoviesView` & `MovieData`.
  - [2026-11-19-admin-rooms.md](docs/superpowers/plans/2026-11-19-admin-rooms.md) *(47 dòng)*: Kế hoạch xây dựng `AdminRoomsView` & `RoomData`.
  - [2026-11-20-admin-showtimes.md](docs/superpowers/plans/2026-11-20-admin-showtimes.md) *(64 dòng)*: Kế hoạch xây dựng `AdminShowtimesView` & `ShowtimeData`.
  - [2026-11-20-customer-poster-strip.md](docs/superpowers/plans/2026-11-20-customer-poster-strip.md) *(124 dòng)*: Kế hoạch nâng cấp `CustomerDashboardView` với dải poster và spotlight.
  - [2026-11-21-admin-orders.md](docs/superpowers/plans/2026-11-21-admin-orders.md) *(90 dòng)*: Kế hoạch xây dựng `AdminOrdersView` & `OrderData`.
  - [2026-11-21-admin-users.md](docs/superpowers/plans/2026-11-21-admin-users.md) *(95 dòng)*: Kế hoạch xây dựng `AdminUsersView` & `UserData`.

---

## 12. Phân tích Chuyên sâu: Giao dịch Đặt ghế, Ràng buộc Khóa ngoại & Phân quyền

### 12.1. Giao dịch Đặt ghế & Hủy vé (`SqliteTransaction` trong `BookingData.cs`)
Để ngăn chặn tuyệt đối tình trạng **đặt trùng ghế (double-booking)**, sai lệch tiền hoặc rò rỉ điểm thưởng khi nhiều bước ghi diễn ra liên tiếp, cả hai nghiệp vụ cốt lõi đều được bọc trọn trong một giao dịch nguyên tử `using SqliteTransaction tx = conn.BeginTransaction();`:

1. **Luồng đặt ghế (`BookingData.CreateOrder`):**
   - **Kiểm tra đầu vào trước giao dịch:** danh sách ghế không rỗng (loại trùng `Distinct()`), tối đa `10` ghế/đơn; kiểm tra hợp lệ kênh (`Counter` bắt buộc có `StaffUserId`; `SelfService` bắt buộc có `CustomerId` và cấm PTTT `Cash`).
   - **Kiểm tra lại trạng thái thực tế trong transaction:**
     - Suất chiếu tồn tại, `Showtimes.Status = 'OpenForSale'` và `Rooms.IsActive = 1`.
     - Toàn bộ `SeatId` được chọn phải thuộc đúng `Showtime.RoomId` và có `Seats.IsActive = 1` (chặn ghế bảo trì như ghế `B1` ở Phòng 1).
     - Truy vấn trực tiếp bảng `Tickets`: nếu **bất kỳ ghế nào** đã tồn tại vé ở trạng thái `Booked` hoặc `CheckedIn` cho cùng `ShowtimeId` → trả về lỗi ngay và rollback toàn bộ giao dịch.
   - **Tự tính lại giá vé & điểm thưởng ở tầng Data (không tin số liệu tính từ UI):**
     - `UnitPrice = Math.Round((Showtime.BaseTicketPrice + Room.BasePriceSurcharge) * Seat.PriceMultiplier, 0)`.
     - Giới hạn điểm đổi: `0 ≤ PointsUsed ≤ min(Customer.LoyaltyPoints, floor(SubTotal * 50% / 1.000đ))`.
     - Kiểm tra tiền khách đưa (`CashReceived ≥ TotalAmount` nếu trả tiền mặt `Cash`), tính `ChangeAmount` và điểm tích lũy `EarnedPoints = floor(TotalAmount / 10.000đ)`.
   - **Ghi nguyên tử & Commit:** `INSERT INTO Orders` → nhận `OrderId` sinh mã `HD-yyyyMMdd-xxxx` → `INSERT INTO Tickets` với từng mã vé `VE-yyMMdd-xxxx-SeatCode` → `UPDATE Customers` cập nhật `LoyaltyPoints`, `TotalSpent` và nâng hạng `Tier` → `tx.Commit()`. Mọi ngoại lệ hoặc lỗi ràng buộc đều tự động rollback.

2. **Luồng hủy vé (`BookingData.CancelCustomerTicket`):**
   - Trong `SqliteTransaction`, xác minh đúng quyền sở hữu (`Orders.CustomerId == customerId`), vé đang ở trạng thái `Booked` (chặn vé đã `CheckedIn` hoặc `Cancelled`), hóa đơn chưa hủy và suất chiếu chưa kết thúc (`OpenForSale` / `Scheduled`).
   - Đánh dấu `Tickets.Status = 'Cancelled'` (giải phóng ghế ngay lập tức cho người khác đặt).
   - Nếu đơn vẫn còn vé hiệu lực → chuyển `Orders.Status = 'PartiallyRefunded'` và giảm `Orders.TotalAmount` theo tỷ lệ; nếu hủy hết toàn bộ vé → chuyển `Orders.Status = 'Cancelled'`, `TotalAmount = 0`.
   - Hoàn điều chỉnh `LoyaltyPoints`, trừ lại `TotalSpent` và tính lại hạng thẻ `Tier` tương ứng rồi `tx.Commit()`.

### 12.2. Ràng buộc Khóa ngoại & Toàn vẹn Dữ liệu SQLite (`PRAGMA foreign_keys = ON`)
Mặc định SQLite tắt kiểm tra khóa ngoại trên mỗi kết nối mới. Vì vậy [DatabaseHelper.OpenConnection()](CinemaManageProject/Helpers/DatabaseHelper.cs) **luôn thực thi `PRAGMA foreign_keys = ON;`** ngay khi mở kết nối, cưỡng chế nghiêm ngặt toàn bộ **13 khóa ngoại** trong [CinemaManageDB.sql](CinemaManageProject/Database/CinemaManageDB.sql) (chi tiết sơ đồ xem tại [docs/diagrams/erd.md](docs/diagrams/erd.md)):
- **Nhóm `ON DELETE RESTRICT` (7 khóa ngoại — bảo vệ lịch sử vận hành & kế toán):** `Showtimes` (`MovieId`, `RoomId`, `CreatedByUserId`), `Orders.StaffUserId`, `Tickets` (`ShowtimeId`, `SeatId`, `CheckedInByUserId`). Nhờ đó CSDL tự chặn ở tầng thấp nhất mọi thao tác xóa phim/phòng/suất/ghế/nhân viên đã phát sinh giao dịch.
- **Nhóm `ON DELETE CASCADE` (4 khóa ngoại — thành phần phụ thuộc chặt):** `MovieGenres` (`MovieId`, `GenreId`), `Seats.RoomId`, `Tickets.OrderId` — tự động dọn dữ liệu phụ thuộc khi bản ghi cha hợp lệ bị xóa.
- **Nhóm `ON DELETE SET NULL` (2 khóa ngoại — giữ nguyên hồ sơ độc lập):** `Customers.UserId` (cho phép hội viên quầy không có tài khoản App) và `Orders.CustomerId` (cho phép khách vãng lai & không làm mất doanh thu kế toán).

### 12.3. Phân quyền theo 3 Vai trò (`Admin`, `Staff`, `Customer`) & Bảo mật
Phiên đăng nhập được lưu qua `App.CurrentUser` (`UserRole`), mọi mật khẩu lưu DB đều băm một chiều bằng `PBKDF2-HMAC-SHA256` (100.000 vòng lặp, salt 16 byte) và bị xóa khỏi `User.Password` trong RAM ngay sau khi đăng nhập. Lưu ý: toàn bộ tài khoản demo trong `SeedData.sql` **chỉ dùng cho mục đích học tập và trình diễn đồ án**.

| Chức năng / Tài nguyên | `Admin` | `Staff` | `Customer` | Cơ chế thực thi trong mã nguồn |
| :--- | :---: | :---: | :---: | :--- |
| **Điều hướng Sidebar** | 6 màn hình quản trị | 4 màn hình quầy | 3 màn hình hội viên | `MainViewModel.BuildSidebar(UserRole)` chỉ nạp đúng các `SidebarMenuItem` của vai trò đang đăng nhập. |
| **Quản lý Phim, Phòng, Lịch chiếu, Hóa đơn** | Toàn quyền | Không truy cập | Không truy cập | Cô lập hoàn toàn trong nhánh `Admin` của `MainViewModel`; `ShowtimeData` khóa sửa suất đã có vé, `RoomData` chặn bảo trì ghế đang có vé. |
| **Quản trị Tài khoản (`UserData`)** | Quản lý `Staff`/`Customer` | Không truy cập | Chỉ tự đăng ký mới | `UserData` chặn gán/sửa/khóa/xóa tài khoản `Admin` gốc và chặn tự khóa/tự xóa chính mình (`currentAdminUserId`). |
| **Bán vé tại quầy (`Counter`)** | Không dùng | Toàn quyền (4 PTTT + tiền mặt) | Không truy cập | `BookingData.CreateOrder` bắt buộc `StaffUserId > 0` khi `BookingChannel == "Counter"`. |
| **Soát vé Check-in (`CheckInData`)** | Không dùng | Xác nhận `CheckedIn` | Không truy cập | Ghi vết `CheckedInAt` & `CheckedInByUserId`; chặn check-in trùng hoặc vé/đơn/suất đã hủy. |
| **Đặt vé trực tuyến (`SelfService`)** | Không dùng | Không dùng | Đặt bằng tài khoản của mình | `BookingData.CreateOrder` bắt buộc `CustomerId > 0`, cấm trả tiền mặt `Cash` (`SelfService` chỉ chấp nhận `BankTransfer`/`EWallet`/`Card`). |
| **Xem & Hủy vé (`CustomerMyTickets`)** | Xem qua Hóa đơn | Xem qua Check-in | Chỉ xem & hủy vé của chính mình | `LoadMyTickets`/`LoadMyOrders` lọc theo `CustomerId`; `CancelCustomerTicket` xác minh `Orders.CustomerId == customerId` trong transaction. |
