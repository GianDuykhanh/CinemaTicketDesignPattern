using movieCinema.Models;
using Microsoft.EntityFrameworkCore;
using MovieCinema.Data;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace movieCinema.Data.Observer
{
    // ── Observer Interface ─────────────────────────────────────────────────
    public interface IOrderObserver
    {
        Task OnOrderStatusChangedAsync(Order order, string oldStatus, string newStatus);
    }

    // ── Subject: Quản lý danh sách observers ────────────────────────────────
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
        private bool _initialized;
        private readonly object _lock = new();

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
                // Merge scoped observers into existing list
                foreach (var obs in scopedObservers)
                    if (!_observers.Contains(obs))
                        _observers.Add(obs);
                _initialized = true;
            }

            return _observers.ToList();
        }

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
                    // Log lỗi nhưng không ngăn observers khác
                }
            }
        }
    }

    // ── Observer 1: Audit Log ──────────────────────────────────────────────
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

    // ── Observer 2: Loyalty Points ─────────────────────────────────────────
    public class LoyaltyPointsObserver : IOrderObserver
    {
        private readonly IServiceScopeFactory _scopeFactory;

        public LoyaltyPointsObserver(IServiceScopeFactory scopeFactory)
        {
            _scopeFactory = scopeFactory;
        }

        public async Task OnOrderStatusChangedAsync(Order order, string oldStatus, string newStatus)
        {
            if (string.IsNullOrEmpty(order.Email))
                return;

            using var scope = _scopeFactory.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            var member = await context.Members
                .FirstOrDefaultAsync(m => m.Email.ToLower() == order.Email.ToLower());

            if (member == null)
                return;

            double finalPrice = Math.Max(0, order.TotalPrice - order.DiscountAmount);
            int earned = (int)(finalPrice / 10000);

            if (newStatus == "Cancelled" || newStatus == "Refunded")
            {
                // Hoàn lại: trừ điểm đã tích - cộng lại điểm đã dùng
                member.Points = Math.Max(0, member.Points - earned + (order.PointsRedeemed / 1000));
            }
            else if (newStatus == "Confirmed" && (oldStatus == "Purchased"))
            {
                // Mới xác nhận → cộng điểm
                member.Points += earned;
            }

            await context.SaveChangesAsync();
        }
    }

    // ── Observer 3: Email Notification ────────────────────────────────────
    public class EmailNotificationObserver : IOrderObserver
    {
        private readonly ILogger<EmailNotificationObserver> _logger;

        public EmailNotificationObserver(ILogger<EmailNotificationObserver> logger)
        {
            _logger = logger;
        }

        public Task OnOrderStatusChangedAsync(Order order, string oldStatus, string newStatus)
        {
            if (string.IsNullOrEmpty(order.Email))
                return Task.CompletedTask;

            var (subject, body) = newStatus switch
            {
                "Confirmed" => (
                    $"[MovieCinema] Xac nhan don hang #{order.Id}",
                    $"Xin chao {order.UserId ?? "quy khach"},\n\n" +
                    $"Don hang #{order.Id} da duoc xac nhan thanh cong.\n" +
                    $"Tong cong: {(order.TotalPrice - order.DiscountAmount):N0}VND\n" +
                    $"Phuong thuc: {order.PaymentMethod}\n\n" +
                    "Cam on ban da su dung dich vu MovieCinema!"
                ),
                "Cancelled" => (
                    $"[MovieCinema] Don hang #{order.Id} da bi huy",
                    $"Xin chao {order.UserId ?? "quy khach"},\n\n" +
                    $"Don hang #{order.Id} da duoc huy.\n" +
                    "Neu da thanh toan, tien se duoc hoan trong 3-5 ngay lam viec."
                ),
                "Refunded" => (
                    $"[MovieCinema] Hoan tien don hang #{order.Id}",
                    $"Xin chao {order.UserId ?? "quy khach"},\n\n" +
                    $"Don hang #{order.Id} da duoc hoan tien.\n" +
                    $"So tien hoan: {(order.TotalPrice - order.DiscountAmount):N0}VND\n" +
                    "Cam on ban da su dung dich vu MovieCinema!"
                ),
                _ => (null as string, null as string)
            };

            if (subject != null)
            {
                // Stub: thay bang IEmailService thuc te (SendGrid, SMTP, ...)
                _logger.LogInformation(
                    "[EMAIL] To: {Email} | Subject: {Subject}",
                    order.Email, subject);
            }

            return Task.CompletedTask;
        }
    }
}
