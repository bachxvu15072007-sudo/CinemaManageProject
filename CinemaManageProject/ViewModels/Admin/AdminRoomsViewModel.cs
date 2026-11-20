using System.Collections.ObjectModel;
using System.Globalization;
using CinemaManageProject.Data;
using CinemaManageProject.Helpers;

namespace CinemaManageProject.ViewModels.Admin;

public sealed class SeatRowGroup
{
    public SeatRowGroup(string rowLabel, IEnumerable<SeatCellItem> seats)
    {
        RowLabel = rowLabel;
        Seats = new ObservableCollection<SeatCellItem>(seats);
    }

    public string RowLabel { get; }
    public ObservableCollection<SeatCellItem> Seats { get; }
}

public sealed class SeatCellItem : ViewModelBase
{
    private bool _isAvailable;
    private bool _isSelected;

    public SeatCellItem(RoomSeatRow row)
    {
        SeatId = row.SeatId;
        RoomId = row.RoomId;
        RowLabel = row.RowLabel;
        SeatNumber = row.SeatNumber;
        SeatCode = row.SeatCode;
        SeatType = row.SeatType;
        PriceMultiplier = row.PriceMultiplier;
        _isAvailable = row.IsActive;
    }

    public int SeatId { get; }
    public int RoomId { get; }
    public string RowLabel { get; }
    public int SeatNumber { get; }
    public string SeatCode { get; }
    public string SeatType { get; }
    public decimal PriceMultiplier { get; }

    public bool IsCouple => SeatType == "Couple";

    public bool IsAvailable
    {
        get => _isAvailable;
        set
        {
            if (_isAvailable == value) return;
            _isAvailable = value;
            OnPropertyChanged(nameof(IsAvailable));
            OnPropertyChanged(nameof(StatusDisplay));
            OnPropertyChanged(nameof(VisualState));
        }
    }

    public bool IsSelected
    {
        get => _isSelected;
        set { if (_isSelected == value) return; _isSelected = value; OnPropertyChanged(nameof(IsSelected)); }
    }

    public string SeatTypeDisplay => FormatHelper.FormatSeatType(SeatType);
    public string MultiplierText => $"×{PriceMultiplier.ToString("0.00", CultureInfo.InvariantCulture)}";
    public string PositionText => $"Hàng {RowLabel} • Ghế số {SeatNumber}";
    public string StatusDisplay => IsAvailable ? "Khả dụng (Cho phép đặt)" : "Đang bảo trì (Không cho đặt)";
    public string VisualState => IsAvailable ? SeatType : "Maintenance";
}

/// <summary>
/// ViewModel Quản lý Phòng chiếu &amp; Sơ đồ Ghế cho Admin: xem phòng, kiểm tra đơn đặt theo ghế và bật/tắt bảo trì ghế.
/// </summary>
public sealed class AdminRoomsViewModel : ViewModelBase
{
    private int? _selectedRoomId;
    private int? _selectedSeatId;
    private AdminRoomRow? _selectedRoom;
    private SeatCellItem? _selectedSeat;
    private string _seatMapHeader = "Sơ đồ Ghế";
    private string _seatMapSubtitle = "Chọn một phòng bên trái để xem sơ đồ ghế.";
    private string _seatOrdersSummaryText = string.Empty;
    private string? _errorMessage;
    private string? _successMessage;

    public AdminRoomsViewModel()
    {
        LoadData();
    }

    public ObservableCollection<AdminRoomRow> Rooms { get; } = [];
    public ObservableCollection<SeatRowGroup> SeatRows { get; } = [];
    public ObservableCollection<SeatOrderRow> SeatOrders { get; } = [];

    public AdminRoomRow? SelectedRoom
    {
        get => _selectedRoom;
        set
        {
            if (value is not null && _selectedRoomId != value.RoomId)
            {
                _selectedSeatId = null;
                ClearMessages();
            }

            _selectedRoom = value;
            OnPropertyChanged(nameof(SelectedRoom));
            if (value is not null)
            {
                _selectedRoomId = value.RoomId;
                ShowSelectedRoom(value);
            }
        }
    }

    public SeatCellItem? SelectedSeat
    {
        get => _selectedSeat;
        private set
        {
            _selectedSeat = value;
            _selectedSeatId = value?.SeatId;
            OnPropertyChanged(nameof(SelectedSeat));
            OnPropertyChanged(nameof(HasSelectedSeat));
            OnPropertyChanged(nameof(SeatActionLabel));
        }
    }

    public bool HasSelectedSeat => SelectedSeat is not null;
    public bool HasNoSeatOrders => HasSelectedSeat && SeatOrders.Count == 0;
    public string SeatActionLabel => SelectedSeat?.IsAvailable == true
        ? "Đặt bảo trì (Không cho đặt)"
        : "Mở lại khả dụng (Available)";

    public string SeatMapHeader { get => _seatMapHeader; private set { _seatMapHeader = value; OnPropertyChanged(nameof(SeatMapHeader)); } }
    public string SeatMapSubtitle { get => _seatMapSubtitle; private set { _seatMapSubtitle = value; OnPropertyChanged(nameof(SeatMapSubtitle)); } }
    public string SeatOrdersSummaryText { get => _seatOrdersSummaryText; private set { _seatOrdersSummaryText = value; OnPropertyChanged(nameof(SeatOrdersSummaryText)); } }

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
        int? preferredRoomId = _selectedRoomId ?? SelectedRoom?.RoomId;
        int? preferredSeatId = _selectedSeatId;

        Rooms.Clear();
        foreach (AdminRoomRow room in RoomData.LoadAllRooms())
        {
            Rooms.Add(room);
        }

        AdminRoomRow? target = preferredRoomId.HasValue
            ? Rooms.FirstOrDefault(r => r.RoomId == preferredRoomId.Value)
            : Rooms.FirstOrDefault();

        if (target is not null)
        {
            _selectedSeatId = preferredSeatId;
            SelectedRoom = target;
        }
    }

    public void SelectSeat(SeatCellItem? seat)
    {
        ClearMessages();
        foreach (SeatRowGroup row in SeatRows)
        {
            foreach (SeatCellItem item in row.Seats)
            {
                item.IsSelected = ReferenceEquals(item, seat);
            }
        }

        SelectedSeat = seat;
        LoadSeatOrderHistory(seat);
    }

    public void ToggleSelectedSeatAvailable()
    {
        ClearMessages();
        if (SelectedSeat is null)
        {
            return;
        }

        bool targetAvailable = !SelectedSeat.IsAvailable;
        if (!targetAvailable)
        {
            int activeTickets = RoomData.CountActiveTicketsForSeat(SelectedSeat.SeatId);
            if (activeTickets > 0)
            {
                ErrorMessage = $"Ghế {SelectedSeat.SeatCode} đang có {activeTickets} vé đã đặt ở suất chiếu sắp tới, không thể chuyển sang bảo trì.";
                return;
            }
        }

        RoomData.SetSeatActive(SelectedSeat.SeatId, targetAvailable);
        string code = SelectedSeat.SeatCode;
        _selectedSeatId = SelectedSeat.SeatId;

        LoadData();
        SuccessMessage = targetAvailable
            ? $"Đã mở khả dụng (Available) cho ghế {code}."
            : $"Đã chuyển ghế {code} sang bảo trì (không cho đặt).";
    }

    private void ShowSelectedRoom(AdminRoomRow room)
    {
        SeatRows.Clear();
        List<RoomSeatRow> seats = RoomData.LoadSeatsByRoom(room.RoomId);

        foreach (IGrouping<int, RoomSeatRow> rowGroup in seats.GroupBy(s => s.RowIndex).OrderBy(g => g.Key))
        {
            string label = rowGroup.First().RowLabel;
            SeatRows.Add(new SeatRowGroup(label, rowGroup.Select(s => new SeatCellItem(s))));
        }

        SeatMapHeader = $"Sơ đồ Ghế — {room.Name}";
        SeatMapSubtitle = $"{room.RoomTypeDisplay} • {room.DimensionsText} • {room.SeatSummaryText}";

        SeatCellItem? seatToSelect = _selectedSeatId.HasValue
            ? SeatRows.SelectMany(r => r.Seats).FirstOrDefault(s => s.SeatId == _selectedSeatId.Value)
            : null;

        foreach (SeatRowGroup row in SeatRows)
        {
            foreach (SeatCellItem item in row.Seats)
            {
                item.IsSelected = ReferenceEquals(item, seatToSelect);
            }
        }

        SelectedSeat = seatToSelect;
        LoadSeatOrderHistory(seatToSelect);
    }

    private void LoadSeatOrderHistory(SeatCellItem? seat)
    {
        SeatOrders.Clear();
        if (seat is null)
        {
            SeatOrdersSummaryText = string.Empty;
            OnPropertyChanged(nameof(HasNoSeatOrders));
            return;
        }

        List<SeatOrderRow> orders = RoomData.LoadSeatOrders(seat.SeatId);
        foreach (SeatOrderRow order in orders)
        {
            SeatOrders.Add(order);
        }

        SeatOrdersSummaryText = orders.Count == 0
            ? $"Ghế {seat.SeatCode} chưa có đơn hàng nào đặt."
            : $"Tìm thấy {orders.Count} vé/đơn hàng gắn với ghế {seat.SeatCode}:";
        OnPropertyChanged(nameof(HasNoSeatOrders));
    }

    private void ClearMessages() { ErrorMessage = null; SuccessMessage = null; }
}
