# Sơ đồ Use Case — CINEVERSE CINEMA (`CinemaManageProject`)

Tài liệu mô tả toàn bộ các ca sử dụng (Use Cases) và phạm vi phân quyền của **3 tác nhân (Actors)** trong hệ thống quản lý rạp chiếu phim **CINEVERSE CINEMA**.

## 1. Sơ đồ Use Case Tổng quan

```mermaid
flowchart LR
    Guest["Khách chưa đăng nhập"]
    Customer["Khách hàng (Customer)"]
    Staff["Nhân viên quầy (Staff)"]
    Admin["Quản trị viên (Admin)"]

    subgraph Auth["Xác thực & Phiên làm việc"]
        UC_Login(["Đăng nhập hệ thống"])
        UC_Register(["Đăng ký tài khoản Hội viên"])
        UC_Logout(["Đăng xuất & làm mới phiên"])
    end

    subgraph CustArea["Phân hệ Khách hàng (Customer)"]
        UC_CustDash(["Xem Trang chủ Hội viên, hạng thẻ & điểm"])
        UC_Browse(["Duyệt phim Đang chiếu / Sắp chiếu"])
        UC_SelfBook(["Chọn suất, chọn ghế & tự đặt vé Online"])
        UC_MyTickets(["Xem vé điện tử & lịch sử hóa đơn"])
        UC_Cancel(["Hủy vé chờ soát & hoàn điều chỉnh điểm"])
    end

    subgraph StaffArea["Phân hệ Nhân viên quầy (Staff)"]
        UC_StaffDash(["Xem Dashboard ca quầy cá nhân"])
        UC_POS(["Bán vé tại quầy (chọn ghế, đổi điểm, tính tiền thối)"])
        UC_CheckIn(["Quét / nhập mã vé & xác nhận Check-in"])
        UC_CustLookup(["Tra cứu, cập nhật & đăng ký hội viên tại quầy"])
    end

    subgraph AdminArea["Phân hệ Quản trị viên (Admin)"]
        UC_AdminDash(["Xem Dashboard KPI & tỷ lệ lấp đầy toàn rạp"])
        UC_Movies(["Quản lý Phim, Thể loại & ảnh Poster"])
        UC_Rooms(["Xem Phòng chiếu & bật/tắt bảo trì ghế"])
        UC_Showtimes(["Lập lịch chiếu (+15p) & chống trùng phòng"])
        UC_Orders(["Tra cứu Hóa đơn & phân tích Doanh thu"])
        UC_Users(["Quản trị Nhân sự & Tài khoản (bảo vệ Admin)"])
    end

    Guest --> UC_Login
    Guest --> UC_Register

    Customer --> UC_Login
    Customer --> UC_Logout
    Customer --> UC_CustDash
    Customer --> UC_Browse
    Customer --> UC_SelfBook
    Customer --> UC_MyTickets
    Customer --> UC_Cancel

    Staff --> UC_Login
    Staff --> UC_Logout
    Staff --> UC_StaffDash
    Staff --> UC_POS
    Staff --> UC_CheckIn
    Staff --> UC_CustLookup

    Admin --> UC_Login
    Admin --> UC_Logout
    Admin --> UC_AdminDash
    Admin --> UC_Movies
    Admin --> UC_Rooms
    Admin --> UC_Showtimes
    Admin --> UC_Orders
    Admin --> UC_Users

    UC_Browse -. "include" .-> UC_SelfBook
    UC_MyTickets -. "extend" .-> UC_Cancel
    UC_POS -. "extend" .-> UC_CustLookup
```

---

## 2. Bảng Ánh xạ Tác nhân — Chức năng — Màn hình

| Tác nhân | Nhóm chức năng chính | Màn hình tương ứng (`CinemaManageProject/Views/`) | Quy tắc nghiệp vụ trọng tâm |
| :--- | :--- | :--- | :--- |
| **Chưa đăng nhập** | Đăng nhập, đăng ký hội viên mới | `LoginWindow` | Xác thực `PBKDF2-HMAC-SHA256`, đăng ký tự gán vai trò `Customer` và tạo đồng bộ hồ sơ `Customers`. |
| **Customer** | Trang chủ hội viên, tự đặt vé online, xem & hủy vé | `Customer/CustomerDashboardView`, `CustomerBrowseView`, `CustomerMyTicketsView` | Chỉ đặt kênh `SelfService` (Chuyển khoản / Ví điện tử / Thẻ); chỉ hủy được vé `Booked` thuộc chính tài khoản của mình. |
| **Staff** | Dashboard quầy, bán vé POS, soát vé check-in, quản lý hội viên quầy | `Staff/StaffDashboardView`, `StaffPosView`, `StaffCheckInView`, `StaffCustomersView` | Bán vé kênh `Counter` (đủ 4 PTTT gồm tiền mặt & tiền thối); soát vé ghi `CheckedInAt`, `CheckedInByUserId`; đăng ký hội viên quầy (`UserId = NULL`). |
| **Admin** | Dashboard toàn rạp, phim, phòng/ghế, lịch chiếu, hóa đơn/doanh thu, tài khoản | `Admin/AdminDashboardView`, `AdminMoviesView`, `AdminRoomsView`, `AdminShowtimesView`, `AdminOrdersView`, `AdminUsersView` | Không thể bảo trì ghế đang có vé hiệu lực; khóa sửa thông số cốt lõi suất đã có vé; bảo vệ nghiêm ngặt tài khoản `Admin` gốc. |
