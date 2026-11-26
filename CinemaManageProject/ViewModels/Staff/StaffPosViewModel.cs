using System.Collections.ObjectModel;
using System.Globalization;
using System.IO;
using System.Windows.Media;
using CinemaManageProject.Data;
using CinemaManageProject.Helpers;
using CommunityToolkit.Mvvm.ComponentModel;

namespace CinemaManageProject.ViewModels.Staff;

public sealed record PosPaymentOption(string Code, string Label)
{
    public override string ToString() => Label;
}

public sealed partial class PosMoviePosterItem : ViewModelBase
{
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ActiveTag))]
    private bool _isSelected;

    public PosMoviePosterItem(
        int movieId,
        string title,
        string ageRating,
        int durationMinutes,
        string language,
        string genresText,
        int openShowtimesCount,
        decimal minStandardPrice,
        ImageSource? posterImage)
    {
        MovieId = movieId;
        Title = title;
        AgeRating = ageRating;
        MetaLine = $"{durationMinutes} phút • {language} • {ageRating}";
        GenresText = genresText;
        OpenShowtimesCount = openShowtimesCount;
        ShowtimeBadgeText = $"{openShowtimesCount} suất mở bán";
        PriceText = $"Từ {FormatHelper.FormatVnd(minStandardPrice)}";
        PosterImage = posterImage;
    }

    public int MovieId { get; }
    public string Title { get; }
    public string AgeRating { get; }
    public string MetaLine { get; }
    public string GenresText { get; }
    public int OpenShowtimesCount { get; }
    public string ShowtimeBadgeText { get; }
    public string PriceText { get; }
    public ImageSource? PosterImage { get; }
    public string? ActiveTag => IsSelected ? "Active" : null;
}

/// <summary>
/// ViewModel màn hình Bán vé tại quầy (POS) cho vai trò Staff.
/// </summary>
public sealed partial class StaffPosViewModel : ViewModelBase
{
    private readonly List<OpenShowtimeOption> _allShowtimes = [];
    private readonly List<PosMoviePosterItem> _allMovies = [];
    private bool _suppressShowtimeLoad;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasSelectedMovie))]
    [NotifyPropertyChangedFor(nameof(AllMoviesTag))]
    [NotifyPropertyChangedFor(nameof(ShowtimeListSubtitle))]
    private PosMoviePosterItem? _selectedMovie;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasSelectedShowtime))]
    private OpenShowtimeOption? _selectedShowtime;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasCustomer))]
    [NotifyPropertyChangedFor(nameof(CustomerDisplayText))]
    [NotifyPropertyChangedFor(nameof(CustomerPointsInfoText))]
    private BookingCustomerSummary? _selectedCustomer;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsCashPayment))]
    private PosPaymentOption _selectedPaymentMethod;

    [ObservableProperty]
    private string _searchKeyword = string.Empty;

    [ObservableProperty]
    private string _customerPhoneInput = string.Empty;

    [ObservableProperty]
    private string _pointsToUseText = "0";

    [ObservableProperty]
    private string _cashReceivedText = string.Empty;

    [ObservableProperty]
    private string _orderNote = string.Empty;

    [ObservableProperty]
    private string _seatMapHeader = "Sơ đồ Ghế suất chiếu";

    [ObservableProperty]
    private string _seatMapSubtitle = "Chọn một suất chiếu đang mở bán bên trái để chọn ghế.";

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasError))]
    private string? _errorMessage;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasSuccess))]
    private string? _successMessage;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasLastReceipt))]
    private string? _lastReceiptSummary;

    public StaffPosViewModel()
    {
        PaymentOptions =
        [
            new PosPaymentOption("Cash", "Tiền mặt"),
            new PosPaymentOption("BankTransfer", "Chuyển khoản"),
            new PosPaymentOption("Card", "Thẻ ngân hàng"),
            new PosPaymentOption("EWallet", "Ví điện tử")
        ];
        _selectedPaymentMethod = PaymentOptions[0];
        LoadData();
    }

    public IReadOnlyList<PosPaymentOption> PaymentOptions { get; }
    public ObservableCollection<PosMoviePosterItem> Movies { get; } = [];
    public ObservableCollection<OpenShowtimeOption> Showtimes { get; } = [];
    public ObservableCollection<BookingSeatRowGroup> SeatRows { get; } = [];
    public ObservableCollection<BookingSeatCellItem> SelectedSeats { get; } = [];

    public bool HasSelectedMovie => SelectedMovie is not null;
    public bool HasNoMovies => Movies.Count == 0;
    public string? AllMoviesTag => SelectedMovie is null ? "Active" : null;
    public string AllMoviesButtonText => $"Tất cả phim ({_allShowtimes.Count} suất)";
    public string ShowtimeListSubtitle => SelectedMovie is null
        ? $"Hiển thị {Showtimes.Count} suất chiếu đang mở bán"
        : $"{SelectedMovie.Title} • {Showtimes.Count} suất mở bán";

    public bool HasSelectedShowtime => SelectedShowtime is not null;
    public bool HasNoShowtimes => Showtimes.Count == 0;
    public bool HasCustomer => SelectedCustomer is not null;

    public string CustomerDisplayText => SelectedCustomer is null
        ? "Khách vãng lai (không tích điểm)"
        : $"{SelectedCustomer.FullName} • {SelectedCustomer.Phone}";

    public string CustomerPointsInfoText => SelectedCustomer is null
        ? "Nhập SĐT hội viên để tích/đổi điểm thưởng."
        : $"{SelectedCustomer.TierDisplay} • Hiện có {SelectedCustomer.LoyaltyPointsText} (tối đa dùng {MaxRedeemablePoints} điểm)";

    public bool IsCashPayment => SelectedPaymentMethod?.Code == "Cash";

    public decimal SubTotalAmount => SelectedSeats.Sum(s => s.UnitPrice);
    public int MaxRedeemablePoints => SelectedCustomer is null
        ? 0
        : BookingData.CalculateMaxRedeemablePoints(SelectedCustomer.LoyaltyPoints, SubTotalAmount);

    public int ValidatedPointsToUse
    {
        get
        {
            if (SelectedCustomer is null) return 0;
            if (!int.TryParse((PointsToUseText ?? string.Empty).Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out int pts) || pts < 0)
            {
                return 0;
            }
            return Math.Min(pts, MaxRedeemablePoints);
        }
    }

    public decimal DiscountAmount => ValidatedPointsToUse * BookingData.PointRedeemValueVnd;
    public decimal TotalAmount => Math.Max(0m, SubTotalAmount - DiscountAmount);

    public decimal ParsedCashReceived =>
        TryParseAmount(CashReceivedText, out decimal val) ? val : 0m;

    public decimal ChangeAmount => IsCashPayment
        ? Math.Max(0m, ParsedCashReceived - TotalAmount)
        : 0m;

    public bool HasSelectedSeats => SelectedSeats.Count > 0;
    public bool CanCheckout => SelectedShowtime is not null && SelectedSeats.Count > 0;

    public string SelectedSeatsText => SelectedSeats.Count == 0
        ? "Chưa chọn ghế nào"
        : string.Join(", ", SelectedSeats.Select(s => $"{s.SeatCode} ({s.UnitPriceText})"));

    public string SubTotalText => FormatHelper.FormatVnd(SubTotalAmount);
    public string DiscountText => DiscountAmount > 0m
        ? $"-{FormatHelper.FormatVnd(DiscountAmount)} ({ValidatedPointsToUse} điểm)"
        : "0đ";
    public string TotalAmountText => FormatHelper.FormatVnd(TotalAmount);
    public string ChangeAmountText => IsCashPayment && ParsedCashReceived < TotalAmount && TotalAmount > 0m
        ? $"Thiếu {FormatHelper.FormatVnd(TotalAmount - ParsedCashReceived)}"
        : FormatHelper.FormatVnd(ChangeAmount);

    public bool HasError => !string.IsNullOrWhiteSpace(ErrorMessage);
    public bool HasSuccess => !string.IsNullOrWhiteSpace(SuccessMessage);
    public bool HasLastReceipt => !string.IsNullOrWhiteSpace(LastReceiptSummary);

    partial void OnSearchKeywordChanged(string value)
    {
        ApplyMovieFilter();
        ApplyShowtimeFilter();
    }

    partial void OnSelectedMovieChanging(PosMoviePosterItem? oldValue, PosMoviePosterItem? newValue)
    {
        if (oldValue is not null)
        {
            oldValue.IsSelected = false;
        }
    }

    partial void OnSelectedMovieChanged(PosMoviePosterItem? value)
    {
        if (value is not null)
        {
            value.IsSelected = true;
        }

        if (!_suppressShowtimeLoad)
        {
            ApplyShowtimeFilter();
        }
    }

    partial void OnSelectedShowtimeChanged(OpenShowtimeOption? value)
    {
        if (!_suppressShowtimeLoad)
        {
            LoadSeatMapForSelectedShowtime();
        }
    }

    partial void OnSelectedCustomerChanged(BookingCustomerSummary? value)
    {
        if (value is null)
        {
            PointsToUseText = "0";
        }

        RecalculateTotals();
    }

    partial void OnSelectedPaymentMethodChanged(PosPaymentOption value) => RecalculateTotals();
    partial void OnPointsToUseTextChanged(string value) => RecalculateTotals();
    partial void OnCashReceivedTextChanged(string value) => RecalculateTotals();

    public override void LoadData()
    {
        int? preferredMovieId = SelectedMovie?.MovieId;
        int? preferredShowtimeId = SelectedShowtime?.ShowtimeId;

        _allShowtimes.Clear();
        _allShowtimes.AddRange(BookingData.LoadOpenShowtimes());

        _allMovies.Clear();
        foreach (IGrouping<int, OpenShowtimeOption> group in _allShowtimes
                     .GroupBy(s => s.MovieId)
                     .OrderByDescending(g => g.Count())
                     .ThenBy(g => g.First().MovieTitle, StringComparer.OrdinalIgnoreCase))
        {
            OpenShowtimeOption first = group.First();
            decimal minPrice = group.Min(x => x.StandardPrice);
            _allMovies.Add(new PosMoviePosterItem(
                first.MovieId,
                first.MovieTitle,
                first.AgeRating,
                first.DurationMinutes,
                first.Language,
                first.GenresText,
                group.Count(),
                minPrice,
                TryLoadPoster(first.PosterPath)));
        }

        OnPropertyChanged(nameof(AllMoviesButtonText));

        if (SelectedCustomer is not null)
        {
            SelectedCustomer = BookingData.FindCustomerById(SelectedCustomer.CustomerId);
        }

        _suppressShowtimeLoad = true;
        ApplyMovieFilter(preferredMovieId);
        _suppressShowtimeLoad = false;

        ApplyShowtimeFilter(preferredShowtimeId);
    }

    public void SelectMovie(PosMoviePosterItem? movie)
    {
        ClearMessages();
        SelectedMovie = ReferenceEquals(SelectedMovie, movie) ? null : movie;
    }

    public void ShowAllMovies()
    {
        ClearMessages();
        SelectedMovie = null;
    }

    public void ToggleSeat(BookingSeatCellItem? seat)
    {
        ClearMessages();
        if (seat is null) return;

        if (!seat.IsActive)
        {
            ErrorMessage = $"Ghế {seat.SeatCode} đang bảo trì, không thể bán.";
            return;
        }

        if (seat.IsBooked)
        {
            ErrorMessage = $"Ghế {seat.SeatCode} đã có khách đặt ở suất chiếu này.";
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
                ErrorMessage = $"Mỗi hóa đơn chỉ chọn tối đa {BookingData.MaxSeatsPerOrder} ghế.";
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

    public void LookupCustomer()
    {
        ClearMessages();
        string phone = (CustomerPhoneInput ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(phone))
        {
            ErrorMessage = "Vui lòng nhập số điện thoại hội viên cần tra cứu.";
            return;
        }

        BookingCustomerSummary? found = BookingData.FindCustomerByPhone(phone);
        if (found is null)
        {
            ErrorMessage = $"Không tìm thấy hội viên với SĐT \"{phone}\". Bạn có thể bán vé vãng lai hoặc tạo hội viên ở mục Tra cứu Khách hàng.";
            return;
        }

        SelectedCustomer = found;
        PointsToUseText = "0";
        SuccessMessage = $"Đã gắn hội viên {found.FullName} ({found.TierDisplay}, {found.LoyaltyPointsText}).";
    }

    public void ClearCustomer()
    {
        ClearMessages();
        CustomerPhoneInput = string.Empty;
        SelectedCustomer = null;
    }

    public void ApplyMaxPoints()
    {
        ClearMessages();
        if (SelectedCustomer is null)
        {
            ErrorMessage = "Vui lòng tra cứu và gắn hội viên trước khi đổi điểm.";
            return;
        }

        PointsToUseText = MaxRedeemablePoints.ToString(CultureInfo.InvariantCulture);
    }

    public void SetExactCash()
    {
        CashReceivedText = TotalAmount > 0m
            ? ((long)TotalAmount).ToString(CultureInfo.InvariantCulture)
            : string.Empty;
    }

    public void SetQuickCash(decimal amount)
    {
        CashReceivedText = ((long)amount).ToString(CultureInfo.InvariantCulture);
    }

    public void CompleteSale()
    {
        ClearMessages();

        if (SelectedShowtime is null)
        {
            ErrorMessage = "Vui lòng chọn suất chiếu cần bán vé.";
            return;
        }

        if (SelectedSeats.Count == 0)
        {
            ErrorMessage = "Vui lòng chọn ít nhất 1 ghế trên sơ đồ.";
            return;
        }

        int pointsToUse = 0;
        if (SelectedCustomer is not null)
        {
            if (!int.TryParse((PointsToUseText ?? string.Empty).Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out pointsToUse) || pointsToUse < 0)
            {
                ErrorMessage = "Số điểm thưởng áp dụng phải là số nguyên không âm.";
                return;
            }

            if (pointsToUse > MaxRedeemablePoints)
            {
                ErrorMessage = $"Hội viên chỉ được áp tối đa {MaxRedeemablePoints} điểm cho đơn này.";
                return;
            }
        }

        decimal cashReceived = 0m;
        if (IsCashPayment)
        {
            if (!TryParseAmount(CashReceivedText, out cashReceived) || cashReceived < TotalAmount)
            {
                ErrorMessage = $"Vui lòng nhập tiền khách đưa tối thiểu {TotalAmountText}.";
                return;
            }
        }

        int staffId = App.CurrentUser?.UserId ?? 0;
        if (staffId <= 0)
        {
            ErrorMessage = "Không xác định được tài khoản nhân viên đang đăng nhập.";
            return;
        }

        PosPaymentOption paymentMethod = SelectedPaymentMethod ?? PaymentOptions[0];
        var input = new BookingCreateInput(
            ShowtimeId: SelectedShowtime.ShowtimeId,
            SeatIds: SelectedSeats.Select(s => s.SeatId).ToList(),
            StaffUserId: staffId,
            CustomerId: SelectedCustomer?.CustomerId,
            BookingChannel: "Counter",
            PointsUsed: pointsToUse,
            PaymentMethod: paymentMethod.Code,
            CashReceived: cashReceived,
            Note: string.IsNullOrWhiteSpace(OrderNote)
                ? $"Bán tại quầy bởi {App.CurrentUser?.Username ?? "staff"}"
                : OrderNote.Trim());

        BookingCreateResult result = BookingData.CreateOrder(input);
        if (!result.Succeeded)
        {
            ErrorMessage = result.ErrorMessage ?? "Không thể xuất vé.";
            LoadSeatMapForSelectedShowtime();
            return;
        }

        string cashSummary = IsCashPayment
            ? $" • Khách đưa {FormatHelper.FormatVnd(result.CashReceived)} • Tiền thối {FormatHelper.FormatVnd(result.ChangeAmount)}"
            : $" • {paymentMethod.Label}";
        string pointsSummary = result.UpdatedCustomerPoints.HasValue
            ? $" • +{result.EarnedPoints} điểm (Còn {result.UpdatedCustomerPoints.Value} điểm, hạng {result.UpdatedCustomerTier})"
            : string.Empty;

        LastReceiptSummary =
            $"Hóa đơn {result.OrderCode} | {result.TicketCodes.Count} vé ({string.Join(", ", result.TicketCodes)}) | Thu {FormatHelper.FormatVnd(result.TotalAmount)}{cashSummary}{pointsSummary}";

        PointsToUseText = "0";
        CashReceivedText = string.Empty;
        OrderNote = string.Empty;

        LoadData();
        SuccessMessage = $"Xuất thành công {result.TicketCodes.Count} vé — Mã hóa đơn {result.OrderCode}.";
    }

    private void ApplyMovieFilter(int? preferredMovieId = null)
    {
        int? keepMovieId = preferredMovieId ?? SelectedMovie?.MovieId;
        string kw = (SearchKeyword ?? string.Empty).Trim();

        Movies.Clear();
        IEnumerable<PosMoviePosterItem> query = _allMovies;
        if (!string.IsNullOrWhiteSpace(kw))
        {
            query = query.Where(m =>
                m.Title.Contains(kw, StringComparison.OrdinalIgnoreCase)
                || m.GenresText.Contains(kw, StringComparison.OrdinalIgnoreCase)
                || _allShowtimes.Any(s =>
                    s.MovieId == m.MovieId
                    && (s.RoomName.Contains(kw, StringComparison.OrdinalIgnoreCase)
                        || s.Format.Contains(kw, StringComparison.OrdinalIgnoreCase))));
        }

        foreach (PosMoviePosterItem movie in query)
        {
            Movies.Add(movie);
        }

        OnPropertyChanged(nameof(HasNoMovies));

        PosMoviePosterItem? nextMovie = keepMovieId.HasValue
            ? Movies.FirstOrDefault(m => m.MovieId == keepMovieId.Value)
            : null;

        bool prevSuppress = _suppressShowtimeLoad;
        _suppressShowtimeLoad = true;
        SelectedMovie = nextMovie;
        _suppressShowtimeLoad = prevSuppress;
    }

    private void ApplyShowtimeFilter(int? preferredShowtimeId = null)
    {
        int? keepId = preferredShowtimeId ?? SelectedShowtime?.ShowtimeId;
        string kw = (SearchKeyword ?? string.Empty).Trim();

        Showtimes.Clear();
        IEnumerable<OpenShowtimeOption> query = _allShowtimes;
        if (SelectedMovie is not null)
        {
            query = query.Where(s => s.MovieId == SelectedMovie.MovieId);
        }

        if (!string.IsNullOrWhiteSpace(kw))
        {
            query = query.Where(s =>
                s.MovieTitle.Contains(kw, StringComparison.OrdinalIgnoreCase)
                || s.RoomName.Contains(kw, StringComparison.OrdinalIgnoreCase)
                || s.Format.Contains(kw, StringComparison.OrdinalIgnoreCase));
        }

        foreach (OpenShowtimeOption item in query)
        {
            Showtimes.Add(item);
        }

        OnPropertyChanged(nameof(HasNoShowtimes));
        OnPropertyChanged(nameof(ShowtimeListSubtitle));

        OpenShowtimeOption? next = keepId.HasValue
            ? Showtimes.FirstOrDefault(s => s.ShowtimeId == keepId.Value) ?? Showtimes.FirstOrDefault()
            : Showtimes.FirstOrDefault();

        _suppressShowtimeLoad = true;
        SelectedShowtime = next;
        _suppressShowtimeLoad = false;
        LoadSeatMapForSelectedShowtime();
    }

    private void LoadSeatMapForSelectedShowtime()
    {
        SeatRows.Clear();
        SelectedSeats.Clear();

        if (SelectedShowtime is null)
        {
            SeatMapHeader = "Sơ đồ Ghế suất chiếu";
            SeatMapSubtitle = "Chưa có suất chiếu mở bán nào.";
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
            $"{SelectedShowtime.ShowDateDisplay} • {SelectedShowtime.TimeRangeDisplay} • {SelectedShowtime.Format} • Giá ghế thường {SelectedShowtime.StandardPriceText} • {SelectedShowtime.AvailabilityText}";

        RecalculateTotals();
    }

    private void RecalculateTotals()
    {
        OnPropertyChanged(nameof(HasSelectedSeats));
        OnPropertyChanged(nameof(CanCheckout));
        OnPropertyChanged(nameof(SelectedSeatsText));
        OnPropertyChanged(nameof(SubTotalAmount));
        OnPropertyChanged(nameof(SubTotalText));
        OnPropertyChanged(nameof(MaxRedeemablePoints));
        OnPropertyChanged(nameof(ValidatedPointsToUse));
        OnPropertyChanged(nameof(DiscountAmount));
        OnPropertyChanged(nameof(DiscountText));
        OnPropertyChanged(nameof(TotalAmount));
        OnPropertyChanged(nameof(TotalAmountText));
        OnPropertyChanged(nameof(ChangeAmount));
        OnPropertyChanged(nameof(ChangeAmountText));
        OnPropertyChanged(nameof(CustomerPointsInfoText));
    }

    private void ClearMessages()
    {
        ErrorMessage = null;
        SuccessMessage = null;
    }

    private static bool TryParseAmount(string? raw, out decimal value)
    {
        string normalized = (raw ?? string.Empty).Replace(".", string.Empty).Replace(",", string.Empty).Trim();
        return decimal.TryParse(normalized, NumberStyles.Number, CultureInfo.InvariantCulture, out value) && value >= 0m;
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
