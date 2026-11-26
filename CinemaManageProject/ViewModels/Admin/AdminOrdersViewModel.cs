using System.Collections.ObjectModel;
using System.Globalization;
using CinemaManageProject.Data;
using CinemaManageProject.Helpers;

namespace CinemaManageProject.ViewModels.Admin;

public sealed record OrderFilterOption(string Value, string Label)
{
    public override string ToString() => Label;
}

public sealed record PaymentBreakdownRow(
    string MethodKey,
    string MethodLabel,
    int OrderCount,
    decimal RevenueAmount,
    string RevenueText,
    string ShareText,
    double ShareRatio);

public sealed record MovieRevenueRow(
    int MovieId,
    string MovieTitle,
    int TicketCount,
    decimal RevenueAmount,
    string RevenueText,
    string TicketCountText,
    string ShareText);

/// <summary>
/// ViewModel tra cứu hóa đơn và phân tích doanh thu cho vai trò Admin (chỉ đọc).
/// </summary>
public sealed class AdminOrdersViewModel : ViewModelBase
{
    private static readonly string[] PaymentMethodOrder = ["Cash", "BankTransfer", "Card", "EWallet"];
    private static readonly string[] SqlDateFormats =
    [
        "yyyy-MM-dd HH:mm:ss",
        "yyyy-MM-dd HH:mm",
        "yyyy-MM-dd",
        "yyyy-MM-ddTHH:mm:ss",
        "yyyy-MM-ddTHH:mm:ss.fff"
    ];

    private List<AdminOrderSummaryRow> _allOrders = [];
    private Dictionary<int, IReadOnlyList<OrderTicketDetailRow>> _ticketsByOrder = [];
    private bool _isResetting;

    private string _searchKeyword = string.Empty;
    private OrderFilterOption? _selectedDateRangeOption;
    private OrderFilterOption? _selectedChannelFilterOption;
    private OrderFilterOption? _selectedPaymentFilterOption;
    private OrderFilterOption? _selectedStatusFilterOption;

    private AdminOrderSummaryRow? _selectedOrder;

    private string _filteredRevenueText = "0đ";
    private string _globalRevenueSubText = "Toàn rạp: 0đ • Hôm nay: 0đ";
    private string _counterRevenueText = "0đ";
    private string _counterOrdersSubText = "0 đơn hiệu lực";
    private string _selfServiceRevenueText = "0đ";
    private string _selfServiceOrdersSubText = "0 đơn hiệu lực";
    private string _filteredOrdersTicketsText = "0 đơn • 0 vé hiệu lực";
    private string _cancelledOrdersSubText = "0 đơn hủy (0đ)";
    private string _resultCountText = "0 hóa đơn";

    public AdminOrdersViewModel()
    {
        DateRangeOptions =
        [
            new OrderFilterOption("All", "Tất cả thời gian"),
            new OrderFilterOption("Today", "Hôm nay"),
            new OrderFilterOption("Last7Days", "7 ngày gần đây"),
            new OrderFilterOption("Last30Days", "30 ngày gần đây")
        ];

        ChannelFilterOptions =
        [
            new OrderFilterOption("All", "Tất cả kênh"),
            new OrderFilterOption("Counter", "Tại quầy"),
            new OrderFilterOption("SelfService", "Tự đặt")
        ];

        PaymentFilterOptions =
        [
            new OrderFilterOption("All", "Tất cả PTTT"),
            new OrderFilterOption("Cash", "Tiền mặt"),
            new OrderFilterOption("BankTransfer", "Chuyển khoản"),
            new OrderFilterOption("Card", "Thẻ"),
            new OrderFilterOption("EWallet", "Ví điện tử")
        ];

        StatusFilterOptions =
        [
            new OrderFilterOption("All", "Tất cả trạng thái"),
            new OrderFilterOption("Paid", "Đã thanh toán"),
            new OrderFilterOption("PartiallyRefunded", "Hoàn một phần"),
            new OrderFilterOption("Cancelled", "Đã hủy")
        ];

        _selectedDateRangeOption = DateRangeOptions[0];
        _selectedChannelFilterOption = ChannelFilterOptions[0];
        _selectedPaymentFilterOption = PaymentFilterOptions[0];
        _selectedStatusFilterOption = StatusFilterOptions[0];

        LoadData();
    }

    public IReadOnlyList<OrderFilterOption> DateRangeOptions { get; }
    public IReadOnlyList<OrderFilterOption> ChannelFilterOptions { get; }
    public IReadOnlyList<OrderFilterOption> PaymentFilterOptions { get; }
    public IReadOnlyList<OrderFilterOption> StatusFilterOptions { get; }

    public ObservableCollection<AdminOrderSummaryRow> Orders { get; } = [];
    public ObservableCollection<OrderTicketDetailRow> SelectedOrderTickets { get; } = [];
    public ObservableCollection<PaymentBreakdownRow> PaymentBreakdowns { get; } = [];
    public ObservableCollection<MovieRevenueRow> MovieRevenues { get; } = [];

    public string SearchKeyword
    {
        get => _searchKeyword;
        set
        {
            if (_searchKeyword == value)
            {
                return;
            }

            _searchKeyword = value ?? string.Empty;
            OnPropertyChanged(nameof(SearchKeyword));
            if (!_isResetting)
            {
                ApplyFilters();
            }
        }
    }

    public OrderFilterOption? SelectedDateRangeOption
    {
        get => _selectedDateRangeOption;
        set => SetFilterOption(ref _selectedDateRangeOption, value, nameof(SelectedDateRangeOption));
    }

    public OrderFilterOption? SelectedChannelFilterOption
    {
        get => _selectedChannelFilterOption;
        set => SetFilterOption(ref _selectedChannelFilterOption, value, nameof(SelectedChannelFilterOption));
    }

    public OrderFilterOption? SelectedPaymentFilterOption
    {
        get => _selectedPaymentFilterOption;
        set => SetFilterOption(ref _selectedPaymentFilterOption, value, nameof(SelectedPaymentFilterOption));
    }

    public OrderFilterOption? SelectedStatusFilterOption
    {
        get => _selectedStatusFilterOption;
        set => SetFilterOption(ref _selectedStatusFilterOption, value, nameof(SelectedStatusFilterOption));
    }

    public AdminOrderSummaryRow? SelectedOrder
    {
        get => _selectedOrder;
        set
        {
            if (Equals(_selectedOrder, value))
            {
                return;
            }

            _selectedOrder = value;
            OnPropertyChanged(nameof(SelectedOrder));
            OnPropertyChanged(nameof(HasSelectedOrder));
            UpdateSelectedOrderTickets();
        }
    }

    public bool HasSelectedOrder => SelectedOrder is not null;
    public bool HasNoOrders => Orders.Count == 0;
    public bool HasNoSelectedOrderTickets => SelectedOrderTickets.Count == 0;
    public bool HasNoMovieRevenues => MovieRevenues.Count == 0;

    public string FilteredRevenueText
    {
        get => _filteredRevenueText;
        private set
        {
            if (_filteredRevenueText == value) return;
            _filteredRevenueText = value;
            OnPropertyChanged(nameof(FilteredRevenueText));
        }
    }

    public string GlobalRevenueSubText
    {
        get => _globalRevenueSubText;
        private set
        {
            if (_globalRevenueSubText == value) return;
            _globalRevenueSubText = value;
            OnPropertyChanged(nameof(GlobalRevenueSubText));
        }
    }

    public string CounterRevenueText
    {
        get => _counterRevenueText;
        private set
        {
            if (_counterRevenueText == value) return;
            _counterRevenueText = value;
            OnPropertyChanged(nameof(CounterRevenueText));
        }
    }

    public string CounterOrdersSubText
    {
        get => _counterOrdersSubText;
        private set
        {
            if (_counterOrdersSubText == value) return;
            _counterOrdersSubText = value;
            OnPropertyChanged(nameof(CounterOrdersSubText));
        }
    }

    public string SelfServiceRevenueText
    {
        get => _selfServiceRevenueText;
        private set
        {
            if (_selfServiceRevenueText == value) return;
            _selfServiceRevenueText = value;
            OnPropertyChanged(nameof(SelfServiceRevenueText));
        }
    }

    public string SelfServiceOrdersSubText
    {
        get => _selfServiceOrdersSubText;
        private set
        {
            if (_selfServiceOrdersSubText == value) return;
            _selfServiceOrdersSubText = value;
            OnPropertyChanged(nameof(SelfServiceOrdersSubText));
        }
    }

    public string FilteredOrdersTicketsText
    {
        get => _filteredOrdersTicketsText;
        private set
        {
            if (_filteredOrdersTicketsText == value) return;
            _filteredOrdersTicketsText = value;
            OnPropertyChanged(nameof(FilteredOrdersTicketsText));
        }
    }

    public string CancelledOrdersSubText
    {
        get => _cancelledOrdersSubText;
        private set
        {
            if (_cancelledOrdersSubText == value) return;
            _cancelledOrdersSubText = value;
            OnPropertyChanged(nameof(CancelledOrdersSubText));
        }
    }

    public string ResultCountText
    {
        get => _resultCountText;
        private set
        {
            if (_resultCountText == value) return;
            _resultCountText = value;
            OnPropertyChanged(nameof(ResultCountText));
        }
    }

    public override void LoadData()
    {
        _allOrders = OrderData.LoadAllOrders();
        _ticketsByOrder = OrderData.LoadAllOrderTickets();
        ApplyFilters();
    }

    public void ResetFilters()
    {
        _isResetting = true;
        try
        {
            _searchKeyword = string.Empty;
            _selectedDateRangeOption = DateRangeOptions[0];
            _selectedChannelFilterOption = ChannelFilterOptions[0];
            _selectedPaymentFilterOption = PaymentFilterOptions[0];
            _selectedStatusFilterOption = StatusFilterOptions[0];

            OnPropertyChanged(nameof(SearchKeyword));
            OnPropertyChanged(nameof(SelectedDateRangeOption));
            OnPropertyChanged(nameof(SelectedChannelFilterOption));
            OnPropertyChanged(nameof(SelectedPaymentFilterOption));
            OnPropertyChanged(nameof(SelectedStatusFilterOption));
        }
        finally
        {
            _isResetting = false;
        }

        ApplyFilters();
    }

    private void SetFilterOption(ref OrderFilterOption? field, OrderFilterOption? value, string propertyName)
    {
        if (Equals(field, value))
        {
            return;
        }

        field = value;
        OnPropertyChanged(propertyName);
        if (!_isResetting)
        {
            ApplyFilters();
        }
    }

    private void ApplyFilters()
    {
        int? previousOrderId = _selectedOrder?.OrderId;
        string keyword = _searchKeyword.Trim();
        string dateRange = _selectedDateRangeOption?.Value ?? "All";
        string channel = _selectedChannelFilterOption?.Value ?? "All";
        string payment = _selectedPaymentFilterOption?.Value ?? "All";
        string status = _selectedStatusFilterOption?.Value ?? "All";
        DateTime today = DateTime.Today;

        List<AdminOrderSummaryRow> filtered = [];
        foreach (AdminOrderSummaryRow order in _allOrders)
        {
            if (!MatchesDateRange(order.CreatedAtSql, dateRange, today))
            {
                continue;
            }

            if (channel != "All" && !string.Equals(order.BookingChannel, channel, StringComparison.Ordinal))
            {
                continue;
            }

            if (payment != "All" && !string.Equals(order.PaymentMethod, payment, StringComparison.Ordinal))
            {
                continue;
            }

            if (status != "All" && !string.Equals(order.Status, status, StringComparison.Ordinal))
            {
                continue;
            }

            if (!string.IsNullOrEmpty(keyword) && !MatchesKeyword(order, keyword))
            {
                continue;
            }

            filtered.Add(order);
        }

        Orders.Clear();
        foreach (AdminOrderSummaryRow order in filtered)
        {
            Orders.Add(order);
        }

        OnPropertyChanged(nameof(HasNoOrders));
        ResultCountText = $"{Orders.Count} hóa đơn";

        UpdateKpis(filtered, today);
        UpdatePaymentBreakdowns(filtered);
        UpdateMovieRevenues(filtered);

        AdminOrderSummaryRow? nextSelected = previousOrderId.HasValue
            ? Orders.FirstOrDefault(o => o.OrderId == previousOrderId.Value)
            : null;
        nextSelected ??= Orders.FirstOrDefault();

        _selectedOrder = nextSelected;
        OnPropertyChanged(nameof(SelectedOrder));
        OnPropertyChanged(nameof(HasSelectedOrder));
        UpdateSelectedOrderTickets();
    }

    private void UpdateKpis(List<AdminOrderSummaryRow> filtered, DateTime today)
    {
        decimal globalRevenue = 0m;
        decimal todayRevenue = 0m;
        foreach (AdminOrderSummaryRow order in _allOrders)
        {
            if (!order.IsRevenueEffective)
            {
                continue;
            }

            globalRevenue += order.TotalAmount;
            if (TryParseSqlDate(order.CreatedAtSql, out DateTime createdDate) && createdDate.Date == today)
            {
                todayRevenue += order.TotalAmount;
            }
        }

        GlobalRevenueSubText =
            $"Toàn rạp: {FormatHelper.FormatVnd(globalRevenue)} • Hôm nay: {FormatHelper.FormatVnd(todayRevenue)}";

        decimal filteredRevenue = 0m;
        decimal counterRevenue = 0m;
        int counterEffectiveCount = 0;
        decimal selfServiceRevenue = 0m;
        int selfServiceEffectiveCount = 0;
        int effectiveTicketsCount = 0;
        int cancelledCount = 0;
        decimal cancelledAmount = 0m;

        foreach (AdminOrderSummaryRow order in filtered)
        {
            if (order.IsRevenueEffective)
            {
                filteredRevenue += order.TotalAmount;
                effectiveTicketsCount += order.ActiveTicketsCount;

                if (order.BookingChannel == "Counter")
                {
                    counterRevenue += order.TotalAmount;
                    counterEffectiveCount++;
                }
                else if (order.BookingChannel == "SelfService")
                {
                    selfServiceRevenue += order.TotalAmount;
                    selfServiceEffectiveCount++;
                }
            }
            else if (order.IsCancelled)
            {
                cancelledCount++;
                cancelledAmount += order.TotalAmount;
            }
        }

        FilteredRevenueText = FormatHelper.FormatVnd(filteredRevenue);
        CounterRevenueText = FormatHelper.FormatVnd(counterRevenue);
        CounterOrdersSubText = $"{counterEffectiveCount} đơn hiệu lực";
        SelfServiceRevenueText = FormatHelper.FormatVnd(selfServiceRevenue);
        SelfServiceOrdersSubText = $"{selfServiceEffectiveCount} đơn hiệu lực";
        FilteredOrdersTicketsText = $"{filtered.Count} đơn • {effectiveTicketsCount} vé hiệu lực";
        CancelledOrdersSubText = $"{cancelledCount} đơn hủy ({FormatHelper.FormatVnd(cancelledAmount)})";
    }

    private void UpdatePaymentBreakdowns(List<AdminOrderSummaryRow> filtered)
    {
        List<AdminOrderSummaryRow> effectiveOrders = filtered.Where(o => o.IsRevenueEffective).ToList();
        decimal totalRevenue = effectiveOrders.Sum(o => o.TotalAmount);

        PaymentBreakdowns.Clear();
        foreach (string methodKey in PaymentMethodOrder)
        {
            int count = 0;
            decimal revenue = 0m;
            foreach (AdminOrderSummaryRow order in effectiveOrders)
            {
                if (order.PaymentMethod == methodKey)
                {
                    count++;
                    revenue += order.TotalAmount;
                }
            }

            double shareRatio = totalRevenue > 0m
                ? Math.Clamp((double)(revenue / totalRevenue), 0.0, 1.0)
                : 0.0;

            PaymentBreakdowns.Add(new PaymentBreakdownRow(
                MethodKey: methodKey,
                MethodLabel: FormatHelper.FormatPaymentMethod(methodKey),
                OrderCount: count,
                RevenueAmount: revenue,
                RevenueText: FormatHelper.FormatVnd(revenue),
                ShareText: FormatPercent(shareRatio),
                ShareRatio: shareRatio));
        }
    }

    private void UpdateMovieRevenues(List<AdminOrderSummaryRow> filtered)
    {
        List<AdminOrderSummaryRow> effectiveOrders = filtered.Where(o => o.IsRevenueEffective).ToList();
        decimal totalFilteredRevenue = effectiveOrders.Sum(o => o.TotalAmount);

        Dictionary<int, (string Title, int TicketCount, decimal RawRevenue)> byMovie = [];

        foreach (AdminOrderSummaryRow order in effectiveOrders)
        {
            if (!_ticketsByOrder.TryGetValue(order.OrderId, out IReadOnlyList<OrderTicketDetailRow>? tickets))
            {
                continue;
            }

            List<OrderTicketDetailRow> activeTickets = tickets.Where(t => t.IsEffective).ToList();
            if (activeTickets.Count == 0)
            {
                continue;
            }

            decimal activeSub = activeTickets.Sum(t => t.UnitPrice);

            foreach (OrderTicketDetailRow ticket in activeTickets)
            {
                // Phân bổ theo tổng UnitPrice các vé chưa hủy (activeSub) để giữ chính xác Orders.TotalAmount
                // khi đơn vừa có DiscountAmount vừa có vé hủy một phần (PartiallyRefunded).
                decimal allocated = activeSub > 0m
                    ? (ticket.UnitPrice * order.TotalAmount) / activeSub
                    : ticket.UnitPrice;

                if (byMovie.TryGetValue(ticket.MovieId, out var acc))
                {
                    byMovie[ticket.MovieId] = (acc.Title, acc.TicketCount + 1, acc.RawRevenue + allocated);
                }
                else
                {
                    byMovie[ticket.MovieId] = (ticket.MovieTitle, 1, allocated);
                }
            }
        }

        List<MovieRevenueRow> rows = byMovie
            .Select(kvp =>
            {
                decimal roundedRevenue = Math.Round(kvp.Value.RawRevenue, 0, MidpointRounding.AwayFromZero);
                double shareRatio = totalFilteredRevenue > 0m
                    ? Math.Clamp((double)(roundedRevenue / totalFilteredRevenue), 0.0, 1.0)
                    : 0.0;

                return new MovieRevenueRow(
                    MovieId: kvp.Key,
                    MovieTitle: kvp.Value.Title,
                    TicketCount: kvp.Value.TicketCount,
                    RevenueAmount: roundedRevenue,
                    RevenueText: FormatHelper.FormatVnd(roundedRevenue),
                    TicketCountText: $"{kvp.Value.TicketCount} vé",
                    ShareText: FormatPercent(shareRatio));
            })
            .OrderByDescending(r => r.RevenueAmount)
            .ThenByDescending(r => r.TicketCount)
            .ThenBy(r => r.MovieTitle, StringComparer.OrdinalIgnoreCase)
            .ToList();

        MovieRevenues.Clear();
        foreach (MovieRevenueRow row in rows)
        {
            MovieRevenues.Add(row);
        }

        OnPropertyChanged(nameof(HasNoMovieRevenues));
    }

    private void UpdateSelectedOrderTickets()
    {
        SelectedOrderTickets.Clear();
        if (_selectedOrder is not null &&
            _ticketsByOrder.TryGetValue(_selectedOrder.OrderId, out IReadOnlyList<OrderTicketDetailRow>? tickets))
        {
            foreach (OrderTicketDetailRow ticket in tickets)
            {
                SelectedOrderTickets.Add(ticket);
            }
        }

        OnPropertyChanged(nameof(HasNoSelectedOrderTickets));
    }

    private static bool MatchesDateRange(string createdAtSql, string rangeKey, DateTime today)
    {
        if (rangeKey == "All")
        {
            return true;
        }

        if (!TryParseSqlDate(createdAtSql, out DateTime parsed))
        {
            return false;
        }

        DateTime localDate = parsed.Date;
        return rangeKey switch
        {
            "Today" => localDate == today,
            "Last7Days" => localDate >= today.AddDays(-6),
            "Last30Days" => localDate >= today.AddDays(-29),
            _ => true
        };
    }

    private static bool MatchesKeyword(AdminOrderSummaryRow order, string keyword)
    {
        bool hasRealNote = !string.IsNullOrWhiteSpace(order.NoteText)
            && !string.Equals(order.NoteText, "Không có ghi chú", StringComparison.Ordinal);

        return order.OrderCode.Contains(keyword, StringComparison.OrdinalIgnoreCase)
            || order.CustomerName.Contains(keyword, StringComparison.OrdinalIgnoreCase)
            || order.CustomerPhone.Contains(keyword, StringComparison.OrdinalIgnoreCase)
            || order.StaffDisplay.Contains(keyword, StringComparison.OrdinalIgnoreCase)
            || (hasRealNote && order.NoteText.Contains(keyword, StringComparison.OrdinalIgnoreCase));
    }

    private static bool TryParseSqlDate(string value, out DateTime parsed)
    {
        if (DateTime.TryParseExact(
                value,
                SqlDateFormats,
                CultureInfo.InvariantCulture,
                DateTimeStyles.AssumeLocal,
                out parsed))
        {
            return true;
        }

        return DateTime.TryParse(
            value,
            CultureInfo.InvariantCulture,
            DateTimeStyles.AssumeLocal,
            out parsed);
    }

    private static string FormatPercent(double ratio)
    {
        double pct = Math.Round(ratio * 100.0, 1, MidpointRounding.AwayFromZero);
        return $"{pct.ToString("0.#", CultureInfo.InvariantCulture)}%";
    }
}
