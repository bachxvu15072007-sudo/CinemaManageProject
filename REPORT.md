# Báo cáo Tổng hợp Hỏi – Đáp Kỹ thuật & Kiến trúc Dự án CINEVERSE CINEMA

Tài liệu này tổng hợp đầy đủ các câu hỏi và phần giải thích chi tiết về **WPF, MVVM, XAML, C#, SQLite và kiến trúc mã nguồn** của dự án `CinemaManageProject`, giúp bạn nắm vững luồng chạy và có thể tự tin giải thích hoặc mở rộng tính năng về sau.

---

## Mục lục
1. [Cách đọc hiểu dự án WPF/MVVM và các khái niệm cốt lõi](#1-cách-đọc-hiểu-dự-án-wpfmvvm-và-các-khái-niệm-cốt-lõi)
2. [Giao diện & XAML: `StaticResource` vs `DynamicResource`, gộp `Controls.xaml`](#2-giao-diện--xaml-staticresource-vs-dynamicresource-gộp-controlsxaml)
3. [Cấu trúc `ControlTemplate`, `TemplateBinding`, `BoxBorder`, `PART_ContentHost` và `BoolToVis`](#3-cấu-trúc-controltemplate-templatebinding-boxborder-part_contenthost-và-booltovis)
4. [`ViewModelBase`, `INotifyPropertyChanged`, `OnPropertyChanged` và `CinemaHelper`](#4-viewmodelbase-inotifypropertychanged-onpropertychanged-và-cinemahelper)
5. [`DataContext`, Data Binding và `ObservableCollection<SidebarMenuItem>`](#5-datacontext-data-binding-và-observablecollectionsidebarmenuitem)
6. [Điều hướng Dashboard: `DataTemplate` vs `switch` trong `MainWindow.xaml.cs`](#6-điều-hướng-dashboard-datatemplate-vs-switch-trong-mainwindowxamlcs)
7. [Phân tích chi tiết vòng đời `App.xaml.cs` và luồng `LoginWindow` / `LoginViewModel`](#7-phân-tích-chi-tiết-vòng-đời-appxamlcs-và-luồng-loginwindow--loginviewmodel)
8. [Tại sao trong C# hay đặt hàm gọi ở trên và hàm helper ở dưới?](#8-tại-sao-trong-c-hay-đặt-hàm-gọi-ở-trên-và-hàm-helper-ở-dưới)
9. [Thiết kế CSDL SQLite, truy vấn thuần vs EF Core và tách lớp truy cập dữ liệu](#9-thiết-kế-csdl-sqlite-truy-vấn-thuần-vs-ef-core-và-tách-lớp-truy-cập-dữ-liệu)
10. [Quá trình tách lớp `Helpers/` – `Data/`, Quản lý Phòng, Quản lý Lịch chiếu và Dữ liệu Khuyến nghị](#10-quá-trình-tách-lớp-helpers--data-quản-lý-phòng-quản-lý-lịch-chiếu-và-dữ-liệu-khuyến-nghị)
11. [Quản lý ảnh Poster (`PosterHelper`), Dải trượt ngang & `ScrollViewer` lồng nhau](#11-quản-lý-ảnh-poster-posterhelper-dải-trượt-ngang--scrollviewer-lồng-nhau)
12. [Khai báo Icon Vector (`<Geometry>`), Nền điện ảnh (`DrawingBrush`) và Sidebar đóng/mở](#12-khai-báo-icon-vector-geometry-nền-điện-ảnh-drawingbrush-và-sidebar-đóngmở)
13. [Hóa đơn & Doanh thu, Nhân sự & Tài khoản, và Chuẩn hóa Kiến trúc `Data/` – `Controls.xaml`](#13-hóa-đơn--doanh-thu-nhân-sự--tài-khoản-và-chuẩn-hóa-kiến-trúc-data--controlsxaml)

---

## 1. Cách đọc hiểu dự án WPF/MVVM và các khái niệm cốt lõi

### Luồng đi dữ liệu chuẩn của ứng dụng
```text
[View (.xaml)]  <── Data Binding ──>  [ViewModel (.cs)]  <── SQL / Model ──>  [Database/cinema.db]
      │                                      ▲
      └──── Click event (.xaml.cs) ──────────┘
```

### 5 khái niệm cốt lõi trong MVVM hiện tại của dự án
1. **`DataContext`**: Là "ngữ cảnh dữ liệu" gắn vào một `Window` hoặc `UserControl`. Khi gán `DataContext = _viewModel;`, mọi `{Binding ...}` trong file `.xaml` tương ứng sẽ tự tìm thuộc tính bên trong `_viewModel`.
2. **Data Binding (`{Binding ...}`)**: Cơ chế nối trực tiếp UI với dữ liệu trong ViewModel mà không cần tự gán tay kiểu `txtError.Text = ...` mỗi khi dữ liệu thay đổi.
3. **`INotifyPropertyChanged` (`OnPropertyChanged`)**: Hợp đồng giúp ViewModel "hét lên" báo cho WPF biết một thuộc tính vừa đổi giá trị để giao diện tự vẽ lại.
4. **Xử lý sự kiện nút bấm (`Click` thay cho `ICommand`)**: Dự án đã bỏ `RelayCommand`/`ICommand` để code gọn dễ đọc. Nút bấm trên `.xaml` gắn `Click="OnLoginClick"`, file `.xaml.cs` chỉ làm đúng 1 việc là chuyển tiếp lời gọi vào hàm public trên ViewModel (ví dụ `_viewModel.Login(LoginPasswordBox.Password)`).
5. **`ObservableCollection<T>`**: Danh sách đặc biệt của WPF. Khi bạn `Add()`, `Remove()`, `Clear()` phần tử, UI (`ItemsControl`, `DataGrid`) tự cập nhật theo mà không cần gán lại `ItemsSource`.

### Mẹo debug nhanh khi đọc code
- Đặt breakpoint tại `OnStartup` trong [App.xaml.cs](CinemaManageProject/App.xaml.cs) để xem app khởi tạo DB và mở `LoginWindow`.
- Đặt breakpoint tại `Login()` trong [LoginViewModel.cs](CinemaManageProject/ViewModels/LoginViewModel.cs) để theo dõi câu SQL kiểm tra tài khoản.
- Đặt breakpoint tại `ShowCurrentDashboard()` trong [MainWindow.xaml.cs](CinemaManageProject/Views/MainWindow.xaml.cs) để xem màn hình con được chuyển đổi ra sao.

---

## 2. Giao diện & XAML: `StaticResource` vs `DynamicResource`, gộp `Controls.xaml`

| Tiêu chí | `{StaticResource ...}` | `{DynamicResource ...}` |
| :--- | :--- | :--- |
| **Thời điểm lấy giá trị** | Lấy **1 lần duy nhất** lúc nạp XAML. | Theo dõi liên tục; tự cập nhật nếu đổi Resource lúc app đang chạy. |
| **Khi nào dùng** | Màu cố định, `Style`, font chữ, kích thước, Converter (`BoolToVis`). | Khi cần tính năng **đổi theme nóng** (Light ↔ Dark) khi app đang mở. |
| **Hiệu năng & Độ phức tạp** | Nhanh, nhẹ, dễ kiểm soát lỗi ngay lúc load. | Tốn chi phí theo dõi hơn và dễ rối nếu lạm dụng. |

### Vì sao dự án gộp về một file [Themes/Controls.xaml](CinemaManageProject/Themes/Controls.xaml)?
- Dự án chỉ sử dụng **một Dark Theme cố định** (bảng màu Zinc + điểm nhấn Indigo `#5E6AD2`), không chuyển đổi Light/Dark lúc runtime.
- Việc bỏ `DarkTheme.xaml`, `LightTheme.xaml`, `SharedTokens.xaml`, `ThemeManager.cs` giúp [App.xaml](CinemaManageProject/App.xaml) chỉ còn 1 dòng nạp duy nhất `<ResourceDictionary Source="Themes/Controls.xaml" />`, không cần `DynamicResource` và mã màu được viết trực tiếp (ví dụ `#09090B`, `#141516`, `#FAFAFA`, `#23252A`) ngay trong style để dễ đọc.

---

## 3. Cấu trúc `ControlTemplate`, `TemplateBinding`, `BoxBorder`, `PART_ContentHost` và `BoolToVis`

### 3.1. `ControlTemplate` trong `ModernPasswordBoxStyle` ([Controls.xaml](CinemaManageProject/Themes/Controls.xaml#L221-L251))
Mặc định của WPF, `PasswordBox` và `TextBox` có khung viền vuông góc cứng và hiệu ứng focus kiểu Windows cũ, **không có thuộc tính `CornerRadius` để bo góc trực tiếp**. Vì vậy phải viết lại `Template` (bộ xương hiển thị bên trong control):

- **`Setter Property="Template"` / `<ControlTemplate TargetType="PasswordBox">`**: Thay thế diện mạo mặc định của `PasswordBox` bằng giao diện tự thiết kế.
- **`x:Name="BoxBorder"`**: Đặt tên cho `<Border>` bọc ngoài để các `<Trigger>` bên dưới (như `IsKeyboardFocused="True"` hoặc `IsMouseOver="True"`) có thể đổi màu `BorderBrush` thành `#5E6AD2` hoặc `#3F3F46`.
- **`{TemplateBinding ...}`**: Ràng buộc thuộc tính của phần tử bên trong template với chính giá trị được gán ở ngoài control (ví dụ `Background="{TemplateBinding Background}"`, `Padding="{TemplateBinding Padding}"`). Nhờ đó `Setter` ở ngoài vẫn có tác dụng lên `Border` bên trong.
- **`<ScrollViewer x:Name="PART_ContentHost" ... />`**: `PART_ContentHost` là **tên quy ước bắt buộc của WPF**. WPF tìm đúng phần tử có tên `PART_ContentHost` bên trong template để nhét vùng nhập ký tự thật (chữ, dấu chấm mật khẩu `•••`, con trỏ nháy) vào đó. Thiếu tên này sẽ không gõ được vào ô nhập.

### 3.2. Converter `BoolToVis` ([Controls.xaml](CinemaManageProject/Themes/Controls.xaml#L7))
```xml
<BooleanToVisibilityConverter x:Key="BoolToVis" />
```
- **Bản chất:** Đây là thẻ tự đóng tạo ra một đối tượng `new BooleanToVisibilityConverter()` có sẵn trong WPF và đặt tên khóa là `BoolToVis`.
- **Vì sao chỉ có mỗi `x:Key`?** Vì class chuẩn này của WPF không cần cấu hình thêm tham số nào — mặc định nó biến `true` → `Visibility.Visible` và `false` → `Visibility.Collapsed`.
- **Ví dụ sử dụng:**
  - `Visibility="{Binding HasError, Converter={StaticResource BoolToVis}}"` trong [LoginWindow.xaml](CinemaManageProject/Views/LoginWindow.xaml#L49): có lỗi thì hiện hộp thông báo, hết lỗi thì ẩn hẳn (không chiếm chỗ).
  - `Visibility="{Binding IsLoginMode, Converter={StaticResource BoolToVis}}"` ([LoginWindow.xaml](CinemaManageProject/Views/LoginWindow.xaml#L63)) và `IsRegisterMode` ([LoginWindow.xaml](CinemaManageProject/Views/LoginWindow.xaml#L145)): bật/tắt luân phiên giữa form Đăng nhập và form Đăng ký.
  - `Visibility="{Binding HasNoTickets, Converter={StaticResource BoolToVis}}"` trong [CustomerDashboardView.xaml](CinemaManageProject/Views/Customer/CustomerDashboardView.xaml#L152).

---

## 4. `ViewModelBase`, `INotifyPropertyChanged`, `OnPropertyChanged` và `CinemaHelper`

### 4.1. `ViewModelBase` ([ViewModelBase.cs](CinemaManageProject/ViewModels/ViewModelBase.cs))
Toàn bộ ViewModel kế thừa `ViewModelBase` (triển khai `INotifyPropertyChanged`):
- Cài đặt chuẩn `public event PropertyChangedEventHandler? PropertyChanged;`.
- Hàm `protected void OnPropertyChanged(string propertyName)` gọi `PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));` để thông báo cho WPF cập nhật UI.
- Hàm `public virtual void LoadData() { }` để các Dashboard con override khi cần tải/làm mới dữ liệu.

### 4.2. Vì sao viết tường minh `_field = value; OnPropertyChanged(...)` thay cho `SetProperty<T>`?
Trước đây dùng helper generic `SetProperty<T>(ref T field, T value)` để gộp 3 bước so sánh, gán và báo đổi. Sau khi đơn giản hóa:
- Với các property đơn giản:
  ```csharp
  public string Username
  {
      get => _username;
      set { _username = value; OnPropertyChanged(nameof(Username)); }
  }
  ```
- Với các property lọc/chọn có thể tốn công nạp lại dữ liệu khi gán giá trị trùng (ví dụ `SelectedFilter` trong `CustomerDashboardViewModel`), giữ guard kiểm tra trùng:
  ```csharp
  if (_selectedFilter == value) return;
  ```
Cách viết tường minh giúp người mới đọc code hiểu ngay từng bước "lưu biến private → báo UI đổi" mà không cần nhảy qua hàm generic phụ trợ.

### 4.3. Tại sao tách các hàm dùng chung ra `CinemaHelper.cs`?
[CinemaHelper.cs](CinemaManageProject/ViewModels/CinemaHelper.cs) gom các hàm tiện ích dùng lặp lại ở nhiều ViewModel:
- Định dạng tiền/ngày/trạng thái: `FormatVnd`, `ReadDecimal`, `FormatSqlDateTime`, `FormatPaymentMethod`, `FormatStatusLabel`.
- Đọc hồ sơ khách hàng từ DB: `LoadCustomerProfile` (được gọi cả khi đăng nhập ở `LoginViewModel` và lúc tải trang khách hàng ở `CustomerDashboardViewModel`).

Nhờ tách về một chỗ:
- Sửa 1 nơi là toàn app đồng bộ (tránh copy-paste lặp code giữa các ViewModel).
- Giữ được file nhẹ ngay trong thư mục `ViewModels/` thay vì tạo thêm nhiều lớp Service/Repository phức tạp.

---

## 5. `DataContext`, Data Binding và `ObservableCollection<SidebarMenuItem>`

### 5.1. Những chỗ gán `DataContext` trong dự án
1. [LoginWindow.xaml.cs](CinemaManageProject/Views/LoginWindow.xaml.cs#L23): `DataContext = _viewModel;` — gắn toàn bộ cửa sổ đăng nhập với `LoginViewModel`.
2. [MainWindow.xaml.cs](CinemaManageProject/Views/MainWindow.xaml.cs#L27): `DataContext = _viewModel;` — gắn khung cửa sổ chính (sidebar, tiêu đề, nút đăng xuất) với `MainViewModel`.
3. [MainWindow.xaml.cs](CinemaManageProject/Views/MainWindow.xaml.cs#L39): `view.DataContext = _viewModel.CurrentDashboard;` — gắn từng trang Dashboard con (`AdminDashboardView`, `StaffDashboardView`, `CustomerDashboardView`) với ViewModel tương ứng của trang đó.

### 5.2. Cơ chế nối `ErrorMessage` và `ToggleMode` trên màn hình Login
- Trong [LoginWindow.xaml](CinemaManageProject/Views/LoginWindow.xaml#L56): `<TextBlock Text="{Binding ErrorMessage}" ... />`.
- WPF tự đọc `DataContext` (`LoginViewModel`) và lấy giá trị từ property `ErrorMessage`.
- Khi `ErrorMessage` được gán giá trị mới trong `LoginViewModel`:
  ```csharp
  ErrorMessage = null;
  ```
  setter của `ErrorMessage` gọi `OnPropertyChanged(nameof(ErrorMessage))` **và** `OnPropertyChanged(nameof(HasError))` → hộp lỗi lập tức cập nhật chữ hoặc tự ẩn đi qua `BoolToVis`.

### 5.3. Giải thích dòng `public ObservableCollection<SidebarMenuItem> MenuItems { get; } = [];` ([MainViewModel.cs](CinemaManageProject/ViewModels/MainViewModel.cs#L79))
- **`ObservableCollection<SidebarMenuItem>`**: Danh sách các mục menu bên trái (`ItemsSource="{Binding MenuItems}"` ở [MainWindow.xaml](CinemaManageProject/Views/MainWindow.xaml#L75)). Khi `BuildSidebar()` chạy `MenuItems.Clear()` rồi `MenuItems.Add(...)`, giao diện sidebar tự động sinh/xóa nút tương ứng.
- **`{ get; }`**: Property chỉ có `get` (read-only property) — bên ngoài chỉ được phép đọc danh sách hoặc gọi `.Add()`/`.Clear()` trên chính list đó, **không thể gán đè** `MenuItems = new ...` làm đứt dây Binding của WPF.
- **`= [];`** (C# 12 Collection Expression): Khởi tạo sẵn một danh sách rỗng ngay khi tạo `MainViewModel` (tương đương `= new ObservableCollection<SidebarMenuItem>();`), đảm bảo `MenuItems` không bao giờ bị `null`.

---

## 6. Điều hướng Dashboard: `DataTemplate` vs `switch` trong `MainWindow.xaml.cs`

Trước đây dự án dùng `<Window.Resources>` chứa `<DataTemplate DataType="...">` trong `MainWindow.xaml`. Hiện tại đã chuyển sang dùng `ContentControl x:Name="DashboardHost"` và hàm `ShowCurrentDashboard()` trong [MainWindow.xaml.cs](CinemaManageProject/Views/MainWindow.xaml.cs#L31-L41):

```csharp
UserControl? view = _viewModel.CurrentDashboard switch
{
    AdminDashboardViewModel => new AdminDashboardView(),
    StaffDashboardViewModel => new StaffDashboardView(),
    CustomerDashboardViewModel => new CustomerDashboardView(),
    _ => null
};
if (view is not null)
    view.DataContext = _viewModel.CurrentDashboard;
DashboardHost.Content = view;
```

### So sánh 3 cách làm
| Cách tiếp cận | Đặc điểm | Đánh giá trong dự án |
| :--- | :--- | :--- |
| **`DataTemplate` trong `MainWindow.xaml`** | WPF tự ánh xạ `ViewModel -> View` qua `<Window.Resources>`. | Ít code C# hơn nhưng hơi "ma thuật" với người mới đọc XAML. |
| **`switch` ở `MainWindow.xaml.cs` (Đang dùng)** | Thấy rõ ràng loại ViewModel nào tạo ra View nào và gán `DataContext` trực tiếp. | Rõ ràng, dễ debug, giữ đúng nguyên tắc ViewModel **không biết** đến UI (`UserControl`). |
| **Đặt `switch` tạo View ngay trong `MainViewModel`** | ViewModel tự `new AdminDashboardView()`. | **Không nên**, vì phá vỡ tách lớp MVVM (tầng logic lại giữ trực tiếp control giao diện). |

---

## 7. Phân tích chi tiết vòng đời `App.xaml.cs` và luồng `LoginWindow` / `LoginViewModel`

### 7.1. Vì sao [App.xaml](CinemaManageProject/App.xaml) và [App.xaml.cs](CinemaManageProject/App.xaml.cs) quản lý cửa sổ & kết nối DB?
Trong WPF, `App` là **điểm vào toàn cục** của cả ứng dụng:
1. **`ShutdownMode="OnExplicitShutdown"` ([App.xaml](CinemaManageProject/App.xaml#L5))**: Mặc định WPF sẽ tự thoát app ngay khi cửa sổ đầu tiên (`LoginWindow`) đóng lại. Bật chế độ này giúp app chỉ thoát khi code gọi `Shutdown()` một cách có chủ đích (dùng cờ `_isSwitching` để phân biệt giữa "đóng cửa sổ để chuyển màn hình" và "người dùng bấm dấu X thoát hẳn app").
2. **Các hàm điều phối vòng đời:**
   - `OnStartup`: Chạy đầu tiên khi mở app, gọi `ShowLoginWindow()`.
   - `ShowLoginWindow()`: Đóng `MainWindow` cũ (nếu có), mở `LoginWindow` mới.
   - `CompleteLogin()`: Đóng `LoginWindow` (với `_isSwitching = true`), khởi tạo `MainWindow` cho `CurrentUser`.
   - `Logout()`: Đặt `CurrentUser = null` và quay lại `ShowLoginWindow()`.
3. **Trạng thái và DB dùng chung:** `public static User? CurrentUser { get; set; }` giữ phiên người dùng đăng nhập hiện tại; `OpenDbConnection()` trả về `SqliteConnection` đã mở sẵn và bật `PRAGMA foreign_keys = ON;`.

### 7.2. Constructor `LoginWindow()` ([LoginWindow.xaml.cs](CinemaManageProject/Views/LoginWindow.xaml.cs#L15-L26))
```csharp
public LoginWindow()
{
    InitializeComponent();

    _viewModel = new LoginViewModel();
    _viewModel.LoginSucceeded += OnLoginSucceeded;
    _viewModel.DemoPasswordRequested += pass => LoginPasswordBox.Password = pass;
    _viewModel.ModeSwitched += ClearPasswords;
    DataContext = _viewModel;

    Loaded += (_, _) => UsernameTextBox.Focus();
}
```
- `InitializeComponent();`: Nạp và dựng toàn bộ cây giao diện từ `LoginWindow.xaml`.
- `_viewModel = new LoginViewModel();`: Tạo bộ não xử lý cho màn hình Login.
- Đăng ký 3 sự kiện từ ViewModel (vì ViewModel không trực tiếp đóng cửa sổ hay đụng `PasswordBox`):
  - `LoginSucceeded`: Gọi `OnLoginSucceeded` để chuyển sang `MainWindow`.
  - `DemoPasswordRequested`: Điền nhanh mật khẩu mẫu vào `LoginPasswordBox.Password`.
  - `ModeSwitched`: Gọi `ClearPasswords()` khi đổi qua lại giữa Đăng nhập và Đăng ký.
- `DataContext = _viewModel;`: Nối giao diện với ViewModel.
- `Loaded += (_, _) => UsernameTextBox.Focus();`: Khi cửa sổ vừa hiện xong, tự động đặt con trỏ gõ vào ô tên đăng nhập.

### 7.3. Backing fields, `IsRegisterMode` vs `IsLoginMode`, `ToggleMode()` và `ClearPasswords()`
- **Các biến backing field (`_isRegisterMode`, `_username`, `_regFullName`, `_regPhone`, `_regUsername`, `_regEmail`, `_errorMessage`):** Lưu giá trị thật bên trong RAM. Property public bên ngoài chỉ là "cửa giao tiếp" vừa trả về biến này, vừa gọi `OnPropertyChanged` khi có thay đổi.
- **Tại sao dùng 1 biến `_isRegisterMode` và suy ra `public bool IsLoginMode => !IsRegisterMode;`?**
  - Đây là nguyên tắc **Single Source of Truth (Một nguồn sự thật duy nhất)**. Nếu tách làm 2 biến độc lập `_isLoginMode` và `_isRegisterMode`, chỉ cần quên cập nhật 1 biến là hai form sẽ cùng hiện hoặc cùng biến mất!
  - Nếu trong tương lai lỡ sửa `IsLoginMode` cùng dính trạng thái đang login (hoặc thêm chế độ thứ 3 như Quên mật khẩu), lúc đó nên dùng `enum AuthMode { Login, Register, ForgotPassword }` thay cho 1 biến `bool`.
  - Khi `IsRegisterMode` thay đổi, setter gọi `OnPropertyChanged` cho cả `nameof(IsRegisterMode)` và `nameof(IsLoginMode)` để hai form bật/tắt đồng bộ.
- **Tại sao `ToggleMode()` gọi `ModeSwitched?.Invoke()` để `ClearPasswords()` xóa ô mật khẩu?**
  - Khi chuyển chế độ Đăng nhập ↔ Đăng ký, các ô `PasswordBox` không bị hủy khỏi RAM mà chỉ bị **ẩn (`Collapsed`)**. Nếu không xóa, mật khẩu người dùng gõ dở bên form này vẫn còn tồn tại âm thầm khi quay lại; đồng thời khi chuyển sang form Đăng ký cũng tránh lộ/giữ lại mật khẩu cũ hoặc mật khẩu điền sẵn từ nút Demo.
  - Vì WPF **không cho Binding 2 chiều vào `PasswordBox.Password`** (lý do bảo mật), ViewModel không thể tự gán `Password = ""`, mà phải phát event `ModeSwitched` nhờ code-behind chạy `ClearPasswords()`.

### 7.4. Hai dòng SQLite và vòng đời kết nối
```csharp
using SqliteConnection conn = App.OpenDbConnection();
using SqliteCommand cmd = conn.CreateCommand();
```
- **Dòng 1:** Mượn một kết nối đang mở tới file `cinema.db` (đã bật `PRAGMA foreign_keys = ON;`).
- **Dòng 2:** Tạo đối tượng câu lệnh SQL (`cmd`) gắn với kết nối `conn` đó để gán `cmd.CommandText`, thêm tham số `cmd.Parameters.AddWithValue(...)` và thực thi (`ExecuteReader` / `ExecuteScalar` / `ExecuteNonQuery`).
- **Từ khóa `using` (C# 8+ using declaration):** Tự động gọi `Dispose()` (đóng câu lệnh và đóng kết nối DB) ngay khi thoát khỏi hàm/scope — kể cả khi có lỗi ném exception — giúp **tránh rò rỉ bộ nhớ và tránh lỗi khóa file SQLite (`database is locked`)**.

### 7.5. Lưu ý các hàm trong `LoginViewModel.cs`
- **`Login(string password)`**: Kiểm tra dữ liệu nhập, truy vấn bảng `Users`, kiểm tra `IsActive` và mật khẩu, cập nhật `LastLoginAt`, tải `CustomerProfile` qua `CinemaHelper.LoadCustomerProfile`, gán `App.CurrentUser` và phát `LoginSucceeded`.
- **`Register(...)`**: Mở `SqliteTransaction tx`, dùng `ScalarExists` kiểm tra trùng `Username` và `Phone`, thêm bản ghi vào `Users`, lấy `SELECT last_insert_rowid()` để chèn tiếp vào `Customers`, `tx.Commit()`, tự gán `App.CurrentUser = new User { ... }` để vào thẳng màn hình chính không cần đăng nhập lại.

---

## 8. Tại sao trong C# hay đặt hàm gọi ở trên và hàm helper ở dưới?

Trong **C/C++**, trình biên dịch đọc tuần tự từ trên xuống nên thường bắt buộc khai báo/định nghĩa hàm được gọi ở phía trên (hoặc viết prototype trước).  
Ngược lại, trong **C#**, trình biên dịch đọc toàn bộ class trước rồi mới biên dịch thân hàm, nên thứ tự trên-dưới không ảnh hưởng tới việc chạy code.

Lý do code C# thường theo quy tắc **Top-Down (Stepdown Rule trong Clean Code)**:
1. Đưa các phần người ngoài cần dùng lên đầu (`fields/properties`, `constructor`, các hàm `public` chính như `Login`, `Register`, `ToggleMode`).
2. Các hàm phụ `private` (như `ScalarExists`) đặt xuống dưới để ai muốn xem tổng quan luồng chính có thể đọc lướt từ trên xuống như đọc mục lục báo, còn chi tiết nhỏ chỉ cuộn xuống xem khi cần.

*Ghi chú:* Nếu bạn thích kiểu đọc tuần tự từ định nghĩa chi tiết tới chỗ gọi (như C/C++), hoàn toàn có thể đưa hàm `private` lên trên trong cùng một class — hành vi chương trình không hề thay đổi.

---

## 9. Thiết kế CSDL SQLite, truy vấn thuần vs EF Core và tách lớp truy cập dữ liệu

### 9.1. Tại sao tách bảng `Users` và `Customers`, còn `Staff` không có bảng riêng?
Theo thiết kế tại [CinemaManageDB.sql](CinemaManageProject/Database/CinemaManageDB.sql):
- **`Users`**: Bảng xác thực & phân quyền chung cho mọi tài khoản đăng nhập (`Admin`, `Staff`, `Customer`), lưu `Username`, `PasswordHash`, `Role`, `FullName`, `Phone`, `Email`, `IsActive`, `LastLoginAt`.
- **`Customers`**: Lưu thông tin nghiệp vụ riêng của khách xem phim (`LoyaltyPoints`, `TotalSpent`, `Tier`). Quan trọng nhất là cột `UserId INTEGER NULL UNIQUE` — cho phép **khách vãng lai mua vé trực tiếp tại quầy** vẫn có hồ sơ khách tích điểm dù chưa tạo tài khoản đăng nhập (`UserId = NULL`).
- **`Staff`**: Hiện chỉ lưu trong `Users` (`Role = 'Staff'`) và liên kết sang nghiệp vụ qua khóa ngoại `Orders.StaffUserId`, `Tickets.CheckedInByUserId`, `Showtimes.CreatedByUserId`.

**Khi làm hệ thống lớn ngoài thực tế:**
- Thường tách hẳn Auth (`Users`) khỏi thông tin cá nhân (`Customers` / `Employees`) để tránh trùng lặp dữ liệu `FullName`, `Phone`, `Email` giữa hai bảng; đồng thời tạo thêm bảng `Employees` (mã nhân viên, ca trực, chi nhánh), **băm mật khẩu thật** (BCrypt/PBKDF2 thay cho mật khẩu mẫu plain-text) và thêm bảng Audit Log.

### 9.2. Dùng SQLite thuần (`Microsoft.Data.Sqlite`) vs Entity Framework Core (EF Core)
- **`Microsoft.Data.Sqlite` (Dự án đang dùng):** Nhẹ, không cần cài thêm package/migration phức tạp, kiểm soát trực tiếp câu SQL và `SqliteTransaction` khi giữ ghế/đặt vé, rất tối ưu cho câu truy vấn thống kê doanh thu.
- **EF Core:** Viết CRUD nhanh qua LINQ (`db.Movies.Where(...)`), an toàn kiểu dữ liệu khi compile, phù hợp dự án lớn có nhiều màn hình nhập liệu nhưng nặng hơn và cần cẩn thận ở các truy vấn báo cáo phức tạp.

### 9.3. Khi nào nên tách SQL ra thư mục riêng (ví dụ `Data/`)?
Hiện tại phần lớn SQL nằm rải rác trong một số ViewModel (`LoginViewModel`, `CustomerDashboardViewModel`, `StaffDashboardViewModel`, `AdminDashboardViewModel`) và kết nối tập trung tại `App.OpenDbConnection()`.  
Khi dự án phát triển thêm các nghiệp vụ nặng như **Đặt vé, Giữ ghế trong Transaction, Lịch chiếu, Quản lý phim/phòng**, nên gom toàn bộ câu SQL sang thư mục `Data/` (hoặc các file helper theo từng nhóm bảng như `BookingData.cs`, `ShowtimeData.cs`) để ViewModel gọn gàng và dễ bảo trì hơn.

---

## 10. Quá trình tách lớp `Helpers/` – `Data/`, Quản lý Phòng, Quản lý Lịch chiếu và Dữ liệu Khuyến nghị

### 10.1. Chia tách từ `CinemaHelper` thành `Helpers/` và `Data/`
Ở các bước mở rộng sau mục 9, file `CinemaHelper.cs` và lời gọi `App.OpenDbConnection()` được tách gọn theo đúng chức năng thành hai tầng rõ ràng:
- **`CinemaManageProject/Helpers/`**:
  - [DatabaseHelper.cs](CinemaManageProject/Helpers/DatabaseHelper.cs): Quản lý `ConnectionString` trỏ tới `Database/cinema.db`, mở kết nối kèm `PRAGMA foreign_keys = ON;` (`OpenConnection()`), đọc số thập phân an toàn (`ReadDecimal()`).
  - [FormatHelper.cs](CinemaManageProject/Helpers/FormatHelper.cs): Định dạng tiền VNĐ (`FormatVnd`), ngày giờ (`FormatSqlDateTime`), nhãn trạng thái/loại phòng/hạng ghế (`FormatStatusLabel`, `FormatRoomType`, `FormatSeatType`).
  - [PosterHelper.cs](CinemaManageProject/Helpers/PosterHelper.cs): Lưu, phân giải đường dẫn và nạp bitmap poster (`ResolvePosterFullPath`, `LoadPosterBitmap`, `SavePosterFile`).
- **`CinemaManageProject/Data/`**: Tách riêng truy vấn SQLite có tham số hóa cho từng module (`CustomerData.cs`, `MovieData.cs`, `RoomData.cs`, `ShowtimeData.cs`), giúp ViewModel chỉ lo trạng thái UI, lọc và validation.

### 10.2. Nghiệp vụ Quản lý Phòng & Sơ đồ Ghế ([RoomData.cs](CinemaManageProject/Data/RoomData.cs), [AdminRoomsViewModel.cs](CinemaManageProject/ViewModels/Admin/AdminRoomsViewModel.cs), [AdminRoomsView.xaml](CinemaManageProject/Views/Admin/AdminRoomsView.xaml))
- **Thông số cố định & trực quan hóa sơ đồ ghế:** Bốn phòng chiếu (`Phòng 1 - 2D`, `Phòng 2 - 2D`, `Phòng 3 - 3D`, `Phòng 4 - IMAX`) có sức chứa vật lý lần lượt `75`, `75`, `90`, `114` ghế. Trong [AdminRoomsViewModel.cs:226-230](CinemaManageProject/ViewModels/Admin/AdminRoomsViewModel.cs#L226-L230), danh sách ghế đọc từ `RoomData.LoadSeatsByRoom(roomId)` được gom nhóm theo `RowIndex` bằng `LINQ` (`GroupBy` → `SeatRowGroup`) để render hai lớp `ItemsControl` lồng nhau theo từng hàng `A, B, C...`.
- **Ghế đôi (`Couple`) bằng 2 ghế đơn & trạng thái màu qua `DataTrigger`:** Style `SeatCellButtonStyle` ([AdminRoomsView.xaml:12-65](CinemaManageProject/Views/Admin/AdminRoomsView.xaml#L12-L65)) đặt ghế thường kích thước `40×34` (`Margin="3"`), khi `IsCouple == true` tự mở rộng thành `Width="86"` (`40 + 40 + 6` phần margin giữa hai ghế nên hàng cuối thẳng hàng khít với các hàng trên). Màu từng ô đổi tự động theo `VisualState` (`VIP`: tím Indigo `#5E6AD2`, `Couple`: tím đậm `#A855F7`, `Maintenance`: đỏ `#E11D48`) và viền trắng `2px` khi `IsSelected == true`.
- **Kiểm tra đơn đặt trước khi chuyển ghế sang bảo trì:** Khi bấm một ghế, `SelectSeat(...)` nạp tối đa `20` hóa đơn/vé gắn với ghế đó (`RoomData.LoadSeatOrders(seatId)`). Khi Admin bấm bật/tắt bảo trì (`ToggleSelectedSeatAvailable` tại [AdminRoomsViewModel.cs:192-219](CinemaManageProject/ViewModels/Admin/AdminRoomsViewModel.cs#L192-L219)), nếu đang chuyển từ khả dụng sang bảo trì, hàm gọi `RoomData.CountActiveTicketsForSeat(seatId)` đếm vé `Booked`/`CheckedIn` ở các suất `Scheduled`/`OpenForSale` — nếu còn vé hiệu lực (ví dụ ghế `E5`) sẽ **chặn** ngay trước khi chạy `RoomData.SetSeatActive(...)`.

### 10.3. Nghiệp vụ Quản lý Lịch chiếu & Đồng nhất giá vé ([ShowtimeData.cs](CinemaManageProject/Data/ShowtimeData.cs), [AdminShowtimesViewModel.cs](CinemaManageProject/ViewModels/Admin/AdminShowtimesViewModel.cs), [AdminShowtimesView.xaml](CinemaManageProject/Views/Admin/AdminShowtimesView.xaml))
- **Đồng nhất giá gốc rạp & xem trước bảng giá:** `ShowtimeData.StandardBaseTicketPrice` ([ShowtimeData.cs:85](CinemaManageProject/Data/ShowtimeData.cs#L85)) cố định ở mức **`80.000đ`** để giá vé không đổi thất thường theo từng phim mà phụ thuộc vào **phụ thu loại phòng** (`BasePriceSurcharge`: 2D `+0đ`, 3D `+20.000đ`, IMAX `+40.000đ`) và **hệ số hạng ghế** (`Standard ×1.00`, `VIP ×1.25`, `Couple ×2.00`). Trong `AdminShowtimesViewModel.UpdatePreviews()` ([lines 533–564](CinemaManageProject/ViewModels/Admin/AdminShowtimesViewModel.cs#L533-L564)), hệ thống tính sẵn giá quy đổi theo 3 hạng ghế và gợi ý định dạng chiếu qua `ShowtimeData.SuggestFormat(roomType, movieLanguage)` khi tạo suất mới.
- **Quy tắc chống trùng lịch & dọn phòng `+15` phút:** Khi tạo hoặc sửa suất chiếu trong `SqliteTransaction` (`ShowtimeData.SaveShowtime` tại [lines 194–254](CinemaManageProject/Data/ShowtimeData.cs#L194-L254)), giờ kết thúc được tính bằng `StartTime + Movie.DurationMinutes + 15 phút`. Hai suất trong cùng một `RoomId` (trừ trạng thái `Cancelled` và chính suất đang sửa `@sid`) bị coi là trùng khi thỏa mãn `StartTime < @end AND EndTime > @start`. Hai suất nối tiếp chạm đúng mốc `EndTime == StartTime` được phép tạo bình thường.
- **Bảo vệ suất đã phát sinh vé (`CanEditScheduleCore`):** Nếu suất đang sửa đã có vé `Booked` hoặc `CheckedIn`, cả `AdminShowtimesViewModel.CanEditScheduleCore` ([lines 130–136](CinemaManageProject/ViewModels/Admin/AdminShowtimesViewModel.cs#L130-L136)) lẫn `ShowtimeData.CheckActiveTicketCoreLock` ([lines 293–335](CinemaManageProject/Data/ShowtimeData.cs#L293-L335)) đều khóa không cho đổi Phim, Phòng, Ngày/Giờ hay Giá gốc (chỉ cho phép cập nhật `Status` sang `Completed`/`Cancelled`). Khi xóa, `CountShowtimeTickets` chặn xóa cứng bất kỳ suất nào đã phát sinh dòng trong `Tickets` (`ON DELETE RESTRICT`).

### 10.4. Cấu trúc dữ liệu mẫu cho Hệ khuyến nghị ([SeedData.sql](CinemaManageProject/Database/SeedData.sql))
Bộ dữ liệu mẫu được mở rộng lên **15 phim** (10 `NowShowing`, 5 `ComingSoon`), **10 thể loại**, **25 khách hàng** (`khach1`–`khach25`), **28 suất chiếu**, **98 hóa đơn** và **160 vé**, tạo ra **95 cặp `(CustomerId, MovieId)` riêng biệt** phân cụm theo gu xem phim (hành động/khoa học viễn tưởng, tâm lý/tình cảm, gia đình/hoạt hình, kinh dị/hài...) sẵn sàng làm đầu vào cho Collaborative Filtering hoặc Content-Based Recommendation.

---

## 11. Quản lý ảnh Poster (`PosterHelper`), Dải trượt ngang & `ScrollViewer` lồng nhau

### 11.1. Sao chép ảnh khi Build & Nạp ảnh an toàn ([CinemaManageProject.csproj](CinemaManageProject/CinemaManageProject.csproj), [PosterHelper.cs](CinemaManageProject/Helpers/PosterHelper.cs#L12-L26))
- Toàn bộ 15 file ảnh `.jpg` đặt tại `CinemaManageProject/Posters/` và được khai báo trong `.csproj`:
  ```xml
  <None Update="Posters\*">
    <CopyToOutputDirectory>PreserveNewest</CopyToOutputDirectory>
  </None>
  ```
  Nhờ đó khi build, thư mục chạy luôn có `Posters/*.jpg` đi kèm để `ResolvePosterFullPath` ghép `AppContext.BaseDirectory + storedPath` đọc được ngay cả ở `bin/Debug` hay bản đóng gói.
- Trong `PosterHelper.LoadPosterBitmap(string fullPath)`, thuộc tính `BitmapCacheOption.OnLoad` buộc WPF đọc hết dữ liệu ảnh vào RAM rồi đóng file ngay lập tức (tránh lỗi khóa file ảnh khi xóa/ghi đè trên Windows), còn `bitmap.Freeze()` biến đối tượng ảnh thành bất biến giúp bind và hiển thị nhẹ hơn.
- Với ảnh tỉ lệ không đồng nhất (12 ảnh dọc và 3 ảnh ngang trong bộ 15 poster), khung ảnh trên [CustomerDashboardView.xaml](CinemaManageProject/Views/Customer/CustomerDashboardView.xaml#L164-L181) cố định kích thước (`168px` rộng thẻ, vùng ảnh cao `208px`) kết hợp `Stretch="UniformToFill"` và `ClipToBounds="True"` trên `<Border>` bọc ngoài để mọi thẻ bằng chằn chặn mà ảnh không bị méo tỷ lệ.

### 11.2. Giữ vị trí cuộn ngang & Chuyển tiếp cuộn chuột ([CustomerDashboardViewModel.cs](CinemaManageProject/ViewModels/Customer/CustomerDashboardViewModel.cs#L14-L86), [CustomerDashboardView.xaml.cs](CinemaManageProject/Views/Customer/CustomerDashboardView.xaml.cs#L36-L86))
- **`CustomerMovieRow : ViewModelBase`:** Thay vì xóa và nạp lại `ObservableCollection<CustomerMovieRow>` mỗi lần người dùng chọn phim (gây giật thanh cuộn về đầu), `CustomerMovieRow` giữ thuộc tính `IsSelected` và `ActiveTag => IsSelected ? "Active" : null`. Khi gọi `SelectMovie(movie)`, chỉ 2 card (cũ và mới) đổi viền sang `#5E6AD2` tại chỗ qua `OnPropertyChanged`.
- **Xung đột `ScrollViewer` lồng nhau:** Vì `MainWindow` đã dùng một `<ScrollViewer>` cuộn dọc bao ngoài toàn trang ([MainWindow.xaml](CinemaManageProject/Views/MainWindow.xaml#L162)), thanh trượt poster ngang `PosterStripScrollViewer` bên trong mặc định sẽ chặn sự kiện `MouseWheel` khi con trỏ rê lên poster. Hàm `OnPosterStripPreviewMouseWheel` xử lý `Shift + Wheel` để cuộn ngang, còn cuộn chuột thường thì đánh dấu `e.Handled = true` rồi phát lại một `MouseWheelEventArgs` (`RoutedEvent = UIElement.MouseWheelEvent`) lên phần tử cha qua `VisualTreeHelper.GetParent` để trang chính vẫn cuộn dọc trơn tru.

---

## 12. Khai báo Icon Vector (`<Geometry>`), Nền điện ảnh (`DrawingBrush`) và Sidebar đóng/mở

### 12.1. Mấy cái icon và họa tiết hình ảnh được khai báo ở đâu?
Toàn bộ các icon (bảng quay phim Clapperboard, dải phim 35mm, vé rạp, màn chiếu, đồng hồ, hóa đơn, người dùng, ngôi sao...) **không phải file ảnh `.png`/`.svg` rời** mà là **chuỗi đường vector SVG Path (`<Geometry>`) khai báo tập trung tại [Controls.xaml](CinemaManageProject/Themes/Controls.xaml#L69-L88)**:
- **Style chung `CinemaVectorIconStyle` ([Controls.xaml:71-77](CinemaManageProject/Themes/Controls.xaml#L71-L77)):** Quy định `Stretch="Uniform"`, độ dày nét `StrokeThickness="1.75"`, bo tròn đầu/góc nối nét (`Round`) để các icon đồng bộ độ mảnh và co giãn sắc nét trên mọi độ phân giải DPI mà không cần cài thư viện ngoài.
- **10 tài nguyên `<Geometry>` ([Controls.xaml:79-88](CinemaManageProject/Themes/Controls.xaml#L79-L88)):** Ví dụ `<Geometry x:Key="FilmClapperIconGeometry">M 20.2,6 L 3,11 ...</Geometry>`. Mỗi chuỗi `M ... L ... H ... V ... Z` chính là dữ liệu thuộc tính `d="..."` chuẩn của vector SVG.

### 12.2. Cách hiển thị lên Sidebar, Logo và Dashboard
- **Gắn tĩnh trực tiếp bằng XAML (`StaticResource`):** Dùng cho logo trên [LoginWindow.xaml](CinemaManageProject/Views/LoginWindow.xaml#L41-L51), [MainWindow.xaml](CinemaManageProject/Views/MainWindow.xaml#L32-L42), họa tiết dải phim 35mm xoay nghiêng `-14°` làm watermark nền (`Opacity="0.045"`–`"0.06"`, `IsHitTestVisible="False"` để không chặn thao tác chuột), và các icon tiêu đề/hạng thành viên/cuống vé trên [CustomerDashboardView.xaml](CinemaManageProject/Views/Customer/CustomerDashboardView.xaml#L22-L27).
- **Gắn động cho Sidebar theo vai trò (`TryFindResource`):** Trong [MainViewModel.cs](CinemaManageProject/ViewModels/MainViewModel.cs#L15-L27), `SidebarMenuItem` lưu tên khóa `IconResourceKey` (ví dụ `"ShowtimeClockIconGeometry"`) và tra ra đối tượng `Geometry` qua `Application.Current?.TryFindResource(IconResourceKey) as Geometry`.
- **Lưu ý "bẫy" `Style TargetType="TextBlock"` trong Sidebar:** Vì [Controls.xaml:97-101](CinemaManageProject/Themes/Controls.xaml#L97-L101) có style ngầm định đặt mọi `TextBlock` thành màu trắng `#FAFAFA`, khi đặt `<TextBlock Text="{Binding Label}" />` vào bên trong nút menu (`NavMenuButtonStyle`) cần viết rõ `Foreground="{Binding Foreground, RelativeSource={RelativeSource AncestorType=Button}}"` ([MainWindow.xaml:92-94](CinemaManageProject/Views/MainWindow.xaml#L92-L94)). Nhờ vậy cả icon `<Path Stroke="...">` lẫn chữ đều đồng loạt chuyển từ xám `#A1A1AA` → trắng khi hover → tím Indigo `#5E6AD2` khi đang chọn.

### 12.3. Nền điện ảnh (`DrawingBrush`) & Đường rãnh vé rạp
- **Nền cửa sổ `CinemaWindowBackgroundBrush` ([Controls.xaml:11-67](CinemaManageProject/Themes/Controls.xaml#L11-L67)):** Dùng một `<DrawingBrush Stretch="Fill">` chồng 4 lớp ánh sáng: lớp nền đen than (`#060609` → `#0A0B10` → `#060608`), quầng sáng Indigo từ phía trên (`Center="0.46,0.12"`), quầng đỏ nhung rạp nhẹ ở góc dưới phải (`Center="0.88,0.84"`), và một vệt sáng tán mềm nghiêng `-22°`. Tài nguyên này gắn một lần vào `CinemaWindowStyle`, tự động áp dụng cho cả `LoginWindow` và `MainWindow`.
- **Thẻ nổi bán trong suốt (`CardBorderStyle`):** Đổi nền từ `#141516` sang `#D8141516` (~85% độ đặc) tại [Controls.xaml:129-135](CinemaManageProject/Themes/Controls.xaml#L129-L135) để ánh sáng nền hắt dịu qua rìa card.
- **Đường rãnh vé (`StrokeDashArray`):** Phần chân khung giới thiệu phim ([CustomerDashboardView.xaml:347-353](CinemaManageProject/Views/Customer/CustomerDashboardView.xaml#L347-L353)) dùng `<Line X1="0" X2="1" Stretch="Fill" StrokeDashArray="4 3" />` tạo đường đục lỗ cuống vé rạp kéo rộng tự động theo bề ngang khung.

### 12.4. Cơ chế Sidebar đóng/mở ([MainWindow.xaml](CinemaManageProject/Views/MainWindow.xaml#L14-L128), [MainViewModel.cs](CinemaManageProject/ViewModels/MainViewModel.cs#L86-L106))
- Cột trái của `<Grid>` dùng `Width="Auto"` thay vì cố định `Width="244"`; thẻ `<Border x:Name="SidebarPanel" Width="244">` gắn `Visibility="{Binding IsSidebarOpen, Converter={StaticResource BoolToVis}}"`.
- Khi bấm nút `SidebarToggleButton` trên Topbar, `_viewModel.ToggleSidebar()` đảo giá trị `IsSidebarOpen` (đồng thời đổi nhãn `‹` ↔ `☰` và tooltip `"Thu gọn thanh bên"` ↔ `"Mở thanh bên"`). Khi `Visibility` về `Collapsed`, cột `Width="Auto"` co về `0px` và vùng `Column 1` (`Width="*"`) tự mở rộng hết chiều ngang cửa sổ (từ 5 lên 6+ poster trên màn hình Khách hàng).

---

## 13. Hóa đơn & Doanh thu, Nhân sự & Tài khoản, và Chuẩn hóa Kiến trúc `Data/` – `Controls.xaml`

### 13.1. Module Hóa đơn & Doanh thu ([OrderData.cs](CinemaManageProject/Data/OrderData.cs), [AdminOrdersViewModel.cs](CinemaManageProject/ViewModels/Admin/AdminOrdersViewModel.cs), [AdminOrdersView.xaml](CinemaManageProject/Views/Admin/AdminOrdersView.xaml))
- **Tối ưu truy vấn 2 bước (tránh N+1 query):** Thay vì bấm vào mỗi hóa đơn lại mở SQLite truy vấn bảng `Tickets`, `OrderData` chạy đúng 2 câu SQL khi mở màn hình: `LoadAllOrders()` (join `Customers` và `Users`) và `LoadAllOrderTickets()` (join `Showtimes`, `Movies`, `Rooms`, `Seats` rồi gom vào `Dictionary<int, IReadOnlyList<OrderTicketDetailRow>>`). Nhờ đó việc chọn từng hóa đơn và tính tổng hợp doanh thu theo bộ lọc diễn ra tức thì trong RAM.
- **Quy tắc đối soát doanh thu & phân bổ theo phim:**
  - Chỉ tính các hóa đơn hiệu lực (`Status != 'Cancelled'`) vào doanh thu lọc (`FilteredRevenue`), hai KPI kênh bán (`Tại quầy` / `Tự đặt`) và bảng cơ cấu 4 phương thức thanh toán (`Tiền mặt`, `Chuyển khoản`, `Thẻ`, `Ví điện tử`). Hóa đơn hủy (`Cancelled`) được tách riêng sang bộ đếm đơn hủy và giá trị hủy để tổng các phân rã luôn khớp 100%.
  - Khi tính **Doanh thu theo Phim (`MovieRevenueRow`)**, doanh thu thực thu `Orders.TotalAmount` được phân bổ theo tỷ lệ `UnitPrice` của **các vé chưa hủy (`Ticket.Status != 'Cancelled'`)** trong đơn đó (`(ticket.UnitPrice * order.TotalAmount) / activeSub`), đảm bảo khớp tổng tiền ngay cả với hóa đơn có giảm giá (`DiscountAmount > 0`) hoặc hoàn một phần (`PartiallyRefunded`).
  - Trong phiếu chi tiết bên dưới, dòng `Đã hoàn / điều chỉnh` tự động hiện khi `SubTotalAmount - DiscountAmount > TotalAmount`, còn `Khách đưa` / `Tiền thối` chỉ hiển thị khi thanh toán `Cash`.

### 13.2. Module Nhân sự & Tài khoản ([UserData.cs](CinemaManageProject/Data/UserData.cs), [AdminUsersViewModel.cs](CinemaManageProject/ViewModels/Admin/AdminUsersViewModel.cs), [AdminUsersView.xaml](CinemaManageProject/Views/Admin/AdminUsersView.xaml))
- **Bảo vệ tài khoản Admin gốc ở cả 2 tầng (ViewModel + Data):** Hệ thống không cho tạo thêm tài khoản `Admin`, không cho nâng/hạ quyền thành `Admin`, không cho sửa/khóa/xóa tài khoản có `Role == 'Admin'` và không cho tự khóa/tự xóa `App.CurrentUser`. Các kiểm tra này chạy song song trên UI (`IsProtectedAdmin`) lẫn trong `SqliteTransaction` của `UserData.SaveUser`, `SetUserActive`, `DeleteUser`.
- **Đồng bộ 1-1 giữa `Users` và `Customers`:** Khi Admin tạo mới hoặc cập nhật tài khoản `Customer`, `UserData.SaveUser` kiểm tra trùng `Username` (không phân biệt hoa/thường `LOWER(Username)`) và trùng `Customers.Phone` (`UNIQUE`), đồng bộ `FullName`, `Phone`, `Email`, `DateOfBirth` giữa 2 bảng trong cùng một `SqliteTransaction` nhưng giữ `Tier`, `LoyaltyPoints`, `TotalSpent` ở chế độ chỉ đọc để không sai lệch đối soát hóa đơn.
- **Chặn xóa cứng tài khoản có lịch sử & Xóa sạch `PasswordBox`:** Trước khi `DELETE`, `UserData.DeleteUser` đếm các liên kết lịch sử (`Showtimes.CreatedByUserId`, `Orders.StaffUserId`, `Tickets.CheckedInByUserId`, `Orders.CustomerId`). Tài khoản đã phát sinh giao dịch sẽ bị chặn xóa và hướng dẫn dùng nút **Khóa tài khoản (`IsActive = 0`)**. Mật khẩu nhập qua `PasswordBox` được xóa trắng tự động qua sự kiện `PasswordClearRequested` mỗi khi đổi dòng chọn, bấm Thêm mới/Làm mới hoặc Lưu xong (bỏ trống khi sửa = giữ mật khẩu cũ).

### 13.3. Chuẩn hóa tầng `Data/` và tài nguyên `Themes/Controls.xaml`
- **Tách sạch 100% SQLite khỏi thư mục `ViewModels/`:** Toàn bộ truy vấn đăng nhập/đăng ký của [LoginViewModel.cs](CinemaManageProject/ViewModels/LoginViewModel.cs) được chuyển sang [AuthData.cs](CinemaManageProject/Data/AuthData.cs), còn truy vấn tổng hợp 3 màn hình Dashboard (`Admin`, `Staff`, `Customer`) được gom về [DashboardData.cs](CinemaManageProject/Data/DashboardData.cs). Từ đây không còn bất kỳ `ViewModel` nào import trực tiếp `Microsoft.Data.Sqlite` — mọi truy cập DB đi qua 8 lớp tĩnh trong `CinemaManageProject/Data/` (`AuthData`, `CustomerData`, `DashboardData`, `MovieData`, `OrderData`, `RoomData`, `ShowtimeData`, `UserData`).
- **Tập trung Style `DataGrid` dùng chung về [Controls.xaml](CinemaManageProject/Themes/Controls.xaml):** Đưa `CompactDataGridStyle` (`FontSize="12"`, giữ highlight chọn dòng `#265E6AD2`, `ScrollViewer.HorizontalScrollBarVisibility="Auto"`) và `EllipsisCellTextStyle` (trước đây lặp lại ở cả `AdminOrdersView.xaml` và `AdminUsersView.xaml`) cùng `WrappingCellTextStyle` (từ `AdminOrdersView.xaml`) về một nơi duy nhất trong `Themes/Controls.xaml` để dùng chung toàn dự án.
- **Ý nghĩa `PRAGMA foreign_keys = ON;`:** Mặc định SQLite tắt kiểm tra khóa ngoại trên mỗi kết nối mới; lệnh `PRAGMA` trong [DatabaseHelper.cs](CinemaManageProject/Helpers/DatabaseHelper.cs#L24) buộc SQLite thực thi nghiêm ngặt mọi ràng buộc `REFERENCES`, `ON DELETE RESTRICT` và `ON DELETE CASCADE` trong schema [CinemaManageDB.sql](CinemaManageProject/Database/CinemaManageDB.sql).
