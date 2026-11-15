using System.Windows;
using CinemaManageProject.Themes;

namespace CinemaManageProject;

/// <summary>
/// Cửa sổ chính khởi chạy cùng ứng dụng, hiển thị bộ theme Zinc tối giản (Dark / Light).
/// </summary>
public partial class MainWindow : Window
{
    private sealed record SampleShowtimeRow(string Title, string Room, string Time, string Price);

    public MainWindow()
    {
        InitializeComponent();

        SampleShowtimesGrid.ItemsSource = new SampleShowtimeRow[]
        {
            new("Dune: Hành Tinh Cát - Phần 2", "Phòng 3 - IMAX", "14:00", "100.000đ"),
            new("Mai",                          "Phòng 1 - 2D",   "15:00", "85.000đ"),
            new("Kung Fu Panda 4",              "Phòng 2 - 3D",   "13:30", "80.000đ"),
            new("Quật Mộ Trùng Ma",             "Phòng 2 - 3D",   "20:00", "95.000đ")
        };

        UpdateThemeUi();
    }

    private void OnToggleThemeClick(object sender, RoutedEventArgs e)
    {
        ThemeManager.ToggleTheme();
        UpdateThemeUi();
    }

    private void UpdateThemeUi()
    {
        bool isDark = ThemeManager.CurrentTheme == AppTheme.Dark;
        ToggleThemeButton.Content = isDark ? "☀ Light Mode" : "🌙 Dark Mode";
        ThemeStatusText.Text = isDark ? "Zinc Obsidian (Dark)" : "Zinc Daylight (Light)";
    }
}
