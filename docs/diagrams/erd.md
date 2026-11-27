# Sơ đồ Thực thể Liên kết (ERD) — CINEVERSE CINEMA (`CinemaManageDB.sql`)

Cơ sở dữ liệu SQLite (`CinemaManageProject/Database/cinema.db`) gồm **10 bảng chuẩn hóa** và **13 ràng buộc khóa ngoại** được thực thi nghiêm ngặt qua `PRAGMA foreign_keys = ON;`.

## 1. Sơ đồ ERD (10 Bảng & 13 Khóa ngoại)

```mermaid
erDiagram
    Users ||--o| Customers : "UserId (SET NULL)"
    Users ||--o{ Showtimes : "CreatedByUserId (RESTRICT)"
    Users ||--o{ Orders : "StaffUserId (RESTRICT)"
    Users ||--o{ Tickets : "CheckedInByUserId (RESTRICT)"

    Customers ||--o{ Orders : "CustomerId (SET NULL)"

    Movies ||--o{ MovieGenres : "MovieId (CASCADE)"
    Genres ||--o{ MovieGenres : "GenreId (CASCADE)"
    Movies ||--o{ Showtimes : "MovieId (RESTRICT)"

    Rooms ||--o{ Seats : "RoomId (CASCADE)"
    Rooms ||--o{ Showtimes : "RoomId (RESTRICT)"

    Orders ||--|{ Tickets : "OrderId (CASCADE)"
    Showtimes ||--o{ Tickets : "ShowtimeId (RESTRICT)"
    Seats ||--o{ Tickets : "SeatId (RESTRICT)"

    Users {
        INTEGER UserId PK
        TEXT Username UK
        TEXT Password
        TEXT FullName
        TEXT Email
        TEXT Phone
        TEXT Role "Admin|Staff|Customer"
        INTEGER IsActive "0|1"
        TEXT CreatedAt
        TEXT LastLoginAt
    }

    Customers {
        INTEGER CustomerId PK
        INTEGER UserId FK "NULL, UNIQUE"
        TEXT FullName
        TEXT Phone UK
        TEXT Email
        TEXT DateOfBirth
        INTEGER LoyaltyPoints
        NUMERIC TotalSpent
        TEXT Tier "Member|Silver|Gold|Platinum"
        TEXT CreatedAt
    }

    Genres {
        INTEGER GenreId PK
        TEXT Name UK
    }

    Movies {
        INTEGER MovieId PK
        TEXT Title
        TEXT OriginalTitle
        TEXT Director
        TEXT Cast
        INTEGER DurationMinutes
        TEXT ReleaseDate
        TEXT EndDate
        TEXT AgeRating "P|K|T13|T16|T18"
        TEXT Language
        TEXT Description
        TEXT PosterPath
        TEXT Status "ComingSoon|NowShowing|Ended"
        TEXT CreatedAt
    }

    MovieGenres {
        INTEGER MovieId PK, FK
        INTEGER GenreId PK, FK
    }

    Rooms {
        INTEGER RoomId PK
        TEXT Name UK
        TEXT RoomType "Standard2D|Standard3D|IMAX|VIP"
        INTEGER TotalRows
        INTEGER TotalCols
        NUMERIC BasePriceSurcharge
        INTEGER IsActive "0|1"
        TEXT CreatedAt
    }

    Seats {
        INTEGER SeatId PK
        INTEGER RoomId FK
        TEXT RowLabel
        INTEGER SeatNumber
        TEXT SeatCode
        TEXT SeatType "Standard|VIP|Couple"
        NUMERIC PriceMultiplier
        INTEGER RowIndex
        INTEGER ColIndex
        INTEGER IsActive "0|1"
    }

    Showtimes {
        INTEGER ShowtimeId PK
        INTEGER MovieId FK
        INTEGER RoomId FK
        TEXT StartTime
        TEXT EndTime
        NUMERIC BaseTicketPrice
        TEXT Format
        TEXT Status "Scheduled|OpenForSale|Completed|Cancelled"
        INTEGER CreatedByUserId FK
        TEXT CreatedAt
    }

    Orders {
        INTEGER OrderId PK
        TEXT OrderCode UK
        INTEGER StaffUserId FK "NULL if SelfService"
        INTEGER CustomerId FK "NULL if walk-in"
        TEXT BookingChannel "Counter|SelfService"
        INTEGER TotalTickets
        NUMERIC SubTotalAmount
        INTEGER PointsUsed
        NUMERIC DiscountAmount
        NUMERIC TotalAmount
        TEXT PaymentMethod "Cash|BankTransfer|Card|EWallet"
        NUMERIC CashReceived
        NUMERIC ChangeAmount
        TEXT Status "Paid|Cancelled|PartiallyRefunded"
        TEXT Note
        TEXT CreatedAt
        TEXT PaidAt
    }

    Tickets {
        INTEGER TicketId PK
        TEXT TicketCode UK
        INTEGER OrderId FK
        INTEGER ShowtimeId FK
        INTEGER SeatId FK
        NUMERIC UnitPrice
        TEXT Status "Booked|CheckedIn|Cancelled"
        TEXT CheckedInAt
        INTEGER CheckedInByUserId FK "NULL until CheckedIn"
    }
```

---

## 2. Tổng hợp 13 Ràng buộc Khóa ngoại & Chính sách Xóa (`ON DELETE`)

| STT | Bảng con (`FK`) | Tham chiếu (`PK`) | Chính sách `ON DELETE` | Ý nghĩa nghiệp vụ |
| :-: | :--- | :--- | :---: | :--- |
| 1 | `Customers.UserId` *(UNIQUE, NULL)* | `Users.UserId` | `SET NULL` | Cho phép hội viên tại quầy (`UserId = NULL`); nếu gỡ tài khoản app thì hồ sơ chi tiêu không bị mất dây chuyền. |
| 2 | `MovieGenres.MovieId` | `Movies.MovieId` | `CASCADE` | Xóa phim (khi chưa có suất chiếu) sẽ tự dọn liên kết thể loại. |
| 3 | `MovieGenres.GenreId` | `Genres.GenreId` | `CASCADE` | Xóa thể loại sẽ tự dọn liên kết trong bảng nối. |
| 4 | `Seats.RoomId` | `Rooms.RoomId` | `CASCADE` | Ghế là thành phần phụ thuộc hoàn toàn vào phòng chiếu. |
| 5 | `Showtimes.MovieId` | `Movies.MovieId` | `RESTRICT` | Chặn xóa phim đã phát sinh lịch chiếu để bảo toàn lịch sử. |
| 6 | `Showtimes.RoomId` | `Rooms.RoomId` | `RESTRICT` | Chặn xóa phòng đã có lịch chiếu. |
| 7 | `Showtimes.CreatedByUserId` | `Users.UserId` | `RESTRICT` | Chặn xóa tài khoản quản trị đã tạo suất chiếu. |
| 8 | `Orders.StaffUserId` *(NULL)* | `Users.UserId` | `RESTRICT` | Chặn xóa nhân viên đã lập hóa đơn quầy (chỉ cho khóa tài khoản `IsActive = 0`). |
| 9 | `Orders.CustomerId` *(NULL)* | `Customers.CustomerId` | `SET NULL` | Cho phép khách vãng lai (`CustomerId = NULL`) và giữ nguyên hóa đơn kế toán. |
| 10 | `Tickets.OrderId` | `Orders.OrderId` | `CASCADE` | Vé thuộc về một hóa đơn duy nhất; xóa hóa đơn nháp/test sẽ dọn luôn vé con. |
| 11 | `Tickets.ShowtimeId` | `Showtimes.ShowtimeId` | `RESTRICT` | Chặn xóa suất chiếu đã phát sinh vé bán. |
| 12 | `Tickets.SeatId` | `Seats.SeatId` | `RESTRICT` | Chặn xóa ghế đã có lịch sử vé đặt. |
| 13 | `Tickets.CheckedInByUserId` *(NULL)* | `Users.UserId` | `RESTRICT` | Chặn xóa nhân viên đã thực hiện soát vé check-in. |
