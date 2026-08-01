using Microsoft.EntityFrameworkCore;
using movieCinema.Models;
using MovieCinema.Data;

namespace movieCinema.Data.Services
{
    public class OrdersService : IOrdersService
    {
        private readonly AppDbContext _context;
        public OrdersService(AppDbContext context)
        {
            _context = context;
        }
        public async Task<List<Order>> GetOrdersByUserIdAsync(string userId)
        {
            var query = _context.Orders
                .Include(n => n.OrderItems)
                    .ThenInclude(n => n.Showtime)
                        .ThenInclude(s => s.Movie)
                .Include(n => n.OrderItems)
                    .ThenInclude(n => n.Showtime)
                        .ThenInclude(s => s.CinemaRoom)
                            .ThenInclude(cr => cr.Cinema)
                .AsQueryable();

            if (!string.IsNullOrEmpty(userId))
            {
                query = query.Where(n => n.UserId == userId);
            }

            return await query.ToListAsync();
        }

        public async Task StoreOrderAsync(List<ShoppingCartItem> items, string userId, string userEmailAddress, double discountAmount = 0, int pointsRedeemed = 0, string paymentMethod = "PayPal")
        {
            double totalCartPrice = items.Sum(item => item.Showtime.Price * item.Amount);
            double finalPrice = totalCartPrice - discountAmount;
            if (finalPrice < 0) finalPrice = 0;

            var order = new Order()
            {
                UserId = userId,
                Email = userEmailAddress,
                OrderDate = DateTime.Now,
                Status = "Purchased",
                PaymentMethod = paymentMethod,
                TotalPrice = totalCartPrice,
                DiscountAmount = discountAmount,
                PointsRedeemed = pointsRedeemed
            };
            // Đảm bảo nếu UserId trống nhưng có Email thì dùng Email làm khóa tra cứu
            if (string.IsNullOrEmpty(order.UserId) && !string.IsNullOrEmpty(order.Email))
                order.UserId = order.Email;
            await _context.Orders.AddAsync(order);
            await _context.SaveChangesAsync();

            foreach (var item in items)
            {
                double itemTotal = item.Showtime.Price * item.Amount;
                double itemProportion = totalCartPrice > 0 ? (itemTotal / totalCartPrice) : 0;
                double itemDiscount = discountAmount * itemProportion;
                double finalItemPrice = itemTotal - itemDiscount;
                if (finalItemPrice < 0) finalItemPrice = 0;

                var orderItem = new OrderItem()
                {
                    Amount = item.Amount,
                    ShowtimeId = item.Showtime.Id,
                    OrderId = order.Id,
                    Price = finalItemPrice / item.Amount
                };
                await _context.OrdersItems.AddAsync(orderItem);
            }
            await _context.SaveChangesAsync();

            if (!string.IsNullOrEmpty(userEmailAddress))
            {
                var member = await _context.Members.FirstOrDefaultAsync(m => m.Email.ToLower() == userEmailAddress.ToLower());
                if (member == null)
                {
                    member = new Member()
                    {
                        Email = userEmailAddress.ToLower(),
                        Name = userId ?? "Member",
                        Points = 0
                    };
                    await _context.Members.AddAsync(member);
                }

                if (pointsRedeemed > 0)
                {
                    member.Points = Math.Max(0, member.Points - pointsRedeemed);
                }

                int pointsEarned = (int)(finalPrice / 10000);
                member.Points += pointsEarned;

                await _context.SaveChangesAsync();
            }
        }

        public async Task ClearAllOrdersAsync()
        {
            var orderItems = await _context.OrdersItems.ToListAsync();
            _context.OrdersItems.RemoveRange(orderItems);

            var orders = await _context.Orders.ToListAsync();
            _context.Orders.RemoveRange(orders);

            await _context.SaveChangesAsync();
        }

        public async Task<List<string>> GetBookedSeatsForShowtimeAsync(int showtimeId)
        {
            var bookedSeats = await _context.OrdersItems
                .Include(oi => oi.Order)
                .Where(oi => oi.ShowtimeId == showtimeId && oi.Order.Status != "Cancelled" && oi.Order.Status != "Refunded" && !string.IsNullOrEmpty(oi.SelectedSeats))
                .Select(oi => oi.SelectedSeats)
                .ToListAsync();

            var list = new List<string>();
            foreach (var seatStr in bookedSeats)
            {
                if (!string.IsNullOrEmpty(seatStr))
                {
                    list.AddRange(seatStr.Split(',').Select(s => s.Trim()));
                }
            }
            return list.Distinct().ToList();
        }

        public async Task StoreDirectOrderAsync(int showtimeId, string name, string email, string selectedSeats, int amount, double price, double discountAmount = 0, int pointsRedeemed = 0, string paymentMethod = "Cash", string? userId = null)
        {
            var finalPrice = price - discountAmount;
            if (finalPrice < 0) finalPrice = 0;

            var order = new Order()
            {
                UserId = userId ?? email ?? name ?? "Guest",
                Email = email,
                OrderDate = DateTime.Now,
                Status = "Purchased",
                PaymentMethod = paymentMethod,
                TotalPrice = price,
                DiscountAmount = discountAmount,
                PointsRedeemed = pointsRedeemed
            };
            await _context.Orders.AddAsync(order);
            await _context.SaveChangesAsync();

            var orderItem = new OrderItem()
            {
                Amount = amount,
                ShowtimeId = showtimeId,
                OrderId = order.Id,
                Price = finalPrice / (amount > 0 ? amount : 1),
                SelectedSeats = selectedSeats
            };
            await _context.OrdersItems.AddAsync(orderItem);
            await _context.SaveChangesAsync();

            var member = await _context.Members.FirstOrDefaultAsync(m => m.Email.ToLower() == email.ToLower());
            if (member == null)
            {
                member = new Member()
                {
                    Email = email.ToLower(),
                    Name = name,
                    Points = 0
                };
                await _context.Members.AddAsync(member);
            }

            if (pointsRedeemed > 0)
            {
                member.Points = Math.Max(0, member.Points - pointsRedeemed);
            }

            int pointsEarned = (int)(finalPrice / 10000);
            member.Points += pointsEarned;

            await _context.SaveChangesAsync();
        }

        public async Task<Voucher?> GetVoucherByCodeAsync(string code)
        {
            return await _context.Vouchers.FirstOrDefaultAsync(v => v.Code.ToUpper() == code.ToUpper() && v.IsActive && v.ExpiryDate >= DateTime.Now);
        }

        public async Task<Member?> GetMemberByEmailAsync(string email)
        {
            return await _context.Members.FirstOrDefaultAsync(m => m.Email.ToLower() == email.ToLower());
        }

        public async Task<List<Order>> GetOrdersByEmailAsync(string email)
        {
            if (string.IsNullOrEmpty(email)) return new List<Order>();
            var lowerEmail = email.ToLower();
            return await _context.Orders
                .Include(n => n.OrderItems)
                    .ThenInclude(n => n.Showtime)
                        .ThenInclude(s => s.Movie)
                .Include(n => n.OrderItems)
                    .ThenInclude(n => n.Showtime)
                        .ThenInclude(s => s.CinemaRoom)
                .Where(n => (!string.IsNullOrEmpty(n.Email) && n.Email.ToLower() == lowerEmail)
                         || (!string.IsNullOrEmpty(n.UserId) && n.UserId.ToLower() == lowerEmail))
                .OrderByDescending(n => n.OrderDate)
                .ToListAsync();
        }

        public async Task CancelOrderAsync(int orderId)
        {
            await ChangeOrderStatusAsync(orderId, "Cancelled");
        }

        public async Task ChangeOrderStatusAsync(int orderId, string status)
        {
            await ChangeOrderStatusWithStateAsync(orderId, status);
        }

        public async Task<StatusChangeResult> ChangeOrderStatusWithStateAsync(int orderId, string newStatus)
        {
            var order = await _context.Orders
                .Include(o => o.OrderItems)
                .FirstOrDefaultAsync(o => o.Id == orderId);

            if (order == null)
                return new StatusChangeResult { Success = false, Message = "Đơn hàng không tồn tại." };

            string oldStatus = order.Status;
            if (oldStatus == newStatus)
                return new StatusChangeResult
                {
                    Success = false,
                    Message = $"Đơn hàng đã ở trạng thái [{newStatus}].",
                    OldStatus = oldStatus,
                    NewStatus = newStatus
                };

            // State pattern — kiểm tra transition hợp lệ
            if (!movieCinema.Data.State.OrderStateMachine.CanTransition(oldStatus, newStatus))
                return new StatusChangeResult
                {
                    Success = false,
                    Message = $"Không thể chuyển từ [{oldStatus}] sang [{newStatus}].",
                    OldStatus = oldStatus,
                    NewStatus = newStatus
                };

            order.Status = newStatus;

            // State pattern — gọi OnEnter
            var state = movieCinema.Data.State.OrderStateMachine.GetState(newStatus);
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
    }
}

