# Kịch bản demo MovieCinema — Design Pattern đã áp dụng

> **Thời lượng:** 30 phút
>
> Tài liệu này chỉ trình bày những pattern có code thật trong project MovieCinema. Mỗi pattern đều có: chức năng trên web, file cần mở, đoạn code tiêu biểu và lời giải thích.

---

# PHẦN I — MỞ ĐẦU VÀ KIẾN TRÚC NỀN TẢNG (2 phút)

---

## 1. Giới thiệu hệ thống

### Lời nói

> "MovieCinema là website đặt vé xem phim xây dựng bằng ASP.NET Core MVC, Entity Framework Core và SQL Server. Người dùng có thể xem phim, xem suất chiếu, chọn ghế, áp dụng voucher, thanh toán và theo dõi đơn hàng. Admin có thể quản lý booking, xác nhận, hủy, hoàn tiền và xem báo cáo.
>
> Trong demo, em sẽ đi theo luồng: xem danh sách phim → chọn ghế → đặt vé → tạo Order → Admin xử lý trạng thái. Sau mỗi bước, em sẽ giải thích design pattern nằm ở đâu và pattern đó giải quyết vấn đề gì."


### Mở `Program.cs` — Dependency Injection

```csharp
builder.Services.AddScoped<IOrdersService, OrdersService>();
builder.Services.AddScoped<ISeatsService, SeatsService>();
builder.Services.AddScoped<IShowtimesService, ShowtimesService>();
builder.Services.AddScoped<IBookingFacade, BookingFacade>();
builder.Services.AddScoped<IOrderBuilder, OrderBuilder>();
builder.Services.AddScoped<IMediator, AppMediator>();
```

### Giải thích

> "Project dùng Dependency Injection: Controller nhận interface qua constructor, không tự tạo service bằng `new`. Ví dụ `OrdersController` nhận `IBookingFacade`, `IOrdersService`, `ISeatsService`. Nhờ đó các class ít phụ thuộc cứng vào nhau, dễ thay implementation và dễ mock khi test.
>
> MVC, Service Layer, Repository và DI là kiến trúc nền tảng, có mặt khắp nơi. Từ đây, em sẽ trình bày từng design pattern cụ thể trong các chức năng chính."

---

# PHẦN II — CREATIONAL PATTERNS (Nhóm khởi tạo — 7 phút)

---

## 2. Singleton — `ShoppingCart` và `OrderSubject` (1 phút)

### Chức năng trên web

- `ShoppingCart` giữ một giỏ hàng duy nhất cho mỗi phiên người dùng.
- `OrderSubject` là một đối tượng duy nhất quản lý danh sách Observer.

### File cần mở

- `Data/Cart/ShoppingCart.cs`
- `Data/Observer/OrderObserver.cs`
- `Program.cs`

### Code trong `Program.cs`

```csharp
builder.Services.AddSingleton<IOrderSubject, OrderSubject>();
builder.Services.AddSingleton<IHttpContextAccessor, HttpContextAccessor>();
builder.Services.AddScoped(sc => ShoppingCart.GetShoppingCart(sc));
```

### Code trong `ShoppingCart.cs`

```csharp
public static ShoppingCart GetShoppingCart(IServiceProvider services)
{
    var session = services
        .GetRequiredService<IHttpContextAccessor>()
        ?.HttpContext.Session;
    var context = services.GetService<AppDbContext>();
    string cartId = session.GetString("CartId")
        ?? Guid.NewGuid().ToString();
    session.SetString("CartId", cartId);
    return new ShoppingCart(context) { ShoppingCartId = cartId };
}
```

### Code trong `OrderSubject.cs`

```csharp
public class OrderSubject : IOrderSubject
{
    private readonly List<IOrderObserver> _observers = new();
    private readonly IServiceScopeFactory _scopeFactory;
    private bool _initialized;
    private readonly object _lock = new();

    public async Task NotifyAsync(Order order,
        string oldStatus, string newStatus)
    {
        var observers = GetScopedObservers();
        foreach (var observer in observers)
        {
            try
            {
                await observer.OnOrderStatusChangedAsync(
                    order, oldStatus, newStatus);
            }
            catch
            {
                // Log lỗi nhưng không ngăn các observer khác
            }
        }
    }
}
```

### Giải thích

> "Singleton đảm bảo một đối tượng duy nhất. `OrderSubject` đăng ký `AddSingleton` trong DI, có một phiên bản duy nhất quản lý danh sách Observer trong toàn bộ application. `ShoppingCart` dùng session để đảm bảo một giỏ hàng cho mỗi phiên người dùng."

---

## 3. Factory Method — đăng ký service trong DI (1 phút)

### Chức năng trên web

Tất cả service được đăng ký trong `Program.cs` — DI container tạo instance khi cần.

### File cần mở

`Program.cs`

### Code

```csharp
builder.Services.AddScoped<IActorsService, ActorsService>();
builder.Services.AddScoped<IProducersService, ProducersService>();
builder.Services.AddScoped<ICinemasService, CinemasService>();
builder.Services.AddScoped<ICinemaRoomsService, CinemaRoomsService>();
builder.Services.AddScoped<ICategoriesService, CategoriesService>();
builder.Services.AddScoped<IOrdersService, OrdersService>();
builder.Services.AddScoped<ISeatsService, SeatsService>();
builder.Services.AddScoped<IShowtimesService, ShowtimesService>();
builder.Services.AddScoped<IVouchersService, VouchersService>();
builder.Services.AddScoped<MoviesService>();
builder.Services.AddScoped<ISeatingPricingStrategy, StandardPricingStrategy>();
builder.Services.AddScoped<IOrderBuilder, OrderBuilder>();
builder.Services.AddScoped<IPaymentStrategy, CashPaymentStrategy>();
```

### Giải thích

> "DI container trong ASP.NET Core hoạt động như một Factory: với `AddScoped<IOrdersService, OrdersService>()`, khi có class yêu cầu `IOrdersService`, container sẽ tạo instance `OrdersService`. Mỗi request HTTP sẽ có một scope chứa các service này.
>
> Đây là Factory Method ở mức framework: interface là sản phẩm, DI container là Factory, và implementation là subclass tương ứng."

---

## 4. Builder — tạo Order nhiều bước (2 phút)

### Chức năng trên web

Đặt vé thành công, Order được tạo với nhiều thuộc tính: khách hàng, email, suất chiếu, ghế, voucher, điểm, phương thức thanh toán, tổng tiền.

### File cần mở

- `Models/Builders/OrderBuilder.cs`
- `Data/Facade/BookingFacade.cs`

### Code trong `OrderBuilder.cs`

```csharp
public interface IOrderBuilder
{
    IOrderBuilder SetCustomer(string name, string email, string userId);
    IOrderBuilder SetShowtime(int showtimeId, string selectedSeats,
                              int seatCount, double basePrice);
    IOrderBuilder ApplyVoucher(double discountAmount, double orderTotal);
    IOrderBuilder RedeemPoints(int points, double totalBeforePoints);
    IOrderBuilder SetPaymentMethod(string method);
    IOrderBuilder CalculateTotal();
    Order Build();
}

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
        return this;
    }

    public IOrderBuilder SetShowtime(int showtimeId, string selectedSeats,
                                     int seatCount, double basePrice)
    {
        _subtotal = basePrice * seatCount;
        var item = new OrderItem
        {
            ShowtimeId = showtimeId,
            SelectedSeats = selectedSeats,
            Amount = seatCount,
            Price = basePrice
        };
        _order.OrderItems.Add(item);
        return this;
    }

    public IOrderBuilder ApplyVoucher(double discountAmount, double orderTotal)
    {
        _order.DiscountAmount = Math.Min(discountAmount, orderTotal);
        return this;
    }

    public IOrderBuilder RedeemPoints(int points, double totalBeforePoints)
    {
        double pointValue = points * 1000.0;
        _order.PointsRedeemed = (int)Math.Min(pointValue, totalBeforePoints);
        return this;
    }

    public IOrderBuilder SetPaymentMethod(string method)
    {
        _order.PaymentMethod = method;
        return this;
    }

    public IOrderBuilder CalculateTotal()
    {
        _finalTotal = _subtotal - _order.DiscountAmount - _order.PointsRedeemed;
        if (_finalTotal < 0) _finalTotal = 0;
        _order.TotalPrice = _finalTotal;
        return this;
    }

    public Order Build() => _order;
}
```

### Gọi từ `BookingFacade.cs`

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

### Giải thích

> "Order có nhiều dữ liệu: khách hàng, email, suất chiếu, danh sách ghế, voucher, điểm, phương thức thanh toán và tổng tiền. Nếu dùng một constructor với quá nhiều tham số, code sẽ khó đọc và dễ truyền nhầm thứ tự.
>
> `OrderBuilder` chia quá trình khởi tạo thành các bước có tên rõ ràng và hỗ trợ fluent chain. `CalculateTotal` tập trung công thức tổng tiền, còn `Build` trả về Order hoàn chỉnh. Code gọi đọc như một quy trình nghiệp vụ."

---

## 5. Abstract Factory — họ chính sách theo cinema (1 phút)

### Chức năng trên web

Khi có nhiều hệ thống rạp (Galaxy, Lotte, BHD) với chính sách giá/hoàn tiền khác nhau, mỗi cinema cần một bộ strategy khác nhau.

### File cần mở

`DESIGN_PATTERNS_GUIDE.md` mục 2.4 — hướng mở rộng trong architecture.

### Code minh họa

```csharp
public interface ICinemaFactory
{
    IPricingStrategy CreatePricingStrategy();
    IRefundPolicy CreateRefundPolicy();
    IMovieSelector CreateMovieSelector();
    string CinemaName { get; }
}

public class GalaxyCinemaFactory : ICinemaFactory
{
    public string CinemaName => "Galaxy Cinema";
    public IPricingStrategy CreatePricingStrategy()
        => new GalaxyPricingStrategy();      // Giảm giá 20% vào thứ Ba
    public IRefundPolicy CreateRefundPolicy()
        => new FlexibleRefundPolicy();       // Hoàn 100% trước 24h
    public IMovieSelector CreateMovieSelector()
        => new StandardMovieSelector();
}
```

### Giải thích

> "Abstract Factory cung cấp interface để tạo **họ các đối tượng liên quan** mà không cần chỉ định class cụ thể. Trong hệ thống đặt vé, mỗi hệ thống rạp có thể có factory riêng với chính sách giá và hoàn tiền khác nhau. Đây là hướng mở rộng đã được tài liệu hóa."

---

# PHẦN III — STRUCTURAL PATTERNS (Nhóm cấu trúc — 10 phút)

---

## 6. Adapter — bao bọc SDK PayPal (1 phút)

### Chức năng trên web

Chuyển đổi API của SDK PayPal thành `IPaymentGateway` nội bộ mà hệ thống có thể dùng.

### File cần mở

`Data/Strategy/PaymentStrategy.cs` — phần stub `PayPalPaymentStrategy`.

### Code minh họa khi tích hợp thật

```csharp
public class PayPalAdapter : IPaymentGateway
{
    public async Task<PaymentResult> ProcessPaymentAsync(
        double amount, string currency, String orderId)
    {
        // Tạo request theo định dạng PayPal SDK
        var request = new OrdersCreateRequest();
        request.RequestBody(new OrderRequest
        {
            Intent = "CAPTURE",
            PurchaseUnits = new[]
            {
                new PurchaseUnitRequest
                {
                    AmountWithBreakdown = new AmountWithBreakdown
                    {
                        CurrencyCode = currency,
                        Value = amount.ToString("F2")
                    }
                }
            }
        });

        var response = await paypalClient.Execute(request);
        return new PaymentResult
        {
            Success = response.Result<PayPalOrder>().Status == "COMPLETED",
            TransactionId = response.Result<PayPalOrder>().Id
        };
    }
}
```

### Giải thích

> "Adapter chuyển interface PayPal SDK sang interface mà hệ thống nội bộ mong đợi. Trong project hiện tại, `PayPalPaymentStrategy` là stub dùng `Task.Delay`. Khi tích hợp thật, cần `PayPalAdapter` bao bọc SDK để giữ nguyên interface `IPaymentStrategy`."

---

## 7. Bridge — tính giá theo loại ghế (2 phút 30 giây)

### Chức năng trên web

1. Chọn rạp/phòng và suất chiếu.
2. Chọn ghế Standard, VIP, Couple hoặc Disabled.
3. Quan sát giá ghế thay đổi theo loại.
4. Có thể mở endpoint: `Orders/GetSeatsForShowtime?showtimeId=<id>`

### File cần mở

- `Controllers/OrdersController.cs`
- `Models/Bridge/SeatPricingBridge.cs`

### Code trong `OrdersController.cs`

```csharp
price = new SeatPricingBridge(s.SeatType).GetPrice(showtime.Price),
isAvailable = s.IsAvailable
    && !bookedSeats.Contains(s.Row + s.Number.ToString())
```

### Code trong `SeatPricingBridge.cs`

```csharp
public interface ISeatingPricingStrategy
{
    double CalculatePrice(double basePrice);
    string SeatTypeName { get; }
}

public class StandardPricingStrategy : ISeatingPricingStrategy
{
    public double CalculatePrice(double basePrice) => basePrice;
    public string SeatTypeName => "Standard";
}

public class VipPricingStrategy : ISeatingPricingStrategy
{
    public double CalculatePrice(double basePrice) => basePrice * 1.2;
    public string SeatTypeName => "VIP";
}

public class CouplePricingStrategy : ISeatingPricingStrategy
{
    public double CalculatePrice(double basePrice) => basePrice * 2.0;
    public string SeatTypeName => "Couple";
}

public class DisabledPricingStrategy : ISeatingPricingStrategy
{
    public double CalculatePrice(double basePrice) => basePrice * 0.5;
    public string SeatTypeName => "Khuyết tật";
}

public class SeatPricingBridge
{
    private readonly ISeatingPricingStrategy _strategy;

    public SeatPricingBridge(SeatType seatType)
    {
        _strategy = seatType switch
        {
            SeatType.VIP      => new VipPricingStrategy(),
            SeatType.Couple   => new CouplePricingStrategy(),
            SeatType.Disabled => new DisabledPricingStrategy(),
            _                 => new StandardPricingStrategy()
        };
    }

    public double GetPrice(double basePrice)
        => _strategy.CalculatePrice(basePrice);
}
```

### Bảng minh họa

| Loại ghế | Implementation | Giá minh họa (cơ bản 100.000đ) |
|---|---|---:|
| Standard | `basePrice` | 100.000 |
| VIP | `basePrice * 1.2` | 120.000 |
| Couple | `basePrice * 2.0` | 200.000 |
| Disabled | `basePrice * 0.5` | 50.000 |

### Giải thích

> "Tại đây có hai phần có thể thay đổi độc lập. Một phần là cách hệ thống yêu cầu tính giá, thể hiện qua `SeatPricingBridge`. Phần còn lại là quy tắc giá của từng loại ghế, thể hiện qua `ISeatingPricingStrategy` và các implementation.
>
> Bridge tách Abstraction (`SeatPricingBridge`) khỏi Implementation (`Standard/Vip/Couple/Disabled`). Nếu thêm loại ghế Recliner, chỉ cần thêm `ReclinerPricingStrategy` và bổ sung mapping. Không cần rải thêm nhiều điều kiện tính giá trong Controller. Phần hiển thị/chọn ghế và phần tính giá có thể thay đổi độc lập."

---

## 8. Composite — ghế theo hàng và phòng (1 phút 30 giây)

### Chức năng trên web

Quản lý ghế theo cấu trúc cây: một phòng chứa nhiều hàng, mỗi hàng chứa nhiều ghế.

### File cần mở

- `Controllers/OrdersController.cs` — phần `GroupBy(s => s.Row)`
- `DESIGN_PATTERNS_GUIDE.md` mục 3.6

### Code minh họa

```csharp
public interface ITheaterComponent
{
    string Code { get; }
    double GetPrice(double basePrice);
    bool IsAvailable(List<string> bookedSeatCodes);
    int CountSeats();
}

public class Seat : ITheaterComponent
{
    public string Code => $"{Row}{Number}";
    public string Row { get; set; }
    public int Number { get; set; }
    public SeatType SeatType { get; set; }

    public double GetPrice(double basePrice) => SeatType switch
    {
        SeatType.VIP      => basePrice * 1.2,
        SeatType.Couple   => basePrice * 2.0,
        SeatType.Disabled => basePrice * 0.5,
        _                 => basePrice
    };

    public bool IsAvailable(List<string> bookedSeatCodes)
        => IsAvailableSeat && !bookedSeatCodes.Contains(Code);

    public int CountSeats() => 1;
}

public class SeatRow : ITheaterComponent
{
    public string Row { get; set; }
    public List<Seat> Seats { get; set; } = new();

    public string Code => $"Row {Row}";

    public double GetPrice(double basePrice)
        => Seats.Sum(s => s.GetPrice(basePrice));

    public bool IsAvailable(List<string> bookedSeatCodes)
        => Seats.All(s => s.IsAvailable(bookedSeatCodes));

    public int CountSeats() => Seats.Count;

    public IEnumerable<Seat> GetAvailableSeats(List<string> bookedSeatCodes)
        => Seats.Where(s => s.IsAvailable(bookedSeatCodes));
}

public class CinemaRoomComposite : ITheaterComponent
{
    public string Name { get; set; }
    public List<SeatRow> Rows { get; set; } = new();

    public string Code => Name;

    public double GetPrice(double basePrice)
        => Rows.Sum(r => r.GetPrice(basePrice));

    public bool IsFull(List<string> bookedSeatCodes)
        => !Rows.SelectMany(r => r.GetAvailableSeats(bookedSeatCodes)).Any();

    public int CountSeats() => Rows.Sum(r => r.CountSeats());
}
```

### Giải thích

> "Composite cho phép xử lý đối tượng đơn lẻ (`Seat`) và tổng hợp (`SeatRow`, `CinemaRoomComposite`) qua cùng interface. Tính giá cả phòng hoặc lấy ghế trống đều dùng chung interface `ITheaterComponent`. Trong Controller hiện tại, `GroupBy(s => s.Row)` là cách tiếp cận tương tự Composite."

---

## 9. Decorator — xếp chồng giảm giá (2 phút 30 giây)

### Chức năng trên web

Tính giá đơn hàng với nhiều loại khuyến mãi xếp chồng: voucher, điểm tích lũy, Happy Hour.

### File cần mở

`Data/Decorators/PricingDecorators.cs`

### Code

```csharp
public interface IOrderPriceDecorator
{
    double CalculatePrice(double currentPrice);
    string Description { get; }
    int Priority { get; }
}

public class BasePriceCalculator : IOrderPriceDecorator
{
    private readonly double _basePrice;
    public BasePriceCalculator(double basePrice) => _basePrice = basePrice;
    public double CalculatePrice(double currentPrice) => _basePrice;
    public string Description => "Giá gốc";
    public int Priority => 0;
}

public class VoucherDecorator : IOrderPriceDecorator
{
    private readonly IOrderPriceDecorator _inner;
    private readonly Voucher _voucher;

    public VoucherDecorator(IOrderPriceDecorator inner, Voucher voucher)
    {
        _inner = inner;
        _voucher = voucher;
    }

    public double CalculatePrice(double currentPrice)
    {
        double discounted = _inner.CalculatePrice(currentPrice);
        if (discounted < _voucher.MinOrderAmount)
            return discounted;

        double reduction = _voucher.IsPercentage
            ? discounted * _voucher.DiscountPercentage / 100.0
            : _voucher.DiscountAmount;

        return Math.Max(0, discounted - Math.Min(reduction, discounted));
    }

    public string Description => _voucher.IsPercentage
        ? $"Voucher giảm {_voucher.DiscountPercentage}% (-{_voucher.Code})"
        : $"Voucher giảm {_voucher.DiscountAmount:N0}đ (-{_voucher.Code})";

    public int Priority => 1;
}

public class LoyaltyPointsDecorator : IOrderPriceDecorator
{
    private readonly IOrderPriceDecorator _inner;
    private readonly int _points;

    public LoyaltyPointsDecorator(IOrderPriceDecorator inner, int points)
    {
        _inner = inner;
        _points = points;
    }

    public double CalculatePrice(double currentPrice)
    {
        double afterVoucher = _inner.CalculatePrice(currentPrice);
        double pointValue = _points * 1000.0;
        return Math.Max(0, afterVoucher - pointValue);
    }

    public string Description
        => $"Điểm tích lũy (-{_points * 1000:N0}đ = {_points} điểm)";

    public int Priority => 2;
}

public class HappyHourDecorator : IOrderPriceDecorator
{
    private readonly IOrderPriceDecorator _inner;
    private readonly TimeSpan _start;
    private readonly TimeSpan _end;
    private readonly double _discountPercent;

    public HappyHourDecorator(
        IOrderPriceDecorator inner,
        TimeSpan start, TimeSpan end, double discountPercent)
    {
        _inner = inner;
        _start = start;
        _end = end;
        _discountPercent = discountPercent;
    }

    public double CalculatePrice(double currentPrice)
    {
        var now = DateTime.Now.TimeOfDay;
        if (now >= _start && now <= _end)
        {
            double basePrice = _inner.CalculatePrice(currentPrice);
            return basePrice * (1 - _discountPercent / 100.0);
        }
        return _inner.CalculatePrice(currentPrice);
    }

    public string Description
        => $"Happy Hour {_discountPercent}% (từ {_start:hh\\:mm}–{_end:hh\\:mm})";

    public int Priority => 3;
}
```

### Cách xếp lớp trong `OrderPriceCalculator`

```csharp
IOrderPriceDecorator calc = new BasePriceCalculator(basePrice);

if (vouner != null)
    calc = new VoucherDecorator(calc, voucher);

if (loyaltyPoints > 0)
    calc = new LoyaltyPointsDecorator(calc, loyaltyPoints);

if (applyHappyHour)
    calc = new HappyHourDecorator(calc,
        new TimeSpan(14, 0, 0),
        new TimeSpan(17, 0, 0),
        15.0);

double finalPrice = calc.CalculatePrice(basePrice);
```

### Sơ đồ xếp lớp

```text
BasePrice
   -> VoucherDecorator
      -> LoyaltyPointsDecorator
         -> HappyHourDecorator
```

### Giải thích

> "Mỗi decorator bọc calculator phía trước và thêm một lớp giảm giá. Không cần viết `PriceWithVoucherAndPointsAndHappyHour` cho từng tổ hợp. Các chính sách giảm giá có thể xếp chồng linh hoạt. Có thể thêm `FestivalDiscountDecorator` mà không sửa calculator gốc.
>
> Đây là Decorator Pattern: gắn thêm trách nhiệm bổ sung vào đối tượng một cách linh hoạt, thay vì dùng kế thừa."

---

## 10. Facade — đặt vé qua một cổng duy nhất (2 phút 30 giây)

### Chức năng trên web

1. Chọn 1–2 ghế trống.
2. Nhập tên và email.
3. Nhập voucher nếu có.
4. Chọn phương thức thanh toán.
5. Nhấn nút đặt vé.
6. Quan sát màn hình đặt vé thành công và tổng tiền.

### File cần mở

- `Controllers/OrdersController.cs`
- `Data/Facade/BookingFacade.cs`

### Code trong `OrdersController.cs` — action BookTickets POST

```csharp
[HttpPost]
public async Task<IActionResult> BookTickets(BookTicketsVM model)
{
    if (!ModelState.IsValid)
    {
        TempData["BookingError"] =
            "Vui lòng nhập đầy đủ thông tin đặt vé hợp lệ.";
        return RedirectToAction(
            nameof(BookTickets),
            new { showtimeId = model.ShowtimeId });
    }

    var result = await _bookingFacade
        .ProcessBookingAsync(model, User.Identity?.Name);

    if (!result.Success)
    {
        TempData["BookingError"] = result.Message;
        return RedirectToAction(
            nameof(BookTickets),
            new { showtimeId = model.ShowtimeId });
    }

    return View("BookingCompleted");
}
```

### Code trong `BookingFacade.cs` — 9 bước xử lý

```csharp
public async Task<BookingResult> ProcessBookingAsync(
    BookTicketsVM model, string? userId)
{
    // 1. Validate ModelState
    if (string.IsNullOrEmpty(model.SelectedSeats))
        return new BookingResult { Success = false,
            Message = "Vui lòng chọn ít nhất một ghế." };

    // 2. Lấy Showtime
    var showtime = await _showtimesService
        .GetShowtimeByIdWithDetailsAsync(model.ShowtimeId);
    if (showtime == null)
        return new BookingResult { Success = false,
            Message = "Suất chiếu không tồn tại." };

    // 3. Parse ghế
    var selectedSeats = model.SelectedSeats
        .Split(',').Select(s => s.Trim())
        .Where(s => !string.IsNullOrEmpty(s)).ToList();

    // 4. Kiểm tra ghế đã bị đặt chưa
    var bookedSeats = await _ordersService
        .GetBookedSeatsForShowtimeAsync(model.ShowtimeId);
    foreach (var seat in selectedSeats)
        if (bookedSeats.Contains(seat))
            return new BookingResult { Success = false,
                Message = $"Ghế {seat} đã được đặt bởi người khác." };

    // 5. Tính giá theo loại ghế (Bridge)
    var roomSeats = await _seatsService
        .GetSeatsByRoomAsync(showtime.CinemaRoomId);
    double totalPrice = 0;
    foreach (var seatCode in selectedSeats)
    {
        var seat = roomSeats.FirstOrDefault(
            s => s.Row + s.Number.ToString() == seatCode);
        var bridge = new SeatPricingBridge(
            seat?.SeatType ?? SeatType.Standard);
        totalPrice += bridge.GetPrice(showtime.Price);
    }

    // 6. Áp dụng voucher
    double discount = 0;
    if (!string.IsNullOrEmpty(model.VoucherCode))
    {
        var voucher = await _ordersService
            .GetVoucherByCodeAsync(model.VoucherCode);
        if (voucher != null && totalPrice >= voucher.MinOrderAmount)
        {
            discount = voucher.IsPercentage
                ? totalPrice * voucher.DiscountPercentage / 100.0
                : voucher.DiscountAmount;
        }
    }

    // 7. Thanh toán (Strategy)
    var paymentCtx = new PaymentContext();
    paymentCtx.SetStrategyByName(model.PaymentMethod);
    var paymentResult = await paymentCtx.PayAsync(
        totalPrice, $"ORDER-{DateTime.Now.Ticks}");
    if (!paymentResult.Success)
        return new BookingResult { Success = false,
            Message = $"Thanh toán thất bại: {paymentResult.Message}" };

    // 8. Tạo Order (Builder)
    var order = new OrderBuilder()
        .SetCustomer(model.Name ?? "Guest", model.Email ?? "", userId ?? "")
        .SetShowtime(model.ShowtimeId, model.SelectedSeats,
                     selectedSeats.Count, showtime.Price)
        .ApplyVoucher(discount, totalPrice)
        .RedeemPoints(model.PointsRedeemed, totalPrice - discount)
        .SetPaymentMethod(paymentCtx.CurrentPaymentMethod)
        .CalculateTotal()
        .Build();

    // 9. Lưu vào DB
    await _ordersService.StoreDirectOrderAsync(
        model.ShowtimeId, model.Name ?? "Guest", model.Email ?? "",
        model.SelectedSeats, selectedSeats.Count,
        totalPrice, discount, model.PointsRedeemed,
        paymentCtx.CurrentPaymentMethod, userId);

    return new BookingResult
    {
        Success = true,
        Message = "Đặt vé thành công!",
        FinalPrice = totalPrice - discount - (model.PointsRedeemed * 1000),
        DiscountApplied = discount
    };
}
```

### Sơ đồ gọi từ Controller

```text
OrdersController
      |
      v
IBookingFacade.ProcessBookingAsync
      |
      +-- IShowtimesService
      +-- ISeatsService
      +-- IOrdersService
      +-- SeatPricingBridge  (Bridge)
      +-- PaymentContext      (Strategy)
      +-- OrderBuilder        (Builder)
      +-- Lưu Order vào database
```

### Giải thích

> "Nếu viết trực tiếp trong Controller, action đặt vé sẽ phải tự lấy suất chiếu, kiểm tra ghế, tính giá, kiểm tra voucher, gọi thanh toán, tạo Order và lưu database. Điều đó làm Controller rất dài và khó test.
>
> `BookingFacade` cung cấp một API đơn giản là `ProcessBookingAsync`. Bên trong Facade điều phối nhiều subsystem: `IShowtimesService`, `ISeatsService`, `IOrdersService`, Bridge tính giá, Strategy thanh toán và Builder tạo Order. Controller chỉ kiểm tra request, gọi Facade và trả View.
>
> Đây là Facade Pattern: che giấu sự phức tạp bên trong và cung cấp một cổng sử dụng đơn giản cho client."

---

## 11. Proxy — cache danh sách phim (1 phút 30 giây)

### Chức năng trên web

1. Mở trang chủ — danh sách phim hiển thị.
2. Refresh trang.
3. Danh sách phim được đọc từ database lần đầu, các lần sau có thể lấy từ cache.

### File cần mở

- `Controllers/MoviesController.cs`
- `Data/Proxy/CachedMoviesServiceProxy.cs`
- `Program.cs`

### Code trong `MoviesController.cs`

```csharp
private readonly IMoviesService _service;

public MoviesController(
    IMoviesService service,
    IWebHostEnvironment webHostEnvironment,
    IShowtimesService showtimesService)
{
    _service = service;
    _webHostEnvironment = webHostEnvironment;
    _showtimesService = showtimesService;
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

### Code trong `Program.cs` — đăng ký Proxy

```csharp
builder.Services.AddScoped<MoviesService>();

builder.Services.AddScoped<IMoviesService>(sp =>
{
    var realService = sp.GetRequiredService<MoviesService>();
    var cache = sp.GetRequiredService<IMemoryCache>();
    return new CachedMoviesServiceProxy(realService, cache);
});
```

### Code trong `CachedMoviesServiceProxy.cs`

```csharp
public async Task<IEnumerable<Movie>> GetAllAsync()
{
    return await _cache.GetOrCreateAsync("movies:all", async entry =>
    {
        entry.SlidingExpiration = DefaultExpiry;
        return await _realService.GetAllAsync();
    }) ?? Enumerable.Empty<Movie>();
}

public async Task<Movie> GetByIdAsync(int id)
{
    string key = $"movies:id:{id}";
    return await _cache.GetOrCreateAsync(key, async entry =>
    {
        entry.SlidingExpiration = DefaultExpiry;
        return await _realService.GetByIdAsync(id);
    }) ?? null!;
}

public async Task AddAsync(Movie entity)
{
    await _realService.AddAsync(entity);
    InvalidateAllCaches();
}

public async Task UpdateAsync(int id, Movie entity)
{
    await _realService.UpdateAsync(id, entity);
    InvalidateAllCaches();
}
```

### Giải thích

> "Tại đây `MoviesController` chỉ biết `IMoviesService`. Đối tượng thực tế được DI cấp vào lại là `CachedMoviesServiceProxy`, không phải `MoviesService` trực tiếp. Proxy có cùng interface với service thật, nhưng đứng phía trước để thêm cơ chế cache.
>
> Lần đầu gọi `GetAllAsync`, proxy gọi `_realService.GetAllAsync()` rồi lưu kết quả vào `IMemoryCache` với key `movies:all`. Những lần đọc tiếp theo có thể lấy dữ liệu từ cache, giảm truy vấn database. Các thao tác thêm, sửa, xóa vẫn được chuyển tiếp cho service thật và gọi `InvalidateAllCaches()`.
>
> Đây là Proxy Pattern: một đối tượng đại diện cho đối tượng thật và kiểm soát cách client truy cập đối tượng đó."

---

# PHẦN IV — BEHAVIORAL PATTERNS (Nhóm hành vi — 11 phút)

---

## 12. Chain of Responsibility — kiểm tra dữ liệu đặt vé (2 phút 30 giây)

### Chức năng trên web

Kiểm tra nhiều bước: Validation → Kiểm tra ghế → Kiểm tra voucher → Kiểm tra thành viên. Sai ở đâu thì dừng ở handler đó.

### File cần mở

`Data/Chain/OrderPipeline.cs`

### Code — Ghép chuỗi

```csharp
public static class OrderPipelineBuilder
{
    public static OrderPipelineHandler Build(IOrdersService ordersService)
    {
        var validation = new ValidationHandler();
        var seats = new SeatAvailabilityHandler(ordersService);
        var voucher = new VoucherValidationHandler(ordersService);
        var member = new MemberValidationHandler(ordersService);

        validation.SetNext(seats).SetNext(voucher).SetNext(member);
        return validation;
    }
}
```

### Code — ValidationHandler

```csharp
public class ValidationHandler : OrderPipelineHandler
{
    public override async Task<OrderPipelineResult> HandleAsync(
        OrderPipelineRequest request,
        OrderPipelineResult result)
    {
        if (request.Model.ShowtimeId <= 0)
        {
            result.IsValid = false;
            result.Message = "Suất chiếu không hợp lệ.";
            return result;
        }

        if (string.IsNullOrEmpty(request.Model.SelectedSeats))
        {
            result.IsValid = false;
            result.Message = "Vui lòng chọn ít nhất một ghế.";
            return result;
        }

        var seats = request.Model.SelectedSeats
            .Split(',').Select(s => s.Trim())
            .Where(s => !string.IsNullOrEmpty(s)).ToList();

        if (!seats.Any())
        {
            result.IsValid = false;
            result.Message = "Danh sách ghế trống.";
            return result;
        }

        if (seats.Count > 10)
        {
            result.IsValid = false;
            result.Message = "Không thể đặt quá 10 ghế mỗi lần.";
            return result;
        }

        return _next != null
            ? await _next.HandleAsync(request, result)
            : result;
    }
}
```

### Code — SeatAvailabilityHandler

```csharp
public class SeatAvailabilityHandler : OrderPipelineHandler
{
    private readonly IOrdersService _ordersService;

    public SeatAvailabilityHandler(IOrdersService ordersService)
        => _ordersService = ordersService;

    public override async Task<OrderPipelineResult> HandleAsync(
        OrderPipelineRequest request,
        OrderPipelineResult result)
    {
        if (!result.IsValid) return result;

        var bookedSeats = await _ordersService
            .GetBookedSeatsForShowtimeAsync(request.Model.ShowtimeId);

        var selectedSeats = request.Model.SelectedSeats
            .Split(',').Select(s => s.Trim())
            .Where(s => !string.IsNullOrEmpty(s)).ToList();

        foreach (var seat in selectedSeats)
        {
            if (bookedSeats.Contains(seat))
            {
                result.IsValid = false;
                result.Message =
                    $"Ghế {seat} đã được đặt bởi người khác. Vui lòng chọn ghế khác.";
                return result;
            }
        }

        return _next != null
            ? await _next.HandleAsync(request, result)
            : result;
    }
}
```

### Code — VoucherValidationHandler

```csharp
public class VoucherValidationHandler : OrderPipelineHandler
{
    private readonly IOrdersService _ordersService;

    public VoucherValidationHandler(IOrdersService ordersService)
        => _ordersService = ordersService;

    public override async Task<OrderPipelineResult> HandleAsync(
        OrderPipelineRequest request,
        OrderPipelineResult result)
    {
        if (!result.IsValid) return result;

        if (!string.IsNullOrEmpty(request.Model.VoucherCode))
        {
            var voucher = await _ordersService
                .GetVoucherByCodeAsync(request.Model.VoucherCode);

            if (voucher == null)
            {
                result.IsValid = false;
                result.Message = "Mã voucher không tồn tại.";
                return result;
            }

            if (!voucher.IsActive)
            {
                result.IsValid = false;
                result.Message = "Mã voucher đã bị vô hiệu hóa.";
                return result;
            }

            if (voucher.ExpiryDate < DateTime.Now)
            {
                result.IsValid = false;
                result.Message = "Mã voucher đã hết hạn.";
                return result;
            }

            result.AppliedDiscounts.Add(
                voucher.IsPercentage
                    ? $"Voucher {voucher.Code}: {voucher.DiscountPercentage}% giảm"
                    : $"Voucher {voucher.Code}: {voucher.DiscountAmount:N0}đ giảm");
        }

        return _next != null
            ? await _next.HandleAsync(request, result)
            : result;
    }
}
```

### Chuỗi thực tế

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

### Demo lỗi có kiểm soát

- Submit form trống → handler `Validation` báo lỗi và dừng.
- Nhập voucher hết hạn → handler `VoucherValidation` dừng.
- Nhập hơn 10 ghế → `Validation` báo "Không thể đặt quá 10 ghế mỗi lần."

### Giải thích

> "Các bước kiểm tra được xếp thành chuỗi: kiểm tra request, kiểm tra ghế, kiểm tra voucher và kiểm tra thành viên. Mỗi handler có thể xử lý lỗi rồi dừng ngay, hoặc chuyển request cho handler tiếp theo qua `_next`.
>
> Vì vậy, khi người dùng chưa chọn ghế, hệ thống không cần chạy các bước kiểm tra voucher và thành viên. Khi thêm quy tắc mới, chỉ cần thêm handler vào chuỗi mà không sửa toàn bộ Controller.
>
> Đây là Chain of Responsibility: request đi qua nhiều đối tượng xử lý tuần tự, và mỗi đối tượng quyết định xử lý hay chuyển tiếp."

---

## 13. Strategy — chọn phương thức thanh toán (1 phút 30 giây)

### Chức năng trên web

1. Ở form đặt vé, chọn `Cash` — thanh toán tại rạp.
2. Đổi sang `PayPal` nếu giao diện có lựa chọn.
3. Quan sát kết quả giao dịch thành công.

### File cần mở

`Data/Strategy/PaymentStrategy.cs`

### Code

```csharp
public interface IPaymentStrategy
{
    string Name { get; }
    string PaymentMethod { get; }
    Task<PaymentResult> PayAsync(double amount, string orderId);
    Task<RefundResult> RefundAsync(string transactionId, double amount);
}

public class CashPaymentStrategy : IPaymentStrategy
{
    public string Name => "Thanh toán tại rạp";
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

public class PayPalPaymentStrategy : IPaymentStrategy
{
    public string Name => "PayPal";
    public string PaymentMethod => "PayPal";

    public async Task<PaymentResult> PayAsync(double amount, string orderId)
    {
        await Task.Delay(100); // mô phỏng API call
        return new PaymentResult
        {
            Success = true,
            TransactionId = $"PP-{orderId}-{DateTime.Now.Ticks}",
            Message = "Thanh toán PayPal thành công."
        };
    }
}

public class PaymentContext
{
    private IPaymentStrategy? _strategy;

    public void SetStrategy(IPaymentStrategy strategy)
        => _strategy = strategy;

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
            throw new InvalidOperationException(
                "Payment strategy not set.");
        return await _strategy.PayAsync(amount, orderId);
    }

    public string CurrentPaymentMethod
        => _strategy?.PaymentMethod ?? "Unknown";
}
```

### Gọi từ `BookingFacade.cs`

```csharp
var paymentCtx = new PaymentContext();
paymentCtx.SetStrategyByName(model.PaymentMethod);
var paymentResult = await paymentCtx.PayAsync(
    totalPrice, $"ORDER-{DateTime.Now.Ticks}");
```

### Giải thích

> "Cash và PayPal đều có cùng interface `IPaymentStrategy`, nhưng bên trong có thuật toán thanh toán khác nhau. `PaymentContext` giữ strategy hiện tại và ủy quyền thao tác `PayAsync`.
>
> Code đặt Order không cần biết chi tiết Cash hay PayPal. Khi người dùng đổi lựa chọn, Context đổi implementation tại runtime. Nếu thêm MoMo hoặc VNPay, chỉ cần thêm class triển khai interface, sau đó bổ sung cách chọn strategy.
>
> Đây là Strategy Pattern. Lưu ý: `PayPalPaymentStrategy` hiện tại là stub dùng `Task.Delay`, chưa gọi API thật."

---

## 14. State — vòng đời Order (2 phút)

### Chức năng trên web

1. Đăng nhập tài khoản Admin.
2. Mở trang quản lý booking.
3. Tìm order vừa tạo.
4. Bấm **Confirm** — trạng thái chuyển từ `Purchased` sang `Confirmed`.
5. Thu **Cancel** hoặc **Refund** nếu có dữ liệu test.

### File cần mở

`Data/State/OrderStateMachine.cs`

### Code

```csharp
public interface IOrderState
{
    string StatusName { get; }
    bool CanTransitionTo(string newStatus);
    Task OnEnterAsync(Order order, AppDbContext context);
}

public class PurchasedState : IOrderState
{
    public string StatusName => "Purchased";

    public bool CanTransitionTo(string newStatus)
        => newStatus is "Confirmed" or "Cancelled";

    public Task OnEnterAsync(Order order, AppDbContext context)
    {
        return Task.CompletedTask;
    }
}

public class ConfirmedState : IOrderState
{
    public string StatusName => "Confirmed";

    public bool CanTransitionTo(string newStatus)
        => newStatus is "Cancelled" or "Refunded";

    public Task OnEnterAsync(Order order, AppDbContext context)
    {
        return Task.CompletedTask;
    }
}

public class CancelledState : IOrderState
{
    public string StatusName => "Cancelled";

    public bool CanTransitionTo(string newStatus) => false;

    public async Task OnEnterAsync(Order order, AppDbContext context)
    {
        if (!string.IsNullOrEmpty(order.Email))
        {
            var member = await context.Members
                .FirstOrDefaultAsync(
                    m => m.Email.ToLower() == order.Email.ToLower());
            if (member != null)
            {
                double finalPrice = Math.Max(0,
                    order.TotalPrice - order.DiscountAmount);
                int earned = (int)(finalPrice / 10000);
                member.Points = Math.Max(0,
                    member.Points - earned
                    + (order.PointsRedeemed / 1000));
            }
            await context.SaveChangesAsync();
        }
    }
}

public class RefundedState : IOrderState
{
    public string StatusName => "Refunded";

    public bool CanTransitionTo(string newStatus) => false;

    public async Task OnEnterAsync(Order order, AppDbContext context)
    {
        if (!string.IsNullOrEmpty(order.Email))
        {
            var member = await context.Members
                .FirstOrDefaultAsync(
                    m => m.Email.ToLower() == order.Email.ToLower());
            if (member != null)
            {
                double finalPrice = Math.Max(0,
                    order.TotalPrice - order.DiscountAmount);
                int earned = (int)(finalPrice / 10000);
                member.Points = Math.Max(0,
                    member.Points - earned
                    + (order.PointsRedeemed / 1000));
            }
            await context.SaveChangesAsync();
        }
    }
}

public class OrderStateMachine
{
    private static readonly Dictionary<string, IOrderState> _states = new()
    {
        ["Purchased"] = new PurchasedState(),
        ["Confirmed"] = new ConfirmedState(),
        ["Cancelled"] = new CancelledState(),
        ["Refunded"]  = new RefundedState(),
    };

    public static IOrderState? GetState(string statusName)
        => _states.GetValueOrDefault(statusName);

    public static bool IsValidStatus(string statusName)
        => _states.ContainsKey(statusName);

    public static bool CanTransition(string from, string to)
    {
        if (!_states.TryGetValue(from, out var state))
            return false;
        return state.CanTransitionTo(to);
    }
}
```

### Sơ đồ vòng đời

```text
Purchased  --->  Confirmed  --->  Refunded
    |                 |
    +---------------> Cancelled
```

### Giải thích

> "Đơn hàng có vòng đời. Từ `Purchased` có thể sang `Confirmed` hoặc `Cancelled`; từ `Confirmed` có thể sang `Cancelled` hoặc `Refunded`. Trạng thái `Cancelled` và `Refunded` là trạng thái kết thúc (terminal).
>
> Mỗi state tự mô tả các transition hợp lệ qua `CanTransitionTo`. Vì vậy hệ thống không cho phép một order đã hủy quay ngược lại thành Confirmed. Quy tắc trạng thái được tập trung thay vì rải nhiều `if/else` trong Controller.
>
> Khi gọi `OnEnterAsync`, mỗi state thực hiện hành động phụ: `CancelledState` giải phóng điểm tích lũy, `RefundedState` hoàn điểm. Nếu thêm trạng thái mới, chỉ cần thêm class implement `IOrderState`."

### Lưu ý thực hiện

> "Một số action quản trị hiện tại gọi `ChangeOrderStatusAsync` trực tiếp. Method `ChangeOrderStatusWithStateAsync` trong `OrdersService` dùng `OrderStateMachine.CanTransition` để kiểm tra trước khi cập nhật DB."

---

## 15. Observer — thông báo khi Order đổi trạng thái (2 phút)

### Chức năng trên web

Khi Admin Confirm, Cancel hoặc Refund, hệ thống tự động:
- Ghi log (audit)
- Cộng/trừ điểm thành viên
- Mô phỏng gửi email

### File cần mở

- `Data/Observer/OrderObserver.cs`
- `Program.cs`

### Code — Interface

```csharp
public interface IOrderObserver
{
    Task OnOrderStatusChangedAsync(Order order,
        string oldStatus, string newStatus);
}

public interface IOrderSubject
{
    void Attach(IOrderObserver observer);
    void Detach(IOrderObserver observer);
    Task NotifyAsync(Order order,
        string oldStatus, string newStatus);
}
```

### Code — AuditLogObserver

```csharp
public class AuditLogObserver : IOrderObserver
{
    private readonly ILogger<AuditLogObserver> _logger;

    public AuditLogObserver(ILogger<AuditLogObserver> logger)
    {
        _logger = logger;
    }

    public Task OnOrderStatusChangedAsync(Order order,
        string oldStatus, string newStatus)
    {
        _logger.LogInformation(
            "[AUDIT] Order #{OrderId} | Email: {Email} "
            + "| {Old} → {New} | Total: {Total:N0}VND "
            + "| Date: {Date}",
            order.Id,
            string.IsNullOrEmpty(order.Email)
                ? "(guest)" : order.Email,
            oldStatus, newStatus,
            order.TotalPrice - order.DiscountAmount,
            order.OrderDate);
        return Task.CompletedTask;
    }
}
```

### Code — LoyaltyPointsObserver

```csharp
public class LoyaltyPointsObserver : IOrderObserver
{
    private readonly IServiceScopeFactory _scopeFactory;

    public LoyaltyPointsObserver(
        IServiceScopeFactory scopeFactory)
    {
        _scopeFactory = scopeFactory;
    }

    public async Task OnOrderStatusChangedAsync(
        Order order, string oldStatus, string newStatus)
    {
        if (string.IsNullOrEmpty(order.Email)) return;

        using var scope = _scopeFactory.CreateScope();
        var context = scope.ServiceProvider
            .GetRequiredService<AppDbContext>();

        var member = await context.Members
            .FirstOrDefaultAsync(
                m => m.Email.ToLower() == order.Email.ToLower());
        if (member == null) return;

        double finalPrice = Math.Max(0,
            order.TotalPrice - order.DiscountAmount);
        int earned = (int)(finalPrice / 10000);

        if (newStatus == "Cancelled" || newStatus == "Refunded")
        {
            member.Points = Math.Max(0,
                member.Points - earned
                + (order.PointsRedeemed / 1000));
        }
        else if (newStatus == "Confirmed"
            && oldStatus == "Purchased")
        {
            member.Points += earned;
        }

        await context.SaveChangesAsync();
    }
}
```

### Code — EmailNotificationObserver

```csharp
public class EmailNotificationObserver : IOrderObserver
{
    private readonly ILogger<EmailNotificationObserver> _logger;

    public EmailNotificationObserver(
        ILogger<EmailNotificationObserver> logger)
    {
        _logger = logger;
    }

    public Task OnOrderStatusChangedAsync(Order order,
        string oldStatus, string newStatus)
    {
        if (string.IsNullOrEmpty(order.Email))
            return Task.CompletedTask;

        var (subject, body) = newStatus switch
        {
            "Confirmed" => (
                $"[MovieCinema] Xác nhận đơn hàng #{order.Id}",
                $"Đơn hàng #{order.Id} đã được xác nhận.\n"
                + $"Tổng cộng: {(order.TotalPrice - order.DiscountAmount):N0}VND"
            ),
            "Cancelled" => (
                $"[MovieCinema] Đơn hàng #{order.Id} đã bị hủy",
                $"Đơn hàng #{order.Id} đã được hủy."
            ),
            "Refunded" => (
                $"[MovieCinema] Hoàn tiền đơn hàng #{order.Id}",
                $"Đơn hàng #{order.Id} đã được hoàn tiền.\n"
                + $"Số tiền hoàn: {(order.TotalPrice - order.DiscountAmount):N0}VND"
            ),
            _ => (null as string, null as string)
        };

        if (subject != null)
        {
            _logger.LogInformation(
                "[EMAIL] To: {Email} | Subject: {Subject}",
                order.Email, subject);
        }

        return Task.CompletedTask;
    }
}
```

### Đăng ký DI

```csharp
builder.Services.AddSingleton<IOrderSubject, OrderSubject>();
builder.Services.AddScoped<IOrderObserver, AuditLogObserver>();
builder.Services.AddScoped<IOrderObserver, LoyaltyPointsObserver>();
builder.Services.AddScoped<IOrderObserver, EmailNotificationObserver>();
```

### Giải thích

> "Admin chỉ thực hiện một hành động là Confirm. Sau khi status thay đổi, Subject thông báo cho nhiều observer. Vì vậy muốn thêm SMS, lịch sử hoạt động hoặc cập nhật dashboard thì chỉ cần thêm một observer mới, không phải sửa logic nghiệp vụ Confirm chính.
>
> Observer xử lý độc lập; nếu một observer lỗi, `OrderSubject` bắt lỗi để các observer còn lại vẫn được thông báo. Đây là Observer Pattern: một subject phát sự kiện và nhiều observer phản ứng độc lập."

---

## 16. Mediator — định tuyến request và handler (2 phút)

### Chức năng trên web

Mediator điều phối các request đến handler tương ứng. `CompleteBookingHandler` chạy Chain trước rồi gọi Facade. `ConfirmBookingHandler` và `CancelBookingHandler` dùng `ChangeOrderStatusWithStateAsync`.

### File cần mở

`Data/Mediator/BookingMediator.cs`

### Code — Interface và Mediator

```csharp
public interface IMediator
{
    Task<TResponse> SendAsync<TResponse>(
        IRequest<TResponse> request);
}

public interface IRequest<TResponse> { }

public interface IRequestHandler<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    Task<TResponse> HandleAsync(TRequest request);
}

public class AppMediator : IMediator
{
    private readonly IServiceProvider _serviceProvider;

    public AppMediator(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public async Task<TResponse> SendAsync<TResponse>(
        IRequest<TResponse> request)
    {
        var handlerType = typeof(IRequestHandler<,>)
            .MakeGenericType(request.GetType(), typeof(TResponse));

        var handler = _serviceProvider
            .GetServices(handlerType).FirstOrDefault();

        if (handler == null)
            throw new InvalidOperationException(
                $"Handler not found for {request.GetType().Name}");

        var method = handlerType.GetMethod("HandleAsync");
        if (method == null)
            throw new InvalidOperationException(
                $"HandleAsync not found on {handlerType.Name}");

        var result = method.Invoke(handler, new[] { request });
        if (result is Task<TResponse> task)
            return await task;

        throw new InvalidOperationException(
            "Handler did not return Task");
    }
}
```

### Code — CompleteBookingHandler

```csharp
public class CompleteBookingHandler
    : IRequestHandler<CompleteBookingRequest, CompleteBookingResponse>
{
    private readonly IBookingFacade _facade;
    private readonly IOrdersService _ordersService;

    public CompleteBookingHandler(
        IBookingFacade facade,
        IOrdersService ordersService)
    {
        _facade = facade;
        _ordersService = ordersService;
    }

    public async Task<CompleteBookingResponse> HandleAsync(
        CompleteBookingRequest request)
    {
        // 1. Validate qua Chain of Responsibility
        var pipeline = OrderPipelineBuilder.Build(_ordersService);
        var pipelineResult = await pipeline.HandleAsync(
            new OrderPipelineRequest { Model = request.Model },
            new OrderPipelineResult { IsValid = true });

        if (!pipelineResult.IsValid)
        {
            return new CompleteBookingResponse
            {
                Success = false,
                Message = pipelineResult.Message
            };
        }

        // 2. Process booking qua Facade
        var bookingResult = await _facade
            .ProcessBookingAsync(request.Model, request.UserId);

        return new CompleteBookingResponse
        {
            Success = bookingResult.Success,
            Message = bookingResult.Message,
            OrderId = bookingResult.OrderId,
            FinalPrice = bookingResult.FinalPrice,
            AppliedDiscounts = pipelineResult.AppliedDiscounts
        };
    }
}
```

### Code — ConfirmBookingHandler

```csharp
public class ConfirmBookingHandler
    : IRequestHandler<ConfirmBookingRequest, ConfirmBookingResponse>
{
    private readonly IOrdersService _ordersService;

    public ConfirmBookingHandler(IOrdersService ordersService)
    {
        _ordersService = ordersService;
    }

    public async Task<ConfirmBookingResponse> HandleAsync(
        ConfirmBookingRequest request)
    {
        var result = await _ordersService
            .ChangeOrderStatusWithStateAsync(
                request.OrderId, "Confirmed");

        return new ConfirmBookingResponse
        {
            Success = result.Success,
            Message = result.Success
                ? "Xác nhận đơn hàng thành công."
                : result.Message
        };
    }
}
```

### Đăng ký DI

```csharp
builder.Services.AddScoped<IMediator, AppMediator>();
builder.Services.AddScoped<IRequestHandler<CompleteBookingRequest,
    CompleteBookingResponse>, CompleteBookingHandler>();
builder.Services.AddScoped<IRequestHandler<CancelBookingRequest,
    CancelBookingResponse>, CancelBookingHandler>();
builder.Services.AddScoped<IRequestHandler<ConfirmBookingRequest,
    ConfirmBookingResponse>, ConfirmBookingHandler>();
```

### Giải thích

> "Mediator giảm coupling: Controller không cần biết trực tiếp toàn bộ service nào phải gọi khi Confirm/Cancel. Nó gửi một request đến Mediator; handler chịu trách nhiệm quy trình tương ứng.
>
> Trong project, Mediator phối hợp với cả Chain và Facade: `CompleteBookingHandler` chạy Chain trước, nếu hợp lệ thì gọi Facade. `ConfirmBookingHandler` và `CancelBookingHandler` dùng `ChangeOrderStatusWithStateAsync` để kết nối với State Pattern.
>
> Đây là Mediator Pattern: encapsulates cách một tập hợp objects tương tác, giảm tham chiếu trực tiếp giữa các thành phần."

---

# PHẦN V — TỔNG KẾT (1 phút)

---

## 17. Bảng tổng kết

| # | Pattern | File chính | Chức năng trên web |
|---|---|---|---|
| 1 | MVC + DI + Repository | Controllers/, Data/Base/, Program.cs | Nền tảng toàn bộ |
| 2 | Singleton | Program.cs (`AddSingleton`) | `OrderSubject`, `ShoppingCart` |
| 3 | Factory Method | Program.cs (DI registration) | Tạo service từ DI container |
| 4 | Abstract Factory | Guide mục 2.4 | Chính sách theo cinema (mở rộng) |
| 5 | Builder | `Models/Builders/OrderBuilder.cs` | Tạo Order nhiều bước |
| 6 | Adapter | Guide mục 3.4 | Bao bọc SDK PayPal |
| 7 | Bridge | `Models/Bridge/SeatPricingBridge.cs` | Tính giá theo loại ghế |
| 8 | Composite | Guide mục 3.6 | Ghế theo hàng/phòng |
| 9 | Decorator | `Data/Decorators/PricingDecorators.cs` | Xếp chồng giảm giá |
| 10 | Facade | `Data/Facade/BookingFacade.cs` | Đặt vé qua một cổng |
| 11 | Proxy | `Data/Proxy/CachedMoviesServiceProxy.cs` | Cache danh sách phim |
| 12 | Chain of Responsibility | `Data/Chain/OrderPipeline.cs` | Kiểm tra ghế/voucher |
| 13 | Strategy | `Data/Strategy/PaymentStrategy.cs` | Chọn Cash/PayPal |
| 14 | State | `Data/State/OrderStateMachine.cs` | Vòng đời Order |
| 15 | Observer | `Data/Observer/OrderObserver.cs` | Log/điểm/email |
| 16 | Mediator | `Data/Mediator/BookingMediator.cs` | Định tuyến request |

### Kết luận mẫu

> "Qua luồng đặt vé và quản trị, mỗi pattern giải quyết một vấn đề cụ thể:
>
> - **Singleton** quản lý Observer và Session một cách duy nhất.
> - **Factory Method** tạo đối tượng service qua DI container.
> - **Builder** tạo Order nhiều thuộc tính theo từng bước dễ đọc.
> - **Bridge** giữ cho quy tắc giá ghế độc lập với phần hiển thị.
> - **Decorator** xếp chồng chính sách giảm giá linh hoạt.
> - **Facade** làm luồng đặt vé đơn giản với Controller.
> - **Proxy** giảm truy vấn khi đọc phim bằng cache.
> - **Chain of Responsibility** chia kiểm tra thành nhiều bước có thể mở rộng.
> - **Strategy** cho phép đổi phương thức thanh toán tại runtime.
> - **State** bảo vệ vòng đời đơn hàng, không cho chuyển trạng thái bất hợp lệ.
> - **Observer** giúp thêm email, log, điểm thành viên mà không sửa nghiệp vụ chính.
> - **Mediator** giảm liên kết giữa các thành phần, phối hợp Chain và Facade.
>
> Design pattern chỉ có ý nghĩa khi gắn với nhu cầu cụ thể. Kết quả là code dễ đọc hơn, dễ test hơn và thuận lợi mở rộng khi hệ thống thêm phương thức thanh toán, loại ghế, chương trình khuyến mãi hoặc kênh thông báo mới."

---

# PHẦN VI — PHÂN BỔ THỜI GIAN 30 PHÚT

| Phần | Nội dung | Thời lượng |
|---|---|---:|
| 1 | Mở đầu, kiến trúc, DI | 2 phút |
| 2 | Singleton | 1 phút |
| 3 | Factory Method | 1 phút |
| 4 | Builder | 2 phút |
| 5 | Abstract Factory | 1 phút |
| 6 | Adapter | 1 phút |
| 7 | Bridge | 2 phút 30 giây |
| 8 | Composite | 1 phút 30 giây |
| 9 | Decorator | 2 phút 30 giây |
| 10 | Facade | 2 phút 30 giây |
| 11 | Proxy | 1 phút 30 giây |
| 12 | Chain of Responsibility | 2 phút 30 giây |
| 13 | Strategy | 1 phút 30 giây |
| 14 | State | 2 phút |
| 15 | Observer | 2 phút |
| 16 | Mediator | 2 phút |
| 17 | Tổng kết | 1 phút |
| **Tổng** | | **30 phút** |

---

# PHẦN VII — CÂU HỎI PHẢN BIỆN THƯỜNG GẶP

### 1. Vì sao không viết tất cả vào `OrdersController`?

> "Controller chỉ nên tập trung nhận request và trả response. Nếu vừa kiểm tra ghế, tính tiền, gọi thanh toán, tạo Order và gửi thông báo trong một action, code sẽ khó đọc, khó test và khó thay đổi. Facade, service và các pattern giúp chia trách nhiệm đó."

### 2. Facade và Mediator khác nhau thế nào?

> "Facade gom nhiều thao tác của một subsystem thành một API nghiệp vụ — ở đây là toàn bộ quy trình booking. Mediator điều phối các request/handler, giúp Controller không tham chiếu trực tiếp nhiều thành phần xử lý. Hai pattern có thể phối hợp: Mediator handler chạy Chain trước rồi gọi Facade."

### 3. Bridge và Strategy có giống nhau không?

> "Cả hai đều dùng interface để tách thuật toán, nhưng mục đích khác nhau. Bridge tách hai chiều biến đổi độc lập: abstraction tính giá và implementation theo loại ghế. Strategy chủ yếu dùng để thay thế một thuật toán tại runtime, ví dụ Cash hoặc PayPal."

### 4. Nếu thêm MoMo thì sửa ở đâu?

> "Tạo `MoMoPaymentStrategy : IPaymentStrategy`, triển khai `PayAsync`/`RefundAsync`, rồi bổ sung cách chọn strategy và đăng ký cấu hình. Phần Controller/Builder không cần biết chi tiết API MoMo."

### 5. Nếu ghế đã bị người khác đặt ngay sau khi kiểm tra thì sao?

> "Pipeline kiểm tra giúp phát hiện ở bước nghiệp vụ, nhưng production vẫn cần transaction/unique constraint hoặc cơ chế giữ ghế có thời hạn để xử lý race condition giữa bước kiểm tra và bước lưu Order."

### 6. PayPal trong demo đã gọi API thật chưa?

> "Chưa. `PayPalPaymentStrategy` hiện mô phỏng API bằng `Task.Delay`; đây là điểm cần thay bằng SDK/API thật khi triển khai production, đồng thời bảo vệ secret trong configuration/secret manager."

### 7. Observer có gây side effect ẩn không?

> "Trong project, Observer ghi log, cập nhật điểm, gửi email — đều có thể coi là side effect. Tuy nhiên chúng được cô lập trong từng observer, và `OrderSubject` bắt lỗi để không lan truyền."
