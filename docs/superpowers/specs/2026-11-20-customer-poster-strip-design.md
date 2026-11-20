# Thiết kế Thanh Poster Giới thiệu Phim trên Dashboard Khách hàng (`CustomerDashboardView`)

## 1. Mục tiêu & Phạm vi
* **Mục tiêu:** Thay thế lưới card toàn chữ hiện tại ở mục **"Phim tại CINEVERSE"** trên màn hình `CustomerDashboardView` bằng **thanh trượt poster ngang (Horizontal Poster Strip)** kết hợp **khung "Giới thiệu phim" chi tiết ngay bên dưới** theo **Phương án A**.
* **Đối tượng phục vụ:** Vai trò `Customer` (`Trang chủ Hội viên`), giúp duyệt nhanh 15 phim bằng hình ảnh poster thật đã import (`CinemaManageProject/Posters/`), đọc giới thiệu phim trực quan mà vẫn giữ bảng **"Vé của tôi"** dễ quan sát trên màn hình.
* **Ngoài phạm vi (Non-goals):**
  * Chưa thay đổi Dashboard của `Admin` hoặc `Staff`.
  * Chưa tạo nút đặt vé / chuyển màn hình chọn ghế (`Booking`) hoặc logic hệ khuyến nghị (chỉ chuẩn bị sẵn cấu trúc chọn phim sạch để gắn vào sau).
  * Không sửa lược đồ CSDL `CinemaManageDB.sql` hay `SeedData.sql`.

## 2. Phương án Kiến trúc đã chốt
Đã đối chiếu 2 phương án cho giao diện Khách hàng:
* **Phương án A (Được chọn):** Thanh trượt poster ngang (các thẻ poster đứng đồng đều có nút điều hướng `‹` / `›` hai bên) + 1 khung giới thiệu chi tiết ngay bên dưới cập nhật theo phim được chọn.
* **Phương án B (Loại):** Chỉ giữ thanh poster tối giản kèm `ToolTip`, không có khung mô tả/đạo diễn/diễn viên rõ ràng cho mục tiêu "giới thiệu phim".

**Cách tổ chức kỹ thuật:**
1. **Dữ liệu & Ảnh (`CustomerDashboardViewModel`):** Tái sử dụng `PosterHelper.ResolvePosterFullPath` và `PosterHelper.LoadPosterBitmap` để nạp sẵn `PosterImage` (`ImageSource?`, an toàn khi thiếu file), cùng thông tin `OriginalTitle`, `Director`, `Cast`, `Description`, `ReleaseDate` vào `CustomerMovieRow : ViewModelBase`.
2. **Trạng thái chọn & bộ lọc mượt (`ActiveTag`):** Bấm chọn poster chỉ đổi thuộc tính `IsSelected` (`ActiveTag = "Active"`) qua `INotifyPropertyChanged` mà không dựng lại collection; bộ lọc `Tất cả` / `Đang chiếu` / `Sắp chiếu` tô sáng nút đang chọn và tự giữ phim đang chọn nếu còn trong kết quả (hoặc tự chọn phim đầu tiên).
3. **Giao diện XAML Dark Zinc (`Themes/Controls.xaml` + `CustomerDashboardView.xaml`):**
   * Tái sử dụng `BaseButtonStyle` cho `FilterChipButtonStyle` và `PosterCardButtonStyle` (không viết thêm `ControlTemplate` phức tạp).
   * Cố định khung poster tỉ lệ dọc (`Width="168"` cho card, vùng ảnh cao `224px`) với `Stretch="UniformToFill"` và `ClipToBounds="True"` để đồng đều giữa 12 poster dọc và 3 poster ngang.
   * Code-behind tối giản trong `CustomerDashboardView.xaml.cs`: xử lý nút `‹` / `›`, cập nhật trạng thái bật/tắt nút cuộn ở hai đầu, reset cuộn về `0` khi đổi bộ lọc, và chuyển tiếp cuộn chuột dọc lên `ScrollViewer` cha trong `MainWindow.xaml`.

## 3. Cấu trúc File sẽ tạo / chỉnh sửa
1. `CinemaManageProject/Themes/Controls.xaml`: Thêm `FilterChipButtonStyle` và `PosterCardButtonStyle` (kế thừa `SecondaryButtonStyle` -> `BaseButtonStyle`, dùng trigger `Tag="Active"`).
2. `CinemaManageProject/ViewModels/Customer/CustomerDashboardViewModel.cs`: Mở rộng `CustomerMovieRow : ViewModelBase`, truy vấn thêm các cột giới thiệu + `PosterPath`, thêm `SelectedMovie`, `HasSelectedMovie`, `HasNoMovies`, `SelectMovie(...)` và trạng thái tag của 3 nút lọc.
3. `CinemaManageProject/Views/Customer/CustomerDashboardView.xaml` & `.xaml.cs`: Bố cục lại mục **"Phim tại CINEVERSE"** thành dải poster trượt ngang + khung **"Giới thiệu phim"** bên dưới thẻ hội viên và phía trên **"Vé của tôi"**.

## 4. Tiêu chí Nghiệm thu
* Build sạch `0 Warning(s)`, `0 Error(s)`.
* Kiểm thử STA WPF + SQLite xác nhận nạp đủ `15` poster thực tế cho tài khoản `khach1`, chọn poster đổi đúng nội dung khung giới thiệu, đổi bộ lọc không làm vỡ lựa chọn, không lỗi khi gặp `PosterPath` rỗng/thiếu file, đo layout sạch ở kích thước chuẩn `988×860` và tối thiểu `788×660` (ứng với `MinWidth="1080"` của `MainWindow`).
