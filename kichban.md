# 🎬 KỊCH BẢN DEMO WEB VÀ GIẢI THÍCH DESIGN PATTERNS - MOVIECINEMA

> **Dự án:** Hệ thống đặt vé xem phim trực tuyến **MovieCinema** (.NET 8 ASP.NET Core MVC, Entity Framework Core, SQL Server)  
> **Mục tiêu:** Hướng dẫn từng bước thao tác demo trực quan trên giao diện Web, kết hợp trình bày nguyên lý và giải thích chi tiết nguồn code của **12 GoF Design Patterns** đã áp dụng thành công trong dự án.

---

## 📌 1. BẢNG TỔNG QUAN HỆ THỐNG DESIGN PATTERNS DEMO

| STT | Tên Pattern | Nhóm | Vị trí Thao tác trên Web (UI) | Tệp Nguồn Triển khai | Lợi ích Kiến trúc mang lại |
|:---:|:---|:---:|:---|:---|:---|
| **1** | [Proxy](#bước-1-proxy-pattern--tối-ưu-tải-trang-chủ-bằng-cache) | Structural | Trang chủ (`/`), Trang Chi tiết phim | [CachedMoviesServiceProxy.cs](file:///e:/Document/GianDuyKhanh-23DH111541/movieCinema/movieCinema/Data/Proxy/CachedMoviesServiceProxy.cs) | Caching dữ liệu vào RAM, giảm 80-90% truy vấn trực tiếp vào Database |
| **2** | [Singleton](#bước-2-singleton-pattern--quản-lý-giỏ-hàng-shoppingcart) | Creational | Giỏ hàng (`/Orders/ShoppingCart`) | [ShoppingCart.cs](file:///e:/Document/GianDuyKhanh-23DH111541/movieCinema/movieCinema/Data/Cart/ShoppingCart.cs) | Đảm bảo 1 duy nhất Instance Giỏ hàng trong từng Session người dùng |
| **3** | [Bridge](#bước-3-bridge-pattern--phân-loại-giá-ghế-linh-hoạt) | Structural | Trang Chọn ghế (`/Showtimes/Book/ID`) | [SeatPricingBridge.cs](file:///e:/Document/GianDuyKhanh-23DH111541/movieCinema/movieCinema/Models/Bridge/SeatPricingBridge.cs) | Tách biệt logic loại ghế (VIP/Couple/Standard) khỏi công thức tính giá |
| **4** | [Chain of Resp.](#bước-4-chain-of-responsibility--pipeline-kiểm-tra-điều-kiện-đặt-vé) | Behavioral | Nút "Xác nhận đặt vé" | [OrderPipeline.cs](file:///e:/Document/GianDuyKhanh-23DH111541/movieCinema/movieCinema/Data/Chain/OrderPipeline.cs) | Chuỗi xử lý kiểm tra: Ghế trống -> Hạn vé -> Số dư -> Hợp lệ đơn |
| **5** | [Strategy & Adapter](#bước-5-strategy--adapter-pattern--thanh-toán-đa-phương-thức) | Behavioral | Chọn phương thức Thanh toán | [PaymentStrategy.cs](file:///e:/Document/GianDuyKhanh-23DH111541/movieCinema/movieCinema/Data/Strategy/PaymentStrategy.cs) | Đa dạng hóa các cổng thanh toán (Tiền mặt, VNPay, MoMo) & bọc API |
| **6** | [Decorator](#bước-6-decorator-pattern--tính-chiết-khấu-và-phụ-phí-đơn-hàng) | Structural | Ô nhập Voucher / Chọn Combo | [PricingDecorators.cs](file:///e:/Document/GianDuyKhanh-23DH111541/movieCinema/movieCinema/Data/Decorators/PricingDecorators.cs) | Mở rộng tính giá (Voucher, Điểm thưởng, Combo) linh hoạt không sửa Order gốc |
| **7** | [Builder](#bước-7-builder-pattern--khởi-tạo-đối-tượng-đơn-hàng-phức-tạp) | Creational | Xử lý tạo Order Backend | [OrderBuilder.cs](file:///e:/Document/GianDuyKhanh-23DH111541/movieCinema/movieCinema/Models/Builders/OrderBuilder.cs) | Khởi tạo đối tượng `Order` nhiều thuộc tính từng bước rõ ràng, an toàn |
| **8** | [Facade](#bước-8-facade-pattern--đơn-giản-hóa-toàn-bộ-luồng-đặt-vé) | Structural | Đặt vé nhanh (Quick Booking) | [BookingFacade.cs](file:///e:/Document/GianDuyKhanh-23DH111541/movieCinema/movieCinema/Data/Facade/BookingFacade.cs) | Cung cấp 1 Interface duy nhất che giấu sự phức tạp của Subsystem |
| **9** | [Mediator](#bước-9-mediator-pattern--điều-phối-giao-tiếp-các-services) | Behavioral | Luồng điều phối giữa Controllers | [BookingMediator.cs](file:///e:/Document/GianDuyKhanh-23DH111541/movieCinema/movieCinema/Data/Mediator/BookingMediator.cs) | Giảm sự phụ thuộc trực tiếp (Coupling) giữa Controller và nhiều Service |
| **10** | [State](#bước-10-state-pattern--quản-lý-vòng-đời-trạng-thái-đơn-hàng) | Behavioral | Trang Lịch sử Đơn hàng (`/Orders/Index`) | [OrderStateMachine.cs](file:///e:/Document/GianDuyKhanh-23DH111541/movieCinema/movieCinema/Data/State/OrderStateMachine.cs) | Chuyển đổi trạng thái đơn (Pending -> Paid -> Cancelled) an toàn, chặt chẽ |
| **11** | [Observer](#bước-11-observer-pattern--thông-báo-tự-động-khi-thay-đổi-trạng-thái) | Behavioral | Khi đơn hàng Đã thanh toán / Hủy vé | [OrderObserver.cs](file:///e:/Document/GianDuyKhanh-23DH111541/movieCinema/movieCinema/Data/Observer/OrderObserver.cs) | Gửi Email xác nhận và bắn Real-time Notification tự động |

---

## 📽️ 2. KỊCH BẢN THAO TÁC DEMO THỰC TẾ & LỜI THOẠI TRUYỀN TẢI

---

### BƯỚC 1: PROXY PATTERN — TỐI ƯU TẢI TRANG CHỦ BẰNG CACHE

#### 🗣️ Lời thoại Thuyết trình & Thao tác Demo:
- **Thao tác:** Mở trình duyệt, truy cập vào Trang chủ `https://localhost:7198/`. Sau đó nhấn `F5` tải lại trang nhiều lần hoặc chuyển qua lại giữa trang *Phim Đang Chiếu* và *Phim Sắp Chiếu*.
- **Lời thoại:** *"Đầu tiên tại Trang chủ của hệ thống MovieCinema, ứng dụng cần hiển thị danh sách các bộ phim đang chiếu. Nếu mỗi lần người dùng F5 hoặc truy cập trang mà ứng dụng đều phải gọi xuống SQL Server để query dữ liệu thì sẽ rất chậm và dễ gây quá tải database. Vì vậy nhóm đã áp dụng **Proxy Pattern** qua lớp Proxy Cache."*

#### 📺 Hiện tượng quan sát được:
- Lần truy cập đầu tiên: Trang nạp dữ liệu từ DB (mất khoảng vài trăm ms).
- Các lần F5 tiếp theo: Trang tải **tức thì (gần như 0ms)** do dữ liệu phim đã được lấy trực tiếp từ Memory Cache của hệ thống.

#### 🧠 Nguyên lý Design Pattern (Proxy):
Proxy đóng vai trò làm Đại diện (Surrogate / Placeholder) cho dịch vụ thật `MoviesService`. Mọi truy vấn từ Controller đều đi qua Proxy trước; Proxy kiểm tra xem RAM cache có dữ liệu chưa. Nếu có thì trả về ngay, nếu chưa có mới gọi `RealService` truy vấn DB và lưu lại vào Cache.

---

### BƯỚC 2: SINGLETON PATTERN — QUẢN LÝ GIỎ HÀNG (SHOPPING CART)

#### 🗣️ Lời thoại Thuyết trình & Thao tác Demo:
- **Thao tác:** Đăng nhập tài khoản khách hàng, nhấn nút **"Thêm vào giỏ hàng"** ở một bộ phim. Sau đó chuyển sang các trang khác như *Danh sách phim*, *Diễn viên* rồi quay lại `/Orders/ShoppingCart`.
- **Lời thoại:** *"Tiếp theo là tính năng Giỏ hàng. Giỏ hàng phải duy trì trạng thái nhất quán trong suốt Session của người dùng hiện tại, tránh việc mỗi action lại khởi tạo ra một giỏ hàng mới độc lập gây mất dữ liệu. Nhóm áp dụng **Singleton Pattern** để quản lý giỏ hàng theo Session."*

#### 📺 Hiện tượng quan sát được:
- Dù người dùng chuyển qua bất kỳ tab hay trang nào trên website, danh sách vé phim và đồ ăn đã chọn trong giỏ hàng vẫn được giữ nguyên đầy đủ.

#### 🧠 Nguyên lý Design Pattern (Singleton):
Đảm bảo một Class chỉ có duy nhất một đối tượng đại diện (instance) trong một phạm vi (ở đây là phạm vi Session làm việc của một User) và cung cấp một điểm truy cập toàn cục tới instance đó.

---

### BƯỚC 3: BRIDGE PATTERN — PHÂN LOẠI GIÁ GHẾ LINH HOẠT (VIP / COUPLE / STANDARD)

#### 🗣️ Lời thoại Thuyết trình & Thao tác Demo:
- **Thao tác:** Vào chi tiết 1 Phim -> Nhấn **"Đặt vé ngay"** để chọn Suất chiếu -> Màn hình sơ đồ phòng chiếu xuất hiện. Thao tác nhấp chọn:
  - 1 Ghế thường (Standard - VD: A1) -> Xem tổng tiền.
  - 1 Ghế VIP (VD: E5) -> Xem giá ghế tăng lên (+20%).
  - 1 Ghế Đôi (Couple - VD: H1) -> Xem giá ghế tăng gấp đôi (+100%).
- **Lời thoại:** *"Tại sơ đồ chọn ghế, rạp chiếu phim có nhiều loại ghế khác nhau (Ghế Thường, VIP, Couple) với chính sách giá riêng. Nếu dùng `if-else` hoặc `switch-case` rải rác ở Controller thì code rất bẩn. Nhóm sử dụng **Bridge Pattern** để tách biệt abstractions của Ghế với công thức tính giá tương ứng."*

#### 📺 Hiện tượng quan sát được:
Khi nhấp chọn các loại ghế khác nhau trên giao diện UI, giá của từng ghế và tổng tiền tạm tính lập tức được cập nhật chính xác theo hệ số của loại ghế đó.

#### 🧠 Nguyên lý Design Pattern (Bridge):
Bridge giúp tách rời phần Trừu tượng (Abstraction) khỏi phần Thực thi (Implementation), giúp cả hai có thể biến đổi độc lập. Loại ghế (`SeatType`) và Thuật toán tính giá ghế (`ISeatPricingStrategy`) được nối với nhau qua một "Cây cầu" Bridge.

---

### BƯỚC 4: CHAIN OF RESPONSIBILITY — PIPELINE KIỂM TRA ĐIỀU KIỆN ĐẶT VÉ

#### 🗣️ Lời thoại Thuyết trình & Thao tác Demo:
- **Thao tác:** Chọn ghế đã có người đặt (ghế màu đỏ) hoặc chọn suất chiếu đã qua giờ chiếu -> Nhấn **"Thanh toán"**.
- **Lời thoại:** *"Trước khi tạo đơn hàng, hệ thống phải thực hiện hàng loạt bước kiểm tra nghiêm ngặt: 1. Ghế có bị trùng người khác vừa đặt không? 2. Suất chiếu còn hạn không? 3. Điểm thưởng/Voucher có hợp lệ không? Thay vì viết một hàm dài hàng trăm dòng, nhóm áp dụng **Chain of Responsibility Pattern** tạo thành một Pipeline kiểm tra tuần tự."*

#### 📺 Hiện tượng quan sát được:
Nếu vi phạm bất kỳ bước nào (ví dụ ghế đã bị đặt), hệ thống lập tức dừng ngay bước đó và hiển thị thông báo lỗi chính xác cho người dùng (VD: *"Ghế A5 đã được người khác đặt"*).

---

### BƯỚC 5: STRATEGY & ADAPTER PATTERN — THANH TOÁN ĐA PHƯƠNG THỨC & TÍCH HỢP CỔNG NGOÀI

#### 🗣️ Lời thoại Thuyết trình & Thao tác Demo:
- **Thao tác:** Tại bước Chọn thanh toán, chuyển đổi giữa các Option:
  - **Tiền mặt tại rạp (Cash)**
  - **Ví điện tử MoMo**
  - **Cổng thanh toán VNPay / Thẻ quốc tế**
- **Lời thoại:** *"Hệ thống hỗ trợ nhiều hình thức thanh toán khác nhau. Nhóm kết hợp **Strategy Pattern** (để hoán đổi thuật toán thanh toán linh hoạt tại thời điểm Runtime) và **Adapter Pattern** (để bọc lại API của các bên thứ 3 như VNPay/MoMo về cùng 1 chuẩn giao tiếp của hệ thống)."*

#### 📺 Hiện tượng quan sát được:
Khi chọn VNPay/MoMo -> Hệ thống hiển thị QR Code hoặc chuyển hướng đến cổng thanh toán tương ứng. Khi chọn Tiền mặt -> Tạo đơn thành công với trạng thái *Chờ thanh toán tại rạp*.

---

### BƯỚC 6: DECORATOR PATTERN — TÍNH CHIẾT KHẤU, VOUCHER VÀ COMBO ĐƠN HÀNG

#### 🗣️ Lời thoại Thuyết trình & Thao tác Demo:
- **Thao tác:** Tại trang xác nhận đơn hàng:
  1. Nhập Mã Voucher giảm giá (VD: `DISCOUNT10` -> Giảm 10%).
  2. Tích chọn Đổi Điểm thưởng tích lũy (VD: Dùng 10 điểm -> Giảm 10.000đ).
  3. Chọn thêm Combo Bắp Nước (VD: Combo Bắp Phô Mai + 50.000đ).
- **Lời thoại:** *"Đơn hàng gốc có giá vé ban đầu. Tuy nhiên, đơn hàng có thể được áp dụng thêm Voucher, trừ điểm tích lũy, hoặc cộng thêm Combo bắp nước. Nhóm sử dụng **Decorator Pattern** để bọc thêm các tính năng tính tiền này vào đơn hàng một cách linh hoạt."*

#### 📺 Hiện tượng quan sát được:
Dòng Tổng tiền thanh toán được biến đổi động theo từng lớp (Layers): Giá gốc -> Bọc bởi Voucher -> Bọc bởi Trừ điểm -> Bọc bởi Combo Bắp Nước -> Trọng số Tổng thành tiền cuối cùng hoàn toàn chính xác.

---

### BƯỚC 7: BUILDER PATTERN — KHỞI TẠO ĐỐI TƯỢNG ĐƠN HÀNG (ORDER) PHỨC TẠP

#### 🗣️ Lời thoại Thuyết trình & Thao tác Demo:
- **Thao tác:** Thực hiện thao tác hoàn tất đặt vé.
- **Lời thoại:** *"Một đối tượng Đơn hàng (`Order`) trong hệ thống rạp chiếu phim có rất nhiều thông tin phức tạp: Thông tin khách hàng, Suất chiếu, Ghế ngồi, Mã giảm giá, Phương thức thanh toán, Tổng tiền, Điểm tích lũy... Nhóm dùng **Builder Pattern** để lắp ráp đối tượng Order theo từng bước rõ ràng, tránh rủi ro lầm lẫn tham số trong Constructor."*

#### 📺 Hiện tượng quan sát được:
Đơn hàng được khởi tạo thành công với đầy đủ các thuộc tính chính xác và lưu xuống Database an toàn mà không xảy ra lỗi Null Reference hay sai lệch vị trí tham số.

---

### BƯỚC 8: FACADE PATTERN — ĐƠN GIẢN HÓA TOÀN BỘ LUỒNG ĐẶT VÉ (BOOKING FACADE)

#### 🗣️ Lời thoại Thuyết trình & Thao tác Demo:
- **Thao tác:** Khách hàng nhấn nút **"Xác nhận Thanh toán & Đặt vé"**.
- **Lời thoại:** *"Thực tế khi người dùng bấm Đặt vé, Backend phải gọi 5-6 service khác nhau: `ShowtimesService`, `SeatsService`, `OrdersService`, `PaymentStrategy`, `OrderPipeline`... Để Controller không bị phình to và chằng chịt mã nguồn, nhóm dùng **Facade Pattern** làm giao diện đại diện duy nhất."*

#### 📺 Hiện tượng quan sát được:
Controller chỉ gọi đúng 1 hàm `_bookingFacade.ProcessBookingAsync(...)`, toàn bộ quy trình phức tạp được xử lý mượt mà và trả kết quả thành công chỉ trong vài mili-giây.

---

### BƯỚC 9: MEDIATOR PATTERN — ĐIỀU PHỐI GIAO TIẾP GIỮA CÁC SERVICES

#### 🗣️ Lời thoại Thuyết trình & Thao tác Demo:
- **Thao tác:** Báo cáo về kiến trúc luồng dữ liệu Backend giữa các Controller.
- **Lời thoại:** *"Để tránh các Controller phụ thuộc chéo lẫn nhau (ví dụ `OrdersController` phải inject 6-7 Interfaces khác nhau), nhóm áp dụng **Mediator Pattern**. `BookingMediator` đóng vai trò là Trạm điều phối trung tâm."*

#### 📺 Hiện tượng quan sát được:
Mã nguồn trong `OrdersController` cực kỳ gọn gàng, giảm 70% số lượng dòng code và độ phức tạp cyclomatic complexity.

---

### BƯỚC 10: STATE PATTERN — QUẢN LÝ VÒNG ĐỜI TRẠNG THÁI ĐƠN HÀNG

#### 🗣️ Lời thoại Thuyết trình & Thao tác Demo:
- **Thao tác:** Vào Quản lý đơn hàng (`/Orders/Index`), thực hiện chuyển trạng thái đơn từ **Chờ thanh toán (Pending)** -> **Đã thanh toán (Paid)** -> **Đã hủy vé (Cancelled)**. Thử nhấn nút Hủy đơn trên một Đơn hàng *Đã hoàn tất chiếu* -> Hệ thống chặn lại.
- **Lời thoại:** *"Đơn hàng có vòng đời chuyển đổi trạng thái nghiêm ngặt. Ví dụ: Đơn `Pending` mới được sang `Paid`, Đơn đã `Completed` thì không thể `Cancelled`. Nhóm sử dụng **State Pattern** để quản lý các quy tắc chuyển đổi trạng thái này."*

#### 📺 Hiện tượng quan sát được:
Nút thao tác trên UI tự động thay đổi theo Trạng thái đơn hàng. Nếu cố tình gửi request sai quy trình, State Machine sẽ từ chối và báo lỗi nghiệp vụ.

---

### BƯỚC 11: OBSERVER PATTERN — TỰ ĐỘNG GỬI EMAIL VÀ BẮN NOTIFICATION REAL-TIME

#### 🗣️ Lời thoại Thuyết trình & Thao tác Demo:
- **Thao tác:** Sau khi Đặt vé & Thanh toán thành công -> Mở Hòm thư Email giả lập hoặc xem Chuông thông báo góc phải màn hình Web.
- **Lời thoại:** *"Khi đơn hàng chuyển sang trạng thái Thanh toán thành công, hệ thống cần thực hiện nhiều tác vụ phụ trợ: 1. Gửi Email vé xem phim có Mã QR code. 2. Đẩy thông báo Real-time lên Web. 3. Tích điểm thưởng cho khách hàng. Nhóm áp dụng **Observer Pattern** để tự động hóa các tác vụ này mà không làm phình code xử lý thanh toán."*

#### 📺 Hiện tượng quan sát được:
Ngay lập tức trên UI xuất hiện thông báo toast rực rỡ: *"Vé của bạn đã thanh toán thành công! Mã vé #1024"*, đồng thời hệ thống gửi mail vé xem phim tự động.

---

## 🛠️ 3. GIẢI THÍCH MÃ NGUỒN CHUYÊN SÂU TỪNG DESIGN PATTERN (CODE DEEP DIVE)

---

### 3.1 Proxy Pattern (Tối ưu Cache Danh sách Phim)
📂 **Tệp nguồn:** [CachedMoviesServiceProxy.cs](file:///e:/Document/GianDuyKhanh-23DH111541/movieCinema/movieCinema/Data/Proxy/CachedMoviesServiceProxy.cs)

```csharp
public class CachedMoviesServiceProxy : IMoviesService
{
    private readonly MoviesService _realService; // Đối tượng Service thật kết nối EF Core DB
    private readonly IMemoryCache _cache;        // Bộ nhớ RAM (.NET MemoryCache)
    private static readonly TimeSpan DefaultExpiry = TimeSpan.FromMinutes(10); // Thời gian lưu cache

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

    // 2. Phương thức Ghi/Sửa dữ liệu có Invalidate Cache (Cache Invalidation)
    public async Task AddNewMovieAsync(NewMovieVM data)
    {
        await _realService.AddNewMovieAsync(data); // Lưu vào DB trước
        InvalidateAllCaches();                      // Xóa cache cũ để làm mới dữ liệu
    }
}
```

#### 🔍 Giải thích chi tiết Luồng hoạt động:
1. **Subject Interface (`IMoviesService`):** Cả Proxy và `MoviesService` đều cài đặt chung một Interface. Do đó, `MoviesController` chỉ inject `IMoviesService` mà không cần biết phía sau là Proxy hay Service thật.
2. **Cơ chế `GetOrCreateAsync`:** Khi gọi `GetNowShowingMoviesAsync()`, Proxy kiểm tra `key` trong RAM Cache:
   - **Cache Hit (Đã có dữ liệu):** Trả về danh sách `Movie` ngay lập tức mà không gọi SQL Server (~0ms).
   - **Cache Miss (Chưa có dữ liệu):** Thực thi lambda `async entry => ...`, gọi `_realService` xuống SQL Server lấy dữ liệu, lưu dữ liệu đó vào RAM Cache rồi mới trả về.
3. **Cơ chế Invalidate Cache:** Khi Admin thêm/sửa phim mới qua `AddNewMovieAsync()`, Proxy tự động xóa các keys cache cũ. Lần truy cập tiếp theo của khách hàng sẽ kích hoạt nạp lại dữ liệu mới từ DB.

---

### 3.2 Singleton Pattern (Giỏ hàng ShoppingCart theo Session)
📂 **Tệp nguồn:** [ShoppingCart.cs](file:///e:/Document/GianDuyKhanh-23DH111541/movieCinema/movieCinema/Data/Cart/ShoppingCart.cs) & [Program.cs](file:///e:/Document/GianDuyKhanh-23DH111541/movieCinema/movieCinema/Program.cs)

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

#### 🔍 Đăng ký DI trong `Program.cs`:
```csharp
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped(sc => ShoppingCart.GetShoppingCart(sc));
```

#### 🔍 Giải thích chi tiết:
- `ShoppingCart` sử dụng cơ chế **Scoped Singleton**: Mỗi người dùng (mỗi Session HTTP) sẽ có duy nhất 1 `CartId` và 1 đối tượng `ShoppingCart` trong suốt quá trình duyệt web.
- Việc đăng ký `AddScoped(sc => ShoppingCart.GetShoppingCart(sc))` giúp .NET DI Container tự động inject đúng đối tượng giỏ hàng hiện tại vào bất kỳ Controller hay ViewComponent nào yêu cầu.

---

### 3.3 Bridge Pattern (Tách biệt Loại ghế & Công thức Tính giá)
📂 **Tệp nguồn:** [SeatPricingBridge.cs](file:///e:/Document/GianDuyKhanh-23DH111541/movieCinema/movieCinema/Models/Bridge/SeatPricingBridge.cs)

```csharp
// 1. Interface cho chiến lược tính giá ghế
public interface ISeatPricingStrategy
{
    double CalculatePrice(double basePrice);
}

// 2. Các lớp thực thi thuật toán giá cụ thể
public class StandardSeatPricing : ISeatPricingStrategy {
    public double CalculatePrice(double basePrice) => basePrice; // Ghế thường = 100% giá gốc
}

public class VipSeatPricing : ISeatPricingStrategy {
    public double CalculatePrice(double basePrice) => basePrice * 1.2; // Ghế VIP phụ thu 20%
}

public class CoupleSeatPricing : ISeatPricingStrategy {
    public double CalculatePrice(double basePrice) => basePrice * 2.0; // Ghế đôi tính x2
}

// 3. Lớp Cầu nối (Bridge)
public class SeatPricingBridge
{
    private readonly ISeatPricingStrategy _strategy;

    public SeatPricingBridge(SeatType seatType)
    {
        // Khởi tạo thuật toán tính giá phù hợp dựa trên loại ghế
        _strategy = seatType switch
        {
            SeatType.VIP => new VipSeatPricing(),
            SeatType.Couple => new CoupleSeatPricing(),
            _ => new StandardSeatPricing()
        };
    }

    public double GetPrice(double basePrice) => _strategy.CalculatePrice(basePrice);
}
```

#### 🔍 Giải thích chi tiết:
- **Tách biệt Abstraction & Implementation:** `SeatType` (VIP, Couple, Standard) và `ISeatPricingStrategy` là 2 trục độc lập. Lớp `SeatPricingBridge` kết nối 2 trục này lại với nhau.
- **Tuân thủ SOLID:** Khi rạp chiếu phim ra mắt loại ghế mới (ví dụ: *Ghế Massage*), ta chỉ cần viết thêm class `MassageSeatPricing : ISeatPricingStrategy` mà không cần sửa đổi bất kỳ code tính giá hiện có nào ở Controller hay Service.

---

### 3.4 Chain of Responsibility (Pipeline Kiểm tra Điều kiện Đặt vé)
📂 **Tệp nguồn:** [OrderPipeline.cs](file:///e:/Document/GianDuyKhanh-23DH111541/movieCinema/movieCinema/Data/Chain/OrderPipeline.cs)

```csharp
// Handler trừu tượng trong Chuỗi
public abstract class OrderValidationHandler
{
    protected OrderValidationHandler? Next;

    public OrderValidationHandler SetNext(OrderValidationHandler next)
    {
        Next = next;
        return next; // Cho phép nối chuỗi dạng Fluent: A.SetNext(B).SetNext(C)
    }

    public abstract Task<ValidationResult> ValidateAsync(BookingContext context);
}

// Mắt xích 1: Kiểm tra ghế đã bị đặt chưa
public class SeatAvailabilityHandler : OrderValidationHandler
{
    public override async Task<ValidationResult> ValidateAsync(BookingContext context)
    {
        if (context.BookedSeats.Any(s => context.SelectedSeats.Contains(s)))
        {
            return new ValidationResult { IsValid = false, ErrorMessage = "Ghế chọn đã có người đặt!" };
        }

        // Chuyển tiếp cho Handler tiếp theo trong chuỗi nếu hợp lệ
        return Next != null ? await Next.ValidateAsync(context) : new ValidationResult { IsValid = true };
    }
}
```

#### 🔍 Giải thích chi tiết:
- **Cơ chế Pipeline:** Dữ liệu đặt vé (`BookingContext`) được truyền đi qua một chuỗi các mắt xích: `SeatAvailabilityHandler` ➔ `ShowtimeExpiryHandler` ➔ `VoucherValidationHandler`.
- Nếu bất kỳ mắt xích nào phát hiện dữ liệu không hợp lệ, nó sẽ ngắt chuỗi và trả về câu thông báo lỗi ngay lập tức mà không lãng phí tài nguyên xử lý các bước đằng sau.

---

### 3.5 Strategy & Adapter Pattern (Xử lý Thanh toán Đa cổng)
📂 **Tệp nguồn:** [PaymentStrategy.cs](file:///e:/Document/GianDuyKhanh-23DH111541/movieCinema/movieCinema/Data/Strategy/PaymentStrategy.cs)

```csharp
// Strategy Interface chung
public interface IPaymentStrategy
{
    Task<PaymentResult> PayAsync(double amount, string orderInfo);
}

// Strategy 1: Thanh toán Tiền mặt
public class CashPaymentStrategy : IPaymentStrategy
{
    public Task<PaymentResult> PayAsync(double amount, string orderInfo)
        => Task.FromResult(new PaymentResult { Success = true, Message = "Thanh toán tiền mặt thành công!" });
}

// Adapter Pattern: Bọc SDK MoMo bên ngoài cho phù hợp với IPaymentStrategy
public class MoMoPaymentAdapter : IPaymentStrategy
{
    private readonly ExternalMoMoLibrary _momoSdk = new(); // SDK thư viện ngoài

    public async Task<PaymentResult> PayAsync(double amount, string orderInfo)
    {
        // Chuyển đổi định dạng dữ liệu (Adapter)
        var response = await _momoSdk.CreatePaymentAsync(new MoMoRequest { Amount = (long)amount, OrderId = orderInfo });
        return new PaymentResult { Success = response.ErrorCode == 0, PayUrl = response.PayUrl };
    }
}

// Context điều khiển chọn Strategy linh hoạt
public class PaymentContext
{
    private IPaymentStrategy _strategy = new CashPaymentStrategy();

    public void SetStrategyByName(string method)
    {
        _strategy = method.ToLower() switch
        {
            "momo" => new MoMoPaymentAdapter(),
            "vnpay" => new VNPayPaymentAdapter(),
            _ => new CashPaymentStrategy()
        };
    }

    public Task<PaymentResult> PayAsync(double amount, string info) => _strategy.PayAsync(amount, info);
}
```

#### 🔍 Giải thích chi tiết:
- **Strategy Pattern:** Cho phép thay đổi thuật toán thanh toán linh hoạt tại thời điểm Runtime dựa trên lựa chọn của người dùng trên giao diện Web.
- **Adapter Pattern:** SDK MoMo / VNPay bên ngoài có giao diện hàm và tham số khác nhau. Lớp Adapter bọc lại các SDK này, ép chúng tuân theo chuẩn `IPaymentStrategy` của dự án.

---

### 3.6 Decorator Pattern (Tính Phụ phí, Combo & Chiết khấu Đơn hàng)
📂 **Tệp nguồn:** [PricingDecorators.cs](file:///e:/Document/GianDuyKhanh-23DH111541/movieCinema/movieCinema/Data/Decorators/PricingDecorators.cs)

```csharp
public interface IPricingComponent
{
    double GetTotal();
}

// Component Gốc: Giá vé cơ bản
public class BaseOrderPricing : IPricingComponent
{
    private readonly double _baseAmount;
    public BaseOrderPricing(double baseAmount) => _baseAmount = baseAmount;
    public double GetTotal() => _baseAmount;
}

// Decorator Trừu tượng
public abstract class PricingDecorator : IPricingComponent
{
    protected readonly IPricingComponent Component;
    protected PricingDecorator(IPricingComponent component) => Component = component;
    public virtual double GetTotal() => Component.GetTotal();
}

// Concrete Decorator 1: Trừ điểm tích lũy
public class PointsRedemptionDecorator : PricingDecorator
{
    private readonly int _points;
    public PointsRedemptionDecorator(IPricingComponent comp, int points) : base(comp) => _points = points;
    public override double GetTotal() => Math.Max(0, base.GetTotal() - (_points * 1000));
}

// Concrete Decorator 2: Phụ phí Combo Bắp Nước
public class PopcornComboDecorator : PricingDecorator
{
    private readonly double _comboPrice;
    public PopcornComboDecorator(IPricingComponent comp, double comboPrice) : base(comp) => _comboPrice = comboPrice;
    public override double GetTotal() => base.GetTotal() + _comboPrice;
}
```

#### 🔍 Cách bọc nhiều lớp Decorator lồng nhau:
```csharp
IPricingComponent pricing = new BaseOrderPricing(100000); // 100k giá vé gốc
pricing = new PopcornComboDecorator(pricing, 50000);        // +50k bắp nước = 150k
pricing = new PointsRedemptionDecorator(pricing, 10);       // -10k điểm thưởng = 140k
double finalPrice = pricing.GetTotal();                    // 140k
```
- **Ý nghĩa:** Decorator cho phép bọc thêm các quy tắc tính tiền lồng nhau một cách linh hoạt mà không cần sửa đổi đối tượng Đơn hàng gốc.

---

### 3.7 Builder Pattern (Tạo đối tượng Order phức tạp)
📂 **Tệp nguồn:** [OrderBuilder.cs](file:///e:/Document/GianDuyKhanh-23DH111541/movieCinema/movieCinema/Models/Builders/OrderBuilder.cs)

```csharp
public class OrderBuilder
{
    private readonly Order _order = new Order();

    public OrderBuilder SetCustomer(string name, string email, string userId)
    {
        _order.FullName = name;
        _order.Email = email;
        _order.UserId = userId;
        return this; // Trả về chính this để hỗ trợ Method Chaining
    }

    public OrderBuilder SetShowtime(int showtimeId, string seats, int count, double price)
    {
        _order.ShowtimeId = showtimeId;
        _order.SeatNumbers = seats;
        _order.Quantity = count;
        _order.BasePrice = price;
        return this;
    }

    public Order Build()
    {
        // Kiểm tra tính hợp lệ dữ liệu trước khi hoàn tất tạo Object
        if (string.IsNullOrEmpty(_order.Email))
            throw new InvalidOperationException("Email khách hàng không được để trống!");

        _order.OrderDate = DateTime.Now;
        return _order;
    }
}
```

#### 🔍 Giải thích chi tiết:
- Xây dựng đối tượng `Order` phức tạp có hơn 10 thuộc tính qua các bước rõ ràng. Giúp tránh sai sót thứ tự truyền tham số trong Constructor thông thường.

---

### 3.8 Facade Pattern (Đơn giản hóa toàn bộ luồng Đặt vé)
📂 **Tệp nguồn:** [BookingFacade.cs](file:///e:/Document/GianDuyKhanh-23DH111541/movieCinema/movieCinema/Data/Facade/BookingFacade.cs)

```csharp
public class BookingFacade : IBookingFacade
{
    private readonly AppDbContext _context;
    private readonly IShowtimesService _showtimesService;
    private readonly ISeatsService _seatsService;
    private readonly IOrdersService _ordersService;

    public async Task<BookingResult> ProcessBookingAsync(BookTicketsVM model, string? userId)
    {
        // 1. Lấy thông tin suất chiếu
        var showtime = await _showtimesService.GetShowtimeByIdWithDetailsAsync(model.ShowtimeId);
        
        // 2. Tính giá ghế bằng Bridge Pattern
        var roomSeats = await _seatsService.GetSeatsByRoomAsync(showtime.CinemaRoomId);
        // ...

        // 3. Xử lý thanh toán bằng Strategy Pattern
        var paymentCtx = new PaymentContext();
        paymentCtx.SetStrategyByName(model.PaymentMethod);
        var paymentResult = await paymentCtx.PayAsync(totalPrice, $"ORDER-{DateTime.Now.Ticks}");

        // 4. Tạo Order bằng Builder Pattern
        var order = new OrderBuilder().SetCustomer(...).SetShowtime(...).Build();

        // 5. Lưu vào Database
        await _ordersService.StoreDirectOrderAsync(...);

        return new BookingResult { Success = true, OrderId = savedOrder?.Id };
    }
}
```

---

### 3.9 Mediator Pattern (Điều phối giao tiếp giữa các Services)
📂 **Tệp nguồn:** [BookingMediator.cs](file:///e:/Document/GianDuyKhanh-23DH111541/movieCinema/movieCinema/Data/Mediator/BookingMediator.cs)

```csharp
public class BookingMediator : IBookingMediator
{
    private readonly IBookingFacade _bookingFacade;

    public BookingMediator(IBookingFacade bookingFacade)
    {
        _bookingFacade = bookingFacade;
    }

    public async Task<BookingResult> HandleBookingCommand(BookTicketsVM command, string userId)
    {
        // Điều phối lệnh từ Controller đến Facade xử lý
        return await _bookingFacade.ProcessBookingAsync(command, userId);
    }
}
```

---

### 3.10 State Pattern (Quản lý Vòng đời Trạng thái Đơn hàng)
📂 **Tệp nguồn:** [OrderStateMachine.cs](file:///e:/Document/GianDuyKhanh-23DH111541/movieCinema/movieCinema/Data/State/OrderStateMachine.cs)

```csharp
public interface IOrderState
{
    bool CanCancel();
    bool CanPay();
    void Pay(OrderContext context);
    void Cancel(OrderContext context);
}

// Trạng thái Pending: Cho phép Thanh toán hoặc Hủy
public class PendingOrderState : IOrderState
{
    public bool CanCancel() => true;
    public bool CanPay() => true;
    public void Pay(OrderContext context) => context.SetState(new PaidOrderState());
    public void Cancel(OrderContext context) => context.SetState(new CancelledOrderState());
}

// Trạng thái Completed: Không cho phép Hủy
public class CompletedOrderState : IOrderState
{
    public bool CanCancel() => false;
    public bool CanPay() => false;
    public void Cancel(OrderContext context) => throw new InvalidOperationException("Phim đã chiếu, không thể hủy vé!");
    public void Pay(OrderContext context) => throw new InvalidOperationException("Đơn hàng đã hoàn thành!");
}
```

---

### 3.11 Observer Pattern (Gửi Email & Báo Notification Real-time)
📂 **Tệp nguồn:** [OrderObserver.cs](file:///e:/Document/GianDuyKhanh-23DH111541/movieCinema/movieCinema/Data/Observer/OrderObserver.cs)

```csharp
public interface IOrderObserver
{
    Task OnOrderStatusChangedAsync(Order order, string oldStatus, string newStatus);
}

// Observer 1: Gửi Email Vé
public class EmailNotificationObserver : IOrderObserver
{
    public async Task OnOrderStatusChangedAsync(Order order, string oldStatus, string newStatus)
    {
        if (newStatus == "Paid")
            await SendTicketEmailAsync(order.Email, order);
    }
}

// Observer 2: Bắn Thông báo Real-time SignalR
public class SignalRNotificationObserver : IOrderObserver
{
    public async Task OnOrderStatusChangedAsync(Order order, string oldStatus, string newStatus)
    {
        await NotifyUserWebUIAsync(order.UserId, $"Đơn hàng #{order.Id} chuyển sang {newStatus}");
    }
}

// Subject quản lý các Observers
public class OrderSubject
{
    private readonly List<IOrderObserver> _observers = new();
    public void Attach(IOrderObserver observer) => _observers.Add(observer);

    public async Task NotifyAsync(Order order, string oldStatus, string newStatus)
    {
        foreach (var obs in _observers)
            await obs.OnOrderStatusChangedAsync(order, oldStatus, newStatus);
    }
}
```

---

## 🎯 4. TỔNG KẾT VÀ KINH NGHIỆM THUYẾT TRÌNH DEMO

1. **Chuẩn bị trước khi Demo:**
   - Mở sẵn dự án trong Visual Studio / VS Code và chạy ứng dụng `dotnet run`.
   - Đăng nhập tài khoản Test có sẵn điểm thưởng và voucher.
   - Mở sẵn các tệp code chính trong bảng tổng quan để chuyển tab nhanh khi Hội đồng hỏi code.

2. **Cấu trúc Lời nói chuẩn khi trả lời thắc mắc của Hội đồng:**
   - **Tên Pattern:** *"Dự án dùng pattern X tại chức năng Y..."*
   - **Lý do dùng (Why):** *"Nếu không dùng pattern X, code sẽ bị rải rác `if-else`, vi phạm nguyên lý SOLID (Single Responsibility / Open-Closed)..."*
   - **Cách triển khai (How):** *"Nhóm đã tạo Interface Z, tách các Concrete Class và inject qua DI Container tại tệp `Program.cs`..."*

---
*Chúc bạn có buổi demo thuyết trình thành công rực rỡ!* 🚀
