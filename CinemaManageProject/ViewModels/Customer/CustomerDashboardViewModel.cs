using System.Collections.ObjectModel;
using System.IO;
using System.Windows.Media;
using CinemaManageProject.Data;
using CinemaManageProject.Helpers;
using CinemaManageProject.Models;
using Microsoft.Data.Sqlite;

namespace CinemaManageProject.ViewModels.Customer;

/// <summary>
/// Mục hiển thị từng phim trên dải poster và khung giới thiệu chi tiết của Customer.
/// </summary>
public sealed class CustomerMovieRow : ViewModelBase
{
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

    public bool IsSelected
    {
        get => _isSelected;
        set
        {
            if (_isSelected == value)
            {
                return;
            }

            _isSelected = value;
            OnPropertyChanged(nameof(IsSelected));
            OnPropertyChanged(nameof(ActiveTag));
        }
    }
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
public sealed class CustomerDashboardViewModel : ViewModelBase
{
    private readonly List<CustomerMovieRow> _allMovies = [];
    private CustomerMovieRow? _selectedMovie;
    private string _memberName = string.Empty;
    private string _memberPhone = string.Empty;
    private string _tierDisplay = "Thành viên";
    private string _pointsText = "0 điểm";
    private string _totalSpentText = "0đ";
    private string _activeTicketsText = "0 vé";
    private string _selectedFilter = "All";

    public CustomerDashboardViewModel()
    {
        LoadData();
    }

    public string MemberName
    {
        get => _memberName;
        private set { _memberName = value; OnPropertyChanged(nameof(MemberName)); }
    }

    public string MemberPhone
    {
        get => _memberPhone;
        private set { _memberPhone = value; OnPropertyChanged(nameof(MemberPhone)); }
    }

    public string TierDisplay
    {
        get => _tierDisplay;
        private set { _tierDisplay = value; OnPropertyChanged(nameof(TierDisplay)); }
    }

    public string PointsText
    {
        get => _pointsText;
        private set { _pointsText = value; OnPropertyChanged(nameof(PointsText)); }
    }

    public string TotalSpentText
    {
        get => _totalSpentText;
        private set { _totalSpentText = value; OnPropertyChanged(nameof(TotalSpentText)); }
    }

    public string ActiveTicketsText
    {
        get => _activeTicketsText;
        private set { _activeTicketsText = value; OnPropertyChanged(nameof(ActiveTicketsText)); }
    }

    public string SelectedFilter
    {
        get => _selectedFilter;
        private set
        {
            if (_selectedFilter == value)
            {
                return;
            }

            _selectedFilter = value;
            OnPropertyChanged(nameof(SelectedFilter));
            OnPropertyChanged(nameof(AllFilterTag));
            OnPropertyChanged(nameof(NowShowingFilterTag));
            OnPropertyChanged(nameof(ComingSoonFilterTag));
            ApplyFilter();
        }
    }

    public string? AllFilterTag => SelectedFilter == "All" ? "Active" : null;
    public string? NowShowingFilterTag => SelectedFilter == "NowShowing" ? "Active" : null;
    public string? ComingSoonFilterTag => SelectedFilter == "ComingSoon" ? "Active" : null;

    public CustomerMovieRow? SelectedMovie
    {
        get => _selectedMovie;
        private set
        {
            if (ReferenceEquals(_selectedMovie, value))
            {
                return;
            }

            if (_selectedMovie is not null)
            {
                _selectedMovie.IsSelected = false;
            }

            _selectedMovie = value;

            if (_selectedMovie is not null)
            {
                _selectedMovie.IsSelected = true;
            }

            OnPropertyChanged(nameof(SelectedMovie));
            OnPropertyChanged(nameof(HasSelectedMovie));
        }
    }

    public bool HasSelectedMovie => SelectedMovie is not null;
    public bool HasNoMovies => Movies.Count == 0;
    public bool HasNoTickets => MyTickets.Count == 0;

    public ObservableCollection<CustomerMovieRow> Movies { get; } = [];
    public ObservableCollection<CustomerTicketRow> MyTickets { get; } = [];

    public void SetFilter(string? filter) => SelectedFilter = filter ?? "All";

    public void SelectMovie(CustomerMovieRow? movie) => SelectedMovie = movie;

    public override void LoadData()
    {
        using SqliteConnection conn = DatabaseHelper.OpenConnection();

        Models.Customer? profile = App.CurrentUser is not null
            ? CustomerData.LoadCustomerProfile(conn, App.CurrentUser.UserId) ?? App.CurrentUser.CustomerProfile
            : null;
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
        using (SqliteCommand mov = conn.CreateCommand())
        {
            mov.CommandText = """
                SELECT m.MovieId, m.Title, m.OriginalTitle, m.Director, m.Cast,
                       m.DurationMinutes, m.Language, m.ReleaseDate, m.AgeRating,
                       m.Description, m.PosterPath, m.Status,
                       COALESCE((SELECT GROUP_CONCAT(g.Name, ', ')
                                 FROM MovieGenres mg INNER JOIN Genres g ON g.GenreId = mg.GenreId
                                 WHERE mg.MovieId = m.MovieId), 'Tổng hợp'),
                       (SELECT MIN(s.BaseTicketPrice + r.BasePriceSurcharge)
                        FROM Showtimes s
                        INNER JOIN Rooms r ON r.RoomId = s.RoomId
                        WHERE s.MovieId = m.MovieId AND s.Status = 'OpenForSale')
                FROM Movies m
                WHERE m.Status IN ('NowShowing', 'ComingSoon')
                ORDER BY CASE m.Status WHEN 'NowShowing' THEN 0 ELSE 1 END, m.ReleaseDate DESC;
                """;
            using SqliteDataReader r = mov.ExecuteReader();
            while (r.Read())
            {
                string rawStatus = r.GetString(11);
                bool nowShowing = rawStatus == "NowShowing";
                string priceText = r.IsDBNull(13)
                    ? "Chưa mở bán"
                    : $"Từ {FormatHelper.FormatVnd(DatabaseHelper.ReadDecimal(r, 13))}";
                string? posterPath = r.IsDBNull(10) ? null : r.GetString(10);

                _allMovies.Add(new CustomerMovieRow(
                    r.GetInt32(0),
                    r.GetString(1),
                    r.IsDBNull(2) ? null : r.GetString(2),
                    $"Đạo diễn: {ReadOptionalText(r, 3, "Đang cập nhật")}",
                    $"Diễn viên: {ReadOptionalText(r, 4, "Đang cập nhật")}",
                    ReadOptionalText(r, 9, "Chưa có mô tả giới thiệu cho phim này."),
                    $"Khởi chiếu {FormatHelper.FormatSqlDateTime(r.GetString(7), "dd/MM/yyyy")}",
                    r.GetString(12),
                    $"{r.GetInt32(5)} phút • {r.GetString(6)}",
                    r.GetString(8),
                    FormatHelper.FormatStatusLabel(rawStatus),
                    priceText,
                    nowShowing,
                    TryLoadPoster(posterPath)));
            }
        }
        ApplyFilter();

        MyTickets.Clear();
        int bookedCount = 0;
        if (profile is not null)
        {
            using SqliteCommand tk = conn.CreateCommand();
            tk.CommandText = """
                SELECT t.TicketCode, m.Title, r.Name, se.SeatCode, s.StartTime, t.UnitPrice, t.Status
                FROM Tickets t
                INNER JOIN Orders o ON o.OrderId = t.OrderId
                INNER JOIN Showtimes s ON s.ShowtimeId = t.ShowtimeId
                INNER JOIN Movies m ON m.MovieId = s.MovieId
                INNER JOIN Rooms r ON r.RoomId = s.RoomId
                INNER JOIN Seats se ON se.SeatId = t.SeatId
                WHERE o.CustomerId = @cid
                ORDER BY s.StartTime DESC
                LIMIT 15;
                """;
            tk.Parameters.AddWithValue("@cid", profile.CustomerId);

            using SqliteDataReader r = tk.ExecuteReader();
            while (r.Read())
            {
                string rawStatus = r.GetString(6);
                if (rawStatus == "Booked")
                {
                    bookedCount++;
                }

                MyTickets.Add(new CustomerTicketRow(
                    r.GetString(0),
                    r.GetString(1),
                    $"{r.GetString(2)} • Ghế {r.GetString(3)}",
                    FormatHelper.FormatSqlDateTime(r.GetString(4), "dd/MM/yyyy HH:mm"),
                    FormatHelper.FormatVnd(DatabaseHelper.ReadDecimal(r, 5)),
                    FormatHelper.FormatStatusLabel(rawStatus)));
            }
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

    private static string ReadOptionalText(SqliteDataReader reader, int ordinal, string fallback)
    {
        if (reader.IsDBNull(ordinal))
        {
            return fallback;
        }

        string text = reader.GetString(ordinal).Trim();
        return string.IsNullOrWhiteSpace(text) ? fallback : text;
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
