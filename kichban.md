# 🎬 KỊCH BẢN DEMO WEB VÀ GIẢI THÍCH DESIGN PATTERNS - MOVIECINEMA

> **Dự án:** Hệ thống đặt vé xem phim trực tuyến **MovieCinema** (.NET 8 ASP.NET Core MVC, Entity Framework Core, SQL Server)  
> **Mục tiêu:** Hướng dẫn từng bước thao tác demo trực quan trên giao diện Web, kết hợp trình bày nguyên lý, lời thoại thuyết trình và giải thích chi tiết nguồn code của **12 GoF Design Patterns** đã áp dụng thành công trong dự án.

---

## 📌 1. BẢNG TỔNG QUAN 12 GOF DESIGN PATTERNS TRONG HỆ THỐNG

| Nhóm Pattern | STT | Tên Pattern | Vị trí Thao tác trên Web (UI) | File Code Backend Triển khai | Công dụng / Lợi ích Kiến trúc mang lại |
|:---:|:---:|:---|:---|:---|:---|
| **Creational**<br>*(Khởi tạo)* | **1** | **Singleton** | Giỏ hàng vé & Đồ ăn (`/Orders/ShoppingCart`) | [ShoppingCart.cs](file:///e:/Document/GianDuyKhanh-23DH111541/movieCinema/movieCinema/Data/Cart/ShoppingCart.cs) | Đảm bảo duy nhất 1 Instance Giỏ hàng trong suốt Session của người dùng |
| | **2** | **Builder** | Xử lý tạo Đơn hàng Backend sau thanh toán | [OrderBuilder.cs](file:///e:/Document/GianDuyKhanh-23DH111541/movieCinema/movieCinema/Models/Builders/OrderBuilder.cs) | Khởi tạo đối tượng `Order` phức tạp có nhiều thuộc tính từng bước an toàn, rõ ràng |
| **Structural**<br>*(Cấu trúc)* | **3** | **Proxy** | Trang chủ (`/`), Trang *Phim đang chiếu* | [CachedMoviesServiceProxy.cs](file:///e:/Document/GianDuyKhanh-23DH111541/movieCinema/movieCinema/Data/Proxy/CachedMoviesServiceProxy.cs) | Caching danh sách phim vào RAM, giảm 80-90% truy vấn trực tiếp xuống Database SQL |
| | **4** | **Bridge** | Sơ đồ chọn ghế (`/Showtimes/Book/ID`) | [SeatPricingBridge.cs](file:///e:/Document/GianDuyKhanh-23DH111541/movieCinema/movieCinema/Models/Bridge/SeatPricingBridge.cs) | Tách biệt loại ghế (VIP/Couple/Standard) khỏi công thức tính giá tương ứng |
| | **5** | **Adapter** | Form chọn phương thức thanh toán | [PaymentStrategy.cs](file:///e:/Document/GianDuyKhanh-23DH111541/movieCinema/movieCinema/Data/Strategy/PaymentStrategy.cs) | Bọc API / SDK cổng thanh toán bên thứ 3 (PayPal/MoMo) về giao diện chuẩn dự án |
| | **6** | **Decorator** | Ô nhập Voucher / Chọn Combo / Trừ điểm | [PricingDecorators.cs](file:///e:/Document/GianDuyKhanh-23DH111541/movieCinema/movieCinema/Data/Decorators/PricingDecorators.cs) | Xếp chồng các lớp chiết khấu & phụ phí lồng nhau mà không sửa đổi Order gốc |
| | **7** | **Facade** | Cổng đại diện cho luồng đặt vé | [BookingFacade.cs](file:///e:/Document/GianDuyKhanh-23DH111541/movieCinema/movieCinema/Data/Facade/BookingFacade.cs) | Gom 8 bước xử lý đặt vé phức tạp vào 1 interface đơn giản giúp Controller cực gọn |
| **Behavioral**<br>*(Hành vi)* | **8** | **Chain of Resp.** | Nút "Xác nhận đặt vé" | [OrderPipeline.cs](file:///e:/Document/GianDuyKhanh-23DH111541/movieCinema/movieCinema/Data/Chain/OrderPipeline.cs) | Chuỗi 4 bước kiểm tra (Ghế trống -> Hạn vé -> Voucher -> Điểm), vi phạm bước nào dừng ngay |
| | **9** | **Strategy** | Chọn phương thức thanh toán Cash / PayPal | [PaymentStrategy.cs](file:///e:/Document/GianDuyKhanh-23DH111541/movieCinema/movieCinema/Data/Strategy/PaymentStrategy.cs) | Chuyển đổi linh hoạt giữa các thuật toán thanh toán tại thời điểm Runtime |
| | **10** | **Mediator** | Luồng điều phối giữa Controller & Handler | [BookingMediator.cs](file:///e:/Document/GianDuyKhanh-23DH111541/movieCinema/movieCinema/Data/Mediator/BookingMediator.cs) | Trạm điều phối trung tâm gửi Command/Request, giảm sự phụ thuộc trực tiếp giữa các lớp |
| | **11** | **State** | Trang Quản lý Đơn hàng (`/Orders/Index`) | [OrderStateMachine.cs](file:///e:/Document/GianDuyKhanh-23DH111541/movieCinema/movieCinema/Data/State/OrderStateMachine.cs) | Kiểm soát vòng đời trạng thái đơn (Purchased -> Confirmed -> Cancelled) đúng quy tắc |
| | **12** | **Observer** | Tự động sau khi duyệt / hủy đơn hàng | [OrderObserver.cs](file:///e:/Document/GianDuyKhanh-23DH111541/movieCinema/movieCinema/Data/Observer/OrderObserver.cs) | Tự động Gửi Mail vé QR, Cộng/Hoàn điểm thưởng & Ghi Log khi trạng thái đơn đổi |

---

## 📽️ 2. KỊCH BẢN THUYẾT TRÌNH VÀ DEMO WEB THỰC TẾ

### 🎙️ PHẦN MỞ ĐẦU THUYẾT TRÌNH (00:00 - 01:00)
> *"Xin chào thầy và hội đồng, em xin đại diện nhóm trình bày đồ án **Hệ thống Đặt vé Xem phim Trực tuyến MovieCinema** (.NET 8 ASP.NET Core MVC, Entity Framework Core, SQL Server).  
> Đồ án được thiết kế chuẩn kiến trúc với việc áp dụng thành công **11 Design Patterns** chia thành 3 nhóm: **Creational** (Khởi tạo), **Structural** (Cấu trúc) và **Behavioral** (Hành vi). Sau đây em xin bắt đầu demo từng phần tương ứng với luồng người dùng thực tế."*

---

### 📦 PHẦN I: NHÓM CREATIONAL PATTERNS (MẪU KHỞI TẠO)

#### **1. Singleton Pattern — Quản lý Giỏ hàng (ShoppingCart)**
* **🗣️ Lời thoại Thuyết trình:**  
  *"Pattern đầu tiên thuộc nhóm Creational là **Singleton Pattern** áp dụng cho chức năng Giỏ hàng. Giỏ hàng phải duy trì duy nhất một đối tượng đại diện (Instance) xuyên suốt phiên Session của người dùng hiện tại, tránh việc mỗi lần người dùng chuyển trang lại tạo mới một giỏ hàng làm mất dữ liệu."*
* **📺 Thao tác Web Demo:**  
  1. Đăng nhập tài khoản khách hàng.
  2. Chọn một bộ phim ➔ Nhấn **"Thêm vào giỏ hàng"**.
  3. Chuyển sang các tab khác trên website (*Danh sách phim, Diễn viên, Rạp chiếu*).
  4. Truy cập lại trang Giỏ hàng `/Orders/ShoppingCart`.
* **📺 Hiện tượng quan sát được:**  
  Danh sách vé phim và đồ ăn đã chọn trong giỏ hàng vẫn được duy trì đầy đủ và nhất quán.

---

#### **2. Builder Pattern — Khởi tạo đối tượng Đơn hàng (Order) phức tạp**
* **🗣️ Lời thoại Thuyết trình:**  
  *"Khi khách hàng nhấn Đặt vé, một đối tượng Đơn hàng (`Order`) chứa rất nhiều thông tin phức tạp: Thông tin khách hàng, Suất chiếu, Ghế ngồi, Mã giảm giá, Phương thức thanh toán, Tổng tiền... Nhóm sử dụng **Builder Pattern** với kỹ thuật Fluent Chaining giúp lắp ráp đối tượng Order theo từng bước rõ ràng, minh bạch."*
* **📺 Thao tác Web Demo:**  
  Thực hiện thao tác nhấn **"Xác nhận thanh toán & Tạo đơn hàng"**.
* **📺 Hiện tượng quan sát được:**  
  Đơn hàng được khởi tạo thành công với đầy đủ thuộc tính chính xác và lưu xuống SQL Server an toàn mà không bị sai sót vị trí tham số.

---

### 🏛️ PHẦN II: NHÓM STRUCTURAL PATTERNS (MẪU CẤU TRÚC)

#### **3. Proxy Pattern — Tối ưu tải Trang chủ bằng Cache**
* **🗣️ Lời thoại Thuyết trình:**  
  *"Tại Trang chủ của website, ứng dụng cần hiển thị danh sách phim đang chiếu. Nếu mỗi lần F5 trang mà ứng dụng đều query xuống SQL Server thì database sẽ bị quá tải. Nhóm áp dụng **Proxy Pattern** qua `CachedMoviesServiceProxy`. Proxy đóng vai trò gác cổng: kiểm tra RAM MemoryCache trước, nếu có dữ liệu thì trả về ngay (~0ms), nếu chưa có mới truy vấn SQL Server."*
* **📺 Thao tác Web Demo:**  
  1. Truy cập Trang chủ `https://localhost:7198/`.
  2. Nhấn `F5` refresh trang nhiều lần.
  3. Mở F12 DevTools -> Tab Network quan sát thời gian phản hồi.
* **📺 Hiện tượng quan sát được:**  
  Các lần F5 tiếp theo trang nạp tức thì do dữ liệu được trả trực tiếp từ RAM Cache.

---

#### **4. Bridge Pattern — Phân loại giá ghế linh hoạt**
* **🗣️ Lời thoại Thuyết trình:**  
  *"Tại sơ đồ phòng chiếu, rạp có nhiều loại ghế khác nhau (Ghế Thường, VIP, Couple) với chính sách giá riêng. Nhóm áp dụng **Bridge Pattern** để tách biệt Abstraction loại ghế khỏi Implementation công thức tính giá (`ISeatingPricingStrategy`). Nhờ đó, khi rạp bổ sung thêm loại ghế mới như 'Ghế Massage', nhóm chỉ cần tạo thêm class chiến lược giá mới mà không sửa Controller."*
* **📺 Thao tác Web Demo:**  
  Vào chi tiết Phim ➔ Nhấn **Đặt vé** chọn Suất chiếu ➔ Màn hình sơ đồ ghế hiện ra:
  1. Chọn 1 Ghế Thường (Standard - A1) ➔ Giá = 100% giá gốc (100.000đ).
  2. Chọn 1 Ghế VIP (E5) ➔ Giá tự động tăng 20% (120.000đ).
  3. Chọn 1 Ghế Đôi (Couple - H1) ➔ Giá tự động tính gấp đôi (200.000đ).

---

#### **5. Adapter Pattern — Bọc SDK cổng thanh toán bên thứ ba**
* **🗣️ Lời thoại Thuyết trình:**  
  *"Hệ thống hỗ trợ thanh toán qua cổng trực tuyến bên thứ 3 như PayPal hay MoMo. Các SDK này có giao diện API riêng. Nhóm áp dụng **Adapter Pattern** (`PayPalPaymentStrategy`) bọc SDK bên ngoài về cùng chuẩn giao tiếp `IPaymentStrategy` của hệ thống."*
* **📺 Thao tác Web Demo:**  
  Tại bước chọn phương thức thanh toán, tích chọn **Ví PayPal / Thẻ quốc tế** ➔ Nhấn Đặt vé.
* **📺 Hiện tượng quan sát được:**  
  Hệ thống kết nối và mô phỏng giao dịch thành công với cổng PayPal.

---

#### **6. Decorator Pattern — Xếp chồng chiết khấu & Phụ phí đơn hàng**
* **🗣️ Lời thoại Thuyết trình:**  
  *"Đơn hàng gốc có giá vé ban đầu. Đơn hàng có thể được bọc thêm Voucher giảm giá, trừ điểm tích lũy, hoặc giảm giá giờ vàng (Happy Hour). Nhóm dùng **Decorator Pattern** để xếp chồng các tính năng tính tiền này lồng nhau một cách linh hoạt tại Runtime."*
* **📺 Thao tác Web Demo:**  
  1. Nhập Mã Voucher giảm giá (`DISCOUNT10` -> Giảm 10%).
  2. Tích chọn Đổi Điểm thưởng tích lũy (Dùng 10 điểm -> Giảm 10.000đ).
* **📺 Hiện tượng quan sát được:**  
  Dòng tổng tiền thanh toán được biến đổi động theo từng lớp bọc: Giá gốc ➔ Bọc Voucher ➔ Bọc Trừ điểm ➔ Trọng số tổng thành tiền cuối cùng hoàn toàn chính xác.

---

#### **7. Facade Pattern — Đơn giản hóa toàn bộ luồng Đặt vé**
* **🗣️ Lời thoại Thuyết trình:**  
  *"Thực tế luồng đặt vé phải gọi 7-8 service phức tạp bên dưới. Nhóm dùng **Facade Pattern** (`BookingFacade`) đóng vai trò là giao diện đại diện duy nhất, giúp `OrdersController` chỉ cần gọi đúng 1 dòng lệnh mà vẫn xử lý trơn tru toàn bộ quy trình."*
* **📺 Thao tác Web Demo:**  
  Khách hàng nhấn nút **"Xác nhận thanh toán & Đặt vé"**.
* **📺 Hiện tượng quan sát được:**  
  Giao dịch được xử lý hoàn tất mượt mà chỉ trong vài mili-giây.

---

### 🔄 PHẦN III: NHÓM MẪU HÀNH VI (BEHAVIORAL PATTERNS)

#### **8. Chain of Responsibility — Pipeline kiểm tra điều kiện đặt vé**
* **🗣️ Lời thoại Thuyết trình:**  
  *"Trước khi tạo đơn, dữ liệu được truyền qua một Pipeline gồm 4 Handler kiểm tra tuần tự (**Chain of Responsibility**). Nếu vi phạm bước nào (ví dụ đặt quá 10 ghế), Handler đó dừng ngay lập tức và báo lỗi mà không lãng phí tài nguyên xử lý các bước đằng sau."*
* **📺 Thao tác Web Demo:**  
  Chọn 11 ghế cùng lúc ➔ Nhấn **Đặt vé**.
* **📺 Hiện tượng quan sát được:**  
  `ValidationHandler` ngắt chuỗi và hiển thị thông báo lỗi lập tức: *"Không thể đặt quá 10 ghế mỗi lần."*

---

#### **9. Strategy Pattern — Chuyển đổi phương thức thanh toán tại Runtime**
* **🗣️ Lời thoại Thuyết trình:**  
  *"Nhóm áp dụng **Strategy Pattern** qua `PaymentContext`. Tùy thuộc vào việc khách hàng chọn 'Tiền mặt' hay 'PayPal' trên Web UI, `PaymentContext` sẽ hoán đổi thuật toán xử lý thanh toán tương ứng ngay tại Runtime."*
* **📺 Thao tác Web Demo:**  
  Chuyển đổi lựa chọn giữa *Tiền mặt tại rạp* và *Ví PayPal*.

---

#### **10. Mediator Pattern — Trạm điều phối giao tiếp Backend**
* **🗣️ Lời thoại Thuyết trình:**  
  *"Để tránh các Controller phụ thuộc trực tiếp vào nhiều Service xử lý, nhóm dùng **Mediator Pattern** (`AppMediator`) làm trạm điều phối trung tâm nhận và chuyển tiếp các Command/Request."*
* **📺 Thao tác Web Demo:**  
  Trình bày cấu trúc code trong Controller cực kỳ gọn gàng, giảm 70% độ phức tạp phụ thuộc.

---

#### **11. State Pattern — Quản lý vòng đời trạng thái Đơn hàng**
* **🗣️ Lời thoại Thuyết trình:**  
  *"Đơn hàng có quy tắc chuyển trạng thái nghiêm ngặt. Nhóm áp dụng **State Pattern**. Mỗi trạng thái là 1 class độc lập. Đơn hàng đã Hủy (`CancelledState`) sẽ khóa toàn bộ luồng, không cho phép đổi sang bất kỳ trạng thái nào khác."*
* **📺 Thao tác Web Demo:**  
  Đăng nhập Admin ➔ Vào Quản lý đơn hàng (`/Orders/ManageBookings`) ➔ Duyệt đơn từ `Purchased` sang `Confirmed` ➔ Thử bấm nút Hủy đơn trên đơn đã hoàn tất.
* **📺 Hiện tượng quan sát được:**  
  State Machine từ chối thao tác sai quy tắc và báo lỗi nghiệp vụ.

---

#### **12. Observer Pattern — Tự động Gửi Mail vé QR & Tích điểm**
* **🗣️ Lời thoại Thuyết trình:**  
  *"Khi đơn hàng chuyển sang trạng thái `Confirmed`, `OrderSubject` tự động phát thông báo cho 3 Observers chạy tự động: `AuditLogObserver` (ghi log), `LoyaltyPointsObserver` (cộng điểm cho khách) và `EmailNotificationObserver` (gửi mail vé QR)."*
* **📺 Thao tác Web Demo:**  
  Admin bấm **Confirm** duyệt đơn ➔ Mở Hòm thư Email / Log Console xem thông báo tự động.

---

## 🛠️ 3. GIẢI THÍCH MÃ NGUỒN CHI TIẾT TỪNG DESIGN PATTERN

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
* **🔍 Phân tích chi tiết code:**
  - **Dòng 11:** Proxy triển khai chung interface `IMoviesService` với `MoviesService` thật. Do đó Controller (`MoviesController`) chỉ cần inject `IMoviesService` mà không cần biết phía sau là Proxy hay Service thật (**Tuân thủ DIP - Dependency Inversion Principle**).
  - **Dòng 71-77:** `_cache.GetOrCreateAsync` kiểm tra RAM Cache. Nếu đã có dữ liệu (**Cache Hit**), trả về ngay danh sách `Movie` (~0ms). Nếu chưa có (**Cache Miss**), mới kích hoạt lambda function gọi `_realService` xuống SQL Server nạp dữ liệu và lưu vào RAM.
  - **Dòng 107-111:** Khi Admin thêm/sửa phim, Proxy ghi vào DB SQL trước rồi gọi `InvalidateAllCaches()` để xóa cache cũ, đảm bảo người dùng luôn thấy phim mới nhất.

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
* **🔍 Phân tích chi tiết code:**
  - **Dòng 237:** `ISession session = ...` lấy phiên HTTP Session hiện tại của người dùng.
  - **Dòng 241:** `session.GetString("CartId") ?? Guid.NewGuid().ToString()` đảm bảo một người dùng trong cùng 1 phiên duyệt web luôn sở hữu đúng một `CartId` duy nhất.
  - Đăng ký `builder.Services.AddScoped(sc => ShoppingCart.GetShoppingCart(sc));` trong [Program.cs](file:///e:/Document/GianDuyKhanh-23DH111541/movieCinema/movieCinema/Program.cs) giúp .NET DI Container tự động inject đúng đối tượng giỏ hàng hiện tại vào bất kỳ Controller nào.

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
* **🔍 Phân tích chi tiết code:**
  - **Tách biệt Abstraction & Implementation:** `SeatType` (VIP, Couple, Standard) và `ISeatingPricingStrategy` là 2 trục biến đổi độc lập. Class `SeatPricingBridge` kết nối 2 trục này lại với nhau.
  - Khi rạp mở thêm loại ghế mới (VD: *Ghế Massage*), chỉ cần tạo class `MassagePricingStrategy : ISeatingPricingStrategy` mà không làm thay đổi mã nguồn Controller.

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
* **🔍 Phân tích chi tiết code:**
  - **Cơ chế Pipeline:** Dữ liệu đặt vé truyền qua chuỗi: `ValidationHandler` ➔ `SeatAvailabilityHandler` ➔ `VoucherValidationHandler` ➔ `MemberValidationHandler`.
  - Nếu bất kỳ mắt xích nào thất bại (VD: chọn >10 ghế), Handler gán câu báo lỗi và **`return result` ngắt chuỗi ngay lập tức**, giúp tiết kiệm tài nguyên không cần xử lý các bước phía sau.

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
* **🔍 Phân tích chi tiết code:**
  - **Strategy Pattern (`PaymentContext`):** Cho phép hoán đổi thuật toán thanh toán linh hoạt tại Runtime dựa trên lựa chọn Cash / PayPal của người dùng trên Web.
  - **Adapter Pattern (`PayPalPaymentStrategy`):** SDK ngoài của PayPal/MoMo có giao diện API riêng. Class Adapter bọc SDK này lại, ép nó tuân theo chuẩn `IPaymentStrategy` của dự án.

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
* **🔍 Phân tích chi tiết code:**
  - Bọc lồng các lớp tính tiền: `BasePriceCalculator` (Giá gốc) ➔ `VoucherDecorator` (Trừ voucher) ➔ `LoyaltyPointsDecorator` (Trừ điểm). Kết quả cuối cùng là tổng chiết khấu qua tất cả các lớp bọc mà không sửa đổi class `Order` ban đầu.

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
* **🔍 Phân tích chi tiết code:**
  - Các phương thức thiết lập (`SetCustomer`, `SetShowtime`) đều trả về `IOrderBuilder` (`return this;`), cho phép khởi tạo đối tượng `Order` phức tạp bằng cú pháp Fluent API trong trẻo, minh bạch.

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
* **🔍 Phân tích chi tiết code:**
  - Che giấu sự phức tạp của 6-7 subsystems đằng sau interface `IBookingFacade`. `OrdersController` chỉ gọi đúng 1 dòng: `await _bookingFacade.ProcessBookingAsync(model, userId);`.

---

### 3.9 Mediator Pattern (Điều phối Request Backend)
📂 **Tệp nguồn:** [BookingMediator.cs](file:///e:/Document/GianDuyKhanh-23DH111541/movieCinema/movieCinema/Data/Mediator/BookingMediator.cs)

```csharp
public class AppMediator : IMediator
{
    private readonly IServiceProvider _serviceProvider;

    public async Task<TResponse> SendAsync<TResponse>(IRequest<TResponse> request)
    {
        // Tìm kiếm Handler tương ứng từ DI Container dựa trên Type của Request
        var handlerType = typeof(IRequestHandler<,>).MakeGenericType(request.GetType(), typeof(TResponse));
        var handler = _serviceProvider.GetServices(handlerType).FirstOrDefault();
        return await (Task<TResponse>)handlerType.GetMethod("HandleAsync")!.Invoke(handler, new[] { request })!;
    }
}
```
* **🔍 Phân tích chi tiết code:**
  - Controller không cần inject trực tiếp nhiều Service mà chỉ gửi `Command` qua `AppMediator.SendAsync()`. Mediator dùng Reflection tự động tìm kiếm Handler tương ứng để thực thi.

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
* **🔍 Phân tích chi tiết code:**
  - Đóng gói quy tắc chuyển đổi trạng thái vào các class riêng biệt. Ngăn chặn triệt để hành vi đổi trạng thái sai quy tắc (VD: đơn đã hủy không thể đổi lại sang đã xác nhận).

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
* **🔍 Phân tích chi tiết code:**
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

---

## 🎯 4. BÍ QUYẾT TRẢ LỜI CÂU HỎI HỘI ĐỒNG (FAQ)

1. **Vì sao dự án phải áp dụng 12 Design Patterns?**  
   * **Trả lời:** *"Dự án hệ thống đặt vé xem phim có nhiều quy tắc nghiệp vụ phức tạp. Việc áp dụng 12 Design Pattern giúp mã nguồn tuân thủ chặt chẽ nguyên lý SOLID, giảm độ phụ thuộc (Loose Coupling), dễ bảo trì và mở rộng tính năng mới trong tương lai."*

2. **Khác biệt lớn nhất giữa Facade và Mediator là gì?**  
   * **Trả lời:** *"Facade gom nhiều bước xử lý phức tạp của subsystem thành 1 giao diện đơn giản cho Controller gọi. Trong khi Mediator là trạm điều phối trung tâm gửi lệnh (Command) giữa Controller và các Handlers để các lớp không gọi trực tiếp lẫn nhau."*

---

> [!NOTE]
> File [kichban.md](file:///e:/Document/GianDuyKhanh-23DH111541/movieCinema/movieCinema/kichban.md) đã được cập nhật thành công toàn bộ nội dung mới nhất trên! 🚀
