using System.Collections.ObjectModel;
using System.IO;
using System.Windows.Media;
using CinemaManageProject.Data;
using CinemaManageProject.Helpers;
using CinemaManageProject.Models;
using CommunityToolkit.Mvvm.ComponentModel;

namespace CinemaManageProject.ViewModels.Customer;

/// <summary>
/// Mục hiển thị từng phim trên dải poster và khung giới thiệu chi tiết của Customer.
/// </summary>
public sealed partial class CustomerMovieRow : ViewModelBase
{
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ActiveTag))]
    private bool _isSelected;

    public CustomerMovieRow(
        int movieId,
        string title,
        string? originalTitle,
        string directorText,
        string castText,
        string descriptionText,
        string releaseDateText,
        string genres,
        string metaInfo,
        string ageRating,
        string statusDisplay,
        string priceText,
        bool isNowShowing,
        ImageSource? posterImage)
    {
        MovieId = movieId;
        Title = title;
        OriginalTitle = originalTitle;
        DirectorText = directorText;
        CastText = castText;
        DescriptionText = descriptionText;
        ReleaseDateText = releaseDateText;
        Genres = genres;
        MetaInfo = metaInfo;
        AgeRating = ageRating;
        StatusDisplay = statusDisplay;
        PriceText = priceText;
        IsNowShowing = isNowShowing;
        PosterImage = posterImage;
    }

    public int MovieId { get; }
    public string Title { get; }
    public string? OriginalTitle { get; }
    public bool HasOriginalTitle => !string.IsNullOrWhiteSpace(OriginalTitle)
        && !string.Equals(Title, OriginalTitle, StringComparison.OrdinalIgnoreCase);

    public string DirectorText { get; }
    public string CastText { get; }
    public string DescriptionText { get; }
    public string ReleaseDateText { get; }
    public string Genres { get; }
    public string MetaInfo { get; }
    public string AgeRating { get; }
    public string StatusDisplay { get; }
    public string PriceText { get; }
    public bool IsNowShowing { get; }
    public ImageSource? PosterImage { get; }
    public bool HasPoster => PosterImage is not null;
    public string? ActiveTag => IsSelected ? "Active" : null;
}

public sealed record CustomerTicketRow(
    string TicketCode,
    string MovieTitle,
    string RoomAndSeat,
    string StartTime,
    string UnitPrice,
    string StatusDisplay);

/// <summary>
/// ViewModel cho Dashboard vai trò Customer: thẻ hội viên, dải poster giới thiệu phim và vé đã đặt.
/// </summary>
public sealed partial class CustomerDashboardViewModel : ViewModelBase
{
    private readonly List<CustomerMovieRow> _allMovies = [];

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasSelectedMovie))]
    private CustomerMovieRow? _selectedMovie;

    [ObservableProperty] private string _memberName = string.Empty;
    [ObservableProperty] private string _memberPhone = string.Empty;
    [ObservableProperty] private string _tierDisplay = "Thành viên";
    [ObservableProperty] private string _pointsText = "0 điểm";
    [ObservableProperty] private string _totalSpentText = "0đ";
    [ObservableProperty] private string _activeTicketsText = "0 vé";

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(AllFilterTag))]
    [NotifyPropertyChangedFor(nameof(NowShowingFilterTag))]
    [NotifyPropertyChangedFor(nameof(ComingSoonFilterTag))]
    private string _selectedFilter = "All";

    public CustomerDashboardViewModel()
    {
        LoadData();
    }

    public string? AllFilterTag => SelectedFilter == "All" ? "Active" : null;
    public string? NowShowingFilterTag => SelectedFilter == "NowShowing" ? "Active" : null;
    public string? ComingSoonFilterTag => SelectedFilter == "ComingSoon" ? "Active" : null;

    public bool HasSelectedMovie => SelectedMovie is not null;
    public bool HasNoMovies => Movies.Count == 0;
    public bool HasNoTickets => MyTickets.Count == 0;

    public ObservableCollection<CustomerMovieRow> Movies { get; } = [];
    public ObservableCollection<CustomerTicketRow> MyTickets { get; } = [];

    partial void OnSelectedFilterChanged(string value) => ApplyFilter();

    partial void OnSelectedMovieChanging(CustomerMovieRow? oldValue, CustomerMovieRow? newValue)
    {
        if (oldValue is not null) oldValue.IsSelected = false;
        if (newValue is not null) newValue.IsSelected = true;
    }

    public void SetFilter(string? filter) => SelectedFilter = filter ?? "All";
    public void SelectMovie(CustomerMovieRow? movie) => SelectedMovie = movie;

    public override void LoadData()
    {
        CustomerDashboardSnapshot data = DashboardData.LoadCustomerDashboard(
            App.CurrentUser?.UserId,
            App.CurrentUser?.CustomerProfile);

        Models.Customer? profile = data.Profile;
        if (App.CurrentUser is not null && profile is not null)
        {
            App.CurrentUser.CustomerProfile = profile;
        }

        MemberName = profile?.FullName ?? App.CurrentUser?.FullName ?? "Khách hàng";
        MemberPhone = profile?.Phone ?? App.CurrentUser?.Phone ?? "Chưa cập nhật";
        TierDisplay = (profile?.Tier ?? CustomerTier.Member) switch
        {
            CustomerTier.Silver => "Hạng Bạc (Silver)",
            CustomerTier.Gold => "Hạng Vàng (Gold)",
            CustomerTier.Platinum => "Bạch kim (Platinum)",
            _ => "Thành viên (Member)"
        };
        PointsText = $"{profile?.LoyaltyPoints ?? 0} điểm";
        TotalSpentText = FormatHelper.FormatVnd(profile?.TotalSpent ?? 0m);

        _allMovies.Clear();
        foreach (CustomerDashboardMovieDto mov in data.Movies)
        {
            bool nowShowing = mov.Status == "NowShowing";
            string priceText = mov.MinOpenPrice.HasValue
                ? $"Từ {FormatHelper.FormatVnd(mov.MinOpenPrice.Value)}"
                : "Chưa mở bán";

            _allMovies.Add(new CustomerMovieRow(
                mov.MovieId,
                mov.Title,
                mov.OriginalTitle,
                $"Đạo diễn: {mov.Director}",
                $"Diễn viên: {mov.Cast}",
                mov.Description,
                $"Khởi chiếu {FormatHelper.FormatSqlDateTime(mov.ReleaseDateSql, "dd/MM/yyyy")}",
                mov.Genres,
                $"{mov.DurationMinutes} phút • {mov.Language}",
                mov.AgeRating,
                FormatHelper.FormatStatusLabel(mov.Status),
                priceText,
                nowShowing,
                TryLoadPoster(mov.PosterPath)));
        }
        ApplyFilter();

        MyTickets.Clear();
        int bookedCount = 0;
        foreach (CustomerDashboardTicketDto tk in data.MyTickets)
        {
            if (tk.Status == "Booked")
            {
                bookedCount++;
            }

            MyTickets.Add(new CustomerTicketRow(
                tk.TicketCode,
                tk.MovieTitle,
                $"{tk.RoomName} • Ghế {tk.SeatCode}",
                FormatHelper.FormatSqlDateTime(tk.StartTimeSql, "dd/MM/yyyy HH:mm"),
                FormatHelper.FormatVnd(tk.UnitPrice),
                FormatHelper.FormatStatusLabel(tk.Status)));
        }

        ActiveTicketsText = $"{bookedCount} vé chờ chiếu";
        OnPropertyChanged(nameof(HasNoTickets));
    }

    private void ApplyFilter()
    {
        int? selectedMovieId = SelectedMovie?.MovieId;

        Movies.Clear();
        IEnumerable<CustomerMovieRow> rows = SelectedFilter switch
        {
            "NowShowing" => _allMovies.Where(m => m.IsNowShowing),
            "ComingSoon" => _allMovies.Where(m => !m.IsNowShowing),
            _ => _allMovies
        };

        foreach (CustomerMovieRow item in rows)
        {
            Movies.Add(item);
        }

        OnPropertyChanged(nameof(HasNoMovies));

        CustomerMovieRow? nextSelection = selectedMovieId.HasValue
            ? Movies.FirstOrDefault(m => m.MovieId == selectedMovieId.Value) ?? Movies.FirstOrDefault()
            : Movies.FirstOrDefault();

        SelectMovie(nextSelection);
    }

    private static ImageSource? TryLoadPoster(string? storedPath)
    {
        if (string.IsNullOrWhiteSpace(storedPath))
        {
            return null;
        }

        try
        {
            string fullPath = PosterHelper.ResolvePosterFullPath(storedPath);
            return File.Exists(fullPath) ? PosterHelper.LoadPosterBitmap(fullPath) : null;
        }
        catch
        {
            return null;
        }
    }
}
