using System.Collections.ObjectModel;
using System.Globalization;
using CinemaManageProject.Data;
using CommunityToolkit.Mvvm.ComponentModel;

namespace CinemaManageProject.ViewModels.Admin;

public sealed record UserFilterOption(string Value, string Label)
{
    public override string ToString() => Label;
}

/// <summary>
/// ViewModel Quản lý Nhân sự &amp; Tài khoản (Staff + Customer, bảo vệ Admin gốc).
/// </summary>
public sealed partial class AdminUsersViewModel : ViewModelBase
{
    private static readonly string[] DobFormats = ["dd/MM/yyyy", "d/M/yyyy", "yyyy-MM-dd"];

    private List<AdminUserRow> _allUsers = [];
    private int? _editingUserId;
    private bool _isResettingFilters;
    private bool _suppressSelectionPopulate;

    [ObservableProperty]
    private string _searchKeyword = string.Empty;

    [ObservableProperty]
    private UserFilterOption? _selectedRoleFilterOption;

    [ObservableProperty]
    private UserFilterOption? _selectedStatusFilterOption;

    [ObservableProperty]
    private AdminUserRow? _selectedUser;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsCustomerFormMode))]
    [NotifyPropertyChangedFor(nameof(ShowMemberReadOnlyCard))]
    [NotifyPropertyChangedFor(nameof(PhoneFieldLabel))]
    private UserFilterOption? _selectedFormRoleOption;

    [ObservableProperty]
    private string _username = string.Empty;

    [ObservableProperty]
    private string _fullName = string.Empty;

    [ObservableProperty]
    private string _phone = string.Empty;

    [ObservableProperty]
    private string _email = string.Empty;

    [ObservableProperty]
    private string _dateOfBirthText = string.Empty;

    [ObservableProperty]
    private bool _isActiveAccount = true;

    [ObservableProperty]
    private string _totalUsersCountText = "0 tài khoản";

    [ObservableProperty]
    private string _staffCountText = "0 nhân viên";

    [ObservableProperty]
    private string _customerCountText = "0 khách hàng";

    [ObservableProperty]
    private string _lockedUsersCountText = "0 đang khóa";

    [ObservableProperty]
    private string _resultCountText = "0 tài khoản";

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasError))]
    private string? _errorMessage;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasSuccess))]
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

    public bool HasError => !string.IsNullOrWhiteSpace(ErrorMessage);
    public bool HasSuccess => !string.IsNullOrWhiteSpace(SuccessMessage);
    public bool HasNoUsers => Users.Count == 0;

    public bool IsEditMode => _editingUserId.HasValue;
    public bool IsProtectedAdmin => SelectedUser?.IsAdminRole == true;
    public bool IsSelfSelected => _editingUserId.HasValue && _editingUserId.Value == CurrentAdminId;
    public bool IsEditableUser => !IsProtectedAdmin;
    public bool CanSelectRole => !IsEditMode && !IsProtectedAdmin;
    public bool CanToggleActiveQuick => IsEditMode && !IsProtectedAdmin && !IsSelfSelected;
    public bool CanClickDelete => IsEditMode && !IsProtectedAdmin && !IsSelfSelected;
    public bool CanEditActiveStatus => !IsProtectedAdmin && !IsSelfSelected;

    public bool IsCustomerFormMode => !IsProtectedAdmin && SelectedFormRoleOption?.Value == "Customer";
    public bool ShowMemberReadOnlyCard => IsEditMode && !IsProtectedAdmin && SelectedUser?.IsCustomerRole == true;

    public string FormHeaderText => IsProtectedAdmin
        ? $"Hồ sơ Quản trị viên: {SelectedUser?.FullName}"
        : IsEditMode
            ? $"Cập nhật tài khoản: {SelectedUser?.Username}"
            : "Thêm tài khoản mới";

    public string FormModeBadge => IsProtectedAdmin
        ? "CHỈ XEM (ADMIN)"
        : IsEditMode
            ? "CHỈNH SỬA"
            : "THÊM MỚI";

    public string FixedRoleDisplayText => IsProtectedAdmin
        ? "Quản trị viên (Bảo vệ — không sửa/khóa/xóa)"
        : SelectedUser?.RoleDisplay ?? string.Empty;

    public string PasswordFieldLabel => IsEditMode
        ? "Mật khẩu mới (bỏ trống nếu giữ nguyên)"
        : "Mật khẩu * (>= 6 ký tự)";

    public string PhoneFieldLabel => IsCustomerFormMode
        ? "Số điện thoại * (bắt buộc cho hội viên)"
        : "Số điện thoại";

    public string QuickLockButtonLabel => SelectedUser?.IsActive == true
        ? "Khóa tài khoản"
        : "Mở khóa";

    public string ProtectedAdminNotice =>
        "Tài khoản Quản trị viên gốc được bảo vệ: hệ thống không cho phép tạo thêm Admin mới, đổi vai trò lên Admin, khóa hoặc xóa tài khoản này.";

    public string MemberTierText => SelectedUser?.TierDisplay ?? "Member";
    public string MemberPointsText => SelectedUser?.LoyaltyPointsText ?? "0 điểm";
    public string MemberTotalSpentText => SelectedUser?.TotalSpentText ?? "0đ";

    private static int CurrentAdminId => App.CurrentUser?.UserId ?? 0;

    partial void OnSearchKeywordChanged(string value)
    {
        if (!_isResettingFilters)
        {
            ApplyFilters();
        }
    }

    partial void OnSelectedRoleFilterOptionChanged(UserFilterOption? value)
    {
        if (!_isResettingFilters)
        {
            ApplyFilters();
        }
    }

    partial void OnSelectedStatusFilterOptionChanged(UserFilterOption? value)
    {
        if (!_isResettingFilters)
        {
            ApplyFilters();
        }
    }

    partial void OnSelectedUserChanged(AdminUserRow? value)
    {
        if (_suppressSelectionPopulate)
        {
            return;
        }

        if (value is not null)
        {
            PopulateFormFromUser(value, clearMessages: true);
        }
        else
        {
            NotifyFormStateProperties();
        }
    }

    public override void LoadData() => ReloadUsers(_editingUserId ?? SelectedUser?.UserId, clearMessages: false);

    public void ResetFilters()
    {
        _isResettingFilters = true;
        try
        {
            SearchKeyword = string.Empty;
            SelectedRoleFilterOption = RoleFilterOptions[0];
            SelectedStatusFilterOption = StatusFilterOptions[0];
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

        _editingUserId = null;

        _suppressSelectionPopulate = true;
        try
        {
            SelectedUser = null;
            SelectedFormRoleOption = RoleFormOptions[0];
            Username = string.Empty;
            FullName = string.Empty;
            Phone = string.Empty;
            Email = string.Empty;
            DateOfBirthText = string.Empty;
            IsActiveAccount = true;
        }
        finally
        {
            _suppressSelectionPopulate = false;
        }

        NotifyFormStateProperties();
        PasswordClearRequested?.Invoke();
    }

    public void ResetForm()
    {
        ErrorMessage = null;
        SuccessMessage = null;

        if (SelectedUser is not null)
        {
            PopulateFormFromUser(SelectedUser, clearMessages: false);
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
            ? (SelectedUser?.Role ?? SelectedFormRoleOption?.Value ?? "Staff")
            : (SelectedFormRoleOption?.Value ?? "Staff");

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
        string savedUsername = (Username ?? string.Empty).Trim();
        var input = new UserSaveInput(
            UserId: _editingUserId,
            Username: savedUsername,
            NewPassword: newPassword,
            FullName: FullName ?? string.Empty,
            Email: Email ?? string.Empty,
            Phone: Phone ?? string.Empty,
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

        if (SelectedUser is null)
        {
            ErrorMessage = "Vui lòng chọn một tài khoản trong danh sách.";
            return;
        }

        bool nextActive = !SelectedUser.IsActive;
        UserActionResult result = UserData.SetUserActive(SelectedUser.UserId, nextActive, CurrentAdminId);
        if (!result.Succeeded)
        {
            ErrorMessage = result.ErrorMessage ?? "Không thể thay đổi trạng thái tài khoản.";
            return;
        }

        string targetName = SelectedUser.Username;
        int targetId = SelectedUser.UserId;
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

        if (SelectedUser is null)
        {
            ErrorMessage = "Vui lòng chọn một tài khoản trong danh sách để xóa.";
            return false;
        }

        if (SelectedUser.IsAdminRole)
        {
            ErrorMessage = ProtectedAdminNotice;
            return false;
        }

        if (SelectedUser.UserId == CurrentAdminId)
        {
            ErrorMessage = "Không thể tự xóa tài khoản đang đăng nhập.";
            return false;
        }

        if (SelectedUser.HasBlockingHistory)
        {
            ErrorMessage = SelectedUser.Role == "Customer"
                ? $"Khách hàng \"{SelectedUser.FullName}\" đã phát sinh {SelectedUser.CustomerOrdersCount} hóa đơn nên không thể xóa. Hãy dùng nút Khóa tài khoản."
                : $"Nhân viên \"{SelectedUser.FullName}\" đã phát sinh {SelectedUser.StaffActivityCount} giao dịch/suất chiếu nên không thể xóa. Hãy dùng nút Khóa tài khoản.";
            return false;
        }

        summary = $"{SelectedUser.FullName} (@{SelectedUser.Username} - {SelectedUser.RoleDisplay})";
        return true;
    }

    public void DeleteSelectedUser()
    {
        ErrorMessage = null;
        SuccessMessage = null;

        if (SelectedUser is null)
        {
            ErrorMessage = "Vui lòng chọn tài khoản cần xóa.";
            return;
        }

        string deletedLabel = SelectedUser.Username;
        UserActionResult result = UserData.DeleteUser(SelectedUser.UserId, CurrentAdminId);
        if (!result.Succeeded)
        {
            ErrorMessage = result.ErrorMessage ?? "Không thể xóa tài khoản.";
            return;
        }

        _editingUserId = null;
        _suppressSelectionPopulate = true;
        SelectedUser = null;
        _suppressSelectionPopulate = false;

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
        int? targetId = preferredUserId ?? SelectedUser?.UserId;
        string keyword = (SearchKeyword ?? string.Empty).Trim();
        string roleFilter = SelectedRoleFilterOption?.Value ?? "All";
        string statusFilter = SelectedStatusFilterOption?.Value ?? "All";

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

        _suppressSelectionPopulate = true;
        SelectedUser = nextSelected;
        _suppressSelectionPopulate = false;

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
        SelectedFormRoleOption = RoleFormOptions.FirstOrDefault(r => r.Value == user.Role) ?? RoleFormOptions[0];
        Username = user.Username;
        FullName = user.FullName;
        Phone = user.Phone;
        Email = user.Email;
        DateOfBirthText = user.DateOfBirthDisplay;
        IsActiveAccount = user.IsActive;

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
