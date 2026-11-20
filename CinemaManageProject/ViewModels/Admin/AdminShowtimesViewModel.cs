using System.Collections.ObjectModel;
using System.Globalization;
using CinemaManageProject.Data;
using CinemaManageProject.Helpers;

namespace CinemaManageProject.ViewModels.Admin;

public sealed record ShowtimeStatusOption(string Value, string Label)
{
    public override string ToString() => Label;
}

public sealed record ShowtimeDateFilterOption(string? DateSql, string Label)
{
    public override string ToString() => Label;
}

public sealed record ShowtimeRoomFilterOption(int? RoomId, string Name)
{
    public override string ToString() => Name;
}

/// <summary>
/// ViewModel Quản lý Lịch chiếu cho vai trò Admin.
/// </summary>
public sealed class AdminShowtimesViewModel : ViewModelBase
{
    private const int CleanupBufferMinutes = 15;
    private static readonly string[] DateFormats = ["dd/MM/yyyy", "d/M/yyyy", "yyyy-MM-dd"];
    private static readonly string[] TimeFormats = ["HH:mm", "H:mm", "HH:mm:ss", "H:mm:ss"];

    private readonly List<AdminShowtimeItem> _allShowtimes = [];
    private int? _editingShowtimeId;
    private int _selectedActiveTicketsCount;
    private bool _suppressAutoFormat;

    private string _searchKeyword = string.Empty;
    private ShowtimeDateFilterOption? _selectedDateFilter;
    private ShowtimeRoomFilterOption? _selectedRoomFilter;
    private ShowtimeStatusOption? _selectedStatusFilterOption;
    private string _resultCountText = "0 suất chiếu";
    private string _summaryStatsText = string.Empty;
    private AdminShowtimeItem? _selectedShowtime;

    private ShowtimeMovieOption? _selectedMovieOption;
    private ShowtimeRoomOption? _selectedRoomOption;
    private string _showDateText = DateTime.Today.ToString("dd/MM/yyyy", CultureInfo.InvariantCulture);
    private string _startTimeText = "14:00";
    private string _formatText = "2D Phụ đề";
    private ShowtimeStatusOption? _selectedStatusOption;

    private string _previewEndTimeText = "Chọn phim, ngày và giờ để xem giờ kết thúc (+15p dọn phòng).";
    private string _previewPriceSummaryText = string.Empty;
    private string? _errorMessage;
    private string? _successMessage;

    public AdminShowtimesViewModel()
    {
        StatusOptions =
        [
            new ShowtimeStatusOption("OpenForSale", "Đang mở bán"),
            new ShowtimeStatusOption("Scheduled", "Đã lên lịch"),
            new ShowtimeStatusOption("Completed", "Đã chiếu"),
            new ShowtimeStatusOption("Cancelled", "Đã hủy")
        ];
        StatusFilterOptions =
        [
            new ShowtimeStatusOption("All", "Tất cả"),
            new ShowtimeStatusOption("OpenForSale", "Đang mở bán"),
            new ShowtimeStatusOption("Scheduled", "Đã lên lịch"),
            new ShowtimeStatusOption("Completed", "Đã chiếu"),
            new ShowtimeStatusOption("Cancelled", "Đã hủy")
        ];

        _selectedStatusOption = StatusOptions[0];
        _selectedStatusFilterOption = StatusFilterOptions[0];
        LoadData();
    }

    public IReadOnlyList<ShowtimeStatusOption> StatusOptions { get; }
    public IReadOnlyList<ShowtimeStatusOption> StatusFilterOptions { get; }

    public ObservableCollection<AdminShowtimeItem> Showtimes { get; } = [];
    public ObservableCollection<ShowtimeMovieOption> MovieOptions { get; } = [];
    public ObservableCollection<ShowtimeRoomOption> RoomOptions { get; } = [];
    public ObservableCollection<ShowtimeDateFilterOption> DateFilterOptions { get; } = [];
    public ObservableCollection<ShowtimeRoomFilterOption> RoomFilterOptions { get; } = [];

    public string SearchKeyword
    {
        get => _searchKeyword;
        set { if (_searchKeyword == value) return; _searchKeyword = value; OnPropertyChanged(nameof(SearchKeyword)); ApplyFilter(); }
    }

    public ShowtimeDateFilterOption? SelectedDateFilter
    {
        get => _selectedDateFilter;
        set { if (Equals(_selectedDateFilter, value)) return; _selectedDateFilter = value; OnPropertyChanged(nameof(SelectedDateFilter)); ApplyFilter(); }
    }

    public ShowtimeRoomFilterOption? SelectedRoomFilter
    {
        get => _selectedRoomFilter;
        set { if (Equals(_selectedRoomFilter, value)) return; _selectedRoomFilter = value; OnPropertyChanged(nameof(SelectedRoomFilter)); ApplyFilter(); }
    }

    public ShowtimeStatusOption? SelectedStatusFilterOption
    {
        get => _selectedStatusFilterOption;
        set { if (Equals(_selectedStatusFilterOption, value)) return; _selectedStatusFilterOption = value; OnPropertyChanged(nameof(SelectedStatusFilterOption)); ApplyFilter(); }
    }

    public AdminShowtimeItem? SelectedShowtime
    {
        get => _selectedShowtime;
        set
        {
            _selectedShowtime = value;
            OnPropertyChanged(nameof(SelectedShowtime));
            if (value is not null)
            {
                PopulateForm(value);
            }
        }
    }

    public string ResultCountText { get => _resultCountText; private set { _resultCountText = value; OnPropertyChanged(nameof(ResultCountText)); } }
    public string SummaryStatsText { get => _summaryStatsText; private set { _summaryStatsText = value; OnPropertyChanged(nameof(SummaryStatsText)); } }
    public bool IsEditMode => _editingShowtimeId.HasValue;
    public bool SelectedHasActiveTickets => _editingShowtimeId.HasValue && _selectedActiveTicketsCount > 0;
    public bool CanEditScheduleCore => !SelectedHasActiveTickets;
    public string FormHeaderText => IsEditMode ? "Chỉnh sửa suất chiếu" : "Thêm suất chiếu mới";
    public string FormModeBadge => IsEditMode ? $"SUẤT #{_editingShowtimeId}" : "THÊM MỚI";
    public string ActiveTicketsNoticeText => SelectedHasActiveTickets
        ? $"Suất chiếu đã có {_selectedActiveTicketsCount} vé hiệu lực — chỉ được cập nhật trạng thái (khóa đổi phim, phòng và giờ chiếu)."
        : string.Empty;
    public string StandardBasePriceDisplay => FormatHelper.FormatVnd(CurrentBasePrice());

    public ShowtimeMovieOption? SelectedMovieOption
    {
        get => _selectedMovieOption;
        set
        {
            _selectedMovieOption = value;
            OnPropertyChanged(nameof(SelectedMovieOption));
            UpdateSuggestedFormatIfNew();
            UpdatePreviews();
        }
    }

    public ShowtimeRoomOption? SelectedRoomOption
    {
        get => _selectedRoomOption;
        set
        {
            _selectedRoomOption = value;
            OnPropertyChanged(nameof(SelectedRoomOption));
            UpdateSuggestedFormatIfNew();
            UpdatePreviews();
        }
    }

    public string ShowDateText
    {
        get => _showDateText;
        set { _showDateText = value; OnPropertyChanged(nameof(ShowDateText)); UpdatePreviews(); }
    }

    public string StartTimeText
    {
        get => _startTimeText;
        set { _startTimeText = value; OnPropertyChanged(nameof(StartTimeText)); UpdatePreviews(); }
    }

    public string FormatText
    {
        get => _formatText;
        set { _formatText = value; OnPropertyChanged(nameof(FormatText)); }
    }

    public ShowtimeStatusOption? SelectedStatusOption
    {
        get => _selectedStatusOption;
        set { _selectedStatusOption = value; OnPropertyChanged(nameof(SelectedStatusOption)); }
    }

    public string PreviewEndTimeText
    {
        get => _previewEndTimeText;
        private set { _previewEndTimeText = value; OnPropertyChanged(nameof(PreviewEndTimeText)); }
    }

    public string PreviewPriceSummaryText
    {
        get => _previewPriceSummaryText;
        private set { _previewPriceSummaryText = value; OnPropertyChanged(nameof(PreviewPriceSummaryText)); }
    }

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
        int? editingId = _editingShowtimeId;

        RefreshMovieAndRoomOptions();
        _allShowtimes.Clear();
        _allShowtimes.AddRange(ShowtimeData.LoadAllShowtimes());
        RefreshDateFilters();
        ApplyFilter();

        if (editingId.HasValue && _allShowtimes.FirstOrDefault(s => s.ShowtimeId == editingId.Value) is { } matched)
        {
            SelectedShowtime = matched;
            return;
        }

        if (!IsEditMode)
        {
            ResetFormFields();
        }
    }

    public void ResetFilters()
    {
        _searchKeyword = string.Empty;
        _selectedDateFilter = DateFilterOptions.FirstOrDefault();
        _selectedRoomFilter = RoomFilterOptions.FirstOrDefault();
        _selectedStatusFilterOption = StatusFilterOptions[0];

        OnPropertyChanged(nameof(SearchKeyword));
        OnPropertyChanged(nameof(SelectedDateFilter));
        OnPropertyChanged(nameof(SelectedRoomFilter));
        OnPropertyChanged(nameof(SelectedStatusFilterOption));
        ApplyFilter();
    }

    public void StartNewShowtime()
    {
        ClearMessages();
        SelectedShowtime = null;
        ResetFormFields();
    }

    public void ResetForm() => StartNewShowtime();

    public void SaveShowtime()
    {
        ClearMessages();
        if (!TryBuildSaveInput(out ShowtimeSaveInput? input, out string? movieTitle))
        {
            return;
        }

        bool wasEdit = _editingShowtimeId.HasValue;
        ShowtimeSaveResult result = ShowtimeData.SaveShowtime(_editingShowtimeId, input!);
        if (!result.Succeeded)
        {
            ErrorMessage = result.ErrorMessage ?? "Không thể lưu suất chiếu.";
            return;
        }

        _editingShowtimeId = result.ShowtimeId;
        LoadData();
        SuccessMessage = wasEdit
            ? $"Đã cập nhật suất chiếu #{result.ShowtimeId} ({movieTitle})."
            : $"Đã thêm suất chiếu mới \"{movieTitle}\".";
    }

    public bool CanDeleteSelectedShowtime(out string summary)
    {
        ClearMessages();
        if (!_editingShowtimeId.HasValue || SelectedShowtime is null)
        {
            summary = string.Empty;
            ErrorMessage = "Vui lòng chọn một suất chiếu trong danh sách để xóa.";
            return false;
        }

        summary = $"{SelectedShowtime.MovieTitle} ({SelectedShowtime.RoomName} • {SelectedShowtime.TimeRangeDisplay} ngày {SelectedShowtime.ShowDateDisplay})";
        int tickets = ShowtimeData.CountShowtimeTickets(_editingShowtimeId.Value);
        if (tickets > 0)
        {
            ErrorMessage = $"Suất chiếu này đã phát sinh {tickets} vé nên không thể xóa. Hãy đổi trạng thái sang Đã hủy (Cancelled).";
            return false;
        }

        return true;
    }

    public void DeleteSelectedShowtime()
    {
        if (!_editingShowtimeId.HasValue)
        {
            return;
        }

        int deletedId = _editingShowtimeId.Value;
        ShowtimeData.DeleteShowtime(deletedId);

        SelectedShowtime = null;
        ResetFormFields();
        LoadData();
        SuccessMessage = $"Đã xóa suất chiếu #{deletedId}.";
    }

    private void RefreshMovieAndRoomOptions()
    {
        int? selectedMovieId = _selectedMovieOption?.MovieId;
        int? selectedRoomId = _selectedRoomOption?.RoomId;
        int? roomFilterId = _selectedRoomFilter?.RoomId;

        MovieOptions.Clear();
        foreach (ShowtimeMovieOption m in ShowtimeData.LoadMovieOptions())
        {
            MovieOptions.Add(m);
        }

        RoomOptions.Clear();
        RoomFilterOptions.Clear();
        RoomFilterOptions.Add(new ShowtimeRoomFilterOption(null, "Tất cả phòng"));
        foreach (ShowtimeRoomOption r in ShowtimeData.LoadRoomOptions())
        {
            RoomOptions.Add(r);
            RoomFilterOptions.Add(new ShowtimeRoomFilterOption(r.RoomId, r.Name));
        }

        _suppressAutoFormat = true;
        _selectedMovieOption = selectedMovieId.HasValue
            ? MovieOptions.FirstOrDefault(m => m.MovieId == selectedMovieId.Value) ?? FirstSchedulableMovie()
            : FirstSchedulableMovie();
        _selectedRoomOption = selectedRoomId.HasValue
            ? RoomOptions.FirstOrDefault(r => r.RoomId == selectedRoomId.Value) ?? FirstActiveRoom()
            : FirstActiveRoom();
        _suppressAutoFormat = false;

        _selectedRoomFilter = roomFilterId.HasValue
            ? RoomFilterOptions.FirstOrDefault(r => r.RoomId == roomFilterId.Value) ?? RoomFilterOptions[0]
            : RoomFilterOptions[0];

        OnPropertyChanged(nameof(SelectedMovieOption));
        OnPropertyChanged(nameof(SelectedRoomOption));
        OnPropertyChanged(nameof(SelectedRoomFilter));
    }

    private void RefreshDateFilters()
    {
        string? activeDateSql = _selectedDateFilter?.DateSql;
        DateFilterOptions.Clear();
        DateFilterOptions.Add(new ShowtimeDateFilterOption(null, "Tất cả ngày"));

        string todaySql = DateTime.Today.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
        string tomorrowSql = DateTime.Today.AddDays(1).ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);

        foreach (string dateSql in _allShowtimes.Select(s => s.ShowDateSql).Distinct().OrderBy(d => d, StringComparer.Ordinal))
        {
            string display = DateOnly.TryParseExact(dateSql, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out DateOnly parsed)
                ? parsed.ToString("dd/MM/yyyy", CultureInfo.InvariantCulture)
                : dateSql;

            string label = dateSql == todaySql
                ? $"Hôm nay ({display})"
                : dateSql == tomorrowSql
                    ? $"Ngày mai ({display})"
                    : display;

            DateFilterOptions.Add(new ShowtimeDateFilterOption(dateSql, label));
        }

        _selectedDateFilter = !string.IsNullOrEmpty(activeDateSql)
            ? DateFilterOptions.FirstOrDefault(d => d.DateSql == activeDateSql) ?? DateFilterOptions[0]
            : DateFilterOptions[0];
        OnPropertyChanged(nameof(SelectedDateFilter));
    }

    private void ApplyFilter()
    {
        Showtimes.Clear();
        string keyword = SearchKeyword.Trim();
        string? dateSql = SelectedDateFilter?.DateSql;
        int? roomId = SelectedRoomFilter?.RoomId;
        string status = SelectedStatusFilterOption?.Value ?? "All";

        foreach (AdminShowtimeItem item in _allShowtimes)
        {
            if (!string.IsNullOrEmpty(dateSql) && item.ShowDateSql != dateSql) continue;
            if (roomId.HasValue && item.RoomId != roomId.Value) continue;
            if (status != "All" && item.Status != status) continue;

            if (!string.IsNullOrEmpty(keyword)
                && !item.MovieTitle.Contains(keyword, StringComparison.OrdinalIgnoreCase)
                && !item.RoomName.Contains(keyword, StringComparison.OrdinalIgnoreCase)
                && !item.Format.Contains(keyword, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            Showtimes.Add(item);
        }

        ResultCountText = Showtimes.Count == _allShowtimes.Count
            ? $"{Showtimes.Count} suất chiếu"
            : $"{Showtimes.Count} / {_allShowtimes.Count} suất chiếu";

        int openCount = _allShowtimes.Count(s => s.Status is "OpenForSale" or "Scheduled");
        int completedCount = _allShowtimes.Count(s => s.Status == "Completed");
        int upcomingTickets = _allShowtimes.Where(s => s.Status is "OpenForSale" or "Scheduled").Sum(s => s.ActiveTicketsCount);
        SummaryStatsText = $"{openCount} sắp/mở bán • {completedCount} đã chiếu • {upcomingTickets} vé đặt trước";
    }

    private void PopulateForm(AdminShowtimeItem item)
    {
        ClearMessages();
        _editingShowtimeId = item.ShowtimeId;
        _selectedActiveTicketsCount = item.ActiveTicketsCount;

        _suppressAutoFormat = true;
        _selectedMovieOption = MovieOptions.FirstOrDefault(m => m.MovieId == item.MovieId);
        _selectedRoomOption = RoomOptions.FirstOrDefault(r => r.RoomId == item.RoomId);
        OnPropertyChanged(nameof(SelectedMovieOption));
        OnPropertyChanged(nameof(SelectedRoomOption));

        if (DateTime.TryParseExact(item.StartTimeSql, "yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture, DateTimeStyles.AssumeLocal, out DateTime startDt))
        {
            ShowDateText = startDt.ToString("dd/MM/yyyy", CultureInfo.InvariantCulture);
            StartTimeText = startDt.ToString("HH:mm", CultureInfo.InvariantCulture);
        }

        FormatText = item.Format;
        SelectedStatusOption = StatusOptions.FirstOrDefault(s => s.Value == item.Status) ?? StatusOptions[0];
        _suppressAutoFormat = false;

        NotifyModeChanged();
        UpdatePreviews();
    }

    private void ResetFormFields()
    {
        _editingShowtimeId = null;
        _selectedActiveTicketsCount = 0;

        _suppressAutoFormat = true;
        _selectedMovieOption = FirstSchedulableMovie();
        _selectedRoomOption = FirstActiveRoom();
        OnPropertyChanged(nameof(SelectedMovieOption));
        OnPropertyChanged(nameof(SelectedRoomOption));

        ShowDateText = DateTime.Today.ToString("dd/MM/yyyy", CultureInfo.InvariantCulture);
        StartTimeText = "14:00";
        SelectedStatusOption = StatusOptions[0];
        _suppressAutoFormat = false;

        UpdateSuggestedFormatIfNew();
        NotifyModeChanged();
        UpdatePreviews();
    }

    private bool TryBuildSaveInput(out ShowtimeSaveInput? input, out string? movieTitle)
    {
        input = null;
        movieTitle = SelectedMovieOption?.Title;

        if (SelectedMovieOption is null) { ErrorMessage = "Vui lòng chọn phim chiếu."; return false; }
        if (SelectedRoomOption is null) { ErrorMessage = "Vui lòng chọn phòng chiếu."; return false; }

        if (!IsEditMode && SelectedMovieOption.Status == "Ended")
        {
            ErrorMessage = $"Phim \"{SelectedMovieOption.Title}\" đã ngừng chiếu, không thể tạo suất mới.";
            return false;
        }

        if (!IsEditMode && !SelectedRoomOption.IsActive)
        {
            ErrorMessage = $"Phòng \"{SelectedRoomOption.Name}\" đang tạm đóng, không thể tạo suất mới.";
            return false;
        }

        if (!DateOnly.TryParseExact(ShowDateText.Trim(), DateFormats, CultureInfo.InvariantCulture, DateTimeStyles.None, out DateOnly showDate))
        {
            ErrorMessage = "Ngày chiếu không hợp lệ (hãy nhập dạng dd/MM/yyyy hoặc yyyy-MM-dd).";
            return false;
        }

        if (!TimeOnly.TryParseExact(StartTimeText.Trim(), TimeFormats, CultureInfo.InvariantCulture, DateTimeStyles.None, out TimeOnly startTime))
        {
            ErrorMessage = "Giờ bắt đầu không hợp lệ (hãy nhập dạng HH:mm, ví dụ 14:30).";
            return false;
        }

        string format = FormatText.Trim();
        if (string.IsNullOrWhiteSpace(format) || format.Length > 50)
        {
            ErrorMessage = "Định dạng chiếu không được để trống và tối đa 50 ký tự.";
            return false;
        }

        DateTime startDt = showDate.ToDateTime(startTime);
        DateTime endDt = startDt.AddMinutes(SelectedMovieOption.DurationMinutes + CleanupBufferMinutes);

        input = new ShowtimeSaveInput(
            SelectedMovieOption.MovieId,
            SelectedRoomOption.RoomId,
            startDt.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture),
            endDt.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture),
            CurrentBasePrice(),
            format,
            SelectedStatusOption?.Value ?? "OpenForSale",
            App.CurrentUser?.UserId ?? 1);
        return true;
    }

    private void UpdateSuggestedFormatIfNew()
    {
        if (_suppressAutoFormat || IsEditMode || SelectedMovieOption is null || SelectedRoomOption is null)
        {
            return;
        }

        FormatText = ShowtimeData.SuggestFormat(SelectedRoomOption.RoomType, SelectedMovieOption.Language);
    }

    private void UpdatePreviews()
    {
        if (SelectedMovieOption is not null
            && DateOnly.TryParseExact(ShowDateText.Trim(), DateFormats, CultureInfo.InvariantCulture, DateTimeStyles.None, out DateOnly d)
            && TimeOnly.TryParseExact(StartTimeText.Trim(), TimeFormats, CultureInfo.InvariantCulture, DateTimeStyles.None, out TimeOnly t))
        {
            DateTime startDt = d.ToDateTime(t);
            DateTime endDt = startDt.AddMinutes(SelectedMovieOption.DurationMinutes + CleanupBufferMinutes);
            PreviewEndTimeText = $"{endDt:HH:mm dd/MM/yyyy} ({SelectedMovieOption.DurationMinutes}p phim + {CleanupBufferMinutes}p dọn phòng)";
        }
        else
        {
            PreviewEndTimeText = "Nhập đúng ngày (dd/MM/yyyy) và giờ (HH:mm) để tính giờ kết thúc (+15p dọn phòng).";
        }

        decimal basePrice = CurrentBasePrice();
        if (SelectedRoomOption is not null)
        {
            decimal stdPrice = (basePrice + SelectedRoomOption.BasePriceSurcharge) * 1.00m;
            decimal vipPrice = (basePrice + SelectedRoomOption.BasePriceSurcharge) * 1.25m;
            decimal couplePrice = (basePrice + SelectedRoomOption.BasePriceSurcharge) * 2.00m;
            string surchargeLabel = SelectedRoomOption.BasePriceSurcharge > 0
                ? $"+{FormatHelper.FormatVnd(SelectedRoomOption.BasePriceSurcharge)}"
                : "0đ";

            PreviewPriceSummaryText = $"Theo loại phòng ({SelectedRoomOption.RoomTypeDisplay}, phụ thu {surchargeLabel}) • Ghế Thường: {FormatHelper.FormatVnd(stdPrice)} • VIP: {FormatHelper.FormatVnd(vipPrice)} • Đôi: {FormatHelper.FormatVnd(couplePrice)}";
        }
        else
        {
            PreviewPriceSummaryText = "Chọn phòng chiếu để xem bảng giá vé theo hạng ghế.";
        }
    }

    private decimal CurrentBasePrice()
        => IsEditMode && SelectedShowtime is not null
            ? SelectedShowtime.BaseTicketPrice
            : ShowtimeData.StandardBaseTicketPrice;

    private ShowtimeMovieOption? FirstSchedulableMovie()
        => MovieOptions.FirstOrDefault(m => m.Status != "Ended") ?? MovieOptions.FirstOrDefault();

    private ShowtimeRoomOption? FirstActiveRoom()
        => RoomOptions.FirstOrDefault(r => r.IsActive) ?? RoomOptions.FirstOrDefault();

    private void NotifyModeChanged()
    {
        OnPropertyChanged(nameof(IsEditMode));
        OnPropertyChanged(nameof(SelectedHasActiveTickets));
        OnPropertyChanged(nameof(CanEditScheduleCore));
        OnPropertyChanged(nameof(FormHeaderText));
        OnPropertyChanged(nameof(FormModeBadge));
        OnPropertyChanged(nameof(ActiveTicketsNoticeText));
        OnPropertyChanged(nameof(StandardBasePriceDisplay));
    }

    private void ClearMessages() { ErrorMessage = null; SuccessMessage = null; }
}
