using Microsoft.EntityFrameworkCore;
using movieCinema.Data.Services;
using movieCinema.Data.Strategy;
using movieCinema.Data.ViewModels;
using movieCinema.Models;
using movieCinema.Models.Bridge;
using movieCinema.Models.Builders;
using MovieCinema.Data;

namespace movieCinema.Data.Facade
{
    public interface IBookingFacade
    {
        Task<BookingResult> ProcessBookingAsync(BookTicketsVM model, string? userId);
        Task<SeatPricingResult> CalculateSeatPricesAsync(int showtimeId, List<string> seatCodes);
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

    public class SeatPricingResult
    {
        public double TotalPrice { get; set; }
        public Dictionary<string, double> SeatPrices { get; set; } = new();
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

        public async Task<BookingResult> ProcessBookingAsync(BookTicketsVM model, string? userId)
        {
            // 1. Validate ModelState
            if (string.IsNullOrEmpty(model.SelectedSeats))
                return new BookingResult { Success = false, Message = "Vui lòng chọn ít nhất một ghế." };

            // 2. Lấy Showtime
            var showtime = await _showtimesService.GetShowtimeByIdWithDetailsAsync(model.ShowtimeId);
            if (showtime == null)
                return new BookingResult { Success = false, Message = "Suất chiếu không tồn tại." };

            // 3. Parse ghế
            var selectedSeats = model.SelectedSeats
                .Split(',').Select(s => s.Trim()).Where(s => !string.IsNullOrEmpty(s)).ToList();
            if (!selectedSeats.Any())
                return new BookingResult { Success = false, Message = "Vui lòng chọn ít nhất một ghế." };

            // 4. Check ghế đã bị đặt chưa
            var bookedSeats = await _ordersService.GetBookedSeatsForShowtimeAsync(model.ShowtimeId);
            foreach (var seat in selectedSeats)
                if (bookedSeats.Contains(seat))
                    return new BookingResult { Success = false, Message = $"Ghế {seat} đã được đặt bởi người khác." };

            // 5. Tính giá theo loại ghế (Bridge pattern)
            var roomSeats = await _seatsService.GetSeatsByRoomAsync(showtime.CinemaRoomId);
            double totalPrice = 0;
            foreach (var seatCode in selectedSeats)
            {
                var seat = roomSeats.FirstOrDefault(s => s.Row + s.Number.ToString() == seatCode);
                var bridge = new SeatPricingBridge(seat?.SeatType ?? SeatType.Standard);
                totalPrice += bridge.GetPrice(showtime.Price);
            }

            // 6. Áp dụng voucher (Strategy)
            double discount = 0;
            if (!string.IsNullOrEmpty(model.VoucherCode))
            {
                var voucher = await _ordersService.GetVoucherByCodeAsync(model.VoucherCode);
                if (voucher != null && totalPrice >= voucher.MinOrderAmount)
                {
                    discount = voucher.IsPercentage
                        ? totalPrice * voucher.DiscountPercentage / 100.0
                        : voucher.DiscountAmount;
                }
            }

            // 7. Thanh toán (Strategy)
            var paymentCtx = new PaymentContext();
            paymentCtx.SetStrategyByName(model.PaymentMethod);
            var paymentResult = await paymentCtx.PayAsync(totalPrice, $"ORDER-{DateTime.Now.Ticks}");

            if (!paymentResult.Success)
                return new BookingResult { Success = false, Message = $"Thanh toán thất bại: {paymentResult.Message}" };

            // 8. Tạo Order (Builder)
            var order = new OrderBuilder()
                .SetCustomer(model.Name ?? "Guest", model.Email ?? "", userId ?? "")
                .SetShowtime(model.ShowtimeId, model.SelectedSeats, selectedSeats.Count, showtime.Price)
                .ApplyVoucher(discount, totalPrice)
                .RedeemPoints(model.PointsRedeemed, totalPrice - discount)
                .SetPaymentMethod(paymentCtx.CurrentPaymentMethod)
                .CalculateTotal()
                .Build();

            // 9. Lưu vào DB
            await _ordersService.StoreDirectOrderAsync(
                model.ShowtimeId,
                model.Name ?? "Guest",
                model.Email ?? "",
                model.SelectedSeats,
                selectedSeats.Count,
                totalPrice,
                discount,
                model.PointsRedeemed,
                paymentCtx.CurrentPaymentMethod,
                userId);

            var savedOrder = await _context.Orders
                .OrderByDescending(o => o.Id).FirstOrDefaultAsync();

            double finalPrice = totalPrice - discount - (model.PointsRedeemed * 1000);
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

        public async Task<SeatPricingResult> CalculateSeatPricesAsync(int showtimeId, List<string> seatCodes)
        {
            var showtime = await _showtimesService.GetShowtimeByIdWithDetailsAsync(showtimeId);
            if (showtime == null)
                return new SeatPricingResult();

            var roomSeats = await _seatsService.GetSeatsByRoomAsync(showtime.CinemaRoomId);
            var result = new SeatPricingResult { TotalPrice = 0 };

            foreach (var seatCode in seatCodes)
            {
                var seat = roomSeats.FirstOrDefault(s => s.Row + s.Number.ToString() == seatCode);
                var bridge = new SeatPricingBridge(seat?.SeatType ?? SeatType.Standard);
                var price = bridge.GetPrice(showtime.Price);
                result.SeatPrices[seatCode] = price;
                result.TotalPrice += price;
            }

            return result;
        }
    }
}
