# 🎬 KỊCH BẢN VIDEO DEMO CHI TIẾT — MOVIECINEMA
## Áp dụng 11 Design Patterns trong ASP.NET Core MVC

> **Thời lượng:** 15–30 phút  
> **Cấu trúc:** 3–4 phút giới thiệu + phần còn lại demo code chi tiết & chạy chức năng  
> **Công cụ cần thiết:** Visual Studio, Browser, SQL Server, phần mềm quay màn hình

---

## PHẦN 1: GIỚI THIỆU SẢN PHẨM (3–4 phút)

### [00:00] — Mở đầu

**Lời dẫn:**

> "Xin chào thầy/cô và các bạn. Hôm nay mình sẽ trình bày dự án **MovieCinema** — hệ thống đặt vé xem phim trực tuyến xây dựng bằng **ASP.NET Core MVC**, **Entity Framework Core** và **SQL Server**. Điểm đặc biệt: dự án áp dụng **11 Design Pattern** giúp code dễ bảo trì, dễ mở rộng."

---

### [00:45] — Kiến trúc tổng quát

```
┌─────────────────────────────────────────────────┐
│                    Browser                       │
│              (Razor Views + JS)                  │
└────────────────────┬────────────────────────────┘
                     │ HTTP Request
┌────────────────────▼────────────────────────────┐
│               Controllers (MVC)                  │
│    MoviesController, OrdersController, ...       │
└────────────────────┬────────────────────────────┘
                     │ Dependency Injection
┌────────────────────▼────────────────────────────┐
│   Services / Facade / Mediator / Pipeline        │
│      (Business Logic + Design Patterns)          │
└────────────────────┬────────────────────────────┘
                     │
┌────────────────────▼────────────────────────────┐
│         AppDbContext (Entity Framework Core)      │
└────────────────────┬────────────────────────────┘
                     │
┌────────────────────▼────────────────────────────┐
│               SQL Server Database                 │
└─────────────────────────────────────────────────┘
```

> "Kiến trúc 5 lớp. Controller nhận request từ browser, thông qua Dependency Injection gọi đến các Service/Facade/Mediator xử lý business logic, truy xuất database qua Entity Framework Core. Tất cả được cấu hình trong `Program.cs`."

---

### [01:30] — Chức năng chính

| Nhóm chức năng | Chi tiết |
|---|---|
| 🎬 **Xem phim** | Danh sách phim đang chiếu/sắp chiếu, chi tiết, suất chiếu |
| 💺 **Chọn ghế** | Sơ đồ ghế: Standard, VIP, Couple, Khuyết tật |
| 🛒 **Giỏ hàng** | Thêm/xóa, voucher, điểm tích lũy |
| 💳 **Đặt vé & Thanh toán** | Cash hoặc PayPal |
| 👤 **Tài khoản** | Đăng ký/đăng nhập, lịch sử đặt vé |
| 👨‍💼 **Quản trị** | Quản lý phim, diễn viên, rạp, đơn hàng, voucher, thành viên |

---

### [02:30] — Tổng quan Design Patterns

| Nhóm | Pattern | Vai trò |
|---|---|---|
| **Creational** | Repository, Builder | Tạo đối tượng, truy xuất dữ liệu |
| **Structural** | Bridge, Decorator, Proxy, Facade | Tổ chức cấu trúc class |
| **Behavioral** | Strategy, Observer, State, Chain, Mediator | Quản lý hành vi & tương tác |

> "Khi khách hàng nhấn 'Đặt vé', **7 pattern phối hợp**: Mediator → Chain validate → Facade → Bridge tính giá → Decorator giảm giá → Strategy thanh toán → Builder tạo Order."

---

## PHẦN 2: DEMO CHỨC NĂNG & GIẢI THÍCH CODE CHI TIẾT (12–26 phút)

---

### 🔹 DEMO 1 — Repository Pattern (1.5 phút)

#### [04:00] — Chạy trên web

1. Mở trang `/Actors`
2. Danh sách diễn viên hiển thị
3. Click **"Add Actor"** → nhập tên → Submit
4. Click **"Edit"** → sửa → Submit
5. Click **"Delete"** → xác nhận

> "Mình vừa CRUD trên bảng Actor — tất cả đi qua một chỗ duy nhất."

#### [05:00] — Giải thích code chi tiết

**Bước 1: Interface IEntityBase** (file `Data/Base/IEntityBase.cs`):

```csharp
public interface IEntityBase
{
    int Id { get; set; }
}
```

> "Mọi entity (Actor, Movie, Cinema...) đều implement `IEntityBase` để đảm bảo có `Id`. Đây là 'hợp đồng chung' cho Repository biết cách truy xuất."

**Bước 2: Interface IEntityBaseRepository\<T\>** (file `Data/Base/IEntityBaseRepository.cs`):

```csharp
public interface IEntityBaseRepository<T> where T : class, IEntityBase, new()
{
    Task<IEnumerable<T>> GetAllAsync();
    Task<IEnumerable<T>> GetAllAsync(params Expression<Func<T, object>>[] includeProperties);
    Task<T> GetByIdAsync(int id);
    Task AddAsync(T entity);
    Task UpdateAsync(int id, T entity);
    Task DeleteAsync(int id);
}
```

> "6 method CRUD generic `<T>`. Ràng buộc `where T : class, IEntityBase, new()` đảm bảo T phải là class, có `Id`, có constructor mặc định."

**Bước 3: Lớp triển khai** (file `Data/Base/EntityBaseRepository.cs`):

```csharp
public class EntityBaseRepository<T> : IEntityBaseRepository<T> 
    where T : class, IEntityBase, new()
{
    protected readonly AppDbContext _context;

    public EntityBaseRepository(AppDbContext context)
    {
        _context = context;
    }

    // GetAllAsync — Lấy tất cả entity bằng _context.Set<T>()
    public async Task<IEnumerable<T>> GetAllAsync() 
        => await _context.Set<T>().ToListAsync();

    // GetAllAsync có Include — Hỗ trợ eager loading
    public async Task<IEnumerable<T>> GetAllAsync(
        params Expression<Func<T, object>>[] includeProperties)
    {
        IQueryable<T> query = _context.Set<T>();
        foreach (var includeProperty in includeProperties)
            query = query.Include(includeProperty);
        return await query.ToListAsync();
    }

    // GetByIdAsync — Tìm theo Id
    public async Task<T> GetByIdAsync(int id) 
        => await _context.Set<T>().FirstOrDefaultAsync(n => n.Id == id);

    // AddAsync — Thêm mới
    public async Task AddAsync(T entity)
    {
        await _context.Set<T>().AddAsync(entity);
        await _context.SaveChangesAsync();
    }

    // UpdateAsync — Cập nhật
    public async Task UpdateAsync(int id, T entity)
    {
        EntityEntry entityEntry = _context.Entry<T>(entity);
        entityEntry.State = EntityState.Modified;
        await _context.SaveChangesAsync();
    }

    // DeleteAsync — Xóa
    public async Task DeleteAsync(int id)
    {
        var entity = await _context.Set<T>().FirstOrDefaultAsync(n => n.Id == id);
        EntityEntry entityEntry = _context.Entry<T>(entity);
        entityEntry.State = EntityState.Deleted;
        await _context.SaveChangesAsync();
    }
}
```

> "**Điểm mấu chốt:** `_context.Set<T>()` trả về `DbSet` tương ứng với kiểu `T`. Cùng một đoạn code xử lý cho Actor, Movie, Cinema — không cần viết lại. Method `GetAllAsync` có tham số `includeProperties` dạng `Expression<Func<T, object>>[]` để hỗ trợ eager loading."

**Bước 4: Service kế thừa** (ví dụ `Data/Services/ActorsService.cs`):

```csharp
public class ActorsService : EntityBaseRepository<Actor>, IActorsService
{
    public ActorsService(AppDbContext context) : base(context) { }
    // Tự động có GetAllAsync, GetByIdAsync, AddAsync, UpdateAsync, DeleteAsync
    // Thanks to inheritance from EntityBaseRepository<Actor>
}
```

> "`ActorsService` kế thừa `EntityBaseRepository<Actor>` — tự động có 6 method CRUD mà không cần viết thêm dòng code nào. Tương tự cho `CinemasService`, `ProducersService`, `CategoriesService`... Code tái sử dụng 100%."

**Sơ đồ:**
```
IEntityBaseRepository<T> (Interface)
          │
          ▼
EntityBaseRepository<T> (Triển khai generic)
          │
          ▼
ActorsService : EntityBaseRepository<Actor>
CinemasService : EntityBaseRepository<Cinema>
ProducersService : EntityBaseRepository<Producer>
CategoriesService : EntityBaseRepository<Category>
SeatsService : EntityBaseRepository<Seat>
ShowtimesService : EntityBaseRepository<Showtime>
```

---

### 🔹 DEMO 2 — Proxy Pattern (2 phút)

#### [06:00] — Chạy trên web

1. Mở trang `/Movies`
2. F5 lần 1 → dữ liệu load
3. F5 lần 2 → nhanh hơn vì lấy từ cache
4. Kiểm tra SQL Server log → chỉ 1 query dù refresh 2 lần

> "Database chỉ bị query 1 lần. Đây là **Proxy Pattern** — cache layer bọc trước service thật."

#### [07:00] — Giải thích code chi tiết

**Proxy class** (file `Data/Proxy/CachedMoviesServiceProxy.cs`):

```csharp
public class CachedMoviesServiceProxy : IMoviesService
{
    private readonly MoviesService _realService;   // Service thật
    private readonly IMemoryCache _cache;          // Cache layer
    private static readonly TimeSpan DefaultExpiry = TimeSpan.FromMinutes(10);  // 10 phút
    private static readonly TimeSpan ShortExpiry = TimeSpan.FromMinutes(2);     // 2 phút

    public CachedMoviesServiceProxy(MoviesService realService, IMemoryCache cache)
    {
        _realService = realService;
        _cache = cache;
    }

    // ── READ: Kiểm tra cache trước, nếu không có thì query DB ──
    public async Task<IEnumerable<Movie>> GetAllAsync()
    {
        return await _cache.GetOrCreateAsync("movies:all", async entry =>
        {
            entry.SlidingExpiration = DefaultExpiry;  // Cache 10 phút
            return await _realService.GetAllAsync();  // Lần đầu query DB
        }) ?? Enumerable.Empty<Movie>();  // Các lần sau lấy từ cache
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

    // ── WRITE: Gọi service thật + xóa cache cũ ──
    public async Task AddAsync(Movie entity)
    {
        await _realService.AddAsync(entity);
        InvalidateAllCaches();  // Xóa cache để lần sau lấy data mới
    }

    public async Task UpdateAsync(int id, Movie entity)
    {
        await _realService.UpdateAsync(id, entity);
        InvalidateAllCaches();
    }

    public async Task DeleteAsync(int id)
    {
        await _realService.DeleteAsync(id);
        InvalidateAllCaches();
    }

    // ── Cache Invalidation ──
    private void InvalidateAllCaches()
    {
        // Đơn giản: xóa tất cả entries liên quan
        // Production: dùng CacheTagHelper hoặc Redis
    }
}
```

> "**Cách hoạt động:** `_cache.GetOrCreateAsync` kiểm tra key `"movies:all"` trong cache. Nếu có → trả về ngay (không query DB). Nếu không có → gọi `_realService.GetAllAsync()` để query DB, lưu vào cache, rồi trả về. `SlidingExpiration = 10 phút` nghĩa là nếu truy cập trong 10 phút → cache được gia hạn, sau 10 phút không dùng → tự xóa."

**Đăng ký trong DI** (file `Program.cs`, dòng 61–66):

```csharp
// Đăng ký service thật (không interface — chỉ dùng nội bộ)
builder.Services.AddScoped<MoviesService>();

// Đăng ký Proxy: khi ai đó yêu cầu IMoviesService → trả về Proxy
builder.Services.AddScoped<IMoviesService>(sp =>
{
    var realService = sp.GetRequiredService<MoviesService>();
    var cache = sp.GetRequiredService<IMemoryCache>();
    return new CachedMoviesServiceProxy(realService, cache);
});
```

> "**Minh bạch hoàn toàn:** `MoviesController` inject `IMoviesService` và không biết đang dùng Proxy. Code trong Controller không cần thay đổi — đây là sức mạnh của Proxy Pattern."

**Sơ đồ:**
```
MoviesController
      │
      │ inject IMoviesService
      ▼
CachedMoviesServiceProxy (Proxy)
      │
      ├── IMemoryCache? → Có cache → Trả về ngay
      │
      └── Không có cache → Gọi _realService.GetAllAsync()
                                   │
                                   ▼
                          MoviesService (Service thật)
                                   │
                                   ▼
                          AppDbContext → SQL Server
```

---

### 🔹 DEMO 3 — Bridge Pattern (2 phút)

#### [08:00] — Chạy trên web

1. Vào trang đặt vé → chọn suất chiếu
2. Sơ đồ ghế hiển thị 4 loại:
   - 🔵 **Standard** — giá gốc: `100,000đ`
   - 🟡 **VIP** — giá ×1.2: `120,000đ`
   - 🩷 **Couple** — giá ×2.0: `200,000đ`
   - ⚪ **Disabled** — giá ×0.5: `50,000đ`
3. Click chọn ghế → tổng tiền tự cập nhật

> "Sơ đồ ghế hiển thị giá khác nhau theo loại. Đây là **Bridge Pattern** — tách 'cái gì tính giá' ra khỏi 'tính giá như thế nào'."

#### [09:30] — Giải thích code chi tiết

**File `Models/Bridge/SeatPricingBridge.cs`:**

**Phần 1 — Implementation (các thuật toán tính giá cụ thể):**

```csharp
// Interface định nghĩa "cách tính giá" — có thể swap được
public interface ISeatingPricingStrategy
{
    double CalculatePrice(double basePrice);
    string SeatTypeName { get; }
}

// Strategy 1: Ghế Standard — giữ nguyên giá gốc (×1.0)
public class StandardPricingStrategy : ISeatingPricingStrategy
{
    public double CalculatePrice(double basePrice) => basePrice;
    public string SeatTypeName => "Standard";
}

// Strategy 2: Ghế VIP — nhân 1.2
public class VipPricingStrategy : ISeatingPricingStrategy
{
    public double CalculatePrice(double basePrice) => basePrice * 1.2;
    public string SeatTypeName => "VIP";
}

// Strategy 3: Ghế Couple — nhân 2.0 (ghế đôi)
public class CouplePricingStrategy : ISeatingPricingStrategy
{
    public double CalculatePrice(double basePrice) => basePrice * 2.0;
    public string SeatTypeName => "Couple";
}

// Strategy 4: Ghế Khuyết tật — giảm 50%
public class DisabledPricingStrategy : ISeatingPricingStrategy
{
    public double CalculatePrice(double basePrice) => basePrice * 0.5;
    public string SeatTypeName => "Khuyết tật";
}
```

> "Mỗi Strategy implement `ISeatingPricingStrategy`, đóng gói thuật toán tính giá riêng. `Standard` giữ nguyên, `VIP` nhân 1.2, `Couple` nhân 2.0, `Disabled` nhân 0.5."

**Phần 2 — Abstraction (lớp trung gian):**

```csharp
public class SeatPricingBridge
{
    private readonly ISeatingPricingStrategy _strategy;

    // Constructor 1: Nhận trực tiếp strategy
    public SeatPricingBridge(ISeatingPricingStrategy strategy)
    {
        _strategy = strategy;
    }

    // Constructor 2: Tự chọn strategy dựa trên SeatType enum
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

    // Ủy thác tính giá cho strategy
    public double GetPrice(double basePrice) => _strategy.CalculatePrice(basePrice);
}
```

> "**Bridge ở đâu?** `SeatPricingBridge` là **Abstraction** — nó không tự tính giá mà **ủy thác** cho `ISeatingPricingStrategy` (Implementation). Constructor 2 dùng `switch` để tự chọn strategy theo `SeatType` enum."

**Bảng giá minh họa (giá gốc 100,000đ):**

| Loại ghế | Strategy | Hệ số | Giá |
|---|---|---|---|
| Standard | `StandardPricingStrategy` | ×1.0 | 100,000đ |
| VIP | `VipPricingStrategy` | ×1.2 | 120,000đ |
| Couple | `CouplePricingStrategy` | ×2.0 | 200,000đ |
| Disabled | `DisabledPricingStrategy` | ×0.5 | 50,000đ |

**Cách sử dụng** (file `Controllers/OrdersController.cs`):

```csharp
// Trong Controller — tính giá hiển thị trên sơ đồ ghế
price = new SeatPricingBridge(s.SeatType).GetPrice(showtime.Price)
```

> "Khi thêm loại ghế 'Student' giảm 30%: tạo `StudentPricingStrategy`, thêm 1 dòng trong switch. SeatPricingBridge không phải sửa."

**Sơ đồ:**
```
SeatType (Abstraction)
    │
    ├── SeatType.Standard → StandardPricingStrategy  → basePrice × 1.0
    ├── SeatType.VIP      → VipPricingStrategy       → basePrice × 1.2
    ├── SeatType.Couple   → CouplePricingStrategy     → basePrice × 2.0
    └── SeatType.Disabled → DisabledPricingStrategy   → basePrice × 0.5
```

---

### 🔹 DEMO 4 — Strategy Pattern (2 phút)

#### [10:00] — Chạy trên web

1. Ở trang đặt vé → kéo xuống **'Phương thức thanh toán'**
2. Chọn **'Thanh toán tại rạp'** (Cash) → submit → thành công
3. Đặt lại, chọn **'PayPal'** → submit → thành công

> "Khách hàng chọn phương thức thanh toán lúc runtime. Đây là **Strategy Pattern**."

#### [11:00] — Giải thích code chi tiết

**File `Data/Strategy/PaymentStrategy.cs`:**

**Phần 1 — Interface chung:**

```csharp
public interface IPaymentStrategy
{
    string Name { get; }           // Tên hiển thị: "Thanh toán tại rạp"
    string PaymentMethod { get; }  // Mã: "Cash", "PayPal"
    Task<PaymentResult> PayAsync(double amount, string orderId);      // Thanh toán
    Task<RefundResult> RefundAsync(string transactionId, double amount); // Hoàn tiền
}

public class PaymentResult
{
    public bool Success { get; set; }
    public string TransactionId { get; set; } = "";
    public string Message { get; set; } = "";
}

public class RefundResult
{
    public bool Success { get; set; }
    public string RefundId { get; set; } = "";
    public string Message { get; set; } = "";
}
```

> "Mọi phương thức thanh toán đều phải implement `IPaymentStrategy` với 2 method chính: `PayAsync` (thanh toán) và `RefundAsync` (hoàn tiền)."

**Phần 2 — Concrete Strategy 1 (Tiền mặt):**

```csharp
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

    public Task<RefundResult> RefundAsync(string transactionId, double amount)
    {
        return Task.FromResult(new RefundResult
        {
            Success = true,
            RefundId = $"REF-{transactionId}",
            Message = "Hoàn tiền thành công."
        });
    }
}
```

> "CashPaymentStrategy: sinh mã giao dịch `CASH-{orderId}-{ticks}`, thông báo 'vui lòng thanh toán khi nhận vé'. Vì tiền mặt không cần gọi API nào nên trả về `Task.FromResult` tức thì."

**Phần 3 — Concrete Strategy 2 (PayPal):**

```csharp
public class PayPalPaymentStrategy : IPaymentStrategy
{
    private readonly string _clientId;
    private readonly string _clientSecret;

    public PayPalPaymentStrategy(string clientId, string clientSecret)
    {
        _clientId = clientId;
        _clientSecret = clientSecret;
    }

    public string Name => "PayPal";
    public string PaymentMethod => "PayPal";

    public async Task<PaymentResult> PayAsync(double amount, string orderId)
    {
        // Stub — thay bằng PayPal SDK thực tế khi production
        await Task.Delay(100); // Giả lập API call
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
        return new RefundResult
        {
            Success = true,
            RefundId = $"REF-{transactionId}",
            Message = "Hoàn tiền PayPal thành công."
        };
    }
}
```

> "PayPalPaymentStrategy: nhận `clientId`/`clientSecret` qua constructor. Hiện tại dùng `Task.Delay(100)` mô phỏng API call — khi production sẽ thay bằng SDK thật."

**Phần 4 — Context (chọn strategy lúc runtime):**

```csharp
public class PaymentContext
{
    private IPaymentStrategy? _strategy;

    // Setter trực tiếp
    public void SetStrategy(IPaymentStrategy strategy) => _strategy = strategy;

    // Setter theo tên — chọn strategy dựa trên chuỗi
    public void SetStrategyByName(string? name)
    {
        var method = name?.ToLower() ?? "cash";
        _strategy = method switch
        {
            "paypal" => new PayPalPaymentStrategy("CLIENT_ID", "CLIENT_SECRET"),
            _        => new CashPaymentStrategy()  // Mặc định: Cash
        };
    }

    // Ủy thác thanh toán cho strategy đã chọn
    public async Task<PaymentResult> PayAsync(double amount, string orderId)
    {
        if (_strategy == null)
            throw new InvalidOperationException("Payment strategy not set. Call SetStrategy first.");
        return await _strategy.PayAsync(amount, orderId);
    }

    public async Task<RefundResult> RefundAsync(string transactionId, double amount)
    {
        if (_strategy == null)
            throw new InvalidOperationException("Payment strategy not set.");
        return await _strategy.RefundAsync(transactionId, amount);
    }

    public string CurrentPaymentMethod => _strategy?.PaymentMethod ?? "Unknown";
}
```

> "`PaymentContext` giữ strategy hiện tại. `SetStrategyByName` dùng `switch` để chọn strategy theo tên: `"paypal"` → `PayPalPaymentStrategy`, còn lại → `CashPaymentStrategy`. Method `PayAsync` ủy thác cho `_strategy.PayAsync()`."

**Cách sử dụng trong Facade** (file `Data/Facade/BookingFacade.cs`, dòng 99–105):

```csharp
// 1. Tạo context
var paymentCtx = new PaymentContext();

// 2. Chọn strategy theo input của user
paymentCtx.SetStrategyByName(model.PaymentMethod); // "Cash" hoặc "PayPal"

// 3. Gọi thanh toán — context tự dispatch đúng strategy
var paymentResult = await paymentCtx.PayAsync(totalPrice, $"ORDER-{DateTime.Now.Ticks}");
```

> "**Mở rộng:** Muốn thêm MoMo? Tạo `MoMoPaymentStrategy : IPaymentStrategy`, thêm 1 dòng trong switch. Code Controller/Facade không cần sửa."

**Sơ đồ:**
```
PaymentContext
    │
    ├── "cash"  → CashPaymentStrategy  → PayAsync()
    └── "paypal" → PayPalPaymentStrategy → PayAsync()
```

---

### 🔹 DEMO 5 — Builder Pattern (2 phút)

#### [12:00] — Chạy trên web

1. Vào trang đặt vé → chọn ghế
2. Điền tên, email, voucher, điểm tích lũy, phương thức thanh toán
3. Click **'Đặt vé'** → thành công
4. Mở SQL Server → kiểm tra `Orders` và `OrderItems` → dữ liệu đã lưu

> "Khi đặt vé, hệ thống cần tạo Order với rất nhiều trường. Đây là **Builder Pattern**."

#### [13:00] — Giải thích code chi tiết

**File `Models/Builders/OrderBuilder.cs`:**

**Phần 1 — Interface:**

```csharp
public interface IOrderBuilder
{
    IOrderBuilder SetCustomer(string name, string email, string userId);
    IOrderBuilder SetShowtime(int showtimeId, string selectedSeats, int seatCount, double basePrice);
    IOrderBuilder ApplyVoucher(double discountAmount, double orderTotal);
    IOrderBuilder RedeemPoints(int points, double totalBeforePoints);
    IOrderBuilder SetPaymentMethod(string method);
    IOrderBuilder CalculateTotal();
    Order Build();
}
```

> "Mỗi method trả về `IOrderBuilder` (chính nó) để cho phép gọi chuỗi: `.SetCustomer(...).SetShowtime(...).ApplyVoucher(...)`. Method cuối `Build()` trả về `Order` hoàn chỉnh."

**Phần 2 — Lớp triển khai:**

```csharp
public class OrderBuilder : IOrderBuilder
{
    // Khởi tạo Order rỗng với giá trị mặc định
    private readonly Order _order = new()
    {
        OrderItems = new List<OrderItem>(),
        OrderDate = DateTime.Now,
        Status = "Purchased"  // Trạng thái mặc định
    };

    private double _subtotal;  // Tổng trước giảm giá

    // Bước 1: Thiết lập thông tin khách hàng
    public IOrderBuilder SetCustomer(string name, string email, string userId)
    {
        _order.Email = email;
        _order.UserId = name;
        return this;  // ← Trả về chính nó để chain tiếp
    }

    // Bước 2: Thiết lập suất chiếu và ghế
    public IOrderBuilder SetShowtime(int showtimeId, string selectedSeats, int seatCount, double basePrice)
    {
        _subtotal = basePrice * seatCount;  // Tính subtotal
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

    // Bước 3: Áp dụng voucher giảm giá
    public IOrderBuilder ApplyVoucher(double discountAmount, double orderTotal)
    {
        _order.DiscountAmount = Math.Min(discountAmount, orderTotal);
        return this;
    }

    // Bước 4: Quy đổi điểm tích lũy (1 điểm = 1,000 VND)
    public IOrderBuilder RedeemPoints(int points, double totalBeforePoints)
    {
        double pointValue = points * 1000.0;
        _order.PointsRedeemed = (int)Math.Min(pointValue, totalBeforePoints);
        return this;
    }

    // Bước 5: Chọn phương thức thanh toán
    public IOrderBuilder SetPaymentMethod(string method)
    {
        _order.PaymentMethod = method;
        return this;
    }

    // Bước 6: Tính tổng tiền cuối cùng
    public IOrderBuilder CalculateTotal()
    {
        double finalTotal = _subtotal - _order.DiscountAmount - _order.PointsRedeemed;
        if (finalTotal < 0) finalTotal = 0;  // Không âm
        _order.TotalPrice = finalTotal;
        return this;
    }

    // Bước cuối: Trả về Order hoàn chỉnh
    public Order Build() => _order;
}
```

> "**Cách hoạt động:** Mỗi method thiết lập 1 phần của Order rồi `return this` để chain tiếp. `CalculateTotal()` tập trung công thức: `Total = Subtotal - Discount - Points`. `Build()` trả về `_order` đã hoàn chỉnh."

**Cách gọi trong Facade** (file `Data/Facade/BookingFacade.cs`, dòng 108–115):

```csharp
var order = new OrderBuilder()
    .SetCustomer(model.Name ?? "Guest", model.Email ?? "", userId ?? "")
    .SetShowtime(model.ShowtimeId, model.SelectedSeats, selectedSeats.Count, showtime.Price)
    .ApplyVoucher(discount, totalPrice)
    .RedeemPoints(model.PointsRedeemed, totalPrice - discount)
    .SetPaymentMethod(paymentCtx.CurrentPaymentMethod)
    .CalculateTotal()
    .Build();
```

> "**So sánh:** Nếu không dùng Builder: `new Order(name, email, userId, showtimeId, seats, count, price, discount, points, method, ...)` — rất khó đọc, dễ sai thứ tự. Với Builder: mỗi bước có tên rõ ràng, code như đọc quy trình nghiệp vụ."

**Sơ đồ:**
```
new OrderBuilder()
    .SetCustomer("An", "an@email.com", "user1")
    .SetShowtime(5, "A1,A2", 2, 100000)
    .ApplyVoucher(20000, 200000)
    .RedeemPoints(5, 180000)
    .SetPaymentMethod("Cash")
    .CalculateTotal()
    .Build()
    │
    ▼
Order { Email="an@email.com", Status="Purchased", TotalPrice=155000, ... }
```

---

### 🔹 DEMO 6 — Decorator Pattern (2 phút)

#### [14:00] — Chạy trên web

1. Đặt vé 2 ghế VIP → giá gốc: `240,000đ`
2. Nhập voucher `SALE20` (20%) → còn `192,000đ`
3. Dùng 5 điểm tích lũy (5,000đ) → còn `187,000đ`
4. Hiển thị breakdown:
   ```
   Giá gốc:                   240,000đ
   Voucher SALE20 (-20%):     -48,000đ
   Điểm tích lũy (-5 điểm):    -5,000đ
   ─────────────────────────────────────
   Tổng:                      187,000đ
   ```

#### [15:30] — Giải thích code chi tiết

**File `Data/Decorators/PricingDecorators.cs`:**

**Phần 1 — Interface gốc:**

```csharp
public interface IOrderPriceDecorator
{
    double CalculatePrice(double currentPrice);  // Tính giá
    string Description { get; }                   // Mô tả
    int Priority { get; }                         // Thứ tự ưu tiên
}
```

**Phần 2 — Base component (giá gốc):**

```csharp
public class BasePriceCalculator : IOrderPriceDecorator
{
    private readonly double _basePrice;
    public BasePriceCalculator(double basePrice) => _basePrice = basePrice;
    public double CalculatePrice(double currentPrice) => _basePrice;  // Luôn trả về giá gốc
    public string Description => "Giá gốc";
    public int Priority => 0;
}
```

**Phần 3 — Decorator 1 (Voucher):**

```csharp
public class VoucherDecorator : IOrderPriceDecorator
{
    private readonly IOrderPriceDecorator _inner;  // ← Lớp bên trong (chained)
    private readonly Voucher _voucher;

    public VoucherDecorator(IOrderPriceDecorator inner, Voucher voucher)
    {
        _inner = inner;         // Lưu lớp bên trong
        _voucher = voucher;     // Lưu voucher
    }

    public double CalculatePrice(double currentPrice)
    {
        // 1. Gọi lớp bên trong trước (đệ quy)
        double discounted = _inner.CalculatePrice(currentPrice);

        // 2. Kiểm tra điều kiện tối thiểu
        if (discounted < _voucher.MinOrderAmount)
            return discounted;

        // 3. Áp dụng giảm giá voucher
        double reduction = _voucher.IsPercentage
            ? discounted * _voucher.DiscountPercentage / 100.0  // Giảm theo %
            : _voucher.DiscountAmount;                          // Giảm cố định

        return Math.Max(0, discounted - Math.Min(reduction, discounted));
    }

    public string Description => _voucher.IsPercentage
        ? $"Voucher giảm {_voucher.DiscountPercentage}% (-{_voucher.Code})"
        : $"Voucher giảm {_voucher.DiscountAmount:N0}đ (-{_voucher.Code})";

    public int Priority => 1;
}
```

> "**Cách hoạt động:** `VoucherDecorator` bọc `_inner` (lớp bên trong). Khi gọi `CalculatePrice()`, nó gọi `_inner.CalculatePrice()` trước (đệ quy), rồi áp dụng giảm giá voucher lên kết quả."

**Phần 4 — Decorator 2 (Điểm tích lũy):**

```csharp
public class LoyaltyPointsDecorator : IOrderPriceDecorator
{
    private readonly IOrderPriceDecorator _inner;
    private readonly int _points;

    public double CalculatePrice(double currentPrice)
    {
        // 1. Gọi lớp bên trong (Voucher đã giảm)
        double afterVoucher = _inner.CalculatePrice(currentPrice);

        // 2. Trừ điểm tích lũy (1 điểm = 1,000 VND)
        double pointValue = _points * 1000.0;
        return Math.Max(0, afterVoucher - pointValue);
    }

    public string Description => $"Điểm tích lũy (-{_points * 1000:N0}đ = {_points} điểm)";
    public int Priority => 2;
}
```

> "`LoyaltyPointsDecorator` nhận kết quả từ `_inner` (đã giảm voucher) rồi trừ thêm điểm tích lũy. 5 điểm = 5,000đ."

**Phần 5 — Composite (xếp chồng các lớp):**

```csharp
public class OrderPriceCalculator
{
    public PriceCalculationResult Calculate(
        double basePrice, Voucher? voucher, int loyaltyPoints, bool applyHappyHour)
    {
        // Bắt đầu từ giá gốc
        IOrderPriceDecorator calc = new BasePriceCalculator(basePrice);

        // Bọc thêm lớp voucher (nếu có)
        if (voucher != null)
            calc = new VoucherDecorator(calc, voucher);

        // Bọc thêm lớp điểm tích lũy (nếu có)
        if (loyaltyPoints > 0)
            calc = new LoyaltyPointsDecorator(calc, loyaltyPoints);

        // Bọc thêm lớp Happy Hour (nếu có)
        if (applyHappyHour)
            calc = new HappyHourDecorator(calc,
                new TimeSpan(14, 0, 0),
                new TimeSpan(17, 0, 0),
                15.0);  // Giảm 15% từ 14:00–17:00

        double finalPrice = calc.CalculatePrice(basePrice);
        // Trả về kết quả với breakdown chi tiết
        return new PriceCalculationResult
        {
            OriginalPrice = basePrice,
            FinalPrice = finalPrice,
            DiscountApplied = basePrice - finalPrice
        };
    }
}
```

> "**Cách xếp lớp:** Bắt đầu bằng `BasePriceCalculator`, rồi lần lượt bọc `VoucherDecorator` → `LoyaltyPointsDecorator` → `HappyHourDecorator`. Khi gọi `CalculatePrice()`, nó chạy **ngược từ ngoài vào trong**: HappyHour → LoyaltyPoints → Voucher → BasePrice."

**Sơ đồ luồng tính giá:**
```
BasePriceCalculator (240,000đ)
         │
         ▼
VoucherDecorator (-20%)
  240,000 × 0.8 = 192,000đ
         │
         ▼
LoyaltyPointsDecorator (-5 điểm = -5,000đ)
  192,000 - 5,000 = 187,000đ
         │
         ▼
Kết quả: 187,000đ
```

> "Thêm `FestivalDiscountDecorator` cho lễ hội? Tạo 1 class mới, xếp vào chuỗi, không sửa code cũ."

---

### 🔹 DEMO 7 — Facade Pattern (2 phút)

#### [16:30] — Chạy trên web

1. Quay lại trang đặt vé
2. Chọn ghế, nhập thông tin, chọn thanh toán
3. Click **'Đặt vé'** → thành công
4. Pause: "Bạn có thấy Controller chỉ làm 1 việc"

#### [17:30] — Giải thích code chi tiết

**Controller cực kỳ gọn** (file `Controllers/OrdersController.cs`):

```csharp
[HttpPost]
public async Task<IActionResult> BookTickets(BookTicketsVM model)
{
    // Validate cơ bản
    if (!ModelState.IsValid)
    {
        TempData["BookingError"] = "Vui lòng nhập đầy đủ thông tin.";
        return RedirectToAction(nameof(BookTickets), new { showtimeId = model.ShowtimeId });
    }

    // ← Chỉ 1 DÒNG gọi Facade!
    var result = await _bookingFacade.ProcessBookingAsync(model, User.Identity?.Name);

    if (!result.Success)
    {
        TempData["BookingError"] = result.Message;
        return RedirectToAction(nameof(BookTickets), new { showtimeId = model.ShowtimeId });
    }

    return View("BookingCompleted");
}
```

> "Controller chỉ cần 1 dòng: `_bookingFacade.ProcessBookingAsync()`. Tất cả sự phức tạp bị ẩn bên trong Facade."

**Facade xử lý 9 bước** (file `Data/Facade/BookingFacade.cs`):

```csharp
public class BookingFacade : IBookingFacade
{
    // Inject 4 service bên trong
    private readonly AppDbContext _context;
    private readonly IShowtimesService _showtimesService;
    private readonly ISeatsService _seatsService;
    private readonly IOrdersService _ordersService;

    public BookingFacade(AppDbContext context, IShowtimesService showtimesService,
        ISeatsService seatsService, IOrdersService ordersService)
    {
        _context = context;
        _showtimesService = showtimesService;
        _seatsService = seatsService;
        _ordersService = ordersService;
    }

    public async Task<BookingResult> ProcessBookingAsync(BookTicketsVM model, string? userId)
    {
        // ── Bước 1: Validate input ──
        if (string.IsNullOrEmpty(model.SelectedSeats))
            return new BookingResult { Success = false, Message = "Vui lòng chọn ít nhất một ghế." };

        // ── Bước 2: Lấy Showtime ──
        var showtime = await _showtimesService.GetShowtimeByIdWithDetailsAsync(model.ShowtimeId);
        if (showtime == null)
            return new BookingResult { Success = false, Message = "Suất chiếu không tồn tại." };

        // ── Bước 3: Parse danh sách ghế ──
        var selectedSeats = model.SelectedSeats
            .Split(',').Select(s => s.Trim())
            .Where(s => !string.IsNullOrEmpty(s)).ToList();
        if (!selectedSeats.Any())
            return new BookingResult { Success = false, Message = "Vui lòng chọn ít nhất một ghế." };

        // ── Bước 4: Kiểm tra ghế đã bị đặt ──
        var bookedSeats = await _ordersService.GetBookedSeatsForShowtimeAsync(model.ShowtimeId);
        foreach (var seat in selectedSeats)
            if (bookedSeats.Contains(seat))
                return new BookingResult { Success = false, Message = $"Ghế {seat} đã được đặt." };

        // ── Bước 5: Tính giá theo loại ghế (BRIDGE PATTERN) ──
        var roomSeats = await _seatsService.GetSeatsByRoomAsync(showtime.CinemaRoomId);
        double totalPrice = 0;
        foreach (var seatCode in selectedSeats)
        {
            var seat = roomSeats.FirstOrDefault(s => s.Row + s.Number.ToString() == seatCode);
            var bridge = new SeatPricingBridge(seat?.SeatType ?? SeatType.Standard);
            totalPrice += bridge.GetPrice(showtime.Price);
        }

        // ── Bước 6: Áp dụng voucher ──
        double discount = 0;
        if (!string.IsNullOrEmpty(model.VoucherCode))
        {
            var voucher = await _ordersService.GetVoucherByCodeAsync(model.VoucherCode);
            if (voucher != null && totalPrice >= voucher.MinOrderAmount)
            {
                discount = voucher.IsPercentage
                    ? totalPrice * voucher.DiscountPercentage / 100.0
                    : voucher.DiscountAmount;
            }
        }

        // ── Bước 7: Thanh toán (STRATEGY PATTERN) ──
        var paymentCtx = new PaymentContext();
        paymentCtx.SetStrategyByName(model.PaymentMethod);
        var paymentResult = await paymentCtx.PayAsync(totalPrice, $"ORDER-{DateTime.Now.Ticks}");
        if (!paymentResult.Success)
            return new BookingResult { Success = false, Message = $"Thanh toán thất bại." };

        // ── Bước 8: Tạo Order (BUILDER PATTERN) ──
        var order = new OrderBuilder()
            .SetCustomer(model.Name ?? "Guest", model.Email ?? "", userId ?? "")
            .SetShowtime(model.ShowtimeId, model.SelectedSeats, selectedSeats.Count, showtime.Price)
            .ApplyVoucher(discount, totalPrice)
            .RedeemPoints(model.PointsRedeemed, totalPrice - discount)
            .SetPaymentMethod(paymentCtx.CurrentPaymentMethod)
            .CalculateTotal()
            .Build();

        // ── Bước 9: Lưu vào database ──
        await _ordersService.StoreDirectOrderAsync(
            model.ShowtimeId, model.Name ?? "Guest", model.Email ?? "",
            model.SelectedSeats, selectedSeats.Count, totalPrice,
            discount, model.PointsRedeemed, paymentCtx.CurrentPaymentMethod, userId);

        var savedOrder = await _context.Orders.OrderByDescending(o => o.Id).FirstOrDefaultAsync();

        double finalPrice = totalPrice - discount - (model.PointsRedeemed * 1000);
        int earned = (int)(finalPrice / 10000);

        return new BookingResult
        {
            Success = true,
            Message = "Đặt vé thành công!",
            OrderId = savedOrder?.Id,
            FinalPrice = finalPrice,
            DiscountApplied = discount,
            PointsEarned = earned
        };
    }
}
```

> "**Facade = Lễ tân khách sạn.** Bạn chỉ cần nói 'Tôi muốn đặt phòng', lễ tân tự liên hệ các bộ phận. 9 bước bên trong: validate → lấy showtime → parse ghế → kiểm tra ghế trống → tính giá Bridge → áp voucher → thanh toán Strategy → tạo Order Builder → lưu DB. Controller không cần biết."

**Sơ đồ:**
```
OrdersController.BookTickets()
        │
        │ _bookingFacade.ProcessBookingAsync(model, userId)
        ▼
BookingFacade (9 bước)
        │
        ├── 1. Validate input
        ├── 2. GetShowtimeByIdWithDetailsAsync()
        ├── 3. Parse selectedSeats
        ├── 4. GetBookedSeatsForShowtimeAsync()
        ├── 5. SeatPricingBridge.GetPrice()        ← BRIDGE
        ├── 6. GetVoucherByCodeAsync()
        ├── 7. PaymentContext.PayAsync()            ← STRATEGY
        ├── 8. new OrderBuilder().Build()           ← BUILDER
        └── 9. StoreDirectOrderAsync()              ← REPOSITORY
```

---

### 🔹 DEMO 8 — Chain of Responsibility (2 phút)

#### [18:30] — Chạy trên web — Demo 4 lỗi

**Case 1:** Không chọn ghế → Submit → "Vui lòng chọn ít nhất một ghế"
**Case 2:** Chọn ghế đã đặt → "Ghế A5 đã được đặt"
**Case 3:** Voucher sai → "Mã voucher không tồn tại"
**Case 4:** Điểm không đủ → "Bạn chỉ có 5 điểm"

#### [19:30] — Giải thích code chi tiết

**File `Data/Chain/OrderPipeline.cs`:**

**Phần 1 — Base Handler:**

```csharp
public abstract class OrderPipelineHandler
{
    protected OrderPipelineHandler? _next;  // Handler tiếp theo trong chuỗi

    // Nối handler tiếp theo
    public OrderPipelineHandler SetNext(OrderPipelineHandler next)
    {
        _next = next;
        return next;  // Trả về next để chain: a.SetNext(b).SetNext(c)
    }

    // Abstract — mỗi handler con phải implement
    public abstract Task<OrderPipelineResult> HandleAsync(
        OrderPipelineRequest request, OrderPipelineResult result);
}
```

> "Base Handler có `_next` — pointer đến handler tiếp theo. `SetNext()` trả về `next` để chain: `a.SetNext(b).SetNext(c)`. Mỗi handler con override `HandleAsync()`."

**Phần 2 — Handler 1 (Validation):**

```csharp
public class ValidationHandler : OrderPipelineHandler
{
    public override async Task<OrderPipelineResult> HandleAsync(
        OrderPipelineRequest request, OrderPipelineResult result)
    {
        // Kiểm tra ShowtimeId
        if (request.Model.ShowtimeId <= 0)
        {
            result.IsValid = false;
            result.Message = "Suất chiếu không hợp lệ.";
            return result;  // ← DỪNG, không chuyển tiếp
        }

        // Kiểm tra đã chọn ghế chưa
        if (string.IsNullOrEmpty(request.Model.SelectedSeats))
        {
            result.IsValid = false;
            result.Message = "Vui lòng chọn ít nhất một ghế.";
            return result;  // ← DỪNG
        }

        var seats = request.Model.SelectedSeats
            .Split(',').Select(s => s.Trim())
            .Where(s => !string.IsNullOrEmpty(s)).ToList();

        // Kiểm tra số lượng ghế
        if (seats.Count > 10)
        {
            result.IsValid = false;
            result.Message = "Không thể đặt quá 10 ghế mỗi lần.";
            return result;  // ← DỪNG
        }

        // Pass → Chuyển cho handler tiếp theo
        return _next != null
            ? await _next.HandleAsync(request, result)
            : result;
    }
}
```

> "`ValidationHandler` kiểm tra 3 điều kiện: ShowtimeId > 0, đã chọn ghế, tối đa 10 ghế. Nếu fail → `result.IsValid = false`, return ngay. Nếu pass → gọi `_next.HandleAsync()` để chuyển cho handler tiếp theo."

**Phần 3 — Handler 2 (Ghế trống):**

```csharp
public class SeatAvailabilityHandler : OrderPipelineHandler
{
    private readonly IOrdersService _ordersService;

    public SeatAvailabilityHandler(IOrdersService ordersService)
    {
        _ordersService = ordersService;
    }

    public override async Task<OrderPipelineResult> HandleAsync(
        OrderPipelineRequest request, OrderPipelineResult result)
    {
        if (!result.IsValid) return result;  // Bước trước fail → bỏ qua

        // Lấy danh sách ghế đã đặt cho suất chiếu này
        var bookedSeats = await _ordersService
            .GetBookedSeatsForShowtimeAsync(request.Model.ShowtimeId);

        var selectedSeats = request.Model.SelectedSeats
            .Split(',').Select(s => s.Trim())
            .Where(s => !string.IsNullOrEmpty(s)).ToList();

        // Kiểm tra từng ghế
        foreach (var seat in selectedSeats)
        {
            if (bookedSeats.Contains(seat))
            {
                result.IsValid = false;
                result.Message = $"Ghế {seat} đã được đặt bởi người khác.";
                return result;  // ← DỪNG
            }
        }

        return _next != null
            ? await _next.HandleAsync(request, result)
            : result;
    }
}
```

> "`SeatAvailabilityHandler` kiểm tra `result.IsValid` trước — nếu bước trước đã fail, bỏ qua luôn. Nếu pass, query database lấy danh sách ghế đã đặt, kiểm tra từng ghế."

**Phần 4 — Handler 3 (Voucher):**

```csharp
public class VoucherValidationHandler : OrderPipelineHandler
{
    private readonly IOrdersService _ordersService;

    public override async Task<OrderPipelineResult> HandleAsync(
        OrderPipelineRequest request, OrderPipelineResult result)
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

> "`VoucherValidationHandler` kiểm tra 3 điều kiện: voucher tồn tại, voucher còn active, voucher chưa hết hạn. Nếu pass, thêm thông tin giảm giá vào `result.AppliedDiscounts`."

**Phần 5 — Handler 4 (Thành viên):**

```csharp
public class MemberValidationHandler : OrderPipelineHandler
{
    private readonly IOrdersService _ordersService;

    public override async Task<OrderPipelineResult> HandleAsync(
        OrderPipelineRequest request, OrderPipelineResult result)
    {
        if (!result.IsValid) return result;

        if (request.Model.PointsRedeemed > 0)
        {
            if (string.IsNullOrEmpty(request.Model.Email))
            {
                result.IsValid = false;
                result.Message = "Cần email để sử dụng điểm tích lũy.";
                return result;
            }

            var member = await _ordersService
                .GetMemberByEmailAsync(request.Model.Email);

            if (member == null)
            {
                result.IsValid = false;
                result.Message = "Email không phải là thành viên.";
                return result;
            }

            if (member.Points < request.Model.PointsRedeemed)
            {
                result.IsValid = false;
                result.Message = $"Bạn chỉ có {member.Points} điểm. Không thể dùng {request.Model.PointsRedeemed} điểm.";
                return result;
            }
        }

        return _next != null
            ? await _next.HandleAsync(request, result)
            : result;
    }
}
```

> "`MemberValidationHandler` kiểm tra: email có trong hệ thống không, điểm tích lũy có đủ không."

**Phần 6 — Pipeline Builder (nối chuỗi):**

```csharp
public static class OrderPipelineBuilder
{
    public static OrderPipelineHandler Build(IOrdersService ordersService)
    {
        var validation = new ValidationHandler();
        var seats      = new SeatAvailabilityHandler(ordersService);
        var voucher    = new VoucherValidationHandler(ordersService);
        var member     = new MemberValidationHandler(ordersService);

        // Nối chuỗi: Validation → Seats → Voucher → Member
        validation.SetNext(seats).SetNext(voucher).SetNext(member);
        return validation;  // Trả về handler đầu tiên
    }
}
```

> "`OrderPipelineBuilder.Build()` tạo 4 handler rồi nối chuỗi bằng `SetNext()`. Trả về `validation` (handler đầu tiên). Khi gọi `validation.HandleAsync()`, nó sẽ tự động chạy qua cả chuỗi."

**Sơ đồ:**
```
ValidationHandler ──SetNext()──► SeatAvailabilityHandler ──SetNext()──► VoucherValidationHandler ──SetNext()──► MemberValidationHandler
       │                                    │                                    │                                    │
  Check: ShowtimeId,                Check: Ghế trống               Check: Voucher tồn tại,           Check: Email member,
  SelectedSeats, Max 10             (query DB)                      active, chưa hết hạn              điểm đủ
       │                                    │                                    │                                    │
  ❌ DỪNG nếu fail                 ❌ DỪNG nếu fail                ❌ DỪNG nếu fail                   ❌ DỪNG nếu fail
  ✅ Chuyển _next                   ✅ Chuyển _next                 ✅ Chuyển _next                    ✅ Return result
```

---

### 🔹 DEMO 9 — State Pattern (2 phút)

#### [20:30] — Chạy trên web

1. Đăng nhập Admin → `/Orders/ManageBookings`
2. Click **'Xác nhận'** đơn `Purchased` → `Purchased → Confirmed`
3. Click **'Hủy'** đơn `Confirmed` → `Confirmed → Cancelled`
4. Thử xác nhận đơn `Cancelled` → **Bị từ chối** ❌

#### [21:30] — Giải thích code chi tiết

**File `Data/State/OrderStateMachine.cs`:**

**Phần 1 — Interface State:**

```csharp
public interface IOrderState
{
    string StatusName { get; }  // Tên trạng thái
    bool CanTransitionTo(string newStatus);  // Có thể chuyển sang trạng thái X?
    Task OnEnterAsync(Order order, AppDbContext context);  // Xử lý khi vào trạng thái này
}
```

**Phần 2 — 4 State implementations:**

```csharp
// State 1: Purchased (Mới đặt)
public class PurchasedState : IOrderState
{
    public string StatusName => "Purchased";

    // Từ Purchased → chỉ được Confirmed hoặc Cancelled
    public bool CanTransitionTo(string newStatus)
        => newStatus is "Confirmed" or "Cancelled";

    public Task OnEnterAsync(Order order, AppDbContext context)
    {
        // Mới đặt — chưa cần xử lý gì thêm
        return Task.CompletedTask;
    }
}

// State 2: Confirmed (Đã xác nhận)
public class ConfirmedState : IOrderState
{
    public string StatusName => "Confirmed";

    // Từ Confirmed → chỉ được Cancelled hoặc Refunded
    public bool CanTransitionTo(string newStatus)
        => newStatus is "Cancelled" or "Refunded";

    public Task OnEnterAsync(Order order, AppDbContext context)
    {
        // Đã xác nhận — có thể sinh QR code vé
        return Task.CompletedTask;
    }
}

// State 3: Cancelled (Đã hủy — TERMINAL)
public class CancelledState : IOrderState
{
    public string StatusName => "Cancelled";

    // TRẠNG THÁI CUỐI — KHÔNG chuyển sang bất kỳ trạng thái nào
    public bool CanTransitionTo(string newStatus) => false;

    public async Task OnEnterAsync(Order order, AppDbContext context)
    {
        // Giải phóng ghế + hoàn điểm tích lũy
        if (!string.IsNullOrEmpty(order.Email))
        {
            var member = await context.Members
                .FirstOrDefaultAsync(m => m.Email.ToLower() == order.Email.ToLower());
            if (member != null)
            {
                double finalPrice = Math.Max(0, order.TotalPrice - order.DiscountAmount);
                int earned = (int)(finalPrice / 10000);
                // Trừ điểm đã tích + cộng lại điểm đã dùng
                member.Points = Math.Max(0, member.Points - earned + (order.PointsRedeemed / 1000));
            }
            await context.SaveChangesAsync();
        }
    }
}

// State 4: Refunded (Hoàn tiền — TERMINAL)
public class RefundedState : IOrderState
{
    public string StatusName => "Refunded";

    public bool CanTransitionTo(string newStatus) => false;  // TERMINAL

    public async Task OnEnterAsync(Order order, AppDbContext context)
    {
        // Hoàn tiền + hoàn điểm (logic tương tự Cancelled)
        if (!string.IsNullOrEmpty(order.Email))
        {
            var member = await context.Members
                .FirstOrDefaultAsync(m => m.Email.ToLower() == order.Email.ToLower());
            if (member != null)
            {
                double finalPrice = Math.Max(0, order.TotalPrice - order.DiscountAmount);
                int earned = (int)(finalPrice / 10000);
                member.Points = Math.Max(0, member.Points - earned + (order.PointsRedeemed / 1000));
            }
            await context.SaveChangesAsync();
        }
    }
}
```

**Phần 3 — State Machine (quản lý tất cả states):**

```csharp
public class OrderStateMachine
{
    private static readonly Dictionary<string, IOrderState> _states = new()
    {
        ["Purchased"] = new PurchasedState(),
        ["Confirmed"] = new ConfirmedState(),
        ["Cancelled"] = new CancelledState(),
        ["Refunded"]  = new RefundedState(),
    };

    // Kiểm tra chuyển đổi hợp lệ
    public static bool CanTransition(string from, string to)
    {
        if (!_states.TryGetValue(from, out var state))
            return false;
        return state.CanTransitionTo(to);
    }

    // Lấy state theo tên
    public static IOrderState? GetState(string statusName)
        => _states.GetValueOrDefault(statusName);

    // Kiểm tra status name có hợp lệ không
    public static bool IsValidStatus(string statusName)
        => _states.ContainsKey(statusName);
}
```

> "`OrderStateMachine` dùng `Dictionary<string, IOrderState>` để quản lý. `CanTransition()` lookup state theo `from`, gọi `CanTransitionTo(to)`. `GetState()` trả về state object để gọi `OnEnterAsync()`."

**Cách sử dụng trong OrdersService** (file `Data/Services/OrdersService.cs`, dòng 224–269):

```csharp
public async Task<StatusChangeResult> ChangeOrderStatusWithStateAsync(int orderId, string newStatus)
{
    var order = await _context.Orders
        .Include(o => o.OrderItems)
        .FirstOrDefaultAsync(o => o.Id == orderId);

    if (order == null)
        return new StatusChangeResult { Success = false, Message = "Đơn hàng không tồn tại." };

    string oldStatus = order.Status;

    // Kiểm tra đã ở trạng thái đó chưa
    if (oldStatus == newStatus)
        return new StatusChangeResult
        {
            Success = false,
            Message = $"Đơn hàng đã ở trạng thái [{newStatus}]."
        };

    // STATE PATTERN — Kiểm tra transition hợp lệ
    if (!OrderStateMachine.CanTransition(oldStatus, newStatus))
        return new StatusChangeResult
        {
            Success = false,
            Message = $"Không thể chuyển từ [{oldStatus}] sang [{newStatus}]."
        };

    // Cập nhật trạng thái
    order.Status = newStatus;

    // STATE PATTERN — Gọi OnEnterAsync của state mới
    var state = OrderStateMachine.GetState(newStatus);
    if (state != null)
        await state.OnEnterAsync(order, _context);

    await _context.SaveChangesAsync();

    return new StatusChangeResult
    {
        Success = true,
        Message = $"Đơn hàng đã chuyển sang [{newStatus}].",
        OldStatus = oldStatus,
        NewStatus = newStatus
    };
}
```

> "**Luồng hoạt động:** Controller gọi `ChangeOrderStatusWithStateAsync(orderId, "Cancelled")`. Service query order, kiểm tra `OrderStateMachine.CanTransition("Purchased", "Cancelled")` → `true`. Cập nhật `order.Status = "Cancelled"`. Gọi `CancelledState.OnEnterAsync()` → hoàn điểm. Lưu DB."

**Sơ đồ chuyển trạng thái:**
```
                    ┌───────────┐
                    │ Purchased │ (Mới đặt)
                    └─────┬─────┘
                          │
              ┌───────────┴───────────┐
              ▼                       ▼
        ┌───────────┐          ┌───────────┐
        │ Confirmed │          │ Cancelled │ ← TERMINAL
        └─────┬─────┘          └───────────┘
              │
        ┌─────┴─────┐
        ▼           ▼
  ┌───────────┐  ┌───────────┐
  │ Cancelled │  │ Refunded  │ ← TERMINAL
  └───────────┘  └───────────┘
```

> "Không thể: `Cancelled → Confirmed`, `Refunded → Purchased`, v.v. State Pattern bảo vệ vòng đời đơn hàng."

---

### 🔹 DEMO 10 — Observer Pattern (2 phút)

#### [22:30] — Chạy trên web

1. Vào `/Orders/ManageBookings`
2. Mở Visual Studio **Output** window
3. Click **'Xác nhận'** đơn → Output hiển thị:
   ```
   [AUDIT] Order #5 | Purchased → Confirmed | Total: 187,000VND
   [EMAIL] To: user@gmail.com | Subject: [MovieCinema] Xac nhan don hang #5
   ```
4. SQL Server → bảng Members → điểm đã cộng
5. Click **'Hủy'** đơn khác → Output:
   ```
   [AUDIT] Order #6 | Purchased → Cancelled
   [EMAIL] To: user2@gmail.com | Subject: Don hang #6 da bi huy
   ```

#### [23:30] — Giải thích code chi tiết

**File `Data/Observer/OrderObserver.cs`:**

**Phần 1 — Observer Interface:**

```csharp
public interface IOrderObserver
{
    Task OnOrderStatusChangedAsync(Order order, string oldStatus, string newStatus);
}
```

> "Mọi observer phải implement `OnOrderStatusChangedAsync`. Khi trạng thái đơn hàng thay đổi, Subject sẽ gọi method này trên tất cả observers."

**Phần 2 — Subject Interface:**

```csharp
public interface IOrderSubject
{
    void Attach(IOrderObserver observer);   // Đăng ký observer
    void Detach(IOrderObserver observer);   // Hủy observer
    Task NotifyAsync(Order order, string oldStatus, string newStatus);  // Thông báo tất cả
}
```

**Phần 3 — Subject triển khai:**

```csharp
public class OrderSubject : IOrderSubject
{
    private readonly List<IOrderObserver> _observers = new();
    private readonly IServiceScopeFactory _scopeFactory;
    private bool _initialized;
    private readonly object _lock = new();  // Thread-safe

    public OrderSubject(IServiceScopeFactory scopeFactory)
    {
        _scopeFactory = scopeFactory;
    }

    public void Attach(IOrderObserver observer)
    {
        lock (_lock) { _observers.Add(observer); }
    }

    public void Detach(IOrderObserver observer)
    {
        lock (_lock) { _observers.Remove(observer); }
    }

    // Lấy observers từ DI (lazy initialization)
    private List<IOrderObserver> GetScopedObservers()
    {
        lock (_lock)
        {
            if (_initialized) return _observers.ToList();
        }

        using var scope = _scopeFactory.CreateScope();
        var scopedObservers = scope.ServiceProvider
            .GetServices<IOrderObserver>()
            .ToList();

        lock (_lock)
        {
            foreach (var obs in scopedObservers)
                if (!_observers.Contains(obs))
                    _observers.Add(obs);
            _initialized = true;
        }

        return _observers.ToList();
    }

    // Thông báo TẤT CẢ observers
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
                // Log lỗi nhưng KHÔNG ngăn observers khác
            }
        }
    }
}
```

> "**`OrderSubject` là Singleton** (đăng ký `AddSingleton` trong `Program.cs`). Dùng `IServiceScopeFactory` để tạo scope mới cho observers — đảm bảo observer có thể truy cập scoped services. `lock(_lock)` đảm bảo thread-safe. **Quan trọng:** try-catch riêng từng observer — nếu `EmailNotificationObserver` lỗi, `AuditLogObserver` và `LoyaltyPointsObserver` vẫn chạy."

**Phần 4 — Observer 1 (Audit Log):**

```csharp
public class AuditLogObserver : IOrderObserver
{
    private readonly ILogger<AuditLogObserver> _logger;

    public AuditLogObserver(ILogger<AuditLogObserver> logger)
    {
        _logger = logger;
    }

    public Task OnOrderStatusChangedAsync(Order order, string oldStatus, string newStatus)
    {
        _logger.LogInformation(
            "[AUDIT] Order #{OrderId} | Email: {Email} | {Old} → {New} | Total: {Total:N0}VND | Date: {Date}",
            order.Id,
            string.IsNullOrEmpty(order.Email) ? "(guest)" : order.Email,
            oldStatus,
            newStatus,
            order.TotalPrice - order.DiscountAmount,
            order.OrderDate);
        return Task.CompletedTask;
    }
}
```

> "`AuditLogObserver`: ghi log mỗi khi trạng thái thay đổi. Dùng `ILogger` để log ra console/file."

**Phần 5 — Observer 2 (Loyalty Points):**

```csharp
public class LoyaltyPointsObserver : IOrderObserver
{
    private readonly IServiceScopeFactory _scopeFactory;

    public LoyaltyPointsObserver(IServiceScopeFactory scopeFactory)
    {
        _scopeFactory = scopeFactory;
    }

    public async Task OnOrderStatusChangedAsync(Order order, string oldStatus, string newStatus)
    {
        if (string.IsNullOrEmpty(order.Email)) return;

        // Tạo scope mới để truy cập AppDbContext
        using var scope = _scopeFactory.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var member = await context.Members
            .FirstOrDefaultAsync(m => m.Email.ToLower() == order.Email.ToLower());

        if (member == null) return;

        double finalPrice = Math.Max(0, order.TotalPrice - order.DiscountAmount);
        int earned = (int)(finalPrice / 10000);  // 10,000đ = 1 điểm

        if (newStatus == "Cancelled" || newStatus == "Refunded")
        {
            // Hủy/Hoàn → Trừ điểm đã tích + Cộng lại điểm đã dùng
            member.Points = Math.Max(0, member.Points - earned + (order.PointsRedeemed / 1000));
        }
        else if (newStatus == "Confirmed" && (oldStatus == "Purchased"))
        {
            // Xác nhận → Cộng điểm mới
            member.Points += earned;
        }

        await context.SaveChangesAsync();
    }
}
``

> "`LoyaltyPointsObserver`: khi `Confirmed` từ `Purchased` → cộng điểm (10,000đ = 1 điểm). Khi `Cancelled`/`Refunded` → trừ điểm đã tích + cộng lại điểm đã dùng. Dùng `IServiceScopeFactory` để tạo scope mới truy cập `AppDbContext`."

**Phần 6 — Observer 3 (Email):**

```csharp
public class EmailNotificationObserver : IOrderObserver
{
    private readonly ILogger<EmailNotificationObserver> _logger;

    public Task OnOrderStatusChangedAsync(Order order, string oldStatus, string newStatus)
    {
        if (string.IsNullOrEmpty(order.Email)) return Task.CompletedTask;

        var (subject, body) = newStatus switch
        {
            "Confirmed" => (
                $"[MovieCinema] Xac nhan don hang #{order.Id}",
                $"Don hang #{order.Id} da duoc xac nhan. Tong cong: {(order.TotalPrice - order.DiscountAmount):N0}VND"
            ),
            "Cancelled" => (
                $"[MovieCinema] Don hang #{order.Id} da bi huy",
                $"Don hang #{order.Id} da duoc huy. Tien se duoc hoan trong 3-5 ngay lam viec."
            ),
            "Refunded" => (
                $"[MovieCinema] Hoan tien don hang #{order.Id}",
                $"Don hang #{order.Id} da duoc hoan tien."
            ),
            _ => (null as string, null as string)
        };

        if (subject != null)
        {
            // Stub: thay bang IEmailService thuc te (SendGrid, SMTP, ...)
            _logger.LogInformation("[EMAIL] To: {Email} | Subject: {Subject}", order.Email, subject);
        }

        return Task.CompletedTask;
    }
}
```

> "`EmailNotificationObserver`: gửi email theo trạng thái. Hiện tại stub (log ra console) — khi production thay bằng `IEmailService` thật."

**Đăng ký trong DI** (file `Program.cs`, dòng 68–72):

```csharp
builder.Services.AddSingleton<IOrderSubject, OrderSubject>();  // Singleton!

builder.Services.AddScoped<IOrderObserver, AuditLogObserver>();
builder.Services.AddScoped<IOrderObserver, LoyaltyPointsObserver>();
builder.Services.AddScoped<IOrderObserver, EmailNotificationObserver>();
```

> "**Singleton vs Scoped:** `OrderSubject` là Singleton (1 instance duy nhất quản lý observers). Các Observer là Scoped (mỗi request có instance riêng). Dùng `IServiceScopeFactory` để bridge giữa Singleton và Scoped."

**Sơ đồ:**
```
OrdersService.ChangeOrderStatusWithStateAsync(orderId, "Confirmed")
        │
        ▼
OrderSubject.NotifyAsync(order, "Purchased", "Confirmed")
        │
        ├──► AuditLogObserver.OnOrderStatusChangedAsync()  → Log [AUDIT]
        ├──► LoyaltyPointsObserver.OnOrderStatusChangedAsync()  → Cộng điểm
        └──► EmailNotificationObserver.OnOrderStatusChangedAsync()  → Gửi email
        │
        ▼ (mỗi observer chạy độc lập, try-catch riêng)
```

---

### 🔹 DEMO 11 — Mediator Pattern (2 phút)

#### [24:30] — Chạy trên web

1. Đặt vé mới → thành công
2. Admin → `/Orders/ManageBookings`
3. Click **'Xác nhận'** → thành công
4. Click **'Hủy'** → thành công
5. Tất cả 3 hành động đều qua Mediator

#### [25:30] — Giải thích code chi tiết

**File `Data/Mediator/BookingMediator.cs`:**

**Phần 1 — Mediator Interface:**

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
```

> "Giống MediatR: `IMediator` nhận request, tìm handler tương ứng, gọi `HandleAsync()`. Request và Response là generic type."

**Phần 2 — Request/Response classes:**

```csharp
// Request 1: Đặt vé
public class CompleteBookingRequest : IRequest<CompleteBookingResponse>
{
    public BookTicketsVM Model { get; set; } = null!;
    public string? UserId { get; set; }
}

public class CompleteBookingResponse
{
    public bool Success { get; set; }
    public string Message { get; set; } = "";
    public int? OrderId { get; set; }
    public double FinalPrice { get; set; }
    public double DiscountApplied { get; set; }
    public List<string> AppliedDiscounts { get; set; } = new();
}

// Request 2: Hủy vé
public class CancelBookingRequest : IRequest<CancelBookingResponse>
{
    public int OrderId { get; set; }
}

public class CancelBookingResponse
{
    public bool Success { get; set; }
    public string Message { get; set; } = "";
}

// Request 3: Xác nhận vé
public class ConfirmBookingRequest : IRequest<ConfirmBookingResponse>
{
    public int OrderId { get; set; }
}

public class ConfirmBookingResponse
{
    public bool Success { get; set; }
    public string Message { get; set; } = "";
}
```

> "Mỗi request có response riêng. `CompleteBookingRequest` mang `BookTicketsVM` + `UserId`. `CancelBookingRequest` chỉ cần `OrderId`."

**Phần 3 — Mediator Implementation (tự tìm handler):**

```csharp
public class AppMediator : IMediator
{
    private readonly IServiceProvider _serviceProvider;

    public AppMediator(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public async Task<TResponse> SendAsync<TResponse>(IRequest<TResponse> request)
    {
        // Tự động tìm handler dựa trên kiểu request
        var handlerType = typeof(IRequestHandler<,>)
            .MakeGenericType(request.GetType(), typeof(TResponse));

        // Tìm handler từ DI container
        var handler = _serviceProvider.GetServices(handlerType).FirstOrDefault();

        if (handler == null)
            throw new InvalidOperationException(
                $"Handler not found for request type {request.GetType().Name}");

        // Gọi HandleAsync qua reflection
        var method = handlerType.GetMethod("HandleAsync");
        if (method == null)
            throw new InvalidOperationException($"HandleAsync not found on {handlerType.Name}");

        var result = method.Invoke(handler, new[] { request });

        if (result is Task<TResponse> task)
            return await task;

        throw new InvalidOperationException($"Handler did not return Task<TResponse>");
    }
}
```

> "**Cách hoạt động:** `AppMediator` dùng reflection để tự tìm handler. Ví dụ: request là `CompleteBookingRequest`, response là `CompleteBookingResponse` → tìm `IRequestHandler<CompleteBookingRequest, CompleteBookingResponse>` từ DI. Rồi gọi `HandleAsync()` qua reflection."

**Phần 4 — Handler 1 (CompleteBooking):**

```csharp
public class CompleteBookingHandler
    : IRequestHandler<CompleteBookingRequest, CompleteBookingResponse>
{
    private readonly IBookingFacade _facade;
    private readonly IOrdersService _ordersService;

    public CompleteBookingHandler(IBookingFacade facade, IOrdersService ordersService)
    {
        _facade = facade;
        _ordersService = ordersService;
    }

    public async Task<CompleteBookingResponse> HandleAsync(CompleteBookingRequest request)
    {
        // ── Bước 1: Validate qua CHAIN OF RESPONSIBILITY ──
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

        // ── Bước 2: Xử lý đặt vé qua FACADE ──
        var bookingResult = await _facade.ProcessBookingAsync(request.Model, request.UserId);

        if (!bookingResult.Success)
        {
            return new CompleteBookingResponse
            {
                Success = false,
                Message = bookingResult.Message
            };
        }

        return new CompleteBookingResponse
        {
            Success = true,
            Message = "Đặt vé thành công!",
            OrderId = bookingResult.OrderId,
            FinalPrice = bookingResult.FinalPrice,
            DiscountApplied = bookingResult.DiscountApplied,
            AppliedDiscounts = pipelineResult.AppliedDiscounts
        };
    }
}
```

> "**`CompleteBookingHandler` kết hợp nhiều pattern:** Chain of Responsibility (validate) → Facade (xử lý booking) → Bridge (tính giá) → Strategy (thanh toán) → Builder (tạo Order). Mediator điều phối tất cả."

**Phần 5 — Handler 2 (CancelBooking):**

```csharp
public class CancelBookingHandler
    : IRequestHandler<CancelBookingRequest, CancelBookingResponse>
{
    private readonly IOrdersService _ordersService;

    public CancelBookingHandler(IOrdersService ordersService)
    {
        _ordersService = ordersService;
    }

    public async Task<CancelBookingResponse> HandleAsync(CancelBookingRequest request)
    {
        // Sử dụng STATE PATTERN để chuyển trạng thái an toàn
        var result = await _ordersService.ChangeOrderStatusWithStateAsync(
            request.OrderId, "Cancelled");

        return new CancelBookingResponse
        {
            Success = result.Success,
            Message = result.Success
                ? "Hủy đơn hàng thành công."
                : result.Message
        };
    }
}
```

> "`CancelBookingHandler` gọi `ChangeOrderStatusWithStateAsync()` — kết hợp **State Pattern** (kiểm tra transition hợp lệ) và **Observer Pattern** (thông báo khi status đổi)."

**Phần 6 — Controller gọi Mediator:**

```csharp
// File Controllers/OrdersController.cs

[HttpPost]
public async Task<IActionResult> ConfirmBooking(int id)
{
    // Controller chỉ gọi _mediator.SendAsync()
    var result = await _mediator.SendAsync(new ConfirmBookingRequest { OrderId = id });

    if (!result.Success)
        TempData["BookingError"] = result.Message;

    return RedirectToAction(nameof(ManageBookings));
}

[HttpPost]
public async Task<IActionResult> CancelBooking(int id)
{
    var result = await _mediator.SendAsync(new CancelBookingRequest { OrderId = id });

    if (!result.Success)
        TempData["BookingError"] = result.Message;

    return RedirectToAction(nameof(ManageBookings));
}
```

> "**Mediator = Tổng đài điện thoại.** Bạn gọi tổng đài (gửi request), tổng đài tự tìm đúng bộ phận (handler) để xử lý. Controller không cần biết handler nào xử lý request nào."

**Đăng ký trong DI** (file `Program.cs`, dòng 75–82):

```csharp
builder.Services.AddScoped<IMediator, AppMediator>();

builder.Services.AddScoped<IRequestHandler<CompleteBookingRequest, CompleteBookingResponse>,
                            CompleteBookingHandler>();
builder.Services.AddScoped<IRequestHandler<CancelBookingRequest, CancelBookingResponse>,
                            CancelBookingHandler>();
builder.Services.AddScoped<IRequestHandler<ConfirmBookingRequest, ConfirmBookingResponse>,
                            ConfirmBookingHandler>();
```

**Sơ đồ:**
```
OrdersController
    │
    │ _mediator.SendAsync(new ConfirmBookingRequest { OrderId = 5 })
    ▼
AppMediator.SendAsync()
    │
    │ Reflection: tìm IRequestHandler<ConfirmBookingRequest, ConfirmBookingResponse>
    ▼
ConfirmBookingHandler.HandleAsync()
    │
    │ _ordersService.ChangeOrderStatusWithStateAsync(orderId, "Confirmed")
    ▼
OrdersService (kết hợp State + Observer)
    │
    ├── OrderStateMachine.CanTransition("Purchased", "Confirmed") → true
    ├── order.Status = "Confirmed"
    ├── ConfirmedState.OnEnterAsync()
    └── OrderSubject.NotifyAsync() → 3 Observers chạy
```

---

## PHẦN 3: TỔNG KẾT & LƯU Ý (1–2 phút)

### [27:00] — Sơ đồ phối hợp 7 Pattern trong 1 lần đặt vé

```
Khách hàng nhấn "Đặt vé"
        │
        ▼
[11. MEDIATOR] Controller → AppMediator
        │
        ▼
[10. CHAIN] Validate 4 bước: Validation → Seats → Voucher → Member
        │
        ▼ (nếu pass)
[7. FACADE] BookingFacade.ProcessBookingAsync()
        │
        ├──► [3. BRIDGE] SeatPricingBridge.GetPrice() — tính giá theo loại ghế
        ├──► [6. DECORATOR] OrderPriceCalculator — xếp chồng giảm giá
        ├──► [4. STRATEGY] PaymentContext.PayAsync() — thanh toán Cash/PayPal
        └──► [5. BUILDER] new OrderBuilder().Build() — tạo Order
        │
        ▼
[1. REPOSITORY] EntityBaseRepository<T>.AddAsync() — lưu vào DB
        │
        ▼
[8. OBSERVER] OrderSubject.NotifyAsync() — 3 observers chạy
        ├── AuditLogObserver → Log
        ├── LoyaltyPointsObserver → Cộng/trừ điểm
        └── EmailNotificationObserver → Gửi email
        │
        ▼
[9. STATE] OrderStateMachine — quản lý vòng đời Order
        │
        ▼
[2. PROXY] CachedMoviesServiceProxy — cache danh sách phim
```

### [28:00] — Bảng tổng hợp

| # | Pattern | Nhóm | File chính | Vấn đề giải quyết |
|---|---------|------|-----------|-------------------|
| 1 | Repository | Creational | `EntityBaseRepository<T>` | Chuẩn hóa CRUD, tái sử dụng code |
| 2 | Builder | Creational | `OrderBuilder.cs` | Tạo Order nhiều trường dễ đọc |
| 3 | Bridge | Structural | `SeatPricingBridge.cs` | Tách loại ghế khỏi thuật toán giá |
| 4 | Decorator | Structural | `PricingDecorators.cs` | Xếp chồng giảm giá linh hoạt |
| 5 | Proxy | Structural | `CachedMoviesServiceProxy.cs` | Cache, tăng tốc đọc |
| 6 | Facade | Structural | `BookingFacade.cs` | Đơn giản hóa Controller |
| 7 | Strategy | Behavioral | `PaymentStrategy.cs` | Đổi phương thức thanh toán |
| 8 | Observer | Behavioral | `OrderObserver.cs` | Phản ứng khi trạng thái đổi |
| 9 | State | Behavioral | `OrderStateMachine.cs` | Bảo vệ vòng đời Order |
| 10 | Chain | Behavioral | `OrderPipeline.cs` | Validate tuần tự, dễ mở rộng |
| 11 | Mediator | Behavioral | `BookingMediator.cs` | Giảm coupling Controller–Handler |

### [29:00] — Kết thúc

> "Qua video demo, mình hy vọng các bạn thấy được rằng Design Patterns không phải là lý thuyết suông — chúng được áp dụng thực tế trong từng dòng code. Khi cần thêm phương thức thanh toán, loại ghế, chương trình khuyến mãi, kênh thông báo — chỉ cần tạo class mới, không sửa code cũ. Đây chính là sức mạnh của SOLID Principles và Design Patterns. Cảm ơn thầy/cô và các bạn đã theo dõi!"

---

## 📋 CHECKLIST KHI QUAY

| # | Việc cần làm | ✅ |
|---|--------------|---|
| 1 | Mở Visual Studio với solution MovieCinema | ☐ |
| 2 | Chạy `dotnet run`, kiểm tra web hoạt động | ☐ |
| 3 | Mở SQL Server, kiểm tra DB có seed data | ☐ |
| 4 | Chuẩn bị sẵn 1 đơn hàng `Purchased` để demo State/Observer | ☐ |
| 5 | Chuẩn bị sẵn voucher còn hạn (ví dụ: `SALE20`) | ☐ |
| 6 | Chuẩn bị tài khoản Admin + Member | ☐ |
| 7 | Mở browser zoom 110-125% cho khán giả thấy rõ | ☐ |
| 8 | Test audio mic trước khi quay | ☐ |
| 9 | Chạy thử 1 lần để tính thời gian thực tế | ☐ |
| 10 | Tắt thông báo trình duyệt & ứng dụng | ☐ |

---

## ⏱️ BẢNG THỜI LƯỢNG

| Phần | Nội dung | Thời lượng |
|------|----------|-----------|
| Phần 1 | Giới thiệu sản phẩm | 3–4 phút |
| Demo 1 | Repository + giải thích code | 1.5 phút |
| Demo 2 | Proxy + giải thích code | 2 phút |
| Demo 3 | Bridge + giải thích code | 2 phút |
| Demo 4 | Strategy + giải thích code | 2 phút |
| Demo 5 | Builder + giải thích code | 2 phút |
| Demo 6 | Decorator + giải thích code | 2 phút |
| Demo 7 | Facade + giải thích code | 2 phút |
| Demo 8 | Chain + giải thích code | 2 phút |
| Demo 9 | State + giải thích code | 2 phút |
| Demo 10 | Observer + giải thích code | 2 phút |
| Demo 11 | Mediator + giải thích code | 2 phút |
| Phần 3 | Tổng kết | 1–2 phút |
| **Tổng** | | **~25–30 phút** |

> **Rút ngắn xuống 15 phút:** Bỏ Demo 1 (Repository), Demo 5 (Builder), Demo 6 (Decorator) → tiết kiệm ~6 phút.
