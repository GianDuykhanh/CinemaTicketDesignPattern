# Kịch bản Demo Design Pattern — Web đặt vé MovieCinema

> **Mục đích:** Hướng dẫn demo từng chức năng trên web, chỉ rõ thao tác, mở file code nào, pattern nào được áp dụng, và giải thích code chi tiết để thuyết trình.
>
> **Thời lượng đề xuất:** 10–15 phút.
>
> **Luồng demo:** Xem phim → Chi tiết phim → Chọn ghế → Đặt vé → Thanh toán → Tạo Order → Quản trị xác nhận/hủy.

---

## Tổng quan kiến trúc

```text
Browser
   │
   v
Controllers (MoviesController, OrdersController)
   │  Dependency Injection (interface, không new trực tiếp)
   v
Services / Facade / Mediator / Pipeline
   │
   v
AppDbContext (Entity Framework Core)
   │
   v
SQL Server
```

Các thành phần được đăng ký trong `Program.cs` bằng Dependency Injection:

```csharp
// Program.cs
builder.Services.AddScoped<IOrdersService, OrdersService>();
builder.Services.AddScoped<IShowtimesService, ShowtimesService>();
builder.Services.AddScoped<ISeatsService, SeatsService>();
builder.Services.AddScoped<IBookingFacade, BookingFacade>();
builder.Services.AddScoped<IOrderBuilder, OrderBuilder>();
builder.Services.AddScoped<IMediator, AppMediator>();
```

**Pattern nền tảng — Dependency Injection (DI):** Controller nhận interface qua constructor, không tự `new` class. Nhờ vậy implementation thay đổi được, dễ mock khi test, và các lớp ít phụ thuộc cứng vào nhau.

---

## Chức năng 1 — Xem danh sách phim: Proxy Pattern

### 🎙️ Lời dẫn demo

> "Đầu tiên, mình sẽ mở trang chủ MovieCinema — đây là trang mà mọi user đều thấy đầu tiên khi vào web. Một điểm đáng chú ý: khi F5 refresh trang nhiều lần, thời gian load rất nhanh vì dữ liệu được cache. Đây chính là lúc Proxy Pattern phát huy tác dụng —Controller không biết rằng phía trước service thật có một lớp proxy đang giữ data trong bộ nhớ đệm."

### Thao tác trên web

1. Mở trang chủ → thấy danh sách phim đang chiếu và sắp chiếu.
2. **F5 refresh trang 2–3 lần** → quan sát thời gian load nhanh hơn (cache).
3. Mở DevTools → Network tab → xem thời gian response giảm ở lần load thứ 2.

### File cần mở

| File | Dòng |
|---|---|
| `Controllers/MoviesController.cs` | 10–25 |
| `Data/Proxy/CachedMoviesServiceProxy.cs` | 1–131 |
| `Program.cs` | 60–66 |

### Pattern: Proxy

**Cụ thể — Controller chỉ biết `IMoviesService`, không biết có cache:**

```csharp
// Controllers/MoviesController.cs:10-25
public class MoviesController : Controller
{
    private readonly IMoviesService _service;

    public MoviesController(IMoviesService service, ...)
    {
        _service = service;   // nhận interface, KHÔNG biết là proxy hay service thật
    }

    public async Task<IActionResult> Index()
    {
        var allMovies = await _service.GetAllAsync(
            n => n.Cinema, n => n.CinemaRoom,
            n => n.Category, n => n.MovieReviews);
        return View(allMovies);
    }
}
```

**Trong Program.cs, `IMoviesService` được gán cho Proxy thay vì service thật:**

```csharp
// Program.cs:60-66
builder.Services.AddScoped<MoviesService>();  // đăng ký service thật TRƯỚC

builder.Services.AddScoped<IMoviesService>(sp =>
{
    var realService = sp.GetRequiredService<MoviesService>();
    var cache = sp.GetRequiredService<IMemoryCache>();
    return new CachedMoviesServiceProxy(realService, cache);
    // Controller nhận proxy, nhưng chỉ biết qua interface IMoviesService
});
```

**Proxy kiểm tra cache trước, hết hạn mới gọi service thật:**

```csharp
// Data/Proxy/CachedMoviesServiceProxy.cs:36-43
public async Task<IEnumerable<Movie>> GetAllAsync()
{
    return await _cache.GetOrCreateAsync("movies:all", async entry =>
    {
        entry.SlidingExpiration = DefaultExpiry; // 10 phút
        return await _realService.GetAllAsync();
    }) ?? Enumerable.Empty<Movie>();
}
```

Khi Admin thêm/sửa/xóa phim, proxy tự invalidate cache:

```csharp
// Data/Proxy/CachedMoviesServiceProxy.cs:24-34
public async Task AddAsync(Movie entity)
{
    await _realService.AddAsync(entity);
    InvalidateAllCaches();  // xóa cache để lần đọc sau lấy data mới
}
```

### Câu nói khi demo

> "Khi mở trang chủ, MoviesController gọi `_service.GetAllAsync()`. Nhưng `_service` thực ra là `CachedMoviesServiceProxy`, không phải `MoviesService` thật. Proxy kiểm tra `IMemoryCache` trước — nếu chưa có dữ liệu hoặc đã hết hạn 10 phút, mới truy vấn database. Nhờ vậy lần load thứ 2 trên cùng trình duyệt sẽ rất nhanh. Controller không cần biết proxy tồn tại — đó là ưu điểm của pattern này."

---

## Chức năng 2 — Chọn ghế và tính giá: Bridge Pattern

### 🎙️ Lời dẫn demo

> "Sau khi xem danh sách phim, mình sẽ vào chi tiết một phim và bấm 'Đặt vé' để đến màn hình chọn ghế. Tại đây, mỗi loại ghế — Standard, VIP, Couple — đều có giá khác nhau. Hệ thống tự động tính giá theo loại ghế mà không cần hardcode trong Controller. Đây là lúc Bridge Pattern hoạt động: tách biệt phần 'biết loại ghế' khỏi phần 'tính giá', mỗi bên phát triển độc lập."

### Thao tác trên web

1. Từ trang chủ → bấm vào một phim → vào trang chi tiết.
2. Bấm **"Đặt vé"** → vào màn hình chọn ghế.
3. Chọn rạp và suất chiếu.
4. **Chọn một ghế Standard, một VIP, một Couple** → quan sát giá mỗi ghế thay đổi theo loại.
5. Mở DevTools → Network → gọi `Orders/GetSeatsForShowtime` → xem JSON trả về có `type`, `price`, `isAvailable`.

### File cần mở

| File | Dòng |
|---|---|
| `Controllers/OrdersController.cs` | 275–309 |
| `Models/Seat.cs` | 7–34 |
| `Models/SeatType.cs` | 1–9 |
| `Models/Bridge/SeatPricingBridge.cs` | 1–55 |

### Pattern: Bridge

**Controller gọi `SeatPricingBridge` để tính giá mỗi ghế theo loại:**

```csharp
// Controllers/OrdersController.cs:286-302
var groupedSeats = seats
    .GroupBy(s => s.Row)
    .OrderBy(g => g.Key)
    .Select(g => new
    {
        row = g.Key,
        seats = g.OrderBy(s => s.Number).Select(s => new
        {
            id = s.Id,
            number = s.Number,
            row = s.Row,
            type = s.SeatType.ToString(),
            // ── Đây là Bridge: truyền SeatType vào → Bridge tự chọn strategy ──
            price = new SeatPricingBridge(s.SeatType).GetPrice(showtime.Price),
            isAvailable = s.IsAvailable
                && !bookedSeats.Contains(s.Row + s.Number.ToString())
        })
    })
    .ToList();
```

**Bridge tách hai chiều biến đổi — Abstraction (`SeatPricingBridge`) và Implementation (`ISeatingPricingStrategy`):**

```csharp
// Models/Bridge/SeatPricingBridge.cs:33-54
public class SeatPricingBridge
{
    private readonly ISeatingPricingStrategy _strategy;

    public SeatPricingBridge(SeatType seatType)
    {
        _strategy = seatType switch
        {
            SeatType.VIP     => new VipPricingStrategy(),
            SeatType.Couple  => new CouplePricingStrategy(),
            SeatType.Disabled => new DisabledPricingStrategy(),
            _                => new StandardPricingStrategy()
        };
    }

    public double GetPrice(double basePrice)
        => _strategy.CalculatePrice(basePrice);
}
```

**Mỗi Implementation tính giá khác nhau:**

```csharp
// Models/Bridge/SeatPricingBridge.cs:9-31
public class StandardPricingStrategy : ISeatingPricingStrategy
{
    public double CalculatePrice(double basePrice) => basePrice;         // ×1.0
}
public class VipPricingStrategy : ISeatingPricingStrategy
{
    public double CalculatePrice(double basePrice) => basePrice * 1.2;   // ×1.2
}
public class CouplePricingStrategy : ISeatingPricingStrategy
{
    public double CalculatePrice(double basePrice) => basePrice * 2.0;   // ×2.0
}
public class DisabledPricingStrategy : ISeatingPricingStrategy
{
    public double CalculatePrice(double basePrice) => basePrice * 0.5;   // ×0.5
}
```

**Ví dụ thực tế** — nếu giá cơ bản 100.000đ:

| Loại ghế | Multiplier | Giá hiển thị |
|---|---|---:|
| Standard | ×1.0 | 100.000đ |
| VIP | ×1.2 | 120.000đ |
| Couple | ×2.0 | 200.000đ |
| Disabled | ×0.5 | 50.000đ |

### Câu nói khi demo

> "Khi bạn chọn ghế, hệ thống gọi `new SeatPricingBridge(seat.SeatType).GetPrice(showtime.Price)`. Bridge tự chọn strategy tính giá dựa trên loại ghế. Nếu thêm loại ghế mới比如 Premium, chỉ cần thêm class `PremiumPricingStrategy` và thêm 1 dòng trong switch. Controller không bị ảnh hưởng — đó là ưu điểm của Bridge: tách phần hiển thị ghế khỏi phần tính giá, mỗi bên thay đổi độc lập."

---

## Chức năng 3 — Đặt vé: Facade + Builder + Strategy + Decorator (4 pattern cùng lúc)

### 🎙️ Lời dẫn demo

> "Tiếp theo là luồng nghiệp vụ quan trọng nhất: đặt vé. Mình sẽ chọn ghế, nhập thông tin khách hàng, áp dụng voucher hoặc điểm tích lũy, rồi chọn phương thức thanh toán. Chỉ một thao tác bấm 'Đặt vé' nhưng bên trong có nhiều bước xử lý liên tiếp. Facade đứng ra điều phối toàn bộ quy trình, Strategy chọn cách thanh toán, Builder tạo Order, còn Decorator xếp chồng các chính sách giảm giá."

### Thao tác trên web

1. Từ màn hình chọn ghế → chọn ghế trống.
2. Nhập **họ tên**, **email**.
3. Nhập **mã voucher** (nếu có).
4. Nhập **điểm tích lũy** (nếu là thành viên).
5. Chọn **Cash** hoặc **PayPal**.
6. Nhấn **Đặt vé**.
7. Trang "Đặt vé thành công" hiển thị thông tin đơn hàng.

### File cần mở

| File | Dòng |
|---|---|
| `Controllers/OrdersController.cs` | 311–351 |
| `Data/Facade/BookingFacade.cs` | 34–168 |
| `Models/Builders/OrderBuilder.cs` | 1–79 |
| `Data/Strategy/PaymentStrategy.cs` | 1–121 |
| `Data/Decorators/PricingDecorators.cs` | 1–218 |

### Pattern 3a — Facade (đơn giản hóa toàn bộ quy trình đặt vé)

**Controller rất ngắn — chỉ gọi một hàm của Facade:**

```csharp
// Controllers/OrdersController.cs:314-351
[HttpPost]
public async Task<IActionResult> BookTickets(BookTicketsVM model)
{
    if (!ModelState.IsValid)
    {
        TempData["BookingError"] = "Vui lòng nhập đầy đủ thông tin đặt vé hợp lệ.";
        return RedirectToAction(nameof(BookTickets),
            new { showtimeId = model.ShowtimeId });
    }

    // Facade xử lý toàn bộ: kiểm tra ghế → tính giá → thanh toán → tạo Order
    var result = await _bookingFacade.ProcessBookingAsync(model, User.Identity?.Name);

    if (!result.Success)
    {
        TempData["BookingError"] = result.Message;
        return RedirectToAction(nameof(BookTickets),
            new { showtimeId = model.ShowtimeId });
    }

    return View("BookingCompleted");
}
```

**Facade bên trong gọi nhiều service — Controller không cần biết:**

```text
OrdersController
      │
      └──► IBookingFacade.ProcessBookingAsync(...)
                 │
                 ├── 1. IShowtimesService    → lấy suất chiếu
                 ├── 2. IOrdersService       → kiểm tra ghế đã đặt
                 ├── 3. SeatPricingBridge    → tính giá theo loại ghế (Bridge)
                 ├── 4. IOrdersService       → kiểm tra voucher
                 ├── 5. PaymentContext       → thanh toán (Strategy)
                 ├── 6. OrderBuilder         → tạo Order (Builder)
                 └── 7. IOrdersService       → lưu database
```

### Pattern 3b — Strategy (chọn phương thức thanh toán tại runtime)

**Trong Facade, `PaymentContext` thay đổi strategy dựa trên lựa chọn của user:**

```csharp
// Data/Facade/BookingFacade.cs:99-105
// 7. Thanh toán (Strategy)
var paymentCtx = new PaymentContext();
paymentCtx.SetStrategyByName(model.PaymentMethod);   // "cash" hoặc "paypal"
var paymentResult = await paymentCtx.PayAsync(totalPrice, $"ORDER-{DateTime.Now.Ticks}");
```

**PaymentContext chọn strategy theo tên:**

```csharp
// Data/Strategy/PaymentStrategy.cs:89-103
public class PaymentContext
{
    private IPaymentStrategy? _strategy;

    public void SetStrategyByName(string? name)
    {
        var method = name?.ToLower() ?? "cash";
        _strategy = method switch
        {
            "paypal" => new PayPalPaymentStrategy("CLIENT_ID", "CLIENT_SECRET"),
            _        => new CashPaymentStrategy()
        };
    }

    public async Task<PaymentResult> PayAsync(double amount, string orderId)
    {
        if (_strategy == null)
            throw new InvalidOperationException("Payment strategy not set.");
        return await _strategy.PayAsync(amount, orderId);
    }

    public string CurrentPaymentMethod => _strategy?.PaymentMethod ?? "Unknown";
}
```

**Mỗi strategy triển khai `PayAsync` khác nhau:**

```csharp
// Data/Strategy/PaymentStrategy.cs:25-48 — Cash
public class CashPaymentStrategy : IPaymentStrategy
{
    public string PaymentMethod => "Cash";
    public Task<PaymentResult> PayAsync(double amount, string orderId)
    {
        return Task.FromResult(new PaymentResult
        {
            Success = true,
            TransactionId = $"CASH-{orderId}-{DateTime.Now.Ticks}",
            Message = "Thanh toán tại rạp - vui lòng thanh toán khi nhận vé."
        });
    }
}

// Data/Strategy/PaymentStrategy.cs:51-87 — PayPal (mô phỏng)
public class PayPalPaymentStrategy : IPaymentStrategy
{
    public string PaymentMethod => "PayPal";
    public async Task<PaymentResult> PayAsync(double amount, string orderId)
    {
        await Task.Delay(100); // giả lập API call
        return new PaymentResult
        {
            Success = true,
            TransactionId = $"PP-{orderId}-{DateTime.Now.Ticks}",
            Message = "Thanh toán PayPal thành công."
        };
    }
}
```

### Pattern 3c — Builder (tạo Order nhiều bước dễ đọc)

**Sau khi thanh toán, Facade dùng Builder ghép Order:**

```csharp
// Data/Facade/BookingFacade.cs:107-115
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

**Mỗi bước của Builder thiết lập một phần của Order:**

```csharp
// Models/Builders/OrderBuilder.cs:16-77
public class OrderBuilder : IOrderBuilder
{
    private readonly Order _order = new()
    {
        OrderItems = new List<OrderItem>(),
        OrderDate = DateTime.Now,
        Status = "Purchased"
    };
    private double _subtotal;
    private double _finalTotal;

    public IOrderBuilder SetCustomer(string name, string email, string userId)
    {
        _order.Email = email;
        _order.UserId = name;
        return this;  // fluent chain — trả về chính nó để method chaining
    }

    public IOrderBuilder SetShowtime(int showtimeId, string selectedSeats,
                                      int seatCount, double basePrice)
    {
        _subtotal = basePrice * seatCount;
        _order.OrderItems.Add(new OrderItem
        {
            ShowtimeId = showtimeId,
            SelectedSeats = selectedSeats,
            Amount = seatCount,
            Price = basePrice
        });
        return this;
    }

    public IOrderBuilder ApplyVoucher(double discountAmount, double orderTotal)
    {
        _order.DiscountAmount = Math.Min(discountAmount, orderTotal);
        return this;
    }

    public IOrderBuilder CalculateTotal()
    {
        _finalTotal = _subtotal - _order.DiscountAmount - _order.PointsRedeemed;
        if (_finalTotal < 0) _finalTotal = 0;
        _order.TotalPrice = _finalTotal;
        return this;
    }

    public Order Build() => _order;  // trả về Order hoàn chỉnh
}
```

### Pattern 3d — Decorator (xếp chồng chính sách giảm giá)

**Cùng trong Facade, Decorator tính giá chi tiết theo từng lớp:**

```csharp
// Data/Decorators/PricingDecorators.cs:113-146
public class OrderPriceCalculator
{
    public PriceCalculationResult Calculate(
        double basePrice, Voucher? voucher,
        int loyaltyPoints, bool applyHappyHour)
    {
        // Lớp 1: giá gốc
        IOrderPriceDecorator calc = new BasePriceCalculator(basePrice);

        // Lớp 2: bọc thêm voucher (nếu có)
        if (voucher != null)
            calc = new VoucherDecorator(calc, voucher);

        // Lớp 3: bọc thêm điểm tích lũy (nếu dùng)
        if (loyaltyPoints > 0)
            calc = new LoyaltyPointsDecorator(calc, loyaltyPoints);

        // Lớp 4: bọc thêm Happy Hour (nếu trong khung giờ 14:00–17:00)
        if (applyHappyHour)
            calc = new HappyHourDecorator(calc,
                new TimeSpan(14, 0, 0),
                new TimeSpan(17, 0, 0),
                15.0);

        double finalPrice = calc.CalculatePrice(basePrice);
        return new PriceCalculationResult { OriginalPrice = basePrice, FinalPrice = finalPrice, ... };
    }
}
```

**Mỗi decorator bọc calculator trước và thêm một bước giảm giá:**

```text
BasePriceCalculator (giá gốc 100.000đ)
   └─► VoucherDecorator (giảm 10% → 90.000đ)
         └─► LoyaltyPointsDecorator (trừ 5.000đ = 5 điểm → 85.000đ)
               └─► HappyHourDecorator (giảm thêm 15% nếu 14h–17h → 72.250đ)
```

```csharp
// Data/Decorators/PricingDecorators.cs:54-74 — ví dụ LoyaltyPointsDecorator
public class LoyaltyPointsDecorator : IOrderPriceDecorator
{
    private readonly IOrderPriceDecorator _inner;  // calculator bên trong
    private readonly int _points;

    public LoyaltyPointsDecorator(IOrderPriceDecorator inner, int points)
    {
        _inner = inner;
        _points = points;
    }

    public double CalculatePrice(double currentPrice)
    {
        double afterVoucher = _inner.CalculatePrice(currentPrice); // gọi calculator bên trong TRƯỚC
        double pointValue = _points * 1000.0;  // 1 điểm = 1.000đ
        return Math.Max(0, afterVoucher - pointValue);  // rồi trừ điểm
    }
}
```

### Câu nói khi demo

> "Khi bấm đặt vé, Controller chỉ gọi đúng 1 hàm: `_bookingFacade.ProcessBookingAsync()`. Facade bên trong thực hiện 9 bước — từ kiểm tra ghế, tính giá theo loại ghế (Bridge), kiểm tra voucher, chọn phương thức thanh toán (Strategy), đến tạo Order bằng Builder và lưu database. Thanks Facade, Controller chỉ 7 dòng code thay vì 70 dòng. Builder giúp code đọc như quy trình nghiệp vụ: SetCustomer → SetShowtime → ApplyVoucher → CalculateTotal → Build. Decorator xếp chồng chính sách giảm giá — muốn thêm FestivalDiscount chỉ cần thêm 1 decorator, không sửa code gốc."

---

## Chức năng 4 — Kiểm tra dữ liệu đặt vé: Chain of Responsibility Pattern

### 🎙️ Lời dẫn demo

> "Để đảm bảo dữ liệu đặt vé luôn hợp lệ trước khi xử lý, hệ thống sử dụng Chain of Responsibility — một chuỗi các bước kiểm tra xếp hàng. Mình sẽ thử一些 tình huống bất hợp lệ: chọn quá 10 ghế, chọn ghế đã bị đặt, hoặc nhập voucher sai. Bạn sẽ thấy từng bước kiểm tra xử lý tuần tự — bước nào fail thì dừng ngay tại đó."

### Thao tác trên web

1. Từ màn hình chọn ghế → chọn **nhiều hơn 10 ghế** → bấm đặt vé.
2. Thông báo lỗi: **"Không thể đặt quá 10 ghế mỗi lần."**
3. Hoặc chọn một ghế **đã bị người khác đặt** → thông báo: **"Ghế A1 đã được đặt bởi người khác."**
4. Hoặc nhập **voucher sai/hết hạn** → thông báo lỗi tương ứng.

### File cần mở

| File | Dòng |
|---|---|
| `Data/Chain/OrderPipeline.cs` | 1–239 |
| `Data/Mediator/BookingMediator.cs` | 117–155 |

### Pattern: Chain of Responsibility

**Pipeline gồm 4 handler xếp hàng, mỗi handler kiểm tra một bước:**

```csharp
// Data/Chain/OrderPipeline.cs:226-238
public static class OrderPipelineBuilder
{
    public static OrderPipelineHandler Build(IOrdersService ordersService)
    {
        var validation = new ValidationHandler();           // Bước 1: kiểm tra dữ liệu đầu vào
        var seats = new SeatAvailabilityHandler(ordersService); // Bước 2: kiểm tra ghế trống
        var voucher = new VoucherValidationHandler(ordersService); // Bước 3: kiểm tra voucher
        var member = new MemberValidationHandler(ordersService);   // Bước 4: kiểm tra điểm thành viên

        validation.SetNext(seats).SetNext(voucher).SetNext(member);
        return validation;  // trả về handler đầu tiên
    }
}
```

**Handler base có `_next` để nối chain:**

```csharp
// Data/Chain/OrderPipeline.cs:23-36
public abstract class OrderPipelineHandler
{
    protected OrderPipelineHandler? _next;

    public OrderPipelineHandler SetNext(OrderPipelineHandler next)
    {
        _next = next;
        return next;  // trả về next để chain tiếp
    }

    public abstract Task<OrderPipelineResult> HandleAsync(
        OrderPipelineRequest request, OrderPipelineResult result);
}
```

**Handler kiểm tra số lượng ghế (Bước 1 — ValidationHandler):**

```csharp
// Data/Chain/OrderPipeline.cs:39-78
public class ValidationHandler : OrderPipelineHandler
{
    public override async Task<OrderPipelineResult> HandleAsync(
        OrderPipelineRequest request, OrderPipelineResult result)
    {
        if (request.Model.ShowtimeId <= 0)
        {
            result.IsValid = false;
            result.Message = "Suất chiếu không hợp lệ.";
            return result;  // DỪNG chain — không gọi _next
        }

        var seats = request.Model.SelectedSeats
            .Split(',').Select(s => s.Trim()).ToList();

        if (seats.Count > 10)
        {
            result.IsValid = false;
            result.Message = "Không thể đặt quá 10 ghế mỗi lần.";
            return result;  // DỪNG chain
        }

        return _next != null
            ? await _next.HandleAsync(request, result)  // HỢP LỆ → chuyển sang handler tiếp
            : result;
    }
}
```

**Handler kiểm tra ghế đã bị đặt (Bước 2 — SeatAvailabilityHandler):**

```csharp
// Data/Chain/OrderPipeline.cs:82-117
public class SeatAvailabilityHandler : OrderPipelineHandler
{
    public override async Task<OrderPipelineResult> HandleAsync(
        OrderPipelineRequest request, OrderPipelineResult result)
    {
        if (!result.IsValid) return result;  // đã fail từ bước trước → bỏ qua

        var bookedSeats = await _ordersService
            .GetBookedSeatsForShowtimeAsync(request.Model.ShowtimeId);

        foreach (var seat in selectedSeats)
        {
            if (bookedSeats.Contains(seat))
            {
                result.IsValid = false;
                result.Message = $"Ghế {seat} đã được đặt bởi người khác.";
                return result;  // DỪNG chain tại đây
            }
        }

        return _next != null
            ? await _next.HandleAsync(request, result)  // OK → chuyển bước tiếp
            : result;
    }
}
```

**Handler kiểm tra voucher và điểm thành viên tương tự:**

```text
ValidationHandler
       │  passes
       ▼
SeatAvailabilityHandler
       │  passes
       ▼
VoucherValidationHandler
       │  passes
       ▼
MemberValidationHandler
       │  passes
       ▼
  Pipeline hoàn tất ✓
```

**Pipeline được gọi trong Mediator handler:**

```csharp
// Data/Mediator/BookingMediator.cs:117-132
public async Task<CompleteBookingResponse> HandleAsync(CompleteBookingRequest request)
{
    // 1. Validate qua Chain of Responsibility
    var pipeline = OrderPipelineBuilder.Build(_ordersService);
    var pipelineResult = await pipeline.HandleAsync(
        new OrderPipelineRequest { Model = request.Model },
        new OrderPipelineResult { IsValid = true });

    if (!pipelineResult.IsValid)
        return new CompleteBookingResponse { Success = false, Message = pipelineResult.Message };

    // 2. Nếu valid → Process booking qua Facade
    var bookingResult = await _facade.ProcessBookingAsync(request.Model, request.UserId);
    ...
}
```

### Câu nói khi demo

> "Trước khi đặt vé, pipeline kiểm tra tuần tự 4 bước. Nếu chọn hơn 10 ghế, ValidationHandler dừng chain ngay tại bước 1 — voucher và member handler không chạy. Nếu chọn ghế đã bị đặt, chain chạy qua ValidationHandler rồi dừng ở SeatAvailabilityHandler. Ưu điểm: muốn thêm bước mới比如 kiểm tra giới hạn đặt vé theo IP, chỉ cần thêm 1 handler và nối vào chain — không sửa code của các handler cũ."

---

## Chức năng 5 — Quản trị xác nhận đơn: State + Mediator + Observer (3 pattern cùng lúc)

### 🎙️ Lời dẫn demo

> "Bây giờ mình sẽ chuyển sang góc độ quản trị. Đăng nhập với tài khoản Admin, vào trang Manage Bookings để quản lý đơn hàng. Ở đây, mỗi đơn có trạng thái riêng — Purchased, Confirmed, Cancelled, Refunded — và việc chuyển trạng thái phải tuân theo quy tắc. Mình sẽ demo Confirm, Cancel, và Refund để bạn thấy State machine kiểm soát vòng đời đơn hàng, Observer phản ứng khi trạng thái thay đổi, và Mediator giúp Controller không phụ thuộc trực tiếp vào nhiều service."

### Thao tác trên web

1. Đăng nhập tài khoản **Admin**.
2. Vào **Admin → Manage Bookings**.
3. Thấy danh sách đơn hàng với trạng thái **Purchased** (màu vàng).
4. Bấm **Confirm** cho một đơn → trạng thái chuyển thành **Confirmed** (màu xanh).
5. Bấm **Cancel** cho một đơn → trạng thái chuyển thành **Cancelled** (màu đỏ).
6. Bấm **Refund** cho một đơn đã Confirm → trạng thái chuyển thành **Refunded**.

### File cần mở

| File | Dòng |
|---|---|
| `Controllers/OrdersController.cs` | 469–497 |
| `Data/Mediator/BookingMediator.cs` | 66–97 |
| `Data/State/OrderStateMachine.cs` | 1–116 |
| `Data/Observer/OrderObserver.cs` | 1–205 |
| `Data/Services/OrdersService.cs` | 224–269 |
| `Program.cs` | 68–83 |

### Pattern 5a — Mediator (giảm coupling giữa Controller và handler)

**Controller gửi request đến Mediator, không gọi trực tiếp service:**

```csharp
// Controllers/OrdersController.cs:469-477
[Authorize(Roles = "Admin")]
[HttpPost]
public async Task<IActionResult> ConfirmBooking(int id, ...)
{
    await _ordersService.ChangeOrderStatusAsync(id, "Confirmed");
    // Gọi trực tiếp OrdersService, bên trong gọi State machine
    TempData["ManageStatusMessage"] = $"Xác nhận vé #{id} thành công!";
    return RedirectToAction(nameof(ManageBookings), ...);
}
```

**Mediator tìm handler đúng dựa trên request type:**

```csharp
// Data/Mediator/BookingMediator.cs:66-97
public class AppMediator : IMediator
{
    private readonly IServiceProvider _serviceProvider;

    public async Task<TResponse> SendAsync<TResponse>(IRequest<TResponse> request)
    {
        // Tìm handler từ DI container dựa trên request type
        var handlerType = typeof(IRequestHandler<,>)
            .MakeGenericType(request.GetType(), typeof(TResponse));

        var handler = _serviceProvider.GetServices(handlerType).FirstOrDefault();
        var method = handlerType.GetMethod("HandleAsync");
        var result = method.Invoke(handler, new[] { request });

        if (result is Task<TResponse> task)
            return await task;

        throw new InvalidOperationException("Handler error");
    }
}
```

**Mediator đăng ký 3 request/handler trong Program.cs:**

```csharp
// Program.cs:73-81
builder.Services.AddScoped<IMediator, AppMediator>();
builder.Services.AddScoped<IRequestHandler<CompleteBookingRequest, CompleteBookingResponse>,
                            CompleteBookingHandler>();
builder.Services.AddScoped<IRequestHandler<CancelBookingRequest, CancelBookingResponse>,
                            CancelBookingHandler>();
builder.Services.AddScoped<IRequestHandler<ConfirmBookingRequest, ConfirmBookingResponse>,
                            ConfirmBookingHandler>();
```

### Pattern 5b — State (kiểm soát vòng đời Order)

**OrderStateMachine kiểm tra transition hợp lệ trước khi chuyển trạng thái:**

```csharp
// Data/State/OrderStateMachine.cs:90-115
public class OrderStateMachine
{
    private static readonly Dictionary<string, IOrderState> _states = new()
    {
        ["Purchased"] = new PurchasedState(),
        ["Confirmed"] = new ConfirmedState(),
        ["Cancelled"] = new CancelledState(),
        ["Refunded"]  = new RefundedState(),
    };

    public static bool CanTransition(string from, string to)
    {
        if (!_states.TryGetValue(from, out var state))
            return false;
        return state.CanTransitionTo(to);  // delegation cho state cụ thể
    }
}
```

**Mỗi state quyết định được chuyển sang trạng thái nào:**

```csharp
// Data/State/OrderStateMachine.cs:14-26
public class PurchasedState : IOrderState
{
    public string StatusName => "Purchased";
    public bool CanTransitionTo(string newStatus)
        => newStatus is "Confirmed" or "Cancelled";
    // Purchased → chỉ được Confirm hoặc Cancel, KHÔNG được Refund trực tiếp
}

// Data/State/OrderStateMachine.cs:28-39
public class ConfirmedState : IOrderState
{
    public string StatusName => "Confirmed";
    public bool CanTransitionTo(string newStatus)
        => newStatus is "Cancelled" or "Refunded";
    // Confirmed → được Cancel hoặc Refund
}

// Data/State/OrderStateMachine.cs:42-47
public class CancelledState : IOrderState
{
    public string StatusName => "Cancelled";
    public bool CanTransitionTo(string newStatus) => false;
    // Cancelled → KHÔNG chuyển được gì nữa (terminal)
}
```

**OrdersService kiểm tra transition trước khi cập nhật DB:**

```csharp
// Data/Services/OrdersService.cs:224-269
public async Task<StatusChangeResult> ChangeOrderStatusWithStateAsync(int orderId, string newStatus)
{
    var order = await _context.Orders.Include(o => o.OrderItems)
        .FirstOrDefaultAsync(o => o.Id == orderId);

    string oldStatus = order.Status;

    // ── State pattern — kiểm tra transition hợp lệ ──
    if (!OrderStateMachine.CanTransition(oldStatus, newStatus))
        return new StatusChangeResult
        {
            Success = false,
            Message = $"Không thể chuyển từ [{oldStatus}] sang [{newStatus}]."
        };

    order.Status = newStatus;

    // ── State pattern — gọi OnEnter() của state mới ──
    var state = OrderStateMachine.GetState(newStatus);
    if (state != null)
        await state.OnEnterAsync(order, _context);  // CancelledState.OnEnter → hoàn điểm

    await _context.SaveChangesAsync();
    return new StatusChangeResult { Success = true, ... };
}
```

**OnEnter của CancelledState tự hoàn điểm thành viên:**

```csharp
// Data/State/OrderStateMachine.cs:48-63
public class CancelledState : IOrderState
{
    public async Task OnEnterAsync(Order order, AppDbContext context)
    {
        // Giải phóng ghế + hoàn điểm
        var member = await context.Members
            .FirstOrDefaultAsync(m => m.Email.ToLower() == order.Email.ToLower());
        if (member != null)
        {
            int earned = (int)(finalPrice / 10000);
            member.Points = Math.Max(0, member.Points - earned + (order.PointsRedeemed / 1000));
        }
        await context.SaveChangesAsync();
    }
}
```

### Pattern 5c — Observer (phản ứng khi status thay đổi)

**OrderSubject quản lý danh sách observer và thông báo khi status đổi:**

```csharp
// Data/Observer/OrderObserver.cs:24-84
public class OrderSubject : IOrderSubject
{
    private readonly List<IOrderObserver> _observers = new();
    private readonly IServiceScopeFactory _scopeFactory;

    public void Attach(IOrderObserver observer) { _observers.Add(observer); }
    public void Detach(IOrderObserver observer) { _observers.Remove(observer); }

    public async Task NotifyAsync(Order order, string oldStatus, string newStatus)
    {
        var observers = GetScopedObservers();
        foreach (var observer in observers)
        {
            try
            {
                await observer.OnOrderStatusChangedAsync(order, oldStatus, newStatus);
            }
            catch
            {
                // Log lỗi nhưng KHÔNG ngăn observer khác chạy
            }
        }
    }
}
```

**3 observer đăng ký trong Program.cs:**

```csharp
// Program.cs:68-83
builder.Services.AddSingleton<IOrderSubject, OrderSubject>();  // Singleton — 1 subject cho toàn app
builder.Services.AddScoped<IOrderObserver, AuditLogObserver>();
builder.Services.AddScoped<IOrderObserver, LoyaltyPointsObserver>();
builder.Services.AddScoped<IOrderObserver, EmailNotificationObserver>();
```

**Observer 1 — AuditLogObserver: ghi log thay đổi trạng thái:**

```csharp
// Data/Observer/OrderObserver.cs:88-109
public class AuditLogObserver : IOrderObserver
{
    private readonly ILogger<AuditLogObserver> _logger;
    public Task OnOrderStatusChangedAsync(Order order, string oldStatus, string newStatus)
    {
        _logger.LogInformation(
            "[AUDIT] Order #{OrderId} | {Old} → {New} | Total: {Total:N0}VND",
            order.Id, oldStatus, newStatus,
            order.TotalPrice - order.DiscountAmount);
        return Task.CompletedTask;
    }
}
```

**Observer 2 — LoyaltyPointsObserver: cộng/trừ điểm thành viên:**

```csharp
// Data/Observer/OrderObserver.cs:112-150
public class LoyaltyPointsObserver : IOrderObserver
{
    public async Task OnOrderStatusChangedAsync(Order order, string oldStatus, string newStatus)
    {
        var member = await context.Members
            .FirstOrDefaultAsync(m => m.Email.ToLower() == order.Email.ToLower());

        int earned = (int)(finalPrice / 10000);

        if (newStatus == "Cancelled" || newStatus == "Refunded")
            member.Points = Math.Max(0, member.Points - earned);  // hoàn điểm
        else if (newStatus == "Confirmed" && oldStatus == "Purchased")
            member.Points += earned;  // cộng điểm khi xác nhận

        await context.SaveChangesAsync();
    }
}
```

**Observer 3 — EmailNotificationObserver: gửi email thông báo:**

```csharp
// Data/Observer/OrderObserver.cs:154-204
public class EmailNotificationObserver : IOrderObserver
{
    public Task OnOrderStatusChangedAsync(Order order, string oldStatus, string newStatus)
    {
        var (subject, body) = newStatus switch
        {
            "Confirmed" => ($"[MovieCinema] Xac nhan don hang #{order.Id}",
                           $"Don hang #{order.Id} da duoc xac nhan thanh cong..."),
            "Cancelled" => ($"[MovieCinema] Don hang #{order.Id} da bi huy",
                           $"Don hang #{order.Id} da duoc huy..."),
            "Refunded"  => ($"[MovieCinema] Hoan tien don hang #{order.Id}",
                           $"Don hang #{order.Id} da duoc hoan tien..."),
            _ => (null, null)
        };
        // Stub: thay bằng SendGrid/SMTP thật khi production
        _logger.LogInformation("[EMAIL] To: {Email} | Subject: {Subject}", order.Email, subject);
        return Task.CompletedTask;
    }
}
```

### Câu nói khi demo

> "Khi Admin bấm Confirm, Controller gọi `ChangeOrderStatusAsync`. Bên trong, State machine kiểm tra: Purchased → Confirmed có hợp lệ không? Có. Rồi `OnEnterAsync()` của ConfirmedState chạy — ví dụ sinh QR code vé. Sau khi status đổi, Observer thông báo cho 3 observer: AuditLog ghi log, LoyaltyPoints cộng điểm thành viên, EmailNotification gửi email xác nhận. Muốn thêm SMS hoặc push notification, chỉ cần thêm 1 observer mới — không sửa code Confirm."

---

## Chức năng 6 — Giỏ hàng (ShoppingCart)

### 🎙️ Lời dẫn demo

> "Cuối cùng, mình sẽ demo giỏ hàng — nơi lưu tạm các suất chiếu mà user muốn đặt. Mình sẽ thêm một suất chiếu, tăng giảm số lượng và xóa item để quan sát cách giỏ hàng cập nhật. ShoppingCart được gắn với Session nên mỗi user có một giỏ riêng; khi thêm cùng một suất chiếu và cùng ghế, hệ thống sẽ gộp vào item hiện có thay vì tạo bản ghi trùng."

### Thao tác trên web

1. Từ trang chi tiết phim → bấm **"Thêm vào giỏ"** trên một suất chiếu.
2. Vào **Giỏ hàng** → thấy suất chiếu đã thêm.
3. Bấm **"+"** để thêm số lượng, **"-"** để giảm.
4. Bấm **"Xóa"** để xóa khỏi giỏ.

### File cần mở

| File | Dòng |
|---|---|
| `Controllers/OrdersController.cs` | 93–142 |
| `Data/Cart/ShoppingCart.cs` | 1–126 |
| `Models/ShoppingCartItem.cs` | 1–30 |

### Pattern nền tảng: Singleton theo Session

**ShoppingCart được tạo 1 lần duy nhất per session, lưu trữ qua DI:**

```csharp
// Program.cs:86
builder.Services.AddScoped(sc => ShoppingCart.GetShoppingCart(sc));

// Data/Cart/ShoppingCart.cs:17-26
public static ShoppingCart GetShoppingCart(IServiceProvider services)
{
    ISession session = services.GetRequiredService<IHttpContextAccessor>()?.HttpContext.Session;
    var context = services.GetService<AppDbContext>();

    string cartId = session.GetString("CartId") ?? Guid.NewGuid().ToString();
    session.SetString("CartId", cartId);  // lưu CartId vào Session

    return new ShoppingCart(context) { ShoppingCartId = cartId };
}
```

**Logic gộp/mở rộng item trong giỏ:**

```csharp
// Data/Cart/ShoppingCart.cs:28-74
public void AddItemToCart(Showtime showtime, string? selectedSeats = null, double? price = null)
{
    ShoppingCartItem? shoppingCartItem = null;

    if (!string.IsNullOrEmpty(selectedSeats))
    {
        // Có ghế → gộp theo Showtime + SelectedSeats
        shoppingCartItem = _context.ShoppingCartItems
            .FirstOrDefault(n => n.ShowtimeId == showtime.Id
                                 && n.ShoppingCartId == ShoppingCartId
                                 && n.SelectedSeats == selectedSeats);
    }

    if (shoppingCartItem == null)
    {
        // Item mới → thêm vào giỏ
        shoppingCartItem = new ShoppingCartItem()
        {
            ShoppingCartId = ShoppingCartId,
            Showtime = showtime,
            Amount = 1,
            SelectedSeats = selectedSeats,
            Price = price ?? showtime.Price
        };
        _context.ShoppingCartItems.Add(shoppingCartItem);
    }
    else
    {
        shoppingCartItem.Amount++;  // đã có → tăng số lượng
    }
    _context.SaveChanges();
}
```

**Tính tổng tiền giỏ hàng:**

```csharp
// Data/Cart/ShoppingCart.cs:108-115
public double GetShoppingCartTotal()
{
    return _context.ShoppingCartItems
        .Where(n => n.ShoppingCartId == ShoppingCartId)
        .Include(n => n.Showtime)
        .ToList()
        .Sum(n => (n.Price > 0 ? n.Price : n.Showtime.Price) * n.Amount);
}
```

### Câu nói khi demo

> "ShoppingCart được tạo theo Session — mỗi user có giỏ riêng. Khi thêm suất chiếu, hệ thống kiểm tra đã có trong giỏ chưa để gộp hoặc thêm mới. Tổng tiền được tính real-time từ database. Đây là cơ chế Singleton per session — đảm bảo mỗi user chỉ có 1 ShoppingCart instance trong suốt phiên."

---

## Bảng tổng kết các Pattern trong project

| # | Pattern | Chức năng áp dụng | File chính | Câu nói ngắn |
|---|---|---|---|---|
| 1 | **Proxy** | Cache danh sách phim | `CachedMoviesServiceProxy.cs` | Cùng interface, thêm cache phía trước service thật |
| 2 | **Bridge** | Tính giá theo loại ghế | `SeatPricingBridge.cs` | Tách loại ghế khỏi thuật toán giá |
| 3 | **Facade** | Đơn giản hóa đặt vé | `BookingFacade.cs` | Controller gọi 1 cổng thay vì 7 service |
| 4 | **Strategy** | Chọn phương thức thanh toán | `PaymentStrategy.cs` | Cash/PayPal thay thế tại runtime |
| 5 | **Builder** | Tạo Order nhiều bước | `OrderBuilder.cs` | Fluent chain dễ đọc như quy trình |
| 6 | **Decorator** | Xếp chồng giảm giá | `PricingDecorators.cs` | Voucher, điểm, HappyHour bọc nhau |
| 7 | **Chain of Responsibility** | Kiểm tra dữ liệu đặt vé | `OrderPipeline.cs` | Sai ở đâu → dừng ở handler đó |
| 8 | **State** | Kiểm soát vòng đời Order | `OrderStateMachine.cs` | Không cho chuyển trạng thái bất hợp lệ |
| 9 | **Mediator** | Giảm coupling Controller-handler | `BookingMediator.cs` | Gửi request, handler xử lý |
| 10 | **Observer** | Phản ứng khi status đổi | `OrderObserver.cs` | Log, điểm, email nhận cùng event |
| 11 | **DI** | Mọi Controller/Service | `Program.cs` | Nhận interface qua constructor |
| 12 | **Singleton** | ShoppingCart per Session | `ShoppingCart.cs` | 1 giỏ hàng cho mỗi user |

---

## Câu hỏi thường gặp khi bảo vệ/demo

### 1. Facade và Mediator khác nhau thế nào?

**Facade** gom các thao tác của một subsystem (đặt vé) thành một API nghiệp vụ. **Mediator** điều phối các request/handler, giúp Controller không tham chiếu trực tiếp nhiều thành phần. Cả hai có thể cùng xuất hiện: Mediator handler nhận request → gọi Facade để xử lý booking.

### 2. Strategy và Bridge có giống nhau không?

Cả hai đều dùng interface để tách thuật toán, nhưng: **Strategy** thay thế một thuật toán tại runtime (Cash/PayPal). **Bridge** tách hai chiều biến đổi độc lập (abstraction tính giá và implementation theo loại ghế).

### 3. Nếu thêm MoMo thanh toán thì sửa ở đâu?

Tạo `MoMoPaymentStrategy : IPaymentStrategy`, triển khai `PayAsync`/`RefundAsync`, rồi thêm `case "momo"` trong `PaymentContext.SetStrategyByName`. Controller/Builder không cần biết chi tiết API MoMo.

### 4. Nếu thêm loại ghế Premium thì sửa ở đâu?

Tạo `PremiumPricingStrategy : ISeatingPricingStrategy`, thêm `SeatType.Premium` trong enum, và thêm 1 dòng trong switch của `SeatPricingBridge`. Không sửa Controller hay các pricing strategy khác.

### 5. PayPal trong demo đã gọi API thật chưa?

Chưa. `PayPalPaymentStrategy` mô phỏng bằng `Task.Delay`. Production cần thay bằng SDK thật và bảo vệ secret trong configuration/secret manager.

### 6. Race condition khi đặt ghế trùng thì sao?

Pipeline kiểm tra ở bước nghiệp vụ, nhưng production cần transaction/unique constraint hoặc cơ chế giữ ghế có thời hạn để xử lý race condition giữa bước kiểm tra và bước lưu Order.
