-- ============================================================================
-- SeedData.sql - Dữ liệu mẫu ban đầu cho cinema.db (1 Admin, 3 Staff, 3 Customer)
-- ============================================================================
PRAGMA foreign_keys = ON;
BEGIN TRANSACTION;

-- 1. Tài khoản đăng nhập (1 Admin, 3 Staff, 3 Customer - mật khẩu lưu trực tiếp cho demo)
INSERT INTO Users (UserId, Username, Password, FullName, Email, Phone, Role, IsActive, CreatedAt) VALUES
    (1, 'admin',  'Admin@123', 'Trần Quản Trị',   'admin@cineverse.vn',  '0900000001', 'Admin',    1, datetime('now', 'localtime', '-30 days')),
    (2, 'staff1', 'Staff@123', 'Nguyễn Minh Khôi', 'staff1@cineverse.vn', '0900000011', 'Staff',    1, datetime('now', 'localtime', '-25 days')),
    (3, 'staff2', 'Staff@123', 'Trần Thảo Vy',     'staff2@cineverse.vn', '0900000012', 'Staff',    1, datetime('now', 'localtime', '-20 days')),
    (4, 'staff3', 'Staff@123', 'Lê Hoàng Nam',     'staff3@cineverse.vn', '0900000013', 'Staff',    1, datetime('now', 'localtime', '-15 days')),
    (5, 'khach1', 'Khach@123', 'Phạm Anh Tuấn',    'tuan.pham@gmail.com', '0901000001', 'Customer', 1, datetime('now', 'localtime', '-14 days')),
    (6, 'khach2', 'Khach@123', 'Võ Ngọc Hân',      'han.vo@gmail.com',    '0901000002', 'Customer', 1, datetime('now', 'localtime', '-10 days')),
    (7, 'khach3', 'Khach@123', 'Đặng Quốc Bảo',    'bao.dang@gmail.com',  '0901000003', 'Customer', 1, datetime('now', 'localtime', '-5 days'));

-- 2. Hồ sơ Khách hàng thành viên (liên kết 1-0..1 với 3 tài khoản Customer)
INSERT INTO Customers (CustomerId, UserId, FullName, Phone, Email, DateOfBirth, LoyaltyPoints, TotalSpent, Tier, CreatedAt) VALUES
    (1, 5, 'Phạm Anh Tuấn', '0901000001', 'tuan.pham@gmail.com', '2002-05-14', 180, 2450000, 'Silver', datetime('now', 'localtime', '-14 days')),
    (2, 6, 'Võ Ngọc Hân',   '0901000002', 'han.vo@gmail.com',    '2001-09-22', 420, 6100000, 'Gold',   datetime('now', 'localtime', '-10 days')),
    (3, 7, 'Đặng Quốc Bảo', '0901000003', 'bao.dang@gmail.com',  '2004-01-10',  50,  480000, 'Member', datetime('now', 'localtime', '-5 days'));

-- 3. Thể loại phim
INSERT INTO Genres (GenreId, Name) VALUES
    (1, 'Hành động'),
    (2, 'Hoạt hình'),
    (3, 'Kinh dị'),
    (4, 'Tình cảm'),
    (5, 'Khoa học viễn tưởng'),
    (6, 'Hài hước');

-- 4. Phim mẫu (4 Đang chiếu, 2 Sắp chiếu)
INSERT INTO Movies (MovieId, Title, OriginalTitle, Director, Cast, DurationMinutes, ReleaseDate, EndDate, AgeRating, Language, Description, Status) VALUES
    (1, 'Dune: Hành Tinh Cát - Phần 2', 'Dune: Part Two', 'Denis Villeneuve', 'Timothée Chalamet, Zendaya, Rebecca Ferguson', 166,
        date('now', 'localtime', '-10 days'), date('now', 'localtime', '+30 days'), 'T13', 'Phụ đề Tiếng Việt',
        'Hành trình Paul Atreides liên minh với người Fremen để trả thù cho gia tộc và đối đầu vận mệnh.', 'NowShowing'),
    (2, 'Mai', 'Mai', 'Trấn Thành', 'Phương Anh Đào, Tuấn Trần, Hồng Đào', 131,
        date('now', 'localtime', '-7 days'), date('now', 'localtime', '+25 days'), 'T18', 'Tiếng Việt',
        'Câu chuyện tâm lý tình cảm đầy day dứt xoay quanh cuộc đời của Mai và những ngã rẽ bất ngờ.', 'NowShowing'),
    (3, 'Kung Fu Panda 4', 'Kung Fu Panda 4', 'Mike Mitchell', 'Jack Black, Awkwafina, Viola Davis', 94,
        date('now', 'localtime', '-5 days'), date('now', 'localtime', '+28 days'), 'P', 'Lồng tiếng Việt',
        'Gấu Po chuẩn bị trở thành Thủ lĩnh Tâm linh của Thung lũng Bình Yên và tìm người kế nhiệm Thần Long Đại Hiệp.', 'NowShowing'),
    (4, 'Quật Mộ Trùng Ma', 'Exhuma', 'Jang Jae-hyun', 'Choi Min-sik, Kim Go-eun, Lee Do-hyun', 134,
        date('now', 'localtime', '-3 days'), date('now', 'localtime', '+21 days'), 'T16', 'Phụ đề Tiếng Việt',
        'Hai pháp sư trẻ cùng thầy phong thủy thực hiện nghi lễ khai quật ngôi mộ bí ẩn và đánh thức lời nguyền cổ xưa.', 'NowShowing'),
    (5, 'Người Nhện: Du Hành Vũ Trụ Mới', 'Spider-Man: Beyond the Spider-Verse', 'Joaquim Dos Santos', 'Shameik Moore, Hailee Steinfeld', 140,
        date('now', 'localtime', '+14 days'), date('now', 'localtime', '+60 days'), 'P', 'Phụ đề Tiếng Việt',
        'Miles Morales tiếp tục cuộc chiến bảo vệ đa vũ trụ cùng biệt đội Người Nhện.', 'ComingSoon'),
    (6, 'Biệt Đội Săn Ma: Kỷ Nguyên Băng Giá', 'Ghostbusters: Frozen Empire', 'Gil Kenan', 'Paul Rudd, Carrie Coon, Finn Wolfhard', 115,
        date('now', 'localtime', '+21 days'), date('now', 'localtime', '+65 days'), 'K', 'Phụ đề Tiếng Việt',
        'Gia đình Spengler quay lại trụ sở cứu hỏa New York huyền thoại để ngăn chặn kỷ băng hà thứ hai.', 'ComingSoon');

INSERT INTO MovieGenres (MovieId, GenreId) VALUES
    (1, 1), (1, 5),
    (2, 4),
    (3, 2), (3, 6),
    (4, 3),
    (5, 1), (5, 2),
    (6, 1), (6, 6);

-- 5. Phòng chiếu
INSERT INTO Rooms (RoomId, Name, RoomType, TotalRows, TotalCols, BasePriceSurcharge, IsActive) VALUES
    (1, 'Phòng 1 - 2D',   'Standard2D', 8,  10,     0, 1),
    (2, 'Phòng 2 - 3D',   'Standard3D', 8,  12, 20000, 1),
    (3, 'Phòng 3 - IMAX', 'IMAX',       10, 12, 40000, 1);

-- 6. Sơ đồ ghế cho 3 phòng (296 ghế: Phòng 1 = 80, Phòng 2 = 96, Phòng 3 = 120)
-- Phòng 1 (8x10)
INSERT INTO Seats (RoomId, RowLabel, SeatNumber, SeatCode, SeatType, PriceMultiplier, RowIndex, ColIndex, IsActive) VALUES
    (1,'A',1,'A1','Standard',1.00,0,0,1),(1,'A',2,'A2','Standard',1.00,0,1,1),(1,'A',3,'A3','Standard',1.00,0,2,1),(1,'A',4,'A4','Standard',1.00,0,3,1),(1,'A',5,'A5','Standard',1.00,0,4,1),(1,'A',6,'A6','Standard',1.00,0,5,1),(1,'A',7,'A7','Standard',1.00,0,6,1),(1,'A',8,'A8','Standard',1.00,0,7,1),(1,'A',9,'A9','Standard',1.00,0,8,1),(1,'A',10,'A10','Standard',1.00,0,9,1),
    (1,'B',1,'B1','Standard',1.00,1,0,0),(1,'B',2,'B2','Standard',1.00,1,1,1),(1,'B',3,'B3','Standard',1.00,1,2,1),(1,'B',4,'B4','Standard',1.00,1,3,1),(1,'B',5,'B5','Standard',1.00,1,4,1),(1,'B',6,'B6','Standard',1.00,1,5,1),(1,'B',7,'B7','Standard',1.00,1,6,1),(1,'B',8,'B8','Standard',1.00,1,7,1),(1,'B',9,'B9','Standard',1.00,1,8,1),(1,'B',10,'B10','Standard',1.00,1,9,1),
    (1,'C',1,'C1','Standard',1.00,2,0,1),(1,'C',2,'C2','Standard',1.00,2,1,1),(1,'C',3,'C3','Standard',1.00,2,2,1),(1,'C',4,'C4','Standard',1.00,2,3,1),(1,'C',5,'C5','Standard',1.00,2,4,1),(1,'C',6,'C6','Standard',1.00,2,5,1),(1,'C',7,'C7','Standard',1.00,2,6,1),(1,'C',8,'C8','Standard',1.00,2,7,1),(1,'C',9,'C9','Standard',1.00,2,8,1),(1,'C',10,'C10','Standard',1.00,2,9,1),
    (1,'D',1,'D1','Standard',1.00,3,0,1),(1,'D',2,'D2','Standard',1.00,3,1,1),(1,'D',3,'D3','Standard',1.00,3,2,1),(1,'D',4,'D4','Standard',1.00,3,3,1),(1,'D',5,'D5','Standard',1.00,3,4,1),(1,'D',6,'D6','Standard',1.00,3,5,1),(1,'D',7,'D7','Standard',1.00,3,6,1),(1,'D',8,'D8','Standard',1.00,3,7,1),(1,'D',9,'D9','Standard',1.00,3,8,1),(1,'D',10,'D10','Standard',1.00,3,9,1),
    (1,'E',1,'E1','VIP',1.25,4,0,1),(1,'E',2,'E2','VIP',1.25,4,1,1),(1,'E',3,'E3','VIP',1.25,4,2,1),(1,'E',4,'E4','VIP',1.25,4,3,1),(1,'E',5,'E5','VIP',1.25,4,4,1),(1,'E',6,'E6','VIP',1.25,4,5,1),(1,'E',7,'E7','VIP',1.25,4,6,1),(1,'E',8,'E8','VIP',1.25,4,7,1),(1,'E',9,'E9','VIP',1.25,4,8,1),(1,'E',10,'E10','VIP',1.25,4,9,1),
    (1,'F',1,'F1','VIP',1.25,5,0,1),(1,'F',2,'F2','VIP',1.25,5,1,1),(1,'F',3,'F3','VIP',1.25,5,2,1),(1,'F',4,'F4','VIP',1.25,5,3,1),(1,'F',5,'F5','VIP',1.25,5,4,1),(1,'F',6,'F6','VIP',1.25,5,5,1),(1,'F',7,'F7','VIP',1.25,5,6,1),(1,'F',8,'F8','VIP',1.25,5,7,1),(1,'F',9,'F9','VIP',1.25,5,8,1),(1,'F',10,'F10','VIP',1.25,5,9,1),
    (1,'G',1,'G1','VIP',1.25,6,0,1),(1,'G',2,'G2','VIP',1.25,6,1,1),(1,'G',3,'G3','VIP',1.25,6,2,1),(1,'G',4,'G4','VIP',1.25,6,3,1),(1,'G',5,'G5','VIP',1.25,6,4,1),(1,'G',6,'G6','VIP',1.25,6,5,1),(1,'G',7,'G7','VIP',1.25,6,6,1),(1,'G',8,'G8','VIP',1.25,6,7,1),(1,'G',9,'G9','VIP',1.25,6,8,1),(1,'G',10,'G10','VIP',1.25,6,9,1),
    (1,'H',1,'H1','Couple',2.00,7,0,1),(1,'H',2,'H2','Couple',2.00,7,1,1),(1,'H',3,'H3','Couple',2.00,7,2,1),(1,'H',4,'H4','Couple',2.00,7,3,1),(1,'H',5,'H5','Couple',2.00,7,4,1),(1,'H',6,'H6','Couple',2.00,7,5,1),(1,'H',7,'H7','Couple',2.00,7,6,1),(1,'H',8,'H8','Couple',2.00,7,7,1),(1,'H',9,'H9','Couple',2.00,7,8,1),(1,'H',10,'H10','Couple',2.00,7,9,1);

-- Phòng 2 (8x12)
INSERT INTO Seats (RoomId, RowLabel, SeatNumber, SeatCode, SeatType, PriceMultiplier, RowIndex, ColIndex, IsActive) VALUES
    (2,'A',1,'A1','Standard',1.00,0,0,1),(2,'A',2,'A2','Standard',1.00,0,1,1),(2,'A',3,'A3','Standard',1.00,0,2,1),(2,'A',4,'A4','Standard',1.00,0,3,1),(2,'A',5,'A5','Standard',1.00,0,4,1),(2,'A',6,'A6','Standard',1.00,0,5,1),(2,'A',7,'A7','Standard',1.00,0,6,1),(2,'A',8,'A8','Standard',1.00,0,7,1),(2,'A',9,'A9','Standard',1.00,0,8,1),(2,'A',10,'A10','Standard',1.00,0,9,1),(2,'A',11,'A11','Standard',1.00,0,10,1),(2,'A',12,'A12','Standard',1.00,0,11,1),
    (2,'B',1,'B1','Standard',1.00,1,0,1),(2,'B',2,'B2','Standard',1.00,1,1,1),(2,'B',3,'B3','Standard',1.00,1,2,1),(2,'B',4,'B4','Standard',1.00,1,3,1),(2,'B',5,'B5','Standard',1.00,1,4,1),(2,'B',6,'B6','Standard',1.00,1,5,1),(2,'B',7,'B7','Standard',1.00,1,6,1),(2,'B',8,'B8','Standard',1.00,1,7,1),(2,'B',9,'B9','Standard',1.00,1,8,1),(2,'B',10,'B10','Standard',1.00,1,9,1),(2,'B',11,'B11','Standard',1.00,1,10,1),(2,'B',12,'B12','Standard',1.00,1,11,1),
    (2,'C',1,'C1','Standard',1.00,2,0,1),(2,'C',2,'C2','Standard',1.00,2,1,1),(2,'C',3,'C3','Standard',1.00,2,2,1),(2,'C',4,'C4','Standard',1.00,2,3,1),(2,'C',5,'C5','Standard',1.00,2,4,1),(2,'C',6,'C6','Standard',1.00,2,5,1),(2,'C',7,'C7','Standard',1.00,2,6,1),(2,'C',8,'C8','Standard',1.00,2,7,1),(2,'C',9,'C9','Standard',1.00,2,8,1),(2,'C',10,'C10','Standard',1.00,2,9,1),(2,'C',11,'C11','Standard',1.00,2,10,1),(2,'C',12,'C12','Standard',1.00,2,11,1),
    (2,'D',1,'D1','Standard',1.00,3,0,1),(2,'D',2,'D2','Standard',1.00,3,1,1),(2,'D',3,'D3','Standard',1.00,3,2,1),(2,'D',4,'D4','Standard',1.00,3,3,1),(2,'D',5,'D5','Standard',1.00,3,4,1),(2,'D',6,'D6','Standard',1.00,3,5,1),(2,'D',7,'D7','Standard',1.00,3,6,1),(2,'D',8,'D8','Standard',1.00,3,7,1),(2,'D',9,'D9','Standard',1.00,3,8,1),(2,'D',10,'D10','Standard',1.00,3,9,1),(2,'D',11,'D11','Standard',1.00,3,10,1),(2,'D',12,'D12','Standard',1.00,3,11,1),
    (2,'E',1,'E1','VIP',1.25,4,0,1),(2,'E',2,'E2','VIP',1.25,4,1,1),(2,'E',3,'E3','VIP',1.25,4,2,1),(2,'E',4,'E4','VIP',1.25,4,3,1),(2,'E',5,'E5','VIP',1.25,4,4,1),(2,'E',6,'E6','VIP',1.25,4,5,1),(2,'E',7,'E7','VIP',1.25,4,6,1),(2,'E',8,'E8','VIP',1.25,4,7,1),(2,'E',9,'E9','VIP',1.25,4,8,1),(2,'E',10,'E10','VIP',1.25,4,9,1),(2,'E',11,'E11','VIP',1.25,4,10,1),(2,'E',12,'E12','VIP',1.25,4,11,1),
    (2,'F',1,'F1','VIP',1.25,5,0,1),(2,'F',2,'F2','VIP',1.25,5,1,1),(2,'F',3,'F3','VIP',1.25,5,2,1),(2,'F',4,'F4','VIP',1.25,5,3,1),(2,'F',5,'F5','VIP',1.25,5,4,1),(2,'F',6,'F6','VIP',1.25,5,5,1),(2,'F',7,'F7','VIP',1.25,5,6,1),(2,'F',8,'F8','VIP',1.25,5,7,1),(2,'F',9,'F9','VIP',1.25,5,8,1),(2,'F',10,'F10','VIP',1.25,5,9,1),(2,'F',11,'F11','VIP',1.25,5,10,1),(2,'F',12,'F12','VIP',1.25,5,11,1),
    (2,'G',1,'G1','VIP',1.25,6,0,1),(2,'G',2,'G2','VIP',1.25,6,1,1),(2,'G',3,'G3','VIP',1.25,6,2,1),(2,'G',4,'G4','VIP',1.25,6,3,1),(2,'G',5,'G5','VIP',1.25,6,4,1),(2,'G',6,'G6','VIP',1.25,6,5,1),(2,'G',7,'G7','VIP',1.25,6,6,1),(2,'G',8,'G8','VIP',1.25,6,7,1),(2,'G',9,'G9','VIP',1.25,6,8,1),(2,'G',10,'G10','VIP',1.25,6,9,1),(2,'G',11,'G11','VIP',1.25,6,10,1),(2,'G',12,'G12','VIP',1.25,6,11,1),
    (2,'H',1,'H1','Couple',2.00,7,0,1),(2,'H',2,'H2','Couple',2.00,7,1,1),(2,'H',3,'H3','Couple',2.00,7,2,1),(2,'H',4,'H4','Couple',2.00,7,3,1),(2,'H',5,'H5','Couple',2.00,7,4,1),(2,'H',6,'H6','Couple',2.00,7,5,1),(2,'H',7,'H7','Couple',2.00,7,6,1),(2,'H',8,'H8','Couple',2.00,7,7,1),(2,'H',9,'H9','Couple',2.00,7,8,1),(2,'H',10,'H10','Couple',2.00,7,9,1),(2,'H',11,'H11','Couple',2.00,7,10,1),(2,'H',12,'H12','Couple',2.00,7,11,1);

-- Phòng 3 (10x12)
INSERT INTO Seats (RoomId, RowLabel, SeatNumber, SeatCode, SeatType, PriceMultiplier, RowIndex, ColIndex, IsActive) VALUES
    (3,'A',1,'A1','Standard',1.00,0,0,1),(3,'A',2,'A2','Standard',1.00,0,1,1),(3,'A',3,'A3','Standard',1.00,0,2,1),(3,'A',4,'A4','Standard',1.00,0,3,1),(3,'A',5,'A5','Standard',1.00,0,4,1),(3,'A',6,'A6','Standard',1.00,0,5,1),(3,'A',7,'A7','Standard',1.00,0,6,1),(3,'A',8,'A8','Standard',1.00,0,7,1),(3,'A',9,'A9','Standard',1.00,0,8,1),(3,'A',10,'A10','Standard',1.00,0,9,1),(3,'A',11,'A11','Standard',1.00,0,10,1),(3,'A',12,'A12','Standard',1.00,0,11,1),
    (3,'B',1,'B1','Standard',1.00,1,0,1),(3,'B',2,'B2','Standard',1.00,1,1,1),(3,'B',3,'B3','Standard',1.00,1,2,1),(3,'B',4,'B4','Standard',1.00,1,3,1),(3,'B',5,'B5','Standard',1.00,1,4,1),(3,'B',6,'B6','Standard',1.00,1,5,1),(3,'B',7,'B7','Standard',1.00,1,6,1),(3,'B',8,'B8','Standard',1.00,1,7,1),(3,'B',9,'B9','Standard',1.00,1,8,1),(3,'B',10,'B10','Standard',1.00,1,9,1),(3,'B',11,'B11','Standard',1.00,1,10,1),(3,'B',12,'B12','Standard',1.00,1,11,1),
    (3,'C',1,'C1','Standard',1.00,2,0,1),(3,'C',2,'C2','Standard',1.00,2,1,1),(3,'C',3,'C3','Standard',1.00,2,2,1),(3,'C',4,'C4','Standard',1.00,2,3,1),(3,'C',5,'C5','Standard',1.00,2,4,1),(3,'C',6,'C6','Standard',1.00,2,5,1),(3,'C',7,'C7','Standard',1.00,2,6,1),(3,'C',8,'C8','Standard',1.00,2,7,1),(3,'C',9,'C9','Standard',1.00,2,8,1),(3,'C',10,'C10','Standard',1.00,2,9,1),(3,'C',11,'C11','Standard',1.00,2,10,1),(3,'C',12,'C12','Standard',1.00,2,11,1),
    (3,'D',1,'D1','Standard',1.00,3,0,1),(3,'D',2,'D2','Standard',1.00,3,1,1),(3,'D',3,'D3','Standard',1.00,3,2,1),(3,'D',4,'D4','Standard',1.00,3,3,1),(3,'D',5,'D5','Standard',1.00,3,4,1),(3,'D',6,'D6','Standard',1.00,3,5,1),(3,'D',7,'D7','Standard',1.00,3,6,1),(3,'D',8,'D8','Standard',1.00,3,7,1),(3,'D',9,'D9','Standard',1.00,3,8,1),(3,'D',10,'D10','Standard',1.00,3,9,1),(3,'D',11,'D11','Standard',1.00,3,10,1),(3,'D',12,'D12','Standard',1.00,3,11,1),
    (3,'E',1,'E1','Standard',1.00,4,0,1),(3,'E',2,'E2','Standard',1.00,4,1,1),(3,'E',3,'E3','Standard',1.00,4,2,1),(3,'E',4,'E4','Standard',1.00,4,3,1),(3,'E',5,'E5','Standard',1.00,4,4,1),(3,'E',6,'E6','Standard',1.00,4,5,1),(3,'E',7,'E7','Standard',1.00,4,6,1),(3,'E',8,'E8','Standard',1.00,4,7,1),(3,'E',9,'E9','Standard',1.00,4,8,1),(3,'E',10,'E10','Standard',1.00,4,9,1),(3,'E',11,'E11','Standard',1.00,4,10,1),(3,'E',12,'E12','Standard',1.00,4,11,1),
    (3,'F',1,'F1','Standard',1.00,5,0,1),(3,'F',2,'F2','Standard',1.00,5,1,1),(3,'F',3,'F3','Standard',1.00,5,2,1),(3,'F',4,'F4','Standard',1.00,5,3,1),(3,'F',5,'F5','Standard',1.00,5,4,1),(3,'F',6,'F6','Standard',1.00,5,5,1),(3,'F',7,'F7','Standard',1.00,5,6,1),(3,'F',8,'F8','Standard',1.00,5,7,1),(3,'F',9,'F9','Standard',1.00,5,8,1),(3,'F',10,'F10','Standard',1.00,5,9,1),(3,'F',11,'F11','Standard',1.00,5,10,1),(3,'F',12,'F12','Standard',1.00,5,11,1),
    (3,'G',1,'G1','VIP',1.25,6,0,1),(3,'G',2,'G2','VIP',1.25,6,1,1),(3,'G',3,'G3','VIP',1.25,6,2,1),(3,'G',4,'G4','VIP',1.25,6,3,1),(3,'G',5,'G5','VIP',1.25,6,4,1),(3,'G',6,'G6','VIP',1.25,6,5,1),(3,'G',7,'G7','VIP',1.25,6,6,1),(3,'G',8,'G8','VIP',1.25,6,7,1),(3,'G',9,'G9','VIP',1.25,6,8,1),(3,'G',10,'G10','VIP',1.25,6,9,1),(3,'G',11,'G11','VIP',1.25,6,10,1),(3,'G',12,'G12','VIP',1.25,6,11,1),
    (3,'H',1,'H1','VIP',1.25,7,0,1),(3,'H',2,'H2','VIP',1.25,7,1,1),(3,'H',3,'H3','VIP',1.25,7,2,1),(3,'H',4,'H4','VIP',1.25,7,3,1),(3,'H',5,'H5','VIP',1.25,7,4,1),(3,'H',6,'H6','VIP',1.25,7,5,1),(3,'H',7,'H7','VIP',1.25,7,6,1),(3,'H',8,'H8','VIP',1.25,7,7,1),(3,'H',9,'H9','VIP',1.25,7,8,1),(3,'H',10,'H10','VIP',1.25,7,9,1),(3,'H',11,'H11','VIP',1.25,7,10,1),(3,'H',12,'H12','VIP',1.25,7,11,1),
    (3,'I',1,'I1','VIP',1.25,8,0,1),(3,'I',2,'I2','VIP',1.25,8,1,1),(3,'I',3,'I3','VIP',1.25,8,2,1),(3,'I',4,'I4','VIP',1.25,8,3,1),(3,'I',5,'I5','VIP',1.25,8,4,1),(3,'I',6,'I6','VIP',1.25,8,5,1),(3,'I',7,'I7','VIP',1.25,8,6,1),(3,'I',8,'I8','VIP',1.25,8,7,1),(3,'I',9,'I9','VIP',1.25,8,8,1),(3,'I',10,'I10','VIP',1.25,8,9,1),(3,'I',11,'I11','VIP',1.25,8,10,1),(3,'I',12,'I12','VIP',1.25,8,11,1),
    (3,'J',1,'J1','Couple',2.00,9,0,1),(3,'J',2,'J2','Couple',2.00,9,1,1),(3,'J',3,'J3','Couple',2.00,9,2,1),(3,'J',4,'J4','Couple',2.00,9,3,1),(3,'J',5,'J5','Couple',2.00,9,4,1),(3,'J',6,'J6','Couple',2.00,9,5,1),(3,'J',7,'J7','Couple',2.00,9,6,1),(3,'J',8,'J8','Couple',2.00,9,7,1),(3,'J',9,'J9','Couple',2.00,9,8,1),(3,'J',10,'J10','Couple',2.00,9,9,1),(3,'J',11,'J11','Couple',2.00,9,10,1),(3,'J',12,'J12','Couple',2.00,9,11,1);

-- 7. Suất chiếu mẫu (hôm nay, ngày mai, ngày kia — đã tính +15 phút dọn phòng)
INSERT INTO Showtimes (ShowtimeId, MovieId, RoomId, StartTime, EndTime, BaseTicketPrice, Format, Status, CreatedByUserId, CreatedAt) VALUES
    (1,  1, 3, datetime('now', 'localtime', 'start of day', '+14 hours'),                datetime('now', 'localtime', 'start of day', '+14 hours', '+181 minutes'),                100000, 'IMAX Phụ đề',   'OpenForSale', 1, datetime('now', 'localtime')),
    (2,  1, 3, datetime('now', 'localtime', 'start of day', '+18 hours', '+30 minutes'), datetime('now', 'localtime', 'start of day', '+18 hours', '+30 minutes', '+181 minutes'), 115000, 'IMAX Phụ đề',   'OpenForSale', 1, datetime('now', 'localtime')),
    (3,  2, 1, datetime('now', 'localtime', 'start of day', '+15 hours'),                datetime('now', 'localtime', 'start of day', '+15 hours', '+146 minutes'),                 85000, '2D Tiếng Việt', 'OpenForSale', 1, datetime('now', 'localtime')),
    (4,  2, 1, datetime('now', 'localtime', 'start of day', '+19 hours', '+30 minutes'), datetime('now', 'localtime', 'start of day', '+19 hours', '+30 minutes', '+146 minutes'),  95000, '2D Tiếng Việt', 'OpenForSale', 1, datetime('now', 'localtime')),
    (5,  3, 2, datetime('now', 'localtime', 'start of day', '+13 hours', '+30 minutes'), datetime('now', 'localtime', 'start of day', '+13 hours', '+30 minutes', '+109 minutes'),  80000, '3D Lồng tiếng', 'OpenForSale', 1, datetime('now', 'localtime')),
    (6,  4, 2, datetime('now', 'localtime', 'start of day', '+20 hours'),                datetime('now', 'localtime', 'start of day', '+20 hours', '+149 minutes'),                 95000, '2D Phụ đề',     'OpenForSale', 1, datetime('now', 'localtime')),

    (7,  1, 1, datetime('now', 'localtime', 'start of day', '+1 day', '+14 hours'), datetime('now', 'localtime', 'start of day', '+1 day', '+14 hours', '+181 minutes'),  90000, '2D Phụ đề',     'OpenForSale', 1, datetime('now', 'localtime')),
    (8,  2, 1, datetime('now', 'localtime', 'start of day', '+1 day', '+19 hours'), datetime('now', 'localtime', 'start of day', '+1 day', '+19 hours', '+146 minutes'),  95000, '2D Tiếng Việt', 'OpenForSale', 1, datetime('now', 'localtime')),
    (9,  3, 2, datetime('now', 'localtime', 'start of day', '+1 day', '+10 hours'), datetime('now', 'localtime', 'start of day', '+1 day', '+10 hours', '+109 minutes'),  75000, '2D Lồng tiếng', 'OpenForSale', 1, datetime('now', 'localtime')),
    (10, 4, 3, datetime('now', 'localtime', 'start of day', '+1 day', '+20 hours'), datetime('now', 'localtime', 'start of day', '+1 day', '+20 hours', '+149 minutes'), 110000, 'IMAX Phụ đề',   'OpenForSale', 1, datetime('now', 'localtime')),

    (11, 1, 3, datetime('now', 'localtime', 'start of day', '+2 days', '+16 hours'), datetime('now', 'localtime', 'start of day', '+2 days', '+16 hours', '+181 minutes'), 110000, 'IMAX Phụ đề',   'OpenForSale', 1, datetime('now', 'localtime')),
    (12, 3, 1, datetime('now', 'localtime', 'start of day', '+2 days', '+14 hours'), datetime('now', 'localtime', 'start of day', '+2 days', '+14 hours', '+109 minutes'),  80000, '2D Lồng tiếng', 'OpenForSale', 1, datetime('now', 'localtime'));

-- 8. Hóa đơn & Vé mẫu (gắn cho khach1 tự đặt online và staff1 bán tại quầy cho khach2)
INSERT INTO Orders (OrderId, OrderCode, StaffUserId, CustomerId, BookingChannel, TotalTickets, SubTotalAmount, PointsUsed, DiscountAmount, TotalAmount, PaymentMethod, CashReceived, ChangeAmount, Status, Note, CreatedAt, PaidAt) VALUES
    (1, 'HD-' || strftime('%Y%m%d', 'now', 'localtime') || '-0001', NULL, 1, 'SelfService', 2, 225000, 0, 0, 225000, 'BankTransfer',      0,    0, 'Paid', 'Đặt vé trực tuyến qua ứng dụng', datetime('now', 'localtime', '-3 hours'), datetime('now', 'localtime', '-3 hours')),
    (2, 'HD-' || strftime('%Y%m%d', 'now', 'localtime') || '-0002', 2,    2, 'Counter',     1,  95000, 0, 0,  95000, 'Cash',         100000, 5000, 'Paid', 'Bán tại quầy bởi staff1',         datetime('now', 'localtime', '-2 hours'), datetime('now', 'localtime', '-2 hours'));

INSERT INTO Tickets (TicketCode, OrderId, ShowtimeId, SeatId, UnitPrice, Status) VALUES
    ('VE-KHACH101', 1, 7, (SELECT SeatId FROM Seats WHERE RoomId = 1 AND SeatCode = 'E5'), 112500, 'Booked'),
    ('VE-KHACH102', 1, 7, (SELECT SeatId FROM Seats WHERE RoomId = 1 AND SeatCode = 'E6'), 112500, 'Booked'),
    ('VE-STAFF101', 2, 8, (SELECT SeatId FROM Seats WHERE RoomId = 1 AND SeatCode = 'C4'),  95000, 'Booked');

COMMIT;
