using movieCinema.Models;
using Microsoft.EntityFrameworkCore;
using MovieCinema.Data;

namespace movieCinema.Data.State
{
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
            // Mới đặt — có thể gửi email xác nhận ở đây
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
            // Đã xác nhận — sinh mã QR vé, gửi cho khách
            return Task.CompletedTask;
        }
    }

    public class CancelledState : IOrderState
    {
        public string StatusName => "Cancelled";

        public bool CanTransitionTo(string newStatus) => false; // terminal

        public async Task OnEnterAsync(Order order, AppDbContext context)
        {
            // Giải phóng ghế + hoàn điểm
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

    public class RefundedState : IOrderState
    {
        public string StatusName => "Refunded";

        public bool CanTransitionTo(string newStatus) => false; // terminal

        public async Task OnEnterAsync(Order order, AppDbContext context)
        {
            // Hoàn tiền + hoàn điểm
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

    public class OrderStateMachine
    {
        private static readonly Dictionary<string, IOrderState> _states = new()
        {
            ["Purchased"] = new PurchasedState(),
            ["Confirmed"] = new ConfirmedState(),
            ["Cancelled"] = new CancelledState(),
            ["Refunded"] = new RefundedState(),
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
