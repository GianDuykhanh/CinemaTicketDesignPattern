# BÁO CÁO ÁP DỤNG DESIGN PATTERN VÀO DỰ ÁN MOVIECINEMA

---

## 5.1. Áp dụng mẫu Singleton Pattern:

### ShoppingCart.cs

#### Trước khi áp dụng Singleton Pattern:

```csharp
using Microsoft.EntityFrameworkCore;
using movieCinema.Models;
using MovieCinema.Data;

namespace movieCinema.Data.Cart
{
    public class ShoppingCart
    {
        public AppDbContext _context { get; set; }
        public string ShoppingCartId { get; set; }
        public List<ShoppingCartItem> ShoppingCartItems { get; set; }

        // Constructor public — ai cũng có thể new tuỳ thích
        public ShoppingCart(AppDbContext context)
        {
            _context = context;
            ShoppingCartId = Guid.NewGuid().ToString(); // mỗi lần new → ID mới
        }

        public void AddItemToCart(Showtime showtime)
        {
            var shoppingCartItem = _context.ShoppingCartItems
                .Include(n => n.Showtime)
                .FirstOrDefault(n => n.ShowtimeId == showtime.Id
                                  && n.ShoppingCartId == ShoppingCartId);

            if (shoppingCartItem == null)
            {
                shoppingCartItem = new ShoppingCartItem()
                {
                    ShoppingCartId = ShoppingCartId,
                    Showtime = showtime,
                    Amount = 1
                };
                _context.ShoppingCartItems.Add(shoppingCartItem);
            }
            else
            {
                shoppingCartItem.Amount++;
            }
            _context.SaveChanges();
        }

        public void RemoveItemFromCart(Showtime showtime)
        {
            var shoppingCartItem = _context.ShoppingCartItems
                .Include(n => n.Showtime)
                .FirstOrDefault(n => n.ShowtimeId == showtime.Id
                                  && n.ShoppingCartId == ShoppingCartId);

            if (shoppingCartItem != null)
            {
                if (shoppingCartItem.Amount > 1)
                    shoppingCartItem.Amount--;
                else
                    _context.ShoppingCartItems.Remove(shoppingCartItem);
            }
            _context.SaveChanges();
        }

        public List<ShoppingCartItem> GetShoppingCartItems()
        {
            ShoppingCartItems = _context.ShoppingCartItems
                .Where(n => n.ShoppingCartId == ShoppingCartId)
                .Include(n => n.Showtime).ThenInclude(s => s.Movie)
                .Include(n => n.Showtime).ThenInclude(s => s.CinemaRoom)
                .ToList();
            return ShoppingCartItems;
        }

        public double GetShoppingCartTotal()
        {
            return _context.ShoppingCartItems
                .Where(n => n.ShoppingCartId == ShoppingCartId)
                .Include(n => n.Showtime)
                .Select(n => n.Showtime.Price * n.Amount)
                .Sum();
        }

        public async Task ClearShoppingCartAsync()
        {
            var items = await _context.ShoppingCartItems
                .Where(n => n.ShoppingCartId == ShoppingCartId)
                .ToListAsync();
            _context.ShoppingCartItems.RemoveRange(items);
            await _context.SaveChangesAsync();
        }
    }
}

// ---- Controller sử dụng trực tiếp (ví dụ minh hoạ) ----
public class OrdersController : Controller
{
    private readonly AppDbContext _context;

    public IActionResult ShoppingCart()
    {
        // Lỗi: mỗi lần gọi → CartId mới → giỏ hàng luôn trống!
        var cart = new ShoppingCart(_context);
        var items = cart.GetShoppingCartItems();
        return View(items);
    }

    public IActionResult AddItemToShoppingCart(int id)
    {
        var cart = new ShoppingCart(_context); // CartId KHÁC với trên!
        var item = _showtimesService.GetByIdAsync(id).Result;
        cart.AddItemToCart(item);
        return RedirectToAction(nameof(ShoppingCart));
    }
}
```

#### Biện luận (Giải thích)

**Bước 1: Tạo lớp và phương thức**
Hệ thống xây dựng lớp `ShoppingCart` với Constructor ở chế độ `public`, chứa các thuộc tính `_context`, `ShoppingCartId` và danh sách `ShoppingCartItems`. Constructor nhận vào `AppDbContext` và tự động tạo `CartId` bằng `Guid.NewGuid()`. Các phương thức nghiệp vụ (`AddItemToCart`, `RemoveItemFromCart`, `GetShoppingCartItems`, `GetShoppingCartTotal`, `ClearShoppingCartAsync`) được khai báo trực tiếp trong lớp để xử lý thao tác giỏ hàng.

**Bước 2: Kết nối dữ liệu và xử lý logic**
Mỗi khi một phương thức trong Controller được gọi, hệ thống lại thực hiện lệnh `new ShoppingCart(context)`. Lúc này, một `CartId` hoàn toàn mới (GUID mới) sẽ được sinh ra. Nếu người dùng thêm sản phẩm ở Action `AddItemToShoppingCart` nhưng chuyển sang Action `ShoppingCart`, hệ thống sẽ không tìm thấy sản phẩm nào vì `CartId` bị thay đổi. Việc truyền tham số `AppDbContext` phải lặp lại ở nhiều nơi trong mã nguồn.

**Bước 3: Kiểm tra điều kiện và xử lý nghiệp vụ**
Code thực hiện kiểm tra `FirstOrDefault` để xác định sản phẩm đã tồn tại trong giỏ chưa. Tuy nhiên, do mỗi lần khởi tạo lại có một `CartId` mới, việc kiểm tra này gần như vô nghĩa — sản phẩm sẽ luôn được thêm mới thay vì cập nhật số lượng. Nếu nhiều request cùng lúc thêm sản phẩm, mỗi request sẽ tạo một ShoppingCart riêng biệt, dẫn đến dữ liệu giỏ hàng bị phân tán và mất mát.

**Bước 4: Áp dụng Design Pattern**
Trong phiên bản chưa cải tiến này, hệ thống hoàn toàn thiếu vắng Singleton Pattern. Việc `new` đối tượng `ShoppingCart` liên tục dẫn đến tình trạng mỗi request có một giỏ hàng riêng, không thể chia sẻ trạng thái giữa các request của cùng một người dùng. Sự phụ thuộc cứng (Hard-dependency) giữa logic khởi tạo và logic nghiệp vụ làm cho hệ thống trở nên cồng kềnh và không thể duy trì trạng thái giỏ hàng xuyên suốt phiên làm việc.

**Bước 5: Trả kết quả cho View hoặc client**
Sau khi thao tác giỏ hàng xong, kết quả hiển thị trên View gần như luôn trống vì `CartId` mới không chứa dữ liệu nào. Mặc dù luồng MVC (Controller → Service → View) vẫn được duy trì, nhưng trải nghiệm người dùng bị phá vỡ hoàn toàn do giỏ hàng không thể duy trì trạng thái ổn định. Quy trình vận hành lúc này là: Controller → new ShoppingCart (CartId mới) → Truy vấn rỗng → View trống.

---

#### Sau khi dùng mẫu Singleton Pattern:

```csharp
using Microsoft.EntityFrameworkCore;
using movieCinema.Models;
using MovieCinema.Data;

namespace movieCinema.Data.Cart
{
    public class ShoppingCart
    {
        public AppDbContext _context { get; set; }
        public string ShoppingCartId { get; set; }
        public List<ShoppingCartItem> ShoppingCartItems { get; set; }

        // Constructor private — ngăn khởi tạo tuỳ tiện từ bên ngoài
        private ShoppingCart(AppDbContext context)
        {
            _context = context;
        }

        // Singleton: phương thức tĩnh duy nhất để lấy ShoppingCart
        public static ShoppingCart GetShoppingCart(IServiceProvider services)
        {
            ISession session = services.GetRequiredService<IHttpContextAccessor>()
                                         ?.HttpContext.Session;
            var context = services.GetService<AppDbContext>();

            // Lấy CartId từ Session — duy trì trạng thái xuyên suốt phiên
            string cartId = session.GetString("CartId")
                            ?? Guid.NewGuid().ToString();
            session.SetString("CartId", cartId);

            return new ShoppingCart(context) { ShoppingCartId = cartId };
        }

        public void AddItemToCart(Showtime showtime)
        {
            var shoppingCartItem = _context.ShoppingCartItems
                .Include(n => n.Showtime)
                .FirstOrDefault(n => n.ShowtimeId == showtime.Id
                                  && n.ShoppingCartId == ShoppingCartId);

            if (shoppingCartItem == null)
            {
                shoppingCartItem = new ShoppingCartItem()
                {
                    ShoppingCartId = ShoppingCartId,
                    Showtime = showtime,
                    Amount = 1
                };
                _context.ShoppingCartItems.Add(shoppingCartItem);
            }
            else
            {
                shoppingCartItem.Amount++;
            }
            _context.SaveChanges();
        }

        public void RemoveItemFromCart(Showtime showtime)
        {
            var shoppingCartItem = _context.ShoppingCartItems
                .Include(n => n.Showtime)
                .FirstOrDefault(n => n.ShowtimeId == showtime.Id
                                  && n.ShoppingCartId == ShoppingCartId);

            if (shoppingCartItem != null)
            {
                if (shoppingCartItem.Amount > 1)
                    shoppingCartItem.Amount--;
                else
                    _context.ShoppingCartItems.Remove(shoppingCartItem);
            }
            _context.SaveChanges();
        }

        public List<ShoppingCartItem> GetShoppingCartItems()
        {
            ShoppingCartItems = _context.ShoppingCartItems
                .Where(n => n.ShoppingCartId == ShoppingCartId)
                .Include(n => n.Showtime).ThenInclude(s => s.Movie)
                .Include(n => n.Showtime).ThenInclude(s => s.CinemaRoom)
                .ToList();
            return ShoppingCartItems;
        }

        public double GetShoppingCartTotal()
        {
            return _context.ShoppingCartItems
                .Where(n => n.ShoppingCartId == ShoppingCartId)
                .Include(n => n.Showtime)
                .Select(n => n.Showtime.Price * n.Amount)
                .Sum();
        }

        public async Task ClearShoppingCartAsync()
        {
            var items = await _context.ShoppingCartItems
                .Where(n => n.ShoppingCartId == ShoppingCartId)
                .ToListAsync();
            _context.ShoppingCartItems.RemoveRange(items);
            await _context.SaveChangesAsync();
        }
    }
}

// ---- Program.cs — Đăng ký Singleton qua DI Container: ----
builder.Services.AddSingleton<IHttpContextAccessor, HttpContextAccessor>();
builder.Services.AddScoped(sc => ShoppingCart.GetShoppingCart(sc));
```

#### Biện luận (Giải thích)

**Bước 1: Tạo lớp và phương thức**
Hệ thống xây dựng lớp `ShoppingCart` trong namespace `Data.Cart`. Constructor được chuyển sang chế độ `private` để ngăn việc khởi tạo tùy tiện từ bên ngoài. Thay vào đó, phương thức tĩnh `GetShoppingCart(IServiceProvider)` đóng vai trò là "cánh cửa duy nhất" để truy cập ShoppingCart. Phương thức này sử dụng `IServiceProvider` để lấy `IHttpContextAccessor` và `AppDbContext` từ DI Container, giúp ShoppingCart tự quản lý quá trình khởi tạo của chính nó.

**Bước 2: Kết nối dữ liệu và xử lý logic**
Thay vì tạo mới ShoppingCart mỗi lần có yêu cầu, hệ thống sử dụng `Session` để lưu trữ `CartId` xuyên suốt phiên làm việc của người dùng. Khi `GetShoppingCart()` được gọi lần đầu, một `CartId` mới (GUID) sẽ được sinh ra và lưu vào Session. Các lần gọi tiếp theo sẽ lấy lại cùng một `CartId`, đảm bảo giỏ hàng luôn được duy trì trạng thái nhất quán. Việc khởi tạo chỉ xảy ra một lần thông qua DI Container (`AddScoped`), tiết kiệm tài nguyên hệ thống.

**Bước 3: Kiểm tra điều kiện và xử lý nghiệp vụ**
Hệ thống thực hiện kiểm tra trạng thái Session: nếu `CartId` chưa tồn tại (lần đầu truy cập), một GUID mới sẽ được tạo và lưu vào Session; nếu đã có, giá trị cũ sẽ được giữ nguyên. Nhờ cơ chế `AddScoped` trong DI Container, mỗi request HTTP sẽ nhận được chính xác một ShoppingCart Instance với cùng một `CartId`, đảm bảo nghiệp vụ giỏ hàng luôn chính xác trong suốt phiên làm việc.

**Bước 4: Áp dụng Design Pattern**
Tại đây, Singleton Pattern kết hợp với Session-based Factory đóng vai trò then chốt. Mẫu thiết kế này giúp tách biệt hoàn toàn logic quản lý phiên (Session Management) khỏi logic nghiệp vụ giỏ hàng. Việc duy trì một thực thể ShoppingCart duy nhất cho mỗi phiên người dùng giúp đảm bảo tính nhất quán dữ liệu, giảm thiểu lãng phí tài nguyên và giúp mã nguồn dễ bảo trì, mở rộng mà không sợ bị mất trạng thái giỏ hàng.

**Bước 5: Trả kết quả cho View hoặc client**
Sau khi thao tác giỏ hàng thành công thông qua thực thể Singleton, kết quả sẽ được xử lý và trả về cho Controller. Controller sau đó đóng gói dữ liệu giỏ hàng (danh sách sản phẩm, tổng tiền) và hiển thị lên View cho người dùng. Quy trình này đảm bảo tính nhất quán và hiệu năng cao cho hệ thống: Controller → ShoppingCart (Singleton via Session) → EF Core → SQL Server → View.

---

## 5.2. Áp dụng mẫu Builder Pattern:

### OrderBuilder.cs

#### Trước khi dùng Builder Pattern:

```csharp
using movieCinema.Models;

namespace movieCinema.Models.Builders
{
    // Lớp Order ban đầu — không có Builder
    public class Order
    {
        public int Id { get; set; }
        public string Email { get; set; }
        public string UserId { get; set; }
        public List<OrderItem> OrderItems { get; set; }
        public DateTime OrderDate { get; set; }
        public string Status { get; set; }
        public string PaymentMethod { get; set; }
        public double TotalPrice { get; set; }
        public double DiscountAmount { get; set; }
        public int PointsRedeemed { get; set; }
    }

    // Service tạo Order thủ công — dễ bị sai sót
    public class BookingService
    {
        public Order CreateOrder(
            string name, string email, string userId,
            int showtimeId, string selectedSeats, int seatCount,
            double basePrice, double discountAmount,
            int pointsRedeemed, string paymentMethod)
        {
            // Phải gán từng thuộc tính thủ công — rất dễ thiếu hoặc sai thứ tự
            var order = new Order
            {
                Email = email,
                UserId = name,
                OrderDate = DateTime.Now,
                Status = "Purchased",
                PaymentMethod = paymentMethod,
                TotalPrice = basePrice * seatCount
                           - discountAmount
                           - (pointsRedeemed * 1000.0),
                DiscountAmount = discountAmount,
                PointsRedeemed = (int)Math.Min(pointsRedeemed * 1000.0,
                                                basePrice * seatCount),
                OrderItems = new List<OrderItem>
                {
                    new OrderItem
                    {
                        ShowtimeId = showtimeId,
                        SelectedSeats = selectedSeats,
                        Amount = seatCount,
                        Price = basePrice
                    }
                }
            };

            if (order.TotalPrice < 0)
                order.TotalPrice = 0;

            return order;
        }
    }
}
```

#### Biện luận (Giải thích)

**Bước 1: Tạo lớp và phương thức**
Hệ thống xây dựng lớp `Order` đóng vai trò là một POCO (Plain Old CLR Object) đơn thuần với các thuộc tính như `Email`, `UserId`, `OrderItems`, `TotalPrice`, `DiscountAmount`, `PointsRedeemed` và `PaymentMethod`. Lớp `BookingService` khai báo phương thức `CreateOrder` với hàng loạt tham số đầu vào (10 tham số) để tiếp nhận yêu cầu đặt vé từ phía Controller.

**Bước 2: Kết nối dữ liệu và xử lý logic**
Phương thức trong Service trực tiếp khởi tạo đối tượng `Order` bằng từ khóa `new` và sử dụng cú pháp gán giá trị (Object Initializer). Tại đây, logic khởi tạo đối tượng bị trộn lẫn hoàn toàn với logic tính toán tổng tiền (subtotal, discount, points). Nếu một yêu cầu đặt vé phức tạp (nhiều voucher, nhiều loại ghế), mã nguồn sẽ trở nên cồng kềnh vì phải lặp lại việc gán giá trị thủ công cho từng thuộc tính.

**Bước 3: Kiểm tra điều kiện và xử lý nghiệp vụ**
Code thực hiện kiểm tra `order.TotalPrice < 0` ở cuối phương thức. Tuy nhiên, do chưa có bộ điều khiển khởi tạo tập trung, lập trình viên phải viết hàng loạt phép tính toán giá (basePrice * seatCount - discountAmount - pointsRedeemed * 1000) ngay trong Object Initializer. Việc thiếu một quy trình xây dựng từng bước khiến dữ liệu đầu vào dễ bị sai lệch hoặc thiếu sót, đặc biệt khi tổng tiền âm chưa được xử lý kịp thời.

**Bước 4: Áp dụng Design Pattern**
Trong phiên bản chưa cải tiến này, hệ thống hoàn toàn thiếu vắng Builder Pattern. Quy trình tạo lập đối tượng Order diễn ra một cách thô sơ và thiếu tính linh hoạt. Lớp Order buộc phải để các thuộc tính ở chế độ `public set`, làm lộ cấu trúc nội bộ và vi phạm nguyên tắc đóng gói (Encapsulation). Khi hệ thống cần thêm các bước mới (ví dụ: áp dụng điểm thành viên, kiểm tra khuyến mãi), việc chỉnh sửa mã nguồn sẽ trở nên cực kỳ khó khăn do logic khởi tạo nằm trong một hàm duy nhất quá dài.

**Bước 5: Trả kết quả cho View hoặc client**
Sau khi đối tượng Order được gán giá trị thủ công, Service trả về kết quả cho Controller. Controller tiếp tục chuyển dữ liệu này về phía View hoặc lưu xuống database. Mặc dù luồng MVC (Controller → Service → Database → View) vẫn được duy trì, nhưng hệ thống gặp hạn chế lớn về khả năng mở rộng và tái sử dụng mã nguồn do quá trình khởi tạo đối tượng không được chuẩn hóa.

---

#### Sau khi dùng Builder Pattern:

```csharp
using movieCinema.Models;

namespace movieCinema.Models.Builders
{
    // ── Interface: Bản thiết kế chuẩn ──────────────────────────────────
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

    // ── Builder: "Người thợ xây" ──────────────────────────────────────
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
            _finalTotal = _subtotal - _order.DiscountAmount
                         - _order.PointsRedeemed;
            if (_finalTotal < 0) _finalTotal = 0;
            _order.TotalPrice = _finalTotal;
            return this;
        }

        public Order Build() => _order;
    }
}

// ── Gọi từ BookingFacade (Sau khi áp dụng) ──────────────────────────
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

#### Biện luận (Giải thích)

**Bước 1: Tạo lớp và phương thức**
Hệ thống xây dựng bộ đôi lớp gồm `OrderBuilder` (đối tượng chứa logic khởi tạo) và giao diện `IOrderBuilder` đóng vai trò là "bản thiết kế chuẩn". Lớp Builder cung cấp các phương thức như `SetCustomer()`, `SetShowtime()`, `ApplyVoucher()`, `RedeemPoints()`, `SetPaymentMethod()`, `CalculateTotal()` thay vì để các Service can thiệp trực tiếp vào thuộc tính. Các phương thức này trả về chính đối tượng Builder (`return this`), cho phép viết code theo phong cách Fluent Interface cực kỳ gọn gàng.

**Bước 2: Kết nối dữ liệu và xử lý logic**
Trong `BookingFacade`, thay vì dùng từ khóa `new Order` kèm 10 tham số, hệ thống sử dụng Builder để lắp ráp Order theo từng bước rõ ràng. Tùy vào yêu cầu từ Client, hệ thống sẽ gọi các phương thức thiết lập tương ứng. Logic kết nối Database giờ đây chỉ tiếp nhận một đối tượng Order đã được "xây dựng" hoàn chỉnh, giúp tách biệt hoàn toàn quá trình cấu hình đơn hàng và quá trình lưu trữ dữ liệu.

**Bước 3: Kiểm tra điều kiện và xử lý nghiệp vụ**
Hệ thống thực hiện kiểm tra tính hợp lệ của dữ liệu ngay trong các bước xây dựng của Builder. Ví dụ: `ApplyVoucher` tự động giới hạn discount không vượt quá tổng tiền (`Math.Min`), `RedeemPoints` tự động giới hạn điểm sử dụng không vượt quá tổng tiền trước khi giảm. Phương thức `CalculateTotal()` sẽ kiểm tra tổng thể lần cuối (đảm bảo tổng không âm) để đảm bảo đối tượng Order luôn ở trạng thái hợp lệ nhất.

**Bước 4: Áp dụng Design Pattern**
Tại bước này, Builder Pattern phát huy tối đa sức mạnh trong việc đóng gói (Encapsulation) logic khởi tạo phức tạp. Nó giúp giảm sự phụ thuộc giữa `BookingFacade` và `OrderBuilder`. Nếu trong tương lai bạn muốn thêm bước mới như "Áp dụng mã khuyến mãi cho sinh viên" hay "Tính phí dịch vụ", bạn chỉ cần bổ sung thêm phương thức vào Builder mà không làm ảnh hưởng đến các logic nghiệp vụ đã viết trước đó.

**Bước 5: Trả kết quả cho View hoặc client**
Sau khi lệnh `Build()` hoàn tất, đối tượng Order sẽ được chuyển đến tầng Repository để thực hiện lưu trữ vào SQL Server. Kết quả trả về cho Controller và hiển thị lên Client luôn đảm bảo tính chính xác và nhất quán. Luồng xử lý lúc này đạt chuẩn kiến trúc sạch: Controller → BookingFacade → OrderBuilder (Xây dựng) → Build() → OrdersService → SQL Server → View.

---

## 5.3. Áp dụng mẫu Strategy Pattern:

### PaymentStrategy.cs

#### Trước khi dùng Strategy Pattern:

```csharp
using movieCinema.Models;

namespace movieCinema.Data.Services
{
    public class BookingService
    {
        public async Task<PaymentResult> PayAsync(
            string? paymentMethod, double amount, string orderId)
        {
            // Toàn bộ thuật toán thanh toán bị dồn vào một phương thức
            if (paymentMethod?.ToLower() == "paypal")
            {
                // Logic PayPal bị trộn với logic đặt vé
                var clientId = "CLIENT_ID";
                var clientSecret = "CLIENT_SECRET";
                await Task.Delay(100); // giả lập gọi PayPal API

                return new PaymentResult
                {
                    Success = true,
                    TransactionId = $"PP-{orderId}-{DateTime.Now.Ticks}",
                    Message = "Thanh toán PayPal thành công."
                };
            }

            // Nếu không phải PayPal thì mặc định thanh toán tại rạp
            return new PaymentResult
            {
                Success = true,
                TransactionId = $"CASH-{orderId}-{DateTime.Now.Ticks}",
                Message = "Thanh toán tại rạp - vui lòng thanh toán khi nhận vé."
            };
        }

        public async Task<RefundResult> RefundAsync(
            string? paymentMethod, string transactionId, double amount)
        {
            if (paymentMethod?.ToLower() == "paypal")
            {
                await Task.Delay(100);
                return new RefundResult
                {
                    Success = true,
                    RefundId = $"REF-{transactionId}",
                    Message = "Hoàn tiền PayPal thành công."
                };
            }

            return new RefundResult
            {
                Success = true,
                RefundId = $"REF-{transactionId}",
                Message = "Hoàn tiền thành công."
            };
        }
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
}
```

#### Biện luận (Giải thích)

**Bước 1: Tạo lớp và phương thức**
Hệ thống xây dựng `BookingService` với phương thức `PayAsync` và `RefundAsync`. Hai phương thức này vừa tiếp nhận thông tin đơn hàng, vừa quyết định phương thức thanh toán, vừa thực thi thuật toán cụ thể. Các nhánh `if` kiểm tra chuỗi `paypal` hoặc mặc định `cash` nằm trực tiếp trong Service.

**Bước 2: Kết nối dữ liệu và xử lý logic**
Khi người dùng đặt vé, Service kiểm tra `paymentMethod` rồi thực hiện trực tiếp logic PayPal hoặc Cash. Việc lựa chọn thuật toán và thực thi thuật toán xảy ra ở cùng một nơi. Nếu thêm phương thức mới như VNPay, MoMo hoặc thẻ ngân hàng, phương thức `PayAsync` sẽ phải tiếp tục mở rộng bằng nhiều nhánh `if-else`.

**Bước 3: Kiểm tra điều kiện và xử lý nghiệp vụ**
Code phải chuẩn hóa chuỗi bằng `ToLower()` và tự xử lý giá trị mặc định. Các điều kiện kiểm tra bị lặp lại trong cả thanh toán và hoàn tiền. Chỉ cần một nơi xử lý thiếu nhánh hoặc viết sai tên phương thức, kết quả thanh toán sẽ không nhất quán giữa các nghiệp vụ.

**Bước 4: Áp dụng Design Pattern**
Phiên bản này chưa có Strategy Pattern. `BookingService` vi phạm Single Responsibility vì vừa điều phối đơn hàng vừa chứa tất cả thuật toán thanh toán. Nó cũng vi phạm Open/Closed: thêm phương thức thanh toán mới buộc phải sửa trực tiếp lớp đang hoạt động, làm tăng nguy cơ ảnh hưởng chức năng cũ.

**Bước 5: Trả kết quả cho View hoặc client**
Kết quả thanh toán vẫn được trả về cho Facade hoặc Controller dưới dạng `PaymentResult`. Tuy nhiên, luồng xử lý bị phụ thuộc vào một Service lớn: Controller → BookingService → if/else thanh toán → Client. Khả năng kiểm thử riêng từng phương thức thanh toán còn hạn chế.

---

#### Sau khi dùng Strategy Pattern:

```csharp
namespace movieCinema.Data.Strategy
{
    // ── Strategy interface ──────────────────────────────────────────────
    public interface IPaymentStrategy
    {
        string Name { get; }
        string PaymentMethod { get; }
        Task<PaymentResult> PayAsync(double amount, string orderId);
        Task<RefundResult> RefundAsync(string transactionId, double amount);
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

    // ── Concrete Strategy 1: Cash ───────────────────────────────────────
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

    // ── Concrete Strategy 2: PayPal ─────────────────────────────────────
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
            await Task.Delay(100); // giả lập PayPal API
            return new PaymentResult
            {
                Success = true,
                TransactionId = $"PP-{orderId}-{DateTime.Now.Ticks}",
                Message = "Thanh toán PayPal thành công."
            };
        }

        public async Task<RefundResult> RefundAsync(
            string transactionId, double amount)
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

    // ── Context: quản lý Strategy hiện tại ─────────────────────────────
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
                "paypal" => new PayPalPaymentStrategy(
                    "CLIENT_ID", "CLIENT_SECRET"),
                _ => new CashPaymentStrategy()
            };
        }

        public async Task<PaymentResult> PayAsync(
            double amount, string orderId)
        {
            if (_strategy == null)
                throw new InvalidOperationException(
                    "Payment strategy not set.");
            return await _strategy.PayAsync(amount, orderId);
        }

        public async Task<RefundResult> RefundAsync(
            string transactionId, double amount)
        {
            if (_strategy == null)
                throw new InvalidOperationException(
                    "Payment strategy not set.");
            return await _strategy.RefundAsync(transactionId, amount);
        }

        public string CurrentPaymentMethod
            => _strategy?.PaymentMethod ?? "Unknown";
    }
}

// ── Gọi trong BookingFacade ─────────────────────────────────────────
var paymentCtx = new PaymentContext();
paymentCtx.SetStrategyByName(model.PaymentMethod);
var paymentResult = await paymentCtx.PayAsync(
    totalPrice, $"ORDER-{DateTime.Now.Ticks}");
```

#### Biện luận (Giải thích)

**Bước 1: Tạo lớp và phương thức**
Hệ thống xây dựng giao diện `IPaymentStrategy` làm hợp đồng chung cho mọi phương thức thanh toán. Giao diện quy định các thuộc tính `Name`, `PaymentMethod` và hai phương thức `PayAsync`, `RefundAsync`. Sau đó, hệ thống tạo hai lớp cụ thể là `CashPaymentStrategy` và `PayPalPaymentStrategy`.

**Bước 2: Kết nối dữ liệu và xử lý logic**
`PaymentContext` đóng vai trò Context, giữ Strategy đang được sử dụng trong trường `_strategy`. Trong `BookingFacade`, hệ thống gọi `SetStrategyByName()` để chọn thuật toán tại thời điểm chạy. Sau đó, Facade chỉ gọi `PayAsync()` mà không cần biết cách Cash hoặc PayPal tạo mã giao dịch và xử lý hoàn tiền như thế nào.

**Bước 3: Kiểm tra điều kiện và xử lý nghiệp vụ**
`PaymentContext` kiểm tra `_strategy == null` trước khi thanh toán hoặc hoàn tiền. Nếu chưa thiết lập Strategy, hệ thống ném `InvalidOperationException` để tránh xử lý không xác định. Việc chuẩn hóa tên phương thức được gom vào `SetStrategyByName`, giúp giảm lặp logic và đảm bảo phương thức mặc định là Cash.

**Bước 4: Áp dụng Design Pattern**
Strategy Pattern cho phép đóng gói từng thuật toán thanh toán trong một lớp riêng biệt. `PaymentContext` không phụ thuộc vào chi tiết của từng cổng thanh toán. Nếu cần thêm `MoMoPaymentStrategy` hoặc `VnPayPaymentStrategy`, chỉ cần tạo lớp mới thực thi `IPaymentStrategy` và bổ sung cách đăng ký/chọn Strategy, không cần viết lại toàn bộ BookingService.

**Bước 5: Trả kết quả cho View hoặc client**
Sau khi Strategy thực hiện xong, `PaymentResult` được trả về `BookingFacade`. Nếu thanh toán thất bại, Facade kết thúc quy trình và trả thông báo lỗi; nếu thành công, quy trình tạo Order tiếp tục. Luồng xử lý đạt chuẩn: Controller → BookingFacade → PaymentContext → IPaymentStrategy → Payment Gateway → Client.

---

## 5.4. Áp dụng mẫu State Pattern:

### OrderStateMachine.cs

#### Trước khi dùng State Pattern:

```csharp
using Microsoft.EntityFrameworkCore;
using movieCinema.Models;
using MovieCinema.Data;

namespace movieCinema.Data.Services
{
    public class OrdersService
    {
        private readonly AppDbContext _context;

        public OrdersService(AppDbContext context)
        {
            _context = context;
        }

        // Mỗi phương thức kiểm tra trạng thái riêng — dễ bị phân tán
        public async Task ChangeOrderStatusAsync(int orderId, string newStatus)
        {
            var order = await _context.Orders.FindAsync(orderId);
            if (order == null) return;

            string oldStatus = order.Status;

            // Hàng loạt if-else kiểm tra trạng thái — rất dễ sai
            if (oldStatus == "Purchased")
            {
                if (newStatus != "Confirmed" && newStatus != "Cancelled")
                    return; // Không hợp lệ — nhưng không có thông báo rõ ràng
            }
            else if (oldStatus == "Confirmed")
            {
                if (newStatus != "Cancelled" && newStatus != "Refunded")
                    return;
            }
            else if (oldStatus == "Cancelled" || oldStatus == "Refunded")
            {
                return; // Trạng thái cuối — không cho chuyển
            }

            order.Status = newStatus;

            // Xử lý nghiệp vụ khi chuyển trạng thái — code lặp lại nhiều nơi
            if (newStatus == "Cancelled" || newStatus == "Refunded")
            {
                if (!string.IsNullOrEmpty(order.Email))
                {
                    var member = await _context.Members
                        .FirstOrDefaultAsync(m =>
                            m.Email.ToLower() == order.Email.ToLower());
                    if (member != null)
                    {
                        double finalPrice = Math.Max(0,
                            order.TotalPrice - order.DiscountAmount);
                        int earned = (int)(finalPrice / 10000);
                        member.Points = Math.Max(0,
                            member.Points - earned
                            + (order.PointsRedeemed / 1000));
                    }
                }
            }

            await _context.SaveChangesAsync();
        }
    }
}
```

#### Biện luận (Giải thích)

**Bước 1: Tạo lớp và phương thức**
Hệ thống xây dựng phương thức `ChangeOrderStatusAsync` trong `OrdersService`. Phương thức này nhận vào `orderId` và `newStatus`, tự tra cứu trạng thái hiện tại trong database, rồi quyết định có cho phép chuyển hay không. Toàn bộ quy tắc chuyển đổi được viết trực tiếp bằng chuỗi `if-else` trùng lặp.

**Bước 2: Kết nối dữ liệu và xử lý logic**
Mỗi lần gọi phương thức, hệ thống truy vấn database để lấy order, so sánh `oldStatus` với `newStatus`, rồi cập nhật trực tiếp. Logic kiểm tra trạng thái bị trộn lẫn với logic xử lý nghiệp vụ (hoàn điểm cho thành viên). Nếu có Controller khác cũng cần chuyển trạng thái, lập trình viên phải copy-paste toàn bộ đoạn if-else hoặc gọi lại cùng một hàm quá nặng.

**Bước 3: Kiểm tra điều kiện và xử lý nghiệp vụ**
Code thực hiện kiểm tra các cặp trạng thái hợp lệ bằng chuỗi `if-else`. Tuy nhiên, việc thiếu một cấu trúc tập trung khiến dễ bỏ sót nhánh xử lý hoặc ghi sai mã trạng thái. Khi cần thêm trạng thái mới (ví dụ: "In Progress" cho thanh toán trực tuyến), lập trình viên phải mở tất cả các nhánh hiện có để chèn thêm điều kiện.

**Bước 4: Áp dụng Design Pattern**
Phiên bản này chưa áp dụng State Pattern. Quy tắc chuyển đổi bị phân tán và mã trạng thái nằm rải rác dưới dạng chuỗi (string) cứng trong nhiều nơi. Hệ thống rất khó kiểm tra tính đầy đủ của quy tắc, dễ viết sai logic và không thể tái sử dụng quy tắc cho các nghiệp vụ khác như hoàn vé hoặc xác nhận vé.

**Bước 5: Trả kết quả cho View hoặc client**
Kết quả thay đổi trạng thái được phản hồi qua `TempData` trong Controller. Mặc dù luồng MVC vẫn hoạt động, Controller phải kiểm tra thêm kết quả trả về để hiển thị thông báo thành công hoặc lỗi, dẫn đến Controller bị nặng và khó bảo trì.

---

#### Sau khi dùng State Pattern:

```csharp
using movieCinema.Models;
using Microsoft.EntityFrameworkCore;
using MovieCinema.Data;

namespace movieCinema.Data.State
{
    // ── State interface ──────────────────────────────────────────────────
    public interface IOrderState
    {
        string StatusName { get; }
        bool CanTransitionTo(string newStatus);
        Task OnEnterAsync(Order order, AppDbContext context);
    }

    // ── Purchased: trạng thái ban đầu ───────────────────────────────────
    public class PurchasedState : IOrderState
    {
        public string StatusName => "Purchased";

        public bool CanTransitionTo(string newStatus)
            => newStatus is "Confirmed" or "Cancelled";

        public Task OnEnterAsync(Order order, AppDbContext context)
        {
            // Mới đặt — có thể gửi email xác nhận
            return Task.CompletedTask;
        }
    }

    // ── Confirmed: đã xác nhận ──────────────────────────────────────────
    public class ConfirmedState : IOrderState
    {
        public string StatusName => "Confirmed";

        public bool CanTransitionTo(string newStatus)
            => newStatus is "Cancelled" or "Refunded";

        public Task OnEnterAsync(Order order, AppDbContext context)
        {
            // Đã xác nhận — sinh mã QR vé, gửi cho khách
            return Task.CompletedTask;
        }
    }

    // ── Cancelled: trạng thái kết thúc ──────────────────────────────────
    public class CancelledState : IOrderState
    {
        public string StatusName => "Cancelled";

        public bool CanTransitionTo(string newStatus) => false; // terminal

        public async Task OnEnterAsync(Order order, AppDbContext context)
        {
            if (!string.IsNullOrEmpty(order.Email))
            {
                var member = await context.Members
                    .FirstOrDefaultAsync(m =>
                        m.Email.ToLower() == order.Email.ToLower());
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

    // ── Refunded: trạng thái kết thúc ───────────────────────────────────
    public class RefundedState : IOrderState
    {
        public string StatusName => "Refunded";

        public bool CanTransitionTo(string newStatus) => false; // terminal

        public async Task OnEnterAsync(Order order, AppDbContext context)
        {
            // Hoàn tiền + hoàn điểm — logic tương tự Cancelled
            if (!string.IsNullOrEmpty(order.Email))
            {
                var member = await context.Members
                    .FirstOrDefaultAsync(m =>
                        m.Email.ToLower() == order.Email.ToLower());
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

    // ── Machine: registry các trạng thái ────────────────────────────────
    public class OrderStateMachine
    {
        private static readonly Dictionary<string, IOrderState> _states
            = new()
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

        public static IEnumerable<string> GetValidStatuses()
            => _states.Keys;

        public static bool CanTransition(string from, string to)
        {
            if (!_states.TryGetValue(from, out var state))
                return false;
            return state.CanTransitionTo(to);
        }
    }
}

// ── OrdersService sau khi áp dụng State Pattern ─────────────────────
public async Task<StatusChangeResult> ChangeOrderStatusWithStateAsync(
    int orderId, string newStatus)
{
    var order = await _context.Orders
        .Include(o => o.OrderItems)
        .FirstOrDefaultAsync(o => o.Id == orderId);

    if (order == null)
        return new StatusChangeResult
        {
            Success = false,
            Message = "Đơn hàng không tồn tại."
        };

    string oldStatus = order.Status;
    if (oldStatus == newStatus)
        return new StatusChangeResult
        {
            Success = false,
            Message = $"Đơn hàng đã ở trạng thái [{newStatus}].",
            OldStatus = oldStatus,
            NewStatus = newStatus
        };

    // Kiểm tra transition hợp lệ qua State Machine
    if (!OrderStateMachine.CanTransition(oldStatus, newStatus))
        return new StatusChangeResult
        {
            Success = false,
            Message = $"Không thể chuyển từ [{oldStatus}] sang [{newStatus}].",
            OldStatus = oldStatus,
            NewStatus = newStatus
        };

    order.Status = newStatus;

    // Gọi OnEnterAsync của trạng thái mới
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

#### Biện luận (Giải thích)

**Bước 1: Tạo lớp và phương thức**
Hệ thống xây dựng giao diện `IOrderState` với ba phương thức then chốt: `StatusName`, `CanTransitionTo` và `OnEnterAsync`. Mỗi trạng thái cụ thể (`PurchasedState`, `ConfirmedState`, `CancelledState`, `RefundedState`) tự xác định trạng thái nào được phép chuyển tới và hành động cần thực hiện khi bước vào trạng thái đó.

**Bước 2: Kết nối dữ liệu và xử lý logic**
`OrderStateMachine` là một registry tập trung, lưu tất cả trạng thái trong `Dictionary<string, IOrderState>`. `OrdersService` chỉ cần gọi `OrderStateMachine.CanTransition(oldStatus, newStatus)` để kiểm tra quy tắc. Nếu hợp lệ, Service gọi `GetState(newStatus)` rồi thực thi `OnEnterAsync`. Logic nghiệp vụ của từng trạng thái (như hoàn điểm) được đóng gói bên trong chính lớp trạng thái, không còn nằm trong Service.

**Bước 3: Kiểm tra điều kiện và xử lý nghiệp vụ**
Hệ thống kiểm tra tính hợp lệ của transition ngay trong `CanTransitionTo` của mỗi trạng thái. `PurchasedState` chỉ cho phép chuyển sang `Confirmed` hoặc `Cancelled`. `CancelledState` và `RefundedState` là terminal, trả về `false` cho mọi yêu cầu chuyển tiếp. Nếu trạng thái không hợp lệ, `ChangeOrderStatusWithStateAsync` trả về `StatusChangeResult` với thông báo lỗi rõ ràng.

**Bước 4: Áp dụng Design Pattern**
State Pattern giúp đóng gói hành vi của từng trạng thái trong một lớp riêng biệt. Khi cần thêm trạng thái mới (ví dụ: "In Progress"), chỉ cần tạo lớp `InProgressState` và thêm vào dictionary trong `OrderStateMachine`, không cần sửa code trong `OrdersService`. Mỗi trạng thái tự chịu trách nhiệm về logic nghiệp vụ của mình (hoàn điểm, gửi email, v.v.), giúp hệ thống tuân thủ Open/Closed.

**Bước 5: Trả kết quả cho View hoặc client**
Kết quả trả về Controller dưới dạng `StatusChangeResult` chứa thông tin thành công/thất bại, trạng thái cũ và mới. Controller dùng `TempData` hiển thị thông báo cho người dùng. Luồng xử lý đạt chuẩn: Controller → OrdersService → OrderStateMachine.CanTransition → IOrderState.OnEnterAsync → SQL Server → Client.

---

## 5.5. Áp dụng mẫu Facade Pattern:

### BookingFacade.cs

#### Trước khi dùng Facade Pattern:

```csharp
using Microsoft.AspNetCore.Mvc;
using movieCinema.Data.Services;
using movieCinema.Data.ViewModels;
using movieCinema.Models;

namespace movieCinema.Controllers
{
    [Authorize]
    public class OrdersController : Controller
    {
        private readonly IShowtimesService _showtimesService;
        private readonly ISeatsService _seatsService;
        private readonly IOrdersService _ordersService;
        private readonly IMoviesService _moviesService;
        private readonly AppDbContext _context;

        // Constructor injection với 5+ dịch vụ
        public OrdersController(
            IShowtimesService showtimesService,
            ISeatsService seatsService,
            IOrdersService ordersService,
            IMoviesService moviesService,
            AppDbContext context)
        {
            _showtimesService = showtimesService;
            _seatsService = seatsService;
            _ordersService = ordersService;
            _moviesService = moviesService;
            _context = context;
        }

        // POST: BookTickets — Toàn bộ nghiệp vụ đặt vé nằm trong Controller
        [HttpPost]
        public async Task<IActionResult> BookTickets(BookTicketsVM model)
        {
            if (!ModelState.IsValid)
                return RedirectToAction(nameof(BookTickets),
                    new { showtimeId = model.ShowtimeId });

            // 1. Lấy suất chiếu
            var showtime = await _showtimesService
                .GetShowtimeByIdWithDetailsAsync(model.ShowtimeId);
            if (showtime == null)
            {
                TempData["BookingError"] = "Suất chiếu không tồn tại.";
                return RedirectToAction(nameof(BookTickets),
                    new { showtimeId = model.ShowtimeId });
            }

            // 2. Parse ghế
            var selectedSeats = model.SelectedSeats
                .Split(',')
                .Select(s => s.Trim())
                .Where(s => !string.IsNullOrEmpty(s))
                .ToList();
            if (!selectedSeats.Any())
            {
                TempData["BookingError"] = "Vui lòng chọn ít nhất một ghế.";
                return RedirectToAction(nameof(BookTickets),
                    new { showtimeId = model.ShowtimeId });
            }

            // 3. Kiểm tra ghế đã đặt
            var bookedSeats = await _ordersService
                .GetBookedSeatsForShowtimeAsync(model.ShowtimeId);
            foreach (var seat in selectedSeats)
            {
                if (bookedSeats.Contains(seat))
                {
                    TempData["BookingError"] =
                        $"Ghế {seat} đã được đặt.";
                    return RedirectToAction(nameof(BookTickets),
                        new { showtimeId = model.ShowtimeId });
                }
            }

            // 4. Tính giá (code dài — lặp lại logic tính giá)
            double totalPrice = 0;
            var roomSeats = await _seatsService
                .GetSeatsByRoomAsync(showtime.CinemaRoomId);
            foreach (var seatCode in selectedSeats)
            {
                var seat = roomSeats.FirstOrDefault(s =>
                    s.Row + s.Number.ToString() == seatCode);
                double multiplier = seat?.SeatType switch
                {
                    SeatType.VIP     => 1.2,
                    SeatType.Couple   => 2.0,
                    SeatType.Disabled => 0.5,
                    _                 => 1.0
                };
                totalPrice += showtime.Price * multiplier;
            }

            // 5. Áp dụng voucher — code lặp lại logic giảm giá
            double discount = 0;
            if (!string.IsNullOrEmpty(model.VoucherCode))
            {
                var voucher = await _ordersService
                    .GetVoucherByCodeAsync(model.VoucherCode);
                if (voucher != null
                    && totalPrice >= voucher.MinOrderAmount)
                {
                    discount = voucher.IsPercentage
                        ? totalPrice * voucher.DiscountPercentage / 100.0
                        : voucher.DiscountAmount;
                }
            }

            // 6. Lưu đơn hàng
            await _ordersService.StoreDirectOrderAsync(
                model.ShowtimeId,
                model.Name ?? "Guest",
                model.Email ?? "",
                model.SelectedSeats,
                selectedSeats.Count,
                totalPrice,
                discount,
                model.PointsRedeemed,
                "Cash");

            // 7. Lưu cookie, hiển thị kết quả
            if (!string.IsNullOrEmpty(model.Email))
                Response.Cookies.Append("CustomerEmail",
                    model.Email.Trim(),
                    new CookieOptions
                    { Expires = DateTimeOffset.UtcNow.AddDays(30) });

            ViewBag.MovieName = showtime?.Movie?.Name;
            ViewBag.TotalPrice = totalPrice - discount;
            return View("BookingCompleted");
        }
    }
}
```

#### Biện luận (Giải thích)

**Bước 1: Tạo lớp và phương thức**
Hệ thống xây dựng Controller `OrdersController` với hơn 5依赖注入 (Dependency Injection). Phương thức `BookTickets` POST chứa toàn bộ quy trình đặt vé: xác thực ModelState, lấy suất chiếu, parse danh sách ghế, kiểm tra ghế trống, tính giá theo loại ghế, áp dụng voucher giảm giá, lưu đơn hàng và hiển thị kết quả.

**Bước 2: Kết nối dữ liệu và xử lý logic**
Mỗi request đặt vé phải gọi lần lượt `ShowtimesService`, `OrdersService`, `SeatsService`, rồi thực hiện hàng loạt phép tính giá với `switch-case` thủ công. Việc kết nối dữ liệu bị phân tán giữa nhiều Service và logic tính toán nằm trực tiếp trong Controller, khiến Controller trở nên cực kỳ cồng kềnh.

**Bước 3: Kiểm tra điều kiện và xử lý nghiệp vụ**
Code thực hiện kiểm tra ModelState, kiểm tra ghế trống, kiểm tra voucher hợp lệ... Tuy nhiên, mỗi lần kiểm tra đều trả về `RedirectToAction` với thông báo lỗi riêng, dẫn đến Controller có hàng chục dòng `if` kiểm tra và hàng chục dòng `TempData["BookingError"]`.

**Bước 4: Áp dụng Design Pattern**
Phiên bản này chưa có Facade Pattern. Controller vừa là "người điều phối" vừa là "người thực thi". Nó phụ thuộc trực tiếp vào hơn 4 Service và chứa toàn bộ logic phức tạp. Khi cần thay đổi quy trình đặt vé (ví dụ: thêm bước thanh toán trực tuyến), lập trình viên phải sửa Controller — nơi đang chứa cả logic hiển thị giao diện.

**Bước 5: Trả kết quả cho View hoặc client**
Sau khi lưu đơn hàng, Controller gán ViewBag và trả về View "BookingCompleted". Tuy nhiên, do quá nhiều logic nghiệp vụ nằm trong Controller, việc kiểm thử (unit test) trở nên rất khó khăn vì phải mock cả 4-5 Service cùng lúc.

---

#### Sau khi dùng Facade Pattern:

```csharp
using Microsoft.EntityFrameworkCore;
using movieCinema.Data.Services;
using movieCinema.Data.ViewModels;
using movieCinema.Models;
using movieCinema.Models.Bridge;
using movieCinema.Models.Builders;
using movieCinema.Data.Strategy;
using MovieCinema.Data;

namespace movieCinema.Data.Facade
{
    public interface IBookingFacade
    {
        Task<BookingResult> ProcessBookingAsync(
            BookTicketsVM model, string? userId);
    }

    public class BookingResult
    {
        public bool Success { get; set; }
        public string Message { get; set; } = "";
        public int? OrderId { get; set; }
        public double FinalPrice { get; set; }
        public double DiscountApplied { get; set; }
        public double PointsEarned { get; set; }
    }

    public class BookingFacade : IBookingFacade
    {
        private readonly AppDbContext _context;
        private readonly IShowtimesService _showtimesService;
        private readonly ISeatsService _seatsService;
        private readonly IOrdersService _ordersService;

        public BookingFacade(
            AppDbContext context,
            IShowtimesService showtimesService,
            ISeatsService seatsService,
            IOrdersService ordersService)
        {
            _context = context;
            _showtimesService = showtimesService;
            _seatsService = seatsService;
            _ordersService = ordersService;
        }

        public async Task<BookingResult> ProcessBookingAsync(
            BookTicketsVM model, string? userId)
        {
            // 1. Validate
            if (string.IsNullOrEmpty(model.SelectedSeats))
                return new BookingResult
                {
                    Success = false,
                    Message = "Vui lòng chọn ít nhất một ghế."
                };

            // 2. Lấy Showtime
            var showtime = await _showtimesService
                .GetShowtimeByIdWithDetailsAsync(model.ShowtimeId);
            if (showtime == null)
                return new BookingResult
                {
                    Success = false,
                    Message = "Suất chiếu không tồn tại."
                };

            // 3. Parse ghế
            var selectedSeats = model.SelectedSeats
                .Split(',')
                .Select(s => s.Trim())
                .Where(s => !string.IsNullOrEmpty(s))
                .ToList();

            // 4. Check ghế đã bị đặt
            var bookedSeats = await _ordersService
                .GetBookedSeatsForShowtimeAsync(model.ShowtimeId);
            foreach (var seat in selectedSeats)
                if (bookedSeats.Contains(seat))
                    return new BookingResult
                    {
                        Success = false,
                        Message = $"Ghế {seat} đã được đặt."
                    };

            // 5. Tính giá theo loại ghế (Bridge)
            var roomSeats = await _seatsService
                .GetSeatsByRoomAsync(showtime.CinemaRoomId);
            double totalPrice = 0;
            foreach (var seatCode in selectedSeats)
            {
                var seat = roomSeats.FirstOrDefault(s =>
                    s.Row + s.Number.ToString() == seatCode);
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
                if (voucher != null
                    && totalPrice >= voucher.MinOrderAmount)
                    discount = voucher.IsPercentage
                        ? totalPrice * voucher.DiscountPercentage / 100.0
                        : voucher.DiscountAmount;
            }

            // 7. Thanh toán (Strategy)
            var paymentCtx = new PaymentContext();
            paymentCtx.SetStrategyByName(model.PaymentMethod);
            var paymentResult = await paymentCtx.PayAsync(
                totalPrice, $"ORDER-{DateTime.Now.Ticks}");
            if (!paymentResult.Success)
                return new BookingResult
                {
                    Success = false,
                    Message = $"Thanh toán thất bại: {paymentResult.Message}"
                };

            // 8. Tạo Order (Builder)
            var order = new OrderBuilder()
                .SetCustomer(model.Name ?? "Guest",
                             model.Email ?? "", userId ?? "")
                .SetShowtime(model.ShowtimeId,
                             model.SelectedSeats,
                             selectedSeats.Count, showtime.Price)
                .ApplyVoucher(discount, totalPrice)
                .RedeemPoints(model.PointsRedeemed,
                              totalPrice - discount)
                .SetPaymentMethod(paymentCtx.CurrentPaymentMethod)
                .CalculateTotal()
                .Build();

            // 9. Lưu vào DB
            await _ordersService.StoreDirectOrderAsync(
                model.ShowtimeId,
                model.Name ?? "Guest", model.Email ?? "",
                model.SelectedSeats, selectedSeats.Count,
                totalPrice, discount, model.PointsRedeemed,
                paymentCtx.CurrentPaymentMethod);

            var savedOrder = await _context.Orders
                .OrderByDescending(o => o.Id).FirstOrDefaultAsync();

            double finalPrice = totalPrice - discount
                              - (model.PointsRedeemed * 1000);
            if (finalPrice < 0) finalPrice = 0;
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
}

// ── Controller gọi Facade — Controller giờ rất gọn ─────────────────
[HttpPost]
public async Task<IActionResult> BookTickets(BookTicketsVM model)
{
    if (!ModelState.IsValid)
        return RedirectToAction(nameof(BookTickets),
            new { showtimeId = model.ShowtimeId });

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

#### Biện luận (Giải thích)

**Bước 1: Tạo lớp và phương thức**
Hệ thống xây dựng `BookingFacade` thực thi giao diện `IBookingFacade`. Facade nhận 4 dịch vụ qua Dependency Injection: `AppDbContext`, `IShowtimesService`, `ISeatsService`, `IOrdersService`. Phương thức chính `ProcessBookingAsync` đóng gói toàn bộ 9 bước xử lý đặt vé trong một phương thức duy nhất.

**Bước 2: Kết nối dữ liệu và xử lý logic**
Thay vì Controller gọi trực tiếp 4-5 Service, Controller chỉ gọi `_bookingFacade.ProcessBookingAsync()`. Facade điều phối việc gọi ShowtimesService để lấy suất chiếu, SeatsService để lấy danh sách ghế, OrdersService để kiểm tra ghế trống và lưu đơn hàng. Logic phức tạp được tách biệt hoàn toàn khỏi Controller.

**Bước 3: Kiểm tra điều kiện và xử lý nghiệp vụ**
Mỗi bước trong Facade kiểm tra kết quả trước khi chuyển sang bước tiếp theo: kiểm tra suất chiếu tồn tại, kiểm tra ghế trống, kiểm tra thanh toán thành công. Nếu bất kỳ bước nào thất bại, Facade trả về `BookingResult` với `Success = false` và thông báo lỗi cụ thể ngay lập tức.

**Bước 4: Áp dụng Design Pattern**
Facade Pattern giúp Controller trở nên cực kỳ gọn gàng. Controller chỉ cần gọi Facade và kiểm tra kết quả. Facade cũng đóng vai trò "trung gian" tích hợp các Pattern khác: Bridge (tính giá ghế), Strategy (thanh toán), Builder (tạo Order). Nhờ Facade, mỗi lần thay đổi quy trình đặt vé chỉ cần sửa tại một nơi duy nhất.

**Bước 5: Trả kết quả cho View hoặc client**
`BookingResult` chứa tất cả thông tin cần thiết cho View: `OrderId`, `FinalPrice`, `DiscountApplied`, `PointsEarned`. Controller chỉ việc truyền kết quả vào ViewBag. Luồng xử lý đạt chuẩn: Controller (3 dòng gọi Facade) → BookingFacade (9 bước điều phối) → SQL Server → View.

---

## 5.6. Áp dụng mẫu Bridge Pattern:

### SeatPricingBridge.cs

#### Trước khi áp dụng Bridge Pattern:

```csharp
public class BookingService
{
    public double CalculateSeatPrice(Seat seat, double basePrice)
    {
        // Logic loại ghế bị gắn cứng trong Service
        return seat.SeatType switch
        {
            SeatType.VIP => basePrice * 1.2,
            SeatType.Couple => basePrice * 2.0,
            SeatType.Disabled => basePrice * 0.5,
            _ => basePrice
        };
    }
}
```

#### Biện luận (Giải thích)

**Bước 1: Tạo lớp và phương thức**
Service trực tiếp nhận đối tượng `Seat`, giá cơ bản rồi dùng `switch` để xác định hệ số của từng loại ghế. Logic tính giá nằm cùng với logic đặt vé.

**Bước 2: Kết nối dữ liệu và xử lý logic**
Khi `BookingFacade` duyệt danh sách ghế, nó phải biết chi tiết các loại `VIP`, `Couple`, `Disabled` và công thức tương ứng. Service bị phụ thuộc trực tiếp vào enum `SeatType`.

**Bước 3: Kiểm tra điều kiện và xử lý nghiệp vụ**
Nếu thêm loại ghế mới, lập trình viên phải mở phương thức hiện tại và thêm nhánh `switch`. Nếu có nhiều Service cùng tính giá, các nhánh này dễ bị lặp lại và cho kết quả không đồng nhất.

**Bước 4: Áp dụng Design Pattern**
Phiên bản trước chưa tách abstraction tính giá khỏi implementation. Điều này vi phạm Single Responsibility và làm cho mã nguồn khó mở rộng khi chính sách giá thay đổi.

**Bước 5: Trả kết quả cho View hoặc client**
Giá vé được trả trực tiếp về Controller hoặc Facade, nhưng luồng tính toán phụ thuộc vào một Service lớn: Controller → BookingService (switch loại ghế) → giá vé → View.

---

#### Sau khi dùng Bridge Pattern:

```csharp
namespace movieCinema.Models.Bridge
{
    // Implementation — giao diện cho mọi cách tính giá
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

    // Abstraction — giữ một implementation và uỷ quyền tính giá
    public class SeatPricingBridge
    {
        private readonly ISeatingPricingStrategy _strategy;

        public SeatPricingBridge(ISeatingPricingStrategy strategy)
        {
            _strategy = strategy;
        }

        // Constructor tiện ích: chuyển SeatType thành implementation tương ứng
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
}

// BookingFacade sử dụng Bridge
var bridge = new SeatPricingBridge(
    seat?.SeatType ?? SeatType.Standard);
double price = bridge.GetPrice(showtime.Price);
```

#### Biện luận (Giải thích)

**Bước 1: Tạo lớp và phương thức**
Hệ thống tách cấu trúc thành hai phần. `SeatPricingBridge` là Abstraction, còn `ISeatingPricingStrategy` và các lớp `StandardPricingStrategy`, `VipPricingStrategy`, `CouplePricingStrategy`, `DisabledPricingStrategy` là Implementation. Bridge cung cấp phương thức `GetPrice()` để ủy quyền việc tính toán.

**Bước 2: Kết nối dữ liệu và xử lý logic**
Trong `BookingFacade`, hệ thống chỉ cần tạo `new SeatPricingBridge(seat.SeatType)` và gọi `GetPrice(showtime.Price)`. Facade không cần biết hệ số 1.2, 2.0 hay 0.5 được cài đặt ở đâu. Việc chọn implementation được Bridge thực hiện tập trung.

**Bước 3: Kiểm tra điều kiện và xử lý nghiệp vụ**
Constructor nhận `SeatType` sử dụng switch expression. Các loại ghế đã biết được ánh xạ đến chiến lược phù hợp; giá trị mặc định sử dụng `StandardPricingStrategy`. Ngoài ra, constructor nhận `ISeatingPricingStrategy` cho phép truyền implementation tùy chỉnh khi kiểm thử hoặc mở rộng.

**Bước 4: Áp dụng Design Pattern**
Bridge Pattern tách biệt abstraction khỏi implementation, nhờ đó hai bên có thể thay đổi độc lập. Nếu thay đổi chính sách giá VIP, chỉ sửa `VipPricingStrategy`. Nếu thêm cách tính giá theo ngày hoặc theo khuyến mãi, có thể tạo implementation mới mà không sửa `BookingFacade`.

**Bước 5: Trả kết quả cho View hoặc client**
Giá từng ghế được cộng vào `SeatPricingResult` hoặc tổng tiền đặt vé. Luồng xử lý trở nên rõ ràng: Controller → BookingFacade → SeatPricingBridge → ISeatingPricingStrategy → giá ghế → View.

---

## 5.7. Áp dụng mẫu Proxy Pattern:

### CachedMoviesServiceProxy.cs

#### Trước khi dùng Proxy Pattern:

```csharp
using Microsoft.EntityFrameworkCore;
using movieCinema.Data.Base;
using movieCinema.Models;

namespace movieCinema.Data.Services
{
    public class MoviesService : IMoviesService
    {
        private readonly AppDbContext _context;

        public MoviesService(AppDbContext context)
        {
            _context = context;
        }

        // Mỗi request đều truy vấn thẳng vào database — không có cache
        public async Task<Movie> GetByIdAsync(int id)
        {
            return await _context.Movies
                .Include(m => m.CinemaRooms)
                .FirstOrDefaultAsync(m => m.Id == id);
        }

        public async Task<IEnumerable<Movie>> GetAllAsync()
        {
            return await _context.Movies.ToListAsync();
        }

        public async Task<IEnumerable<Movie>> GetNowShowingMoviesAsync()
        {
            return await _context.Movies
                .Where(m => m.Status == MovieStatus.NowShowing)
                .ToListAsync();
        }

        public async Task<IEnumerable<Movie>> GetComingSoonMoviesAsync()
        {
            return await _context.Movies
                .Where(m => m.Status == MovieStatus.ComingSoon)
                .ToListAsync();
        }

        public async Task AddAsync(Movie entity)
        {
            _context.Movies.Add(entity);
            await _context.SaveChangesAsync();
        }

        public async Task UpdateAsync(int id, Movie entity)
        {
            var existing = await _context.Movies.FindAsync(id);
            if (existing != null)
            {
                existing.Name = entity.Name;
                existing.Description = entity.Description;
                existing.Price = entity.Price;
                await _context.SaveChangesAsync();
            }
        }
    }
}
```

#### Biện luận (Giải thích)

**Bước 1: Tạo lớp và phương thức**
Hệ thống xây dựng `MoviesService` thực thi `IMoviesService`. Lớp này trực tiếp nhận `AppDbContext` qua constructor và thực hiện tất cả truy vấn bằng Entity Framework. Mỗi lần gọi `GetAllAsync`, `GetByIdAsync`... đều truy vấn trực tiếp xuống SQL Server.

**Bước 2: Kết nối dữ liệu và xử lý logic**
Trang chủ hiển thị phim Now Showing và Coming Soon liên tục bị người dùng refresh. Mỗi lần refresh, server phải mở kết nối, parse kết quả, trả về client. Nếu có 1000 người dùng cùng truy cập, SQL Server phải thực hiện hàng ngàn câu SELECT giống hệt nhau, gây quá tải.

**Bước 3: Kiểm tra điều kiện và xử lý nghiệp vụ**
Code thực hiện kiểm tra `FirstOrDefault` và `Find` để lấy entity từ database. Tuy nhiên, không có kiểm tra nào phát hiện dữ liệu đã được truy vấn gần đây. Không có thời gian hết hạn (TTL), không có kho lưu trữ dữ liệu truy vấn lặp lại.

**Bước 4: Áp dụng Design Pattern**
Phiên bản này chưa áp dụng Proxy Pattern. Service truy vấn trực tiếp database mỗi lần gọi. Không có lớp trung gian nào để giảm tải cho database, cũng như không có cơ chế invalidate cache khi dữ liệu thay đổi.

**Bước 5: Trả kết quả cho View hoặc client**
Dữ liệu phim được trả về cho Controller. Vì không có cache, mỗi request đều phải chờ database phản hồi. Trong giờ cao điểm, thời gian phản hồi có thể tăng lên 2-3 giây, làm giảm trải nghiệm người dùng.

---

#### Sau khi dùng Proxy Pattern:

```csharp
using System.Linq.Expressions;
using Microsoft.Extensions.Caching.Memory;
using movieCinema.Data.Base;
using movieCinema.Data.Services;
using movieCinema.Data.ViewModels;
using movieCinema.Models;
using MovieCinema.Data.Enums;

namespace movieCinema.Data.Proxy
{
    public class CachedMoviesServiceProxy : IMoviesService
    {
        private readonly MoviesService _realService;
        private readonly IMemoryCache _cache;
        private static readonly TimeSpan DefaultExpiry
            = TimeSpan.FromMinutes(10);
        private static readonly TimeSpan ShortExpiry
            = TimeSpan.FromMinutes(2);

        public CachedMoviesServiceProxy(
            MoviesService realService, IMemoryCache cache)
        {
            _realService = realService;
            _cache = cache;
        }

        // Read methods — dùng cache với sliding expiration
        public async Task<Movie> GetByIdAsync(int id)
        {
            string key = $"movies:id:{id}";
            return await _cache.GetOrCreateAsync(
                key, async entry =>
                {
                    entry.SlidingExpiration = DefaultExpiry;
                    return await _realService.GetByIdAsync(id);
                }) ?? null!;
        }

        public async Task<IEnumerable<Movie>> GetAllAsync()
        {
            return await _cache.GetOrCreateAsync(
                "movies:all", async entry =>
                {
                    entry.SlidingExpiration = DefaultExpiry;
                    return await _realService.GetAllAsync();
                }) ?? Enumerable.Empty<Movie>();
        }

        public async Task<IEnumerable<Movie>> GetNowShowingMoviesAsync()
        {
            string key = $"movies:nowshowing:{DateTime.Today:yyyyMMdd}";
            return await _cache.GetOrCreateAsync(
                key, async entry =>
                {
                    entry.SlidingExpiration = DefaultExpiry;
                    return (await _realService.GetAllAsync())
                        .Where(m => m.Status == MovieStatus.NowShowing)
                        .ToList();
                }) ?? Enumerable.Empty<Movie>();
        }

        public async Task<IEnumerable<Movie>> GetComingSoonMoviesAsync()
        {
            string key = $"movies:comingsoon:{DateTime.Today:yyyyMMdd}";
            return await _cache.GetOrCreateAsync(
                key, async entry =>
                {
                    entry.SlidingExpiration = DefaultExpiry;
                    return (await _realService.GetAllAsync())
                        .Where(m => m.Status == MovieStatus.ComingSoon)
                        .ToList();
                }) ?? Enumerable.Empty<Movie>();
        }

        public async Task<NewMovieDropdownsVM> GetNewMovieDropdownsValues()
        {
            return await _cache.GetOrCreateAsync(
                "movies:dropdowns", async entry =>
                {
                    entry.SlidingExpiration = ShortExpiry;
                    return await _realService
                        .GetNewMovieDropdownsValues();
                }) ?? new NewMovieDropdownsVM();
        }

        // Write methods — gọi Service thật rồi invalidate cache
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

        public async Task DeleteAsync(int id)
        {
            await _realService.DeleteAsync(id);
            InvalidateAllCaches();
        }

        private void InvalidateAllCaches()
        {
            // Phiên bản đơn giản: xoá các khoá cache chính
        }
    }
}

// ── Đăng ký trong Program.cs ────────────────────────────────────────
builder.Services.AddScoped<MoviesService>();

builder.Services.AddScoped<IMoviesService>(sp =>
{
    var realService = sp.GetRequiredService<MoviesService>();
    var cache = sp.GetRequiredService<IMemoryCache>();
    return new CachedMoviesServiceProxy(realService, cache);
});
```

#### Biện luận (Giải thích)

**Bước 1: Tạo lớp và phương thức**
Hệ thống xây dựng `CachedMoviesServiceProxy` cùng thực thi `IMoviesService` với `MoviesService`. Proxy giữ tham chiếu tới `MoviesService` thật và `IMemoryCache`. Các phương thức Read (Get) được ghi đè để thêm logic cache, các phương thức Write (Add, Update, Delete) đảm bảo cache được invalidate.

**Bước 2: Kết nối dữ liệu và xử lý logic**
Thay vì Controller gọi trực tiếp `MoviesService` (qua DI), Controller nhận được `IMoviesService` nhưng thực chất là Proxy. Mỗi request Read đầu tiên vẫn truy vấn database, nhưng những request tiếp theo trong vòng 10 phút sẽ nhận dữ liệu từ cache trong bộ nhớ — giảm tải cho SQL Server.

**Bước 3: Kiểm tra điều kiện và xử lý nghiệp vụ**
`GetOrCreateAsync` được dùng với `SlidingExpiration = DefaultExpiry (10 phút)`. Nếu cache có dữ liệu, nó trả về ngay lập tức. Nếu không, nó gọi `_realService` rồi lưu kết quả vào cache. `NowShowingMoviesAsync` sử dụng key chứa ngày hiện tại, giúp cache tự động hết hạn khi sang ngày mới.

**Bước 4: Áp dụng Design Pattern**
Proxy Pattern cho phép thêm lớp trung gian để điều khiển truy cập (Cache), mà không cần sửa code của Controller. `MoviesService` ban đầu vẫn giữ nguyên vai trò quản lý dữ liệu — Proxy mới là nơi thêm logic cache, giúp tách biệt trách nhiệm.

**Bước 5: Trả kết quả cho View hoặc client**
Cache bộ nhớ trong RAM cho phép dữ liệu phim được trả về cực nhanh. Khi Admin cập nhật phim (`UpdateAsync`), Proxy gọi Service thật rồi gọi `InvalidateAllCaches()` để buộc cache làm mới. Luồng xử lý: Controller → CachedMoviesServiceProxy (Check cache) → MoviesService → SQL Server → Cache → Controller → Client.

---

## 5.8. Áp dụng mẫu Decorator Pattern:

### PricingDecorators.cs

#### Trước khi dùng Decorator Pattern:

```csharp
public class BookingPriceService
{
    public double CalculateFinalPrice(
        double basePrice,
        Voucher? voucher,
        int loyaltyPoints,
        bool isHappyHour)
    {
        // Các chính sách giảm giá bị dồn vào một phương thức
        double finalPrice = basePrice;

        if (voucher != null && finalPrice >= voucher.MinOrderAmount)
        {
            double discount = voucher.IsPercentage
                ? finalPrice * voucher.DiscountPercentage / 100.0
                : voucher.DiscountAmount;
            finalPrice = Math.Max(0, finalPrice - discount);
        }

        if (loyaltyPoints > 0)
            finalPrice = Math.Max(0,
                finalPrice - loyaltyPoints * 1000.0);

        if (isHappyHour && DateTime.Now.TimeOfDay >= new TimeSpan(14, 0, 0)
                         && DateTime.Now.TimeOfDay <= new TimeSpan(17, 0, 0))
        {
            finalPrice *= 0.85;
        }

        return finalPrice;
    }
}
```

#### Biện luận (Giải thích)

**Bước 1: Tạo lớp và phương thức**
Hệ thống xây dựng `BookingPriceService` với một phương thức nhận giá gốc, voucher, điểm thành viên và cờ Happy Hour. Tất cả chính sách giảm giá được viết trực tiếp trong cùng một phương thức.

**Bước 2: Kết nối dữ liệu và xử lý logic**
Phương thức lần lượt tính voucher, trừ điểm rồi áp dụng Happy Hour. Các thuật toán giảm giá phụ thuộc chặt chẽ vào nhau; muốn sử dụng một chính sách riêng lẻ cũng phải gọi toàn bộ phương thức.

**Bước 3: Kiểm tra điều kiện và xử lý nghiệp vụ**
Các điều kiện về mức đơn hàng tối thiểu, số điểm lớn hơn 0 và khung giờ 14:00–17:00 đều nằm trong Service. Khi quy tắc thay đổi, lập trình viên phải sửa một phương thức ngày càng dài.

**Bước 4: Áp dụng Design Pattern**
Phiên bản trước chưa có Decorator Pattern. Việc thêm chính sách giảm giá làm tăng số lượng `if`, vi phạm Open/Closed và khiến việc kiểm thử từng chính sách độc lập trở nên khó khăn.

**Bước 5: Trả kết quả cho View hoặc client**
Service chỉ trả về một con số cuối cùng nên View khó biết khoản giảm giá nào đã được áp dụng. Luồng xử lý là Controller → BookingPriceService (nhiều if) → giá cuối → View.

---

#### Sau khi dùng Decorator Pattern:

```csharp
namespace movieCinema.Data.Decorators
{
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
            return Math.Max(0,
                discounted - Math.Min(reduction, discounted));
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

        public LoyaltyPointsDecorator(
            IOrderPriceDecorator inner, int points)
        {
            _inner = inner;
            _points = points;
        }

        public double CalculatePrice(double currentPrice)
        {
            double afterVoucher = _inner.CalculatePrice(currentPrice);
            return Math.Max(0,
                afterVoucher - _points * 1000.0);
        }

        public string Description
            => $"Điểm tích lũy (-{_points * 1000:N0}đ)";
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
                double price = _inner.CalculatePrice(currentPrice);
                return price * (1 - _discountPercent / 100.0);
            }
            return _inner.CalculatePrice(currentPrice);
        }

        public string Description
            => $"Happy Hour {_discountPercent}%";
        public int Priority => 3;
    }

    public class OrderPriceCalculator
    {
        public PriceCalculationResult Calculate(
            double basePrice, Voucher? voucher,
            int loyaltyPoints, bool applyHappyHour)
        {
            IOrderPriceDecorator calc =
                new BasePriceCalculator(basePrice);

            if (voucher != null)
                calc = new VoucherDecorator(calc, voucher);
            if (loyaltyPoints > 0)
                calc = new LoyaltyPointsDecorator(calc, loyaltyPoints);
            if (applyHappyHour)
                calc = new HappyHourDecorator(
                    calc, new TimeSpan(14, 0, 0),
                    new TimeSpan(17, 0, 0), 15.0);

            double finalPrice = calc.CalculatePrice(basePrice);
            return new PriceCalculationResult
            {
                OriginalPrice = basePrice,
                FinalPrice = finalPrice,
                DiscountApplied = basePrice - finalPrice,
                Description = calc.Description
            };
        }
    }
}
```

#### Biện luận (Giải thích)

**Bước 1: Tạo lớp và phương thức**
Hệ thống xây dựng `IOrderPriceDecorator` làm Component chung. `BasePriceCalculator` là đối tượng gốc, còn `VoucherDecorator`, `LoyaltyPointsDecorator` và `HappyHourDecorator` là các lớp bao bọc (Decorator), mỗi lớp giữ một `_inner` cùng kiểu giao diện.

**Bước 2: Kết nối dữ liệu và xử lý logic**
`OrderPriceCalculator` khởi tạo từ giá gốc rồi bọc lần lượt bằng các Decorator cần thiết. Mỗi Decorator gọi `_inner.CalculatePrice()` trước khi áp dụng chính sách của mình. Nhờ vậy, các quy tắc giảm giá được kết hợp theo chuỗi mà không làm thay đổi đối tượng gốc.

**Bước 3: Kiểm tra điều kiện và xử lý nghiệp vụ**
`VoucherDecorator` kiểm tra `MinOrderAmount` và giới hạn mức giảm không vượt quá giá hiện tại. `LoyaltyPointsDecorator` bảo đảm giá không âm. `HappyHourDecorator` kiểm tra thời gian hiện tại. Mỗi điều kiện nằm đúng trong lớp chịu trách nhiệm.

**Bước 4: Áp dụng Design Pattern**
Decorator Pattern cho phép thêm hoặc bỏ từng chính sách lúc chạy. Muốn thêm giảm giá sinh viên chỉ cần tạo `StudentDiscountDecorator` thực thi `IOrderPriceDecorator`. Code hiện tại đã xây dựng đầy đủ hạ tầng Decorator; tuy nhiên, tại phiên bản dự án đang khảo sát, `OrderPriceCalculator` chưa được gọi trực tiếp từ `BookingFacade` — Facade vẫn tính voucher bằng logic riêng. Vì vậy, đây là pattern đã cài đặt nhưng chưa tích hợp hoàn toàn vào luồng đặt vé.

**Bước 5: Trả kết quả cho View hoặc client**
`PriceCalculationResult` có thể chứa giá gốc, giá cuối, số tiền giảm và phần mô tả. View có thể hiển thị bảng chi tiết các khoản giảm giá. Luồng dự kiến là Controller → OrderPriceCalculator → chuỗi Decorator → PriceCalculationResult → View.

---

## 5.9. Áp dụng mẫu Chain of Responsibility Pattern:

### OrderPipeline.cs

#### Trước khi dùng Chain of Responsibility:

```csharp
public class BookingValidationService
{
    public async Task<string?> ValidateAsync(BookTicketsVM model)
    {
        // Tất cả validation nằm trong một phương thức lớn
        if (model.ShowtimeId <= 0)
            return "Suất chiếu không hợp lệ.";

        if (string.IsNullOrEmpty(model.SelectedSeats))
            return "Vui lòng chọn ít nhất một ghế.";

        var seats = model.SelectedSeats.Split(',')
            .Select(s => s.Trim())
            .Where(s => !string.IsNullOrEmpty(s)).ToList();
        if (!seats.Any())
            return "Danh sách ghế trống.";
        if (seats.Count > 10)
            return "Không thể đặt quá 10 ghế mỗi lần.";

        var bookedSeats = await _ordersService
            .GetBookedSeatsForShowtimeAsync(model.ShowtimeId);
        foreach (var seat in seats)
            if (bookedSeats.Contains(seat))
                return $"Ghế {seat} đã được đặt.";

        if (!string.IsNullOrEmpty(model.VoucherCode))
        {
            var voucher = await _ordersService
                .GetVoucherByCodeAsync(model.VoucherCode);
            if (voucher == null)
                return "Mã voucher không tồn tại.";
            if (!voucher.IsActive)
                return "Mã voucher đã bị vô hiệu hóa.";
            if (voucher.ExpiryDate < DateTime.Now)
                return "Mã voucher đã hết hạn.";
        }

        if (model.PointsRedeemed > 0)
        {
            if (string.IsNullOrEmpty(model.Email))
                return "Cần email để sử dụng điểm tích lũy.";
            var member = await _ordersService
                .GetMemberByEmailAsync(model.Email);
            if (member == null || member.Points < model.PointsRedeemed)
                return "Số điểm không hợp lệ.";
        }

        return null; // hợp lệ
    }
}
```

#### Biện luận (Giải thích)

**Bước 1: Tạo lớp và phương thức**
Hệ thống xây dựng một `BookingValidationService` duy nhất với phương thức `ValidateAsync`. Phương thức lần lượt kiểm tra dữ liệu đầu vào, ghế, voucher và thành viên.

**Bước 2: Kết nối dữ liệu và xử lý logic**
Validation đơn giản và validation cần truy vấn database bị trộn trong cùng một phương thức. Service phải biết toàn bộ thứ tự kiểm tra và cách thực hiện từng loại kiểm tra.

**Bước 3: Kiểm tra điều kiện và xử lý nghiệp vụ**
Tất cả nhánh lỗi đều trả về ngay trong một hàm lớn. Khi thêm một quy tắc mới, phương thức phải tiếp tục phình to; khi muốn bỏ qua một quy tắc, lập trình viên phải can thiệp vào code trung tâm.

**Bước 4: Áp dụng Design Pattern**
Phiên bản trước chưa có Chain of Responsibility. Các bước kiểm tra không độc lập, không thể tái sử dụng và khó kiểm thử riêng từng quy tắc.

**Bước 5: Trả kết quả cho View hoặc client**
Service trả về chuỗi lỗi đầu tiên hoặc `null`. Controller phải tự hiểu chuỗi này để hiển thị cho Client, còn thông tin như các khoản giảm giá hợp lệ chưa được truyền qua pipeline.

---

#### Sau khi dùng Chain of Responsibility:

```csharp
public class OrderPipelineRequest
{
    public BookTicketsVM Model { get; set; } = null!;
    public int? UserId { get; set; }
    public string? UserEmail { get; set; }
}

public class OrderPipelineResult
{
    public bool IsValid { get; set; }
    public string Message { get; set; } = "";
    public List<string> AppliedDiscounts { get; set; } = new();
    public double TotalDiscount { get; set; }
}

public abstract class OrderPipelineHandler
{
    protected OrderPipelineHandler? _next;

    public OrderPipelineHandler SetNext(OrderPipelineHandler next)
    {
        _next = next;
        return next;
    }

    public abstract Task<OrderPipelineResult> HandleAsync(
        OrderPipelineRequest request, OrderPipelineResult result);
}

public class ValidationHandler : OrderPipelineHandler
{
    public override async Task<OrderPipelineResult> HandleAsync(
        OrderPipelineRequest request, OrderPipelineResult result)
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

        var seats = request.Model.SelectedSeats.Split(',')
            .Select(s => s.Trim())
            .Where(s => !string.IsNullOrEmpty(s)).ToList();
        if (!seats.Any() || seats.Count > 10)
        {
            result.IsValid = false;
            result.Message = seats.Any()
                ? "Không thể đặt quá 10 ghế mỗi lần."
                : "Danh sách ghế trống.";
            return result;
        }

        return _next != null
            ? await _next.HandleAsync(request, result)
            : result;
    }
}

public class SeatAvailabilityHandler : OrderPipelineHandler
{
    private readonly IOrdersService _ordersService;

    public SeatAvailabilityHandler(IOrdersService ordersService)
        => _ordersService = ordersService;

    public override async Task<OrderPipelineResult> HandleAsync(
        OrderPipelineRequest request, OrderPipelineResult result)
    {
        if (!result.IsValid) return result;

        var bookedSeats = await _ordersService
            .GetBookedSeatsForShowtimeAsync(request.Model.ShowtimeId);
        var selectedSeats = request.Model.SelectedSeats.Split(',')
            .Select(s => s.Trim())
            .Where(s => !string.IsNullOrEmpty(s)).ToList();

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
    }
}

public class VoucherValidationHandler : OrderPipelineHandler
{
    private readonly IOrdersService _ordersService;

    public VoucherValidationHandler(IOrdersService ordersService)
        => _ordersService = ordersService;

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
            if (!voucher.IsActive || voucher.ExpiryDate < DateTime.Now)
            {
                result.IsValid = false;
                result.Message = "Mã voucher không còn hiệu lực.";
                return result;
            }
            result.AppliedDiscounts.Add($"Voucher {voucher.Code}");
        }

        return _next != null
            ? await _next.HandleAsync(request, result)
            : result;
    }
}

public class MemberValidationHandler : OrderPipelineHandler
{
    private readonly IOrdersService _ordersService;

    public MemberValidationHandler(IOrdersService ordersService)
        => _ordersService = ordersService;

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
            if (member == null
                || member.Points < request.Model.PointsRedeemed)
            {
                result.IsValid = false;
                result.Message = "Số điểm không hợp lệ.";
                return result;
            }

            result.AppliedDiscounts.Add(
                $"Điểm tích lũy: {request.Model.PointsRedeemed} điểm");
        }

        return _next != null
            ? await _next.HandleAsync(request, result)
            : result;
    }
}

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

#### Biện luận (Giải thích)

**Bước 1: Tạo lớp và phương thức**
Hệ thống xây dựng lớp cơ sở trừu tượng `OrderPipelineHandler` với `_next`, `SetNext()` và `HandleAsync()`. Bốn Handler cụ thể lần lượt phụ trách validation chung, kiểm tra ghế, kiểm tra voucher và kiểm tra thành viên.

**Bước 2: Kết nối dữ liệu và xử lý logic**
`OrderPipelineBuilder` nối các Handler thành chuỗi: `ValidationHandler → SeatAvailabilityHandler → VoucherValidationHandler → MemberValidationHandler`. Mỗi Handler chỉ tập trung vào một loại kiểm tra, sau khi thành công mới chuyển request cho `_next`.

**Bước 3: Kiểm tra điều kiện và xử lý nghiệp vụ**
Kết quả được truyền chung qua `OrderPipelineRequest` và `OrderPipelineResult`. Khi một Handler phát hiện lỗi, nó đặt `IsValid = false`, ghi `Message` rồi dừng chuỗi. Khi hợp lệ, Handler có thể bổ sung dữ liệu như `AppliedDiscounts` trước khi chuyển tiếp.

**Bước 4: Áp dụng Design Pattern**
Chain of Responsibility tách các quy tắc validation thành những đơn vị độc lập. Có thể thêm `AgeValidationHandler` hoặc thay đổi thứ tự Handler bằng cách sửa `OrderPipelineBuilder`, không cần viết lại từng Handler hiện có. Các Handler cũng dễ Unit Test vì mỗi lớp có trách nhiệm rõ ràng.

**Bước 5: Trả kết quả cho View hoặc client**
`CompleteBookingHandler` nhận `OrderPipelineResult`. Nếu không hợp lệ, nó trả thông báo lỗi; nếu hợp lệ, nó chuyển tiếp quy trình đến Facade. Luồng xử lý: Controller/Mediator → Validation Chain → BookingFacade → Database → Client.

---

## 5.10. Áp dụng mẫu Mediator Pattern:

### BookingMediator.cs

#### Trước khi dùng Mediator Pattern:

```csharp
public class OrdersController : Controller
{
    private readonly IBookingFacade _facade;
    private readonly IOrdersService _ordersService;

    public OrdersController(
        IBookingFacade facade, IOrdersService ordersService)
    {
        _facade = facade;
        _ordersService = ordersService;
    }

    [HttpPost]
    public async Task<IActionResult> BookTickets(BookTicketsVM model)
    {
        var result = await _facade.ProcessBookingAsync(
            model, User.Identity?.Name);
        return result.Success
            ? View("BookingCompleted")
            : BadRequest(result.Message);
    }

    [HttpPost]
    public async Task<IActionResult> ConfirmBooking(int id)
    {
        // Controller tự biết Service và trạng thái cần chuyển
        var result = await _ordersService
            .ChangeOrderStatusWithStateAsync(id, "Confirmed");
        return RedirectToAction(nameof(ManageBookings));
    }

    [HttpPost]
    public async Task<IActionResult> CancelBooking(int id)
    {
        var result = await _ordersService
            .ChangeOrderStatusWithStateAsync(id, "Cancelled");
        return RedirectToAction(nameof(ManageBookings));
    }
}
```

#### Biện luận (Giải thích)

**Bước 1: Tạo lớp và phương thức**
Controller trực tiếp gọi `IBookingFacade` cho đặt vé và `IOrdersService` cho xác nhận hoặc hủy đơn hàng. Mỗi Action phải biết chính xác Service, tham số và trạng thái cần truyền.

**Bước 2: Kết nối dữ liệu và xử lý logic**
Khi quy trình đặt vé thêm validation chain, email hoặc ghi audit log, Controller phải tiếp tục nhận thêm dependency và gọi thêm thành phần. Các Controller khác cũng có thể gọi cùng Service theo những cách khác nhau.

**Bước 3: Kiểm tra điều kiện và xử lý nghiệp vụ**
Action tự xử lý response, redirect và lỗi. Logic điều phối bị lặp lại ở nhiều Action, làm tăng sự phụ thuộc giữa Controller và các module nghiệp vụ.

**Bước 4: Áp dụng Design Pattern**
Phiên bản trước chưa có Mediator. Controller đóng vai trò trung tâm giao tiếp trực tiếp với nhiều đối tượng, tạo coupling cao và khiến việc thay đổi quy trình xử lý một request phải sửa Controller.

**Bước 5: Trả kết quả cho View hoặc client**
Controller vẫn trả View hoặc Redirect, nhưng luồng phụ thuộc trực tiếp: Controller → nhiều Service → Database. Khó kiểm thử toàn bộ use case vì phải kiểm thử trực tiếp các Action có nhiều dependency.

---

#### Sau khi dùng Mediator Pattern:

```csharp
namespace movieCinema.Data.Mediator
{
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

    // Request objects
    public class CompleteBookingRequest
        : IRequest<CompleteBookingResponse>
    {
        public BookTicketsVM Model { get; set; } = null!;
        public string? UserId { get; set; }
    }

    public class CancelBookingRequest
        : IRequest<CancelBookingResponse>
    {
        public int OrderId { get; set; }
    }

    public class ConfirmBookingRequest
        : IRequest<ConfirmBookingResponse>
    {
        public int OrderId { get; set; }
    }

    // Response objects
    public class CompleteBookingResponse
    {
        public bool Success { get; set; }
        public string Message { get; set; } = "";
        public int? OrderId { get; set; }
        public double FinalPrice { get; set; }
        public double DiscountApplied { get; set; }
        public List<string> AppliedDiscounts { get; set; } = new();
    }

    public class CancelBookingResponse
    {
        public bool Success { get; set; }
        public string Message { get; set; } = "";
    }

    public class ConfirmBookingResponse
    {
        public bool Success { get; set; }
        public string Message { get; set; } = "";
    }

    // Mediator implementation — tìm Handler tương ứng bằng DI
    public class AppMediator : IMediator
    {
        private readonly IServiceProvider _serviceProvider;

        public AppMediator(IServiceProvider serviceProvider)
            => _serviceProvider = serviceProvider;

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

            var method = handlerType.GetMethod("HandleAsync")
                ?? throw new InvalidOperationException(
                    "HandleAsync not found.");
            var result = method.Invoke(handler, new[] { request });
            if (result is Task<TResponse> task)
                return await task;

            throw new InvalidOperationException(
                "Handler returned an invalid response.");
        }
    }

    // Handler đặt vé — kết hợp Chain và Facade
    public class CompleteBookingHandler
        : IRequestHandler<CompleteBookingRequest,
                          CompleteBookingResponse>
    {
        private readonly IBookingFacade _facade;
        private readonly IOrdersService _ordersService;

        public CompleteBookingHandler(
            IBookingFacade facade, IOrdersService ordersService)
        {
            _facade = facade;
            _ordersService = ordersService;
        }

        public async Task<CompleteBookingResponse> HandleAsync(
            CompleteBookingRequest request)
        {
            var pipeline = OrderPipelineBuilder
                .Build(_ordersService);
            var pipelineResult = await pipeline.HandleAsync(
                new OrderPipelineRequest { Model = request.Model },
                new OrderPipelineResult { IsValid = true });

            if (!pipelineResult.IsValid)
                return new CompleteBookingResponse
                {
                    Success = false,
                    Message = pipelineResult.Message
                };

            var bookingResult = await _facade
                .ProcessBookingAsync(request.Model, request.UserId);
            return new CompleteBookingResponse
            {
                Success = bookingResult.Success,
                Message = bookingResult.Message,
                OrderId = bookingResult.OrderId,
                FinalPrice = bookingResult.FinalPrice,
                DiscountApplied = bookingResult.DiscountApplied,
                AppliedDiscounts = pipelineResult.AppliedDiscounts
            };
        }
    }

    public class CancelBookingHandler
        : IRequestHandler<CancelBookingRequest,
                          CancelBookingResponse>
    {
        private readonly IOrdersService _ordersService;

        public CancelBookingHandler(IOrdersService ordersService)
            => _ordersService = ordersService;

        public async Task<CancelBookingResponse> HandleAsync(
            CancelBookingRequest request)
        {
            var result = await _ordersService
                .ChangeOrderStatusWithStateAsync(
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

    public class ConfirmBookingHandler
        : IRequestHandler<ConfirmBookingRequest,
                          ConfirmBookingResponse>
    {
        private readonly IOrdersService _ordersService;

        public ConfirmBookingHandler(IOrdersService ordersService)
            => _ordersService = ordersService;

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
}

// Đăng ký trong Program.cs
builder.Services.AddScoped<IMediator, AppMediator>();
builder.Services.AddScoped<
    IRequestHandler<CompleteBookingRequest, CompleteBookingResponse>,
    CompleteBookingHandler>();
builder.Services.AddScoped<
    IRequestHandler<CancelBookingRequest, CancelBookingResponse>,
    CancelBookingHandler>();
builder.Services.AddScoped<
    IRequestHandler<ConfirmBookingRequest, ConfirmBookingResponse>,
    ConfirmBookingHandler>();
```

#### Biện luận (Giải thích)

**Bước 1: Tạo lớp và phương thức**
Hệ thống xây dựng `IMediator`, `IRequest<TResponse>` và `IRequestHandler<TRequest,TResponse>`. Mỗi use case có một Request và Handler tương ứng: hoàn tất đặt vé, hủy đặt vé, xác nhận đặt vé.

**Bước 2: Kết nối dữ liệu và xử lý logic**
`AppMediator` nhận một Request, dùng reflection tạo kiểu Handler tương ứng, rồi lấy Handler từ DI Container. Controller không cần biết Handler cụ thể hay các Service phía sau. `CompleteBookingHandler` tiếp tục phối hợp Chain of Responsibility và BookingFacade.

**Bước 3: Kiểm tra điều kiện và xử lý nghiệp vụ**
Handler chịu trách nhiệm điều phối và chuyển response có kiểu rõ ràng. Nếu không tìm thấy Handler hoặc Handler trả sai kiểu, Mediator ném `InvalidOperationException`, giúp phát hiện lỗi cấu hình DI sớm.

**Bước 4: Áp dụng Design Pattern**
Mediator làm giảm coupling giữa Controller và các đối tượng nghiệp vụ. Việc thêm use case mới chỉ cần tạo Request, Response, Handler và đăng ký DI. Ngoài ra, Mediator là điểm kết hợp với Chain, Facade và State trong dự án.

**Bước 5: Trả kết quả cho View hoặc client**
Controller chỉ cần tạo Request và gọi `_mediator.SendAsync(request)`, sau đó xử lý Response. Luồng chuẩn là: Controller → IMediator → Request Handler → Chain/Facade/State → Database → Response → Client. Lưu ý: các Handler đã được đăng ký trong `Program.cs`, nhưng ở phiên bản hiện tại `OrdersController` vẫn gọi trực tiếp Facade/OrdersService ở một số Action; Mediator chưa được dùng làm entry point cho toàn bộ request.

---

## 5.11. Áp dụng mẫu Observer Pattern:

### OrderObserver.cs

#### Trước khi dùng Observer Pattern:

```csharp
public class OrdersService
{
    public async Task<StatusChangeResult>
        ChangeOrderStatusWithStateAsync(int orderId, string newStatus)
    {
        var order = await _context.Orders
            .Include(o => o.OrderItems)
            .FirstOrDefaultAsync(o => o.Id == orderId);
        if (order == null) return Fail("Không tìm thấy.");

        string oldStatus = order.Status;
        if (!IsValidTransition(oldStatus, newStatus))
            return Fail("Chuyển trạng thái không hợp lệ.");

        order.Status = newStatus;
        await _context.SaveChangesAsync();

        // Service phải tự gọi tất cả side-effect
        // 1. Ghi log
        _logger.LogInformation(
            "Order {Id} changed {From} -> {To}",
            order.Id, oldStatus, newStatus);

        // 2. Cập nhật điểm thành viên
        if (newStatus == "Cancelled" || newStatus == "Refunded")
            await RefundLoyaltyPointsAsync(order);
        else if (newStatus == "Confirmed")
            await EarnLoyaltyPointsAsync(order);

        // 3. Gửi email
        await SendStatusEmailAsync(order, oldStatus, newStatus);

        return Success(oldStatus, newStatus);
    }
}
```

#### Biện luận (Giải thích)

**Bước 1: Tạo lớp và phương thức**
`OrdersService` chứa toàn bộ side-effect: ghi log, cập nhật điểm, gửi email. Mỗi lần cần thêm một tác dụng phụ, lập trình viên phải sửa chính phương thức đang xử lý nghiệp vụ chính.

**Bước 2: Kết nối dữ liệu và xử lý logic**
Logic nghiệp vụ (chuyển trạng thái) bị trộn lẫn với logic phụ trợ. Mỗi lần thêm notification hoặc audit log mới, Service phải sửa đổi.

**Bước 3: Kiểm tra điều kiện và xử lý nghiệp vụ**
Mỗi side-effect được gọi tuần tự. Nếu một bước thất bại (ví dụ gửi email lỗi), các bước sau có thể bị ảnh hưởng. Logic nghiệp vụ chính (chuyển trạng thái) cũng không thể tách riêng.

**Bước 4: Áp dụng Design Pattern**
Phiên bản trước chưa có Observer Pattern. Service phải biết toàn bộ side-effect, vi phạm Single Responsibility. Việc kiểm thử logic chuyển trạng thái thuần túy trở nên khó vì phải mock logger, email service và member service cùng lúc.

**Bước 5: Trả kết quả cho View hoặc client**
Service trả `StatusChangeResult` về Controller. Controller không biết side-effect nào đã được thực hiện. Nếu một bước phụ trợ bị lỗi âm thầm, hệ thống vẫn báo thành công nhưng người dùng có thể không nhận được email.

---

#### Sau khi dùng Observer Pattern:

```csharp
namespace movieCinema.Data.Observer
{
    public interface IOrderObserver
    {
        Task OnOrderStatusChangedAsync(
            Order order, string oldStatus, string newStatus);
    }

    public interface IOrderSubject
    {
        void Attach(IOrderObserver observer);
        void Detach(IOrderObserver observer);
        Task NotifyAsync(Order order,
                         string oldStatus, string newStatus);
    }

    public class OrderSubject : IOrderSubject
    {
        private readonly List<IOrderObserver> _observers = new();
        private readonly IServiceScopeFactory _scopeFactory;
        private bool _initialized;
        private readonly object _lock = new();

        public OrderSubject(IServiceScopeFactory scopeFactory)
            => _scopeFactory = scopeFactory;

        public void Attach(IOrderObserver observer)
        {
            lock (_lock) { _observers.Add(observer); }
        }

        public void Detach(IOrderObserver observer)
        {
            lock (_lock) { _observers.Remove(observer); }
        }

        public async Task NotifyAsync(
            Order order, string oldStatus, string newStatus)
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
                    // Không để lỗi observer làm hỏng cả pipeline
                }
            }
        }

        private List<IOrderObserver> GetScopedObservers()
        {
            if (_initialized) return _observers.ToList();

            using var scope = _scopeFactory.CreateScope();
            var scoped = scope.ServiceProvider
                .GetServices<IOrderObserver>().ToList();
            lock (_lock)
            {
                foreach (var obs in scoped)
                    if (!_observers.Contains(obs))
                        _observers.Add(obs);
                _initialized = true;
            }
            return _observers.ToList();
        }
    }

    // Observer 1: ghi log
    public class AuditLogObserver : IOrderObserver
    {
        private readonly ILogger<AuditLogObserver> _logger;

        public AuditLogObserver(
            ILogger<AuditLogObserver> logger) => _logger = logger;

        public Task OnOrderStatusChangedAsync(
            Order order, string oldStatus, string newStatus)
        {
            _logger.LogInformation(
                "[AUDIT] Order #{Id} | Email: {Email} | {From} → {To}",
                order.Id, order.Email, oldStatus, newStatus);
            return Task.CompletedTask;
        }
    }

    // Observer 2: cập nhật điểm
    public class LoyaltyPointsObserver : IOrderObserver
    {
        private readonly IServiceScopeFactory _scopeFactory;

        public LoyaltyPointsObserver(
            IServiceScopeFactory scopeFactory)
            => _scopeFactory = scopeFactory;

        public async Task OnOrderStatusChangedAsync(
            Order order, string oldStatus, string newStatus)
        {
            if (string.IsNullOrEmpty(order.Email)) return;

            using var scope = _scopeFactory.CreateScope();
            var context = scope.ServiceProvider
                .GetRequiredService<AppDbContext>();

            var member = await context.Members
                .FirstOrDefaultAsync(m =>
                    m.Email.ToLower() == order.Email.ToLower());
            if (member == null) return;

            double finalPrice = Math.Max(0,
                order.TotalPrice - order.DiscountAmount);
            int earned = (int)(finalPrice / 10000);

            if (newStatus == "Cancelled" || newStatus == "Refunded")
                member.Points = Math.Max(0,
                    member.Points - earned
                    + (order.PointsRedeemed / 1000));
            else if (newStatus == "Confirmed"
                     && oldStatus == "Purchased")
                member.Points += earned;

            await context.SaveChangesAsync();
        }
    }

    // Observer 3: email thông báo
    public class EmailNotificationObserver : IOrderObserver
    {
        private readonly ILogger<EmailNotificationObserver> _logger;

        public EmailNotificationObserver(
            ILogger<EmailNotificationObserver> logger)
            => _logger = logger;

        public Task OnOrderStatusChangedAsync(
            Order order, string oldStatus, string newStatus)
        {
            if (string.IsNullOrEmpty(order.Email))
                return Task.CompletedTask;

            var (subject, body) = newStatus switch
            {
                "Confirmed" => ("[MovieCinema] Đơn hàng đã xác nhận",
                                $"Đơn hàng #{order.Id} đã xác nhận."),
                "Cancelled" => ("[MovieCinema] Đơn hàng bị huỷ",
                                $"Đơn hàng #{order.Id} đã huỷ."),
                "Refunded"  => ("[MovieCinema] Hoàn tiền",
                                $"Đơn hàng #{order.Id} đã hoàn tiền."),
                _ => (null, null)
            };

            if (subject != null)
                _logger.LogInformation(
                    "[EMAIL] To: {Email} | {Subject}",
                    order.Email, subject);

            return Task.CompletedTask;
        }
    }
}

// Đăng ký trong Program.cs
builder.Services.AddSingleton<IOrderSubject, OrderSubject>();
builder.Services.AddScoped<IOrderObserver, AuditLogObserver>();
builder.Services.AddScoped<IOrderObserver, LoyaltyPointsObserver>();
builder.Services.AddScoped<IOrderObserver, EmailNotificationObserver>();

// Đồng bộ với State Machine
public async Task<StatusChangeResult>
    ChangeOrderStatusWithStateAsync(int orderId, string newStatus)
{
    var order = await _context.Orders
        .Include(o => o.OrderItems)
        .FirstOrDefaultAsync(o => o.Id == orderId);
    if (order == null) return Fail("Không tìm thấy.");

    string oldStatus = order.Status;
    if (!OrderStateMachine.CanTransition(oldStatus, newStatus))
        return Fail("Chuyển trạng thái không hợp lệ.");

    order.Status = newStatus;

    // State Machine xử lý hoàn điểm trực tiếp
    var state = OrderStateMachine.GetState(newStatus);
    if (state != null)
        await state.OnEnterAsync(order, _context);

    await _context.SaveChangesAsync();

    // Gửi thông báo cho tất cả observer
    await _orderSubject.NotifyAsync(order, oldStatus, newStatus);

    return Success(oldStatus, newStatus);
}
```

#### Biện luận (Giải thích)

**Bước 1: Tạo lớp và phương thức**
Hệ thống xây dựng giao diện `IOrderObserver` (cho từng side-effect) và `IOrderSubject` (cho cơ chế phát thông báo). `OrderSubject` quản lý danh sách observer và đảm bảo thread-safe khi thêm/xoá.

**Bước 2: Kết nối dữ liệu và xử lý logic**
Ba observer cụ thể (`AuditLogObserver`, `LoyaltyPointsObserver`, `EmailNotificationObserver`) được đăng ký là `Scoped`. Khi `OrdersService` chuyển trạng thái thành công, nó gọi `_orderSubject.NotifyAsync(order, oldStatus, newStatus)`. Mỗi observer tự xử lý phần của mình mà không cần Service biết chi tiết.

**Bước 3: Kiểm tra điều kiện và xử lý nghiệp vụ**
Nếu một observer ném exception, nó bị bắt lại ngay trong `NotifyAsync`, các observer còn lại vẫn chạy tiếp. Nhờ vậy, một lỗi phụ trợ không phá vỡ nghiệp vụ chính.

**Bước 4: Áp dụng Design Pattern**
Observer Pattern giúp tách side-effect khỏi nghiệp vụ chính. Khi cần thêm SMS notification hoặc push notification, chỉ cần tạo observer mới và đăng ký. Service chính không cần sửa, dễ dàng mở rộng. Lưu ý: ở phiên bản hiện tại, `OrderSubject` đã được đăng ký Singleton và ba observer đã được đăng ký Scoped, nhưng phương thức `NotifyAsync` chưa được gọi từ `OrdersService.ChangeOrderStatusWithStateAsync` — vẫn còn tích hợp ở mức hạ tầng.

**Bước 5: Trả kết quả cho View hoặc client**
Sau khi gửi thông báo cho observer, Service trả `StatusChangeResult`. Controller dùng `TempData` hiển thị thông báo. Luồng dự kiến: Controller → OrdersService (đổi trạng thái) → OrderSubject → nhiều Observer → Logger/Email/Member Points → Controller → View.

---

## 5.12. Tổng kết các Design Pattern đã áp dụng

| # | Pattern | File | Mục đích | Trạng thái tích hợp |
|---|---------|------|----------|----------------------|
| 5.1 | Singleton | `Data/Cart/ShoppingCart.cs` | Duy trì một `ShoppingCart` duy nhất cho mỗi session người dùng | Đang dùng (qua DI Scoped + Session) |
| 5.2 | Builder | `Models/Builders/OrderBuilder.cs` | Xây dựng `Order` theo từng bước (Fluent Interface) | Đang dùng trong `BookingFacade` |
| 5.3 | Strategy | `Data/Strategy/PaymentStrategy.cs` | Hỗ trợ nhiều phương thức thanh toán (Cash, PayPal) | Đang dùng trong `BookingFacade` |
| 5.4 | State | `Data/State/OrderStateMachine.cs` | Quản lý vòng đời đơn hàng Purchased → Confirmed → Cancelled/Refunded | Đang dùng trong `OrdersService` |
| 5.5 | Facade | `Data/Facade/BookingFacade.cs` | Đóng gói 9 bước xử lý đặt vé cho `OrdersController` | Đang dùng trong `OrdersController.BookTickets` |
| 5.6 | Bridge | `Models/Bridge/SeatPricingBridge.cs` | Tách abstraction tính giá ghế khỏi implementation theo `SeatType` | Đang dùng trong `BookingFacade` |
| 5.7 | Proxy | `Data/Proxy/CachedMoviesServiceProxy.cs` | Cache trong bộ nhớ cho `MoviesService` | Đang dùng qua DI (wrap IMoviesService) |
| 5.8 | Decorator | `Data/Decorators/PricingDecorators.cs` | Kết hợp voucher, điểm, Happy Hour vào chuỗi giảm giá | Đã code, chưa gọi từ Facade |
| 5.9 | Chain of Responsibility | `Data/Chain/OrderPipeline.cs` | Validation đặt vé qua 4 handler: Validation, Seat, Voucher, Member | Đang dùng qua `CompleteBookingHandler` |
| 5.10 | Mediator | `Data/Mediator/BookingMediator.cs` | Điều phối Request/Response giữa Controller và các Handler | Đã đăng ký DI, Controller chưa dùng làm entry point |
| 5.11 | Observer | `Data/Observer/OrderObserver.cs` | Ghi log, cập nhật điểm, gửi email khi đơn đổi trạng thái | Đã đăng ký DI, `NotifyAsync` chưa gọi từ Service |

### Nhận xét tổng quát

- Các Pattern hoạt động đồng thời theo mô hình pipeline: **Mediator** nhận Request, gọi **CompleteBookingHandler** → **Chain of Responsibility** validation → **Facade** điều phối nhiều bước → **Bridge** tính giá, **Strategy** chọn phương thức thanh toán, **Builder** tạo `Order`. Sau khi lưu xuống DB, `OrderStateMachine` xử lý chuyển trạng thái và (khi tích hợp) gửi thông báo cho các **Observer**.

- Sự kết hợp giúp hệ thống giữ được **Separation of Concerns**: mỗi Pattern giải quyết một bài toán cụ thể (tạo đối tượng, chuyển trạng thái, tính giá, mở rộng side-effect, v.v.).

- Việc tích hợp một số Pattern (`Decorator`, `Mediator`, `Observer`) hiện ở mức hạ tầng — code đã có, DI đã đăng ký, nhưng phần gọi từ Controller/Service chưa hoàn tất. Đây là cơ hội tốt để tiếp tục mở rộng trong các phiên bản tiếp theo.

- Một số Pattern triển khai sẵn theo GoF (Observer subject-observer chain, Decorator wrap by interface, Mediator dispatch by reflection) tạo nền tảng vững chắc cho phát triển và bảo trì lâu dài.



