# Kịch bản demo Design Pattern trong dự án MovieCinema

> **Mục đích:** Dùng tài liệu này để thuyết trình/demo trực tiếp web đặt vé xem phim và giải thích design pattern đang được áp dụng trong source code.
>
> **Thời lượng đề xuất:** 10–15 phút.
>
> **Luồng demo chính:** Xem danh sách phim → xem chi tiết → chọn suất chiếu/ghế → đặt vé → quản trị xác nhận/hủy/hoàn tiền.

---

## 1. Mở đầu bài demo

### Lời dẫn

“MovieCinema là web đặt vé xem phim xây dựng bằng ASP.NET Core MVC, Entity Framework Core và SQL Server. Điểm chính của phần demo không chỉ là giao diện đặt vé, mà là cách code được tổ chức để mỗi nghiệp vụ có thể thay đổi và mở rộng mà không làm Controller trở nên quá phức tạp.

Trong một luồng đặt vé, hệ thống cần xử lý nhiều việc: lấy suất chiếu, kiểm tra ghế, tính giá theo loại ghế, kiểm tra voucher/điểm thành viên, chọn phương thức thanh toán, tạo Order, lưu database và phản hồi trạng thái. Các design pattern giúp chia các trách nhiệm này thành những thành phần độc lập.”

### Kiến trúc tổng quát

```text
Browser
   |
   v
Controllers (MoviesController, OrdersController, ...)
   |  Dependency Injection
   v
Services / Facade / Mediator / Pipeline
   |
   v
AppDbContext (Entity Framework Core)
   |
   v
SQL Server
```

Các thành phần được đăng ký trong `Program.cs` bằng Dependency Injection. Ví dụ:

```csharp
builder.Services.AddScoped<IOrdersService, OrdersService>();
builder.Services.AddScoped<IShowtimesService, ShowtimesService>();
builder.Services.AddScoped<ISeatsService, SeatsService>();
builder.Services.AddScoped<IBookingFacade, BookingFacade>();
builder.Services.AddScoped<IOrderBuilder, OrderBuilder>();
builder.Services.AddScoped<IMediator, AppMediator>();
```

**Giải thích:** Controller phụ thuộc vào interface thay vì tự tạo class bằng `new`. Nhờ vậy, implementation có thể thay đổi, dễ mock khi test và các lớp ít phụ thuộc cứng vào nhau.

---

## 2. Chuẩn bị trước khi demo

1. Mở solution `MovieCinema.csproj` trong Visual Studio/IDE.
2. Kiểm tra connection string trong `appsettings.json`.
3. Đảm bảo SQL Server đang chạy và database đã có seed data.
4. Chạy project bằng Visual Studio hoặc:

```bash
dotnet run
```

5. Mở URL được hiển thị trên terminal, thường là `https://localhost:<port>`.
6. Chuẩn bị hai tài khoản:
   - Tài khoản khách hàng để đặt vé.
   - Tài khoản `Admin` để xác nhận, hủy, hoàn tiền và xem báo cáo.

> Nếu database chưa có dữ liệu, chạy migration/seed theo cấu hình của project trước khi trình bày. Không nên chạy thao tác xóa toàn bộ order trong buổi demo thật.

---

# 3. Demo theo luồng người dùng

## Bước 1 — Xem danh sách phim: MVC + Service Layer + Proxy

### Thao tác trên web

1. Mở trang chủ.
2. Chỉ ra danh sách phim đang hiển thị.
3. Có thể refresh trang hai lần để giải thích cơ chế cache.

### Code cần mở

- `Controllers/MoviesController.cs:10`
- `Controllers/MoviesController.cs:21`
- `Data/Proxy/CachedMoviesServiceProxy.cs:11`
- `Program.cs:38-66`

Trong Controller:

```csharp
private readonly IMoviesService _service;

public MoviesController(IMoviesService service, ...)
{
    _service = service;
}

public async Task<IActionResult> Index()
{
    var allMovies = await _service.GetAllAsync(
        n => n.Cinema,
        n => n.CinemaRoom,
        n => n.Category,
        n => n.MovieReviews);

    return View(allMovies);
}
```

Trong `Program.cs`, `IMoviesService` được cung cấp bởi proxy:

```csharp
builder.Services.AddScoped<MoviesService>();

builder.Services.AddScoped<IMoviesService>(sp =>
{
    var realService = sp.GetRequiredService<MoviesService>();
    var cache = sp.GetRequiredService<IMemoryCache>();
    return new CachedMoviesServiceProxy(realService, cache);
});
```

### Pattern: Proxy

`CachedMoviesServiceProxy` đóng vai trò đại diện cho `MoviesService` thật. Client là `MoviesController` chỉ biết `IMoviesService`, không cần biết có cache ở phía sau.

```csharp
public async Task<IEnumerable<Movie>> GetAllAsync()
{
    return await _cache.GetOrCreateAsync("movies:all", async entry =>
    {
        entry.SlidingExpiration = DefaultExpiry;
        return await _realService.GetAllAsync();
    }) ?? Enumerable.Empty<Movie>();
}
```

**Cách giải thích:**

- Lần đầu gọi `Index`, proxy lấy dữ liệu từ service thật và lưu vào `IMemoryCache`.
- Các lần đọc tiếp theo có thể lấy từ cache, giảm truy vấn database.
- Các thao tác `AddAsync`, `UpdateAsync`, `DeleteAsync` vẫn chuyển tiếp tới service thật.
- Controller không bị thay đổi nếu sau này thay `IMemoryCache` bằng Redis.

**Lợi ích:** Kiểm soát truy cập và tối ưu dữ liệu đọc nhiều mà không sửa `MoviesController`.

> Lưu ý khi thuyết trình: các method `GetAllAsync` có `includeProperties` trong proxy hiện chuyển thẳng tới service thật; cache hiện tập trung ở các method đọc danh sách/chi tiết không có include.

---

## Bước 2 — Xem chi tiết phim: MVC và Dependency Injection

### Thao tác trên web

1. Bấm vào một phim.
2. Chỉ ra phần mô tả, thể loại, rạp và các suất chiếu.
3. Bấm “Đặt vé” ở một suất chiếu.

### Code cần mở

`Controllers/MoviesController.cs:43-52`:

```csharp
public async Task<IActionResult> Details(int id)
{
    var movieDetails = await _service.GetMovieByIdAsync(id);
    if (movieDetails == null) return View("NotFound");

    var showtimes = await _showtimesService
        .GetShowtimesByMovieIdAsync(id);
    ViewBag.Showtimes = showtimes;

    return View(movieDetails);
}
```

**Pattern nền tảng:** MVC kết hợp Service Layer. Controller nhận request, gọi service, đưa model sang View; truy vấn và nghiệp vụ dữ liệu không nằm trực tiếp trong Razor View.

---

## Bước 3 — Chọn ghế: Bridge Pattern

### Thao tác trên web

1. Chọn rạp và phòng chiếu.
2. Chọn một ghế Standard, một ghế VIP hoặc Couple.
3. Quan sát giá ghế thay đổi theo loại.
4. Có thể mở DevTools/Network và gọi endpoint `Orders/GetSeatsForShowtime` để thấy JSON có `type`, `price`, `isAvailable`.

### Code cần mở

- `Controllers/OrdersController.cs:260-293`
- `Models/Bridge/SeatPricingBridge.cs:3-53`
- `Models/Seat.cs:7-33`

Trong Controller:

```csharp
price = new SeatPricingBridge(s.SeatType)
    .GetPrice(showtime.Price),
isAvailable = s.IsAvailable
    && !bookedSeats.Contains(s.Row + s.Number.ToString())
```

Trong Bridge:

```csharp
public class SeatPricingBridge
{
    private readonly ISeatingPricingStrategy _strategy;

    public SeatPricingBridge(SeatType seatType)
    {
        _strategy = seatType switch
        {
            SeatType.VIP => new VipPricingStrategy(),
            SeatType.Couple => new CouplePricingStrategy(),
            SeatType.Disabled => new DisabledPricingStrategy(),
            _ => new StandardPricingStrategy()
        };
    }

    public double GetPrice(double basePrice)
        => _strategy.CalculatePrice(basePrice);
}
```

### Pattern: Bridge

Bridge tách:

- **Abstraction:** `SeatPricingBridge` — cách hệ thống yêu cầu tính giá.
- **Implementation:** `ISeatingPricingStrategy` và các lớp `StandardPricingStrategy`, `VipPricingStrategy`, `CouplePricingStrategy`, `DisabledPricingStrategy` — quy tắc giá cụ thể.

Ví dụ giá cơ bản là 100.000 VND:

| Loại ghế | Implementation | Giá minh họa |
|---|---|---:|
| Standard | `basePrice` | 100.000 |
| VIP | `basePrice * 1.2` | 120.000 |
| Couple | `basePrice * 2.0` | 200.000 |
| Disabled | `basePrice * 0.5` | 50.000 |

**Cách giải thích:** Nếu thêm loại ghế mới, ta thêm pricing strategy tương ứng thay vì rải thêm nhiều `if/else` trong Controller. Phần hiển thị/chọn ghế và phần tính giá có thể thay đổi độc lập.

---

## Bước 4 — Submit đặt vé: Facade Pattern

### Thao tác trên web

1. Chọn ghế còn trống.
2. Nhập họ tên, email.
3. Nhập voucher nếu có.
4. Chọn `Cash` hoặc `PayPal`.
5. Nhấn đặt vé.

### Code cần mở

- `Controllers/OrdersController.cs:296-335`
- `Data/Facade/BookingFacade.cs:34-145`

Action trong Controller rất ngắn:

```csharp
[HttpPost]
public async Task<IActionResult> BookTickets(BookTicketsVM model)
{
    if (!ModelState.IsValid)
    {
        TempData["BookingError"] =
            "Vui lòng nhập đầy đủ thông tin đặt vé hợp lệ.";
        return RedirectToAction(nameof(BookTickets),
            new { showtimeId = model.ShowtimeId });
    }

    var result = await _bookingFacade.ProcessBookingAsync(
        model, User.Identity?.Name);

    if (!result.Success)
    {
        TempData["BookingError"] = result.Message;
        return RedirectToAction(nameof(BookTickets),
            new { showtimeId = model.ShowtimeId });
    }

    return View("BookingCompleted");
}
```

### Pattern: Facade

`BookingFacade` cung cấp một cổng đơn giản cho toàn bộ subsystem đặt vé. Bên trong Facade thực hiện:

1. Kiểm tra đã chọn ghế.
2. Lấy `Showtime`.
3. Parse danh sách ghế.
4. Kiểm tra ghế đã bị đặt.
5. Tính giá bằng Bridge.
6. Tính giảm giá voucher.
7. Gọi thanh toán.
8. Tạo Order bằng Builder.
9. Lưu order qua `IOrdersService`.

Ví dụ đoạn gọi từ Controller:

```text
OrdersController
      |
      +--> IBookingFacade.ProcessBookingAsync(...)
                |
                +--> IShowtimesService
                +--> ISeatsService
                +--> IOrdersService
                +--> PaymentContext
                +--> OrderBuilder
                +--> AppDbContext
```

**Lợi ích:** Controller chỉ điều phối request/response. Nếu thay đổi quy trình đặt vé, phần lớn thay đổi nằm trong `BookingFacade`, không làm Controller phình to.

---

## Bước 5 — Kiểm tra dữ liệu đặt vé: Chain of Responsibility

Luồng đặt vé hiện còn được kiểm tra qua pipeline trong Mediator handler. Mở:

- `Data/Chain/OrderPipeline.cs:23-237`
- `Data/Mediator/BookingMediator.cs:102-155`

Pipeline được ghép như sau:

```csharp
var validation = new ValidationHandler();
var seats = new SeatAvailabilityHandler(ordersService);
var voucher = new VoucherValidationHandler(ordersService);
var member = new MemberValidationHandler(ordersService);

validation.SetNext(seats).SetNext(voucher).SetNext(member);
```

### Pattern: Chain of Responsibility

Mỗi handler xử lý một bước và quyết định:

- Nếu dữ liệu sai: dừng chain và trả thông báo.
- Nếu hợp lệ: chuyển sang `_next`.

Chuỗi thực tế:

```text
ValidationHandler
       |
       v
SeatAvailabilityHandler
       |
       v
VoucherValidationHandler
       |
       v
MemberValidationHandler
```

Ví dụ `SeatAvailabilityHandler`:

```csharp
foreach (var seat in selectedSeats)
{
    if (bookedSeats.Contains(seat))
    {
        result.IsValid = false;
        result.Message = $"Ghế {seat} đã được đặt bởi người khác.";
        return result;
    }
}

return _next != null
    ? await _next.HandleAsync(request, result)
    : result;
```

**Demo lỗi:** Chọn một ghế đã đặt hoặc nhập hơn 10 ghế. Pipeline dừng đúng tại handler tương ứng, không chạy các bước sau.

**Lợi ích:** Thêm bước kiểm tra mới — ví dụ kiểm tra giới hạn thành viên hoặc chống đặt trùng — bằng cách thêm handler, không sửa tất cả logic hiện có.

---

## Bước 6 — Chọn phương thức thanh toán: Strategy Pattern

### Code cần mở

`Data/Strategy/PaymentStrategy.cs:3-120` và `Data/Facade/BookingFacade.cs:99-105`.

```csharp
var paymentCtx = new PaymentContext();
paymentCtx.SetStrategyByName(model.PaymentMethod);
var paymentResult = await paymentCtx.PayAsync(
    totalPrice, $"ORDER-{DateTime.Now.Ticks}");
```

Các strategy:

```csharp
public interface IPaymentStrategy
{
    string Name { get; }
    string PaymentMethod { get; }
    Task<PaymentResult> PayAsync(double amount, string orderId);
    Task<RefundResult> RefundAsync(string transactionId, double amount);
}
```

- `CashPaymentStrategy`: sinh mã giao dịch Cash, thông báo thanh toán tại rạp.
- `PayPalPaymentStrategy`: mô phỏng API PayPal bằng `Task.Delay` trong bản demo.
- `PaymentContext`: giữ strategy hiện tại và ủy quyền `PayAsync`/`RefundAsync`.

### Pattern: Strategy

Strategy đóng gói các thuật toán thanh toán có cùng interface. Client có thể thay đổi thuật toán lúc runtime:

```csharp
paymentCtx.SetStrategyByName("cash");
// hoặc
paymentCtx.SetStrategyByName("paypal");
```

**Cách giải thích:** Nếu thêm MoMo, VNPay hoặc thẻ ngân hàng, chỉ cần thêm class implement `IPaymentStrategy` và đăng ký/chọn nó. Code tạo order không cần biết chi tiết API của từng nhà cung cấp.

> Trong project hiện tại PayPal là stub để minh họa; khi production cần thay phần mô phỏng bằng SDK/API thật và đưa secret vào configuration/secret store, không hard-code.

---

## Bước 7 — Tạo Order: Builder Pattern

### Code cần mở

- `Models/Builders/OrderBuilder.cs:5-77`
- `Data/Facade/BookingFacade.cs:107-115`

```csharp
var order = new OrderBuilder()
    .SetCustomer(model.Name ?? "Guest", model.Email ?? "", userId ?? "")
    .SetShowtime(model.ShowtimeId, model.SelectedSeats,
                 selectedSeats.Count, showtime.Price)
    .ApplyVoucher(discount, totalPrice)
    .RedeemPoints(model.PointsRedeemed, totalPrice - discount)
    .SetPaymentMethod(paymentCtx.CurrentPaymentMethod)
    .CalculateTotal()
    .Build();
```

### Pattern: Builder

`Order` có nhiều dữ liệu: khách hàng, suất chiếu, ghế, voucher, điểm, phương thức thanh toán, tổng tiền. Builder chia việc tạo thành các bước có tên rõ ràng.

`CalculateTotal()` tập trung công thức:

```csharp
_finalTotal = _subtotal
    - _order.DiscountAmount
    - _order.PointsRedeemed;

if (_finalTotal < 0) _finalTotal = 0;
_order.TotalPrice = _finalTotal;
```

**Lợi ích:**

- Code gọi dễ đọc như một quy trình nghiệp vụ.
- Không phải truyền một constructor có quá nhiều tham số.
- Logic tính tổng tập trung một chỗ.
- Thêm bước mới ít ảnh hưởng tới nơi khác.

> `OrderBuilder` có interface `IOrderBuilder`, được đăng ký DI trong `Program.cs`, vì vậy có thể thay implementation hoặc mock khi test.

---

## Bước 8 — Giá cuối cùng: Decorator Pattern

### Code cần mở

`Data/Decorators/PricingDecorators.cs:5-217`.

Các decorator hiện có:

- `BasePriceCalculator`: giá gốc.
- `VoucherDecorator`: giảm theo voucher.
- `LoyaltyPointsDecorator`: trừ điểm tích lũy.
- `HappyHourDecorator`: giảm 15% từ 14:00–17:00.

Cách xếp lớp:

```csharp
IOrderPriceDecorator calc = new BasePriceCalculator(basePrice);

if (voucher != null)
    calc = new VoucherDecorator(calc, voucher);

if (loyaltyPoints > 0)
    calc = new LoyaltyPointsDecorator(calc, loyaltyPoints);

if (applyHappyHour)
    calc = new HappyHourDecorator(calc,
        new TimeSpan(14, 0, 0),
        new TimeSpan(17, 0, 0),
        15.0);
```

### Pattern: Decorator

Mỗi decorator bọc một calculator khác và thêm một trách nhiệm tính giá. Không cần tạo hàng loạt class như `PriceWithVoucherAndPointsAndHappyHour`.

```text
BasePrice
   -> VoucherDecorator
      -> LoyaltyPointsDecorator
         -> HappyHourDecorator
```

**Lợi ích:** Các chính sách giảm giá có thể xếp chồng linh hoạt. Có thể thêm `MemberDiscountDecorator` hoặc `FestivalDiscountDecorator` mà không sửa calculator gốc.

> Trong `BookingFacade`, voucher và điểm hiện cũng được chuyển vào Builder để lưu Order. File `PricingDecorators.cs` cung cấp cơ chế tính giá/breakdown mở rộng và được đăng ký sử dụng qua `OrderPriceCalculator` trong kiến trúc pattern của project.

---

## Bước 9 — Quản trị xác nhận đơn: State + Mediator + Observer

### Thao tác trên web

1. Đăng nhập tài khoản Admin.
2. Mở màn hình quản lý booking.
3. Bấm **Confirm** cho order vừa tạo.
4. Quan sát status chuyển từ `Purchased` sang `Confirmed`.
5. Nếu có thời gian, thử Cancel hoặc Refund để minh họa trạng thái khác.

### 9.1 Mediator Pattern

Mở:

- `Data/Mediator/BookingMediator.cs:10-97`
- `Program.cs:75-82`

`IMediator` nhận request và tìm handler tương ứng:

```csharp
public interface IMediator
{
    Task<TResponse> SendAsync<TResponse>(IRequest<TResponse> request);
}
```

Các request hiện có:

- `CompleteBookingRequest`
- `CancelBookingRequest`
- `ConfirmBookingRequest`

`AppMediator` dùng request type để tìm `IRequestHandler<,>` từ DI và gọi `HandleAsync`.

**Ý nghĩa:** Controller không cần biết trực tiếp toàn bộ service nào phải gọi khi Confirm/Cancel. Nó gửi một request đến Mediator; handler chịu trách nhiệm quy trình tương ứng.

### 9.2 State Pattern

Mở `Data/State/OrderStateMachine.cs:7-115`.

Các trạng thái:

```text
Purchased  --> Confirmed
    |             |
    v             v
Cancelled      Refunded
```

Mỗi state quyết định được chuyển sang trạng thái nào:

```csharp
public class PurchasedState : IOrderState
{
    public string StatusName => "Purchased";

    public bool CanTransitionTo(string newStatus)
        => newStatus is "Confirmed" or "Cancelled";
}

public class ConfirmedState : IOrderState
{
    public string StatusName => "Confirmed";

    public bool CanTransitionTo(string newStatus)
        => newStatus is "Cancelled" or "Refunded";
}
```

Khi service gọi `ChangeOrderStatusWithStateAsync`, hệ thống kiểm tra transition hợp lệ trước khi cập nhật DB. Ví dụ order đã `Cancelled` không thể quay lại `Confirmed`.

**Lợi ích:** Quy tắc chuyển trạng thái được tập trung, rõ ràng và tránh một `switch` lớn rải trong Controller.

### 9.3 Observer Pattern

Mở:

- `Data/Observer/OrderObserver.cs:10-85`
- `Program.cs:68-72`

Subject:

```csharp
public interface IOrderSubject
{
    void Attach(IOrderObserver observer);
    void Detach(IOrderObserver observer);
    Task NotifyAsync(Order order,
        string oldStatus, string newStatus);
}
```

Các observer:

1. `AuditLogObserver`: ghi log thay đổi trạng thái.
2. `LoyaltyPointsObserver`: cộng/trừ điểm thành viên.
3. `EmailNotificationObserver`: ghi/gửi thông báo email.

Đăng ký DI:

```csharp
builder.Services.AddSingleton<IOrderSubject, OrderSubject>();
builder.Services.AddScoped<IOrderObserver, AuditLogObserver>();
builder.Services.AddScoped<IOrderObserver, LoyaltyPointsObserver>();
builder.Services.AddScoped<IOrderObserver, EmailNotificationObserver>();
```

### Lời giải thích khi demo

“Admin chỉ thực hiện một hành động là Confirm. Sau khi status thay đổi, Subject thông báo cho nhiều observer. Vì vậy muốn thêm SMS, lịch sử hoạt động hoặc cập nhật dashboard thì chỉ cần thêm một observer mới, không phải sửa logic Confirm chính.”

Observer xử lý độc lập; nếu một observer lỗi, `OrderSubject` bắt lỗi để các observer còn lại vẫn được thông báo.

---

# 4. Demo báo cáo: Service/Repository và hướng mở rộng Visitor

### Thao tác trên web

1. Vào `Admin → Revenue` hoặc `Admin → Reports`.
2. Chọn khoảng ngày.
3. Chỉ ra tổng doanh thu, số vé, doanh thu theo phim/rạp.

`OrdersController` lấy order qua `IOrdersService`, lọc các order `Purchased`/`Confirmed`, sau đó group theo ngày, tháng, phim và rạp.

### Pattern nền tảng: Repository/Service

Các service dùng `IEntityBaseRepository<T>` để chuẩn hóa thao tác CRUD. Controller không truy cập trực tiếp từng câu SQL cho nghiệp vụ thông thường.

### Visitor — hướng áp dụng cho phần báo cáo

Project có tài liệu và cấu trúc minh họa Visitor trong `DESIGN_PATTERNS_GUIDE.md`. Ý tưởng là tách thuật toán phân tích khỏi model `Order`/`OrderItem`:

- `NetRevenueVisitor`: doanh thu thực sau giảm giá.
- `GrossRevenueVisitor`: doanh thu gộp.
- `TicketCountVisitor`: tổng số vé.

Khi cần thêm cách phân tích mới, đổi Visitor thay vì sửa model. Khi thuyết trình nên nói rõ: phần báo cáo đang chạy thực tế chủ yếu dùng LINQ trong `OrdersController`; Visitor là hướng refactor/mở rộng được tài liệu hóa cho module này, không nên khẳng định toàn bộ report hiện tại đã dùng Visitor.

---

# 5. Bảng tổng kết các pattern trong project

| Pattern | Vị trí chính | Bài toán giải quyết | Câu nói ngắn khi demo |
|---|---|---|---|
| MVC | `Controllers/`, `Views/`, `Models/` | Tách request, giao diện và dữ liệu | Controller nhận request, View hiển thị, Model biểu diễn dữ liệu |
| Dependency Injection | `Program.cs` | Giảm phụ thuộc cứng | Controller nhận interface qua constructor |
| Service/Repository | `Data/Services/`, `Data/Base/` | Chuẩn hóa truy cập dữ liệu/nghiệp vụ | Không nhồi truy vấn vào View |
| Proxy | `Data/Proxy/CachedMoviesServiceProxy.cs` | Cache phim | Cùng interface nhưng thêm cache ở phía trước service thật |
| Bridge | `Models/Bridge/SeatPricingBridge.cs` | Tính giá theo loại ghế | Tách loại ghế khỏi thuật toán giá |
| Facade | `Data/Facade/BookingFacade.cs` | Đơn giản hóa đặt vé | Controller gọi một cổng thay vì nhiều service |
| Chain of Responsibility | `Data/Chain/OrderPipeline.cs` | Kiểm tra nhiều bước | Sai ở đâu thì dừng ở handler đó |
| Strategy | `Data/Strategy/PaymentStrategy.cs` | Nhiều phương thức thanh toán | Cash/PayPal thay thế được tại runtime |
| Builder | `Models/Builders/OrderBuilder.cs` | Tạo Order nhiều dữ liệu | Ghép Order bằng fluent chain dễ đọc |
| Decorator | `Data/Decorators/PricingDecorators.cs` | Xếp chồng giảm giá | Voucher, điểm, Happy Hour bọc nhau |
| State | `Data/State/OrderStateMachine.cs` | Kiểm soát vòng đời Order | Không cho chuyển trạng thái bất hợp lệ |
| Mediator | `Data/Mediator/BookingMediator.cs` | Giảm coupling giữa Controller và handler | Gửi request, handler xử lý |
| Observer | `Data/Observer/OrderObserver.cs` | Phản ứng khi status đổi | Log, điểm, email nhận cùng một event |
| Singleton theo DI | `OrderSubject` đăng ký `AddSingleton` | Một Subject dùng chung | Subject quản lý danh sách observer |

---

# 6. Kết luận bài thuyết trình

### Lời kết mẫu

“Qua luồng demo, có thể thấy các pattern không được dùng chỉ để làm code phức tạp hơn. Mỗi pattern xuất hiện để giải quyết một vấn đề cụ thể:

- Proxy giảm truy vấn khi đọc phim.
- Bridge giữ cho quy tắc giá ghế độc lập.
- Facade làm luồng đặt vé đơn giản với Controller.
- Chain kiểm tra tuần tự và có thể mở rộng.
- Strategy cho phép đổi phương thức thanh toán.
- Builder tạo Order nhiều bước dễ đọc.
- Decorator xếp chồng chính sách giảm giá.
- State bảo vệ vòng đời đơn hàng.
- Mediator giảm liên kết giữa các thành phần.
- Observer giúp thêm email, log, điểm thành viên mà không sửa nghiệp vụ chính.

Kết quả là code dễ đọc hơn, dễ test hơn và thuận lợi mở rộng khi hệ thống thêm phương thức thanh toán, loại ghế, chương trình khuyến mãi hoặc kênh thông báo mới.”

---

# 7. Câu hỏi thường gặp khi bảo vệ/demo

### 1. Vì sao không viết tất cả vào `OrdersController`?

Vì Controller chỉ nên điều phối HTTP request/response. Nếu vừa kiểm tra ghế, tính tiền, thanh toán, lưu DB và gửi thông báo trong một action, code sẽ khó đọc, khó test và khó thay đổi.

### 2. Facade và Mediator khác nhau thế nào?

- **Facade** gom các thao tác của một subsystem thành một API nghiệp vụ — ở đây là toàn bộ quy trình booking.
- **Mediator** điều phối các request/handler, giúp Controller không tham chiếu trực tiếp nhiều thành phần xử lý.

Chúng có thể cùng xuất hiện: Mediator handler nhận request, sau đó gọi Facade để xử lý booking.

### 3. Strategy và Bridge có giống nhau không?

Cả hai đều dùng abstraction/interface để tách thuật toán, nhưng mục đích khác nhau:

- **Strategy:** thay thế một thuật toán tại runtime, ví dụ Cash/PayPal.
- **Bridge:** tách hai chiều biến đổi độc lập, ví dụ abstraction tính giá và implementation theo loại ghế.

### 4. Nếu thêm MoMo thì sửa ở đâu?

Tạo `MoMoPaymentStrategy : IPaymentStrategy`, triển khai `PayAsync`/`RefundAsync`, rồi bổ sung cách chọn strategy và đăng ký cấu hình. Phần Controller/Builder không cần biết chi tiết API MoMo.

### 5. Nếu ghế đã bị người khác đặt ngay sau khi kiểm tra thì sao?

Pipeline kiểm tra giúp phát hiện ở bước nghiệp vụ, nhưng production vẫn cần transaction/unique constraint hoặc cơ chế giữ ghế có thời hạn để xử lý race condition giữa bước kiểm tra và bước lưu Order.

### 6. PayPal trong demo đã gọi API thật chưa?

Chưa. `PayPalPaymentStrategy` hiện mô phỏng API bằng `Task.Delay`; đây là điểm cần thay bằng SDK/API thật khi triển khai production, đồng thời bảo vệ secret trong configuration/secret manager.

### 7. Pattern nào nên ưu tiên nếu refactor tiếp?

Ưu tiên giữ Controller mỏng, đưa báo cáo ra service riêng, thêm transaction cho booking, hoàn thiện cache invalidation trong `CachedMoviesServiceProxy`, và đưa các chính sách thanh toán/giảm giá vào DI thay vì khởi tạo trực tiếp trong Facade.
