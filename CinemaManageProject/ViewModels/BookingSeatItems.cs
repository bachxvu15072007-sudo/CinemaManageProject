using System.Collections.ObjectModel;
using CinemaManageProject.Data;
using CinemaManageProject.Helpers;
using CommunityToolkit.Mvvm.ComponentModel;

namespace CinemaManageProject.ViewModels;

/// <summary>
/// Nhóm ghế theo từng hàng dùng chung cho sơ đồ chọn ghế (Staff POS và Customer Self-Booking).
/// </summary>
public sealed class BookingSeatRowGroup
{
    public BookingSeatRowGroup(string rowLabel, IEnumerable<BookingSeatCellItem> seats)
    {
        RowLabel = rowLabel;
        Seats = new ObservableCollection<BookingSeatCellItem>(seats);
    }

    public string RowLabel { get; }
    public ObservableCollection<BookingSeatCellItem> Seats { get; }
}

/// <summary>
/// Ô ghế tương tác trên sơ đồ chọn ghế với trạng thái Thường / VIP / Đôi / Đang chọn / Đã đặt / Bảo trì.
/// </summary>
public sealed partial class BookingSeatCellItem : ViewModelBase
{
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(VisualState))]
    private bool _isSelected;

    public BookingSeatCellItem(ShowtimeSeatRow row, decimal unitPrice)
    {
        SeatId = row.SeatId;
        RoomId = row.RoomId;
        RowLabel = row.RowLabel;
        SeatNumber = row.SeatNumber;
        SeatCode = row.SeatCode;
        SeatType = row.SeatType;
        PriceMultiplier = row.PriceMultiplier;
        IsActive = row.IsActive;
        IsBooked = row.IsBooked;
        UnitPrice = unitPrice;
    }

    public int SeatId { get; }
    public int RoomId { get; }
    public string RowLabel { get; }
    public int SeatNumber { get; }
    public string SeatCode { get; }
    public string SeatType { get; }
    public decimal PriceMultiplier { get; }
    public bool IsActive { get; }
    public bool IsBooked { get; }
    public decimal UnitPrice { get; }

    public bool IsCouple => SeatType == "Couple";
    public bool IsSelectable => IsActive && !IsBooked;

    public string VisualState => !IsActive
        ? "Maintenance"
        : IsBooked
            ? "Booked"
            : IsSelected
                ? "Selected"
                : SeatType;

    public string SeatTypeDisplay => FormatHelper.FormatSeatType(SeatType);
    public string UnitPriceText => FormatHelper.FormatVnd(UnitPrice);

    public string ToolTipText => !IsActive
        ? $"{SeatCode} • Bảo trì (Không thể đặt)"
        : IsBooked
            ? $"{SeatCode} • Đã có khách đặt"
            : $"{SeatCode} • {SeatTypeDisplay} • {UnitPriceText}";
}
