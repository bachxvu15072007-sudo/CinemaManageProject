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
