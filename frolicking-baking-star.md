# Kế hoạch: Áp dụng Design Patterns — Phase 1 + 2

## Context

OrdersController ~800 dòng, logic nghiệp vụ đặt vé + thanh toán + trạng thái đơn hàng nằm gói ghém trong Controller. Cần tái cấu trúc bằng Facade + Builder + Strategy + State.

**Người dùng chọn:** Phase 1 + 2

---

## PHASE 1: Facade + Builder + Bridge

### 1.1. BookingFacade
**File:** `Data/Facade/BookingFacade.cs` (mới)

Gói toàn bộ luồng `BookTickets` POST:
1. Validate ModelState
2. Lấy Showtime + kiểm tra tồn tại
3. Parse ghế đã chọn
4. Check ghế đã bị đặt chưa
5. Tính giá theo loại ghế (Standard/VIP/Couple)
6. Áp dụng voucher
7. Tạo Order
8. Trả về `BookingResult`

Interface `IBookingFacade` để inject. `BookingResult` DTO.

### 1.2. SeatPricingBridge
**File:** `Models/Bridge/SeatPricingBridge.cs` (mới)

Tách logic tính giá theo loại ghế:
- `ISeatingPricingStrategy` interface
- `StandardPricingStrategy` → basePrice
- `VipPricingStrategy` → basePrice × 1.2
- `CouplePricingStrategy` → basePrice × 2.0
- `DisabledPricingStrategy` → basePrice × 0.5

`SeatPricingBridge` class chọn strategy theo `SeatType` enum.

### 1.3. OrderBuilder
**File:** `Models/Builders/OrderBuilder.cs` (mới)

Builder cho Order entity — chain:
```
new OrderBuilder()
  .SetCustomer(name, email, userId)
  .SetShowtime(showtimeId, seats, count, basePrice)
  .ApplyVoucher(voucher, currentTotal)
  .RedeemPoints(points, total)
  .SetPaymentMethod(method)
  .CalculateTotal()
  .Build();
```

### 1.4. OrdersController refactor
**File:** `Controllers/OrdersController.cs`

- Inject `IBookingFacade`, `ISeatingPricingStrategy`
- `BookTickets` POST: rút gọn → gọi `facade.ProcessBooking()` (~15 dòng)
- Giữ nguyên các action khác: GET, Index, ShoppingCart, Dashboard, Revenue, Reports

### 1.5. Program.cs
Đăng ký:
```csharp
builder.Services.AddScoped<ISeatingPricingStrategy, StandardPricingStrategy>();
builder.Services.AddScoped<IBookingFacade, BookingFacade>();
```

---

## PHASE 2: Strategy + State

### 2.1. Payment Strategy
**File:** `Data/Strategy/PaymentStrategy.cs` (mới)

- `IPaymentStrategy` interface
- `CashPaymentStrategy` → success ngay, transactionId = `CASH-{orderId}`
- `PayPalPaymentStrategy` → gọi PayPal API (stub nếu chưa có SDK)
- `PaymentContext` → `SetStrategyByName(name)` chọn đúng strategy

Tích hợp vào `BookingFacade`.

### 2.2. Order State
**File:** `Data/State/OrderStateMachine.cs` (mới)

- `IOrderState` interface: `StatusName`, `CanConfirm()`, `CanCancel()`, `CanRefund()`, `OnEnterAsync()`
- 4 state: `PurchasedState`, `ConfirmedState`, `CancelledState`, `RefundedState`
- `OrderStateMachine` với ma trận transition hợp lệ:
  - Purchased → Confirmed | Cancelled
  - Confirmed → Cancelled | Refunded
  - Cancelled → (terminal)
  - Refunded → (terminal)

### 2.3. Cập nhật OrdersService
**Files:**
- `Data/Services/IOrdersService.cs` — thêm `ChangeStatusWithStateAsync`
- `Data/Services/OrdersService.cs` — implement dùng `OrderStateMachine`

---

## Files mới tạo

| File | Pattern | Nội dung |
|---|---|---|
| `Data/Facade/BookingFacade.cs` | Facade | Luồng đặt vé |
| `Models/Bridge/SeatPricingBridge.cs` | Bridge | Tính giá ghế |
| `Models/Builders/OrderBuilder.cs` | Builder | Tạo Order chain |
| `Data/Strategy/PaymentStrategy.cs` | Strategy | Thanh toán đa phương thức |
| `Data/State/OrderStateMachine.cs` | State | Quản lý trạng thái Order |

## Files sửa

| File | Thay đổi |
|---|---|
| `Controllers/OrdersController.cs` | Inject Facade, rút gọn BookTickets POST |
| `Data/Services/IOrdersService.cs` | Thêm method State |
| `Data/Services/OrdersService.cs` | Implement State pattern |
| `Program.cs` | Đăng ký services mới |

## Verification

1. `dotnet build` trong thư mục project
2. `dotnet run` → truy cập `https://localhost:5001`
3. Đặt vé thử → Facade xử lý
4. Thanh toán Cash → CashStrategy
5. Đổi trạng thái Order (Admin) → State machine validate transition
