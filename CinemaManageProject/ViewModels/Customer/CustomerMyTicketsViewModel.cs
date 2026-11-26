using System.Collections.ObjectModel;
using CinemaManageProject.Data;
using CinemaManageProject.Helpers;
using CinemaManageProject.Models;
using CommunityToolkit.Mvvm.ComponentModel;

namespace CinemaManageProject.ViewModels.Customer;

/// <summary>
/// ViewModel màn hình Vé của tôi &amp; Lịch sử hóa đơn cho vai trò Customer.
/// </summary>
public sealed partial class CustomerMyTicketsViewModel : ViewModelBase
{
    private readonly List<CustomerMyTicketRow> _allTickets = [];
    private BookingCustomerSummary? _memberProfile;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasSelectedTicket))]
    [NotifyPropertyChangedFor(nameof(CanCancelSelectedTicket))]
    private CustomerMyTicketRow? _selectedTicket;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(AllFilterTag))]
    [NotifyPropertyChangedFor(nameof(BookedFilterTag))]
    [NotifyPropertyChangedFor(nameof(CheckedInFilterTag))]
    [NotifyPropertyChangedFor(nameof(CancelledFilterTag))]
    private string _statusFilter = "All";

    [ObservableProperty]
    private string _searchKeyword = string.Empty;

    [ObservableProperty]
    private string _pendingCountText = "0 vé";

    [ObservableProperty]
    private string _checkedInCountText = "0 vé";

    [ObservableProperty]
    private string _ordersCountText = "0 hóa đơn";

    [ObservableProperty]
    private string _tierAndPointsText = "Member • 0 điểm";

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasError))]
    private string? _errorMessage;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasSuccess))]
    private string? _successMessage;

    public CustomerMyTicketsViewModel()
    {
        LoadData();
    }

    public ObservableCollection<CustomerMyTicketRow> FilteredTickets { get; } = [];
    public ObservableCollection<CustomerMyOrderRow> MyOrders { get; } = [];

    public string? AllFilterTag => StatusFilter == "All" ? "Active" : null;
    public string? BookedFilterTag => StatusFilter == "Booked" ? "Active" : null;
    public string? CheckedInFilterTag => StatusFilter == "CheckedIn" ? "Active" : null;
    public string? CancelledFilterTag => StatusFilter == "Cancelled" ? "Active" : null;

    public bool HasSelectedTicket => SelectedTicket is not null;
    public bool CanCancelSelectedTicket => SelectedTicket?.CanCancel == true;
    public bool HasNoFilteredTickets => FilteredTickets.Count == 0;
    public bool HasNoOrders => MyOrders.Count == 0;
    public bool HasError => !string.IsNullOrWhiteSpace(ErrorMessage);
    public bool HasSuccess => !string.IsNullOrWhiteSpace(SuccessMessage);

    partial void OnStatusFilterChanged(string value) => ApplyTicketFilter();
    partial void OnSearchKeywordChanged(string value) => ApplyTicketFilter();

    public override void LoadData()
    {
        int? keepTicketId = SelectedTicket?.TicketId;
        RefreshMemberProfile();

        _allTickets.Clear();
        MyOrders.Clear();

        if (_memberProfile is not null)
        {
            _allTickets.AddRange(BookingData.LoadMyTickets(_memberProfile.CustomerId));
            foreach (CustomerMyOrderRow order in BookingData.LoadMyOrders(_memberProfile.CustomerId))
            {
                MyOrders.Add(order);
            }
        }

        PendingCountText = $"{_allTickets.Count(t => t.IsPending)} vé";
        CheckedInCountText = $"{_allTickets.Count(t => t.IsCheckedIn)} vé";
        OrdersCountText = $"{MyOrders.Count} hóa đơn";
        TierAndPointsText = _memberProfile is null
            ? "Chưa có hồ sơ"
            : $"{_memberProfile.Tier} • {_memberProfile.LoyaltyPointsText}";

        OnPropertyChanged(nameof(HasNoOrders));
        ApplyTicketFilter(keepTicketId);
    }

    public void SetStatusFilter(string? filter) => StatusFilter = filter ?? "All";

    public void CancelSelectedTicket()
    {
        ClearMessages();
        if (_memberProfile is null || SelectedTicket is null)
        {
            ErrorMessage = "Vui lòng chọn một vé hợp lệ để hủy.";
            return;
        }

        if (!SelectedTicket.CanCancel)
        {
            ErrorMessage = "Vé này không thể hủy (đã check-in, đã hủy hoặc suất chiếu đã kết thúc).";
            return;
        }

        string ticketCode = SelectedTicket.TicketCode;
        int ticketId = SelectedTicket.TicketId;

        CancelTicketResult result = BookingData.CancelCustomerTicket(ticketId, _memberProfile.CustomerId);
        if (!result.Succeeded)
        {
            ErrorMessage = result.ErrorMessage ?? "Không thể hủy vé.";
            return;
        }

        LoadData();
        SuccessMessage =
            $"Đã hủy vé {ticketCode} • Hoàn điều chỉnh {FormatHelper.FormatVnd(result.RefundedAmount)} • Điểm hiện tại: {result.UpdatedPoints} điểm ({result.UpdatedTier}).";
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
    }

    private void ApplyTicketFilter(int? preferredTicketId = null)
    {
        int? keepId = preferredTicketId ?? SelectedTicket?.TicketId;
        string kw = (SearchKeyword ?? string.Empty).Trim();

        FilteredTickets.Clear();
        IEnumerable<CustomerMyTicketRow> query = _allTickets;

        if (StatusFilter != "All")
        {
            query = query.Where(t => t.TicketStatus == StatusFilter);
        }

        if (!string.IsNullOrWhiteSpace(kw))
        {
            query = query.Where(t =>
                t.TicketCode.Contains(kw, StringComparison.OrdinalIgnoreCase)
                || t.OrderCode.Contains(kw, StringComparison.OrdinalIgnoreCase)
                || t.MovieTitle.Contains(kw, StringComparison.OrdinalIgnoreCase)
                || t.SeatCode.Contains(kw, StringComparison.OrdinalIgnoreCase)
                || t.RoomName.Contains(kw, StringComparison.OrdinalIgnoreCase));
        }

        foreach (CustomerMyTicketRow item in query)
        {
            FilteredTickets.Add(item);
        }

        OnPropertyChanged(nameof(HasNoFilteredTickets));

        SelectedTicket = keepId.HasValue
            ? FilteredTickets.FirstOrDefault(t => t.TicketId == keepId.Value) ?? FilteredTickets.FirstOrDefault()
            : FilteredTickets.FirstOrDefault();
    }

    private void ClearMessages()
    {
        ErrorMessage = null;
        SuccessMessage = null;
    }
}
