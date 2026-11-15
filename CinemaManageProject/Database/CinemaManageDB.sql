-- ============================================================================
-- CinemaManageDB - SQLite Schema (10 Bảng Chuẩn hóa cho 6 Đối tượng Cốt lõi)
-- ============================================================================
PRAGMA foreign_keys = ON;

CREATE TABLE IF NOT EXISTS Users (
    UserId       INTEGER PRIMARY KEY AUTOINCREMENT,
    Username     TEXT    NOT NULL UNIQUE,
    Password     TEXT    NOT NULL,
    FullName     TEXT    NOT NULL,
    Email        TEXT    NULL,
    Phone        TEXT    NULL,
    Role         TEXT    NOT NULL DEFAULT 'Customer' CHECK (Role IN ('Admin', 'Staff', 'Customer')),
    IsActive     INTEGER NOT NULL DEFAULT 1 CHECK (IsActive IN (0, 1)),
    CreatedAt    TEXT    NOT NULL DEFAULT (datetime('now', 'localtime')),
    LastLoginAt  TEXT    NULL
);

CREATE TABLE IF NOT EXISTS Customers (
    CustomerId    INTEGER PRIMARY KEY AUTOINCREMENT,
    UserId        INTEGER NULL UNIQUE REFERENCES Users(UserId) ON DELETE SET NULL,
    FullName      TEXT    NOT NULL,
    Phone         TEXT    NOT NULL UNIQUE,
    Email         TEXT    NULL,
    DateOfBirth   TEXT    NULL,
    LoyaltyPoints INTEGER NOT NULL DEFAULT 0 CHECK (LoyaltyPoints >= 0),
    TotalSpent    NUMERIC NOT NULL DEFAULT 0 CHECK (TotalSpent >= 0),
    Tier          TEXT    NOT NULL DEFAULT 'Member' CHECK (Tier IN ('Member', 'Silver', 'Gold', 'Platinum')),
    CreatedAt     TEXT    NOT NULL DEFAULT (datetime('now', 'localtime'))
);

CREATE TABLE IF NOT EXISTS Genres (
    GenreId INTEGER PRIMARY KEY AUTOINCREMENT,
    Name    TEXT    NOT NULL UNIQUE
);

CREATE TABLE IF NOT EXISTS Movies (
    MovieId         INTEGER PRIMARY KEY AUTOINCREMENT,
    Title           TEXT    NOT NULL,
    OriginalTitle   TEXT    NULL,
    Director        TEXT    NULL,
    Cast            TEXT    NULL,
    DurationMinutes INTEGER NOT NULL CHECK (DurationMinutes > 0),
    ReleaseDate     TEXT    NOT NULL,
    EndDate         TEXT    NULL,
    AgeRating       TEXT    NOT NULL DEFAULT 'P' CHECK (AgeRating IN ('P', 'K', 'T13', 'T16', 'T18')),
    Language        TEXT    NOT NULL DEFAULT 'Phụ đề Tiếng Việt',
    Description     TEXT    NULL,
    PosterPath      TEXT    NULL,
    Status          TEXT    NOT NULL DEFAULT 'NowShowing' CHECK (Status IN ('ComingSoon', 'NowShowing', 'Ended')),
    CreatedAt       TEXT    NOT NULL DEFAULT (datetime('now', 'localtime'))
);

CREATE TABLE IF NOT EXISTS MovieGenres (
    MovieId INTEGER NOT NULL REFERENCES Movies(MovieId) ON DELETE CASCADE,
    GenreId INTEGER NOT NULL REFERENCES Genres(GenreId) ON DELETE CASCADE,
    PRIMARY KEY (MovieId, GenreId)
);

CREATE TABLE IF NOT EXISTS Rooms (
    RoomId             INTEGER PRIMARY KEY AUTOINCREMENT,
    Name               TEXT    NOT NULL UNIQUE,
    RoomType           TEXT    NOT NULL DEFAULT 'Standard2D' CHECK (RoomType IN ('Standard2D', 'Standard3D', 'IMAX', 'VIP')),
    TotalRows          INTEGER NOT NULL CHECK (TotalRows > 0),
    TotalCols          INTEGER NOT NULL CHECK (TotalCols > 0),
    BasePriceSurcharge NUMERIC NOT NULL DEFAULT 0 CHECK (BasePriceSurcharge >= 0),
    IsActive           INTEGER NOT NULL DEFAULT 1 CHECK (IsActive IN (0, 1)),
    CreatedAt          TEXT    NOT NULL DEFAULT (datetime('now', 'localtime'))
);

CREATE TABLE IF NOT EXISTS Seats (
    SeatId          INTEGER PRIMARY KEY AUTOINCREMENT,
    RoomId          INTEGER NOT NULL REFERENCES Rooms(RoomId) ON DELETE CASCADE,
    RowLabel        TEXT    NOT NULL,
    SeatNumber      INTEGER NOT NULL CHECK (SeatNumber > 0),
    SeatCode        TEXT    NOT NULL,
    SeatType        TEXT    NOT NULL DEFAULT 'Standard' CHECK (SeatType IN ('Standard', 'VIP', 'Couple')),
    PriceMultiplier NUMERIC NOT NULL DEFAULT 1.00 CHECK (PriceMultiplier > 0),
    RowIndex        INTEGER NOT NULL CHECK (RowIndex >= 0),
    ColIndex        INTEGER NOT NULL CHECK (ColIndex >= 0),
    IsActive        INTEGER NOT NULL DEFAULT 1 CHECK (IsActive IN (0, 1)),
    UNIQUE (RoomId, SeatCode),
    UNIQUE (RoomId, RowIndex, ColIndex)
);

CREATE TABLE IF NOT EXISTS Showtimes (
    ShowtimeId      INTEGER PRIMARY KEY AUTOINCREMENT,
    MovieId         INTEGER NOT NULL REFERENCES Movies(MovieId) ON DELETE RESTRICT,
    RoomId          INTEGER NOT NULL REFERENCES Rooms(RoomId) ON DELETE RESTRICT,
    StartTime       TEXT    NOT NULL,
    EndTime         TEXT    NOT NULL,
    BaseTicketPrice NUMERIC NOT NULL CHECK (BaseTicketPrice >= 0),
    Format          TEXT    NOT NULL DEFAULT '2D Phụ đề',
    Status          TEXT    NOT NULL DEFAULT 'OpenForSale' CHECK (Status IN ('Scheduled', 'OpenForSale', 'Completed', 'Cancelled')),
    CreatedByUserId INTEGER NOT NULL REFERENCES Users(UserId) ON DELETE RESTRICT,
    CreatedAt       TEXT    NOT NULL DEFAULT (datetime('now', 'localtime'))
);

CREATE TABLE IF NOT EXISTS Orders (
    OrderId        INTEGER PRIMARY KEY AUTOINCREMENT,
    OrderCode      TEXT    NOT NULL UNIQUE,
    StaffUserId    INTEGER NULL REFERENCES Users(UserId) ON DELETE RESTRICT,
    CustomerId     INTEGER NULL REFERENCES Customers(CustomerId) ON DELETE SET NULL,
    BookingChannel TEXT    NOT NULL DEFAULT 'Counter' CHECK (BookingChannel IN ('Counter', 'SelfService')),
    TotalTickets   INTEGER NOT NULL CHECK (TotalTickets > 0),
    SubTotalAmount NUMERIC NOT NULL CHECK (SubTotalAmount >= 0),
    PointsUsed     INTEGER NOT NULL DEFAULT 0 CHECK (PointsUsed >= 0),
    DiscountAmount NUMERIC NOT NULL DEFAULT 0 CHECK (DiscountAmount >= 0),
    TotalAmount    NUMERIC NOT NULL CHECK (TotalAmount >= 0),
    PaymentMethod  TEXT    NOT NULL DEFAULT 'Cash' CHECK (PaymentMethod IN ('Cash', 'BankTransfer', 'Card', 'EWallet')),
    CashReceived   NUMERIC NOT NULL DEFAULT 0 CHECK (CashReceived >= 0),
    ChangeAmount   NUMERIC NOT NULL DEFAULT 0 CHECK (ChangeAmount >= 0),
    Status         TEXT    NOT NULL DEFAULT 'Paid' CHECK (Status IN ('Paid', 'Cancelled', 'PartiallyRefunded')),
    Note           TEXT    NULL,
    CreatedAt      TEXT    NOT NULL DEFAULT (datetime('now', 'localtime')),
    PaidAt         TEXT    NULL
);

CREATE TABLE IF NOT EXISTS Tickets (
    TicketId          INTEGER PRIMARY KEY AUTOINCREMENT,
    TicketCode        TEXT    NOT NULL UNIQUE,
    OrderId           INTEGER NOT NULL REFERENCES Orders(OrderId) ON DELETE CASCADE,
    ShowtimeId        INTEGER NOT NULL REFERENCES Showtimes(ShowtimeId) ON DELETE RESTRICT,
    SeatId            INTEGER NOT NULL REFERENCES Seats(SeatId) ON DELETE RESTRICT,
    UnitPrice         NUMERIC NOT NULL CHECK (UnitPrice >= 0),
    Status            TEXT    NOT NULL DEFAULT 'Booked' CHECK (Status IN ('Booked', 'CheckedIn', 'Cancelled')),
    CheckedInAt       TEXT    NULL,
    CheckedInByUserId INTEGER NULL REFERENCES Users(UserId) ON DELETE RESTRICT
);
