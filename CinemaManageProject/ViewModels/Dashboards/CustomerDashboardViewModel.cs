using System.Collections.ObjectModel;
using System.Globalization;
using CinemaManageProject.Models;
using Microsoft.Data.Sqlite;

namespace CinemaManageProject.ViewModels.Dashboards;

public sealed record CustomerMovieRow(
    string Title,
    string Genres,
    string MetaInfo,
    string AgeRating,
    string StatusDisplay,
    string PriceText,
    bool IsNowShowing);

public sealed record CustomerTicketRow(
    string TicketCode,
    string MovieTitle,
    string RoomAndSeat,
    string StartTime,
    string UnitPrice,
    string StatusDisplay);

/// <summary>
/// ViewModel cho Dashboard vai trò Customer: thẻ hội viên, phim đang chiếu/sắp chiếu và vé đã đặt.
/// </summary>
public sealed class CustomerDashboardViewModel : ViewModelBase
{
    private readonly List<CustomerMovieRow> _allMovies = [];
    private string _memberName = string.Empty;
    private string _memberPhone = string.Empty;
    private string _tierDisplay = "Thành viên";
    private string _pointsText = "0 điểm";
    private string _totalSpentText = "0đ";
    private string _activeTicketsText = "0 vé";
    private string _selectedFilter = "All";

    public CustomerDashboardViewModel()
    {
        FilterCommand = new RelayCommand(param => SelectedFilter = param?.ToString() ?? "All");
        LoadData();
    }

    public string MemberName { get => _memberName; private set => SetProperty(ref _memberName, value); }
    public string MemberPhone { get => _memberPhone; private set => SetProperty(ref _memberPhone, value); }
    public string TierDisplay { get => _tierDisplay; private set => SetProperty(ref _tierDisplay, value); }
    public string PointsText { get => _pointsText; private set => SetProperty(ref _pointsText, value); }
    public string TotalSpentText { get => _totalSpentText; private set => SetProperty(ref _totalSpentText, value); }
    public string ActiveTicketsText { get => _activeTicketsText; private set => SetProperty(ref _activeTicketsText, value); }

    public string SelectedFilter
    {
        get => _selectedFilter;
        private set
        {
            if (SetProperty(ref _selectedFilter, value))
            {
                ApplyFilter();
            }
        }
    }

    public bool HasNoTickets => MyTickets.Count == 0;

    public ObservableCollection<CustomerMovieRow> Movies { get; } = [];
    public ObservableCollection<CustomerTicketRow> MyTickets { get; } = [];

    public RelayCommand FilterCommand { get; }

    public void LoadData()
    {
        Customer? profile = App.CurrentUser?.CustomerProfile;
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
        TotalSpentText = FormatVnd(profile?.TotalSpent ?? 0m);

        using SqliteConnection conn = App.OpenDbConnection();

        _allMovies.Clear();
        using (SqliteCommand mov = conn.CreateCommand())
        {
            mov.CommandText = """
                SELECT m.Title,
                       COALESCE((SELECT GROUP_CONCAT(g.Name, ', ')
                                 FROM MovieGenres mg INNER JOIN Genres g ON g.GenreId = mg.GenreId
                                 WHERE mg.MovieId = m.MovieId), 'Tổng hợp'),
                       m.DurationMinutes, m.Language, m.AgeRating, m.Status,
                       (SELECT MIN(s.BaseTicketPrice) FROM Showtimes s WHERE s.MovieId = m.MovieId AND s.Status = 'OpenForSale')
                FROM Movies m
                WHERE m.Status IN ('NowShowing', 'ComingSoon')
                ORDER BY CASE m.Status WHEN 'NowShowing' THEN 0 ELSE 1 END, m.ReleaseDate DESC;
                """;
            using SqliteDataReader r = mov.ExecuteReader();
            while (r.Read())
            {
                bool nowShowing = r.GetString(5) == "NowShowing";
                string priceText = r.IsDBNull(6)
                    ? "Chưa mở bán"
                    : $"Từ {FormatVnd(Convert.ToDecimal(r.GetValue(6), CultureInfo.InvariantCulture))}";

                _allMovies.Add(new CustomerMovieRow(
                    r.GetString(0),
                    r.GetString(1),
                    $"{r.GetInt32(2)} phút • {r.GetString(3)}",
                    r.GetString(4),
                    nowShowing ? "Đang chiếu" : "Sắp chiếu",
                    priceText,
                    nowShowing));
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

                string statusDisplay = rawStatus switch
                {
                    "Booked" => "Chờ soát vé",
                    "CheckedIn" => "Đã check-in",
                    "Cancelled" => "Đã hủy",
                    var s => s
                };

                string timeText = DateTime.TryParse(r.GetString(4), CultureInfo.InvariantCulture, DateTimeStyles.AssumeLocal, out DateTime dt)
                    ? dt.ToString("dd/MM/yyyy HH:mm")
                    : r.GetString(4);

                MyTickets.Add(new CustomerTicketRow(
                    r.GetString(0),
                    r.GetString(1),
                    $"{r.GetString(2)} • Ghế {r.GetString(3)}",
                    timeText,
                    FormatVnd(Convert.ToDecimal(r.GetValue(5), CultureInfo.InvariantCulture)),
                    statusDisplay));
            }
        }

        ActiveTicketsText = $"{bookedCount} vé chờ chiếu";
        OnPropertyChanged(nameof(HasNoTickets));
    }

    private void ApplyFilter()
    {
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
    }
}
