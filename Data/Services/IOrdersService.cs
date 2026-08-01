using movieCinema.Models;

namespace movieCinema.Data.Services
{
    public interface IOrdersService
    {
        Task StoreOrderAsync(List<ShoppingCartItem> items, string userId, string userEmailAddress, double discountAmount = 0, int pointsRedeemed = 0, string paymentMethod = "PayPal");
        Task<List<Order>> GetOrdersByUserIdAsync(string userId);
        Task ClearAllOrdersAsync();
        Task StoreDirectOrderAsync(int showtimeId, string name, string email, string selectedSeats, int amount, double price, double discountAmount = 0, int pointsRedeemed = 0, string paymentMethod = "Cash", string? userId = null);
        Task<List<string>> GetBookedSeatsForShowtimeAsync(int showtimeId);
        Task<Voucher?> GetVoucherByCodeAsync(string code);
        Task<Member?> GetMemberByEmailAsync(string email);
        Task<List<Order>> GetOrdersByEmailAsync(string email);
        Task CancelOrderAsync(int orderId);
        Task ChangeOrderStatusAsync(int orderId, string status);
        Task<StatusChangeResult> ChangeOrderStatusWithStateAsync(int orderId, string newStatus);
    }
}

public class StatusChangeResult
{
    public bool Success { get; set; }
    public string Message { get; set; } = "";
    public string OldStatus { get; set; } = "";
    public string NewStatus { get; set; } = "";
}


