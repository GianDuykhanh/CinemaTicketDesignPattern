namespace movieCinema.Data.Strategy
{
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
            // Stub — thay bằng PayPal SDK thực tế khi triển khai production
            await Task.Delay(100); // giả lập API call
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

    public class PaymentContext
    {
        private IPaymentStrategy? _strategy;

        public void SetStrategy(IPaymentStrategy strategy) => _strategy = strategy;

        public void SetStrategyByName(string? name)
        {
            var method = name?.ToLower() ?? "cash";
            _strategy = method switch
            {
                "paypal" => new PayPalPaymentStrategy("CLIENT_ID", "CLIENT_SECRET"),
                _ => new CashPaymentStrategy()
            };
        }

        public async Task<PaymentResult> PayAsync(double amount, string orderId)
        {
            if (_strategy == null)
                throw new InvalidOperationException("Payment strategy not set. Call SetStrategy or SetStrategyByName first.");
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
}
