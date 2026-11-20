using System.IO;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace CinemaManageProject.Helpers;

/// <summary>
/// Các hàm phụ trợ lưu, phân giải đường dẫn và nạp ảnh poster không khóa file.
/// </summary>
public static class PosterHelper
{
    public static string ResolvePosterFullPath(string storedPath)
        => Path.IsPathRooted(storedPath)
            ? storedPath
            : Path.Combine(AppContext.BaseDirectory, storedPath);

    public static ImageSource LoadPosterBitmap(string fullPath)
    {
        var bitmap = new BitmapImage();
        bitmap.BeginInit();
        bitmap.CacheOption = BitmapCacheOption.OnLoad;
        bitmap.UriSource = new Uri(fullPath, UriKind.Absolute);
        bitmap.EndInit();
        bitmap.Freeze();
        return bitmap;
    }

    public static string SavePosterFile(string sourcePath)
    {
        string postersDir = Path.Combine(AppContext.BaseDirectory, "Posters");
        Directory.CreateDirectory(postersDir);

        string fileName = $"poster_{DateTime.Now:yyyyMMdd_HHmmss_fff}{Path.GetExtension(sourcePath)}";
        string destPath = Path.Combine(postersDir, fileName);
        File.Copy(sourcePath, destPath, overwrite: true);

        return Path.Combine("Posters", fileName);
    }
}
