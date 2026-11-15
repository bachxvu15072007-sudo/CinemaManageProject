using System.Windows;

namespace CinemaManageProject.Themes;

public enum AppTheme
{
    Dark,
    Light
}

/// <summary>
/// Quản lý chuyển đổi nhanh Dark / Light Theme (tông Zinc tối giản) cho toàn ứng dụng.
/// </summary>
public static class ThemeManager
{
    private static readonly Uri DarkThemeUri = new("/CinemaManageProject;component/Themes/DarkTheme.xaml", UriKind.Relative);
    private static readonly Uri LightThemeUri = new("/CinemaManageProject;component/Themes/LightTheme.xaml", UriKind.Relative);

    public static AppTheme CurrentTheme { get; private set; } = AppTheme.Dark;

    public static event EventHandler<AppTheme>? ThemeChanged;

    public static void ApplyTheme(AppTheme theme)
    {
        if (Application.Current is null)
        {
            return;
        }

        var merged = Application.Current.Resources.MergedDictionaries;
        var nextDictionary = new ResourceDictionary
        {
            Source = theme == AppTheme.Dark ? DarkThemeUri : LightThemeUri
        };

        if (merged.Count > 0) merged[0] = nextDictionary;
        else merged.Add(nextDictionary);

        CurrentTheme = theme;
        ThemeChanged?.Invoke(null, theme);
    }

    public static void ToggleTheme() =>
        ApplyTheme(CurrentTheme == AppTheme.Dark ? AppTheme.Light : AppTheme.Dark);
}
