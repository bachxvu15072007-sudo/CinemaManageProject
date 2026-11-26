using System.Collections.ObjectModel;
using System.Globalization;
using CinemaManageProject.Data;
using CinemaManageProject.Helpers;
using CommunityToolkit.Mvvm.ComponentModel;

namespace CinemaManageProject.ViewModels.Staff;

public sealed record PosPaymentOption(string Code, string Label)
{
    public override string ToString() => Label;
}

/// <summary>
/// ViewModel màn hình Bán vé tại quầy (POS) cho vai trò Staff.
/// </summary>
public sealed partial class StaffPosViewModel : ViewModelBase
{
    private readonly List<OpenShowtimeOption> _allShowtimes = [];
    private bool _suppressShowtimeLoad;

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
    private string _seatMapSubtitle = "Chọn một suất chiếu đang mở bán phía trên để chọn ghế.";

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
    public ObservableCollection<OpenShowtimeOption> Showtimes { get; } = [];
    public ObservableCollection<BookingSeatRowGroup> SeatRows { get; } = [];
    public ObservableCollection<BookingSeatCellItem> SelectedSeats { get; } = [];

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

    partial void OnSearchKeywordChanged(string value) => ApplyShowtimeFilter();

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
        int? preferredShowtimeId = SelectedShowtime?.ShowtimeId;

        _allShowtimes.Clear();
        _allShowtimes.AddRange(BookingData.LoadOpenShowtimes());

        if (SelectedCustomer is not null)
        {
            SelectedCustomer = BookingData.FindCustomerById(SelectedCustomer.CustomerId);
        }

        ApplyShowtimeFilter(preferredShowtimeId);
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

    private void ApplyShowtimeFilter(int? preferredShowtimeId = null)
    {
        int? keepId = preferredShowtimeId ?? SelectedShowtime?.ShowtimeId;
        string kw = (SearchKeyword ?? string.Empty).Trim();

        Showtimes.Clear();
        IEnumerable<OpenShowtimeOption> query = _allShowtimes;
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
}
