using System.Collections.ObjectModel;
using System.Globalization;
using System.IO;
using System.Windows.Media;
using CinemaManageProject.Data;
using CinemaManageProject.Helpers;
using CinemaManageProject.Models;

namespace CinemaManageProject.ViewModels.Customer;

public sealed record OnlinePaymentOption(string Code, string Label)
{
    public override string ToString() => Label;
}

public sealed class BrowseMovieCardItem : ViewModelBase
{
    private bool _isSelected;

    public BrowseMovieCardItem(BrowseMovieRow row, ImageSource? posterImage)
    {
        MovieId = row.MovieId;
        Title = row.Title;
        OriginalTitle = row.OriginalTitle;
        DirectorText = string.IsNullOrWhiteSpace(row.Director) ? "Đang cập nhật" : row.Director.Trim();
        CastText = string.IsNullOrWhiteSpace(row.Cast) ? "Đang cập nhật" : row.Cast.Trim();
        DescriptionText = string.IsNullOrWhiteSpace(row.Description)
            ? "Chưa có mô tả chi tiết cho phim này."
            : row.Description.Trim();
        DurationMinutes = row.DurationMinutes;
        Language = row.Language;
        ReleaseDateText = $"Khởi chiếu {FormatHelper.FormatSqlDateTime(row.ReleaseDateSql, "dd/MM/yyyy")}";
        AgeRating = row.AgeRating;
        Status = row.Status;
        GenresText = row.GenresText;
        OpenShowtimesCount = row.OpenShowtimesCount;
        PriceText = row.MinOpenPrice.HasValue
            ? $"Từ {FormatHelper.FormatVnd(row.MinOpenPrice.Value)}"
            : "Chưa mở bán";
        PosterImage = posterImage;
    }

    public int MovieId { get; }
    public string Title { get; }
    public string? OriginalTitle { get; }
    public string DirectorText { get; }
    public string CastText { get; }
    public string DescriptionText { get; }
    public int DurationMinutes { get; }
    public string Language { get; }
    public string ReleaseDateText { get; }
    public string AgeRating { get; }
    public string Status { get; }
    public string GenresText { get; }
    public int OpenShowtimesCount { get; }
    public string PriceText { get; }
    public ImageSource? PosterImage { get; }

    public bool HasPoster => PosterImage is not null;
    public bool IsNowShowing => Status == "NowShowing";
    public string StatusDisplay => FormatHelper.FormatStatusLabel(Status);
    public string MetaLine => $"{DurationMinutes} phút • {Language} • {AgeRating}";
    public string ShowtimeBadgeText => OpenShowtimesCount > 0 ? $"{OpenShowtimesCount} suất mở bán" : "Chưa mở bán";
    public string? ActiveTag => IsSelected ? "Active" : null;

    public bool IsSelected
    {
        get => _isSelected;
        set
        {
            if (_isSelected == value) return;
            _isSelected = value;
            OnPropertyChanged(nameof(IsSelected));
            OnPropertyChanged(nameof(ActiveTag));
        }
    }
}

/// <summary>
/// ViewModel màn hình Phim &amp; Lịch chiếu (Tự đặt vé trực tuyến) cho vai trò Customer.
/// </summary>
public sealed class CustomerBrowseViewModel : ViewModelBase
{
    private const string AllGenresLabel = "Tất cả thể loại";

    private readonly List<BrowseMovieCardItem> _allMovies = [];
    private BookingCustomerSummary? _memberProfile;
    private BrowseMovieCardItem? _selectedMovie;
    private OpenShowtimeOption? _selectedShowtime;
    private OnlinePaymentOption _selectedPaymentMethod;
    private string _statusFilter = "NowShowing";
    private string _selectedGenre = AllGenresLabel;
    private string _searchKeyword = string.Empty;
    private string _pointsToUseText = "0";
    private string _orderNote = string.Empty;
    private string _seatMapHeader = "Sơ đồ Ghế suất chiếu";
    private string _seatMapSubtitle = "Chọn phim và suất chiếu mở bán để chọn ghế.";
    private string? _errorMessage;
    private string? _successMessage;
    private string? _lastBookingSummary;

    public CustomerBrowseViewModel()
    {
        PaymentOptions =
        [
            new OnlinePaymentOption("BankTransfer", "Chuyển khoản ngân hàng / QR"),
            new OnlinePaymentOption("EWallet", "Ví điện tử (MoMo / ZaloPay)"),
            new OnlinePaymentOption("Card", "Thẻ ngân hàng / Quốc tế")
        ];
        _selectedPaymentMethod = PaymentOptions[0];
        LoadData();
    }

    public IReadOnlyList<OnlinePaymentOption> PaymentOptions { get; }
    public ObservableCollection<string> GenreOptions { get; } = [];
    public ObservableCollection<BrowseMovieCardItem> Movies { get; } = [];
    public ObservableCollection<OpenShowtimeOption> MovieShowtimes { get; } = [];
    public ObservableCollection<BookingSeatRowGroup> SeatRows { get; } = [];
    public ObservableCollection<BookingSeatCellItem> SelectedSeats { get; } = [];

    public string MemberHeaderText => _memberProfile is null
        ? "Chưa liên kết hồ sơ hội viên"
        : $"{_memberProfile.FullName} • {_memberProfile.TierDisplay} • {_memberProfile.LoyaltyPointsText}";

    public string PointsHintText => _memberProfile is null
        ? "Đăng nhập tài khoản hội viên để đổi điểm."
        : $"Đang có {_memberProfile.LoyaltyPointsText} • Tối đa dùng {MaxRedeemablePoints} điểm cho đơn này";

    public string StatusFilter
    {
        get => _statusFilter;
        private set
        {
            if (_statusFilter == value) return;
            _statusFilter = value;
            OnPropertyChanged(nameof(StatusFilter));
            OnPropertyChanged(nameof(NowShowingTag));
            OnPropertyChanged(nameof(ComingSoonTag));
            OnPropertyChanged(nameof(AllStatusTag));
            ApplyMovieFilter();
        }
    }

    public string? NowShowingTag => StatusFilter == "NowShowing" ? "Active" : null;
    public string? ComingSoonTag => StatusFilter == "ComingSoon" ? "Active" : null;
    public string? AllStatusTag => StatusFilter == "All" ? "Active" : null;

    public string SelectedGenre
    {
        get => _selectedGenre;
        set
        {
            string next = string.IsNullOrWhiteSpace(value) ? AllGenresLabel : value;
            if (_selectedGenre == next) return;
            _selectedGenre = next;
            OnPropertyChanged(nameof(SelectedGenre));
            ApplyMovieFilter();
        }
    }

    public string SearchKeyword
    {
        get => _searchKeyword;
        set
        {
            if (_searchKeyword == value) return;
            _searchKeyword = value ?? string.Empty;
            OnPropertyChanged(nameof(SearchKeyword));
            ApplyMovieFilter();
        }
    }

    public BrowseMovieCardItem? SelectedMovie
    {
        get => _selectedMovie;
        private set
        {
            if (ReferenceEquals(_selectedMovie, value)) return;
            if (_selectedMovie is not null) _selectedMovie.IsSelected = false;
            _selectedMovie = value;
            if (_selectedMovie is not null) _selectedMovie.IsSelected = true;

            OnPropertyChanged(nameof(SelectedMovie));
            OnPropertyChanged(nameof(HasSelectedMovie));
            LoadShowtimesForSelectedMovie();
        }
    }

    public OpenShowtimeOption? SelectedShowtime
    {
        get => _selectedShowtime;
        set
        {
            if (ReferenceEquals(_selectedShowtime, value)) return;
            _selectedShowtime = value;
            OnPropertyChanged(nameof(SelectedShowtime));
            OnPropertyChanged(nameof(HasSelectedShowtime));
            LoadSeatMapForSelectedShowtime();
        }
    }

    public OnlinePaymentOption SelectedPaymentMethod
    {
        get => _selectedPaymentMethod;
        set
        {
            if (value is null || ReferenceEquals(_selectedPaymentMethod, value)) return;
            _selectedPaymentMethod = value;
            OnPropertyChanged(nameof(SelectedPaymentMethod));
        }
    }

    public string PointsToUseText
    {
        get => _pointsToUseText;
        set
        {
            _pointsToUseText = value ?? "0";
            OnPropertyChanged(nameof(PointsToUseText));
            RecalculateTotals();
        }
    }

    public string OrderNote
    {
        get => _orderNote;
        set { _orderNote = value ?? string.Empty; OnPropertyChanged(nameof(OrderNote)); }
    }

    public bool HasSelectedMovie => SelectedMovie is not null;
    public bool HasNoMovies => Movies.Count == 0;
    public bool HasMovieShowtimes => MovieShowtimes.Count > 0;
    public bool HasNoMovieShowtimes => HasSelectedMovie && MovieShowtimes.Count == 0;
    public bool HasSelectedShowtime => SelectedShowtime is not null;

    public string SeatMapHeader { get => _seatMapHeader; private set { _seatMapHeader = value; OnPropertyChanged(nameof(SeatMapHeader)); } }
    public string SeatMapSubtitle { get => _seatMapSubtitle; private set { _seatMapSubtitle = value; OnPropertyChanged(nameof(SeatMapSubtitle)); } }

    public decimal SubTotalAmount => SelectedSeats.Sum(s => s.UnitPrice);
    public int MaxRedeemablePoints => _memberProfile is null
        ? 0
        : BookingData.CalculateMaxRedeemablePoints(_memberProfile.LoyaltyPoints, SubTotalAmount);

    public int ValidatedPointsToUse
    {
        get
        {
            if (_memberProfile is null) return 0;
            if (!int.TryParse(PointsToUseText.Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out int pts) || pts < 0)
            {
                return 0;
            }
            return Math.Min(pts, MaxRedeemablePoints);
        }
    }

    public decimal DiscountAmount => ValidatedPointsToUse * BookingData.PointRedeemValueVnd;
    public decimal TotalAmount => Math.Max(0m, SubTotalAmount - DiscountAmount);
    public int EstimatedEarnedPoints => BookingData.CalculateEarnedPoints(TotalAmount);

    public bool CanBook => _memberProfile is not null && SelectedShowtime is not null && SelectedSeats.Count > 0;

    public string SelectedSeatsText => SelectedSeats.Count == 0
        ? "Chưa chọn ghế nào"
        : string.Join(", ", SelectedSeats.Select(s => $"{s.SeatCode} ({s.UnitPriceText})"));

    public string SubTotalText => FormatHelper.FormatVnd(SubTotalAmount);
    public string DiscountText => DiscountAmount > 0m
        ? $"-{FormatHelper.FormatVnd(DiscountAmount)} ({ValidatedPointsToUse} điểm)"
        : "0đ";
    public string TotalAmountText => FormatHelper.FormatVnd(TotalAmount);
    public string EstimatedPointsText => $"+{EstimatedEarnedPoints} điểm";

    public string? ErrorMessage
    {
        get => _errorMessage;
        private set { _errorMessage = value; OnPropertyChanged(nameof(ErrorMessage)); OnPropertyChanged(nameof(HasError)); }
    }
    public bool HasError => !string.IsNullOrWhiteSpace(ErrorMessage);

    public string? SuccessMessage
    {
        get => _successMessage;
        private set { _successMessage = value; OnPropertyChanged(nameof(SuccessMessage)); OnPropertyChanged(nameof(HasSuccess)); }
    }
    public bool HasSuccess => !string.IsNullOrWhiteSpace(SuccessMessage);

    public string? LastBookingSummary
    {
        get => _lastBookingSummary;
        private set { _lastBookingSummary = value; OnPropertyChanged(nameof(LastBookingSummary)); OnPropertyChanged(nameof(HasLastBooking)); }
    }
    public bool HasLastBooking => !string.IsNullOrWhiteSpace(LastBookingSummary);

    public override void LoadData()
    {
        int? preferredMovieId = SelectedMovie?.MovieId;
        int? preferredShowtimeId = SelectedShowtime?.ShowtimeId;

        RefreshMemberProfile();

        GenreOptions.Clear();
        GenreOptions.Add(AllGenresLabel);
        foreach (string genre in BookingData.LoadGenreNames())
        {
            GenreOptions.Add(genre);
        }
        if (!GenreOptions.Contains(SelectedGenre))
        {
            _selectedGenre = AllGenresLabel;
            OnPropertyChanged(nameof(SelectedGenre));
        }

        _allMovies.Clear();
        foreach (BrowseMovieRow row in BookingData.LoadBrowseMovies())
        {
            _allMovies.Add(new BrowseMovieCardItem(row, TryLoadPoster(row.PosterPath)));
        }

        ApplyMovieFilter(preferredMovieId, preferredShowtimeId);
    }

    public void SetStatusFilter(string? filter) => StatusFilter = filter ?? "NowShowing";

    public void SelectMovie(BrowseMovieCardItem? movie)
    {
        ClearMessages();
        SelectedMovie = movie;
    }

    public void ToggleSeat(BookingSeatCellItem? seat)
    {
        ClearMessages();
        if (seat is null) return;

        if (!seat.IsActive)
        {
            ErrorMessage = $"Ghế {seat.SeatCode} đang bảo trì, không thể chọn.";
            return;
        }

        if (seat.IsBooked)
        {
            ErrorMessage = $"Ghế {seat.SeatCode} đã có người đặt ở suất chiếu này.";
            return;
        }

        if (seat.IsSelected)
        {
            seat.IsSelected = false;
            SelectedSeats.Remove(seat);
        }
        else
        {
            if (SelectedSeats.Count >= BookingData.MaxSeatsPerOrder)
            {
                ErrorMessage = $"Mỗi đơn đặt trực tuyến chỉ được chọn tối đa {BookingData.MaxSeatsPerOrder} ghế.";
                return;
            }

            seat.IsSelected = true;
            SelectedSeats.Add(seat);
        }

        RecalculateTotals();
    }

    public void ClearSelectedSeats()
    {
        foreach (BookingSeatCellItem seat in SelectedSeats)
        {
            seat.IsSelected = false;
        }
        SelectedSeats.Clear();
        RecalculateTotals();
    }

    public void ApplyMaxPoints()
    {
        ClearMessages();
        PointsToUseText = MaxRedeemablePoints.ToString(CultureInfo.InvariantCulture);
    }

    public void ConfirmBooking()
    {
        ClearMessages();

        if (_memberProfile is null)
        {
            ErrorMessage = "Tài khoản hiện tại chưa có hồ sơ hội viên để đặt vé trực tuyến.";
            return;
        }

        if (SelectedShowtime is null)
        {
            ErrorMessage = "Vui lòng chọn suất chiếu mở bán.";
            return;
        }

        if (SelectedSeats.Count == 0)
        {
            ErrorMessage = "Vui lòng chọn ít nhất 1 ghế trên sơ đồ.";
            return;
        }

        if (!int.TryParse(PointsToUseText.Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out int pointsToUse) || pointsToUse < 0)
        {
            ErrorMessage = "Số điểm đổi thưởng phải là số nguyên không âm.";
            return;
        }

        if (pointsToUse > MaxRedeemablePoints)
        {
            ErrorMessage = $"Bạn chỉ có thể dùng tối đa {MaxRedeemablePoints} điểm cho đơn này.";
            return;
        }

        var input = new BookingCreateInput(
            ShowtimeId: SelectedShowtime.ShowtimeId,
            SeatIds: SelectedSeats.Select(s => s.SeatId).ToList(),
            StaffUserId: null,
            CustomerId: _memberProfile.CustomerId,
            BookingChannel: "SelfService",
            PointsUsed: pointsToUse,
            PaymentMethod: SelectedPaymentMethod.Code,
            CashReceived: 0m,
            Note: string.IsNullOrWhiteSpace(OrderNote)
                ? $"Đặt vé trực tuyến {SelectedShowtime.MovieTitle}"
                : OrderNote.Trim());

        BookingCreateResult result = BookingData.CreateOrder(input);
        if (!result.Succeeded)
        {
            ErrorMessage = result.ErrorMessage ?? "Không thể hoàn tất đặt vé.";
            LoadSeatMapForSelectedShowtime();
            return;
        }

        LastBookingSummary =
            $"Đặt vé thành công • Mã đơn {result.OrderCode} | Vé: {string.Join(", ", result.TicketCodes)} | Đã thanh toán {FormatHelper.FormatVnd(result.TotalAmount)} ({SelectedPaymentMethod.Label}) | +{result.EarnedPoints} điểm (Còn {result.UpdatedCustomerPoints ?? 0} điểm, hạng {result.UpdatedCustomerTier ?? _memberProfile.Tier})";

        PointsToUseText = "0";
        OrderNote = string.Empty;

        LoadData();
        SuccessMessage = $"Đã đặt thành công {result.TicketCodes.Count} vé — xem chi tiết trong mục \"Vé của tôi\".";
    }

    private void RefreshMemberProfile()
    {
        if (App.CurrentUser?.CustomerProfile is not null)
        {
            _memberProfile = BookingData.FindCustomerById(App.CurrentUser.CustomerProfile.CustomerId);
        }
        else if (App.CurrentUser is not null)
        {
            _memberProfile = BookingData.FindCustomerByUserId(App.CurrentUser.UserId);
        }

        if (_memberProfile is not null && App.CurrentUser is not null)
        {
            App.CurrentUser.CustomerProfile = new Models.Customer
            {
                CustomerId = _memberProfile.CustomerId,
                UserId = _memberProfile.UserId,
                FullName = _memberProfile.FullName,
                Phone = _memberProfile.Phone,
                Email = _memberProfile.Email,
                LoyaltyPoints = _memberProfile.LoyaltyPoints,
                TotalSpent = _memberProfile.TotalSpent,
                Tier = Enum.TryParse(_memberProfile.Tier, true, out CustomerTier t) ? t : CustomerTier.Member
            };
        }

        OnPropertyChanged(nameof(MemberHeaderText));
        OnPropertyChanged(nameof(PointsHintText));
    }

    private void ApplyMovieFilter(int? preferredMovieId = null, int? preferredShowtimeId = null)
    {
        int? keepMovieId = preferredMovieId ?? SelectedMovie?.MovieId;
        string kw = SearchKeyword.Trim();

        Movies.Clear();
        IEnumerable<BrowseMovieCardItem> query = _allMovies;

        if (StatusFilter != "All")
        {
            query = query.Where(m => m.Status == StatusFilter);
        }

        if (!string.IsNullOrWhiteSpace(SelectedGenre) && SelectedGenre != AllGenresLabel)
        {
            query = query.Where(m => m.GenresText.Contains(SelectedGenre, StringComparison.OrdinalIgnoreCase));
        }

        if (!string.IsNullOrWhiteSpace(kw))
        {
            query = query.Where(m =>
                m.Title.Contains(kw, StringComparison.OrdinalIgnoreCase)
                || (!string.IsNullOrWhiteSpace(m.OriginalTitle) && m.OriginalTitle.Contains(kw, StringComparison.OrdinalIgnoreCase))
                || m.DirectorText.Contains(kw, StringComparison.OrdinalIgnoreCase)
                || m.GenresText.Contains(kw, StringComparison.OrdinalIgnoreCase));
        }

        foreach (BrowseMovieCardItem item in query)
        {
            Movies.Add(item);
        }

        OnPropertyChanged(nameof(HasNoMovies));

        BrowseMovieCardItem? target = keepMovieId.HasValue
            ? Movies.FirstOrDefault(m => m.MovieId == keepMovieId.Value) ?? Movies.FirstOrDefault()
            : Movies.FirstOrDefault();

        if (_selectedMovie is not null) _selectedMovie.IsSelected = false;
        _selectedMovie = target;
        if (_selectedMovie is not null) _selectedMovie.IsSelected = true;

        OnPropertyChanged(nameof(SelectedMovie));
        OnPropertyChanged(nameof(HasSelectedMovie));
        LoadShowtimesForSelectedMovie(preferredShowtimeId);
    }

    private void LoadShowtimesForSelectedMovie(int? preferredShowtimeId = null)
    {
        MovieShowtimes.Clear();
        if (SelectedMovie is not null)
        {
            foreach (OpenShowtimeOption st in BookingData.LoadOpenShowtimes(SelectedMovie.MovieId))
            {
                MovieShowtimes.Add(st);
            }
        }

        OnPropertyChanged(nameof(HasMovieShowtimes));
        OnPropertyChanged(nameof(HasNoMovieShowtimes));

        OpenShowtimeOption? nextShowtime = preferredShowtimeId.HasValue
            ? MovieShowtimes.FirstOrDefault(s => s.ShowtimeId == preferredShowtimeId.Value) ?? MovieShowtimes.FirstOrDefault()
            : MovieShowtimes.FirstOrDefault();

        _selectedShowtime = nextShowtime;
        OnPropertyChanged(nameof(SelectedShowtime));
        OnPropertyChanged(nameof(HasSelectedShowtime));
        LoadSeatMapForSelectedShowtime();
    }

    private void LoadSeatMapForSelectedShowtime()
    {
        SeatRows.Clear();
        SelectedSeats.Clear();

        if (SelectedShowtime is null)
        {
            SeatMapHeader = SelectedMovie is null
                ? "Sơ đồ Ghế suất chiếu"
                : $"{SelectedMovie.Title} — Chưa có suất chiếu mở bán";
            SeatMapSubtitle = "Chọn một suất chiếu đang mở bán phía trên để chọn chỗ ngồi.";
            RecalculateTotals();
            return;
        }

        List<ShowtimeSeatRow> rows = BookingData.LoadShowtimeSeatMap(SelectedShowtime.ShowtimeId);
        foreach (IGrouping<int, ShowtimeSeatRow> group in rows.GroupBy(r => r.RowIndex).OrderBy(g => g.Key))
        {
            string label = group.First().RowLabel;
            IEnumerable<BookingSeatCellItem> cells = group.Select(r => new BookingSeatCellItem(
                r,
                BookingData.CalculateSeatUnitPrice(SelectedShowtime.BaseTicketPrice, SelectedShowtime.RoomSurcharge, r.PriceMultiplier)));
            SeatRows.Add(new BookingSeatRowGroup(label, cells));
        }

        SeatMapHeader = $"{SelectedShowtime.MovieTitle} — {SelectedShowtime.RoomName}";
        SeatMapSubtitle =
            $"{SelectedShowtime.ShowDateDisplay} • {SelectedShowtime.TimeRangeDisplay} • {SelectedShowtime.Format} • Giá thường {SelectedShowtime.StandardPriceText} • {SelectedShowtime.AvailabilityText}";

        RecalculateTotals();
    }

    private void RecalculateTotals()
    {
        OnPropertyChanged(nameof(CanBook));
        OnPropertyChanged(nameof(SelectedSeatsText));
        OnPropertyChanged(nameof(SubTotalAmount));
        OnPropertyChanged(nameof(SubTotalText));
        OnPropertyChanged(nameof(MaxRedeemablePoints));
        OnPropertyChanged(nameof(ValidatedPointsToUse));
        OnPropertyChanged(nameof(DiscountAmount));
        OnPropertyChanged(nameof(DiscountText));
        OnPropertyChanged(nameof(TotalAmount));
        OnPropertyChanged(nameof(TotalAmountText));
        OnPropertyChanged(nameof(EstimatedEarnedPoints));
        OnPropertyChanged(nameof(EstimatedPointsText));
        OnPropertyChanged(nameof(PointsHintText));
    }

    private void ClearMessages()
    {
        ErrorMessage = null;
        SuccessMessage = null;
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
