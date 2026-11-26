using System.Collections.ObjectModel;
using CinemaManageProject.Data;
using CommunityToolkit.Mvvm.ComponentModel;

namespace CinemaManageProject.ViewModels.Staff;

/// <summary>
/// ViewModel màn hình Tra cứu &amp; Đăng ký Khách hàng hội viên tại quầy cho vai trò Staff.
/// </summary>
public sealed partial class StaffCustomersViewModel : ViewModelBase
{
    private readonly List<StaffCustomerRow> _allCustomers = [];
    private bool _isCreatingNew;
    private bool _suppressSelectionSideEffects;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsEditingExisting))]
    [NotifyPropertyChangedFor(nameof(FormHeader))]
    [NotifyPropertyChangedFor(nameof(SaveButtonLabel))]
    private StaffCustomerRow? _selectedCustomer;

    [ObservableProperty] private string _searchKeyword = string.Empty;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(AllTierTag))]
    [NotifyPropertyChangedFor(nameof(MemberTierTag))]
    [NotifyPropertyChangedFor(nameof(SilverTierTag))]
    [NotifyPropertyChangedFor(nameof(GoldTierTag))]
    [NotifyPropertyChangedFor(nameof(PlatinumTierTag))]
    private string _tierFilter = "All";

    [ObservableProperty] private string _editFullName = string.Empty;
    [ObservableProperty] private string _editPhone = string.Empty;
    [ObservableProperty] private string _editEmail = string.Empty;
    [ObservableProperty] private string _editDateOfBirth = string.Empty;
    [ObservableProperty] private string _totalCustomersText = "0";
    [ObservableProperty] private string _silverPlusText = "0";
    [ObservableProperty] private string _appLinkedText = "0";
    [ObservableProperty] private string _counterOnlyText = "0";

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasError))]
    private string? _errorMessage;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasSuccess))]
    private string? _successMessage;

    public StaffCustomersViewModel()
    {
        LoadData();
    }

    public ObservableCollection<StaffCustomerRow> FilteredCustomers { get; } = [];
    public ObservableCollection<CustomerOrderHistoryRow> OrderHistory { get; } = [];

    public string? AllTierTag => TierFilter == "All" ? "Active" : null;
    public string? MemberTierTag => TierFilter == "Member" ? "Active" : null;
    public string? SilverTierTag => TierFilter == "Silver" ? "Active" : null;
    public string? GoldTierTag => TierFilter == "Gold" ? "Active" : null;
    public string? PlatinumTierTag => TierFilter == "Platinum" ? "Active" : null;

    public bool IsEditingExisting => !_isCreatingNew && SelectedCustomer is not null;
    public string FormHeader => _isCreatingNew
        ? "Đăng ký Hội viên mới tại quầy"
        : SelectedCustomer is not null
            ? $"Hồ sơ Hội viên — {SelectedCustomer.FullName}"
            : "Đăng ký Hội viên mới tại quầy";

    public string SaveButtonLabel => IsEditingExisting ? "Cập nhật thông tin" : "Tạo hội viên mới";
    public bool HasNoOrderHistory => OrderHistory.Count == 0;
    public bool HasError => !string.IsNullOrWhiteSpace(ErrorMessage);
    public bool HasSuccess => !string.IsNullOrWhiteSpace(SuccessMessage);

    partial void OnSearchKeywordChanged(string value) => ApplyFilter();
    partial void OnTierFilterChanged(string value) => ApplyFilter();

    partial void OnSelectedCustomerChanged(StaffCustomerRow? value)
    {
        if (_suppressSelectionSideEffects)
        {
            return;
        }

        if (value is not null)
        {
            _isCreatingNew = false;
            PopulateForm(value);
            LoadHistory(value.CustomerId);
        }
        else if (!_isCreatingNew)
        {
            OrderHistory.Clear();
            OnPropertyChanged(nameof(HasNoOrderHistory));
        }

        OnPropertyChanged(nameof(IsEditingExisting));
        OnPropertyChanged(nameof(FormHeader));
        OnPropertyChanged(nameof(SaveButtonLabel));
    }

    public override void LoadData() => ReloadCustomers(SelectedCustomer?.CustomerId);
    public void SetTierFilter(string? tier) => TierFilter = tier ?? "All";

    public void StartNewCustomer()
    {
        ClearMessages();
        _isCreatingNew = true;
        SelectedCustomer = null;
        EditFullName = string.Empty;
        EditPhone = string.Empty;
        EditEmail = string.Empty;
        EditDateOfBirth = string.Empty;
        OrderHistory.Clear();

        OnPropertyChanged(nameof(IsEditingExisting));
        OnPropertyChanged(nameof(FormHeader));
        OnPropertyChanged(nameof(SaveButtonLabel));
        OnPropertyChanged(nameof(HasNoOrderHistory));
    }

    public void SaveCustomer()
    {
        ClearMessages();
        int? editingId = IsEditingExisting ? SelectedCustomer!.CustomerId : null;

        CounterCustomerSaveResult result = CustomerData.SaveCounterCustomer(new CounterCustomerSaveInput(
            editingId,
            EditFullName,
            EditPhone,
            EditEmail,
            EditDateOfBirth));

        if (!result.Succeeded)
        {
            ErrorMessage = result.ErrorMessage ?? "Không thể lưu thông tin khách hàng.";
            return;
        }

        string actionMsg = editingId.HasValue
            ? $"Đã cập nhật thông tin hội viên \"{(EditFullName ?? string.Empty).Trim()}\"."
            : $"Đã đăng ký hội viên mới \"{(EditFullName ?? string.Empty).Trim()}\" tại quầy (hạng Member).";

        _isCreatingNew = false;
        ReloadCustomers(result.CustomerId);
        SuccessMessage = actionMsg;
    }

    private void ReloadCustomers(int? selectCustomerId)
    {
        _allCustomers.Clear();
        _allCustomers.AddRange(CustomerData.LoadAllCustomers());

        TotalCustomersText = $"{_allCustomers.Count} hội viên";
        SilverPlusText = $"{_allCustomers.Count(c => c.Tier is "Silver" or "Gold" or "Platinum")} hội viên";
        AppLinkedText = $"{_allCustomers.Count(c => c.HasAppAccount)} tài khoản";
        CounterOnlyText = $"{_allCustomers.Count(c => !c.HasAppAccount)} hồ sơ";

        ApplyFilter(selectCustomerId);
    }

    private void ApplyFilter(int? preferredId = null)
    {
        int? keepId = preferredId ?? SelectedCustomer?.CustomerId;
        string kw = (SearchKeyword ?? string.Empty).Trim();

        FilteredCustomers.Clear();
        IEnumerable<StaffCustomerRow> query = _allCustomers;
        if (TierFilter != "All")
        {
            query = query.Where(c => c.Tier == TierFilter);
        }

        if (!string.IsNullOrWhiteSpace(kw))
        {
            query = query.Where(c =>
                c.FullName.Contains(kw, StringComparison.OrdinalIgnoreCase)
                || c.Phone.Contains(kw, StringComparison.OrdinalIgnoreCase)
                || c.Email.Contains(kw, StringComparison.OrdinalIgnoreCase)
                || (!string.IsNullOrWhiteSpace(c.Username) && c.Username.Contains(kw, StringComparison.OrdinalIgnoreCase)));
        }

        foreach (StaffCustomerRow row in query)
        {
            FilteredCustomers.Add(row);
        }

        if (_isCreatingNew)
        {
            return;
        }

        StaffCustomerRow? target = keepId.HasValue
            ? FilteredCustomers.FirstOrDefault(c => c.CustomerId == keepId.Value) ?? FilteredCustomers.FirstOrDefault()
            : FilteredCustomers.FirstOrDefault();

        _suppressSelectionSideEffects = true;
        SelectedCustomer = target;
        _suppressSelectionSideEffects = false;

        if (target is not null)
        {
            PopulateForm(target);
            LoadHistory(target.CustomerId);
        }
        else
        {
            OrderHistory.Clear();
            OnPropertyChanged(nameof(HasNoOrderHistory));
        }

        OnPropertyChanged(nameof(IsEditingExisting));
        OnPropertyChanged(nameof(FormHeader));
        OnPropertyChanged(nameof(SaveButtonLabel));
    }

    private void PopulateForm(StaffCustomerRow row)
    {
        EditFullName = row.FullName;
        EditPhone = row.Phone;
        EditEmail = row.Email;
        EditDateOfBirth = row.DateOfBirthSql ?? string.Empty;
    }

    private void LoadHistory(int customerId)
    {
        OrderHistory.Clear();
        foreach (CustomerOrderHistoryRow item in CustomerData.LoadCustomerOrderHistory(customerId))
        {
            OrderHistory.Add(item);
        }
        OnPropertyChanged(nameof(HasNoOrderHistory));
    }

    private void ClearMessages()
    {
        ErrorMessage = null;
        SuccessMessage = null;
    }
}
