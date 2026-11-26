using System.Collections.ObjectModel;
using CinemaManageProject.Data;
using CommunityToolkit.Mvvm.ComponentModel;

namespace CinemaManageProject.ViewModels.Staff;

/// <summary>
/// ViewModel màn hình Soát vé Check-in cho vai trò Staff.
/// </summary>
public sealed partial class StaffCheckInViewModel : ViewModelBase
{
    private readonly List<CheckInTicketRow> _allTickets = [];

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasCurrentTicket))]
    [NotifyPropertyChangedFor(nameof(CanConfirmCheckIn))]
    [NotifyPropertyChangedFor(nameof(IsCurrentValid))]
    [NotifyPropertyChangedFor(nameof(IsCurrentAlreadyCheckedIn))]
    [NotifyPropertyChangedFor(nameof(IsCurrentBlocked))]
    private CheckInTicketRow? _currentTicket;

    [ObservableProperty] private CheckInTicketRow? _selectedQueueTicket;
    [ObservableProperty] private string _ticketCodeInput = string.Empty;
    [ObservableProperty] private string _searchKeyword = string.Empty;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(BookedFilterTag))]
    [NotifyPropertyChangedFor(nameof(CheckedInFilterTag))]
    [NotifyPropertyChangedFor(nameof(CancelledFilterTag))]
    [NotifyPropertyChangedFor(nameof(AllFilterTag))]
    private string _statusFilter = "Booked";

    [ObservableProperty] private string _pendingCountText = "0 vé";
    [ObservableProperty] private string _checkedInTodayText = "0 vé";
    [ObservableProperty] private string _checkedInTotalText = "0 vé";
    [ObservableProperty] private string _cancelledCountText = "0 vé";

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasError))]
    private string? _errorMessage;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasSuccess))]
    private string? _successMessage;

    public StaffCheckInViewModel()
    {
        LoadData();
    }

    public ObservableCollection<CheckInTicketRow> FilteredTickets { get; } = [];

    public string? BookedFilterTag => StatusFilter == "Booked" ? "Active" : null;
    public string? CheckedInFilterTag => StatusFilter == "CheckedIn" ? "Active" : null;
    public string? CancelledFilterTag => StatusFilter == "Cancelled" ? "Active" : null;
    public string? AllFilterTag => StatusFilter == "All" ? "Active" : null;

    public bool HasCurrentTicket => CurrentTicket is not null;
    public bool CanConfirmCheckIn => CurrentTicket?.CanCheckIn == true;
    public bool IsCurrentValid => CurrentTicket?.ValidationState == "Valid";
    public bool IsCurrentAlreadyCheckedIn => CurrentTicket?.ValidationState == "AlreadyCheckedIn";
    public bool IsCurrentBlocked => CurrentTicket?.ValidationState == "Blocked";
    public bool HasNoFilteredTickets => FilteredTickets.Count == 0;
    public bool HasError => !string.IsNullOrWhiteSpace(ErrorMessage);
    public bool HasSuccess => !string.IsNullOrWhiteSpace(SuccessMessage);

    partial void OnSearchKeywordChanged(string value) => ApplyFilter();
    partial void OnStatusFilterChanged(string value) => ApplyFilter();

    partial void OnSelectedQueueTicketChanged(CheckInTicketRow? value)
    {
        if (value is not null)
        {
            ClearMessages();
            TicketCodeInput = value.TicketCode;
            CurrentTicket = value;
        }
    }

    public override void LoadData()
    {
        int? keepTicketId = CurrentTicket?.TicketId;

        _allTickets.Clear();
        _allTickets.AddRange(CheckInData.LoadAllTicketsForCheckIn());

        string todayPrefix = DateTime.Today.ToString("yyyy-MM-dd");
        int pendingCount = _allTickets.Count(t => t.IsPending);
        int checkedInTotal = _allTickets.Count(t => t.IsCheckedIn);
        int checkedInToday = _allTickets.Count(t =>
            t.IsCheckedIn && !string.IsNullOrWhiteSpace(t.CheckedInAtSql) && t.CheckedInAtSql.StartsWith(todayPrefix, StringComparison.Ordinal));
        int cancelledCount = _allTickets.Count(t => t.IsCancelled);

        PendingCountText = $"{pendingCount} vé";
        CheckedInTodayText = $"{checkedInToday} vé";
        CheckedInTotalText = $"{checkedInTotal} vé";
        CancelledCountText = $"{cancelledCount} vé";

        ApplyFilter();

        if (keepTicketId.HasValue)
        {
            CurrentTicket = _allTickets.FirstOrDefault(t => t.TicketId == keepTicketId.Value);
        }
    }

    public void SetStatusFilter(string? filter) => StatusFilter = filter ?? "Booked";

    public void LookupTicket()
    {
        ClearMessages();
        string code = (TicketCodeInput ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(code))
        {
            ErrorMessage = "Vui lòng nhập hoặc quét mã vé (ví dụ: VE-KHACH101).";
            return;
        }

        CheckInTicketRow? found = CheckInData.LookupTicketByCode(code);
        if (found is null)
        {
            CurrentTicket = null;
            ErrorMessage = $"Không tìm thấy vé nào khớp mã \"{code}\".";
            return;
        }

        CurrentTicket = found;
        TicketCodeInput = found.TicketCode;
        if (!found.CanCheckIn)
        {
            ErrorMessage = found.ValidationSummary;
        }
    }

    public void ConfirmCurrentTicketCheckIn()
    {
        ClearMessages();
        if (CurrentTicket is null)
        {
            ErrorMessage = "Vui lòng tra cứu hoặc chọn một vé cần soát.";
            return;
        }

        int staffId = App.CurrentUser?.UserId ?? 0;
        CheckInActionResult result = CheckInData.ConfirmCheckIn(CurrentTicket.TicketId, staffId);
        if (!result.Succeeded)
        {
            if (result.UpdatedTicket is not null)
            {
                CurrentTicket = result.UpdatedTicket;
            }
            ErrorMessage = result.ErrorMessage ?? "Không thể check-in vé này.";
            return;
        }

        CheckInTicketRow? updated = result.UpdatedTicket;
        LoadData();
        if (updated is not null)
        {
            CurrentTicket = updated;
        }

        SuccessMessage = $"Check-in thành công vé {CurrentTicket?.TicketCode} — {CurrentTicket?.MovieTitle} ({CurrentTicket?.RoomAndSeat}).";
    }

    public void ClearLookup()
    {
        ClearMessages();
        TicketCodeInput = string.Empty;
        CurrentTicket = null;
        SelectedQueueTicket = null;
    }

    private void ApplyFilter()
    {
        string kw = (SearchKeyword ?? string.Empty).Trim();
        FilteredTickets.Clear();

        IEnumerable<CheckInTicketRow> query = _allTickets;
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
                || t.CustomerName.Contains(kw, StringComparison.OrdinalIgnoreCase)
                || t.CustomerPhone.Contains(kw, StringComparison.OrdinalIgnoreCase));
        }

        foreach (CheckInTicketRow item in query)
        {
            FilteredTickets.Add(item);
        }

        OnPropertyChanged(nameof(HasNoFilteredTickets));
    }

    private void ClearMessages()
    {
        ErrorMessage = null;
        SuccessMessage = null;
    }
}
