namespace CinemaManageProject.Models;

/// <summary>
/// Vai trò tài khoản trong hệ thống quản lý rạp phim.
/// </summary>
public enum UserRole
{
    Admin = 0,
    Staff = 1,
    Customer = 2
}

/// <summary>
/// Hạng thành viên của khách hàng dựa trên tổng chi tiêu.
/// </summary>
public enum CustomerTier
{
    Member = 0,
    Silver = 1,
    Gold = 2,
    Platinum = 3
}

/// <summary>
/// Trạng thái phát hành của phim tại rạp.
/// </summary>
public enum MovieStatus
{
    ComingSoon = 0,
    NowShowing = 1,
    Ended = 2
}

/// <summary>
/// Phân loại độ tuổi khán giả theo quy định rạp chiếu phim.
/// </summary>
public enum AgeRating
{
    P = 0,
    K = 1,
    T13 = 2,
    T16 = 3,
    T18 = 4
}

/// <summary>
/// Loại phòng chiếu phim.
/// </summary>
public enum RoomType
{
    Standard2D = 0,
    Standard3D = 1,
    IMAX = 2,
    VIP = 3
}

/// <summary>
/// Loại ghế trong phòng chiếu.
/// </summary>
public enum SeatType
{
    Standard = 0,
    VIP = 1,
    Couple = 2
}

/// <summary>
/// Trạng thái của một suất chiếu.
/// </summary>
public enum ShowtimeStatus
{
    Scheduled = 0,
    OpenForSale = 1,
    Completed = 2,
    Cancelled = 3
}

/// <summary>
/// Trạng thái hóa đơn bán vé.
/// </summary>
public enum OrderStatus
{
    Paid = 0,
    Cancelled = 1,
    PartiallyRefunded = 2
}

/// <summary>
/// Phương thức thanh toán hóa đơn.
/// </summary>
public enum PaymentMethod
{
    Cash = 0,
    BankTransfer = 1,
    Card = 2,
    EWallet = 3
}

/// <summary>
/// Kênh đặt vé (tại quầy bởi nhân viên hoặc khách hàng tự đặt).
/// </summary>
public enum BookingChannel
{
    Counter = 0,
    SelfService = 1
}

/// <summary>
/// Trạng thái của từng vé xem phim.
/// </summary>
public enum TicketStatus
{
    Booked = 0,
    CheckedIn = 1,
    Cancelled = 2
}
