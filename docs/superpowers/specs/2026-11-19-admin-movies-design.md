# Thiết kế Màn hình Quản lý Phim & Thể loại (`Admin`)

## 1. Mục tiêu & Phạm vi
* **Mục tiêu:** Kích hoạt mục **Quản lý Phim** (`movies`) trên Sidebar của vai trò `Admin` để thêm, sửa, tìm kiếm, lọc trạng thái, gán nhiều thể loại, gắn ảnh poster và xóa an toàn các bộ phim trong `Database/cinema.db`.
* **Ngoài phạm vi (Non-goals):** Không chỉnh sửa suất chiếu (`Showtimes`), phòng chiếu (`Rooms`), đặt vé (`Orders`/`Tickets`), hay thay đổi giao diện màn hình `CustomerDashboardView`.

## 2. Phương án kiến trúc đã chọn
* **Phương án 1 (Đã chọn): Bố cục 2 cột trên cùng 1 màn hình (`AdminMoviesView`)**
  * **Lý do chọn:** Đồng bộ phong cách tối giản của `AdminDashboardView`, thao tác chọn phim bên trái → sửa ngay bên phải mượt và nhanh, không cần tạo thêm cửa sổ `Window` phụ.
  * **Phương án từng cân nhắc & bỏ qua:** Bảng full chiều ngang + Popup `MovieFormWindow` riêng (rộng bảng hơn nhưng phát sinh thêm Window quản lý vòng đời và tách rời ngữ cảnh danh sách).

## 3. Cấu trúc File & Điều hướng
1. [CinemaManageProject/Themes/Controls.xaml](../../CinemaManageProject/Themes/Controls.xaml)
   * Bổ sung style Dark Theme cho `CheckBox` (`ModernCheckBoxStyle`) và `ComboBox` (`ModernComboBoxStyle`), đồng thời chỉnh `ModernTextBoxStyle` hỗ trợ `VerticalContentAlignment` (giúp ô nhập mô tả nhiều dòng căn lề trên đẹp mắt).
2. `CinemaManageProject/ViewModels/Admin/AdminMoviesViewModel.cs`
   * Kế thừa `ViewModelBase`, dùng trực tiếp `App.OpenDbConnection()`, tuân thủ thuộc tính viết tường minh + `OnPropertyChanged`.
3. `CinemaManageProject/Views/Admin/AdminMoviesView.xaml` & `.xaml.cs`
   * Code-behind tối giản: khởi tạo `InitializeComponent()`, xử lý `Click` gọi method public trên ViewModel, mở `Microsoft.Win32.OpenFileDialog` chọn ảnh poster và hiện `MessageBox` xác nhận trước khi xóa phim.
4. [CinemaManageProject/ViewModels/MainViewModel.cs](../../CinemaManageProject/ViewModels/MainViewModel.cs) & [CinemaManageProject/Views/MainWindow.xaml.cs](../../CinemaManageProject/Views/MainWindow.xaml.cs)
   * Mở khóa item `"movies"` (`isAvailable: true`), cập nhật `SelectMenu` chuyển đổi giữa `AdminDashboardViewModel` (`"dashboard"`) và `AdminMoviesViewModel` (`"movies"`), và bổ sung case tương ứng trong `ShowCurrentDashboard()`.

## 4. Thiết kế Giao diện & Nghiệp vụ chi tiết

### 4.1. Bố cục màn hình (`AdminMoviesView`)
* **Thanh tiêu đề & thao tác:** Tiêu đề *Quản lý Phim*, phụ đề mô tả ngắn, nút `+ Thêm phim mới`.
* **Thanh tìm kiếm & lọc:** Ô tìm kiếm theo tên phim / tên gốc / đạo diễn, các nút lọc trạng thái (`Tất cả`, `Đang chiếu`, `Sắp chiếu`, `Ngừng chiếu`) và bộ đếm số phim khớp.
* **Cột trái (`DataGrid`):** Hiển thị `Tên phim`, `Thể loại`, `Thời lượng`, `Độ tuổi`, `Trạng thái`. Click chọn dòng sẽ đổ dữ liệu phim và các thể loại đã gán sang form bên phải.
* **Cột phải (Form chi tiết phim):**
  * Header chế độ (`Thêm phim mới` / `Chỉnh sửa phim`) + thanh báo lỗi / thông báo thành công.
  * Khung xem trước Poster (`90×128`) + nút `Chọn ảnh...` và `Xóa ảnh`.
  * Các trường nhập liệu: `Title`*, `OriginalTitle`, `Director`, `Cast`, `DurationMinutes`*, `Language`*, `ReleaseDate`* (`dd/MM/yyyy` hoặc `yyyy-MM-dd`), `EndDate`, `AgeRating` (`P`, `K`, `T13`, `T16`, `T18`), `Status` (`ComingSoon`, `NowShowing`, `Ended`), `Description`.
  * Danh sách `CheckBox` chọn nhiều thể loại (`Genres` + `MovieGenres`) kèm ô nhập `+ Thể loại mới` và nút `Thêm`.
  * Cụm nút đáy form: `[Lưu phim]` (`PrimaryButtonStyle`), `[Làm mới]` (`SecondaryButtonStyle`), `[Xóa phim]` (`DangerButtonStyle` — chỉ bật khi đang sửa phim đã tồn tại).

### 4.2. Quy tắc Dữ liệu & Kiểm tra hợp lệ
* **Validation khi lưu:**
  * Bắt buộc có `Title`, `Language`, `DurationMinutes` là số nguyên `> 0`.
  * `ReleaseDate` đúng định dạng ngày; nếu nhập `EndDate` thì phải đúng định dạng và `>= ReleaseDate`.
  * Phải chọn ít nhất 1 thể loại.
* **Lưu vào SQLite (`SqliteTransaction`):**
  * `INSERT` hoặc `UPDATE` bảng `Movies`.
  * Đồng bộ lại `MovieGenres` (xóa liên kết cũ của `MovieId` rồi chèn lại các `GenreId` đang được tick).
  * Lưu ngày chuẩn `yyyy-MM-dd` vào SQLite (`ReleaseDate`, `EndDate`) khớp `SeedData.sql`.
* **Xử lý Poster:**
  * Khi chọn ảnh từ máy, copy file ảnh vào `Path.Combine(AppContext.BaseDirectory, "Posters")` với tên file duy nhất và lưu đường dẫn tương đối (`Posters/<ten-file>`) vào `Movies.PosterPath`.
  * Hiển thị ảnh bằng `BitmapCacheOption.OnLoad` để không khóa file trên ổ đĩa.
* **Quy tắc Xóa an toàn:**
  * Kiểm tra `SELECT COUNT(*) FROM Showtimes WHERE MovieId = @id`.
  * Nếu `> 0`: từ chối xóa cứng và hiện thông báo hướng dẫn đổi trạng thái sang `Ngừng chiếu` (`Ended`).
  * Nếu `= 0`: xác nhận qua `MessageBox`, xóa khỏi `Movies` (liên kết `MovieGenres` tự xóa nhờ `ON DELETE CASCADE`).

## 5. Tiêu chí Nghiệm thu
* `dotnet build CinemaManageProject.slnx -p:UseSharedCompilation=false` đạt `0 Warning(s)`, `0 Error(s)`.
* Kiểm thử thực tế đủ luồng: chuyển qua lại `Dashboard Quản trị` ↔ `Quản lý Phim`, tìm kiếm/lọc phim, thêm phim mới + thêm thể loại + chọn poster, sửa phim, kiểm tra chặn xóa phim đang có suất chiếu và xóa thành công phim mới tạo chưa có suất chiếu.
