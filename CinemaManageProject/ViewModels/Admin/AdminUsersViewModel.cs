using System.Collections.ObjectModel;
using System.Globalization;
using CinemaManageProject.Data;

namespace CinemaManageProject.ViewModels.Admin;

public sealed record UserFilterOption(string Value, string Label)
{
    public override string ToString() => Label;
}

/// <summary>
/// ViewModel Quản lý Nhân sự &amp; Tài khoản (Staff + Customer, bảo vệ Admin gốc).
/// </summary>
public sealed class AdminUsersViewModel : ViewModelBase
{
    private static readonly string[] DobFormats = ["dd/MM/yyyy", "d/M/yyyy", "yyyy-MM-dd"];

    private List<AdminUserRow> _allUsers = [];
    private int? _editingUserId;
    private bool _isResettingFilters;

    private string _searchKeyword = string.Empty;
    private UserFilterOption? _selectedRoleFilterOption;
    private UserFilterOption? _selectedStatusFilterOption;
    private AdminUserRow? _selectedUser;

    private UserFilterOption? _selectedFormRoleOption;
    private string _username = string.Empty;
    private string _fullName = string.Empty;
    private string _phone = string.Empty;
    private string _email = string.Empty;
    private string _dateOfBirthText = string.Empty;
    private bool _isActiveAccount = true;

    private string _totalUsersCountText = "0 tài khoản";
    private string _staffCountText = "0 nhân viên";
    private string _customerCountText = "0 khách hàng";
    private string _lockedUsersCountText = "0 đang khóa";
    private string _resultCountText = "0 tài khoản";
    private string? _errorMessage;
    private string? _successMessage;

    public AdminUsersViewModel()
    {
        RoleFilterOptions =
        [
            new UserFilterOption("All", "Tất cả vai trò"),
            new UserFilterOption("Staff", "Nhân viên"),
            new UserFilterOption("Customer", "Khách hàng"),
            new UserFilterOption("Admin", "Quản trị viên")
        ];

        StatusFilterOptions =
        [
            new UserFilterOption("All", "Tất cả trạng thái"),
            new UserFilterOption("Active", "Hoạt động"),
            new UserFilterOption("Locked", "Đã khóa")
        ];

        // Chỉ cho phép Admin tạo hoặc quản lý Staff & Customer (không bao giờ có Admin).
        RoleFormOptions =
        [
            new UserFilterOption("Staff", "Nhân viên"),
            new UserFilterOption("Customer", "Khách hàng")
        ];

        _selectedRoleFilterOption = RoleFilterOptions[0];
        _selectedStatusFilterOption = StatusFilterOptions[0];
        _selectedFormRoleOption = RoleFormOptions[0];

        LoadData();
    }

    public event Action? PasswordClearRequested;

    public IReadOnlyList<UserFilterOption> RoleFilterOptions { get; }
    public IReadOnlyList<UserFilterOption> StatusFilterOptions { get; }
    public IReadOnlyList<UserFilterOption> RoleFormOptions { get; }

    public ObservableCollection<AdminUserRow> Users { get; } = [];

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
            if (!_isResettingFilters)
            {
                ApplyFilters();
            }
        }
    }

    public UserFilterOption? SelectedRoleFilterOption
    {
        get => _selectedRoleFilterOption;
        set
        {
            if (Equals(_selectedRoleFilterOption, value))
            {
                return;
            }

            _selectedRoleFilterOption = value;
            OnPropertyChanged(nameof(SelectedRoleFilterOption));
            if (!_isResettingFilters)
            {
                ApplyFilters();
            }
        }
    }

    public UserFilterOption? SelectedStatusFilterOption
    {
        get => _selectedStatusFilterOption;
        set
        {
            if (Equals(_selectedStatusFilterOption, value))
            {
                return;
            }

            _selectedStatusFilterOption = value;
            OnPropertyChanged(nameof(SelectedStatusFilterOption));
            if (!_isResettingFilters)
            {
                ApplyFilters();
            }
        }
    }

    public AdminUserRow? SelectedUser
    {
        get => _selectedUser;
        set
        {
            if (Equals(_selectedUser, value))
            {
                return;
            }

            _selectedUser = value;
            OnPropertyChanged(nameof(SelectedUser));

            if (value is not null)
            {
                PopulateFormFromUser(value, clearMessages: true);
            }
            else
            {
                NotifyFormStateProperties();
            }
        }
    }

    public UserFilterOption? SelectedFormRoleOption
    {
        get => _selectedFormRoleOption;
        set
        {
            if (Equals(_selectedFormRoleOption, value))
            {
                return;
            }

            _selectedFormRoleOption = value;
            OnPropertyChanged(nameof(SelectedFormRoleOption));
            OnPropertyChanged(nameof(IsCustomerFormMode));
            OnPropertyChanged(nameof(ShowMemberReadOnlyCard));
            OnPropertyChanged(nameof(PhoneFieldLabel));
        }
    }

    public string Username
    {
        get => _username;
        set { if (_username == value) return; _username = value ?? string.Empty; OnPropertyChanged(nameof(Username)); }
    }

    public string FullName
    {
        get => _fullName;
        set { if (_fullName == value) return; _fullName = value ?? string.Empty; OnPropertyChanged(nameof(FullName)); }
    }

    public string Phone
    {
        get => _phone;
        set { if (_phone == value) return; _phone = value ?? string.Empty; OnPropertyChanged(nameof(Phone)); }
    }

    public string Email
    {
        get => _email;
        set { if (_email == value) return; _email = value ?? string.Empty; OnPropertyChanged(nameof(Email)); }
    }

    public string DateOfBirthText
    {
        get => _dateOfBirthText;
        set { if (_dateOfBirthText == value) return; _dateOfBirthText = value ?? string.Empty; OnPropertyChanged(nameof(DateOfBirthText)); }
    }

    public bool IsActiveAccount
    {
        get => _isActiveAccount;
        set { if (_isActiveAccount == value) return; _isActiveAccount = value; OnPropertyChanged(nameof(IsActiveAccount)); }
    }

    public string TotalUsersCountText
    {
        get => _totalUsersCountText;
        private set { if (_totalUsersCountText == value) return; _totalUsersCountText = value; OnPropertyChanged(nameof(TotalUsersCountText)); }
    }

    public string StaffCountText
    {
        get => _staffCountText;
        private set { if (_staffCountText == value) return; _staffCountText = value; OnPropertyChanged(nameof(StaffCountText)); }
    }

    public string CustomerCountText
    {
        get => _customerCountText;
        private set { if (_customerCountText == value) return; _customerCountText = value; OnPropertyChanged(nameof(CustomerCountText)); }
    }

    public string LockedUsersCountText
    {
        get => _lockedUsersCountText;
        private set { if (_lockedUsersCountText == value) return; _lockedUsersCountText = value; OnPropertyChanged(nameof(LockedUsersCountText)); }
    }

    public string ResultCountText
    {
        get => _resultCountText;
        private set { if (_resultCountText == value) return; _resultCountText = value; OnPropertyChanged(nameof(ResultCountText)); }
    }

    public string? ErrorMessage
    {
        get => _errorMessage;
        private set
        {
            _errorMessage = value;
            OnPropertyChanged(nameof(ErrorMessage));
            OnPropertyChanged(nameof(HasError));
        }
    }

    public string? SuccessMessage
    {
        get => _successMessage;
        private set
        {
            _successMessage = value;
            OnPropertyChanged(nameof(SuccessMessage));
            OnPropertyChanged(nameof(HasSuccess));
        }
    }

    public bool HasError => !string.IsNullOrWhiteSpace(ErrorMessage);
    public bool HasSuccess => !string.IsNullOrWhiteSpace(SuccessMessage);
    public bool HasNoUsers => Users.Count == 0;

    public bool IsEditMode => _editingUserId.HasValue;
    public bool IsProtectedAdmin => _selectedUser?.IsAdminRole == true;
    public bool IsSelfSelected => _editingUserId.HasValue && _editingUserId.Value == CurrentAdminId;
    public bool IsEditableUser => !IsProtectedAdmin;
    public bool CanSelectRole => !IsEditMode && !IsProtectedAdmin;
    public bool CanToggleActiveQuick => IsEditMode && !IsProtectedAdmin && !IsSelfSelected;
    public bool CanClickDelete => IsEditMode && !IsProtectedAdmin && !IsSelfSelected;
    public bool CanEditActiveStatus => !IsProtectedAdmin && !IsSelfSelected;

    public bool IsCustomerFormMode => !IsProtectedAdmin && _selectedFormRoleOption?.Value == "Customer";
    public bool ShowMemberReadOnlyCard => IsEditMode && !IsProtectedAdmin && _selectedUser?.IsCustomerRole == true;

    public string FormHeaderText => IsProtectedAdmin
        ? $"Hồ sơ Quản trị viên: {_selectedUser?.FullName}"
        : IsEditMode
            ? $"Cập nhật tài khoản: {_selectedUser?.Username}"
            : "Thêm tài khoản mới";

    public string FormModeBadge => IsProtectedAdmin
        ? "CHỈ XEM (ADMIN)"
        : IsEditMode
            ? "CHỈNH SỬA"
            : "THÊM MỚI";

    public string FixedRoleDisplayText => IsProtectedAdmin
        ? "Quản trị viên (Bảo vệ — không sửa/khóa/xóa)"
        : _selectedUser?.RoleDisplay ?? string.Empty;

    public string PasswordFieldLabel => IsEditMode
        ? "Mật khẩu mới (bỏ trống nếu giữ nguyên)"
        : "Mật khẩu * (>= 6 ký tự)";

    public string PhoneFieldLabel => IsCustomerFormMode
        ? "Số điện thoại * (bắt buộc cho hội viên)"
        : "Số điện thoại";

    public string QuickLockButtonLabel => _selectedUser?.IsActive == true
        ? "Khóa tài khoản"
        : "Mở khóa";

    public string ProtectedAdminNotice =>
        "Tài khoản Quản trị viên gốc được bảo vệ: hệ thống không cho phép tạo thêm Admin mới, đổi vai trò lên Admin, khóa hoặc xóa tài khoản này.";

    public string MemberTierText => _selectedUser?.TierDisplay ?? "Member";
    public string MemberPointsText => _selectedUser?.LoyaltyPointsText ?? "0 điểm";
    public string MemberTotalSpentText => _selectedUser?.TotalSpentText ?? "0đ";

    private static int CurrentAdminId => App.CurrentUser?.UserId ?? 0;

    public override void LoadData() => ReloadUsers(_editingUserId ?? _selectedUser?.UserId, clearMessages: false);

    public void ResetFilters()
    {
        _isResettingFilters = true;
        try
        {
            _searchKeyword = string.Empty;
            _selectedRoleFilterOption = RoleFilterOptions[0];
            _selectedStatusFilterOption = StatusFilterOptions[0];

            OnPropertyChanged(nameof(SearchKeyword));
            OnPropertyChanged(nameof(SelectedRoleFilterOption));
            OnPropertyChanged(nameof(SelectedStatusFilterOption));
        }
        finally
        {
            _isResettingFilters = false;
        }

        ApplyFilters();
    }

    public void StartNewUser() => ResetFormToNew(clearMessages: true);

    private void ResetFormToNew(bool clearMessages)
    {
        if (clearMessages)
        {
            ErrorMessage = null;
            SuccessMessage = null;
        }

        _selectedUser = null;
        _editingUserId = null;

        _selectedFormRoleOption = RoleFormOptions[0];
        _username = string.Empty;
        _fullName = string.Empty;
        _phone = string.Empty;
        _email = string.Empty;
        _dateOfBirthText = string.Empty;
        _isActiveAccount = true;

        OnPropertyChanged(nameof(SelectedUser));
        OnPropertyChanged(nameof(SelectedFormRoleOption));
        OnPropertyChanged(nameof(Username));
        OnPropertyChanged(nameof(FullName));
        OnPropertyChanged(nameof(Phone));
        OnPropertyChanged(nameof(Email));
        OnPropertyChanged(nameof(DateOfBirthText));
        OnPropertyChanged(nameof(IsActiveAccount));

        NotifyFormStateProperties();
        PasswordClearRequested?.Invoke();
    }

    public void ResetForm()
    {
        ErrorMessage = null;
        SuccessMessage = null;

        if (_selectedUser is not null)
        {
            PopulateFormFromUser(_selectedUser, clearMessages: false);
        }
        else
        {
            StartNewUser();
        }
    }

    public void SaveUser(string? newPassword)
    {
        ErrorMessage = null;
        SuccessMessage = null;

        if (IsProtectedAdmin)
        {
            ErrorMessage = ProtectedAdminNotice;
            return;
        }

        string roleValue = IsEditMode
            ? (_selectedUser?.Role ?? _selectedFormRoleOption?.Value ?? "Staff")
            : (_selectedFormRoleOption?.Value ?? "Staff");

        string? dobSql = null;
        if (roleValue == "Customer" && !string.IsNullOrWhiteSpace(DateOfBirthText))
        {
            if (!DateTime.TryParseExact(
                    DateOfBirthText.Trim(),
                    DobFormats,
                    CultureInfo.InvariantCulture,
                    DateTimeStyles.None,
                    out DateTime parsedDob)
                || parsedDob.Date > DateTime.Today)
            {
                ErrorMessage = "Ngày sinh không hợp lệ (dùng định dạng dd/MM/yyyy và không vượt quá ngày hiện tại).";
                return;
            }

            dobSql = parsedDob.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
        }

        bool isCreating = !IsEditMode;
        string savedUsername = Username.Trim();
        var input = new UserSaveInput(
            UserId: _editingUserId,
            Username: savedUsername,
            NewPassword: newPassword,
            FullName: FullName,
            Email: Email,
            Phone: Phone,
            Role: roleValue,
            IsActive: IsActiveAccount,
            DateOfBirthSql: dobSql);

        UserSaveResult result = UserData.SaveUser(input, CurrentAdminId);
        if (!result.Succeeded)
        {
            ErrorMessage = result.ErrorMessage ?? "Không thể lưu tài khoản.";
            return;
        }

        ReloadUsers(result.UserId, clearMessages: false);
        SuccessMessage = isCreating
            ? $"Đã tạo tài khoản \"{savedUsername}\" thành công."
            : $"Đã cập nhật tài khoản \"{savedUsername}\".";
        PasswordClearRequested?.Invoke();
    }

    public void ToggleSelectedUserActive()
    {
        ErrorMessage = null;
        SuccessMessage = null;

        if (_selectedUser is null)
        {
            ErrorMessage = "Vui lòng chọn một tài khoản trong danh sách.";
            return;
        }

        bool nextActive = !_selectedUser.IsActive;
        UserActionResult result = UserData.SetUserActive(_selectedUser.UserId, nextActive, CurrentAdminId);
        if (!result.Succeeded)
        {
            ErrorMessage = result.ErrorMessage ?? "Không thể thay đổi trạng thái tài khoản.";
            return;
        }

        string targetName = _selectedUser.Username;
        int targetId = _selectedUser.UserId;
        ReloadUsers(targetId, clearMessages: false);
        SuccessMessage = nextActive
            ? $"Đã mở khóa tài khoản \"{targetName}\"."
            : $"Đã khóa tài khoản \"{targetName}\".";
    }

    public bool CanDeleteSelectedUser(out string summary)
    {
        summary = string.Empty;
        ErrorMessage = null;
        SuccessMessage = null;

        if (_selectedUser is null)
        {
            ErrorMessage = "Vui lòng chọn một tài khoản trong danh sách để xóa.";
            return false;
        }

        if (_selectedUser.IsAdminRole)
        {
            ErrorMessage = ProtectedAdminNotice;
            return false;
        }

        if (_selectedUser.UserId == CurrentAdminId)
        {
            ErrorMessage = "Không thể tự xóa tài khoản đang đăng nhập.";
            return false;
        }

        if (_selectedUser.HasBlockingHistory)
        {
            ErrorMessage = _selectedUser.Role == "Customer"
                ? $"Khách hàng \"{_selectedUser.FullName}\" đã phát sinh {_selectedUser.CustomerOrdersCount} hóa đơn nên không thể xóa. Hãy dùng nút Khóa tài khoản."
                : $"Nhân viên \"{_selectedUser.FullName}\" đã phát sinh {_selectedUser.StaffActivityCount} giao dịch/suất chiếu nên không thể xóa. Hãy dùng nút Khóa tài khoản.";
            return false;
        }

        summary = $"{_selectedUser.FullName} (@{_selectedUser.Username} - {_selectedUser.RoleDisplay})";
        return true;
    }

    public void DeleteSelectedUser()
    {
        ErrorMessage = null;
        SuccessMessage = null;

        if (_selectedUser is null)
        {
            ErrorMessage = "Vui lòng chọn tài khoản cần xóa.";
            return;
        }

        string deletedLabel = _selectedUser.Username;
        UserActionResult result = UserData.DeleteUser(_selectedUser.UserId, CurrentAdminId);
        if (!result.Succeeded)
        {
            ErrorMessage = result.ErrorMessage ?? "Không thể xóa tài khoản.";
            return;
        }

        _selectedUser = null;
        _editingUserId = null;
        ReloadUsers(preferredUserId: null, clearMessages: false);
        if (Users.Count == 0)
        {
            ResetFormToNew(clearMessages: false);
        }

        SuccessMessage = $"Đã xóa tài khoản \"{deletedLabel}\" khỏi hệ thống.";
    }

    private void ReloadUsers(int? preferredUserId, bool clearMessages)
    {
        _allUsers = UserData.LoadAllUsers();
        UpdateKpis();
        ApplyFilters(preferredUserId, clearMessages);
    }

    private void UpdateKpis()
    {
        int total = _allUsers.Count;
        int staff = _allUsers.Count(u => u.Role == "Staff");
        int customer = _allUsers.Count(u => u.Role == "Customer");
        int locked = _allUsers.Count(u => !u.IsActive);

        TotalUsersCountText = $"{total} tài khoản";
        StaffCountText = $"{staff} nhân viên";
        CustomerCountText = $"{customer} khách hàng";
        LockedUsersCountText = $"{locked} đang khóa";
    }

    private void ApplyFilters(int? preferredUserId = null, bool clearMessages = false)
    {
        int? targetId = preferredUserId ?? _selectedUser?.UserId;
        string keyword = _searchKeyword.Trim();
        string roleFilter = _selectedRoleFilterOption?.Value ?? "All";
        string statusFilter = _selectedStatusFilterOption?.Value ?? "All";

        List<AdminUserRow> filtered = [];
        foreach (AdminUserRow user in _allUsers)
        {
            if (roleFilter != "All" && !string.Equals(user.Role, roleFilter, StringComparison.Ordinal))
            {
                continue;
            }

            if (statusFilter == "Active" && !user.IsActive)
            {
                continue;
            }

            if (statusFilter == "Locked" && user.IsActive)
            {
                continue;
            }

            if (!string.IsNullOrEmpty(keyword) && !MatchesKeyword(user, keyword))
            {
                continue;
            }

            filtered.Add(user);
        }

        Users.Clear();
        foreach (AdminUserRow user in filtered)
        {
            Users.Add(user);
        }

        OnPropertyChanged(nameof(HasNoUsers));
        ResultCountText = $"{Users.Count} tài khoản";

        AdminUserRow? nextSelected = targetId.HasValue
            ? Users.FirstOrDefault(u => u.UserId == targetId.Value)
            : null;
        nextSelected ??= Users.FirstOrDefault();

        _selectedUser = nextSelected;
        OnPropertyChanged(nameof(SelectedUser));

        if (nextSelected is not null)
        {
            PopulateFormFromUser(nextSelected, clearMessages);
        }
        else if (Users.Count == 0 && _editingUserId.HasValue)
        {
            ResetFormToNew(clearMessages: false);
        }
        else
        {
            NotifyFormStateProperties();
        }
    }

    private void PopulateFormFromUser(AdminUserRow user, bool clearMessages)
    {
        if (clearMessages)
        {
            ErrorMessage = null;
            SuccessMessage = null;
        }

        _editingUserId = user.UserId;
        _selectedFormRoleOption = RoleFormOptions.FirstOrDefault(r => r.Value == user.Role) ?? RoleFormOptions[0];
        _username = user.Username;
        _fullName = user.FullName;
        _phone = user.Phone;
        _email = user.Email;
        _dateOfBirthText = user.DateOfBirthDisplay;
        _isActiveAccount = user.IsActive;

        OnPropertyChanged(nameof(SelectedFormRoleOption));
        OnPropertyChanged(nameof(Username));
        OnPropertyChanged(nameof(FullName));
        OnPropertyChanged(nameof(Phone));
        OnPropertyChanged(nameof(Email));
        OnPropertyChanged(nameof(DateOfBirthText));
        OnPropertyChanged(nameof(IsActiveAccount));

        NotifyFormStateProperties();
        PasswordClearRequested?.Invoke();
    }

    private void NotifyFormStateProperties()
    {
        OnPropertyChanged(nameof(IsEditMode));
        OnPropertyChanged(nameof(IsProtectedAdmin));
        OnPropertyChanged(nameof(IsSelfSelected));
        OnPropertyChanged(nameof(IsEditableUser));
        OnPropertyChanged(nameof(CanSelectRole));
        OnPropertyChanged(nameof(CanToggleActiveQuick));
        OnPropertyChanged(nameof(CanClickDelete));
        OnPropertyChanged(nameof(CanEditActiveStatus));
        OnPropertyChanged(nameof(IsCustomerFormMode));
        OnPropertyChanged(nameof(ShowMemberReadOnlyCard));
        OnPropertyChanged(nameof(FormHeaderText));
        OnPropertyChanged(nameof(FormModeBadge));
        OnPropertyChanged(nameof(FixedRoleDisplayText));
        OnPropertyChanged(nameof(PasswordFieldLabel));
        OnPropertyChanged(nameof(PhoneFieldLabel));
        OnPropertyChanged(nameof(QuickLockButtonLabel));
        OnPropertyChanged(nameof(MemberTierText));
        OnPropertyChanged(nameof(MemberPointsText));
        OnPropertyChanged(nameof(MemberTotalSpentText));
    }

    private static bool MatchesKeyword(AdminUserRow user, string keyword)
    {
        return user.Username.Contains(keyword, StringComparison.OrdinalIgnoreCase)
            || user.FullName.Contains(keyword, StringComparison.OrdinalIgnoreCase)
            || user.Phone.Contains(keyword, StringComparison.OrdinalIgnoreCase)
            || user.Email.Contains(keyword, StringComparison.OrdinalIgnoreCase);
    }
}
