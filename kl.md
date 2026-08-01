# KẾT LUẬN DỰ ÁN — MovieCinema

## I. Tổng quan

Dự án **MovieCinema** là hệ thống quản lý rạp chiếu phim và đặt vé trực tuyến, được xây dựng trên nền tảng **ASP.NET Core 8 MVC** kết hợp **Entity Framework Core** và **SQL Server**. Hệ thống áp dụng **11/23 GoF Design Patterns** thuộc cả 3 nhóm Creational, Structural và Behavioral, thể hiện khả năng thiết kế phần mềm hướng đối tượng có tính tái sử dụng và mở rộng cao.

Trong quá trình thực hiện, nhóm đã hoàn thành phần lớn các chức năng cốt lõi của một hệ thống đặt vé xem phim, đồng thời tích hợp các mẫu thiết kế vào thực tế nhằm nâng cao tính bảo trì và linh hoạt của mã nguồn.

---

## II. Các chức năng đã hoàn thành

### 1. Quản lý nội dung (CRUD đầy đủ)

| Chức năng | Mô tả |
|---|---|
| **Quản lý Phim** | Thêm/sửa/xóa phim, upload hình ảnh poster, gán thể loại/đạo diễn/diễn viên, phân trang và tìm kiếm |
| **Quản lý Diễn viên** | CRUD diễn viên kèm upload ảnh đại diện, mô tả tiểu sử |
| **Quản lý Đạo diễn (Producer)** | CRUD đạo diễn, upload ảnh, mô tả |
| **Quản lý Thể loại** | CRUD thể loại phim |
| **Quản lý Rạp chiếu** | CRUD rạp, upload logo |
| **Quản lý Phòng chiếu** | CRUD phòng chiếu theo rạp, quản lý sức chứa |
| **Quản lý Ghế ngồi** | Tạo ghế hàng loạt theo phòng (số hàng × số ghế/hàng), chỉnh sửa/xóa từng ghế, phân loại ghế (Standard/VIP/Couple/Disabled) |
| **Quản lý Voucher** | CRUD voucher giảm giá (% hoặc cố định), cài đặt ngày hết hạn và trạng thái kích hoạt |

### 2. Quản lý lịch chiếu

- Tạo suất chiếu với chọn phim, phòng chiếu, ngày giờ bắt đầu/kết thúc, giá vé
- Chọn nhiều phòng chiếu cho cùng một suất chiếu
- Xem lịch chiếu theo ngày, lọc theo rạp
- Hiển thị chỉ suất chiếu còn khả dụng

### 3. Đặt vé và thanh toán

- **Giỏ hàng (Shopping Cart):** Thêm/xóa phim vào giỏ, hiển thị tóm tắt giỏ hàng qua ViewComponent
- **Chọn ghế:** Giao diện chọn ghế trực quan (Visual Seat Map) theo phòng chiếu, hiển thị trạng thái ghế (đã đặt/chưa đặt), phân biệt màu theo loại ghế
- **Áp dụng voucher:** Nhập mã voucher khi thanh toán
- **Đổi điểm tích lũy:** Sử dụng điểm thành viên để giảm giá
- **Thanh toán đa phương thức:** Hỗ trợ thanh toán tiền mặt (tại quầy) và PayPal (demo/stub)
- **Xác nhận đặt vé:** Hiển thị trang xác nhận sau khi đặt thành công

### 4. Quản lý đơn hàng

- Xem danh sách đơn hàng theo email tìm kiếm
- Xem chi tiết đơn hàng
- Hủy đơn hàng
- Xóa tất cả đơn hàng
- Quản lý trạng thái đơn hàng (Purchased → Confirmed → Cancelled/Refunded)

### 5. Hệ thống tài khoản và phân quyền

- Đăng ký/đăng nhập/đăng xuất bằng ASP.NET Identity
- Phân quyền Admin và User
- Trang "Access Denied" khi truy cập không hợp lệ
- Tài khoản mặc định: Admin (`admin@tickets.com`) và User (`user@tickets.com`)

### 6. Dashboard và Báo cáo

- **Dashboard Admin:** Tổng quan số lượng phim, suất chiếu, đơn hàng, doanh thu
- **Báo cáo doanh thu:** Thống kê theo ngày/tháng/năm, theo phim, theo rạp
- **Tỷ lệ lấp đầy suất chiếu:** Hiển thị tỷ lệ ghế đã đặt trên tổng ghế

### 7. Design Patterns đã tích hợp

| Pattern | Vai trò trong hệ thống |
|---|---|
| **Singleton** | Đảm bảo mỗi session có một ShoppingCart duy nhất |
| **Bridge** | Tách riêng logic tính giá theo loại ghế (Standard ×1.0, VIP ×1.2, Couple ×2.0, Disabled ×0.5) |
| **Decorator** | Xếp chồng khuyến mãi: Voucher → Loyalty Points → Happy Hour |
| **Proxy** | Cache danh sách phim bằng `IMemoryCache`, tự động invalidation khi dữ liệu thay đổi |
| **Facade** | Gói gọn toàn bộ luồng đặt vé phức tạp (validate → check ghế → tính giá → thanh toán → tạo Order) vào một method |
| **Builder** | Tạo đối tượng Order phức tạp một cách sạch sẽ và dễ đọc |
| **Strategy** | Thanh toán đa phương thức (Cash, PayPal) — dễ thêm phương thức mới |
| **State** | Quản lý trạng thái Order với transition rules rõ ràng |
| **Observer** | Tự động thông báo (ghi log audit, cộng/trừ điểm, gửi email) khi trạng thái đơn hàng thay đổi |
| **Chain of Responsibility** | Pipeline validate đơn hàng qua chuỗi handler độc lập |
| **Mediator** | Tập trung hóa giao tiếp giữa Facade, Chain và các services |

---

## III. Các chức năng chưa được thực hiện / còn hạn chế

### 1. Thanh toán PayPal — còn ở dạng Demo (Stub)

PayPal Payment Strategy hiện chỉ là **stub** (mô phỏng), chưa tích hợp thật với PayPal API. Khi chọn PayPal, hệ thống chỉ ghi nhận phương thức thanh toán mà không gọi API thanh toán thực tế.

**Hạn chế:** Không thể xử lý thanh toán quốc tế thật, không có xác nhận thanh toán từ phía PayPal.

### 2. Gửi Email thông báo — còn ở dạng Stub

`EmailNotificationObserver` hiện chỉ **ghi log** thay vì gửi email thật. Hệ thống chưa tích hợp dịch vụ gửi email (SendGrid, SMTP, v.v.).

**Hạn chế:** Khách hàng không nhận được email xác nhận đặt vé, email hóa đơn, hay thông báo hủy đơn.

### 3. Happy Hour — Logic có nhưng chưa tích hợp giao diện

`HappyHourDecorator` đã được triển khai trong code (giảm 15% từ 14:00–17:00), nhưng **chưa có giao diện quản lý** (không có trang cấu hình khung giờ Happy Hour) và chưa hiển thị rõ ràng cho người dùng khi áp dụng.

### 4. Đánh giá phim (Movie Review) — Model có nhưng chưa triển khai

`MovieReview` model đã được định nghĩa trong `Models/MovieReview.cs` nhưng **chưa có Controller, Service hay View** tương ứng. Người dùng hiện tại không thể đánh giá hay bình luận phim.

### 5. Quản lý thành viên / Điểm tích lũy — chưa hoàn thiện

`Member` model đã có nhưng **chưa có Controller quản lý thành viên**. Điểm tích lũy (`LoyaltyPointsObserver`) có logic cộng/trừ nhưng chưa có giao diện để:
- Xem lịch sử điểm
- Quản lý hạng thành viên
- Cấu hình tỷ lệ tích điểm

### 6. Chức năng tìm kiếm — cơ bản

- Tìm kiếm phim chỉ theo tên (`Filter` action), chưa hỗ trợ tìm kiếm nâng cao (theo thể loại, đạo diễn, diễn viên, năm sản xuất).
- Tìm kiếm diễn viên theo tên, nhưng các thực thể khác (rạp, thể loại) chưa có tìm kiếm.

### 7. Phân quyền chi tiết — còn hạn chế

- Hiện tại phân quyền chỉ gồm **Admin** và **User**.
- Chưa có vai trò **Staff/Nhân viên rạp** (quản lý phòng chiếu, ghế, suất chiếu mà không cần toàn quyền Admin).
- Các action quản trị (CRUD) hiện sử dụng `[Authorize]` chung, chưa phân quyền chi tiết theo chức năng.

### 8. Responsive Design — cơ bản

Giao diện sử dụng Bootstrap 5 nhưng chưa tối ưu hóa đầy đủ cho thiết bị di động (mobile). Một số trang (như bảng ghế, báo cáo) hiển thị chưa tốt trên màn hình nhỏ.

### 9. Unit Test / Integration Test

Dự án hiện **chưa có** bất kỳ file test nào. Không có Unit Test cho các Design Patterns, không có Integration Test cho luồng đặt vé.

### 10. API RESTful

Dự án hiện chỉ triển khai MVC pattern (server-rendered). Chưa có REST API riêng để phục vụ cho mobile app hoặc frontend SPA.

---

## IV. Các chức năng có thể phát triển trong tương lai

### 1. Thanh toán trực tuyến thật

- **Tích hợp VNPay / MoMo / ZaloPay:** Cổng thanh toán phổ biến tại Việt Nam, hỗ trợ QR code.
- **Tích hợp PayPal API thật:** Triển khai OAuth, xác nhận thanh toán real-time, webhook thông báo.
- **Quản lý hoàn tiền:** Tích hợp logic hoàn tiền tự động qua cổng thanh toán.

### 2. Hệ thống Email & Thông báo

- **Gửi email xác nhận đặt vé** với mã QR code / mã đặt chỗ.
- **Thông báo push** qua Firebase Cloud Messaging (FCM) khi suất chiếu sắp bắt đầu.
- **Nhật ký thông báo:** Theo dõi trạng thái gửi email, retry khi thất bại.

### 3. Ứng dụng di động (Mobile App)

- Xây dựng **React Native / Flutter** app dựa trên REST API từ backend.
- Tích hợp thanh toán qua app, quét QR code tại rạp.
- Push notification cho khách hàng.

### 4. Hệ thống thành viên nâng cao

- **Hạng thành viên (Silver/Gold/Diamond):** Tích lũy điểm theo doanh số, tự động nâng hạng.
- **Đặc quyền theo hạng:** Giảm giá theo hạng, ưu tiên đặt vé, xem phim sớm (premiere).
- **Chương trình khuyến mãi theo sự kiện:** Giảm giá sinh nhật, khuyến mãi theo mùa.

### 5. Đánh giá & Xếp hạng phim

- **Đánh giá sao (1–5) và bình luận:** Triển khai đầy đủ `MovieReview` với Controller/Service/View.
- **Xếp hạng phim:** Hiển thị phim được yêu thích nhất, đánh giá trung bình.
- **Moderation:** Kiểm duyệt bình luận trước khi hiển thị.

### 6. Quản lý nội dung nâng cao

- **Trailer phim:** Nhúng video YouTube/Bunny CDN.
- **Quản lý suất chiếu thông minh:** Tự động gợi ý lịch chiếu theo lịch sử đặt vé, tránh trùng phòng.
- **Seat blocking:** Tự động giữ ghế trong 15 phút khi khách đang chọn.

### 7. Dashboard Analytics nâng cao

- **Biểu đồ doanh thu real-time** (dùng Chart.js / D3.js).
- **Dự báo doanh thu** dựa trên dữ liệu lịch sử.
- **Thống kê hành vi khách hàng:** Phim nào được xem nhiều nhất, khung giờ cao điểm.
- **Xuất báo cáo Excel / PDF.**

### 8. Multi-language & Multi-currency

- **Đa ngôn ngữ (i18n):** Hỗ trợ Tiếng Việt / English.
- **Đa loại tiền tệ:** Hiển thị giá theo loại tiền tệ, tỷ giá tự động.

### 9. Cải thiện kiến trúc & DevOps

- **Microservices:** Tách Services Layer thành các microservice riêng (Movie Service, Order Service, Payment Service).
- **Docker & CI/CD:** Container hóa ứng dụng, triển khai tự động qua GitHub Actions.
- **Logging & Monitoring:** Tích hợp Serilog + Seq/ELK Stack để theo dõi log và lỗi.
- **Caching nâng cao:** Redis thay cho In-Memory Cache, hỗ trợ distributed cache.
- **Performance:** Pagination server-side, lazy loading ảnh, CDN cho static files.

### 10. Kiểm thử tự động

- **Unit Test:** Viết test cho từng Design Pattern (Bridge, Strategy, State, Decorator, v.v.).
- **Integration Test:** Test toàn bộ luồng đặt vé (chọn ghế → thanh toán → xác nhận).
- **E2E Test:** Dùng Selenium/Playwright để test giao diện người dùng.

---

## V. Kết luận chung

Dự án **MovieCinema** đã hoàn thành mục tiêu xây dựng một hệ thống đặt vé xem phim với đầy đủ chức năng cốt lõi, từ quản lý nội dung, lịch chiếu, đặt vé, thanh toán đến báo cáo doanh thu. Việc tích hợp **11 Design Patterns** trong thực tế đã thể hiện khả năng áp dụng kiến thức lập trình hướng đối tượng vào bài toán thực tiễn.

Bên cạnh đó, dự án còn một số hạn chế cần khắc phục như: thanh toán PayPal chưa tích hợp thật, chưa có hệ thống email thông báo, chưa có unit test, và phân quyền chưa chi tiết. Các điểm này đặt ra hướng phát triển tiếp theo cho dự án, bao gồm tích hợp cổng thanh toán Việt Nam (VNPay/MoMo), xây dựng ứng dụng di động, nâng cấp hệ thống thành viên và cải thiện kiến trúc phần mềm.

Tổng kết, đây là một dự án có tính ứng dụng cao, phù hợp cho việc học hỏi và áp dụng các nguyên tắc thiết kế phần mềm, đồng thời có tiềm năng phát triển thành sản phẩm thương mại trong tương lai.
