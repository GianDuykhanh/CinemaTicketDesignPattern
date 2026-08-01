namespace movieCinema.Models.Bridge
{
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

    public class SeatPricingBridge
    {
        private readonly ISeatingPricingStrategy _strategy;

        public SeatPricingBridge(ISeatingPricingStrategy strategy)
        {
            _strategy = strategy;
        }

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

        public double GetPrice(double basePrice) => _strategy.CalculatePrice(basePrice);
    }
}
