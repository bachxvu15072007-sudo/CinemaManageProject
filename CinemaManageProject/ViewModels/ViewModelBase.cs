using System.ComponentModel;

namespace CinemaManageProject.ViewModels;

/// <summary>
/// Lớp nền MVVM cài đặt INotifyPropertyChanged và cổng LoadData dùng chung cho các màn hình.
/// </summary>
public abstract class ViewModelBase : INotifyPropertyChanged
{
    public event PropertyChangedEventHandler? PropertyChanged;

    public virtual void LoadData() { }

    protected void OnPropertyChanged(string propertyName)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
}
