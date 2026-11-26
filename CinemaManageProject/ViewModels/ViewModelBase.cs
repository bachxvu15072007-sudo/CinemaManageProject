using CommunityToolkit.Mvvm.ComponentModel;

namespace CinemaManageProject.ViewModels;

/// <summary>
/// Lớp nền MVVM kế thừa ObservableObject của CommunityToolkit.Mvvm và cổng LoadData dùng chung cho các màn hình.
/// </summary>
public abstract class ViewModelBase : ObservableObject
{
    public virtual void LoadData() { }
}
