using movieCinema.Data.Services;
using movieCinema.Data.ViewModels;

namespace movieCinema.Data.Chain
{
    // ── Request / Result ────────────────────────────────────────────────────
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

    // ── Base Handler ──────────────────────────────────────────────────────
    public abstract class OrderPipelineHandler
    {
        protected OrderPipelineHandler? _next;

        public OrderPipelineHandler SetNext(OrderPipelineHandler next)
        {
            _next = next;
            return next;
        }

        public abstract Task<OrderPipelineResult> HandleAsync(
            OrderPipelineRequest request,
            OrderPipelineResult result);
    }

    // ── Handler 1: Validation ─────────────────────────────────────────────
    public class ValidationHandler : OrderPipelineHandler
    {
        public override async Task<OrderPipelineResult> HandleAsync(
            OrderPipelineRequest request,
            OrderPipelineResult result)
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

            var seats = request.Model.SelectedSeats
                .Split(',').Select(s => s.Trim()).Where(s => !string.IsNullOrEmpty(s)).ToList();
            if (!seats.Any())
            {
                result.IsValid = false;
                result.Message = "Danh sách ghế trống.";
                return result;
            }

            if (seats.Count > 10)
            {
                result.IsValid = false;
                result.Message = "Không thể đặt quá 10 ghế mỗi lần.";
                return result;
            }

            return _next != null
                ? await _next.HandleAsync(request, result)
                : result;
        }
    }

    // ── Handler 2: Seat Availability ──────────────────────────────────────
    public class SeatAvailabilityHandler : OrderPipelineHandler
    {
        private readonly IOrdersService _ordersService;

        public SeatAvailabilityHandler(IOrdersService ordersService)
        {
            _ordersService = ordersService;
        }

        public override async Task<OrderPipelineResult> HandleAsync(
            OrderPipelineRequest request,
            OrderPipelineResult result)
        {
            if (!result.IsValid) return result;

            var bookedSeats = await _ordersService
                .GetBookedSeatsForShowtimeAsync(request.Model.ShowtimeId);

            var selectedSeats = request.Model.SelectedSeats
                .Split(',').Select(s => s.Trim()).Where(s => !string.IsNullOrEmpty(s)).ToList();

            foreach (var seat in selectedSeats)
            {
                if (bookedSeats.Contains(seat))
                {
                    result.IsValid = false;
                    result.Message = $"Ghế {seat} đã được đặt bởi người khác. Vui lòng chọn ghế khác.";
                    return result;
                }
            }

            return _next != null
                ? await _next.HandleAsync(request, result)
                : result;
        }
    }

    // ── Handler 3: Voucher Validation ─────────────────────────────────────
    public class VoucherValidationHandler : OrderPipelineHandler
    {
        private readonly IOrdersService _ordersService;

        public VoucherValidationHandler(IOrdersService ordersService)
        {
            _ordersService = ordersService;
        }

        public override async Task<OrderPipelineResult> HandleAsync(
            OrderPipelineRequest request,
            OrderPipelineResult result)
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

                if (!voucher.IsActive)
                {
                    result.IsValid = false;
                    result.Message = "Mã voucher đã bị vô hiệu hóa.";
                    return result;
                }

                if (voucher.ExpiryDate < DateTime.Now)
                {
                    result.IsValid = false;
                    result.Message = "Mã voucher đã hết hạn.";
                    return result;
                }

                result.AppliedDiscounts.Add(
                    voucher.IsPercentage
                        ? $"Voucher {voucher.Code}: {voucher.DiscountPercentage}% giảm"
                        : $"Voucher {voucher.Code}: {voucher.DiscountAmount:N0}đ giảm");
            }

            return _next != null
                ? await _next.HandleAsync(request, result)
                : result;
        }
    }

    // ── Handler 4: Member Validation ───────────────────────────────────────
    public class MemberValidationHandler : OrderPipelineHandler
    {
        private readonly IOrdersService _ordersService;

        public MemberValidationHandler(IOrdersService ordersService)
        {
            _ordersService = ordersService;
        }

        public override async Task<OrderPipelineResult> HandleAsync(
            OrderPipelineRequest request,
            OrderPipelineResult result)
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

                if (member == null)
                {
                    result.IsValid = false;
                    result.Message = "Email không phải là thành viên. Vui lòng đặt vé trước để tích điểm.";
                    return result;
                }

                if (member.Points < request.Model.PointsRedeemed)
                {
                    result.IsValid = false;
                    result.Message = $"Bạn chỉ có {member.Points} điểm. Không thể dùng {request.Model.PointsRedeemed} điểm.";
                    return result;
                }

                result.AppliedDiscounts.Add(
                    $"Điểm tích lũy: -{request.Model.PointsRedeemed * 1000:N0}đ ({request.Model.PointsRedeemed} điểm)");
            }

            return _next != null
                ? await _next.HandleAsync(request, result)
                : result;
        }
    }

    // ── Pipeline Builder ───────────────────────────────────────────────────
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
}
