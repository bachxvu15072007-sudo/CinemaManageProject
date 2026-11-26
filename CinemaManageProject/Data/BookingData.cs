using System.Globalization;
using CinemaManageProject.Helpers;
using Dapper;
using Microsoft.Data.Sqlite;

namespace CinemaManageProject.Data;

public sealed record OpenShowtimeOption
{
    public int ShowtimeId { get; init; }
    public int MovieId { get; init; }
    public string MovieTitle { get; init; } = string.Empty;
    public string AgeRating { get; init; } = string.Empty;
    public int DurationMinutes { get; init; }
    public string Language { get; init; } = string.Empty;
    public string? PosterPath { get; init; }
    public string GenresText { get; init; } = string.Empty;
    public int RoomId { get; init; }
    public string RoomName { get; init; } = string.Empty;
    public string RoomType { get; init; } = string.Empty;
    public string StartTimeSql { get; init; } = string.Empty;
    public string EndTimeSql { get; init; } = string.Empty;
    public decimal BaseTicketPrice { get; init; }
    public decimal RoomSurcharge { get; init; }
    public string Format { get; init; } = string.Empty;
    public int ActiveSeatsCount { get; init; }
    public int BookedSeatsCount { get; init; }

    public string ShowDateDisplay => FormatHelper.FormatSqlDateTime(StartTimeSql, "dd/MM/yyyy");
    public string StartTimeShort => FormatHelper.FormatSqlDateTime(StartTimeSql, "dd/MM HH:mm");
    public string TimeRangeDisplay =>
        $"{FormatHelper.FormatSqlDateTime(StartTimeSql, "HH:mm")} - {FormatHelper.FormatSqlDateTime(EndTimeSql, "HH:mm")}";
    public decimal StandardPrice => BaseTicketPrice + RoomSurcharge;
    public string StandardPriceText => FormatHelper.FormatVnd(StandardPrice);
    public int AvailableSeatsCount => Math.Max(0, ActiveSeatsCount - BookedSeatsCount);
    public bool IsSoldOut => AvailableSeatsCount <= 0;
    public string AvailabilityText => $"{AvailableSeatsCount}/{ActiveSeatsCount} trống";
    public string RoomTypeDisplay => FormatHelper.FormatRoomType(RoomType);
}

public sealed record ShowtimeSeatRow
{
    public int SeatId { get; init; }
    public int RoomId { get; init; }
    public string RowLabel { get; init; } = string.Empty;
    public int SeatNumber { get; init; }
    public string SeatCode { get; init; } = string.Empty;
    public string SeatType { get; init; } = string.Empty;
    public decimal PriceMultiplier { get; init; }
    public int RowIndex { get; init; }
    public int ColIndex { get; init; }
    public bool IsActive { get; init; }
    public bool IsBooked { get; init; }
}

public sealed record BookingCustomerSummary
{
    private static readonly CultureInfo ViCulture = CultureInfo.GetCultureInfo("vi-VN");

    public int CustomerId { get; init; }
    public int? UserId { get; init; }
    public string FullName { get; init; } = string.Empty;
    public string Phone { get; init; } = string.Empty;
    public string? Email { get; init; }
    public int LoyaltyPoints { get; init; }
    public decimal TotalSpent { get; init; }
    public string Tier { get; init; } = "Member";

    public string TierDisplay => Tier switch
    {
        "Silver" => "Hạng Bạc (Silver)",
        "Gold" => "Hạng Vàng (Gold)",
        "Platinum" => "Bạch kim (Platinum)",
        _ => "Thành viên (Member)"
    };

    public string LoyaltyPointsText => $"{LoyaltyPoints.ToString("N0", ViCulture)} điểm";
    public string TotalSpentText => FormatHelper.FormatVnd(TotalSpent);
}

public sealed record BookingCreateInput(
    int ShowtimeId,
    IReadOnlyList<int> SeatIds,
    int? StaffUserId,
    int? CustomerId,
    string BookingChannel,
    int PointsUsed,
    string PaymentMethod,
    decimal CashReceived,
    string? Note);

public sealed record BookingCreateResult(
    bool Succeeded,
    int OrderId,
    string OrderCode,
    IReadOnlyList<string> TicketCodes,
    decimal SubTotalAmount,
    int PointsUsed,
    decimal DiscountAmount,
    decimal TotalAmount,
    decimal CashReceived,
    decimal ChangeAmount,
    int EarnedPoints,
    int? UpdatedCustomerPoints,
    string? UpdatedCustomerTier,
    string? ErrorMessage);

/// <summary>
/// Lõi truy vấn và giao dịch đặt vé SQLite bằng Dapper dùng chung cho Staff POS (Counter) và Customer Self-Booking (SelfService).
/// </summary>
public static class BookingData
{
    public const decimal PointRedeemValueVnd = 1000m;
    public const decimal SpendPerEarnedPointVnd = 10000m;
    public const decimal MaxPointDiscountRatio = 0.50m;
    public const int MaxSeatsPerOrder = 10;

    private static readonly HashSet<string> ValidPaymentMethods =
        ["Cash", "BankTransfer", "Card", "EWallet"];

    private sealed class ShowtimeTxRow
    {
        public int ShowtimeId { get; init; }
        public int RoomId { get; init; }
        public decimal BaseTicketPrice { get; init; }
        public decimal RoomSurcharge { get; init; }
        public string Status { get; init; } = string.Empty;
        public bool RoomIsActive { get; init; }
    }

    private sealed class SeatTxRow
    {
        public int SeatId { get; init; }
        public int RoomId { get; init; }
        public string SeatCode { get; init; } = string.Empty;
        public decimal PriceMultiplier { get; init; }
        public bool IsActive { get; init; }
    }

    private sealed class CustomerTxRow
    {
        public int CustomerId { get; init; }
        public int LoyaltyPoints { get; init; }
        public decimal TotalSpent { get; init; }
    }

    public static decimal CalculateSeatUnitPrice(decimal baseTicketPrice, decimal roomSurcharge, decimal priceMultiplier)
        => Math.Round((baseTicketPrice + roomSurcharge) * priceMultiplier, 0, MidpointRounding.AwayFromZero);

    public static int CalculateMaxRedeemablePoints(int availablePoints, decimal subTotalAmount)
    {
        if (availablePoints <= 0 || subTotalAmount <= 0m)
        {
            return 0;
        }

        int capBySubTotal = (int)Math.Floor((subTotalAmount * MaxPointDiscountRatio) / PointRedeemValueVnd);
        return Math.Max(0, Math.Min(availablePoints, capBySubTotal));
    }

    public static int CalculateEarnedPoints(decimal totalAmount)
        => totalAmount <= 0m ? 0 : (int)Math.Floor(totalAmount / SpendPerEarnedPointVnd);

    public static string ComputeTier(decimal totalSpent) => totalSpent switch
    {
        >= 10_000_000m => "Platinum",
        >= 5_000_000m => "Gold",
        >= 1_000_000m => "Silver",
        _ => "Member"
    };

    public static List<OpenShowtimeOption> LoadOpenShowtimes(int? movieId = null)
    {
        using SqliteConnection conn = DatabaseHelper.OpenConnection();
        const string sql = """
            SELECT s.ShowtimeId,
                   s.MovieId,
                   m.Title AS MovieTitle,
                   m.AgeRating,
                   m.DurationMinutes,
                   m.Language,
                   m.PosterPath,
                   COALESCE((SELECT GROUP_CONCAT(g.Name, ', ')
                             FROM MovieGenres mg INNER JOIN Genres g ON g.GenreId = mg.GenreId
                             WHERE mg.MovieId = m.MovieId), 'Tổng hợp') AS GenresText,
                   s.RoomId,
                   r.Name AS RoomName,
                   r.RoomType,
                   s.StartTime AS StartTimeSql,
                   s.EndTime AS EndTimeSql,
                   s.BaseTicketPrice,
                   r.BasePriceSurcharge AS RoomSurcharge,
                   s.Format,
                   (SELECT COUNT(*) FROM Seats se WHERE se.RoomId = s.RoomId AND se.IsActive = 1) AS ActiveSeatsCount,
                   (SELECT COUNT(*) FROM Tickets t WHERE t.ShowtimeId = s.ShowtimeId AND t.Status IN ('Booked', 'CheckedIn')) AS BookedSeatsCount
            FROM Showtimes s
            INNER JOIN Movies m ON m.MovieId = s.MovieId
            INNER JOIN Rooms r ON r.RoomId = s.RoomId
            WHERE s.Status = 'OpenForSale'
              AND r.IsActive = 1
              AND (@mid IS NULL OR s.MovieId = @mid)
            ORDER BY s.StartTime ASC, r.RoomId ASC;
            """;

        return conn.Query<OpenShowtimeOption>(sql, new { mid = movieId }).AsList();
    }

    public static List<ShowtimeSeatRow> LoadShowtimeSeatMap(int showtimeId)
    {
        using SqliteConnection conn = DatabaseHelper.OpenConnection();
        const string sql = """
            SELECT se.SeatId,
                   se.RoomId,
                   se.RowLabel,
                   se.SeatNumber,
                   se.SeatCode,
                   se.SeatType,
                   se.PriceMultiplier,
                   se.RowIndex,
                   se.ColIndex,
                   se.IsActive,
                   CASE WHEN EXISTS (
                       SELECT 1
                       FROM Tickets t
                       WHERE t.ShowtimeId = @sid
                         AND t.SeatId = se.SeatId
                         AND t.Status IN ('Booked', 'CheckedIn')
                   ) THEN 1 ELSE 0 END AS IsBooked
            FROM Showtimes s
            INNER JOIN Seats se ON se.RoomId = s.RoomId
            WHERE s.ShowtimeId = @sid
            ORDER BY se.RowIndex ASC, se.ColIndex ASC;
            """;

        return conn.Query<ShowtimeSeatRow>(sql, new { sid = showtimeId }).AsList();
    }

    public static BookingCustomerSummary? FindCustomerByPhone(string phone)
    {
        string cleanPhone = (phone ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(cleanPhone))
        {
            return null;
        }

        using SqliteConnection conn = DatabaseHelper.OpenConnection();
        const string sql = """
            SELECT CustomerId, UserId, FullName, Phone, Email, LoyaltyPoints, TotalSpent, Tier
            FROM Customers
            WHERE Phone = @ph
            LIMIT 1;
            """;
        return conn.QueryFirstOrDefault<BookingCustomerSummary>(sql, new { ph = cleanPhone });
    }

    public static BookingCustomerSummary? FindCustomerById(int customerId)
    {
        using SqliteConnection conn = DatabaseHelper.OpenConnection();
        const string sql = """
            SELECT CustomerId, UserId, FullName, Phone, Email, LoyaltyPoints, TotalSpent, Tier
            FROM Customers
            WHERE CustomerId = @cid
            LIMIT 1;
            """;
        return conn.QueryFirstOrDefault<BookingCustomerSummary>(sql, new { cid = customerId });
    }

    public static BookingCreateResult CreateOrder(BookingCreateInput input)
    {
        List<int> distinctSeatIds = input.SeatIds?.Distinct().ToList() ?? [];
        if (distinctSeatIds.Count == 0)
        {
            return Fail("Vui lòng chọn ít nhất 1 ghế trước khi thanh toán.");
        }

        if (distinctSeatIds.Count > MaxSeatsPerOrder)
        {
            return Fail($"Mỗi hóa đơn chỉ được đặt tối đa {MaxSeatsPerOrder} ghế.");
        }

        if (input.BookingChannel != "Counter" && input.BookingChannel != "SelfService")
        {
            return Fail("Kênh đặt vé không hợp lệ.");
        }

        if (!ValidPaymentMethods.Contains(input.PaymentMethod))
        {
            return Fail("Phương thức thanh toán không hợp lệ.");
        }

        if (input.BookingChannel == "Counter" && (!input.StaffUserId.HasValue || input.StaffUserId.Value <= 0))
        {
            return Fail("Thiếu thông tin nhân viên lập hóa đơn tại quầy.");
        }

        if (input.BookingChannel == "SelfService")
        {
            if (!input.CustomerId.HasValue || input.CustomerId.Value <= 0)
            {
                return Fail("Vui lòng đăng nhập tài khoản hội viên để đặt vé trực tuyến.");
            }

            if (input.PaymentMethod == "Cash")
            {
                return Fail("Đặt vé trực tuyến không hỗ trợ thanh toán tiền mặt.");
            }
        }

        if (input.PointsUsed < 0)
        {
            return Fail("Số điểm thưởng sử dụng không hợp lệ.");
        }

        if (input.PointsUsed > 0 && !input.CustomerId.HasValue)
        {
            return Fail("Chỉ hội viên đã gắn vào hóa đơn mới có thể sử dụng điểm thưởng.");
        }

        try
        {
            using SqliteConnection conn = DatabaseHelper.OpenConnection();
            using SqliteTransaction tx = conn.BeginTransaction();

            ShowtimeTxRow? showtime = conn.QueryFirstOrDefault<ShowtimeTxRow>(
                """
                SELECT s.ShowtimeId, s.RoomId, s.BaseTicketPrice, r.BasePriceSurcharge AS RoomSurcharge,
                       s.Status, r.IsActive AS RoomIsActive
                FROM Showtimes s
                INNER JOIN Rooms r ON r.RoomId = s.RoomId
                WHERE s.ShowtimeId = @sid
                LIMIT 1;
                """,
                new { sid = input.ShowtimeId },
                tx);

            if (showtime is null)
            {
                return Fail("Không tìm thấy suất chiếu đã chọn.");
            }

            if (showtime.Status != "OpenForSale" || !showtime.RoomIsActive)
            {
                return Fail("Suất chiếu này hiện không còn mở bán vé.");
            }

            List<SeatTxRow> seats = conn.Query<SeatTxRow>(
                """
                SELECT SeatId, RoomId, SeatCode, PriceMultiplier, IsActive
                FROM Seats
                WHERE SeatId IN @ids
                ORDER BY RowIndex ASC, ColIndex ASC;
                """,
                new { ids = distinctSeatIds },
                tx).AsList();

            if (seats.Count != distinctSeatIds.Count || seats.Any(s => s.RoomId != showtime.RoomId))
            {
                return Fail("Có ghế không hợp lệ hoặc không thuộc phòng chiếu của suất này.");
            }

            List<string> maintenanceCodes = seats.Where(s => !s.IsActive).Select(s => s.SeatCode).ToList();
            if (maintenanceCodes.Count > 0)
            {
                return Fail($"Ghế {string.Join(", ", maintenanceCodes)} đang bảo trì, không thể đặt.");
            }

            List<string> alreadyBookedCodes = conn.Query<string>(
                """
                SELECT se.SeatCode
                FROM Tickets t
                INNER JOIN Seats se ON se.SeatId = t.SeatId
                WHERE t.ShowtimeId = @sid
                  AND t.SeatId IN @ids
                  AND t.Status IN ('Booked', 'CheckedIn')
                ORDER BY se.RowIndex ASC, se.ColIndex ASC;
                """,
                new { sid = showtime.ShowtimeId, ids = distinctSeatIds },
                tx).AsList();

            if (alreadyBookedCodes.Count > 0)
            {
                return Fail($"Ghế {string.Join(", ", alreadyBookedCodes)} vừa có người khác đặt. Vui lòng tải lại sơ đồ ghế.");
            }

            List<(SeatTxRow Seat, decimal UnitPrice)> pricedSeats = seats
                .Select(s => (s, CalculateSeatUnitPrice(showtime.BaseTicketPrice, showtime.RoomSurcharge, s.PriceMultiplier)))
                .ToList();

            decimal subTotal = pricedSeats.Sum(x => x.UnitPrice);

            CustomerTxRow? customer = null;
            int pointsUsed = 0;
            decimal discountAmount = 0m;

            if (input.CustomerId.HasValue)
            {
                customer = conn.QueryFirstOrDefault<CustomerTxRow>(
                    "SELECT CustomerId, LoyaltyPoints, TotalSpent FROM Customers WHERE CustomerId = @cid LIMIT 1;",
                    new { cid = input.CustomerId.Value },
                    tx);

                if (customer is null)
                {
                    return Fail("Không tìm thấy hồ sơ hội viên đã chọn.");
                }

                int maxPoints = CalculateMaxRedeemablePoints(customer.LoyaltyPoints, subTotal);
                if (input.PointsUsed > maxPoints)
                {
                    return Fail($"Số điểm áp dụng tối đa cho đơn này là {maxPoints} điểm (tối đa 50% tổng tiền và không vượt điểm hiện có).");
                }

                pointsUsed = input.PointsUsed;
                discountAmount = pointsUsed * PointRedeemValueVnd;
            }

            decimal totalAmount = Math.Max(0m, subTotal - discountAmount);
            decimal cashReceived = 0m;
            decimal changeAmount = 0m;

            if (input.PaymentMethod == "Cash")
            {
                if (input.CashReceived < totalAmount)
                {
                    return Fail($"Tiền khách đưa ({FormatHelper.FormatVnd(input.CashReceived)}) chưa đủ tổng thanh toán ({FormatHelper.FormatVnd(totalAmount)}).");
                }

                cashReceived = Math.Round(input.CashReceived, 0, MidpointRounding.AwayFromZero);
                changeAmount = cashReceived - totalAmount;
            }

            string orderCode = GenerateUniqueOrderCode(conn, tx);
            string? cleanNote = string.IsNullOrWhiteSpace(input.Note) ? null : input.Note.Trim();
            int? staffUserId = input.BookingChannel == "Counter" ? input.StaffUserId : null;

            int orderId = conn.ExecuteScalar<int>(
                """
                INSERT INTO Orders (
                    OrderCode, StaffUserId, CustomerId, BookingChannel, TotalTickets,
                    SubTotalAmount, PointsUsed, DiscountAmount, TotalAmount,
                    PaymentMethod, CashReceived, ChangeAmount, Status, Note, CreatedAt, PaidAt)
                VALUES (
                    @OrderCode, @StaffUserId, @CustomerId, @BookingChannel, @TotalTickets,
                    @SubTotalAmount, @PointsUsed, @DiscountAmount, @TotalAmount,
                    @PaymentMethod, @CashReceived, @ChangeAmount, 'Paid', @Note,
                    datetime('now', 'localtime'), datetime('now', 'localtime'));
                SELECT last_insert_rowid();
                """,
                new
                {
                    OrderCode = orderCode,
                    StaffUserId = staffUserId,
                    CustomerId = customer?.CustomerId,
                    input.BookingChannel,
                    TotalTickets = pricedSeats.Count,
                    SubTotalAmount = subTotal,
                    PointsUsed = pointsUsed,
                    DiscountAmount = discountAmount,
                    TotalAmount = totalAmount,
                    input.PaymentMethod,
                    CashReceived = cashReceived,
                    ChangeAmount = changeAmount,
                    Note = cleanNote
                },
                tx);

            string dateTag = DateTime.Now.ToString("yyMMdd", CultureInfo.InvariantCulture);
            List<string> ticketCodes = [];
            foreach ((SeatTxRow seat, decimal unitPrice) in pricedSeats)
            {
                string ticketCode = $"VE-{dateTag}-{orderId:D4}-{seat.SeatCode}";
                conn.Execute(
                    """
                    INSERT INTO Tickets (TicketCode, OrderId, ShowtimeId, SeatId, UnitPrice, Status, CheckedInAt, CheckedInByUserId)
                    VALUES (@TicketCode, @OrderId, @ShowtimeId, @SeatId, @UnitPrice, 'Booked', NULL, NULL);
                    """,
                    new
                    {
                        TicketCode = ticketCode,
                        OrderId = orderId,
                        ShowtimeId = showtime.ShowtimeId,
                        seat.SeatId,
                        UnitPrice = unitPrice
                    },
                    tx);
                ticketCodes.Add(ticketCode);
            }

            int earnedPoints = 0;
            int? updatedPoints = null;
            string? updatedTier = null;

            if (customer is not null)
            {
                earnedPoints = CalculateEarnedPoints(totalAmount);
                int nextPoints = Math.Max(0, customer.LoyaltyPoints - pointsUsed + earnedPoints);
                decimal nextSpent = customer.TotalSpent + totalAmount;
                string nextTier = ComputeTier(nextSpent);

                conn.Execute(
                    """
                    UPDATE Customers
                    SET LoyaltyPoints = @pts, TotalSpent = @spent, Tier = @tier
                    WHERE CustomerId = @cid;
                    """,
                    new { pts = nextPoints, spent = nextSpent, tier = nextTier, cid = customer.CustomerId },
                    tx);

                updatedPoints = nextPoints;
                updatedTier = nextTier;
            }

            tx.Commit();
            return new BookingCreateResult(
                true,
                orderId,
                orderCode,
                ticketCodes,
                subTotal,
                pointsUsed,
                discountAmount,
                totalAmount,
                cashReceived,
                changeAmount,
                earnedPoints,
                updatedPoints,
                updatedTier,
                null);
        }
        catch (SqliteException ex)
        {
            return Fail($"Không thể tạo hóa đơn đặt vé: {ex.Message}");
        }
    }

    private static string GenerateUniqueOrderCode(SqliteConnection conn, SqliteTransaction tx)
    {
        string today = DateTime.Now.ToString("yyyyMMdd", CultureInfo.InvariantCulture);
        int seq = conn.ExecuteScalar<int>("SELECT COALESCE(MAX(OrderId), 0) + 1 FROM Orders;", transaction: tx);

        while (true)
        {
            string candidate = $"HD-{today}-{seq:D4}";
            bool exists = conn.ExecuteScalar<int?>(
                "SELECT 1 FROM Orders WHERE OrderCode = @code LIMIT 1;",
                new { code = candidate },
                tx).HasValue;
            if (!exists)
            {
                return candidate;
            }

            seq++;
        }
    }

    private static BookingCreateResult Fail(string message)
        => new(false, 0, string.Empty, [], 0m, 0, 0m, 0m, 0m, 0m, 0, null, null, message);
}
