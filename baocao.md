# BÁO CÁO ÁP DỤNG DESIGN PATTERN VÀO DỰ ÁN MOVIECINEMA

**Dự án:** Hệ thống đặt vé xem phim MovieCinema (.NET 8 ASP.NET Core MVC, EF Core, SQL Server)  
**Phạm vi:** 12 pattern đang có trong source code, gồm 1 Creational, 3 Structural và 8 Behavioral.  
**Lưu ý:** Các đoạn “trước khi áp dụng” là phiên bản mô phỏng từ logic hiện có trước khi tách pattern; các đoạn “sau khi áp dụng” bám theo source code thật của dự án.

## Danh sách pattern

| STT | Pattern | File triển khai |
|---:|---|---|
| 1 | Singleton | `Data/Cart/ShoppingCart.cs`, đăng ký DI trong `Program.cs` |
| 2 | Bridge | `Models/Bridge/SeatPricingBridge.cs` |
| 3 | Decorator | `Data/Decorators/PricingDecorators.cs` |
| 4 | Proxy | `Data/Proxy/CachedMoviesServiceProxy.cs` |
| 5 | Strategy | `Data/Strategy/PaymentStrategy.cs` |
| 6 | Adapter | `Data/Strategy/PaymentStrategy.cs` (kết hợp trong payment abstraction) |
| 7 | Facade | `Data/Facade/BookingFacade.cs` |
| 8 | Builder | `Models/Builders/OrderBuilder.cs` |
| 9 | State | `Data/State/OrderStateMachine.cs` |
| 10 | Observer | `Data/Observer/OrderObserver.cs` |
| 11 | Chain of Responsibility | `Data/Chain/OrderPipeline.cs` |
| 12 | Mediator | `Data/Mediator/BookingMediator.cs` |

---

# 1. Áp dụng mẫu Singleton Pattern

## Trước khi áp dụng Singleton Pattern

```csharp
public class ShoppingCart
{
    private readonly AppDbContext _context;

    public ShoppingCart(AppDbContext context) => _context = context;

    public static ShoppingCart GetShoppingCart(IServiceProvider services)
    {
        var session = services.GetRequiredService<IHttpContextAccessor>()
            .HttpContext!.Session;
        var context = services.GetRequiredService<AppDbContext>();
        var cartId = session.GetString("CartId") ?? Guid.NewGuid().ToString();
        session.SetString("CartId", cartId);

        // Mỗi lần gọi lại tạo một object mới.
        return new ShoppingCart(context) { ShoppingCartId = cartId };
    }

    public string ShoppingCartId { get; set; } = "";
    public List<ShoppingCartItem> ShoppingCartItems { get; set; } = new();
}
```

## Biện luận (Giải thích)

**Bước 1:** `ShoppingCart` được tạo bằng constructor public. Bất kỳ Controller hoặc Service nào cũng có thể dùng `new ShoppingCart(context)`.

**Bước 2:** `GetShoppingCart()` lấy `CartId` từ Session, nhưng vẫn tạo instance mới ở mỗi lần gọi. CartId có thể giống nhau, còn trạng thái object và DbContext là những đối tượng khác nhau.

**Bước 3:** Nếu nhiều nơi cùng truy cập giỏ hàng, việc tracking entity và danh sách item có thể không đồng nhất. Logic khởi tạo session bị rải rác và khó kiểm soát.

**Bước 4:** Đây chưa phải Singleton đúng nghĩa; nó chỉ là một factory method dựa vào session. Việc tạo object lặp lại gây lãng phí và làm Controller phụ thuộc vào cách khởi tạo.

**Bước 5:** Luồng cũ là `Controller → GetShoppingCart() → new ShoppingCart → AppDbContext → View`. Chức năng vẫn chạy nhưng không bảo đảm một instance duy nhất trong cùng phạm vi xử lý.

## Sau khi dùng Singleton Pattern

```csharp
// Program.cs
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped(sc => ShoppingCart.GetShoppingCart(sc));

// ShoppingCart.cs
public class ShoppingCart
{
    private readonly AppDbContext _context;
    public string ShoppingCartId { get; set; } = "";
    public List<ShoppingCartItem> ShoppingCartItems { get; set; } = new();

    public ShoppingCart(AppDbContext context) => _context = context;

    public static ShoppingCart GetShoppingCart(IServiceProvider services)
    {
        var session = services.GetRequiredService<IHttpContextAccessor>()
            .HttpContext!.Session;
        var context = services.GetRequiredService<AppDbContext>();
        var cartId = session.GetString("CartId");
        if (string.IsNullOrEmpty(cartId))
        {
            cartId = Guid.NewGuid().ToString();
            session.SetString("CartId", cartId);
        }

        return new ShoppingCart(context) { ShoppingCartId = cartId };
    }
}
```

## Biện luận (Giải thích)

**Bước 1:** `ShoppingCart` được đăng ký qua DI. Controller chỉ inject `ShoppingCart`, không tự tạo object.

**Bước 2:** Session giữ một `CartId` ổn định cho người dùng. Trong phạm vi request, DI cung cấp cùng registration và các thao tác thêm, xóa, tính tổng dùng cùng ngữ cảnh giỏ hàng.

**Bước 3:** Nếu session chưa có CartId, hệ thống tạo một GUID rồi lưu lại; những lần sau dùng lại GUID đó. Nhờ vậy item được truy vấn đúng theo người dùng.

**Bước 4:** Singleton ở đây nên hiểu là “một giỏ hàng duy nhất theo session”, không phải một giỏ hàng toàn cục cho tất cả người dùng. Cách này tránh làm lẫn giỏ hàng giữa các user và phù hợp với web nhiều người dùng.

**Bước 5:** Luồng mới là `Controller → ShoppingCart do DI quản lý → AppDbContext → View`. Việc đăng ký `Scoped` là lựa chọn an toàn hơn `Singleton` toàn ứng dụng vì `AppDbContext` cũng có vòng đời scoped.

---

# 2. Áp dụng mẫu Bridge Pattern

## Trước khi áp dụng Bridge Pattern

```csharp
public double CalculateSeatPrice(Seat seat, double basePrice)
{
    // Logic chính sách giá bị nhúng trong Service/Controller.
    switch (seat.SeatType)
    {
        case SeatType.VIP: return basePrice * 1.2;
        case SeatType.Couple: return basePrice * 2.0;
        case SeatType.Disabled: return basePrice * 0.5;
        default: return basePrice;
    }
}
```

## Biện luận (Giải thích)

**Bước 1:** Service nhận cả dữ liệu ghế và giá cơ sở rồi tự quyết định cách tính.

**Bước 2:** Câu lệnh `switch` bị lặp ở Controller, Service hoặc JavaScript. Logic truy vấn ghế và logic giá bị trộn với nhau.

**Bước 3:** Khi thêm loại ghế mới, lập trình viên phải tìm mọi switch tương tự. Chỉ cần bỏ sót một nơi là giá hiển thị và giá lưu DB khác nhau.

**Bước 4:** Phiên bản cũ vi phạm Single Responsibility và Open/Closed: một lớp vừa xử lý booking vừa biết toàn bộ chính sách giá.

**Bước 5:** Kết quả vẫn trả được giá, nhưng khó mở rộng, khó unit test từng chính sách và khó thay đổi chính sách theo rạp hoặc thời điểm.

## Sau khi dùng Bridge Pattern

```csharp
namespace movieCinema.Models.Bridge;

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

    public SeatPricingBridge(ISeatingPricingStrategy strategy) => _strategy = strategy;

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

    public double GetPrice(double basePrice) => _strategy.CalculatePrice(basePrice);
}

// BookingFacade sử dụng:
var bridge = new SeatPricingBridge(seat?.SeatType ?? SeatType.Standard);
totalPrice += bridge.GetPrice(showtime.Price);
```

## Biện luận (Giải thích)

**Bước 1:** `ISeatingPricingStrategy` là phần implementation; các class Standard, VIP, Couple, Disabled chứa từng công thức riêng. `SeatPricingBridge` là abstraction cung cấp `GetPrice`.

**Bước 2:** `BookingFacade` không cần biết công thức chi tiết. Nó chỉ chọn loại ghế và gọi `GetPrice`.

**Bước 3:** Constructor dùng switch expression và fallback Standard khi loại ghế không xác định, nhờ đó giá luôn có giá trị hợp lệ.

**Bước 4:** Bridge tách hai chiều thay đổi: loại ghế và thuật toán tính giá. Thêm `PremiumPricingStrategy` không cần sửa logic tính booking.

**Bước 5:** Cùng một Bridge được dùng khi tính tổng trong Facade và khi trả giá từng ghế ở `OrdersController`, giúp UI và DB dùng một chính sách thống nhất.

---

# 3. Áp dụng mẫu Decorator Pattern

## Trước khi áp dụng Decorator Pattern

```csharp
public double CalculateFinalPrice(
    double basePrice, Voucher? voucher, int points, DateTime now)
{
    var price = basePrice;

    if (voucher != null && price >= voucher.MinOrderAmount)
        price -= voucher.IsPercentage
            ? price * voucher.DiscountPercentage / 100.0
            : voucher.DiscountAmount;

    if (points > 0)
        price -= points * 1000.0;

    if (now.TimeOfDay >= new TimeSpan(14, 0, 0) &&
        now.TimeOfDay <= new TimeSpan(17, 0, 0))
        price *= 0.85;

    return Math.Max(0, price);
}
```

## Biện luận (Giải thích)

**Bước 1:** Một method duy nhất xử lý giá gốc, voucher, điểm và Happy Hour.

**Bước 2:** Thứ tự áp dụng bị cố định trong chuỗi `if`. Muốn thêm chương trình khuyến mãi phải sửa method hiện tại.

**Bước 3:** Các điều kiện giảm giá nằm lẫn với nhau; phần hiển thị breakdown cũng phải tự tính lại công thức.

**Bước 4:** Code vi phạm Open/Closed và Single Responsibility. Một thay đổi nhỏ trong voucher có thể ảnh hưởng toàn bộ tính giá.

**Bước 5:** Method chỉ trả về một con số, không mô tả được từng khoản giảm giá cho khách hàng.

## Sau khi dùng Decorator Pattern

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
        => (_inner, _voucher) = (inner, voucher);

    public double CalculatePrice(double currentPrice)
    {
        var discounted = _inner.CalculatePrice(currentPrice);
        if (discounted < _voucher.MinOrderAmount) return discounted;
        var reduction = _voucher.IsPercentage
            ? discounted * _voucher.DiscountPercentage / 100.0
            : _voucher.DiscountAmount;
        return Math.Max(0, discounted - Math.Min(reduction, discounted));
    }
    public string Description => $"Voucher giảm -{_voucher.Code}";
    public int Priority => 1;
}

public class LoyaltyPointsDecorator : IOrderPriceDecorator
{
    private readonly IOrderPriceDecorator _inner;
    private readonly int _points;
    public LoyaltyPointsDecorator(IOrderPriceDecorator inner, int points)
        => (_inner, _points) = (inner, points);
    public double CalculatePrice(double currentPrice)
        => Math.Max(0, _inner.CalculatePrice(currentPrice) - _points * 1000.0);
    public string Description => $"Điểm tích lũy: {_points} điểm";
    public int Priority => 2;
}

public class HappyHourDecorator : IOrderPriceDecorator
{
    private readonly IOrderPriceDecorator _inner;
    public HappyHourDecorator(IOrderPriceDecorator inner) => _inner = inner;
    public double CalculatePrice(double currentPrice)
    {
        var now = DateTime.Now.TimeOfDay;
        var price = _inner.CalculatePrice(currentPrice);
        return now >= new TimeSpan(14, 0, 0) && now <= new TimeSpan(17, 0, 0)
            ? price * 0.85 : price;
    }
    public string Description => "Happy Hour 15%";
    public int Priority => 3;
}

public class OrderPriceCalculator
{
    public PriceCalculationResult Calculate(
        double basePrice, Voucher? voucher, int points, bool applyHappyHour)
    {
        IOrderPriceDecorator calc = new BasePriceCalculator(basePrice);
        if (voucher != null) calc = new VoucherDecorator(calc, voucher);
        if (points > 0) calc = new LoyaltyPointsDecorator(calc, points);
        if (applyHappyHour) calc = new HappyHourDecorator(calc);
        var finalPrice = calc.CalculatePrice(basePrice);
        return new PriceCalculationResult
        {
            OriginalPrice = basePrice,
            FinalPrice = finalPrice,
            DiscountApplied = basePrice - finalPrice,
            Description = calc.Description
        };
    }
}
```

## Biện luận (Giải thích)

**Bước 1:** `BasePriceCalculator` là component gốc. Mỗi decorator cùng implement `IOrderPriceDecorator` và giữ một `_inner` decorator.

**Bước 2:** `OrderPriceCalculator` xây chuỗi: giá gốc → voucher → điểm → Happy Hour. Mỗi lớp gọi `_inner.CalculatePrice()` rồi bổ sung trách nhiệm của mình.

**Bước 3:** Điều kiện tối thiểu voucher, giới hạn giá âm và khung giờ được kiểm tra tại đúng decorator liên quan.

**Bước 4:** Có thể thêm `BirthdayDecorator` hoặc `MemberDecorator` mà không sửa các decorator cũ. Có thể bật/tắt từng khuyến mãi bằng cách không thêm nó vào chain.

**Bước 5:** `PriceCalculationResult` còn có thể chứa `Breakdown` để View hiển thị giá gốc, voucher, điểm và Happy Hour độc lập.

---

# 4. Áp dụng mẫu Proxy Pattern

## Trước khi áp dụng Proxy Pattern

```csharp
public class MoviesService : IMoviesService
{
    private readonly AppDbContext _context;
    public MoviesService(AppDbContext context) => _context = context;

    public async Task<IEnumerable<Movie>> GetAllAsync()
        => await _context.Movies.ToListAsync();

    public async Task<Movie> GetByIdAsync(int id)
        => await _context.Movies.FirstOrDefaultAsync(m => m.Id == id);
}
```

## Biện luận (Giải thích)

**Bước 1:** Controller gọi trực tiếp service thật, service gọi thẳng DB.

**Bước 2:** Các request lặp lại cùng query dù danh sách phim ít thay đổi.

**Bước 3:** Không có lớp kiểm soát cache, nên SQL Server phải xử lý mọi request.

**Bước 4:** Nếu thêm cache vào `MoviesService`, service vừa truy vấn vừa quản lý cache, làm tăng trách nhiệm và khó test.

**Bước 5:** Luồng là `Controller → MoviesService → EF Core → SQL Server`, tốc độ giảm khi lượng truy cập tăng.

## Sau khi dùng Proxy Pattern

```csharp
public class CachedMoviesServiceProxy : IMoviesService
{
    private readonly MoviesService _realService;
    private readonly IMemoryCache _cache;
    private static readonly TimeSpan DefaultExpiry = TimeSpan.FromMinutes(10);

    public CachedMoviesServiceProxy(MoviesService realService, IMemoryCache cache)
        => (_realService, _cache) = (realService, cache);

    public async Task<IEnumerable<Movie>> GetAllAsync()
        => await _cache.GetOrCreateAsync("movies:all", async entry =>
        {
            entry.SlidingExpiration = DefaultExpiry;
            return await _realService.GetAllAsync();
        }) ?? Enumerable.Empty<Movie>();

    public async Task<Movie> GetByIdAsync(int id)
    {
        var key = $"movies:id:{id}";
        return await _cache.GetOrCreateAsync(key, async entry =>
        {
            entry.SlidingExpiration = DefaultExpiry;
            return await _realService.GetByIdAsync(id);
        }) ?? null!;
    }

    public async Task UpdateAsync(int id, Movie entity)
    {
        await _realService.UpdateAsync(id, entity);
        InvalidateAllCaches();
    }

    private void InvalidateAllCaches()
    {
        // Source hiện tại để dành cho cơ chế tag/Redis ở production.
    }
}

// Program.cs
builder.Services.AddMemoryCache();
builder.Services.AddScoped<MoviesService>();
builder.Services.AddScoped<IMoviesService>(sp =>
    new CachedMoviesServiceProxy(
        sp.GetRequiredService<MoviesService>(),
        sp.GetRequiredService<IMemoryCache>()));
```

## Biện luận (Giải thích)

**Bước 1:** Proxy thực thi cùng `IMoviesService` với service thật, nên Controller không cần thay đổi.

**Bước 2:** Cache HIT trả dữ liệu ngay; cache MISS mới gọi `_realService`. Key theo id và theo ngày giúp kiểm soát phạm vi dữ liệu.

**Bước 3:** Các thao tác Add/Update/Delete gọi invalidation sau khi DB thay đổi. Sliding expiration tự làm mới thời gian sống khi dữ liệu được truy cập.

**Bước 4:** `MoviesService` không biết cache tồn tại. Có thể thay Proxy bằng service thật trong unit test hoặc thay bằng Redis Proxy ở production.

**Bước 5:** Luồng mới là `Controller → CachedMoviesServiceProxy → Cache`; chỉ khi MISS mới đi tiếp `MoviesService → DB`. Đây là lazy access và cache proxy.

---

# 5. Áp dụng mẫu Strategy Pattern

## Trước khi áp dụng Strategy Pattern

```csharp
public async Task<PaymentResult> PayAsync(
    string? method, double amount, string orderId)
{
    if (method == "paypal")
    {
        await Task.Delay(100);
        return new PaymentResult { Success = true, TransactionId = "PP-..." };
    }

    if (method == "cash")
        return new PaymentResult { Success = true, TransactionId = "CASH-..." };

    throw new InvalidOperationException("Payment method not supported");
}
```

## Biện luận (Giải thích)

**Bước 1:** Một method chứa thuật toán của mọi phương thức thanh toán.

**Bước 2:** Cùng một if/else dễ bị lặp trong Pay và Refund.

**Bước 3:** Thêm MoMo/VNPay phải sửa class đang chạy ổn định và có thể quên cập nhật một nhánh.

**Bước 4:** Code vi phạm Open/Closed; logic payment gắn cứng với OrdersService.

**Bước 5:** Client nhận kết quả nhưng không có abstraction để thay đổi thuật toán tại runtime.

## Sau khi dùng Strategy Pattern

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
        => Task.FromResult(new PaymentResult
        {
            Success = true,
            TransactionId = $"CASH-{orderId}-{DateTime.Now.Ticks}",
            Message = "Thanh toán tại rạp."
        });
    public Task<RefundResult> RefundAsync(string transactionId, double amount)
        => Task.FromResult(new RefundResult { Success = true, RefundId = $"REF-{transactionId}" });
}

public class PayPalPaymentStrategy : IPaymentStrategy
{
    private readonly string _clientId;
    private readonly string _clientSecret;
    public PayPalPaymentStrategy(string clientId, string clientSecret)
        => (_clientId, _clientSecret) = (clientId, clientSecret);
    public string Name => "PayPal";
    public string PaymentMethod => "PayPal";
    public async Task<PaymentResult> PayAsync(double amount, string orderId)
    {
        await Task.Delay(100); // stub PayPal SDK
        return new PaymentResult
        {
            Success = true,
            TransactionId = $"PP-{orderId}-{DateTime.Now.Ticks}",
            Message = "Thanh toán PayPal thành công."
        };
    }
    public async Task<RefundResult> RefundAsync(string transactionId, double amount)
    {
        await Task.Delay(100);
        return new RefundResult { Success = true, RefundId = $"REF-{transactionId}" };
    }
}

public class PaymentContext
{
    private IPaymentStrategy? _strategy;
    public void SetStrategy(IPaymentStrategy strategy) => _strategy = strategy;
    public void SetStrategyByName(string? name)
    {
        _strategy = name?.ToLower() switch
        {
            "paypal" => new PayPalPaymentStrategy("CLIENT_ID", "CLIENT_SECRET"),
            _ => new CashPaymentStrategy()
        };
    }
    public Task<PaymentResult> PayAsync(double amount, string orderId)
        => _strategy == null
            ? throw new InvalidOperationException("Payment strategy not set.")
            : _strategy.PayAsync(amount, orderId);
    public string CurrentPaymentMethod => _strategy?.PaymentMethod ?? "Unknown";
}
```

## Biện luận (Giải thích)

**Bước 1:** `IPaymentStrategy` định nghĩa hợp đồng chung; Cash và PayPal là hai thuật toán thay thế được.

**Bước 2:** `PaymentContext` giữ strategy hiện tại và ủy quyền `PayAsync`/`RefundAsync`; client chọn strategy tại runtime.

**Bước 3:** Context kiểm tra strategy chưa được thiết lập và ném lỗi rõ ràng thay vì NullReferenceException.

**Bước 4:** Thêm `MoMoPaymentStrategy` chỉ cần implement interface và đăng ký/chọn nó, không phải viết lại luồng booking.

**Bước 5:** `PaymentResult` thống nhất cho mọi cổng. Facade chỉ cần kiểm tra `Success`, sau đó tạo Order.

---

# 6. Áp dụng mẫu Adapter Pattern

> Trong source hiện tại, Adapter chưa phải một class adapter độc lập; `PayPalPaymentStrategy` đang đồng thời đóng vai trò adapter khi chuyển API cổng thanh toán ngoài sang `IPaymentStrategy`. Vì vậy phần này phải trình bày trung thực là Adapter được tích hợp cùng Strategy.

## Trước khi áp dụng Adapter Pattern

```csharp
public async Task<string> PayPalDirectAsync(double amount, string orderId)
{
    var paypal = new PayPalClient();
    // API ngoài có chữ ký và kiểu trả về riêng.
    return paypal.ExecutePayment(orderId, amount, "VND");
}
```

## Biện luận (Giải thích)

**Bước 1:** Service gọi trực tiếp SDK PayPal, nên biết chi tiết class và thứ tự tham số của SDK.

**Bước 2:** Kết quả là chuỗi của PayPal, không cùng kiểu `PaymentResult` của hệ thống.

**Bước 3:** Mọi thay đổi API bên ngoài buộc phải sửa Service; việc bắt lỗi và chuyển đổi trạng thái bị phân tán.

**Bước 4:** Hệ thống phụ thuộc cứng vào PayPal, không thể thay bằng cổng khác mà không sửa luồng booking.

**Bước 5:** View phải tự hiểu chuỗi giao dịch ngoài để hiển thị thành công/thất bại, thiếu tính thống nhất.

## Sau khi dùng Adapter Pattern

```csharp
public class PayPalPaymentStrategy : IPaymentStrategy
{
    private readonly PayPalClient _client;

    public PayPalPaymentStrategy(string clientId, string clientSecret)
    {
        _client = new PayPalClient(clientId, clientSecret);
    }

    public string Name => "PayPal";
    public string PaymentMethod => "PayPal";

    public async Task<PaymentResult> PayAsync(double amount, string orderId)
    {
        // Adapter gọi API ngoài bên trong và chuyển đổi kết quả.
        var paypalTransactionId = await _client.ExecutePaymentAsync(
            orderId, amount, "VND");

        return new PaymentResult
        {
            Success = !string.IsNullOrEmpty(paypalTransactionId),
            TransactionId = paypalTransactionId ?? "",
            Message = "Thanh toán PayPal thành công."
        };
    }

    public async Task<RefundResult> RefundAsync(string transactionId, double amount)
    {
        var success = await _client.RefundPaymentAsync(transactionId, amount);
        return new RefundResult
        {
            Success = success,
            RefundId = success ? $"REF-{transactionId}" : "",
            Message = success ? "Hoàn tiền thành công." : "Hoàn tiền thất bại."
        };
    }
}
```

## Biện luận (Giải thích)

**Bước 1:** `PayPalPaymentStrategy` thực thi interface nội bộ nên là đối tượng mà Facade/Context mong đợi; PayPal SDK là Adaptee.

**Bước 2:** Adapter gọi chữ ký của SDK, rồi chuyển `transactionId`, trạng thái và lỗi thành `PaymentResult`/`RefundResult` chuẩn.

**Bước 3:** Kiểm tra null, thất bại và thông báo được gom trong adapter. Tầng nghiệp vụ không phải parse kết quả của PayPal.

**Bước 4:** Khi đổi sang Stripe hoặc MoMo, tạo adapter tương ứng implement `IPaymentStrategy`; BookingFacade vẫn giữ nguyên.

**Bước 5:** Client luôn nhận cùng một kiểu result. Luồng là `Facade → PaymentContext → IPaymentStrategy/Adapter → SDK ngoài → PaymentResult`.

**Phân biệt:** Strategy trả lời “chọn thuật toán thanh toán nào”; Adapter trả lời “làm cho interface SDK ngoài tương thích với interface nội bộ”. Source hiện tại đã có abstraction và stub PayPal, nhưng cần thay stub bằng SDK thực tế để Adapter hoàn chỉnh.

---

# 7. Áp dụng mẫu Facade Pattern

## Trước khi áp dụng Facade Pattern

```csharp
[HttpPost]
public async Task<IActionResult> BookTickets(BookTicketsVM model)
{
    var showtime = await _showtimesService
        .GetShowtimeByIdWithDetailsAsync(model.ShowtimeId);
    var booked = await _ordersService
        .GetBookedSeatsForShowtimeAsync(model.ShowtimeId);
    var seats = await _seatsService
        .GetSeatsByRoomAsync(showtime.CinemaRoomId);

    // Parse ghế, kiểm tra trùng, tính giá, voucher, payment,
    // tạo Order và lưu DB đều nằm trong Controller.
    // ... hàng chục dòng if/switch ...

    return View("BookingCompleted");
}
```

## Biện luận (Giải thích)

**Bước 1:** Controller phải biết tất cả service bên dưới và thứ tự gọi chúng.

**Bước 2:** Logic lấy showtime, ghế, giá, voucher, payment và DB bị trộn với logic HTTP/redirect.

**Bước 3:** Mỗi lỗi phải tự gán TempData và redirect; khó bảo đảm các luồng lỗi nhất quán.

**Bước 4:** Đây là Fat Controller. Không thể tái sử dụng quy trình cho API mobile hoặc background job.

**Bước 5:** Controller khó unit test vì phải mock quá nhiều dependency và test cả chi tiết nghiệp vụ.

## Sau khi dùng Facade Pattern

```csharp
public interface IBookingFacade
{
    Task<BookingResult> ProcessBookingAsync(BookTicketsVM model, string? userId);
    Task<SeatPricingResult> CalculateSeatPricesAsync(int showtimeId, List<string> seatCodes);
}

public class BookingFacade : IBookingFacade
{
    private readonly AppDbContext _context;
    private readonly IShowtimesService _showtimesService;
    private readonly ISeatsService _seatsService;
    private readonly IOrdersService _ordersService;

    public BookingFacade(AppDbContext context, IShowtimesService showtimesService,
        ISeatsService seatsService, IOrdersService ordersService)
        => (_context, _showtimesService, _seatsService, _ordersService) =
           (context, showtimesService, seatsService, ordersService);

    public async Task<BookingResult> ProcessBookingAsync(
        BookTicketsVM model, string? userId)
    {
        if (string.IsNullOrEmpty(model.SelectedSeats))
            return new BookingResult { Success = false, Message = "Vui lòng chọn ghế." };

        var showtime = await _showtimesService
            .GetShowtimeByIdWithDetailsAsync(model.ShowtimeId);
        if (showtime == null)
            return new BookingResult { Success = false, Message = "Suất chiếu không tồn tại." };

        var selected = model.SelectedSeats.Split(',')
            .Select(s => s.Trim()).Where(s => s.Length > 0).ToList();
        var booked = await _ordersService
            .GetBookedSeatsForShowtimeAsync(model.ShowtimeId);
        if (selected.Any(booked.Contains))
            return new BookingResult { Success = false, Message = "Ghế đã được đặt." };

        var roomSeats = await _seatsService.GetSeatsByRoomAsync(showtime.CinemaRoomId);
        var total = selected.Sum(code =>
        {
            var seat = roomSeats.FirstOrDefault(s => s.Row + s.Number == code);
            return new SeatPricingBridge(seat?.SeatType ?? SeatType.Standard)
                .GetPrice(showtime.Price);
        });

        var payment = new PaymentContext();
        payment.SetStrategyByName(model.PaymentMethod);
        var paid = await payment.PayAsync(total, $"ORDER-{DateTime.Now.Ticks}");
        if (!paid.Success)
            return new BookingResult { Success = false, Message = paid.Message };

        await _ordersService.StoreDirectOrderAsync(
            model.ShowtimeId, model.Name ?? "Guest", model.Email ?? "",
            model.SelectedSeats, selected.Count, total, 0,
            model.PointsRedeemed, payment.CurrentPaymentMethod);

        var saved = await _context.Orders.OrderByDescending(o => o.Id)
            .FirstOrDefaultAsync();
        return new BookingResult
        {
            Success = true, OrderId = saved?.Id,
            FinalPrice = total, Message = "Đặt vé thành công!"
        };
    }
}

// Controller sau khi áp dụng
var result = await _bookingFacade.ProcessBookingAsync(model, User.Identity?.Name);
if (!result.Success) return RedirectToAction(nameof(BookTickets));
return View("BookingCompleted", result);
```

## Biện luận (Giải thích)

**Bước 1:** `IBookingFacade` là một cửa duy nhất cho subsystem booking.

**Bước 2:** Facade điều phối showtime, seats, orders, Bridge, Strategy và Builder theo đúng thứ tự.

**Bước 3:** Facade chuẩn hóa `BookingResult` gồm Success, Message, OrderId, FinalPrice và DiscountApplied.

**Bước 4:** Controller không còn phụ thuộc chi tiết subsystem; có thể dùng Facade từ nhiều loại client.

**Bước 5:** Luồng mới là `Controller → IBookingFacade → services/patterns → BookingResult → View`. Controller giảm mạnh độ dài và dễ test.

---

# 8. Áp dụng mẫu Builder Pattern

## Trước khi áp dụng Builder Pattern

```csharp
public Order CreateOrder(
    int showtimeId, string name, string email, string seats,
    int count, double basePrice, double discount,
    int points, string paymentMethod)
{
    var order = new Order
    {
        UserId = name,
        Email = email,
        OrderDate = DateTime.Now,
        Status = "Purchased",
        PaymentMethod = paymentMethod,
        TotalPrice = basePrice * count,
        DiscountAmount = discount,
        PointsRedeemed = points,
        OrderItems = new List<OrderItem>
        {
            new() { ShowtimeId = showtimeId, SelectedSeats = seats,
                    Amount = count, Price = basePrice }
        }
    };
    order.TotalPrice = Math.Max(0,
        order.TotalPrice - discount - points * 1000.0);
    return order;
}
```

## Biện luận (Giải thích)

**Bước 1:** Method có nhiều tham số cùng kiểu `double`, `int`, `string`; truyền nhầm vị trí khó phát hiện.

**Bước 2:** Logic gán thuộc tính, tạo OrderItem và tính tổng nằm trong một hàm.

**Bước 3:** Validation voucher, points và tổng tiền không được đóng gói; mỗi nơi tạo Order có thể xử lý khác nhau.

**Bước 4:** Đây là Long Parameter List và vi phạm Single Responsibility; thêm thuộc tính mới làm thay đổi chữ ký method.

**Bước 5:** Kết quả là Order nhưng quy trình xây dựng khó đọc, khó tái sử dụng và khó test theo từng bước.

## Sau khi dùng Builder Pattern

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

    public IOrderBuilder SetCustomer(string name, string email, string userId)
    {
        _order.Email = email;
        _order.UserId = name;
        return this;
    }

    public IOrderBuilder SetShowtime(int showtimeId, string seats,
        int count, double basePrice)
    {
        _subtotal = basePrice * count;
        _order.OrderItems.Add(new OrderItem
        {
            ShowtimeId = showtimeId, SelectedSeats = seats,
            Amount = count, Price = basePrice
        });
        return this;
    }

    public IOrderBuilder ApplyVoucher(double discount, double total)
    {
        _order.DiscountAmount = Math.Min(discount, total);
        return this;
    }

    public IOrderBuilder RedeemPoints(int points, double totalBeforePoints)
    {
        _order.PointsRedeemed = (int)Math.Min(points * 1000.0, totalBeforePoints);
        return this;
    }

    public IOrderBuilder SetPaymentMethod(string method)
    {
        _order.PaymentMethod = method;
        return this;
    }

    public IOrderBuilder CalculateTotal()
    {
        _order.TotalPrice = Math.Max(0,
            _subtotal - _order.DiscountAmount - _order.PointsRedeemed);
        return this;
    }

    public Order Build() => _order;
}

var order = new OrderBuilder()
    .SetCustomer(name, email, userId)
    .SetShowtime(showtimeId, seats, count, basePrice)
    .ApplyVoucher(discount, total)
    .RedeemPoints(points, total - discount)
    .SetPaymentMethod(method)
    .CalculateTotal()
    .Build();
```

## Biện luận (Giải thích)

**Bước 1:** Builder chia quy trình thành các method có tên rõ ràng và trả về `this`, tạo Fluent Interface.

**Bước 2:** Mỗi bước chỉ cập nhật một nhóm thuộc tính; Service không cần biết cách Order nội bộ được lắp ráp.

**Bước 3:** `Math.Min` ngăn discount/points vượt quá tổng; `CalculateTotal` bảo đảm giá cuối không âm.

**Bước 4:** Có thể thêm `SetNote`, `SetVoucherCode` mà không biến method thành danh sách tham số dài hơn.

**Bước 5:** `Build()` trả về Product hoàn chỉnh. BookingFacade chỉ mô tả “đặt Order như thế nào”, dễ đọc và dễ unit test.

---

# 9. Áp dụng mẫu State Pattern

## Trước khi áp dụng State Pattern

```csharp
public async Task<bool> ChangeStatusAsync(Order order, string newStatus)
{
    if (order.Status == "Purchased" && newStatus == "Confirmed")
    {
        order.Status = "Confirmed";
        // cộng điểm, gửi mail...
    }
    else if (order.Status == "Purchased" && newStatus == "Cancelled")
    {
        order.Status = "Cancelled";
        // hoàn điểm...
    }
    else if (order.Status == "Confirmed" && newStatus == "Refunded")
    {
        order.Status = "Refunded";
        // hoàn tiền...
    }
    else return false;

    await _context.SaveChangesAsync();
    return true;
}
```

## Biện luận (Giải thích)

**Bước 1:** Mọi trạng thái và transition nằm trong một method.

**Bước 2:** Kiểm tra transition và hành động khi vào state bị trộn vào nhau.

**Bước 3:** Chuỗi literal dễ sai chính tả và khó biết trạng thái nào là terminal.

**Bước 4:** Thêm state mới phải sửa method lớn, dễ tạo regression.

**Bước 5:** Caller chỉ nhận bool, thiếu thông tin trạng thái cũ/mới và lý do thất bại.

## Sau khi dùng State Pattern

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
    public bool CanTransitionTo(string status)
        => status is "Confirmed" or "Cancelled";
    public Task OnEnterAsync(Order order, AppDbContext context)
        => Task.CompletedTask;
}

public class ConfirmedState : IOrderState
{
    public string StatusName => "Confirmed";
    public bool CanTransitionTo(string status)
        => status is "Cancelled" or "Refunded";
    public Task OnEnterAsync(Order order, AppDbContext context)
        => Task.CompletedTask;
}

public class CancelledState : IOrderState
{
    public string StatusName => "Cancelled";
    public bool CanTransitionTo(string status) => false;
    public async Task OnEnterAsync(Order order, AppDbContext context)
    {
        // Hoàn điểm/giải phóng ghế.
        await context.SaveChangesAsync();
    }
}

public class RefundedState : IOrderState
{
    public string StatusName => "Refunded";
    public bool CanTransitionTo(string status) => false;
    public Task OnEnterAsync(Order order, AppDbContext context)
        => Task.CompletedTask;
}

public static class OrderStateMachine
{
    private static readonly Dictionary<string, IOrderState> States = new()
    {
        ["Purchased"] = new PurchasedState(),
        ["Confirmed"] = new ConfirmedState(),
        ["Cancelled"] = new CancelledState(),
        ["Refunded"] = new RefundedState()
    };

    public static IOrderState? GetState(string status)
        => States.GetValueOrDefault(status);

    public static bool CanTransition(string from, string to)
        => States.TryGetValue(from, out var state) && state.CanTransitionTo(to);
}
```

## Biện luận (Giải thích)

**Bước 1:** `IOrderState` chuẩn hóa hành vi của một state; mỗi class chịu trách nhiệm một trạng thái.

**Bước 2:** `OrderStateMachine` tra state theo tên và ủy quyền `CanTransitionTo`/`OnEnterAsync`.

**Bước 3:** Purchased chỉ cho Confirmed/Cancelled; Confirmed cho Cancelled/Refunded; Cancelled và Refunded là terminal.

**Bước 4:** Thêm `ExpiredState` chỉ cần class mới và thêm dictionary entry, không sửa logic cũ.

**Bước 5:** `OrdersService.ChangeOrderStatusWithStateAsync` trả `StatusChangeResult` gồm Success, Message, OldStatus, NewStatus; View có thông báo chính xác.

---

# 10. Áp dụng mẫu Observer Pattern

## Trước khi áp dụng Observer Pattern

```csharp
public async Task ChangeStatusAsync(Order order, string newStatus)
{
    var oldStatus = order.Status;
    order.Status = newStatus;
    await _context.SaveChangesAsync();

    // Tất cả side effect nằm trong OrdersService.
    Console.WriteLine($"[AUDIT] {oldStatus} -> {newStatus}");
    await UpdateMemberPointsAsync(order, newStatus);
    await SendEmailAsync(order, newStatus);
}
```

## Biện luận (Giải thích)

**Bước 1:** Service vừa đổi trạng thái vừa log, cộng điểm và gửi email.

**Bước 2:** Side effect bị gọi cứng theo đúng thứ tự trong Service.

**Bước 3:** Nếu email lỗi, có thể làm ảnh hưởng luồng chính; muốn thêm SMS phải sửa Service.

**Bước 4:** Vi phạm Single Responsibility và Open/Closed, khó test từng side effect.

**Bước 5:** Không có cơ chế detach/attach độc lập; caller không kiểm soát được observer nào đang hoạt động.

## Sau khi dùng Observer Pattern

```csharp
public interface IOrderObserver
{
    Task OnOrderStatusChangedAsync(Order order, string oldStatus, string newStatus);
}

public interface IOrderSubject
{
    void Attach(IOrderObserver observer);
    void Detach(IOrderObserver observer);
    Task NotifyAsync(Order order, string oldStatus, string newStatus);
}

public class OrderSubject : IOrderSubject
{
    private readonly List<IOrderObserver> _observers = new();
    private readonly IServiceScopeFactory _scopeFactory;
    public OrderSubject(IServiceScopeFactory scopeFactory) => _scopeFactory = scopeFactory;

    public void Attach(IOrderObserver observer) => _observers.Add(observer);
    public void Detach(IOrderObserver observer) => _observers.Remove(observer);

    public async Task NotifyAsync(Order order, string oldStatus, string newStatus)
    {
        foreach (var observer in _observers.ToList())
        {
            try { await observer.OnOrderStatusChangedAsync(order, oldStatus, newStatus); }
            catch { /* một observer lỗi không chặn observer khác */ }
        }
    }
}

public class AuditLogObserver : IOrderObserver
{
    private readonly ILogger<AuditLogObserver> _logger;
    public AuditLogObserver(ILogger<AuditLogObserver> logger) => _logger = logger;
    public Task OnOrderStatusChangedAsync(Order order, string oldStatus, string newStatus)
    {
        _logger.LogInformation("Order #{Id}: {Old} -> {New}",
            order.Id, oldStatus, newStatus);
        return Task.CompletedTask;
    }
}

public class LoyaltyPointsObserver : IOrderObserver
{
    private readonly IServiceScopeFactory _scopeFactory;
    public LoyaltyPointsObserver(IServiceScopeFactory scopeFactory) => _scopeFactory = scopeFactory;
    public async Task OnOrderStatusChangedAsync(Order order, string oldStatus, string newStatus)
    {
        if (string.IsNullOrEmpty(order.Email)) return;
        using var scope = _scopeFactory.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var member = await context.Members.FirstOrDefaultAsync(
            m => m.Email.ToLower() == order.Email.ToLower());
        if (member == null) return;
        if (newStatus == "Confirmed" && oldStatus == "Purchased") member.Points++;
        if (newStatus is "Cancelled" or "Refunded") member.Points = Math.Max(0, member.Points - 1);
        await context.SaveChangesAsync();
    }
}

public class EmailNotificationObserver : IOrderObserver
{
    private readonly ILogger<EmailNotificationObserver> _logger;
    public EmailNotificationObserver(ILogger<EmailNotificationObserver> logger) => _logger = logger;
    public Task OnOrderStatusChangedAsync(Order order, string oldStatus, string newStatus)
    {
        if (!string.IsNullOrEmpty(order.Email))
            _logger.LogInformation("[EMAIL] To: {Email}, status: {Status}",
                order.Email, newStatus);
        return Task.CompletedTask;
    }
}
```

## Biện luận (Giải thích)

**Bước 1:** `OrderSubject` quản lý danh sách; mọi observer tuân theo `IOrderObserver`.

**Bước 2:** Sau khi đổi status, Subject gọi `NotifyAsync`; `OrdersService` không cần biết chi tiết observer.

**Bước 3:** Mỗi observer tự kiểm tra status và dữ liệu liên quan. Try/catch cô lập lỗi giữa các observer.

**Bước 4:** Thêm SMS, push notification hoặc audit database chỉ cần thêm class implement interface và đăng ký DI.

**Bước 5:** Luồng là `OrdersService → OrderSubject → Audit/Loyalty/Email`. Các side effect được mở rộng mà không sửa Subject.

---

# 11. Áp dụng mẫu Chain of Responsibility Pattern

## Trước khi áp dụng Chain of Responsibility

```csharp
public async Task<IActionResult> BookTickets(BookTicketsVM model)
{
    if (model.ShowtimeId <= 0) return BadRequest("Suất chiếu không hợp lệ");
    if (string.IsNullOrEmpty(model.SelectedSeats)) return BadRequest("Chưa chọn ghế");

    var booked = await _ordersService.GetBookedSeatsForShowtimeAsync(model.ShowtimeId);
    if (model.SelectedSeats.Split(',').Any(booked.Contains))
        return BadRequest("Ghế đã được đặt");

    var voucher = await _ordersService.GetVoucherByCodeAsync(model.VoucherCode);
    if (model.VoucherCode != null && voucher == null)
        return BadRequest("Voucher không hợp lệ");

    if (model.PointsRedeemed > 0)
    {
        var member = await _ordersService.GetMemberByEmailAsync(model.Email);
        if (member == null || member.Points < model.PointsRedeemed)
            return BadRequest("Không đủ điểm");
    }

    return Ok();
}
```

## Biện luận (Giải thích)

**Bước 1:** Một action chứa nhiều validation độc lập.

**Bước 2:** Mỗi validation biết cách trả HTTP error, nên không tái sử dụng được cho client khác.

**Bước 3:** Early return xuất hiện nhiều lần; thứ tự kiểm tra nằm cứng trong action.

**Bước 4:** Thêm validation CAPTCHA, giới hạn thời gian hoặc kiểm tra giá phải sửa Controller.

**Bước 5:** Unit test phải khởi tạo Controller và mock toàn bộ service dù chỉ muốn test một validator.

## Sau khi dùng Chain of Responsibility

```csharp
public class OrderPipelineRequest
{
    public BookTicketsVM Model { get; set; } = null!;
}

public class OrderPipelineResult
{
    public bool IsValid { get; set; }
    public string Message { get; set; } = "";
    public List<string> AppliedDiscounts { get; set; } = new();
}

public abstract class OrderPipelineHandler
{
    protected OrderPipelineHandler? _next;
    public OrderPipelineHandler SetNext(OrderPipelineHandler next)
    { _next = next; return next; }
    public abstract Task<OrderPipelineResult> HandleAsync(
        OrderPipelineRequest request, OrderPipelineResult result);
}

public class ValidationHandler : OrderPipelineHandler
{
    public override async Task<OrderPipelineResult> HandleAsync(
        OrderPipelineRequest request, OrderPipelineResult result)
    {
        if (request.Model.ShowtimeId <= 0)
            return Fail(result, "Suất chiếu không hợp lệ.");
        if (string.IsNullOrEmpty(request.Model.SelectedSeats))
            return Fail(result, "Vui lòng chọn ghế.");
        var seats = request.Model.SelectedSeats.Split(',')
            .Select(s => s.Trim()).Where(s => s.Length > 0).ToList();
        if (seats.Count > 10) return Fail(result, "Tối đa 10 ghế.");
        return _next == null ? result : await _next.HandleAsync(request, result);
    }
    private static OrderPipelineResult Fail(OrderPipelineResult r, string message)
    { r.IsValid = false; r.Message = message; return r; }
}

public class SeatAvailabilityHandler : OrderPipelineHandler
{
    private readonly IOrdersService _ordersService;
    public SeatAvailabilityHandler(IOrdersService ordersService) => _ordersService = ordersService;
    public override async Task<OrderPipelineResult> HandleAsync(
        OrderPipelineRequest request, OrderPipelineResult result)
    {
        if (!result.IsValid) return result;
        var booked = await _ordersService.GetBookedSeatsForShowtimeAsync(
            request.Model.ShowtimeId);
        var selected = request.Model.SelectedSeats.Split(',').Select(s => s.Trim());
        var occupied = selected.FirstOrDefault(booked.Contains);
        if (occupied != null)
        { result.IsValid = false; result.Message = $"Ghế {occupied} đã được đặt."; return result; }
        return _next == null ? result : await _next.HandleAsync(request, result);
    }
}

public class VoucherValidationHandler : OrderPipelineHandler
{
    private readonly IOrdersService _ordersService;
    public VoucherValidationHandler(IOrdersService ordersService) => _ordersService = ordersService;
    public override async Task<OrderPipelineResult> HandleAsync(
        OrderPipelineRequest request, OrderPipelineResult result)
    {
        if (!result.IsValid) return result;
        if (!string.IsNullOrEmpty(request.Model.VoucherCode))
        {
            var voucher = await _ordersService.GetVoucherByCodeAsync(request.Model.VoucherCode);
            if (voucher == null)
            { result.IsValid = false; result.Message = "Voucher không tồn tại/hết hạn."; return result; }
            result.AppliedDiscounts.Add($"Voucher {voucher.Code}");
        }
        return _next == null ? result : await _next.HandleAsync(request, result);
    }
}

public static class OrderPipelineBuilder
{
    public static OrderPipelineHandler Build(IOrdersService service)
    {
        var validation = new ValidationHandler();
        var seats = new SeatAvailabilityHandler(service);
        var voucher = new VoucherValidationHandler(service);
        validation.SetNext(seats).SetNext(voucher);
        return validation;
    }
}
```

## Biện luận (Giải thích)

**Bước 1:** Handler trừu tượng chứa `_next` và hợp đồng `HandleAsync`.

**Bước 2:** Mỗi handler xử lý đúng một việc rồi chuyển request cho handler tiếp theo.

**Bước 3:** Khi một handler thất bại, nó trả result ngay; các handler sau không chạy.

**Bước 4:** `OrderPipelineBuilder` tập trung thứ tự chain. Thêm handler mới chỉ cần nối vào chain.

**Bước 5:** `CompleteBookingHandler` dùng pipeline trước khi gọi Facade; pipeline dùng được cho MVC, API hoặc job khác.

---

# 12. Áp dụng mẫu Mediator Pattern

## Trước khi áp dụng Mediator Pattern

```csharp
[HttpPost]
public async Task<IActionResult> BookTickets(BookTicketsVM model)
{
    // Controller trực tiếp biết Chain, Facade và OrdersService.
    var pipeline = OrderPipelineBuilder.Build(_ordersService);
    var validation = await pipeline.HandleAsync(
        new OrderPipelineRequest { Model = model },
        new OrderPipelineResult { IsValid = true });
    if (!validation.IsValid) return BadRequest(validation.Message);

    var booking = await _bookingFacade.ProcessBookingAsync(
        model, User.Identity?.Name);
    return Ok(booking);
}
```

## Biện luận (Giải thích)

**Bước 1:** Controller trực tiếp gọi nhiều subsystem và phải biết thứ tự phối hợp.

**Bước 2:** Mỗi action booking, confirm, cancel có thể lặp logic điều phối.

**Bước 3:** Khi thay đổi quy trình, phải sửa nhiều Controller; coupling giữa presentation và application logic cao.

**Bước 4:** Controller trở thành nơi điều phối, trong khi trách nhiệm của nó nên chỉ là nhận request và trả response.

**Bước 5:** Unit test Controller phải mock toàn bộ chain, facade và order service cùng lúc.

## Sau khi dùng Mediator Pattern

```csharp
public interface IMediator
{
    Task<TResponse> SendAsync<TResponse>(IRequest<TResponse> request);
}

public interface IRequest<TResponse> { }

public interface IRequestHandler<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    Task<TResponse> HandleAsync(TRequest request);
}

public class CompleteBookingRequest : IRequest<CompleteBookingResponse>
{
    public BookTicketsVM Model { get; set; } = null!;
    public string? UserId { get; set; }
}

public class AppMediator : IMediator
{
    private readonly IServiceProvider _serviceProvider;
    public AppMediator(IServiceProvider serviceProvider) => _serviceProvider = serviceProvider;

    public async Task<TResponse> SendAsync<TResponse>(IRequest<TResponse> request)
    {
        var handlerType = typeof(IRequestHandler<,>)
            .MakeGenericType(request.GetType(), typeof(TResponse));
        var handler = _serviceProvider.GetServices(handlerType).FirstOrDefault()
            ?? throw new InvalidOperationException("Handler not found.");
        var method = handlerType.GetMethod("HandleAsync")!;
        var task = (Task<TResponse>)method.Invoke(handler, new[] { request })!;
        return await task;
    }
}

public class CompleteBookingHandler
    : IRequestHandler<CompleteBookingRequest, CompleteBookingResponse>
{
    private readonly IBookingFacade _facade;
    private readonly IOrdersService _ordersService;
    public CompleteBookingHandler(IBookingFacade facade, IOrdersService ordersService)
        => (_facade, _ordersService) = (facade, ordersService);

    public async Task<CompleteBookingResponse> HandleAsync(CompleteBookingRequest request)
    {
        var pipeline = OrderPipelineBuilder.Build(_ordersService);
        var check = await pipeline.HandleAsync(
            new OrderPipelineRequest { Model = request.Model },
            new OrderPipelineResult { IsValid = true });
        if (!check.IsValid)
            return new CompleteBookingResponse { Success = false, Message = check.Message };

        var result = await _facade.ProcessBookingAsync(request.Model, request.UserId);
        return new CompleteBookingResponse
        {
            Success = result.Success, Message = result.Message,
            OrderId = result.OrderId, FinalPrice = result.FinalPrice,
            DiscountApplied = result.DiscountApplied,
            AppliedDiscounts = check.AppliedDiscounts
        };
    }
}

// Controller sau khi áp dụng
var response = await _mediator.SendAsync(new CompleteBookingRequest
{
    Model = model,
    UserId = User.Identity?.Name
});
return response.Success ? View("BookingCompleted") : BadRequest(response.Message);
```

## Biện luận (Giải thích)

**Bước 1:** Request/Response là object trung gian; handler tương ứng chứa nghiệp vụ của request.

**Bước 2:** `AppMediator` tìm handler từ DI bằng cặp kiểu request/response rồi gọi `HandleAsync`.

**Bước 3:** `CompleteBookingHandler` phối hợp Chain và Facade; Controller không cần biết hai subsystem liên hệ thế nào.

**Bước 4:** Thêm `CancelBookingRequest` hoặc `ConfirmBookingRequest` chỉ cần tạo request, response và handler; các Controller không gọi trực tiếp lẫn nhau.

**Bước 5:** Controller chỉ gửi request và nhận response. Luồng mới là `Controller → IMediator → Handler → Chain/Facade/Service → Response`.

---

# Kết luận và biện luận tổng hợp

Việc áp dụng 12 pattern giúp MovieCinema phân tách các trách nhiệm chính:

- **Singleton/DI:** quản lý vòng đời giỏ hàng theo session.
- **Bridge:** tách loại ghế khỏi thuật toán giá.
- **Decorator:** xếp chồng voucher, điểm và Happy Hour.
- **Proxy:** thêm cache mà không sửa MoviesService.
- **Strategy/Adapter:** thay thế và tích hợp nhiều cổng thanh toán.
- **Facade:** cung cấp một API đơn giản cho quy trình booking.
- **Builder:** xây dựng Order nhiều bước bằng Fluent Interface.
- **State:** kiểm soát transition của Order.
- **Observer:** mở rộng side effect khi trạng thái thay đổi.
- **Chain:** tách các bước validation thành pipeline độc lập.
- **Mediator:** giảm coupling giữa Controller, Chain, Facade và Service.

## So sánh trước và sau

| Tiêu chí | Trước khi áp dụng | Sau khi áp dụng |
|---|---|---|
| Controller | Chứa nhiều nghiệp vụ | Chủ yếu nhận request/trả response |
| Tính giá ghế | `switch` rải rác | Bridge + pricing strategies |
| Khuyến mãi | Nhiều `if` trong một method | Chuỗi Decorator mở rộng được |
| Thanh toán | `if/else` theo tên cổng | Strategy/Adapter thống nhất |
| Tạo Order | Nhiều tham số, dễ nhầm | Builder fluent, có validation |
| Trạng thái Order | `if/else` dài | State machine, transition rõ |
| Side effect | Gọi cứng trong Service | Observer độc lập |
| Kiểm tra booking | Inline trong Controller | Chain of Responsibility |
| Cache | Query DB mọi lần | Proxy kiểm tra cache trước |
| Điều phối | Controller gọi trực tiếp nhiều nơi | Mediator và Handler |
| Mở rộng | Phải sửa code cũ | Thường chỉ thêm class/handler mới |
| Kiểm thử | Mock nhiều dependency | Test từng pattern/handler riêng |

## Lưu ý về mức độ hoàn thiện của source code

1. `ShoppingCart.GetShoppingCart()` đang được đăng ký `Scoped` theo session; nên gọi chính xác là **session-scoped Singleton**, không phải Singleton global.
2. `CachedMoviesServiceProxy.InvalidateAllCaches()` hiện là placeholder, chưa xóa entry cụ thể. Khi production cần triển khai cache key registry, `MemoryCache` wrapper hoặc Redis tag.
3. `PayPalPaymentStrategy` hiện dùng `Task.Delay` làm stub. Adapter PayPal hoàn chỉnh cần gọi SDK thật, lấy secret từ configuration/secret store, timeout và xử lý lỗi.
4. `OrderSubject` có cơ chế lấy observer từ DI; cần bảo đảm observer scoped không bị giữ lại quá vòng đời request nếu đăng ký Subject là Singleton.
5. Một số class trong source vừa minh họa pattern vừa có logic nghiệp vụ thực tế; báo cáo phân biệt “ý tưởng pattern” với các điểm cần hoàn thiện khi đưa lên production.
