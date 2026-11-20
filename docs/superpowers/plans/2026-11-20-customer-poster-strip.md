# CustomerDashboard Poster Strip Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Thay lưới card chữ 3 cột ở mục **Phim tại CINEVERSE** trên `CustomerDashboardView` bằng dải poster trượt ngang kèm khung **Giới thiệu phim** đồng bộ theo phim được chọn (chỉ áp dụng cho `Customer`).

**Architecture:** Giữ nguyên kiến trúc MVVM gọn nhẹ của dự án (`ViewModelBase` + code-behind `Click` tối giản, không dùng `ICommand` hay `ControlTemplate` phức tạp). Mở rộng `CustomerMovieRow` thành lớp kế thừa `ViewModelBase` để bật tắt viền chọn (`ActiveTag`) tại chỗ, nạp poster tái sử dụng `PosterHelper`, thêm 2 button style nhỏ trong `Themes/Controls.xaml` dựa trên `SecondaryButtonStyle`, và xử lý cuộn ngang `PosterStripScrollViewer` + chuyển tiếp cuộn dọc ở `CustomerDashboardView.xaml.cs`.

**Tech Stack:** `.NET 10 Windows` (`net10.0-windows`), WPF, SQLite (`Microsoft.Data.Sqlite`), `AGENTS.md` Dark Zinc Theme (`#09090B` / `#141516` / `#18181B` / `#5E6AD2`).

**Spec:** [2026-11-20-customer-poster-strip-design.md](../specs/2026-11-20-customer-poster-strip-design.md)

## Global Constraints
* Tuân thủ `AGENTS.md`: dùng `DatabaseHelper.OpenConnection()`, parameterized query, không thêm package hay converter thừa, không sửa Dashboard của `Admin` và `Staff`.
* Toàn bộ `Style` dùng chung (`FilterChipButtonStyle`, `PosterCardButtonStyle`) đặt trong `CinemaManageProject/Themes/Controls.xaml`, ghi mã màu trực tiếp (không `{DynamicResource}`), kế thừa `SecondaryButtonStyle` để tận dụng `BaseButtonStyle`.
* Tái sử dụng `PosterHelper.ResolvePosterFullPath`, `PosterHelper.LoadPosterBitmap` và `FormatHelper.*` sẵn có; không nhân bản hàm nạp ảnh hay định dạng ngày/tiền.
* Sau chỉnh sửa, build solution/project đạt `0 Warning(s)`, `0 Error(s)` và kiểm thử qua STA WPF + SQLite trên bản sao CSDL cô lập.

## Review Focus
* **Ảnh ngang vs ảnh dọc (3/15 ảnh khổ ngang):** khung ảnh trong cả thẻ strip (`168` card, vùng ảnh cao `224`) và khung giới thiệu (`148×214`) phải dùng `Stretch="UniformToFill"` + `ClipToBounds="True"` + `RenderOptions.BitmapScalingMode="HighQuality"` để không méo hình hay lệch chiều cao thẻ.
* **Poster null hoặc file không tồn tại:** `TryLoadPoster(string? storedPath)` trả về `null` an toàn khi rỗng/thiếu file/lỗi đọc, UI hiển thị nền `#18181B` kèm chữ `"Chưa có poster"` phía sau `<Image>`.
* **Xung đột con lăn chuột trong `ScrollViewer` lồng nhau:** `OnPosterStripPreviewMouseWheel` không được nuốt cuộn dọc của `MainWindow` (trừ khi giữ `Shift` để cuộn ngang), mà re-raise sự kiện wheel lên `ScrollViewer` cha qua `VisualTreeHelper.GetParent`.
* **Đồng bộ `SelectedMovie` khi lọc / làm mới / danh sách rỗng:** `ApplyFilter()` phải giữ lại phim có cùng `MovieId` nếu vẫn nằm trong danh sách sau lọc, ngược lại chọn phim đầu danh sách; khi danh sách rỗng (`0` phim), `SelectedMovie = null`, `HasSelectedMovie == false`, `HasNoMovies == true`.
* **Ổn định vị trí cuộn ngang:** click chọn poster trên dải chỉ cập nhật `IsSelected` trên `CustomerMovieRow`, không xóa/nạp lại `Movies`; chỉ reset `HorizontalOffset` về `0` khi đổi bộ lọc (`SetFilter`).

---

### Task 1: Thêm `FilterChipButtonStyle` và `PosterCardButtonStyle` vào `Themes/Controls.xaml`

**Files:**
- Modify: `CinemaManageProject/Themes/Controls.xaml:113-167`

**Interfaces:**
- Produces (XAML resource keys trong `Themes/Controls.xaml`):
  - `FilterChipButtonStyle` (`TargetType="Button"`, `BasedOn="{StaticResource SecondaryButtonStyle}"`)
  - `PosterCardButtonStyle` (`TargetType="Button"`, `BasedOn="{StaticResource SecondaryButtonStyle}"`)

- [ ] **Step 1: Thêm 2 style dùng chung ngay sau `NavMenuButtonStyle` trong `CinemaManageProject/Themes/Controls.xaml`**
  * `FilterChipButtonStyle`: đặt `Padding="12,6"`, `FontSize="12"`, `<Trigger Property="Tag" Value="Active">` chuyển `Background="#265E6AD2"`, `BorderBrush="#5E6AD2"`, `Foreground="#FAFAFA"`, `FontWeight="SemiBold"`.
  * `PosterCardButtonStyle`: đặt `Background="#141516"`, `BorderBrush="#23252A"`, `BorderThickness="1"`, `Padding="10"`, `HorizontalContentAlignment="Stretch"`, `VerticalContentAlignment="Stretch"`; `<Trigger Property="IsMouseOver" Value="True">` đổi `Background="#18181B"`, `BorderBrush="#3F3F46"`; `<Trigger Property="Tag" Value="Active">` đổi `Background="#1B1E2E"`, `BorderBrush="#5E6AD2"`. (Kế thừa sẵn `ControlTemplate` từ `BaseButtonStyle` nên không tạo template mới.)

- [ ] **Step 2: Kiểm tra nạp ResourceDictionary và biên dịch**
  Run: `dotnet build --no-restore CinemaManageProject.slnx -p:UseSharedCompilation=false`
  Expected: `Build succeeded. 0 Warning(s), 0 Error(s)`.

---

### Task 2: Mở rộng `CustomerMovieRow` và logic chọn/giới thiệu phim trong `CustomerDashboardViewModel.cs`

**Files:**
- Modify: `CinemaManageProject/ViewModels/Customer/CustomerDashboardViewModel.cs:1-222`

**Interfaces:**
- Consumes: `PosterHelper.ResolvePosterFullPath`, `PosterHelper.LoadPosterBitmap`, `FormatHelper.FormatVnd`, `FormatHelper.FormatSqlDateTime`, `FormatHelper.FormatStatusLabel`, `DatabaseHelper.OpenConnection`, `DatabaseHelper.ReadDecimal`
- Produces:
  - Class `CustomerMovieRow : ViewModelBase` với các thuộc tính:
    - `int MovieId`, `string Title`, `string? OriginalTitle`, `bool HasOriginalTitle`
    - `string Genres`, `string MetaInfo`, `string AgeRating`, `string StatusDisplay`, `string PriceText`, `bool IsNowShowing`
    - `string ReleaseDateText`, `string DirectorText`, `string CastText`, `string DescriptionText`
    - `ImageSource? PosterImage`, `bool HasPoster`
    - `bool IsSelected`, `string? ActiveTag` (`IsSelected ? "Active" : null`)
  - Properties & Methods trên `CustomerDashboardViewModel`:
    - `CustomerMovieRow? SelectedMovie { get; private set; }`
    - `bool HasSelectedMovie => SelectedMovie is not null`
    - `bool HasNoMovies => Movies.Count == 0`
    - `string? AllFilterTag`, `string? NowShowingFilterTag`, `string? ComingSoonFilterTag`
    - `void SelectMovie(CustomerMovieRow? movie)`
    - `void SetFilter(string? filter)`

- [ ] **Step 1: Refactor `CustomerMovieRow` thành `public sealed class CustomerMovieRow : ViewModelBase` trong `CinemaManageProject/ViewModels/Customer/CustomerDashboardViewModel.cs`**
  Khai báo đầy đủ property bất biến cho metadata/giới thiệu (`DirectorText`, `CastText`, `DescriptionText`, `ReleaseDateText`, `HasOriginalTitle`, `PosterImage`) và property `IsSelected` phát `OnPropertyChanged` cho `IsSelected` và `ActiveTag`.

- [ ] **Step 2: Cập nhật `LoadData()`, `TryLoadPoster(string? storedPath)`, `SelectMovie(...)` và `ApplyFilter()`**
  * Truy vấn `Movies` lấy thêm `m.MovieId, m.OriginalTitle, m.Director, m.Cast, m.ReleaseDate, m.Description, m.PosterPath`; ghép chuỗi nhãn rõ ràng (`DirectorText`, `CastText`, fallback `"Đang cập nhật"` / `"Chưa có mô tả giới thiệu cho phim này."`).
  * Định dạng `ReleaseDateText = $"Khởi chiếu {FormatHelper.FormatSqlDateTime(releaseRaw, "dd/MM/yyyy")}"`.
  * `TryLoadPoster(string? storedPath)`: trả về `null` nếu trống hoặc `!File.Exists(...)`, bọc `try/catch` quanh `PosterHelper.LoadPosterBitmap(fullPath)`.
  * Trong `SelectedFilter` setter: phát `OnPropertyChanged` cho `AllFilterTag`, `NowShowingFilterTag`, `ComingSoonFilterTag` và gọi `ApplyFilter()`.
  * Trong `ApplyFilter()`: giữ `SelectedMovie.MovieId` trước đó nếu còn trong kết quả sau lọc, nếu không chọn phần tử đầu (hoặc `null` khi rỗng) qua `SelectMovie(...)`; phát `OnPropertyChanged(nameof(HasNoMovies))`.

- [ ] **Step 3: Xác minh biên dịch dự án**
  Run: `dotnet build --no-restore CinemaManageProject.slnx -p:UseSharedCompilation=false`
  Expected: `Build succeeded. 0 Warning(s), 0 Error(s)`.

---

### Task 3: Thiết kế Dải Poster Trượt Ngang & Khung Giới thiệu Phim trong `CustomerDashboardView.xaml` và `.xaml.cs`

**Files:**
- Modify: `CinemaManageProject/Views/Customer/CustomerDashboardView.xaml:57-130`
- Modify: `CinemaManageProject/Views/Customer/CustomerDashboardView.xaml.cs:1-21`

**Interfaces:**
- Consumes: `CustomerDashboardViewModel` (`Movies`, `SelectedMovie`, `HasSelectedMovie`, `HasNoMovies`, `AllFilterTag`, `NowShowingFilterTag`, `ComingSoonFilterTag`, `SetFilter`, `SelectMovie`) và styles `FilterChipButtonStyle`, `PosterCardButtonStyle`, `CardBorderStyle`, `BadgeBorderStyle`, `BoolToVis`
- Code-behind handlers trên `CustomerDashboardView.xaml.cs`:
  - `OnFilterClick`, `OnMovieCardClick`, `OnScrollLeftClick`, `OnScrollRightClick`, `OnPosterStripScrollChanged`, `OnPosterStripPreviewMouseWheel`

- [ ] **Step 1: Cập nhật vùng `PHIM TẠI CINEVERSE` trong `CinemaManageProject/Views/Customer/CustomerDashboardView.xaml`**
  * Thay 3 nút lọc bằng `Style="{StaticResource FilterChipButtonStyle}"`, đặt `x:Name` (`FilterAllButton`, `FilterNowShowingButton`, `FilterComingSoonButton`) và gắn `Tag="{Binding AllFilterTag}"`, `Tag="{Binding NowShowingFilterTag}"`, `Tag="{Binding ComingSoonFilterTag}"` (tránh đè `Tag` dùng để nhận diện bộ lọc).
  * Thay `<UniformGrid Columns="3" />` bằng `Grid` 3 cột chứa:
    * Cột 0: nút điều hướng `‹` (`x:Name="ScrollLeftButton"`, `Width="34"`, `Height="80"`, `Click="OnScrollLeftClick"`).
    * Cột 1: `<ScrollViewer x:Name="PosterStripScrollViewer" HorizontalScrollBarVisibility="Hidden" VerticalScrollBarVisibility="Disabled" ScrollChanged="OnPosterStripScrollChanged" PreviewMouseWheel="OnPosterStripPreviewMouseWheel">` chứa `<ItemsControl ItemsSource="{Binding Movies}">` dùng `<StackPanel Orientation="Horizontal" />`. Mỗi item là `<Button Style="{StaticResource PosterCardButtonStyle}" Width="168" Margin="0,0,12,0" Tag="{Binding ActiveTag}" Click="OnMovieCardClick">` với khung poster cố định `Height="224"` (`Background="#18181B"`, `CornerRadius="8"`, `ClipToBounds="True"` chứa placeholder `"Chưa có poster"` và `<Image Source="{Binding PosterImage}" Stretch="UniformToFill" RenderOptions.BitmapScalingMode="HighQuality" />`), bên dưới là badge trạng thái/độ tuổi, tiêu đề 2 dòng (`TextTrimming="CharacterEllipsis"`), thể loại 1 dòng và `PriceText`.
    * Cột 2: nút điều hướng `›` (`x:Name="ScrollRightButton"`, `Width="34"`, `Height="80"`, `Click="OnScrollRightClick"`).
  * Thêm trạng thái rỗng khi `HasNoMovies == true`.
  * Ngay dưới dải poster, đặt khung **"Giới thiệu phim"** (`Visibility="{Binding HasSelectedMovie, Converter={StaticResource BoolToVis}}"`) chứa `<Border Style="{StaticResource CardBorderStyle}" DataContext="{Binding SelectedMovie}">` gồm cột trái ảnh poster `Width="148"` (`Height="214"`) và cột phải: badges (`StatusDisplay`, `AgeRating`, `ReleaseDateText`), tiêu đề + `OriginalTitle` (`HasOriginalTitle`), `Genres` + `MetaInfo`, `DirectorText` + `CastText`, đoạn mô tả `DescriptionText` (`TextWrapping="Wrap"`), và chân khung hiển thị giá vé `Từ ...đ` (`#5E6AD2`, `Bold`).

- [ ] **Step 2: Bổ sung xử lý cuộn & chọn phim gọn sạch trong `CinemaManageProject/Views/Customer/CustomerDashboardView.xaml.cs`**
  * `OnFilterClick`: ánh xạ `btn.Name` (`FilterNowShowingButton => "NowShowing"`, `FilterComingSoonButton => "ComingSoon"`, `_ => "All"`), gọi `vm.SetFilter(filter)` và `PosterStripScrollViewer.ScrollToHorizontalOffset(0)`.
  * `OnMovieCardClick`: lấy `(sender as FrameworkElement)?.DataContext as CustomerMovieRow` và gọi `vm.SelectMovie(movie)`.
  * `OnScrollLeftClick` / `OnScrollRightClick`: dịch chuyển `HorizontalOffset` theo bước `360` px (khoảng 2 thẻ poster).
  * `OnPosterStripScrollChanged`: cập nhật `ScrollLeftButton.IsEnabled` (`HorizontalOffset > 0.5`) và `ScrollRightButton.IsEnabled` (`HorizontalOffset + 0.5 < ScrollableWidth`).
  * `OnPosterStripPreviewMouseWheel`: nếu `Keyboard.Modifiers == ModifierKeys.Shift` thì cuộn ngang; ngược lại tạo `MouseWheelEventArgs` mới (`RoutedEvent = UIElement.MouseWheelEvent`) chuyển lên parent để `MainWindow` vẫn cuộn dọc bình thường.

- [ ] **Step 3: Viết và chạy kiểm thử STA WPF + SQLite xác minh toàn bộ Review Focus & hồi quy**
  Kiểm tra tự động bằng harness STA (chạy trên bản sao `Database/cinema.db` + `Posters/`):
  * `cust.Movies.Count == 15` và 15/15 item có `PosterImage != null`.
  * Mặc định chọn phim đầu (`cust.SelectedMovie != null`), `SelectedMovie.ActiveTag == "Active"`, còn lại `null`.
  * Chọn phim `Mai` -> `SelectedMovie.DirectorText` chứa `"Trấn Thành"`, `HasOriginalTitle == false`; chọn `Exhuma` (`Quật Mộ Trùng Ma`) -> `HasOriginalTitle == true`.
  * Đổi bộ lọc sang `"NowShowing"` khi đang chọn `Mai` -> giữ nguyên `Mai`; đổi sang `"ComingSoon"` -> tự chuyển sang phim sắp chiếu đầu tiên, `Movies.Count == 5`, `ComingSoonFilterTag == "Active"`.
  * Kiểm thử `PosterHelper` an toàn khi gặp `PosterPath` không tồn tại -> `PosterImage == null`, không ngoại lệ.
  * Đo render `CustomerDashboardView` (`Measure` + `Arrange` + `UpdateLayout`) ở `988×860` và `788×660` (chiều rộng tối thiểu của `MainWindow`), xác nhận `ScrollLeftButton`, `ScrollRightButton`, `PosterStripScrollViewer` khởi tạo sạch và không có ngoại lệ binding.
  * Chạy lại toàn bộ regression (`CheckDashboards`, `CheckShowtimes`, `CheckRooms`, `CheckFilters`, `SmokeTest`) và `dotnet build --no-restore CinemaManageProject.slnx -p:UseSharedCompilation=false` (`0 Warning(s)`, `0 Error(s)`).

- [ ] **Step 4: Commit thay đổi theo Conventional Commits**
  * Commit gọn thay đổi giao diện + ViewModel (`feat(customer): add horizontal poster strip and movie spotlight to dashboard`) sau khi tất cả kiểm thử pass.
