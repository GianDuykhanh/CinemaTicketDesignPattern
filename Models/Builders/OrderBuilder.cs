using movieCinema.Models;

namespace movieCinema.Models.Builders
{
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
            _order.UserId = name; // dùng name làm mock userId (theo convention cũ)
            return this;
        }

        public IOrderBuilder SetShowtime(int showtimeId, string selectedSeats, int seatCount, double basePrice)
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
            // 1 point = 1,000 VND
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
            _finalTotal = _subtotal - _order.DiscountAmount - _order.PointsRedeemed;
            if (_finalTotal < 0) _finalTotal = 0;
            _order.TotalPrice = _finalTotal;
            return this;
        }

        public Order Build() => _order;
    }
}
