using System.Windows;
using CinemaManageProject.ViewModels;

namespace CinemaManageProject.Views;

/// <summary>
/// Cửa sổ khung chính (Shell) hiển thị Sidebar phân quyền và Dashboard theo từng vai trò.
/// </summary>
public partial class MainWindow : Window
{
    private readonly MainViewModel _viewModel;

    public MainWindow()
    {
        InitializeComponent();
        _viewModel = new MainViewModel();
        DataContext = _viewModel;
    }

    protected override void OnClosed(EventArgs e)
    {
        _viewModel.Detach();
        base.OnClosed(e);
    }
}
