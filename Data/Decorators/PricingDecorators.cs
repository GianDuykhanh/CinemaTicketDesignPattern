using movieCinema.Models;

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
        public string Description => $"Giá gốc";
        public int Priority => 0;
    }

    // ── Voucher Decorator ───────────────────────────────────────────────────
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

            return Math.Max(0, discounted - Math.Min(reduction, discounted));
        }

        public string Description => _voucher.IsPercentage
            ? $"Voucher giảm {_voucher.DiscountPercentage}% (-{_voucher.Code})"
            : $"Voucher giảm {_voucher.DiscountAmount:N0}đ (-{_voucher.Code})";

        public int Priority => 1;
    }

    // ── Loyalty Points Decorator ───────────────────────────────────────────
    public class LoyaltyPointsDecorator : IOrderPriceDecorator
    {
        private readonly IOrderPriceDecorator _inner;
        private readonly int _points;

        public LoyaltyPointsDecorator(IOrderPriceDecorator inner, int points)
        {
            _inner = inner;
            _points = points;
        }

        public double CalculatePrice(double currentPrice)
        {
            double afterVoucher = _inner.CalculatePrice(currentPrice);
            double pointValue = _points * 1000.0; // 1 point = 1,000 VND
            return Math.Max(0, afterVoucher - pointValue);
        }

        public string Description => $"Điểm tích lũy (-{_points * 1000:N0}đ = {_points} điểm)";

        public int Priority => 2;
    }

    // ── Happy Hour Decorator ─────────────────────────────────────────────────
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
                double basePrice = _inner.CalculatePrice(currentPrice);
                return basePrice * (1 - _discountPercent / 100.0);
            }
            return _inner.CalculatePrice(currentPrice);
        }

        public string Description
            => $"Happy Hour {_discountPercent}% (từ {_start:hh\\:mm}–{_end:hh\\:mm})";

        public int Priority => 3;
    }

    // ── Composite Decorator Calculator ──────────────────────────────────────
    public class OrderPriceCalculator
    {
        public PriceCalculationResult Calculate(
            double basePrice,
            Voucher? voucher,
            int loyaltyPoints,
            bool applyHappyHour)
        {
            IOrderPriceDecorator calc = new BasePriceCalculator(basePrice);

            if (voucher != null)
                calc = new VoucherDecorator(calc, voucher);

            if (loyaltyPoints > 0)
                calc = new LoyaltyPointsDecorator(calc, loyaltyPoints);

            if (applyHappyHour)
                calc = new HappyHourDecorator(calc,
                    new TimeSpan(14, 0, 0),
                    new TimeSpan(17, 0, 0),
                    15.0); // Giảm 15% từ 14:00–17:00

            double finalPrice = calc.CalculatePrice(basePrice);
            var breakdown = BuildBreakdown(basePrice, voucher, loyaltyPoints, applyHappyHour, finalPrice);

            return new PriceCalculationResult
            {
                OriginalPrice = basePrice,
                FinalPrice = finalPrice,
                DiscountApplied = basePrice - finalPrice,
                Description = calc.Description,
                Breakdown = breakdown
            };
        }

        private List<PriceBreakdownItem> BuildBreakdown(
            double basePrice,
            Voucher? voucher,
            int loyaltyPoints,
            bool applyHappyHour,
            double finalPrice)
        {
            var items = new List<PriceBreakdownItem>
            {
                new() { Label = "Giá vé gốc", Amount = basePrice, IsDiscount = false }
            };

            if (voucher != null)
            {
                var reduction = voucher.IsPercentage
                    ? basePrice * voucher.DiscountPercentage / 100.0
                    : voucher.DiscountAmount;
                items.Add(new PriceBreakdownItem
                {
                    Label = $"Voucher {voucher.Code}",
                    Amount = -reduction,
                    IsDiscount = true
                });
            }

            if (loyaltyPoints > 0)
            {
                items.Add(new PriceBreakdownItem
                {
                    Label = $"Điểm tích lũy ({loyaltyPoints} điểm)",
                    Amount = -(loyaltyPoints * 1000.0),
                    IsDiscount = true
                });
            }

            if (applyHappyHour && IsHappyHour())
            {
                items.Add(new PriceBreakdownItem
                {
                    Label = "Happy Hour (14:00–17:00)",
                    Amount = -(basePrice * 0.15),
                    IsDiscount = true
                });
            }

            return items;
        }

        private bool IsHappyHour()
        {
            var now = DateTime.Now.TimeOfDay;
            return now >= new TimeSpan(14, 0, 0) && now <= new TimeSpan(17, 0, 0);
        }
    }

    public class PriceCalculationResult
    {
        public double OriginalPrice { get; set; }
        public double FinalPrice { get; set; }
        public double DiscountApplied { get; set; }
        public string Description { get; set; } = "";
        public List<PriceBreakdownItem> Breakdown { get; set; } = new();
    }

    public class PriceBreakdownItem
    {
        public string Label { get; set; } = "";
        public double Amount { get; set; }
        public bool IsDiscount { get; set; }
    }
}
