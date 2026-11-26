using System.Collections.ObjectModel;
using CinemaManageProject.Data;

namespace CinemaManageProject.ViewModels.Staff;

/// <summary>
/// ViewModel màn hình Tra cứu &amp; Đăng ký Khách hàng hội viên tại quầy cho vai trò Staff.
/// </summary>
public sealed class StaffCustomersViewModel : ViewModelBase
{
    private readonly List<StaffCustomerRow> _allCustomers = [];
    private StaffCustomerRow? _selectedCustomer;
    private bool _isCreatingNew;
    private string _searchKeyword = string.Empty;
    private string _tierFilter = "All";
    private string _editFullName = string.Empty;
    private string _editPhone = string.Empty;
    private string _editEmail = string.Empty;
    private string _editDateOfBirth = string.Empty;
    private string _totalCustomersText = "0";
    private string _silverPlusText = "0";
    private string _appLinkedText = "0";
    private string _counterOnlyText = "0";
    private string? _errorMessage;
    private string? _successMessage;

    public StaffCustomersViewModel()
    {
        LoadData();
    }

    public ObservableCollection<StaffCustomerRow> FilteredCustomers { get; } = [];
    public ObservableCollection<CustomerOrderHistoryRow> OrderHistory { get; } = [];

    public string TotalCustomersText { get => _totalCustomersText; private set { _totalCustomersText = value; OnPropertyChanged(nameof(TotalCustomersText)); } }
    public string SilverPlusText { get => _silverPlusText; private set { _silverPlusText = value; OnPropertyChanged(nameof(SilverPlusText)); } }
    public string AppLinkedText { get => _appLinkedText; private set { _appLinkedText = value; OnPropertyChanged(nameof(AppLinkedText)); } }
    public string CounterOnlyText { get => _counterOnlyText; private set { _counterOnlyText = value; OnPropertyChanged(nameof(CounterOnlyText)); } }

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

    public string TierFilter
    {
        get => _tierFilter;
        private set
        {
            if (_tierFilter == value) return;
            _tierFilter = value;
            OnPropertyChanged(nameof(TierFilter));
            OnPropertyChanged(nameof(AllTierTag));
            OnPropertyChanged(nameof(MemberTierTag));
            OnPropertyChanged(nameof(SilverTierTag));
            OnPropertyChanged(nameof(GoldTierTag));
            OnPropertyChanged(nameof(PlatinumTierTag));
            ApplyFilter();
        }
    }

    public string? AllTierTag => TierFilter == "All" ? "Active" : null;
    public string? MemberTierTag => TierFilter == "Member" ? "Active" : null;
    public string? SilverTierTag => TierFilter == "Silver" ? "Active" : null;
    public string? GoldTierTag => TierFilter == "Gold" ? "Active" : null;
    public string? PlatinumTierTag => TierFilter == "Platinum" ? "Active" : null;

    public StaffCustomerRow? SelectedCustomer
    {
        get => _selectedCustomer;
        set
        {
            if (ReferenceEquals(_selectedCustomer, value)) return;
            _selectedCustomer = value;
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

            OnPropertyChanged(nameof(SelectedCustomer));
            OnPropertyChanged(nameof(IsEditingExisting));
            OnPropertyChanged(nameof(FormHeader));
            OnPropertyChanged(nameof(SaveButtonLabel));
        }
    }

    public bool IsEditingExisting => !_isCreatingNew && SelectedCustomer is not null;
    public string FormHeader => _isCreatingNew
        ? "Đăng ký Hội viên mới tại quầy"
        : SelectedCustomer is not null
            ? $"Hồ sơ Hội viên — {SelectedCustomer.FullName}"
            : "Đăng ký Hội viên mới tại quầy";

    public string SaveButtonLabel => IsEditingExisting ? "Cập nhật thông tin" : "Tạo hội viên mới";
    public bool HasNoOrderHistory => OrderHistory.Count == 0;

    public string EditFullName { get => _editFullName; set { _editFullName = value ?? string.Empty; OnPropertyChanged(nameof(EditFullName)); } }
    public string EditPhone { get => _editPhone; set { _editPhone = value ?? string.Empty; OnPropertyChanged(nameof(EditPhone)); } }
    public string EditEmail { get => _editEmail; set { _editEmail = value ?? string.Empty; OnPropertyChanged(nameof(EditEmail)); } }
    public string EditDateOfBirth { get => _editDateOfBirth; set { _editDateOfBirth = value ?? string.Empty; OnPropertyChanged(nameof(EditDateOfBirth)); } }

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

    public override void LoadData() => ReloadCustomers(SelectedCustomer?.CustomerId);

    public void SetTierFilter(string? tier) => TierFilter = tier ?? "All";

    public void StartNewCustomer()
    {
        ClearMessages();
        _isCreatingNew = true;
        _selectedCustomer = null;
        EditFullName = string.Empty;
        EditPhone = string.Empty;
        EditEmail = string.Empty;
        EditDateOfBirth = string.Empty;
        OrderHistory.Clear();

        OnPropertyChanged(nameof(SelectedCustomer));
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
            ? $"Đã cập nhật thông tin hội viên \"{EditFullName.Trim()}\"."
            : $"Đã đăng ký hội viên mới \"{EditFullName.Trim()}\" tại quầy (hạng Member).";

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
        string kw = SearchKeyword.Trim();

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

        _selectedCustomer = target;
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

        OnPropertyChanged(nameof(SelectedCustomer));
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
