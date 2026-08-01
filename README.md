# MovieCinema — Hệ thống đặt vé xem phim

> Hệ thống quản lý rạp chiếu phim và đặt vé trực tuyến với .NET Core MVC, Entity Framework Core, và 10 Design Patterns (GoF).

---

## Mục lục

1. [Tổng quan](#tổng-quan)
2. [Design Patterns đã áp dụng](#design-patterns-đã-áp-dụng)
3. [Kiến trúc hệ thống](#kiến-trúc-hệ-thống)
4. [Cấu trúc project](#cấu-trúc-project)
5. [Bắt đầu](#bắt-đầu)
6. [Tài khoản mặc định](#tài-khoản-mặc-định)

---

## Tổng quan

**MovieCinema** là hệ thống quản lý rạp chiếu phim bao gồm:

- **Quản lý nội dung:** Phim, Diễn viên, Đạo diễn, Thể loại, Rạp chiếu, Phòng chiếu, Ghế ngồi
- **Quản lý lịch chiếu:** Tạo suất chiếu theo phòng/phim/ngày
- **Đặt vé:** Chọn ghế, tính giá theo loại ghế (Standard/VIP/Couple/Disabled), thanh toán
- **Khuyến mãi:** Voucher (giảm % hoặc giảm cố định), điểm tích lũy thành viên, Happy Hour
- **Báo cáo:** Doanh thu theo ngày/tháng/phim/rạp, tỷ lệ lấp đầy suất chiếu

**Công nghệ:**
- .NET 8 / ASP.NET Core MVC
- Entity Framework Core (Code-First, SQL Server)
- ASP.NET Identity (Authentication + Authorization)
- Session-based Shopping Cart
- Bootstrap 5 + Custom CSS

---

## Design Patterns đã áp dụng

Hệ thống áp dụng **10/23 GoF Design Patterns**, được tổ chức theo 3 nhóm:

### Nhóm Creational (Khởi tạo)

#### 1. Singleton — ShoppingCart
**File:** `Data/Cart/ShoppingCart.cs`

Đảm bảo mỗi session người dùng có một ShoppingCart instance duy nhất.

```csharp
builder.Services.AddScoped(sc => ShoppingCart.GetShoppingCart(sc));
```

---

### Nhóm Structural (Cấu trúc)

#### 2. Bridge — Tính giá theo loại ghế
**File:** `Models/Bridge/SeatPricingBridge.cs`

Tách rời **abstraction** (cách tính giá) khỏi **implementation** (quy tắc tính giá theo loại ghế). Mỗi loại ghế có strategy riêng, dễ thêm loại ghế mới.

```
ISeatingPricingStrategy
├── StandardPricingStrategy  → basePrice × 1.0
├── VipPricingStrategy      → basePrice × 1.2
├── CouplePricingStrategy  → basePrice × 2.0
└── DisabledPricingStrategy → basePrice × 0.5
```

#### 3. Decorator — Xếp chồng khuyến mãi
**File:** `Data/Decorators/PricingDecorators.cs`

Gắn thêm khuyến mãi một cách linh hoạt bằng cách xếp chồng decorators:

```
BasePriceCalculator
    └── VoucherDecorator     (giảm % hoặc giảm cố định)
        └── LoyaltyPointsDecorator  (đổi điểm)
            └── HappyHourDecorator   (giảm 15% từ 14:00–17:00)
```

Mỗi decorator có `Priority` để đảm bảo thứ tự áp dụng đúng.

#### 4. Proxy — Cache danh sách phim
**File:** `Data/Proxy/CachedMoviesServiceProxy.cs`

Proxy bao bọc `MoviesService`, cache kết quả với `IMemoryCache`:

```csharp
// Cache 10 phút
GetAllAsync()           → "movies:all"
GetNowShowingMoviesAsync() → "movies:nowshowing:{yyyyMMdd}"
GetComingSoonMoviesAsync() → "movies:comingsoon:{yyyyMMdd}"
GetByIdAsync(id)        → "movies:id:{id}"
```

Cache được invalidate khi `Add`/`Update`/`Delete` phim.

#### 5. Adapter — Tích hợp thanh toán
**File:** `Data/Strategy/PaymentStrategy.cs`

Chuyển đổi interface của các cổng thanh toán (PayPal, Cash) thành interface thống nhất `IPaymentStrategy`.

---

### Nhóm Behavioral (Hành vi)

#### 6. Facade — Luồng đặt vé
**File:** `Data/Facade/BookingFacade.cs`

Gói gọn toàn bộ luồng đặt vé phức tạp vào một method duy nhất:

```
ProcessBookingAsync(BookTicketsVM)
  1. Validate ModelState
  2. Lấy Showtime → kiểm tra tồn tại
  3. Parse danh sách ghế đã chọn
  4. Kiểm tra ghế đã bị đặt chưa
  5. Tính giá theo loại ghế (Bridge)
  6. Áp dụng voucher (Decorator)
  7. Thanh toán (Strategy)
  8. Tạo Order (Builder)
  9. Trả về BookingResult
```

`OrdersController.BookTickets` POST giảm từ ~90 dòng xuống ~30 dòng.

#### 7. Builder — Tạo Order
**File:** `Models/Builders/OrderBuilder.cs`

Tách rời việc xây dựng Order phức tạp, code sạch và dễ đọc:

```csharp
var order = new OrderBuilder()
    .SetCustomer(name, email, userId)
    .SetShowtime(showtimeId, seats, count, basePrice)
    .ApplyVoucher(discount, total)
    .RedeemPoints(points, total)
    .SetPaymentMethod(method)
    .CalculateTotal()
    .Build();
```

#### 8. Strategy — Thanh toán đa phương thức
**File:** `Data/Strategy/PaymentStrategy.cs`

Mỗi phương thức thanh toán là một strategy riêng, dễ thêm mà không sửa code cũ:

```
IPaymentStrategy
├── CashPaymentStrategy     → Thanh toán tại quầy
└── PayPalPaymentStrategy   → PayPal API (stub)
```

`PaymentContext.SetStrategyByName("paypal")` → chọn strategy phù hợp tại runtime.

#### 9. State — Quản lý trạng thái Order
**File:** `Data/State/OrderStateMachine.cs`

Mỗi trạng thái Order là một state class riêng với logic và transition rules:

```
┌─────────────┐  confirm  ┌────────────┐
│ Purchased   │ ─────────→│ Confirmed │
└──────┬──────┘           └──────┬─────┘
       │ cancel                  │ cancel / refund
       ▼                        ▼
┌─────────────┐           ┌────────────┐
│ Cancelled   │           │  Refunded  │
└─────────────┘           └────────────┘
```

`ChangeOrderStatusWithStateAsync()` kiểm tra transition hợp lệ trước khi đổi state.

#### 10. Observer — Thông báo khi Order đổi trạng thái
**File:** `Data/Observer/OrderObserver.cs`

Khi Order chuyển trạng thái → tất cả observers được notify tự động:

```
OrderSubject (Subject)
├── AuditLogObserver       → Ghi log [AUDIT] khi status đổi
├── LoyaltyPointsObserver  → Cộng/trừ điểm thành viên
└── EmailNotificationObserver → Log email thông báo (stub)
```

#### 11. Chain of Responsibility — Validate đơn hàng
**File:** `Data/Chain/OrderPipeline.cs`

Pipeline xử lý đơn hàng qua chuỗi handlers độc lập:

```
ValidationHandler         → Kiểm tra dữ liệu hợp lệ (ghế > 0, ≤ 10)
        ↓
SeatAvailabilityHandler   → Ghế đã bị đặt chưa?
        ↓
VoucherValidationHandler → Voucher còn hạn & active?
        ↓
MemberValidationHandler  → Member đủ điểm để đổi?
```

Mỗi handler độc lập, dễ thêm bước mới mà không ảnh hưởng các bước khác.

#### 12. Mediator — Tập trung hóa giao tiếp
**File:** `Data/Mediator/BookingMediator.cs`

Mediator điều phối giữa Facade, Chain, và các services, giảm coupling tối đa:

```
IMediator.SendAsync(CompleteBookingRequest)
  → CompleteBookingHandler
      ├── Chain of Responsibility (validate)
      └── BookingFacade (process)
```

---

## Kiến trúc hệ thống

```
┌─────────────────────────────────────────────────────────┐
│                    Controllers (11)                     │
│  Movies | Orders | Actors | Cinemas | Showtimes | ...  │
└────────────────────────┬────────────────────────────────┘
                         │ DI (AddScoped)
┌────────────────────────▼────────────────────────────────┐
│              Design Patterns Layer                        │
│  ┌──────────┐ ┌──────────┐ ┌──────────┐              │
│  │  Facade  │ │ Mediator │ │  Chain   │              │
│  └────┬─────┘ └────┬─────┘ └────┬─────┘              │
│  ┌────▼─────┐ ┌────▼─────┐ ┌────▼─────┐              │
│  │  Builder  │ │  State   │ │ Observer │              │
│  └────┬─────┘ └────┬─────┘ └────┬─────┘              │
│  ┌────▼─────┐ ┌────▼─────┐ ┌────▼─────┐ ┌──────────┐ │
│  │  Bridge  │ │ Strategy │ │Decorator │ │   Proxy   │ │
│  └────┬─────┘ └────┬─────┘ └────┬─────┘ └────┬─────┘ │
│       └─────────────┴─────────────┘            │        │
│  ┌──────────────────────────────────────────────▼──┐    │
│  │              Services Layer                     │    │
│  │  IMoviesService | IOrdersService | ISeatsService │
│  └────────────────────────┬──────────────────────────┘    │
│                            │                               │
│  ┌────────────────────────▼──────────────────────────┐  │
│  │              AppDbContext (EF Core)                │  │
│  │           SQL Server — MovieCinema                  │  │
│  └─────────────────────────────────────────────────────┘  │
└─────────────────────────────────────────────────────────┘
```

---

## Cấu trúc project

```
movieCinema/
├── Controllers/           # 11 MVC Controllers
├── Models/
│   ├── Bridge/            # Bridge pattern
│   │   └── SeatPricingBridge.cs
│   └── Builders/         # Builder pattern
│       └── OrderBuilder.cs
├── Data/
│   ├── Cart/              # ShoppingCart (Singleton)
│   ├── Chain/             # Chain of Responsibility
│   │   └── OrderPipeline.cs
│   ├── Decorators/       # Decorator pattern
│   │   └── PricingDecorators.cs
│   ├── Facade/            # Facade pattern
│   │   └── BookingFacade.cs
│   ├── Mediator/          # Mediator pattern
│   │   └── BookingMediator.cs
│   ├── Observer/          # Observer pattern
│   │   └── OrderObserver.cs
│   ├── Proxy/             # Proxy pattern
│   │   └── CachedMoviesServiceProxy.cs
│   ├── Services/          # 11 service interfaces + implementations
│   ├── State/             # State pattern
│   │   └── OrderStateMachine.cs
│   └── Strategy/          # Strategy pattern
│       └── PaymentStrategy.cs
├── Views/                 # Razor Views (folder-per-controller)
└── wwwroot/              # CSS, images, scripts
```

---

## Bắt đầu

### Yêu cầu

- .NET 8 SDK
- SQL Server (LocalDB hoặc SQL Server Express)
- Visual Studio 2022 hoặc VS Code

### Các bước

**1. Clone và mở project**

```bash
cd movieCinema
dotnet restore
```

**2. Cấu hình chuỗi kết nối**

Sửa `appsettings.json`:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=YOUR_SERVER;Database=MovieCinema;Trusted_Connection=True;TrustServerCertificate=True"
  }
}
```

**3. Chạy**

```bash
dotnet run
```

Mở trình duyệt: `https://localhost:5001`

### Chạy với seed data

Database được seed tự động khi chạy lần đầu (EnsureCreated + seed methods). Không cần migration thủ công.

---

## Tài khoản mặc định

| Vai trò | Email | Mật khẩu |
|---|---|---|
| **Admin** | admin@tickets.com | Coding@1234? |
| **User** | user@tickets.com | Coding@1234? |

Sau khi đăng nhập Admin, có quyền truy cập Dashboard, quản lý tất cả entities, báo cáo doanh thu, và quản lý đơn hàng.

---

## Ma trận Design Patterns

| Vấn đề | Pattern | File |
|---|---|---|
| Đặt vé nhiều bước | **Facade** | `Data/Facade/BookingFacade.cs` |
| Tạo Order nhiều tham số | **Builder** | `Models/Builders/OrderBuilder.cs` |
| Tính giá theo loại ghế | **Bridge** | `Models/Bridge/SeatPricingBridge.cs` |
| Thanh toán Cash/PayPal | **Strategy** | `Data/Strategy/PaymentStrategy.cs` |
| Trạng thái Order phức tạp | **State** | `Data/State/OrderStateMachine.cs` |
| Thông báo khi status đổi | **Observer** | `Data/Observer/OrderObserver.cs` |
| Xếp chồng voucher/points | **Decorator** | `Data/Decorators/PricingDecorators.cs` |
| Cache danh sách phim | **Proxy** | `Data/Proxy/CachedMoviesServiceProxy.cs` |
| Validate đơn hàng pipeline | **Chain of Resp.** | `Data/Chain/OrderPipeline.cs` |
| Điều phối booking handlers | **Mediator** | `Data/Mediator/BookingMediator.cs` |
| Mỗi session một cart | **Singleton** | `Data/Cart/ShoppingCart.cs` |
| Tích hợp PayPal API | **Adapter** | `Data/Strategy/PaymentStrategy.cs` |

---

*Nguồn Design Patterns: "Design Patterns: Elements of Reusable Object-Oriented Software" — Erich Gamma, Richard Helm, Ralph Johnson, John Vlissides (Gang of Four)*
