# Hướng dẫn áp dụng Design Patterns vào dự án MovieCinema

> Áp dụng 23 GoF Design Patterns cho hệ thống đặt vé xem phim .NET Core MVC
> Nguồn: "Design Patterns: Elements of Reusable Object-Oriented Software" -- Gamma, Helm, Johnson, Vlissides

---

## Mục lục

1. [Tổng quan kiến trúc hiện tại](#1-tổng-quan-kiến-trúc-hiện-tại)
2. [Creational Patterns](#2-creational-patterns-nhóm-khởi-tạo)
   - [Singleton — ShoppingCart](#21-singleton--shoppingcart)
   - [Factory Method — Service Registration](#22-factory-method--đăng-ký-service-tập-trung)
   - [Builder — Tạo Order phức tạp](#23-builder--tạo-order-phức-tạp)
   - [Abstract Factory — Multi-tenant Cinema](#24-abstract-factory--multi-tenant-cinema)
   - [Prototype — Sao chép Movie/Showtime](#25-prototype--sao-chép-movie)
3. [Structural Patterns](#3-structural-patterns-nhóm-cấu-trúc)
   - [Decorator — Mở rộng đơn hàng](#31-decorator--mở-rộng-đơn-hàng)
   - [Facade — Luồng đặt vé đơn giản](#32-facade--luồng-đặt-vé)
   - [Proxy — Cache & Lazy Load](#33-proxy--cache--lazy-load)
   - [Adapter — Tích hợp API ngoài](#34-adapter--tích-hợp-api-paypal)
   - [Bridge — Tách giá vé theo loại ghế](#35-bridge--tách-giá-vé-theo-loại-ghế)
   - [Composite — Ghế trong phòng chiếu](#36-composite--ghế-trong-phòng-chiếu)
4. [Behavioral Patterns](#4-behavioral-patterns-nhóm-hành-vi)
   - [Strategy — Thanh toán đa phương thức](#41-strategy--thanh-toán-đa-phương-thức)
   - [Observer — Thông báo trạng thái đơn hàng](#42-observer--thông-báo-trạng-thái-đơn-hàng)
   - [State — Quản lý trạng thái Order](#43-state--quản-lý-trạng-thái-order)
   - [Command — Hành động đặt vé](#44-command--hành-động-đặt-vé)
   - [Chain of Responsibility — Pipeline xử lý đơn hàng](#45-chain-of-responsibility--pipeline-xử-lý-đơn-hàng)
   - [Template Method — Báo cáo doanh thu](#46-template-method--báo-cáo-doanh-thu)
   - [Mediator — Giảm coupling giữa Controllers](#47-mediator--giảm-coupling-giữa-controllers)
   - [Visitor — Tính doanh thu theo nhiều chiều](#48-visitor--tính-doanh-thu-theo-nhiều-chiều)
5. [Sơ đồ quan hệ các Pattern](#5-sơ-đồ-quan-hệ-các-pattern)

---

## 1. Tổng quan kiến trúc hiện tại

```
┌─────────────────────────────────────────────────────────────┐
│                      Controllers (11)                       │
│   Movies | Orders | Actors | Cinemas | Showtimes | ...      │
└────────────────────────┬────────────────────────────────────┘
                         │ DI (AddScoped)
┌────────────────────────▼────────────────────────────────────┐
│                   Services Layer                            │
│  IMoviesService, IOrdersService, IShowtimesService, ...     │
│  (Đã có: Repository pattern qua IEntityBaseRepository)     │
└────────────────────────┬────────────────────────────────────┘
                         │
┌────────────────────────▼────────────────────────────────────┐
│                  AppDbContext (EF Core)                    │
│         SQL Server — MovieCinema Database                  │
└─────────────────────────────────────────────────────────────┘
```

**Điểm mạnh hiện tại:**
- Dependency Injection đã dùng tốt
- Service layer đã tách biệt interface/implementation
- Repository pattern cơ bản qua `IEntityBaseRepository`

**Điểm cần cải thiện:**
- Logic nghiệp vụ nằm trong Controller (OrdersController ~800 dòng)
- Ghép nối cứng: `ShoppingCart` truy cập trực tiếp DbContext
- Thiếu Strategy cho thanh toán → code `if/else` payment method rải khắp
- Thiếu State pattern → `ChangeOrderStatusAsync` chứa switch/if khổng lồ
- Thiếu Builder → tạo Order phức tạp, nhiều tham số
- Thiếu Observer → không thông báo khi trạng thái Order thay đổi
- Thiếu Decorator → mở rộng đơn hàng (voucher, điểm tích lũy) nằm chồng chất

---

## 2. Creational Patterns (Nhóm Khởi tạo)

### 2.1 Singleton — ShoppingCart

**Intent:** Đảm bảo một class chỉ có **một instance duy nhất** và cung cấp một điểm truy cập toàn cục.

**Áp dụng:** `ShoppingCart` hiện tại đã dùng singleton pattern qua `GetShoppingCart()` static method. Cải thiện thêm bằng cách đăng ký như một **真正的** Singleton trong DI container.

```csharp
// Data/Cart/ShoppingCart.cs
// ─── HIỆN TẠI (Partial Singleton) ───────────────────────
public class ShoppingCart
{
    public static ShoppingCart GetShoppingCart(IServiceProvider services)
    {
        // Mỗi lần gọi đều tạo instance mới nếu session chưa có CartId
        var session = services.GetRequiredService<IHttpContextAccessor>()?.HttpContext.Session;
        var context = services.GetService<AppDbContext>();
        string cartId = session.GetString("CartId") ?? Guid.NewGuid().ToString();
        session.SetString("CartId", cartId);
        return new ShoppingCart(context) { ShoppingCartId = cartId };
    }
    // ...
}

// ─── CẢI TIẾN: Đăng ký Singleton thực sự trong DI ───────
// Program.cs
builder.Services.AddSingleton<IShoppingCartAccessor, ShoppingCartAccessor>();

// Interface cho Singleton
public interface IShoppingCartAccessor
{
    ShoppingCart Current { get; }
}

// Singleton wrapper — giữ instance theo session
public class ShoppingCartAccessor : IShoppingCartAccessor
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly IServiceProvider _serviceProvider;

    public ShoppingCartAccessor(IHttpContextAccessor httpContextAccessor,
                                IServiceProvider serviceProvider)
    {
        _httpContextAccessor = httpContextAccessor;
        _serviceProvider = serviceProvider;
    }

    public ShoppingCart Current
    {
        get
        {
            var session = _httpContextAccessor.HttpContext?.Session;
            string cartId = session?.GetString("CartId");

            if (string.IsNullOrEmpty(cartId))
            {
                cartId = Guid.NewGuid().ToString();
                session?.SetString("CartId", cartId);
            }

            // Tái sử dụng instance cho cùng cartId
            var context = _serviceProvider.GetRequiredService<AppDbContext>();
            return new ShoppingCart(context) { ShoppingCartId = cartId };
        }
    }
}
```

**Tại sao tốt hơn:**
- Đưa vào DI container → test được, quản lý lifecycle rõ ràng
- `IShoppingCartAccessor` có thể mock trong unit test
- Logic tạo cartId tập trung ở một chỗ

---

### 2.2 Factory Method — Đăng ký Service tập trung

**Intent:** Định nghĩa interface để tạo object, nhưng để **subclass quyết định** class nào được khởi tạo.

**Áp dụng:** Thay vì đăng ký từng service riêng lẻ trong `Program.cs`, dùng Factory Method để scan và đăng ký tự động.

```csharp
// Infrastructure/Factory/ServiceFactory.cs
public interface IServiceFactory<TService>
{
    TService Create();
}

public interface IServiceRegistrar
{
    void Add(IServiceCollection services);
}

// Implementation: tự động đăng ký tất cả service theo convention
public static class ServiceRegistrationExtensions
{
    public static IServiceCollection AddMovieCinemaServices(
        this IServiceCollection services)
    {
        // Scan assembly và đăng ký theo convention
        // I[Entity]Service + [Entity]Service → Scoped
        var serviceInterfaceType = typeof(IEntityBaseRepository<>);
        var assembly = typeof(Program).Assembly;

        foreach (var serviceType in assembly.GetTypes())
        {
            var interfaces = serviceType.GetInterfaces()
                .Where(i => i.Name.StartsWith("I") &&
                           i.Name != nameof(IDisposable) &&
                           serviceType.Name.StartsWith(i.Name.Substring(1)));

            foreach (var iface in interfaces)
            {
                services.AddScoped(iface, serviceType);
            }
        }

        return services;
    }
}

// Program.cs — thay vì viết từng dòng:
// builder.Services.AddScoped<IMoviesService, MoviesService>();
// builder.Services.AddScoped<IActorsService, ActorsService>();
// ... (10+ dòng)

// → Chỉ cần một dòng:
builder.Services.AddMovieCinemaServices();
```

---

### 2.3 Builder — Tạo Order phức tạp

**Intent:** Tách rời việc xây dựng một object phức tạp khỏi biểu diễn của nó, để cùng một quy trình tạo ra các đại diện khác nhau.

**Áp dụng:** Tạo `Order` hiện tại có quá nhiều tham số, logic tính giá rải ở nhiều nơi. Builder giúp:

```csharp
// Models/Builders/OrderBuilder.cs
public class OrderBuilder
{
    private readonly Order _order = new();

    public OrderBuilder SetCustomer(string name, string email, string userId)
    {
        _order.Name = name;
        _order.Email = email;
        _order.UserId = userId;
        return this;
    }

    public OrderBuilder SetShowtime(int showtimeId, string selectedSeats,
                                     int seatCount, double basePrice)
    {
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

    public OrderBuilder ApplyVoucher(Voucher voucher, double currentTotal)
    {
        double discount = voucher.IsPercentage
            ? currentTotal * voucher.DiscountPercentage / 100.0
            : voucher.DiscountAmount;

        // Không cho vượt quá tổng
        discount = Math.Min(discount, currentTotal);

        _order.DiscountAmount = discount;
        return this;
    }

    public OrderBuilder RedeemPoints(int points, double total)
    {
        // 1 point = 1,000 VND
        double pointValue = points * 1000.0;
        _order.PointsRedeemed = Math.Min(pointValue, total);
        return this;
    }

    public OrderBuilder SetPaymentMethod(string method)
    {
        _order.PaymentMethod = method;
        return this;
    }

    public OrderBuilder CalculateTotal()
    {
        double subtotal = _order.OrderItems.Sum(i => i.Amount * i.Price);
        _order.TotalPrice = subtotal - _order.DiscountAmount
                         - _order.PointsRedeemed;
        if (_order.TotalPrice < 0) _order.TotalPrice = 0;
        return this;
    }

    public Order Build() => _order;
}

// Sử dụng trong OrdersController:
[HttpPost]
public async Task<IActionResult> BookTickets(BookTicketsVM model)
{
    var showtime = await _showtimesService.GetShowtimeByIdWithDetailsAsync(
        model.ShowtimeId);

    var order = new OrderBuilder()
        .SetCustomer(model.Name, model.Email, User.Identity?.Name)
        .SetShowtime(model.ShowtimeId, model.SelectedSeats,
                     selectedSeatsCount, showtime.Price)
        .ApplyVoucher(voucher, totalPrice)
        .RedeemPoints(model.PointsRedeemed, totalPrice)
        .SetPaymentMethod(model.PaymentMethod)
        .CalculateTotal()
        .Build();

    await _ordersService.CreateOrderAsync(order);
    return View("BookingCompleted");
}
```

**So sánh Before/After:**

| Before (Controller) | After (Builder) |
|---|---|
| 50+ dòng trong 1 action | 1 chain rõ ràng |
| Logic tính giá lặp lại | Tính giá tập trung trong Builder |
| Khó test từng bước | Test từng bước được |
| Thêm voucher/points → sửa nhiều chỗ | Thêm step mới vào chain |

---

### 2.4 Abstract Factory — Multi-tenant Cinema

**Intent:** Cung cấp interface để tạo **họ các object liên quan** mà không cần chỉ định class cụ thể.

**Áp dụng:** Hệ thống có nhiều cinema (Galaxy, Lotte, Cineplex), mỗi cinema có cách tính giá, chính sách hoàn vé khác nhau.

```csharp
// Infrastructure/CinemaFactory/ICinemaFactory.cs
public interface ICinemaFactory
{
    IPricingStrategy CreatePricingStrategy();
    IRefundPolicy CreateRefundPolicy();
    IMovieSelector CreateMovieSelector();
    string CinemaName { get; }
}

// Factory cụ thể cho từng cinema chain
public class GalaxyCinemaFactory : ICinemaFactory
{
    public string CinemaName => "Galaxy Cinema";

    public IPricingStrategy CreatePricingStrategy()
        => new GalaxyPricingStrategy();  // Giảm giá 20% vào thứ Ba

    public IRefundPolicy CreateRefundPolicy()
        => new FlexibleRefundPolicy();   // Hoàn 100% trước 24h

    public IMovieSelector CreateMovieSelector()
        => new StandardMovieSelector();
}

public class LotteCinemaFactory : ICinemaFactory
{
    public string CinemaName => "Lotte Cinema";

    public IPricingStrategy CreatePricingStrategy()
        => new LottePricingStrategy();   // Giảm 10% cho thành viên

    public IRefundPolicy CreateRefundPolicy()
        => new StrictRefundPolicy();      // Không hoàn sau khi mua

    public IMovieSelector CreateMovieSelector()
        => new PremiumMovieSelector();   // Chỉ phim >= 7 sao
}

// Đăng ký factory theo cinema
public class CinemaFactoryProvider
{
    private readonly Dictionary<string, ICinemaFactory> _factories;

    public CinemaFactoryProvider()
    {
        _factories = new Dictionary<string, ICinemaFactory>
        {
            ["Galaxy"] = new GalaxyCinemaFactory(),
            ["Lotte"] = new LotteCinemaFactory(),
            ["Cineplex"] = new CineplexCinemaFactory(),
        };
    }

    public ICinemaFactory GetFactory(string cinemaName)
        => _factories.GetValueOrDefault(cinemaName)
           ?? throw new ArgumentException($"Unknown cinema: {cinemaName}");

    public IEnumerable<string> AvailableCinemas => _factories.Keys;
}

// Sử dụng khi đặt vé:
public async Task<IActionResult> BookAtCinema(string cinemaName, int showtimeId)
{
    var factoryProvider = new CinemaFactoryProvider();
    var factory = factoryProvider.GetFactory(cinemaName);

    // Tính giá theo chính sách của cinema
    var pricing = factory.CreatePricingStrategy();
    double finalPrice = pricing.CalculatePrice(basePrice, seatType, showtime);

    // Áp dụng chính sách hoàn tiền
    var refund = factory.CreateRefundPolicy();
    ViewBag.CanRefund = refund.CanRefund(orderDate);
    ViewBag.RefundPercentage = refund.GetRefundPercentage(orderDate);
}
```

---

### 2.5 Prototype — Sao chép Movie

**Intent:** Chỉ định các loại object cần tạo bằng cách **sao chép (clone)** một instance nguyên mẫu, thay vì tạo mới từ class.

**Áp dụng:** Khi cần tạo một bản sao của Movie để sửa đổi (ví dụ: tạo lịch chiếu mùa mới từ template phim cũ).

```csharp
// Models/Prototypes/MoviePrototype.cs
public interface IPrototype<T>
{
    T Clone();
}

// Deep clone cho Movie
public class Movie : IPrototype<Movie>
{
    public int Id { get; set; }
    public string Name { get; set; }
    public string Description { get; set; }
    public double Price { get; set; }
    public string ImageURL { get; set; }
    public int Duration { get; set; }
    public MovieStatus Status { get; set; }
    public int CategoryId { get; set; }
    public int CinemaId { get; set; }
    public Category Category { get; set; }
    public Cinema Cinema { get; set; }
    public List<Actor_Movie> Actors_Movies { get; set; }

    public Movie Clone()
    {
        // Deep clone — sao chép tất cả thuộc tính, không share reference
        var clone = new Movie
        {
            Name = this.Name + " (Mùa mới)",
            Description = this.Description,
            Price = this.Price,
            ImageURL = this.ImageURL,
            Duration = this.Duration,
            Status = MovieStatus.ComingSoon,
            CategoryId = this.CategoryId,
            CinemaId = this.CinemaId,
            Category = this.Category,
            Cinema = this.Cinema,
            StartDate = DateTime.Today.AddMonths(1),
            EndDate = DateTime.Today.AddMonths(3),
            // Actors giữ nguyên
            Actors_Movies = this.Actors_Movies
                .Select(a => new Actor_Movie
                {
                    ActorId = a.ActorId,
                    Actor = a.Actor
                }).ToList()
        };
        return clone;
    }
}

// Sử dụng: tạo Movie mùa mới từ phim cũ
[HttpPost]
public async Task<IActionResult> CloneMovieForNewSeason(int movieId, DateTime startDate, DateTime endDate)
{
    var original = await _moviesService.GetMovieByIdAsync(movieId);
    var clone = original.Clone();
    clone.StartDate = startDate;
    clone.EndDate = endDate;
    await _moviesService.AddAsync(clone);
    return RedirectToAction(nameof(Index));
}
```

---

## 3. Structural Patterns (Nhóm Cấu trúc)

### 3.1 Decorator — Mở rộng đơn hàng

**Intent:** Gắn thêm **trách nhiệm bổ sung** (voucher, điểm tích lũy, khuyến mãi) vào object một cách linh hoạt, thay vì dùng kế thừa.

**Áp dụng:** Tính giá đơn hàng với nhiều loại khuyến mãi xếp chồng.

```csharp
// Decorators/OrderPricing/IOrderPriceDecorator.cs
public interface IOrderPriceDecorator
{
    double CalculatePrice(double currentPrice);
    string Description { get; }
}

// Decorator cơ sở
public abstract class OrderPriceDecorator : IOrderPriceDecorator
{
    protected IOrderPriceDecorator _inner;

    protected OrderPriceDecorator(IOrderPriceDecorator inner)
        => _inner = inner;

    public abstract double CalculatePrice(double currentPrice);
    public abstract string Description { get; }
}

// Decorator: Voucher giảm giá
public class VoucherDecorator : OrderPriceDecorator
{
    private readonly Voucher _voucher;

    public VoucherDecorator(IOrderPriceDecorator inner, Voucher voucher)
        : base(inner) => _voucher = voucher;

    public override double CalculatePrice(double currentPrice)
    {
        double discounted = _inner.CalculatePrice(currentPrice);
        if (discounted < _voucher.MinOrderAmount) return discounted;

        double reduction = _voucher.IsPercentage
            ? discounted * _voucher.DiscountPercentage / 100.0
            : _voucher.DiscountAmount;

        return discounted - Math.Min(reduction, discounted);
    }

    public override string Description
        => _voucher.IsPercentage
            ? $"Voucher {_voucher.DiscountPercentage}% (-{_voucher.Code})"
            : $"Voucher giảm {_voucher.DiscountAmount:N0}đ (-{_voucher.Code})";
}

// Decorator: Điểm tích lũy
public class LoyaltyPointsDecorator : OrderPriceDecorator
{
    private readonly int _points;

    public LoyaltyPointsDecorator(IOrderPriceDecorator inner, int points)
        : base(inner) => _points = points;

    public override double CalculatePrice(double currentPrice)
    {
        double afterLoyalty = _inner.CalculatePrice(currentPrice);
        double pointValue = _points * 1000.0; // 1 point = 1,000 VND
        return Math.Max(0, afterLoyalty - pointValue);
    }

    public override string Description
        => $"Điểm tích lũy (-{_points * 1000:N0}đ)";
}

// Decorator: Khuyến mãi theo thời điểm
public class HappyHourDecorator : OrderPriceDecorator
{
    private readonly TimeSpan _start;
    private readonly TimeSpan _end;
    private readonly double _discountPercent;

    public HappyHourDecorator(IOrderPriceDecorator inner,
        TimeSpan start, TimeSpan end, double discountPercent)
        : base(inner)
    {
        _start = start;
        _end = end;
        _discountPercent = discountPercent;
    }

    public override double CalculatePrice(double currentPrice)
    {
        var now = DateTime.Now.TimeOfDay;
        if (now >= _start && now <= _end)
        {
            double basePrice = _inner.CalculatePrice(currentPrice);
            return basePrice * (1 - _discountPercent / 100.0);
        }
        return _inner.CalculatePrice(currentPrice);
    }

    public override string Description
        => $"Happy Hour giảm {_discountPercent}%";
}

// Sử dụng: xếp chồng decorators
public class OrderPricingService
{
    public double CalculateFinalPrice(double basePrice, Voucher voucher,
                                      int loyaltyPoints, bool isHappyHour)
    {
        // Bắt đầu với giá gốc
        IOrderPriceDecorator pricing = new BasePriceCalculator(basePrice);

        // Áp dụng voucher
        if (voucher != null)
            pricing = new VoucherDecorator(pricing, voucher);

        // Áp dụng điểm tích lũy
        if (loyaltyPoints > 0)
            pricing = new LoyaltyPointsDecorator(pricing, loyaltyPoints);

        // Áp dụng happy hour
        if (isHappyHour)
            pricing = new HappyHourDecorator(pricing,
                new TimeSpan(14, 0, 0),
                new TimeSpan(17, 0, 0),
                15.0); // Giảm 15%

        return pricing.CalculatePrice(basePrice);
    }
}
```

---

### 3.2 Facade — Luồng đặt vé

**Intent:** Cung cấp một interface **thống nhất** cho một tập hợp các interface phức tạp trong subsystem, giúp client giao tiếp đơn giản hơn.

**Áp dụng:** `BookTickets` hiện tại cần gọi nhiều service. Facade gói gọn toàn bộ luồng đặt vé phức tạp.

```csharp
// Facade/BookingFacade.cs
public interface IBookingFacade
{
    Task<BookingResult> ProcessBookingAsync(BookTicketsVM model, string userId);
    Task<BookingSummary> GetBookingSummaryAsync(int showtimeId);
}

public class BookingResult
{
    public bool Success { get; set; }
    public string Message { get; set; }
    public Order Order { get; set; }
    public double FinalPrice { get; set; }
    public double DiscountApplied { get; set; }
}

public class BookingFacade : IBookingFacade
{
    private readonly IOrdersService _ordersService;
    private readonly IShowtimesService _showtimesService;
    private readonly ISeatsService _seatsService;
    private readonly IEntityBaseRepository<Order> _orderRepo;

    public BookingFacade(
        IOrdersService ordersService,
        IShowtimesService showtimesService,
        ISeatsService seatsService,
        IEntityBaseRepository<Order> orderRepo)
    {
        _ordersService = ordersService;
        _showtimesService = showtimesService;
        _seatsService = seatsService;
        _orderRepo = orderRepo;
    }

    public async Task<BookingResult> ProcessBookingAsync(
        BookTicketsVM model, string userId)
    {
        // 1. Lấy thông tin showtime
        var showtime = await _showtimesService
            .GetShowtimeByIdWithDetailsAsync(model.ShowtimeId);
        if (showtime == null)
            return new BookingResult { Success = false, Message = "Suất chiếu không tồn tại." };

        // 2. Parse ghế đã chọn
        var selectedSeats = model.SelectedSeats
            .Split(',').Select(s => s.Trim()).Where(s => !string.IsNullOrEmpty(s)).ToList();

        // 3. Kiểm tra ghế còn trống
        var bookedSeats = await _ordersService
            .GetBookedSeatsForShowtimeAsync(model.ShowtimeId);
        foreach (var seat in selectedSeats)
            if (bookedSeats.Contains(seat))
                return new BookingResult
                {
                    Success = false,
                    Message = $"Ghế {seat} đã được đặt."
                };

        // 4. Tính giá theo loại ghế
        var roomSeats = await _seatsService.GetSeatsByRoomAsync(showtime.CinemaRoomId);
        double totalPrice = 0;
        foreach (var seatCode in selectedSeats)
        {
            var seat = roomSeats.FirstOrDefault(s =>
                (s.Row + s.Number.ToString()) == seatCode);
            totalPrice += seat?.SeatType switch
            {
                SeatType.VIP => showtime.Price * 1.2,
                SeatType.Couple => showtime.Price * 2.0,
                _ => showtime.Price
            };
        }

        // 5. Áp dụng voucher
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

        // 6. Tạo order
        var order = await _ordersService.StoreDirectOrderAsync(
            model.ShowtimeId, model.Name, model.Email,
            model.SelectedSeats, selectedSeats.Count,
            totalPrice, discount, model.PointsRedeemed,
            model.PaymentMethod ?? "PayPal");

        return new BookingResult
        {
            Success = true,
            Message = "Đặt vé thành công!",
            Order = order,
            FinalPrice = totalPrice - discount,
            DiscountApplied = discount
        };
    }

    public async Task<BookingSummary> GetBookingSummaryAsync(int showtimeId)
    {
        var showtime = await _showtimesService
            .GetShowtimeByIdWithDetailsAsync(showtimeId);
        var seats = await _seatsService.GetSeatsByRoomAsync(
            showtime.CinemaRoomId);
        var booked = await _ordersService
            .GetBookedSeatsForShowtimeAsync(showtimeId);

        return new BookingSummary
        {
            Movie = showtime.Movie,
            Cinema = showtime.CinemaRoom?.Cinema,
            Room = showtime.CinemaRoom?.Name,
            StartTime = showtime.StartTime,
            BasePrice = showtime.Price,
            TotalSeats = seats.Count,
            AvailableSeats = seats.Count(s =>
                s.IsAvailable && !booked.Contains(s.Row + s.Number))
        };
    }
}

// OrdersController đơn giản hóa:
private readonly IBookingFacade _bookingFacade;

[HttpPost]
public async Task<IActionResult> BookTickets(BookTicketsVM model)
{
    var result = await _bookingFacade.ProcessBookingAsync(
        model, User.Identity?.Name);

    if (!result.Success)
    {
        TempData["BookingError"] = result.Message;
        return RedirectToAction(nameof(BookTickets),
            new { showtimeId = model.ShowtimeId });
    }

    ViewBag.FinalPrice = result.FinalPrice;
    ViewBag.DiscountApplied = result.DiscountApplied;
    return View("BookingCompleted", result.Order);
}
```

**Trước:** `BookTickets` POST action ~90 dòng
**Sau:** Controller action ~15 dòng, logic nghiệp vụ trong Facade

---

### 3.3 Proxy — Cache & Lazy Load

**Intent:** Cung cấp một surrogate (đại diện) thay thế cho object thực để kiểm soát truy cập, lazy load, hoặc caching.

**Áp dụng:** Cache danh sách Movie (đọc nhiều, ít thay đổi) và Lazy Load Showtimes.

```csharp
// Proxy/CachedMoviesService.cs

// Proxy cho IMoviesService — cache kết quả
public class CachedMoviesServiceProxy : IMoviesService
{
    private readonly IMoviesService _realService;
    private readonly IMemoryCache _cache;
    private static readonly TimeSpan CacheDuration = TimeSpan.FromMinutes(10);

    public CachedMoviesServiceProxy(IMoviesService realService,
                                    IMemoryCache cache)
    {
        _realService = realService;
        _cache = cache;
    }

    public async Task<IEnumerable<Movie>> GetAllAsync()
    {
        // Cache toàn bộ danh sách phim — cache key đổi khi có phim mới
        return await _cache.GetOrCreateAsync("all_movies", async entry =>
        {
            entry.SlidingExpiration = CacheDuration;
            return await _realService.GetAllAsync();
        });
    }

    public async Task<Movie> GetByIdAsync(int id)
    {
        string key = $"movie_{id}";
        return await _cache.GetOrCreateAsync(key, async entry =>
        {
            entry.SlidingExpiration = CacheDuration;
            return await _realService.GetByIdAsync(id);
        });
    }

    // Với NowShowing — cache theo ngày
    public async Task<IEnumerable<Movie>> GetNowShowingMoviesAsync()
    {
        string key = $"now_showing_{DateTime.Today:yyyyMMdd}";
        return await _cache.GetOrCreateAsync(key, async entry =>
        {
            entry.SlidingExpiration = CacheDuration;
            return await _realService.GetNowShowingMoviesAsync();
        });
    }
}

// Proxy cho ShowtimesService — lazy load chi tiết
public class LazyShowtimesServiceProxy : IShowtimesService
{
    private readonly IShowtimesService _realService;

    public LazyShowtimesServiceProxy(IShowtimesService realService)
        => _realService = realService;

    // Chỉ load đầy đủ khi cần (Entity Framework lazy loading)
    public async Task<Showtime> GetShowtimeByIdWithDetailsAsync(int id)
    {
        // Lazy load — không Include tất cả navigation properties
        // nếu không cần. Giảm query DB không cần thiết.
        var showtime = await _realService.GetByIdAsync(id);
        return showtime;
    }

    // Eager load khi cần đầy đủ thông tin (cho báo cáo)
    public async Task<Showtime> GetShowtimeFullDetailsAsync(int id)
    {
        // Load đầy đủ — explicit loading khi cần
        return await _realService.GetShowtimeByIdWithDetailsAsync(id);
    }
}

// Đăng ký Proxy trong Program.cs
builder.Services.AddMemoryCache();
builder.Services.AddScoped<IMoviesService, MoviesService>();
builder.Services.AddScoped<IMoviesService>(sp =>
    new CachedMoviesServiceProxy(
        sp.GetRequiredService<MoviesService>(),
        sp.GetRequiredService<IMemoryCache>()));
```

---

### 3.4 Adapter — Tích hợp API PayPal

**Intent:** Chuyển đổi interface của một class thành interface mà client **mong đợi**, cho phép các class không tương thích làm việc cùng nhau.

**Áp dụng:** Tích hợp PayPal API (interface PayPal không match với hệ thống thanh toán nội bộ).

```csharp
// Adapters/Payment/IPaymentGateway.cs
// Interface mà hệ thống internal mong đợi
public interface IPaymentGateway
{
    Task<PaymentResult> ProcessPaymentAsync(double amount,
        string currency, string orderId);
    Task<RefundResult> ProcessRefundAsync(string transactionId,
        double amount);
    PaymentMethod Type { get; }
}

public enum PaymentMethod { Cash, PayPal, CreditCard, MoMo }

// PayPal Adapter — bao bọc PayPal SDK
public class PayPalAdapter : IPaymentGateway
{
    public PaymentMethod Type => PaymentMethod.PayPal;

    public async Task<PaymentResult> ProcessPaymentAsync(
        double amount, string currency, string orderId)
    {
        // PayPal SDK gốc có API khác (PayPal.NET SDK)
        var paypalClient = new PayPalHttpClient();
        var request = new OrdersCreateRequest();
        request.RequestBody(new OrderRequest
        {
            Intent = "CAPTURE",
            PurchaseUnits = new[]
            {
                new PurchaseUnitRequest
                {
                    AmountWithBreakdown =
                        new AmountWithBreakdown { CurrencyCode = currency,
                                                 Value = amount.ToString("F2") }
                }
            }
        });

        var response = await paypalClient.Execute(request);

        return new PaymentResult
        {
            Success = response.Result<PayPalOrder>().Status == "COMPLETED",
            TransactionId = response.Result<PayPalOrder>().Id,
            Message = response.Result<PayPalOrder>().Status
        };
    }

    public async Task<RefundResult> ProcessRefundAsync(
        string transactionId, double amount)
    {
        var capture = new CapturesRefundRequest(transactionId,
            new RefundRequest
            {
                Amount =
                    new Money { CurrencyCode = "VND",
                                Value = amount.ToString("F2") }
            });

        var response = await paypalClient.Execute(capture);
        return new RefundResult
        {
            Success = response.Result<Refund>().Status == "COMPLETED",
            RefundId = response.Result<Refund>().Id
        };
    }
}

// Cash Adapter (thanh toán tại quầy)
public class CashAdapter : IPaymentGateway
{
    public PaymentMethod Type => PaymentMethod.Cash;

    public Task<PaymentResult> ProcessPaymentAsync(double amount,
        string currency, string orderId)
    {
        // Tiền mặt — không cần xử lý online
        return Task.FromResult(new PaymentResult
        {
            Success = true,
            TransactionId = $"CASH-{orderId}",
            Message = "Thanh toán tại quầy"
        });
    }

    public Task<RefundResult> ProcessRefundAsync(string transactionId,
        double amount)
    {
        return Task.FromResult(new RefundResult
        {
            Success = true,
            RefundId = $"REFUND-{transactionId}"
        });
    }
}

// Payment Factory — chọn adapter phù hợp
public class PaymentGatewayFactory
{
    public IPaymentGateway GetGateway(string paymentMethod)
    {
        return paymentMethod?.ToLower() switch
        {
            "paypal" => new PayPalAdapter(),
            "cash" => new CashAdapter(),
            _ => new CashAdapter()
        };
    }
}
```

---

### 3.5 Bridge — Tách giá vé theo loại ghế

**Intent:** Tách **abstraction** (cách tính giá) khỏi **implementation** (quy tắc tính giá cụ thể), để cả hai có thể thay đổi độc lập.

**Áp dụng:** Tính giá ghế — Abstraction = `SeatPricing`, Implementation = các chiến lược tính giá riêng biệt.

```csharp
// Bridge/SeatPricing/ISeatingPricingStrategy.cs
// Implementation hierarchy
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
    public double CalculatePrice(double basePrice) => basePrice * 0.5; // Giảm 50%
    public string SeatTypeName => "Khuyết tật";
}

// Abstraction
public class SeatPricingBridge
{
    private readonly ISeatingPricingStrategy _strategy;

    public SeatPricingBridge(ISeatingPricingStrategy strategy)
        => _strategy = strategy;

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

// Sử dụng trong OrderItem:
public double CalculateItemPrice(Showtime showtime, Seat seat)
{
    var bridge = new SeatPricingBridge(seat.SeatType);
    return bridge.GetPrice(showtime.Price);
}
```

---

### 3.6 Composite — Ghế trong phòng chiếu

**Intent:** Compose objects thành **cấu trúc cây** để biểu diễn hierarchy part-whole. Client có thể xử lý các object đơn lẻ và composite một cách thống nhất.

**Áp dụng:** Quản lý ghế theo Hàng (Row) — mỗi hàng chứa nhiều ghế.

```csharp
// Composite/Seating/ITheaterComponent.cs
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
    public bool IsAvailableSeat { get; set; }

    public double GetPrice(double basePrice)
    {
        return SeatType switch
        {
            SeatType.VIP => basePrice * 1.2,
            SeatType.Couple => basePrice * 2.0,
            SeatType.Disabled => basePrice * 0.5,
            _ => basePrice
        };
    }

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

    // Trả về tất cả ghế trong hàng
    public IEnumerable<Seat> GetAvailableSeats(List<string> bookedSeatCodes)
        => Seats.Where(s => s.IsAvailable(bookedSeatCodes));
}

public class CinemaRoomComposite : ITheaterComponent
{
    public string Name { get; set; }
    public List<SeatRow> Rows { get; set; } = new();

    public string Code => Name;

    public double GetTotalRevenue(double basePrice)
        => Rows.Sum(r => r.GetPrice(basePrice));

    public bool IsFull(List<string> bookedSeatCodes)
        => !Rows.SelectMany(r => r.GetAvailableSeats(bookedSeatCodes)).Any();

    public int CountSeats() => Rows.Sum(r => r.CountSeats());

    // Interface để thống nhất xử lý
    public double GetPrice(double basePrice)
        => Rows.Sum(r => r.GetPrice(basePrice));

    public bool IsAvailable(List<string> bookedSeatCodes)
        => Rows.Any(r => r.IsAvailable(bookedSeatCodes));
}
```

---

## 4. Behavioral Patterns (Nhóm Hành vi)

### 4.1 Strategy — Thanh toán đa phương thức

**Intent:** Định nghĩa một **họ thuật toán** (thanh toán PayPal, Cash, MoMo), đóng gói từng thuật toán, và làm cho chúng **có thể thay thế được** tại runtime.

**Áp dụng:** Hiện tại code thanh toán nằm rải trong Controller và Service. Strategy đóng gói từng phương thức thanh toán riêng biệt.

```csharp
// Strategy/Payment/IPaymentStrategy.cs
public interface IPaymentStrategy
{
    string Name { get; }
    Task<PaymentResponse> PayAsync(double amount, string orderId);
    Task<RefundResponse> RefundAsync(string transactionId, double amount);
}

public class CashPaymentStrategy : IPaymentStrategy
{
    public string Name => "Thanh toán tại rạp";

    public Task<PaymentResponse> PayAsync(double amount, string orderId)
        => Task.FromResult(new PaymentResponse
        {
            Success = true,
            TransactionId = $"CASH-{orderId}",
            Message = "Vui lòng thanh toán khi nhận vé."
        });

    public Task<RefundResponse> RefundAsync(string transactionId, double amount)
        => Task.FromResult(new RefundResponse { Success = true });
}

public class PayPalPaymentStrategy : IPaymentStrategy
{
    private readonly string _clientId;
    private readonly string _secret;

    public PayPalPaymentStrategy(string clientId, string secret)
    {
        _clientId = clientId;
        _secret = secret;
    }

    public string Name => "PayPal";

    public async Task<PaymentResponse> PayAsync(double amount, string orderId)
    {
        // Gọi PayPal API
        var accessToken = await GetAccessTokenAsync();
        var response = await CallPayPalApiAsync(accessToken, amount, orderId);
        return new PaymentResponse
        {
            Success = response.Status == "COMPLETED",
            TransactionId = response.Id,
            Message = response.Status
        };
    }

    public async Task<RefundResponse> RefundAsync(string transactionId, double amount)
    {
        var accessToken = await GetAccessTokenAsync();
        await CallPayPalRefundApiAsync(accessToken, transactionId, amount);
        return new RefundResponse { Success = true };
    }

    private Task<string> GetAccessTokenAsync() => Task.FromResult(_clientId);
    private Task<dynamic> CallPayPalApiAsync(string token, double amt, string oid)
        => Task.FromResult<dynamic>(new { Id = $"PP-{oid}", Status = "COMPLETED" });
    private Task CallPayPalRefundApiAsync(string token, string tid, double amt)
        => Task.CompletedTask;
}

// Context — chọn strategy tại runtime
public class PaymentContext
{
    private IPaymentStrategy _strategy;

    public void SetStrategy(IPaymentStrategy strategy)
        => _strategy = strategy;

    public void SetStrategyByName(string name)
    {
        _strategy = name.ToLower() switch
        {
            "paypal" => new PayPalPaymentStrategy("client_id", "secret"),
            "cash" => new CashPaymentStrategy(),
            _ => new CashPaymentStrategy()
        };
    }

    public async Task<PaymentResponse> Pay(double amount, string orderId)
        => await _strategy.PayAsync(amount, orderId);

    public async Task<RefundResponse> Refund(string transactionId, double amount)
        => await _strategy.RefundAsync(transactionId, amount);
}

// Sử dụng trong OrdersController:
private readonly PaymentContext _paymentContext;

[HttpPost]
public async Task<IActionResult> CompleteOrder(...)
{
    _paymentContext.SetStrategyByName(paymentMethod);
    var result = await _paymentContext.Pay(totalPrice, orderId);
    // ...
}
```

---

### 4.2 Observer — Thông báo trạng thái đơn hàng

**Intent:** Định nghĩa subscription **một-nhiều** giữa object (Subject) và các object phụ thuộc (Observers), sao cho khi Subject thay đổi, tất cả Observers được **tự động thông báo**.

**Áp dụng:** Khi trạng thái Order thay đổi (Purchased → Confirmed → Cancelled → Refunded), thông báo cho khách hàng và cập nhật điểm tích lũy.

```csharp
// Observer/IOrderObserver.cs
public interface IOrderObserver
{
    Task OnOrderStatusChangedAsync(Order order, string oldStatus,
                                   string newStatus);
}

// Subject quản lý observers
public class OrderSubject
{
    private readonly List<IOrderObserver> _observers = new();
    private readonly IServiceScopeFactory _scopeFactory;

    public OrderSubject(IServiceScopeFactory scopeFactory)
        => _scopeFactory = scopeFactory;

    public void Attach(IOrderObserver observer)
        => _observers.Add(observer);

    public void Detach(IOrderObserver observer)
        => _observers.Remove(observer);

    public async Task NotifyAsync(Order order, string oldStatus,
                                  string newStatus)
    {
        foreach (var observer in _observers)
        {
            // Tạo scope mới cho mỗi observer (tránh dispose context)
            using var scope = _scopeFactory.CreateScope();
            var scopedObserver = CreateScopedObserver(observer, scope);
            if (scopedObserver != null)
                await scopedObserver.OnOrderStatusChangedAsync(order,
                    oldStatus, newStatus);
        }
    }

    private IOrderObserver CreateScopedObserver(IOrderObserver observer,
        IServiceScope scope) => observer;
}

// Observer 1: Gửi email thông báo
public class EmailNotificationObserver : IOrderObserver
{
    private readonly IEmailService _emailService;

    public EmailNotificationObserver(IEmailService emailService)
        => _emailService = emailService;

    public async Task OnOrderStatusChangedAsync(Order order,
        string oldStatus, string newStatus)
    {
        var subject = newStatus switch
        {
            "Confirmed" => "Xác nhận đơn hàng #" + order.Id,
            "Cancelled" => "Đơn hàng #" + order.Id + " đã bị hủy",
            "Refunded" => "Hoàn tiền cho đơn hàng #" + order.Id,
            _ => $"Cập nhật đơn hàng #" + order.Id
        };

        var body = $"Xin chào {order.Name}, đơn hàng của bạn đã được " +
                   $"chuyển từ [{oldStatus}] → [{newStatus}].";

        await _emailService.SendAsync(order.Email, subject, body);
    }
}

// Observer 2: Cập nhật điểm tích lũy thành viên
public class LoyaltyPointsObserver : IOrderObserver
{
    public async Task OnOrderStatusChangedAsync(Order order,
        string oldStatus, string newStatus)
    {
        // Xử lý điểm tích lũy trong scope riêng
        using var scope = _scopeFactory.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var member = await context.Members
            .FirstOrDefaultAsync(m => m.Email == order.Email);

        if (member == null) return;

        if (newStatus == "Cancelled" || newStatus == "Refunded")
        {
            // Hoàn lại điểm đã dùng
            member.Points += (int)(order.PointsRedeemed / 1000);
        }
        else if (newStatus == "Confirmed")
        {
            // Cộng điểm mới: 1 điểm / 10,000 VND
            int earned = (int)(order.TotalPrice / 10000);
            member.Points += earned;
        }

        await context.SaveChangesAsync();
    }
}

// Observer 3: Ghi log audit
public class AuditLogObserver : IOrderObserver
{
    private readonly ILogger<AuditLogObserver> _logger;

    public AuditLogObserver(ILogger<AuditLogObserver> logger)
        => _logger = logger;

    public Task OnOrderStatusChangedAsync(Order order,
        string oldStatus, string newStatus)
    {
        _logger.LogInformation(
            "Order #{OrderId} status changed: {Old} → {New} by {Email}",
            order.Id, oldStatus, newStatus, order.Email);
        return Task.CompletedTask;
    }
}

// Đăng ký trong Program.cs
builder.Services.AddScoped<IOrderObserver, EmailNotificationObserver>();
builder.Services.AddScoped<IOrderObserver, LoyaltyPointsObserver>();
builder.Services.AddScoped<IOrderObserver, AuditLogObserver>();
```

---

### 4.3 State — Quản lý trạng thái Order

**Intent:** Cho phép object **thay đổi hành vi** khi trạng thái nội bộ thay đổi — object có vẻ như đã thay đổi class.

**Áp dụng:** Mỗi trạng thái Order (Purchased, Confirmed, Cancelled, Refunded) có logic xử lý khác nhau.

```csharp
// State/OrderState/IOrderState.cs
public interface IOrderState
{
    string StatusName { get; }
    bool CanConfirm(Order order);
    bool CanCancel(Order order);
    bool CanRefund(Order order);
    Task OnEnterAsync(Order order);
}

// State: Purchased (mới đặt, chờ xác nhận)
public class PurchasedState : IOrderState
{
    public string StatusName => "Purchased";

    public bool CanConfirm(Order order) => true;
    public bool CanCancel(Order order) => true;   // Khách hủy được
    public bool CanRefund(Order order) => false;

    public Task OnEnterAsync(Order order)
    {
        // Khi đặt thành công: gửi email xác nhận
        // _emailService.Send(order.Email, "Xác nhận đơn hàng");
        return Task.CompletedTask;
    }
}

// State: Confirmed (đã xác nhận, vé được phát hành)
public class ConfirmedState : IOrderState
{
    public string StatusName => "Confirmed";

    public bool CanConfirm(Order order) => false;
    public bool CanCancel(Order order) => true;   // Admin hủy được
    public bool CanRefund(Order order) => true;    // Hoàn tiền được

    public Task OnEnterAsync(Order order)
    {
        // Sinh mã QR vé, gửi cho khách
        // _ticketService.GenerateQrCode(order.Id);
        return Task.CompletedTask;
    }
}

// State: Cancelled (đã hủy)
public class CancelledState : IOrderState
{
    public string StatusName => "Cancelled";

    public bool CanConfirm(Order order) => false;
    public bool CanCancel(Order order) => false;
    public bool CanRefund(Order order) => false;

    public Task OnEnterAsync(Order order)
    {
        // Giải phóng ghế, hoàn điểm
        // _seatService.ReleaseSeats(order);
        return Task.CompletedTask;
    }
}

// State: Refunded (đã hoàn tiền)
public class RefundedState : IOrderState
{
    public string StatusName => "Refunded";

    public bool CanConfirm(Order order) => false;
    public bool CanCancel(Order order) => false;
    public bool CanRefund(Order order) => false;

    public Task OnEnterAsync(Order order)
    {
        // Gửi email thông báo hoàn tiền
        return Task.CompletedTask;
    }
}

// Context — chứa state hiện tại
public class OrderStateContext
{
    private IOrderState _state;
    private readonly Dictionary<string, IOrderState> _states;

    public OrderStateContext()
    {
        _states = new Dictionary<string, IOrderState>
        {
            ["Purchased"] = new PurchasedState(),
            ["Confirmed"] = new ConfirmedState(),
            ["Cancelled"] = new CancelledState(),
            ["Refunded"] = new RefundedState(),
        };
        _state = _states["Purchased"];
    }

    public string CurrentStatus => _state.StatusName;

    public void SetState(string statusName)
    {
        if (_states.TryGetValue(statusName, out var newState))
        {
            _state = newState;
        }
    }

    public bool CanConfirm() => _state.CanConfirm(null);
    public bool CanCancel() => _state.CanCancel(null);
    public bool CanRefund() => _state.CanRefund(null);

    public bool TransitionTo(Order order, string newStatus)
    {
        // Kiểm tra transition hợp lệ
        var validTransitions = new Dictionary<string, string[]>
        {
            ["Purchased"] = new[] { "Confirmed", "Cancelled" },
            ["Confirmed"] = new[] { "Cancelled", "Refunded" },
            ["Cancelled"] = Array.Empty<string>(),
            ["Refunded"] = Array.Empty<string>(),
        };

        if (!validTransitions[_state.StatusName].Contains(newStatus))
            return false;

        _state = _states[newStatus];
        return true;
    }
}

// Sử dụng trong OrdersService:
public class OrderStateService
{
    private readonly AppDbContext _context;
    private readonly OrderStateContext _stateContext;

    public async Task<bool> ChangeOrderStatusAsync(int orderId,
        string newStatus)
    {
        var order = await _context.Orders.FindAsync(orderId);
        if (order == null) return false;

        if (!_stateContext.TransitionTo(order, newStatus))
            return false; // Transition không hợp lệ

        string oldStatus = order.Status;
        order.Status = newStatus;
        await _context.SaveChangesAsync();

        // Gọi OnEnter của state mới
        var state = _states[newStatus];
        await state.OnEnterAsync(order);

        return true;
    }
}
```

---

### 4.4 Command — Hành động đặt vé

**Intent:** Đóng gói một **yêu cầu** thành một object, cho phép parameterize các client với yêu cầu, queue hoặc log yêu cầu, và hỗ trợ undo.

**Áp dụng:** Đóng gói các hành động đặt vé, hủy, hoàn tiền thành Command objects để có thể log, undo, và queue.

```csharp
// Command/ICommand.cs
public interface ICommand
{
    Task ExecuteAsync();
    Task UndoAsync();
    string Description { get; }
}

public class BookTicketCommand : ICommand
{
    private readonly AppDbContext _context;
    private readonly int _showtimeId;
    private readonly string _selectedSeats;
    private readonly string _customerName;
    private readonly string _customerEmail;
    private Order _createdOrder;

    public string Description => $"Đặt vé suất {_showtimeId} - {_selectedSeats}";

    public BookTicketCommand(AppDbContext context, int showtimeId,
        string selectedSeats, string customerName, string customerEmail)
    {
        _context = context;
        _showtimeId = showtimeId;
        _selectedSeats = selectedSeats;
        _customerName = customerName;
        _customerEmail = customerEmail;
    }

    public async Task ExecuteAsync()
    {
        var order = new Order
        {
            Email = _customerEmail,
            Name = _customerName,
            OrderDate = DateTime.Now,
            Status = "Purchased",
            PaymentMethod = "PayPal"
        };
        _context.Orders.Add(order);
        await _context.SaveChangesAsync();
        _createdOrder = order;
    }

    public async Task UndoAsync()
    {
        if (_createdOrder != null)
        {
            _createdOrder.Status = "Cancelled";
            await _context.SaveChangesAsync();
        }
    }
}

// Invoker — quản lý lịch sử command
public class CommandInvoker
{
    private readonly Stack<ICommand> _history = new();
    private readonly Stack<ICommand> _redoStack = new();

    public void ExecuteCommand(ICommand command)
    {
        command.ExecuteAsync().Wait();
        _history.Push(command);
        _redoStack.Clear(); // Xóa redo khi có command mới
    }

    public void Undo()
    {
        if (_history.Count > 0)
        {
            var command = _history.Pop();
            command.UndoAsync().Wait();
            _redoStack.Push(command);
        }
    }

    public void Redo()
    {
        if (_redoStack.Count > 0)
        {
            var command = _redoStack.Pop();
            command.ExecuteAsync().Wait();
            _history.Push(command);
        }
    }

    public IEnumerable<ICommand> GetHistory() => _history.Reverse();
}
```

---

### 4.5 Chain of Responsibility — Pipeline xử lý đơn hàng

**Intent:** Chuyển yêu cầu qua một **chuỗi handlers** cho đến khi một handler xử lý được.

**Áp dụng:** Pipeline xử lý đơn hàng: Validate → Check Seat Availability → Apply Voucher → Calculate Price → Process Payment.

```csharp
// ChainOfResponsibility/OrderPipeline/OrderHandler.cs
public abstract class OrderHandler
{
    protected OrderHandler _next;

    public OrderHandler SetNext(OrderHandler next)
    {
        _next = next;
        return next;
    }

    public abstract Task<OrderResult> HandleAsync(BookTicketsVM model);
}

public class ValidationHandler : OrderHandler
{
    public override async Task<OrderResult> HandleAsync(BookTicketsVM model)
    {
        if (!ModelState.IsValid)
            return new OrderResult(false, "Dữ liệu không hợp lệ.");

        if (string.IsNullOrEmpty(model.SelectedSeats))
            return new OrderResult(false, "Vui lòng chọn ít nhất một ghế.");

        return _next != null
            ? await _next.HandleAsync(model)
            : OrderResult.Success();
    }
}

public class SeatAvailabilityHandler : OrderHandler
{
    private readonly IOrdersService _ordersService;

    public SeatAvailabilityHandler(IOrdersService ordersService)
        => _ordersService = ordersService;

    public override async Task<OrderResult> HandleAsync(BookTicketsVM model)
    {
        var booked = await _ordersService
            .GetBookedSeatsForShowtimeAsync(model.ShowtimeId);
        var selected = model.SelectedSeats
            .Split(',').Select(s => s.Trim()).ToList();

        foreach (var seat in selected)
            if (booked.Contains(seat))
                return new OrderResult(false, $"Ghế {seat} đã được đặt.");

        return _next != null
            ? await _next.HandleAsync(model)
            : OrderResult.Success();
    }
}

public class VoucherValidationHandler : OrderHandler
{
    private readonly IOrdersService _ordersService;

    public VoucherValidationHandler(IOrdersService ordersService)
        => _ordersService = ordersService;

    public override async Task<OrderResult> HandleAsync(BookTicketsVM model)
    {
        if (!string.IsNullOrEmpty(model.VoucherCode))
        {
            var voucher = await _ordersService
                .GetVoucherByCodeAsync(model.VoucherCode);
            if (voucher == null)
                return new OrderResult(false, "Mã voucher không hợp lệ.");
            if (!voucher.IsActive || voucher.ExpiryDate < DateTime.Now)
                return new OrderResult(false, "Mã voucher đã hết hạn.");
        }

        return _next != null
            ? await _next.HandleAsync(model)
            : OrderResult.Success();
    }
}

public class PaymentProcessingHandler : OrderHandler
{
    private readonly IPaymentGateway _paymentGateway;

    public PaymentProcessingHandler(IPaymentGateway gateway)
        => _paymentGateway = gateway;

    public override async Task<OrderResult> HandleAsync(BookTicketsVM model)
    {
        // Xử lý thanh toán
        var paymentResult = await _paymentGateway.PayAsync(
            model.TotalPrice, $"ORDER-{DateTime.Now.Ticks}");

        if (!paymentResult.Success)
            return new OrderResult(false,
                $"Thanh toán thất bại: {paymentResult.Message}");

        return _next != null
            ? await _next.HandleAsync(model)
            : OrderResult.Success();
    }
}

// Đăng ký pipeline trong Program.cs
public static class OrderPipelineBuilder
{
    public static OrderHandler BuildPipeline(
        IServiceProvider sp)
    {
        var ordersService = sp.GetRequiredService<IOrdersService>();
        var paymentGateway = new PaymentGatewayFactory()
            .GetGateway("Cash");

        var vh = new ValidationHandler();
        var sh = new SeatAvailabilityHandler(ordersService);
        var vch = new VoucherValidationHandler(ordersService);
        var ph = new PaymentProcessingHandler(paymentGateway);

        vh.SetNext(sh).SetNext(vch).SetNext(ph);
        return vh;
    }
}
```

---

### 4.6 Template Method — Báo cáo doanh thu

**Intent:** Định nghĩa khung (skeleton) của thuật toán trong một method, để các subclass **override** các bước cụ thể mà không thay đổi cấu trúc thuật toán.

**Áp dụng:** Tạo báo cáo doanh thu — cùng khung nhưng dữ liệu và cách hiển thị khác nhau (Daily, Monthly, ByMovie, ByCinema).

```csharp
// TemplateMethod/ReportGenerator.cs
public abstract class ReportGenerator
{
    // Template Method — không override được
    public async Task<ReportData> GenerateAsync(DateTime start, DateTime end)
    {
        var orders = await FetchOrdersAsync(start, end);
        var filtered = FilterActiveOrders(orders);
        var metrics = CalculateMetrics(filtered);
        var chartData = PrepareChartData(filtered);

        return new ReportData
        {
            ReportType = GetReportName(),
            StartDate = start,
            EndDate = end,
            TotalRevenue = metrics.Revenue,
            TotalOrders = metrics.OrderCount,
            TotalTickets = metrics.TicketCount,
            ChartLabels = chartData.Labels,
            ChartValues = chartData.Values
        };
    }

    // Các bước — subclass override
    protected abstract string GetReportName();
    protected abstract Task<IEnumerable<Order>> FetchOrdersAsync(DateTime start, DateTime end);
    protected abstract IEnumerable<Order> FilterActiveOrders(IEnumerable<Order> orders);
    protected abstract Metrics CalculateMetrics(IEnumerable<Order> orders);
    protected abstract ChartData PrepareChartData(IEnumerable<Order> orders);
}

public class DailyRevenueReport : ReportGenerator
{
    protected override string GetReportName() => "Báo cáo doanh thu theo ngày";

    protected override async Task<IEnumerable<Order>> FetchOrdersAsync(
        DateTime start, DateTime end)
    {
        using var scope = _serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        return await Task.FromResult(
            context.Orders.Where(o =>
                o.OrderDate >= start && o.OrderDate <= end).ToList());
    }

    protected override IEnumerable<Order> FilterActiveOrders(
        IEnumerable<Order> orders)
        => orders.Where(o => o.Status == "Purchased" || o.Status == "Confirmed");

    protected override Metrics CalculateMetrics(IEnumerable<Order> orders)
        => new Metrics
        {
            Revenue = orders.Sum(o => o.TotalPrice - o.DiscountAmount),
            OrderCount = orders.Count(),
            TicketCount = orders.Sum(o => o.OrderItems.Sum(i => i.Amount))
        };

    protected override ChartData PrepareChartData(IEnumerable<Order> orders)
    {
        var daily = orders.GroupBy(o => o.OrderDate.Date)
            .OrderBy(g => g.Key)
            .Select(g => new { Date = g.Key.ToString("dd/MM"), Revenue = g.Sum(o => o.TotalPrice) })
            .ToList();

        return new ChartData
        {
            Labels = daily.Select(d => d.Date).ToList(),
            Values = daily.Select(d => d.Revenue).ToList()
        };
    }
}

public class MoviePerformanceReport : ReportGenerator
{
    protected override string GetReportName() => "Báo cáo doanh thu theo phim";

    // Override FetchOrdersAsync để Include Movie navigation
    protected override Task<IEnumerable<Order>> FetchOrdersAsync(
        DateTime start, DateTime end)
    {
        // Cần Include OrderItems → Showtime → Movie
        // ...
        return Task.FromResult(Enumerable.Empty<Order>());
    }

    protected override ChartData PrepareChartData(IEnumerable<Order> orders)
    {
        var byMovie = orders
            .SelectMany(o => o.OrderItems)
            .GroupBy(i => i.Showtime?.Movie?.Name ?? "N/A")
            .Select(g => new { Movie = g.Key, Revenue = g.Sum(i => i.Amount * i.Price) })
            .OrderByDescending(x => x.Revenue)
            .ToList();

        return new ChartData
        {
            Labels = byMovie.Select(m => m.Movie).ToList(),
            Values = byMovie.Select(m => m.Revenue).ToList()
        };
    }
}
```

---

### 4.7 Mediator — Giảm coupling giữa Controllers

**Intent:** Định nghĩa một object **encapsulates** cách một tập hợp objects tương tác. Mediator ngăn các objects tham chiếu trực tiếp nhau, giảm coupling.

**Áp dụng:** Khi `OrdersController` cần cập nhật dữ liệu ở nhiều service khác (Vouchers, Members, Seats), dùng Mediator thay vì gọi trực tiếp.

```csharp
// Mediator/OrderMediator.cs
public interface IMediatorHandler
{
    Task<T> SendAsync<T>(IRequest<T> request);
}

public interface IRequest<T> { }

// Request cụ thể
public class CompleteBookingRequest : IRequest<CompleteBookingResponse>
{
    public int ShowtimeId { get; set; }
    public string SelectedSeats { get; set; }
    public string CustomerName { get; set; }
    public string CustomerEmail { get; set; }
    public string VoucherCode { get; set; }
    public int PointsRedeemed { get; set; }
    public string PaymentMethod { get; set; }
}

public class CompleteBookingResponse
{
    public bool Success { get; set; }
    public string Message { get; set; }
    public int OrderId { get; set; }
    public double FinalPrice { get; set; }
}

// Mediator Handler — xử lý tất cả trong một chỗ
public class OrderMediatorHandler :
    IRequestHandler<CompleteBookingRequest, CompleteBookingResponse>
{
    private readonly AppDbContext _context;
    private readonly IShowtimesService _showtimesService;
    private readonly IOrdersService _ordersService;
    private readonly ISeatsService _seatsService;
    private readonly IVouchersService _vouchersService;

    public OrderMediatorHandler(
        AppDbContext context,
        IShowtimesService showtimesService,
        IOrdersService ordersService,
        ISeatsService seatsService,
        IVouchersService vouchersService)
    {
        _context = context;
        _showtimesService = showtimesService;
        _ordersService = ordersService;
        _seatsService = seatsService;
        _vouchersService = vouchersService;
    }

    public async Task<CompleteBookingResponse> Handle(
        CompleteBookingRequest request)
    {
        // Tất cả logic nghiệp vụ ở đây
        // Thay vì rải trong OrdersController
        // ...
        return new CompleteBookingResponse { Success = true };
    }
}

// OrdersController trở nên cực kỳ đơn giản:
[HttpPost]
public async Task<IActionResult> BookTickets(BookTicketsVM model)
{
    var result = await _mediator.SendAsync(new CompleteBookingRequest
    {
        ShowtimeId = model.ShowtimeId,
        SelectedSeats = model.SelectedSeats,
        CustomerName = model.Name,
        CustomerEmail = model.Email,
        VoucherCode = model.VoucherCode,
        PointsRedeemed = model.PointsRedeemed,
        PaymentMethod = model.PaymentMethod
    });

    if (!result.Success)
    {
        TempData["BookingError"] = result.Message;
        return RedirectToAction(nameof(BookTickets));
    }

    return View("BookingCompleted");
}
```

---

### 4.8 Visitor — Tính doanh thu theo nhiều chiều

**Intent:** Tách rời thuật toán khỏi cấu trúc object — cho phép định nghĩa **thao tác mới** trên các elements mà **không thay đổi** classes của elements.

**Áp dụng:** Tính doanh thu từ nhiều góc độ (theo phim, theo rạp, theo thời gian) mà không thay đổi model Order/OrderItem.

```csharp
// Visitor/IRevenueVisitor.cs
public interface IRevenueVisitor
{
    double Visit(Order order);
    double Visit(OrderItem item);
    double Visit(Movie movie, IEnumerable<OrderItem> items);
    double Visit(Cinema cinema, IEnumerable<OrderItem> items);
}

// Visitor: Tính doanh thu thực (sau giảm giá)
public class NetRevenueVisitor : IRevenueVisitor
{
    public double Visit(Order order)
        => order.Status != "Cancelled" && order.Status != "Refunded"
            ? order.TotalPrice - order.DiscountAmount
            : 0;

    public double Visit(OrderItem item)
        => item.Amount * item.Price;

    public double Visit(Movie movie, IEnumerable<OrderItem> items)
        => items.Sum(i => Visit(i));

    public double Visit(Cinema cinema, IEnumerable<OrderItem> items)
        => items.Sum(i => Visit(i));
}

// Visitor: Tính doanh thu gộp (trước giảm giá)
public class GrossRevenueVisitor : IRevenueVisitor
{
    public double Visit(Order order) => order.TotalPrice;

    public double Visit(OrderItem item)
        => item.Amount * item.Price;

    public double Visit(Movie movie, IEnumerable<OrderItem> items)
        => items.Sum(i => Visit(i));

    public double Visit(Cinema cinema, IEnumerable<OrderItem> items)
        => items.Sum(i => Visit(i));
}

// Visitor: Tính tổng vé bán ra
public class TicketCountVisitor : IRevenueVisitor
{
    public double Visit(Order order) => order.OrderItems.Sum(i => i.Amount);

    public double Visit(OrderItem item) => item.Amount;

    public double Visit(Movie movie, IEnumerable<OrderItem> items)
        => items.Sum(i => i.Amount);

    public double Visit(Cinema cinema, IEnumerable<OrderItem> items)
        => items.Sum(i => i.Amount);
}

// RevenueAnalyzer — element chứa visitor logic
public class RevenueAnalyzer
{
    private readonly IRevenueVisitor _visitor;

    public RevenueAnalyzer(IRevenueVisitor visitor) => _visitor = visitor;

    public double Analyze(IEnumerable<Order> orders)
        => orders.Where(o => o.Status == "Purchased" || o.Status == "Confirmed")
            .Sum(o => _visitor.Visit(o));

    public Dictionary<string, double> ByMovie(IEnumerable<Order> orders)
        => orders
            .SelectMany(o => o.OrderItems)
            .Where(i => i.Showtime?.Movie != null)
            .GroupBy(i => i.Showtime!.Movie!.Name)
            .ToDictionary(g => g.Key,
                          g => _visitor.Visit(g.Key, g.ToList()));

    public Dictionary<string, double> ByCinema(IEnumerable<Order> orders)
        => orders
            .SelectMany(o => o.OrderItems)
            .Where(i => i.Showtime?.CinemaRoom?.Cinema != null)
            .GroupBy(i => i.Showtime!.CinemaRoom!.Cinema!.Name)
            .ToDictionary(g => g.Key,
                          g => _visitor.Visit(g.Key, g.ToList()));
}

// Sử dụng: chỉ cần đổi visitor, logic không đổi
var netAnalyzer = new RevenueAnalyzer(new NetRevenueVisitor());
var byMovie = netAnalyzer.ByMovie(activeOrders);

var grossAnalyzer = new RevenueAnalyzer(new GrossRevenueVisitor());
var totalGross = grossAnalyzer.Analyze(activeOrders);
```

---

## 5. Sơ đồ quan hệ các Pattern

```
┌──────────────────────────────────────────────────────────────────────────┐
│                          APPLICATION LAYER                               │
│                                                                          │
│  ┌────────────────┐     ┌────────────────┐     ┌────────────────┐        │
│  │ OrdersController│     │ MoviesController│    │ ShowtimesCtrl │        │
│  └───────┬────────┘     └───────┬────────┘     └───────┬────────┘        │
│          │                      │                      │                │
│          └──────────┬───────────┘                      │                │
│                     ▼                                  │                │
│         ┌─────────────────────┐                        │                │
│         │  BookingFacade      │◄─── FACADE             │                │
│         │  (Luồng đặt vé)     │                        │                │
│         └──────────┬──────────┘                        │                │
│                    │                                   │                │
│      ┌─────────────┼─────────────┐                     │                │
│      ▼             ▼             ▼                     │                │
│ ┌─────────┐ ┌───────────┐ ┌──────────────┐             │                │
│ │Builder  │ │ Mediator  │ │ChainOfResp.  │             │                │
│ │Order    │ │OrderMedia │ │OrderPipeline │             │                │
│ └────┬────┘ └─────┬─────┘ └──────┬───────┘             │                │
│      │            │             │                      │                │
│      │     ┌──────▼──────┐      │                      │                │
│      │     │ OrderService │◄─────┘                      │                │
│      │     │(CRUD+State) │                             │                │
│      │     └──────┬───────┘                            │                │
│      │            │                                    │                │
│      │   ┌─────────┼─────────┬──────────────┐          │                │
│      │   ▼         ▼         ▼              ▼          │                │
│      │ ┌──────┐ ┌──────┐ ┌─────────┐ ┌─────────┐     │                │
│      │ │State │ │Obser-│ │ Command │ │Template │     │                │
│      │ │Purch- │ │ver   │ │(Book,   │ │Method   │     │                │
│      │ │ased,  │ │Email, │ │Cancel,  │ │Report   │     │                │
│      │ │Confirm│ │Loyalty│ │Refund)  │ │Generator│     │                │
│      │ └──────┘ └──────┘ └────┬────┘ └────┬────┘     │                │
│      │                        │           │           │                │
│      │         ┌──────────────┴───────────┘           │                │
│      │         ▼                                     │                │
│ ┌────▼──────────────────────────────────────────────────────────┐      │
│ │                     DATA ACCESS LAYER                        │      │
│ │  ┌──────────────┐  ┌──────────────┐  ┌──────────────┐       │      │
│ │  │ CachedProxy  │  │   Adapter    │  │   Bridge     │       │      │
│ │  │ (Proxy)      │  │ (PayPal API) │  │(Seat Pricing)│       │      │
│ │  └──────────────┘  └──────────────┘  └──────────────┘       │      │
│ │                                                             │      │
│ │  ┌──────────────┐  ┌──────────────┐  ┌──────────────┐       │      │
│ │  │  Decorator   │  │  Composite   │  │   Factory    │       │      │
│ │  │(Voucher, Pt) │  │ (Seat Row)   │  │(Abstract F.) │       │      │
│ │  └──────────────┘  └──────────────┘  └──────────────┘       │      │
│ └──────────────────────────────────────────────────────────────┘      │
│                                                                          │
│  CREATIONAL     STRUCTURAL           BEHAVIORAL                          │
│  ──────────     ─────────           ──────────                          │
│  Singleton  ─┐                                                       │
│  Factory ───┼──► Proxy ◄── Decorator ◄── Builder ──► Facade          │
│  Abstract ──┘     │           │             │                          │
│  Builder ─────────┘           │             │                          │
│  Prototype                    │             │                          │
│                               ▼             ▼                          │
│                          Adapter      Bridge                           │
│                               │             │                          │
│                          Composite ──── Visitor ──── Strategy         │
│                               │             │         │              │
│                               └──────┬──────┘         │              │
│                                      ▼                │              │
│                              Template Method ── Observer ── Mediator   │
│                                      │              │               │
│                                      ▼              ▼               │
│                              Chain of Responsibility                   │
│                                      │                               │
│                                      ▼                               │
│                              State ◄──────► Command                   │
│                                                                        
└──────────────────────────────────────────────────────────────────────────┘
```

---

## Bảng tổng hợp: Pattern nào cho vấn đề nào

| Vấn đề | Pattern | Lợi ích |
|---|---|---|
| Nhiều service đăng ký trùng lặp | Factory Method | Tự động scan + đăng ký |
| Đơn hàng quá nhiều tham số | Builder | Code sạch, dễ đọc |
| Tính giá ghế cứng nhắc | Bridge | Tách rời loại ghế + cách tính |
| Thanh toán nhiều phương thức | Strategy | Dễ thêm phương thức mới |
| Thông báo khi trạng thái đổi | Observer | Mở rộng không sửa Subject |
| Trạng thái đơn hàng phức tạp | State | Rõ ràng, tránh switch/if |
| Luồng đặt vé rối | Facade | Controller chỉ ~15 dòng |
| Xử lý đơn hàng nhiều bước | Chain of Responsibility | Dễ thêm bước mới |
| Báo cáo có cùng khung | Template Method | Tái sử dụng skeleton |
| Ghế theo hàng/cấu trúc cây | Composite | Xử lý đơn + composite thống nhất |
| Cache dữ liệu hay đọc | Proxy | Giảm query DB, lazy load |
| Tính doanh thu nhiều chiều | Visitor | Thêm chiều mà không đổi model |
| Ghép nối Controller-Service | Mediator | Giảm coupling |
| Multi-tenant cinema chain | Abstract Factory | Họ factory cho từng cinema |
| Sao chép Movie cho mùa mới | Prototype | Clone không cần biết class |
| Thêm voucher/points vào giá | Decorator | Xếp chồng decorators |
| Tích hợp PayPal API | Adapter | Adapter chuyển đổi interface |
| ShoppingCart là singleton | Singleton | Một instance duy nhất |
