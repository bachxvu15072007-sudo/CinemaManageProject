using System.Collections.ObjectModel;
using CinemaManageProject.Data;

namespace CinemaManageProject.ViewModels.Staff;

/// <summary>
/// ViewModel màn hình Soát vé Check-in cho vai trò Staff.
/// </summary>
public sealed class StaffCheckInViewModel : ViewModelBase
{
    private readonly List<CheckInTicketRow> _allTickets = [];
    private CheckInTicketRow? _currentTicket;
    private CheckInTicketRow? _selectedQueueTicket;
    private string _ticketCodeInput = string.Empty;
    private string _searchKeyword = string.Empty;
    private string _statusFilter = "Booked";
    private string _pendingCountText = "0 vé";
    private string _checkedInTodayText = "0 vé";
    private string _checkedInTotalText = "0 vé";
    private string _cancelledCountText = "0 vé";
    private string? _errorMessage;
    private string? _successMessage;

    public StaffCheckInViewModel()
    {
        LoadData();
    }

    public ObservableCollection<CheckInTicketRow> FilteredTickets { get; } = [];

    public string PendingCountText { get => _pendingCountText; private set { _pendingCountText = value; OnPropertyChanged(nameof(PendingCountText)); } }
    public string CheckedInTodayText { get => _checkedInTodayText; private set { _checkedInTodayText = value; OnPropertyChanged(nameof(CheckedInTodayText)); } }
    public string CheckedInTotalText { get => _checkedInTotalText; private set { _checkedInTotalText = value; OnPropertyChanged(nameof(CheckedInTotalText)); } }
    public string CancelledCountText { get => _cancelledCountText; private set { _cancelledCountText = value; OnPropertyChanged(nameof(CancelledCountText)); } }

    public string TicketCodeInput
    {
        get => _ticketCodeInput;
        set { _ticketCodeInput = value ?? string.Empty; OnPropertyChanged(nameof(TicketCodeInput)); }
    }

    public string SearchKeyword
    {
        get => _searchKeyword;
        set
        {
            if (_searchKeyword == value) return;
            _searchKeyword = value ?? string.Empty;
            OnPropertyChanged(nameof(SearchKeyword));
            ApplyFilter();
        }
    }

    public string StatusFilter
    {
        get => _statusFilter;
        private set
        {
            if (_statusFilter == value) return;
            _statusFilter = value;
            OnPropertyChanged(nameof(StatusFilter));
            OnPropertyChanged(nameof(BookedFilterTag));
            OnPropertyChanged(nameof(CheckedInFilterTag));
            OnPropertyChanged(nameof(CancelledFilterTag));
            OnPropertyChanged(nameof(AllFilterTag));
            ApplyFilter();
        }
    }

    public string? BookedFilterTag => StatusFilter == "Booked" ? "Active" : null;
    public string? CheckedInFilterTag => StatusFilter == "CheckedIn" ? "Active" : null;
    public string? CancelledFilterTag => StatusFilter == "Cancelled" ? "Active" : null;
    public string? AllFilterTag => StatusFilter == "All" ? "Active" : null;

    public CheckInTicketRow? CurrentTicket
    {
        get => _currentTicket;
        private set
        {
            _currentTicket = value;
            OnPropertyChanged(nameof(CurrentTicket));
            OnPropertyChanged(nameof(HasCurrentTicket));
            OnPropertyChanged(nameof(CanConfirmCheckIn));
            OnPropertyChanged(nameof(IsCurrentValid));
            OnPropertyChanged(nameof(IsCurrentAlreadyCheckedIn));
            OnPropertyChanged(nameof(IsCurrentBlocked));
        }
    }

    public CheckInTicketRow? SelectedQueueTicket
    {
        get => _selectedQueueTicket;
        set
        {
            _selectedQueueTicket = value;
            OnPropertyChanged(nameof(SelectedQueueTicket));
            if (value is not null)
            {
                ClearMessages();
                TicketCodeInput = value.TicketCode;
                CurrentTicket = value;
            }
        }
    }

    public bool HasCurrentTicket => CurrentTicket is not null;
    public bool CanConfirmCheckIn => CurrentTicket?.CanCheckIn == true;
    public bool IsCurrentValid => CurrentTicket?.ValidationState == "Valid";
    public bool IsCurrentAlreadyCheckedIn => CurrentTicket?.ValidationState == "AlreadyCheckedIn";
    public bool IsCurrentBlocked => CurrentTicket?.ValidationState == "Blocked";
    public bool HasNoFilteredTickets => FilteredTickets.Count == 0;

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
        string code = TicketCodeInput.Trim();
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
        _selectedQueueTicket = null;
        OnPropertyChanged(nameof(SelectedQueueTicket));
    }

    private void ApplyFilter()
    {
        string kw = SearchKeyword.Trim();
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
