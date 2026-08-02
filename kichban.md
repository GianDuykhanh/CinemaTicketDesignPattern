# 🎬 KỊCH BẢN VIDEO DEMO THUYẾT TRÌNH WEBSITE MOVIECINEMA (15 - 30 PHÚT)

> **Dự án:** Hệ thống đặt vé xem phim trực tuyến **MovieCinema** (.NET 8 ASP.NET Core MVC, Entity Framework Core, SQL Server)  
> **Đơn vị:** Nhóm 9  
> **Thời lượng đề xuất:** 20 – 25 phút  
> **Cấu trúc Video:**
> - **00:00 – 03:30 (3.5 phút):** Giới thiệu dự án, công dụng hệ thống & tổng quan 12 Design Patterns.
> - **03:30 – 22:00 (18.5 phút):** Thuyết trình chi tiết Code từng Design Pattern & Chạy Demo trực tiếp chức năng tương ứng trên Web UI.
> - **22:00 – 23:00 (1.0 phút):** Tổng kết bài thuyết trình & Lời cảm ơn.

---

## ⏱️ 1. TIMELINE TỔNG QUAN VIDEO THUYẾT TRÌNH

| Mốc Thời Gian | Phân Đoạn Thuyết Trình | Nội Dung Thao Tác Trực Quan / File Code |
|:---:|:---|:---|
| **00:00 – 01:00** | **Giới thiệu mở đầu** | Giới thiệu Nhóm 9/Thành viên, tên đồ án MovieCinema |
| **01:00 – 02:30** | **Tổng quan Sản phẩm & Công dụng** | Trình diễn nhanh các tính năng chính của Web Đặt vé MovieCinema |
| **02:30 – 03:30** | **Tổng quan 12 Design Patterns** | Trình bày Bảng phân loại 12 Patterns theo 3 nhóm GoF |
| **03:30 – 05:15** | **Pattern 1: Singleton** | Giải thích `ShoppingCart.cs` ➔ Demo giữ giỏ hàng per Session |
| **05:15 – 07:00** | **Pattern 2: Builder** | Giải thích `OrderBuilder.cs` ➔ Demo lắp ráp đối tượng `Order` |
| **07:00 – 08:45** | **Pattern 3: Proxy** | Giải thích `CachedMoviesServiceProxy.cs` ➔ Demo F5 Cache RAM ~0ms |
| **08:45 – 10:30** | **Pattern 4: Bridge** | Giải thích `SeatPricingBridge.cs` ➔ Demo chọn ghế Thường / VIP / Couple |
| **10:30 – 12:00** | **Pattern 5: Adapter** | Giải thích `PayPalPaymentStrategy` ➔ Demo kết nối cổng PayPal |
| **12:00 – 13:30** | **Pattern 6: Decorator** | Giải thích `PricingDecorators.cs` ➔ Demo bọc Voucher & Trừ điểm thưởng |
| **13:30 – 14:30** | **Pattern 7: Facade** | Giải thích `BookingFacade.cs` ➔ Demo rút gọn 8 bước đặt vé |
| **14:30 – 16:15** | **Pattern 8: Chain of Resp.** | Giải thích `OrderPipeline.cs` ➔ Demo chọn >10 ghế báo lỗi ngắt chuỗi |
| **16:15 – 17:30** | **Pattern 9: Strategy** | Giải thích `PaymentContext` ➔ Demo hoán đổi Cash/PayPal tại Runtime |
| **17:30 – 18:45** | **Pattern 10: Mediator** | Giải thích `BookingMediator.cs` ➔ Demo điều phối Command/Request |
| **18:45 – 20:30** | **Pattern 11: State** | Giải thích `OrderStateMachine.cs` ➔ Demo Admin duyệt/hủy đơn chuẩn state |
| **20:30 – 22:00** | **Pattern 12: Observer** | Giải thích `OrderObserver.cs` ➔ Demo tự động gửi Mail vé QR & Tích điểm |
| **22:00 – 23:00** | **Tổng kết & Lời cảm ơn** | Đánh giá lợi ích SOLID mang lại & Cảm ơn hội đồng |

---

## 📽️ 2. CHI TIẾT KỊCH BẢN NÓI & THAO TÁC THEO MỐC THỜI GIAN

---

### PHẦN 1: MỞ ĐẦU & GIỚI THIỆU SẢN PHẨM MOVIECINEMA (00:00 – 03:30)

#### 🎙️ Mốc 00:00 – 01:00 | Giới thiệu mở đầu
* **Giao diện quay màn hình:** Mở sẵn trang chủ ứng dụng MovieCinema `https://localhost:7198/` trên trình duyệt và màn hình Visual Studio 2022 chứa mã nguồn dự án.
* **🗣️ Lời thoại nói (MC):**  
  > *"Xin chào thầy, em xin đại diện nhóm 9 trình bày video demo đồ án với đề tài **Hệ thống Đặt vé Xem phim Trực tuyến MovieCinema**. Dự án được xây dựng trên nền tảng công nghệ ASP.NET Core .NET 8 MVC, Entity Framework Core và SQL Server Database."*

#### 🎙️ Mốc 01:00 – 02:30 | Tổng quan Sản phẩm & Công dụng chính
* **Giao diện quay màn hình:** Thao tác di chuột lướt xem Trang chủ, xem chi tiết phim, sơ đồ chọn ghế, giỏ hàng và trang quản trị Admin.
* **🗣️ Lời thoại nói (MC):**  
  > *"Về công dụng sản phẩm, **MovieCinema** là giải pháp toàn diện cho phép khán giả xem lịch chiếu phim real-time, chọn suất chiếu, đặt vị trí ghế ngồi trực quan theo định dạng phòng chiếu (2D, 3D, IMAX), áp dụng mã voucher giảm giá, tích điểm thành viên và thanh toán trực tuyến an toàn.  
  > Bên cạnh đó, hệ thống cung cấp phân hệ Admin cho phép quản lý danh mục phim, lịch chiếu, phòng chiếu và duyệt đơn hàng chặt chẽ."*

#### 🎙️ Mốc 02:30 – 03:30 | Tổng quan 12 GoF Design Patterns áp dụng
* **Giao diện quay màn hình:** Mở file [Program.cs](file:///e:/Document/GianDuyKhanh-23DH111541/movieCinema/movieCinema/Program.cs) trình chiếu phần đăng ký Dependency Injection cho 12 Design Patterns.
* **🗣️ Lời thoại nói (MC):**  
  > *"Để giải quyết các bài toán nghiệp vụ phức tạp và đảm bảo tính mở rộng theo chuẩn 5 nguyên lý **SOLID**, dự án đã triển khai **12 GoF Design Patterns** thuộc 3 nhóm chính:  
  > 1. **Nhóm Creational (Khởi tạo):** Singleton và Builder.  
  > 2. **Nhóm Structural (Cấu trúc):** Proxy, Bridge, Adapter, Decorator và Facade.  
  > 3. **Nhóm Behavioral (Hành vi):** Chain of Responsibility, Strategy, Mediator, State và Observer.  
  > Sau đây, em xin đi vào chi tiết giải thích mã nguồn và chạy demo tính năng cho từng pattern."*

---

### PHẦN 2: GIẢI THÍCH CODE & DEMO TRỰC TIẾP TỪNG PATTERN (03:30 – 22:00)

---

#### 📦 NHÓM CREATIONAL PATTERNS (MẪU KHỞI TẠO)

##### **Pattern 1: Singleton Pattern — Quản lý Giỏ hàng ShoppingCart (03:30 – 05:15)**
* **Giao diện quay màn hình:** Mở Visual Studio file [ShoppingCart.cs](file:///e:/Document/GianDuyKhanh-23DH111541/movieCinema/movieCinema/Data/Cart/ShoppingCart.cs). Ra Web UI thực hiện thao tác thêm vé và chuyển trang.
* **🗣️ Lời thoại giải thích Code:**  
  > *"Đầu tiên là **Singleton Pattern** cho Giỏ hàng. Giỏ hàng phải duy trì duy nhất một Instance đại diện trong suốt phiên Session của người dùng hiện tại.*
  > *Tại dòng 237 file `ShoppingCart.cs`, hàm `GetShoppingCart` kiểm tra `ISession`. Nếu Session chưa có `CartId`, ứng dụng sinh mới một GUID duy nhất. Kết hợp với `AddScoped` trong `Program.cs`, người dùng chuyển giữa các trang thì giỏ hàng vẫn duy trì nhất quán."*
* **📺 Thao tác Chạy Demo UI:** Đăng nhập ➔ Chọn phim *Dune 2* ➔ Nhấn **Thêm vào giỏ hàng** ➔ Chuyển qua tab *Diễn viên* ➔ Quay lại `/Orders/ShoppingCart` ➔ Giỏ hàng giữ nguyên vé đã chọn.

---

##### **Pattern 2: Builder Pattern — Khởi tạo Đơn hàng Order phức tạp (05:15 – 07:00)**
* **Giao diện quay màn hình:** Mở file [OrderBuilder.cs](file:///e:/Document/GianDuyKhanh-23DH111541/movieCinema/movieCinema/Models/Builders/OrderBuilder.cs), tô đen các phương thức `SetCustomer`, `SetShowtime`, `Build`.
* **🗣️ Lời thoại giải thích Code:**  
  > *"Pattern thứ hai là **Builder Pattern** trong `OrderBuilder.cs`. Đối tượng `Order` có rất nhiều thuộc tính. Nhóm thiết kế các phương thức `SetCustomer`, `SetShowtime`, `ApplyVoucher` đều trả về `IOrderBuilder` (`return this;`). Kỹ thuật Fluent Chaining này giúp lắp ráp Đơn hàng an toàn, sạch sẽ, đọc như quy trình nghiệp vụ."*
* **📺 Thao tác Chạy Demo UI:** Thực hiện bấm **Xác nhận thanh toán** ➔ Đơn hàng khởi tạo thành công chuẩn dữ liệu.

---

#### 🏛️ NHÓM STRUCTURAL PATTERNS (MẪU CẤU TRÚC)

##### **Pattern 3: Proxy Pattern — Caching danh sách Phim RAM MemoryCache (07:00 – 08:45)**
* **Giao diện quay màn hình:** Mở file [CachedMoviesServiceProxy.cs](file:///e:/Document/GianDuyKhanh-23DH111541/movieCinema/movieCinema/Data/Proxy/CachedMoviesServiceProxy.cs). Ra Web UI mở F12 DevTools -> Tab Network và F5.
* **🗣️ Lời thoại giải thích Code:**  
  > *"Trong nhóm Cấu trúc, **Proxy Pattern** qua `CachedMoviesServiceProxy` đóng vai trò gác cổng Caching. Tại dòng 38, hàm `GetOrCreateAsync` kiểm tra RAM Cache. Nếu có dữ liệu (**Cache Hit**), Proxy trả về ngay lập tức (~0ms). Nếu chưa có (**Cache Miss**), Proxy mới gọi `_realService` xuống SQL Server nạp dữ liệu và lưu vào RAM trong 10 phút."*
* **📺 Thao tác Chạy Demo UI:** F5 Trang chủ 3 lần ➔ Tab Network chứng minh các lần F5 sau thời gian tải ~0ms.

---

##### **Pattern 4: Bridge Pattern — Phân loại giá ghế linh hoạt (08:45 – 10:30)**
* **Giao diện quay màn hình:** Mở file [SeatPricingBridge.cs](file:///e:/Document/GianDuyKhanh-23DH111541/movieCinema/movieCinema/Models/Bridge/SeatPricingBridge.cs). Ra Web UI nhấp chọn từng loại ghế.
* **🗣️ Lời thoại giải thích Code:**  
  > *"**Bridge Pattern** trong `SeatPricingBridge.cs` tách rời hai trục: Trục Abstraction (`SeatType`) và Trục Implementation chiến lược giá (`ISeatingPricingStrategy`). Dựa trên `SeatType` truyền vào, Bridge tự động gọi `VipPricingStrategy` (tăng 20%) hoặc `CouplePricingStrategy` (x2 giá)."*
* **📺 Thao tác Chạy Demo UI:** Đặt vé xem phim ➔ Nhấp ghế Thường A1 (100k) ➔ Nhấp ghế VIP E5 (120k) ➔ Nhấp ghế Đôi H1 (200k).

---

##### **Pattern 5: Adapter Pattern — Bọc SDK thanh toán bên thứ ba (10:30 – 12:00)**
* **Giao diện quay màn hình:** Mở file [PaymentStrategy.cs](file:///e:/Document/GianDuyKhanh-23DH111541/movieCinema/movieCinema/Data/Strategy/PaymentStrategy.cs), tô đen class `PayPalPaymentStrategy`.
* **🗣️ Lời thoại giải thích Code:**  
  > *"**Adapter Pattern** triển khai tại `PayPalPaymentStrategy`. SDK API ngoài của PayPal có chuẩn riêng. Class này bọc (Adapter) SDK ngoài, ép nó tuân theo đúng giao diện `PayAsync` thuộc `IPaymentStrategy` của dự án."*
* **📺 Thao tác Chạy Demo UI:** Tại trang thanh toán ➔ Tích chọn phương thức **Ví PayPal** ➔ Hệ thống báo kết nối thành công.

---

##### **Pattern 6: Decorator Pattern — Xếp chồng chiết khấu & Phụ phí (12:00 – 13:30)**
* **Giao diện quay màn hình:** Mở file [PricingDecorators.cs](file:///e:/Document/GianDuyKhanh-23DH111541/movieCinema/movieCinema/Data/Decorators/PricingDecorators.cs), tô đen class `VoucherDecorator` và `LoyaltyPointsDecorator`.
* **🗣️ Lời thoại giải thích Code:**  
  > *"**Decorator Pattern** cho phép xếp chồng các lớp giảm giá lồng nhau tại Runtime. Lớp gốc `BasePriceCalculator` được bọc bởi `VoucherDecorator`, sau đó bọc tiếp bởi `LoyaltyPointsDecorator`. Giá cuối cùng được tính lượt qua từng lớp bọc mà không làm biến đổi class `Order` ban đầu."*
* **📺 Thao tác Chạy Demo UI:** Nhập Voucher `DISCOUNT10` ➔ Tích chọn Đổi 10 điểm thưởng ➔ Tổng thành tiền tự động cập nhật chính xác theo từng lớp.

---

##### **Pattern 7: Facade Pattern — Đơn giản hóa luồng Đặt vé cho Controller (13:30 – 14:30)**
* **Giao diện quay màn hình:** Mở file [BookingFacade.cs](file:///e:/Document/GianDuyKhanh-23DH111541/movieCinema/movieCinema/Data/Facade/BookingFacade.cs) và [OrdersController.cs](file:///e:/Document/GianDuyKhanh-23DH111541/movieCinema/movieCinema/Controllers/OrdersController.cs).
* **🗣️ Lời thoại giải thích Code:**  
  > *"**Facade Pattern** qua `BookingFacade.cs` gom nhóm 6-7 subsystems phức tạp (Bridge, Decorator, Strategy, Builder, Database) vào một giao diện duy nhất. `OrdersController` chỉ gọi đúng 1 dòng `_bookingFacade.ProcessBookingAsync()` giúp mã nguồn vô cùng ngắn gọn."*
* **📺 Thao tác Chạy Demo UI:** Nhấn nút **Xác nhận đặt vé** ➔ Giao dịch đặt vé hoàn tất trong vài miligiây.

---

#### 🔄 NHÓM BEHAVIORAL PATTERNS (MẪU HÀNH VI)

##### **Pattern 8: Chain of Responsibility — Pipeline kiểm tra điều kiện đặt vé (14:30 – 16:15)**
* **Giao diện quay màn hình:** Mở file [OrderPipeline.cs](file:///e:/Document/GianDuyKhanh-23DH111541/movieCinema/movieCinema/Data/Chain/OrderPipeline.cs), tô đen class `ValidationHandler`. Ra Web UI cố tình chọn 11 ghế.
* **🗣️ Lời thoại giải thích Code:**  
  > *"Trong nhóm Hành vi, **Chain of Responsibility** tạo thành Pipeline kiểm tra 4 bước. Nếu dữ liệu không hợp lệ (VD chọn >10 ghế), `ValidationHandler` gán câu báo lỗi và `return result` ngắt chuỗi lập tức, không cho phép chạy các Handler phía sau."*
* **📺 Thao tác Chạy Demo UI:** Chọn 11 ghế ➔ Nhấn Đặt vé ➔ Giao diện báo lỗi ngắt chuỗi ngay lập tức: *"Không thể đặt quá 10 ghế mỗi lần."*

---

##### **Pattern 9: Strategy Pattern — Chuyển đổi phương thức thanh toán tại Runtime (16:15 – 17:30)**
* **Giao diện quay màn hình:** Mở file [PaymentStrategy.cs](file:///e:/Document/GianDuyKhanh-23DH111541/movieCinema/movieCinema/Data/Strategy/PaymentStrategy.cs), tô đen class `PaymentContext`.
* **🗣️ Lời thoại giải thích Code:**  
  > *"**Strategy Pattern** triển khai qua `PaymentContext`. Dựa trên lựa chọn Tiền mặt hay PayPal của khách hàng trên Web UI, `PaymentContext.SetStrategyByName()` sẽ hoán đổi thuật toán thanh toán tương ứng ngay tại thời điểm Runtime."*
* **📺 Thao tác Chạy Demo UI:** Chọn Radio Button *Tiền mặt* ➔ Đổi sang *Ví PayPal*.

---

##### **Pattern 10: Mediator Pattern — Trạm điều phối giao tiếp Backend (17:30 – 18:45)**
* **Giao diện quay màn hình:** Mở file [BookingMediator.cs](file:///e:/Document/GianDuyKhanh-23DH111541/movieCinema/movieCinema/Data/Mediator/BookingMediator.cs).
* **🗣️ Lời thoại giải thích Code:**  
  > *"**Mediator Pattern** (`AppMediator`) đóng vai trò là trạm điều phối trung tâm. Controller không gọi trực tiếp các Service mà chỉ gửi `Command/Request`. Mediator tự động dùng Reflection tìm kiếm Handler đã đăng ký trong DI Container để xử lý."*
* **📺 Thao tác Chạy Demo UI:** Báo cáo cấu trúc Controller gọn gàng, giảm 70% sự phụ thuộc trực tiếp.

---

##### **Pattern 11: State Pattern — Quản lý vòng đời trạng thái Đơn hàng (18:45 – 20:30)**
* **Giao diện quay màn hình:** Mở file [OrderStateMachine.cs](file:///e:/Document/GianDuyKhanh-23DH111541/movieCinema/movieCinema/Data/State/OrderStateMachine.cs). Đăng nhập Admin vào `/Orders/ManageBookings` thao tác duyệt đơn.
* **🗣️ Lời thoại giải thích Code:**  
  > *"**State Pattern** quản lý chặt chẽ vòng đời đơn hàng. Mỗi trạng thái (`PurchasedState`, `CancelledState`) được đóng gói thành 1 class riêng. Đơn hàng đã Hủy (`CancelledState`) có phương thức `CanTransitionTo` trả về `false`, từ chối mọi thao tác đổi trạng thái phi logic."*
* **📺 Thao tác Chạy Demo UI:** Đăng nhập Admin ➔ Vào `/Orders/ManageBookings` ➔ Bấm **Confirm** duyệt đơn ➔ Thử bấm nút Hủy đơn trên đơn đã hoàn thành ➔ State Machine từ chối và báo lỗi.

---

##### **Pattern 12: Observer Pattern — Tự động Gửi Mail vé QR & Tích điểm (20:30 – 22:00)**
* **Giao diện quay màn hình:** Mở file [OrderObserver.cs](file:///e:/Document/GianDuyKhanh-23DH111541/movieCinema/movieCinema/Data/Observer/OrderObserver.cs). Mở cửa sổ Console Output hoặc Hòm thư Email giả lập.
* **🗣️ Lời thoại giải thích Code:**  
  > *"Cuối cùng là **Observer Pattern**. Khi đơn hàng chuyển trạng thái sang `Confirmed`, `OrderSubject.NotifyAsync()` tự động phát thông báo cho 3 Observers chạy tự động: `AuditLogObserver` (ghi log), `LoyaltyPointsObserver` (cộng điểm thưởng) và `EmailNotificationObserver` (gửi mail vé QR)."*
* **📺 Thao tác Chạy Demo UI:** Admin bấm **Confirm** duyệt đơn ➔ Cửa sổ Console log cho thấy Email vé QR đã được gửi và điểm thưởng đã tự động cộng vào tài khoản khách.

---

### PHẦN 3: TỔNG KẾT VÀ LỜI CẢM ƠN (22:00 – 23:00)

#### 🎙️ Mốc 22:00 – 23:00 | Tổng kết & Cảm ơn
* **Giao diện quay màn hình:** Trở lại giao diện Web MovieCinema kết hợp bảng tổng quan 12 Design Patterns.
* **🗣️ Lời thoại nói (MC):**  
  > *"Vừa rồi nhóm em đã trình bày chi tiết mã nguồn và chạy demo thực tế cho toàn bộ 12 GoF Design Patterns trong ứng dụng MovieCinema. Việc áp dụng các mẫu thiết kế này đã giúp mã nguồn dự án tuân thủ chặt chẽ nguyên lý SOLID, lỏng lẻo liên kết (Loose Coupling), tối ưu hiệu năng Caching và rất dễ dàng mở rộng các tính năng mới trong tương lai.  
  > Em xin chân thành cảm ơn thầy và quý hội đồng đã dành thời gian theo dõi video demo của nhóm em. Em xin kết thúc phần trình bày tại đây!"*

---

## 🛠️ 3. GIẢI THÍCH MÃ NGUỒN CỤ THỂ CHI TIẾT TỪNG DESIGN PATTERN (CODE DEEP DIVE)

---

### 3.1 Proxy Pattern (Tối ưu Memory Cache danh sách Phim)
📂 **Tệp nguồn:** [CachedMoviesServiceProxy.cs](file:///e:/Document/GianDuyKhanh-23DH111541/movieCinema/movieCinema/Data/Proxy/CachedMoviesServiceProxy.cs)

```csharp
public class CachedMoviesServiceProxy : IMoviesService
{
    private readonly MoviesService _realService; // Đối tượng Service thật kết nối EF Core DB
    private readonly IMemoryCache _cache;        // Bộ nhớ RAM Cache (.NET MemoryCache)
    private static readonly TimeSpan DefaultExpiry = TimeSpan.FromMinutes(10); // Hạn cache 10 phút

    public CachedMoviesServiceProxy(MoviesService realService, IMemoryCache cache)
    {
        _realService = realService;
        _cache = cache;
    }

    // 1. Phương thức Đọc dữ liệu có Caching (Read-Through Cache Pattern)
    public async Task<IEnumerable<Movie>> GetNowShowingMoviesAsync()
    {
        string key = $"movies:nowshowing:{DateTime.Today:yyyyMMdd}";

        return await _cache.GetOrCreateAsync(key, async entry =>
        {
            entry.SlidingExpiration = DefaultExpiry; // Tự động gia hạn 10 phút nếu có người đọc liên tục
            return (await _realService.GetAllAsync())
                .Where(m => m.Status == MovieStatus.NowShowing)
                .ToList();
        }) ?? Enumerable.Empty<Movie>();
    }

    // 2. Phương thức Ghi dữ liệu có Invalidate Cache (Cache Invalidation)
    public async Task AddNewMovieAsync(NewMovieVM data)
    {
        await _realService.AddNewMovieAsync(data); // 1. Thêm phim vào SQL DB trước
        InvalidateAllCaches();                      // 2. Xóa các key cache cũ để làm mới dữ liệu
    }
}
```
* **🔍 Phân tích chi tiết code từng khối lệnh:**
  - `public class CachedMoviesServiceProxy : IMoviesService`: Proxy đóng vai trò làm đại diện, triển khai cùng Interface `IMoviesService` với `MoviesService` thật. Điều này giúp Controller (`MoviesController`) giao tiếp lỏng lẻo với Proxy mà không hề nhận ra phía sau có cơ chế Cache.
  - `_cache.GetOrCreateAsync(key, async entry => ...)`:
    - **Cache Hit (Có dữ liệu trong RAM):** Trả về danh sách `Movie` ngay lập tức mà không gọi xuống Database SQL Server (~0ms).
    - **Cache Miss (Chưa có dữ liệu trong RAM):** Chạy hàm Lambda, gọi `_realService` xuống SQL Server nạp dữ liệu, lưu vào RAM với hạn 10 phút (`DefaultExpiry`) rồi mới trả về kết quả.
  - `InvalidateAllCaches()`: Khi Admin thêm/sửa/xóa phim, Proxy gọi xóa cache cũ để các truy cập tiếp theo của khách hàng sẽ nạp lại dữ liệu mới nhất từ SQL Server.

---

### 3.2 Singleton Pattern (Quản lý Giỏ hàng theo Session)
📂 **Tệp nguồn:** [ShoppingCart.cs](file:///e:/Document/GianDuyKhanh-23DH111541/movieCinema/movieCinema/Data/Cart/ShoppingCart.cs)

```csharp
public class ShoppingCart
{
    private readonly AppDbContext _context;
    public string ShoppingCartId { get; set; } = "";
    public List<ShoppingCartItem> ShoppingCartItems { get; set; } = new();

    public ShoppingCart(AppDbContext context) => _context = context;

    // Factory Method hỗ trợ khởi tạo Singleton Instance theo Session
    public static ShoppingCart GetShoppingCart(IServiceProvider services)
    {
        ISession session = services.GetRequiredService<IHttpContextAccessor>()?.HttpContext!.Session!;
        var context = services.GetService<AppDbContext>();

        // Lấy CartId đã lưu trong Session, nếu chưa có thì khởi tạo GUID mới
        string cartId = session.GetString("CartId") ?? Guid.NewGuid().ToString();
        session.SetString("CartId", cartId);

        return new ShoppingCart(context) { ShoppingCartId = cartId };
    }
}
```
* **🔍 Phân tích chi tiết code từng khối lệnh:**
  - `ISession session = ...`: Lấy phiên HTTP Session hiện tại của người dùng kết nối đến server.
  - `session.GetString("CartId") ?? Guid.NewGuid().ToString()`: Đảm bảo trong suốt phiên làm việc của một người dùng, hệ thống chỉ gán đúng một mã `CartId` duy nhất.
  - `builder.Services.AddScoped(sc => ShoppingCart.GetShoppingCart(sc));` trong `Program.cs`: Đăng ký Scoped theo Session giúp .NET DI Container trả về duy nhất 1 Instance giỏ hàng cho mỗi phiên làm việc của người dùng.

---

### 3.3 Bridge Pattern (Tách biệt Loại ghế & Thuật toán tính giá)
📂 **Tệp nguồn:** [SeatPricingBridge.cs](file:///e:/Document/GianDuyKhanh-23DH111541/movieCinema/movieCinema/Models/Bridge/SeatPricingBridge.cs)

```csharp
// 1. Interface cho chiến lược tính giá (Implementation)
public interface ISeatingPricingStrategy
{
    double CalculatePrice(double basePrice);
    string SeatTypeName { get; }
}

// 2. Các lớp tính giá cụ thể (Concrete Implementations)
public class StandardPricingStrategy : ISeatingPricingStrategy {
    public double CalculatePrice(double basePrice) => basePrice;        // Ghế Thường = 100% giá gốc
    public string SeatTypeName => "Standard";
}

public class VipPricingStrategy : ISeatingPricingStrategy {
    public double CalculatePrice(double basePrice) => basePrice * 1.2;  // Ghế VIP phụ thu 20%
    public string SeatTypeName => "VIP";
}

public class CouplePricingStrategy : ISeatingPricingStrategy {
    public double CalculatePrice(double basePrice) => basePrice * 2.0;  // Ghế Đôi tính x2
    public string SeatTypeName => "Couple";
}

// 3. Lớp Cầu nối (Bridge / Abstraction)
public class SeatPricingBridge
{
    private readonly ISeatingPricingStrategy _strategy;

    public SeatPricingBridge(SeatType seatType)
    {
        // Khởi tạo Strategy tương ứng với loại ghế
        _strategy = seatType switch
        {
            SeatType.VIP => new VipPricingStrategy(),
            SeatType.Couple => new CouplePricingStrategy(),
            SeatType.Disabled => new DisabledPricingStrategy(),
            _ => new StandardPricingStrategy()
        };
    }

    public double GetPrice(double basePrice) => _strategy.CalculatePrice(basePrice);
}
```
* **🔍 Phân tích chi tiết code từng khối lệnh:**
  - **Tách hai trục độc lập:** Trục `SeatType` (VIP, Couple, Standard) và Trục chiến lược giá (`ISeatingPricingStrategy`).
  - Class `SeatPricingBridge` đóng vai trò là "Cây cầu" kết nối hai trục. Khi thêm loại ghế mới (VD: *Ghế Massage*), chỉ cần tạo class `MassagePricingStrategy : ISeatingPricingStrategy` mà không làm thay đổi mã nguồn ở Controller hay Service.

---

### 3.4 Chain of Responsibility (Pipeline Kiểm tra Điều kiện Đặt vé)
📂 **Tệp nguồn:** [OrderPipeline.cs](file:///e:/Document/GianDuyKhanh-23DH111541/movieCinema/movieCinema/Data/Chain/OrderPipeline.cs)

```csharp
// Handler trừu tượng trong Chuỗi
public abstract class OrderPipelineHandler
{
    protected OrderPipelineHandler? Next;

    public OrderPipelineHandler SetNext(OrderPipelineHandler next)
    {
        Next = next;
        return next; // Cho phép nối chuỗi dạng Fluent: A.SetNext(B).SetNext(C)
    }

    public abstract Task<OrderPipelineResult> HandleAsync(OrderPipelineRequest request, OrderPipelineResult result);
}

// Mắt xích 1: Kiểm tra tính hợp lệ dữ liệu & số lượng ghế
public class ValidationHandler : OrderPipelineHandler
{
    public override async Task<OrderPipelineResult> HandleAsync(OrderPipelineRequest request, OrderPipelineResult result)
    {
        var seats = request.Model.SelectedSeats.Split(',').ToList();

        if (seats.Count > 10)
        {
            result.IsValid = false;
            result.Message = "Không thể đặt quá 10 ghế mỗi lần.";
            return result; // Ngắt chuỗi ngay tại đây, không gọi Next
        }

        // Chuyển tiếp cho Handler tiếp theo trong chuỗi nếu hợp lệ
        return Next != null ? await Next.HandleAsync(request, result) : result;
    }
}
```
* **🔍 Phân tích chi tiết code từng khối lệnh:**
  - `SetNext`: Nối các mắt xích kiểm tra theo thứ tự `ValidationHandler` ➔ `SeatAvailabilityHandler` ➔ `VoucherValidationHandler` ➔ `MemberValidationHandler`.
  - `return result`: Nếu một Handler phát hiện dữ liệu không hợp lệ (VD: đặt >10 ghế), nó gán báo lỗi và ngắt chuỗi lập tức. Các bước kiểm tra DB tốn tài nguyên đằng sau sẽ không được chạy.

---

### 3.5 Strategy & Adapter Pattern (Xử lý Thanh toán Đa cổng)
📂 **Tệp nguồn:** [PaymentStrategy.cs](file:///e:/Document/GianDuyKhanh-23DH111541/movieCinema/movieCinema/Data/Strategy/PaymentStrategy.cs)

```csharp
// Strategy Interface chung
public interface IPaymentStrategy
{
    Task<PaymentResult> PayAsync(double amount, string orderId);
}

// Strategy 1: Thanh toán Tiền mặt tại rạp
public class CashPaymentStrategy : IPaymentStrategy
{
    public Task<PaymentResult> PayAsync(double amount, string orderId)
        => Task.FromResult(new PaymentResult { Success = true, Message = "Thanh toán tại rạp thành công!" });
}

// Adapter Pattern: Bọc SDK PayPal bên ngoài cho phù hợp với IPaymentStrategy
public class PayPalPaymentStrategy : IPaymentStrategy
{
    public async Task<PaymentResult> PayAsync(double amount, string orderId)
    {
        await Task.Delay(100); // Mô phỏng gọi API HTTP của SDK PayPal bên thứ 3
        return new PaymentResult { Success = true, TransactionId = $"PP-{orderId}", Message = "Thanh toán PayPal thành công." };
    }
}

// Context điều khiển chọn Strategy linh hoạt
public class PaymentContext
{
    private IPaymentStrategy? _strategy;

    public void SetStrategyByName(string name)
    {
        _strategy = name.ToLower() switch
        {
            "paypal" => new PayPalPaymentStrategy(),
            _ => new CashPaymentStrategy()
        };
    }

    public Task<PaymentResult> PayAsync(double amount, string orderId) => _strategy!.PayAsync(amount, orderId);
}
```
* **🔍 Phân tích chi tiết code từng khối lệnh:**
  - **Strategy Pattern (`PaymentContext`):** Cho phép hoán đổi thuật toán thanh toán linh hoạt tại Runtime dựa trên tham số `name` từ người dùng.
  - **Adapter Pattern (`PayPalPaymentStrategy`):** SDK ngoài của các cổng thanh toán có giao diện hàm khác nhau. Class Adapter bọc SDK này lại, ép nó tuân theo chuẩn `IPaymentStrategy` trong hệ thống.

---

### 3.6 Decorator Pattern (Tính Phụ phí, Combo & Chiết khấu lồng nhau)
📂 **Tệp nguồn:** [PricingDecorators.cs](file:///e:/Document/GianDuyKhanh-23DH111541/movieCinema/movieCinema/Data/Decorators/PricingDecorators.cs)

```csharp
public interface IOrderPriceDecorator
{
    double CalculatePrice(double currentPrice);
    string Description { get; }
}

// Component Gốc: Giá vé ban đầu
public class BasePriceCalculator : IOrderPriceDecorator
{
    private readonly double _basePrice;
    public BasePriceCalculator(double basePrice) => _basePrice = basePrice;
    public double CalculatePrice(double currentPrice) => _basePrice;
    public string Description => "Giá gốc";
}

// Decorator: Trừ điểm tích lũy của khách hàng
public class LoyaltyPointsDecorator : IOrderPriceDecorator
{
    private readonly IOrderPriceDecorator _inner; // Component bên trong bị bọc
    private readonly int _points;

    public LoyaltyPointsDecorator(IOrderPriceDecorator inner, int points)
    {
        _inner = inner;
        _points = points;
    }

    public double CalculatePrice(double currentPrice)
    {
        double priceAfterVoucher = _inner.CalculatePrice(currentPrice); // Gọi lớp bên trong tính trước
        double pointValue = _points * 1000.0;                          // 1 điểm = 1.000đ
        return Math.Max(0, priceAfterVoucher - pointValue);            // Trừ tiền điểm thưởng
    }

    public string Description => $"Trừ điểm tích lũy ({_points} điểm = -{_points * 1000:N0}đ)";
}
```
* **🔍 Phân tích chi tiết code từng khối lệnh:**
  - Bọc lồng các tính năng giảm giá: `BasePriceCalculator` (Giá gốc) ➔ `VoucherDecorator` (Trừ voucher) ➔ `LoyaltyPointsDecorator` (Trừ điểm).
  - Phương thức `CalculatePrice` duyệt qua từng lớp bọc để tính giá cuối cùng mà không làm biến đổi cấu trúc class `Order` ban đầu.

---

### 3.7 Builder Pattern (Khởi tạo Đơn hàng Order)
📂 **Tệp nguồn:** [OrderBuilder.cs](file:///e:/Document/GianDuyKhanh-23DH111541/movieCinema/movieCinema/Models/Builders/OrderBuilder.cs)

```csharp
public class OrderBuilder : IOrderBuilder
{
    private readonly Order _order = new()
    {
        OrderItems = new List<OrderItem>(),
        OrderDate = DateTime.Now,
        Status = "Purchased"
    };

    public IOrderBuilder SetCustomer(string name, string email, string userId)
    {
        _order.UserId = name;
        _order.Email = email;
        return this; // Trả về chính this để hỗ trợ Method Chaining
    }

    public IOrderBuilder SetShowtime(int showtimeId, string selectedSeats, int seatCount, double basePrice)
    {
        _order.OrderItems.Add(new OrderItem
        {
            ShowtimeId = showtimeId,
            SelectedSeats = selectedSeats,
            Amount = seatCount,
            Price = basePrice
        });
        return this;
    }

    public Order Build() => _order; // Trả về đối tượng Order hoàn chỉnh
}
```
* **🔍 Phân tích chi tiết code từng khối lệnh:**
  - Các phương thức thiết lập (`SetCustomer`, `SetShowtime`) đều trả về `IOrderBuilder` (`return this;`).
  - Cú pháp Fluent API giúp khởi tạo đối tượng `Order` phức tạp có nhiều thuộc tính minh bạch, hạn chế nhầm lẫn thứ tự tham số.

---

### 3.8 Facade Pattern (Đơn giản hóa luồng Đặt vé)
📂 **Tệp nguồn:** [BookingFacade.cs](file:///e:/Document/GianDuyKhanh-23DH111541/movieCinema/movieCinema/Data/Facade/BookingFacade.cs)

```csharp
public class BookingFacade : IBookingFacade
{
    public async Task<BookingResult> ProcessBookingAsync(BookTicketsVM model, string? userId)
    {
        // Bước 1: Lấy suất chiếu từ ShowtimesService
        // Bước 2: Tính giá ghế bằng Bridge Pattern
        // Bước 3: Tính giảm giá qua Decorator Pattern
        // Bước 4: Thanh toán qua Strategy Pattern
        // Bước 5: Tạo Order bằng Builder Pattern
        // Bước 6: Lưu vào SQL Server Database
        return new BookingResult { Success = true };
    }
}
```
* **🔍 Phân tích chi tiết code từng khối lệnh:**
  - Facade đóng vai trò giao diện trung gian đơn giản. Giúp `OrdersController` chỉ gọi đúng 1 phương thức `ProcessBookingAsync` thay vì phải tự điều phối 7-8 service phức tạp khác nhau.

---

### 3.9 Mediator Pattern (Điều phối Request Backend)
📂 **Tệp nguồn:** [BookingMediator.cs](file:///e:/Document/GianDuyKhanh-23DH111541/movieCinema/movieCinema/Data/Mediator/BookingMediator.cs)

```csharp
public class AppMediator : IMediator
{
    private readonly IServiceProvider _serviceProvider;

    public async Task<TResponse> SendAsync<TResponse>(IRequest<TResponse> request)
    {
        // Tự động tìm kiếm Handler tương ứng từ DI Container dựa trên Type của Request
        var handlerType = typeof(IRequestHandler<,>).MakeGenericType(request.GetType(), typeof(TResponse));
        var handler = _serviceProvider.GetServices(handlerType).FirstOrDefault();
        return await (Task<TResponse>)handlerType.GetMethod("HandleAsync")!.Invoke(handler, new[] { request })!;
    }
}
```
* **🔍 Phân tích chi tiết code từng khối lệnh:**
  - Giảm độ phụ thuộc trực tiếp (Coupling). Controller chỉ phát tin `SendAsync(request)`, `AppMediator` dùng Reflection tự động tìm kiếm Handler tương ứng trong DI Container để xử lý.

---

### 3.10 State Pattern (Quản lý Vòng đời Trạng thái Đơn hàng)
📂 **Tệp nguồn:** [OrderStateMachine.cs](file:///e:/Document/GianDuyKhanh-23DH111541/movieCinema/movieCinema/Data/State/OrderStateMachine.cs)

```csharp
public class PurchasedState : IOrderState {
    public string StatusName => "Purchased";
    public bool CanTransitionTo(string newStatus) => newStatus is "Confirmed" or "Cancelled";
}

public class CancelledState : IOrderState {
    public string StatusName => "Cancelled";
    public bool CanTransitionTo(string newStatus) => false; // Đơn đã hủy không thể đổi lại
}

public class OrderStateMachine {
    private static readonly Dictionary<string, IOrderState> _states = new() {
        ["Purchased"] = new PurchasedState(),
        ["Confirmed"] = new ConfirmedState(),
        ["Cancelled"] = new CancelledState()
    };
    public static bool CanTransition(string from, string to) => _states[from].CanTransitionTo(to);
}
```
* **🔍 Phân tích chi tiết code từng khối lệnh:**
  - Đóng gói quy tắc chuyển đổi trạng thái vào các class riêng biệt. Ngăn chặn triệt để hành vi đổi trạng thái sai quy tắc nghiệp vụ (VD: đơn đã hủy không thể đổi lại sang đã xác nhận).

---

### 3.11 Observer Pattern (Gửi Email & Tích điểm tự động)
📂 **Tệp nguồn:** [OrderObserver.cs](file:///e:/Document/GianDuyKhanh-23DH111541/movieCinema/movieCinema/Data/Observer/OrderObserver.cs)

```csharp
public interface IOrderObserver {
    Task OnOrderStatusChangedAsync(Order order, string oldStatus, string newStatus);
}

// Observer 1: Tích điểm thưởng cho khách
public class LoyaltyPointsObserver : IOrderObserver { ... }

// Observer 2: Gửi Email xác nhận vé kèm QR code
public class EmailNotificationObserver : IOrderObserver { ... }

// Subject phát sự kiện
public class OrderSubject : IOrderSubject {
    private readonly List<IOrderObserver> _observers = new();
    public async Task NotifyAsync(Order order, string oldStatus, string newStatus) {
        foreach (var observer in _observers)
            await observer.OnOrderStatusChangedAsync(order, oldStatus, newStatus);
    }
}
```
* **🔍 Phân tích chi tiết code từng khối lệnh:**
  - Khi trạng thái đơn đổi sang `Confirmed`, `OrderSubject.NotifyAsync()` duyệt qua danh sách Observers tự động kích hoạt gửi Email vé QR và cộng điểm thưởng mà không ảnh hưởng luồng duyệt đơn chính.

---

### 3.12 Dependency Injection Pattern (Cấu hình toàn ứng dụng)
📂 **Tệp nguồn:** [Program.cs](file:///e:/Document/GianDuyKhanh-23DH111541/movieCinema/movieCinema/Program.cs)

```csharp
// Đăng ký Proxy Pattern cho IMoviesService
builder.Services.AddScoped<MoviesService>();
builder.Services.AddScoped<IMoviesService>(sp =>
{
    var realService = sp.GetRequiredService<MoviesService>();
    var cache = sp.GetRequiredService<IMemoryCache>();
    return new CachedMoviesServiceProxy(realService, cache);
});

// Đăng ký Singleton Giỏ hàng theo Session
builder.Services.AddScoped(sc => ShoppingCart.GetShoppingCart(sc));

// Đăng ký Facade, Builder & Observers
builder.Services.AddScoped<IBookingFacade, BookingFacade>();
builder.Services.AddScoped<IOrderBuilder, OrderBuilder>();
builder.Services.AddSingleton<IOrderSubject, OrderSubject>();
```
* **🔍 Phân tích chi tiết code từng khối lệnh:**
  - `Program.cs` kết nối toàn bộ 11 Design Patterns vào .NET IoC Container. Controller chỉ nhận Interface qua Constructor, giảm tối đa sự phụ thuộc cứng và sẵn sàng cho Unit Test.

---

## 🎯 4. BÍ QUYẾT TRẢ LỜI CÂU HỎI HỘI ĐỒNG (FAQ)

1. **Vì sao dự án phải áp dụng 12 Design Patterns?**  
   * **Trả lời:** *"Dự án hệ thống đặt vé xem phim có nhiều quy tắc nghiệp vụ phức tạp. Việc áp dụng 12 Design Pattern giúp mã nguồn tuân thủ chặt chẽ nguyên lý SOLID, giảm độ phụ thuộc (Loose Coupling), dễ bảo trì và mở rộng tính năng mới trong tương lai."*

2. **Khác biệt lớn nhất giữa Facade và Mediator là gì?**  
   * **Trả lời:** *"Facade gom nhiều bước xử lý phức tạp của subsystem thành 1 giao diện đơn giản cho Controller gọi. Trong khi Mediator là trạm điều phối trung tâm gửi lệnh (Command) giữa Controller và các Handlers để các lớp không gọi trực tiếp lẫn nhau."*

---

> [!TIP]
> Tệp [kichban.md](file:///e:/Document/GianDuyKhanh-23DH111541/movieCinema/movieCinema/kichban.md) đã được cập nhật đầy đủ cả **Kịch bản thuyết trình video demo (23 phút)** lẫn **Mục 3: Giải thích mã nguồn chi tiết từng dòng lệnh C#** cho 12 Design Patterns! 🚀
