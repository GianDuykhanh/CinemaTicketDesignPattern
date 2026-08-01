using movieCinema.Data.Chain;
using movieCinema.Data.Facade;
using movieCinema.Data.Services;
using movieCinema.Data.ViewModels;
using Microsoft.Extensions.DependencyInjection;

namespace movieCinema.Data.Mediator
{
    // ── Mediator Base ──────────────────────────────────────────────────────
    public interface IMediator
    {
        Task<TResponse> SendAsync<TResponse>(IRequest<TResponse> request);
    }

    public interface IRequest<TResponse>
    {
    }

    public interface IRequestHandler<TRequest, TResponse>
        where TRequest : IRequest<TResponse>
    {
        Task<TResponse> HandleAsync(TRequest request);
    }

    // ── Requests ───────────────────────────────────────────────────────────
    public class CompleteBookingRequest : IRequest<CompleteBookingResponse>
    {
        public BookTicketsVM Model { get; set; } = null!;
        public string? UserId { get; set; }
    }

    public class CancelBookingRequest : IRequest<CancelBookingResponse>
    {
        public int OrderId { get; set; }
    }

    public class ConfirmBookingRequest : IRequest<ConfirmBookingResponse>
    {
        public int OrderId { get; set; }
    }

    // ── Responses ─────────────────────────────────────────────────────────
    public class CompleteBookingResponse
    {
        public bool Success { get; set; }
        public string Message { get; set; } = "";
        public int? OrderId { get; set; }
        public double FinalPrice { get; set; }
        public double DiscountApplied { get; set; }
        public List<string> AppliedDiscounts { get; set; } = new();
    }

    public class CancelBookingResponse
    {
        public bool Success { get; set; }
        public string Message { get; set; } = "";
    }

    public class ConfirmBookingResponse
    {
        public bool Success { get; set; }
        public string Message { get; set; } = "";
    }

    // ── Mediator Implementation ────────────────────────────────────────────
    public class AppMediator : IMediator
    {
        private readonly IServiceProvider _serviceProvider;

        public AppMediator(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        public async Task<TResponse> SendAsync<TResponse>(IRequest<TResponse> request)
        {
            var handlerType = typeof(IRequestHandler<,>)
                .MakeGenericType(request.GetType(), typeof(TResponse));

            // Tìm handler tương ứng với request type
            var handler = _serviceProvider.GetServices(handlerType)
                .FirstOrDefault();

            if (handler == null)
                throw new InvalidOperationException(
                    $"Handler not found for request type {request.GetType().Name}");

            var method = handlerType.GetMethod("HandleAsync");
            if (method == null)
                throw new InvalidOperationException($"HandleAsync not found on {handlerType.Name}");

            var result = method.Invoke(handler, new[] { request });
            if (result is Task<TResponse> task)
                return await task;

            throw new InvalidOperationException($"Handler for {request.GetType().Name} did not return Task<{typeof(TResponse).Name}>");
        }
    }

    // ── Handlers ──────────────────────────────────────────────────────────

    // Handler: CompleteBooking
    public class CompleteBookingHandler
        : IRequestHandler<CompleteBookingRequest, CompleteBookingResponse>
    {
        private readonly IBookingFacade _facade;
        private readonly IOrdersService _ordersService;

        public CompleteBookingHandler(
            IBookingFacade facade,
            IOrdersService ordersService)
        {
            _facade = facade;
            _ordersService = ordersService;
        }

        public async Task<CompleteBookingResponse> HandleAsync(CompleteBookingRequest request)
        {
            // 1. Validate qua Chain of Responsibility
            var pipeline = movieCinema.Data.Chain.OrderPipelineBuilder.Build(_ordersService);
            var pipelineResult = await pipeline.HandleAsync(
                new OrderPipelineRequest { Model = request.Model },
                new OrderPipelineResult { IsValid = true });

            if (!pipelineResult.IsValid)
            {
                return new CompleteBookingResponse
                {
                    Success = false,
                    Message = pipelineResult.Message
                };
            }

            // 2. Process booking qua Facade
            var bookingResult = await _facade.ProcessBookingAsync(request.Model, request.UserId);

            if (!bookingResult.Success)
            {
                return new CompleteBookingResponse
                {
                    Success = false,
                    Message = bookingResult.Message
                };
            }

            return new CompleteBookingResponse
            {
                Success = true,
                Message = "Đặt vé thành công!",
                OrderId = bookingResult.OrderId,
                FinalPrice = bookingResult.FinalPrice,
                DiscountApplied = bookingResult.DiscountApplied,
                AppliedDiscounts = pipelineResult.AppliedDiscounts
            };
        }
    }

    // Proxy để inject OrdersService vào Chain mà không cần thay đổi signature
    public class OrderPipelineHandlerProxy : OrderPipelineHandler
    {
        private readonly IOrdersService _ordersService;

        public OrderPipelineHandlerProxy(IOrdersService ordersService)
        {
            _ordersService = ordersService;
        }

        public override async Task<OrderPipelineResult> HandleAsync(
            OrderPipelineRequest request, OrderPipelineResult result)
        {
            var pipeline = Data.Chain.OrderPipelineBuilder.Build(_ordersService);
            return await pipeline.HandleAsync(request, result);
        }
    }

    // Handler: CancelBooking
    public class CancelBookingHandler
        : IRequestHandler<CancelBookingRequest, CancelBookingResponse>
    {
        private readonly IOrdersService _ordersService;

        public CancelBookingHandler(IOrdersService ordersService)
        {
            _ordersService = ordersService;
        }

        public async Task<CancelBookingResponse> HandleAsync(CancelBookingRequest request)
        {
            var result = await _ordersService.ChangeOrderStatusWithStateAsync(
                request.OrderId, "Cancelled");

            return new CancelBookingResponse
            {
                Success = result.Success,
                Message = result.Success
                    ? "Hủy đơn hàng thành công."
                    : result.Message
            };
        }
    }

    // Handler: ConfirmBooking
    public class ConfirmBookingHandler
        : IRequestHandler<ConfirmBookingRequest, ConfirmBookingResponse>
    {
        private readonly IOrdersService _ordersService;

        public ConfirmBookingHandler(IOrdersService ordersService)
        {
            _ordersService = ordersService;
        }

        public async Task<ConfirmBookingResponse> HandleAsync(ConfirmBookingRequest request)
        {
            var result = await _ordersService.ChangeOrderStatusWithStateAsync(
                request.OrderId, "Confirmed");

            return new ConfirmBookingResponse
            {
                Success = result.Success,
                Message = result.Success
                    ? "Xác nhận đơn hàng thành công."
                    : result.Message
            };
        }
    }
}
