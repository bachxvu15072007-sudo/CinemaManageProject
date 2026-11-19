using System.Windows;
using System.Windows.Controls;
using CinemaManageProject.ViewModels.Admin;
using Microsoft.Win32;

namespace CinemaManageProject.Views.Admin;

/// <summary>
/// Màn hình Quản lý Phim &amp; Thể loại cho vai trò Admin.
/// </summary>
public partial class AdminMoviesView : UserControl
{
    public AdminMoviesView()
    {
        InitializeComponent();
    }

    private AdminMoviesViewModel? ViewModel => DataContext as AdminMoviesViewModel;

    private void OnNewMovieClick(object sender, RoutedEventArgs e)
        => ViewModel?.StartNewMovie();

    private void OnResetFiltersClick(object sender, RoutedEventArgs e)
        => ViewModel?.ResetFilters();

    private void OnChoosePosterClick(object sender, RoutedEventArgs e)
    {
        var dialog = new OpenFileDialog
        {
            Title = "Chọn ảnh Poster phim",
            Filter = "Ảnh (*.jpg;*.jpeg;*.png;*.webp;*.bmp)|*.jpg;*.jpeg;*.png;*.webp;*.bmp"
        };

        if (dialog.ShowDialog() == true)
        {
            ViewModel?.SetPosterFromFile(dialog.FileName);
        }
    }

    private void OnClearPosterClick(object sender, RoutedEventArgs e)
        => ViewModel?.ClearPoster();

    private void OnAddGenreClick(object sender, RoutedEventArgs e)
        => ViewModel?.AddGenre();

    private void OnSaveMovieClick(object sender, RoutedEventArgs e)
        => ViewModel?.SaveMovie();

    private void OnResetFormClick(object sender, RoutedEventArgs e)
        => ViewModel?.ResetForm();

    private void OnDeleteMovieClick(object sender, RoutedEventArgs e)
    {
        if (ViewModel is null || !ViewModel.CanDeleteSelectedMovie(out string title))
        {
            return;
        }

        MessageBoxResult confirm = MessageBox.Show(
            $"Bạn có chắc chắn muốn xóa phim \"{title}\"?\nHành động này không thể hoàn tác.",
            "Xác nhận xóa phim",
            MessageBoxButton.YesNo,
            MessageBoxImage.Warning);

        if (confirm == MessageBoxResult.Yes)
        {
            ViewModel.DeleteSelectedMovie();
        }
    }
}
