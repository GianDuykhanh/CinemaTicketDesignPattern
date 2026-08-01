using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace movieCinema.Models
{
    public class Order
    {
        [Key]
        public int Id { get; set; }

        public string Email { get; set; }

        public string UserId { get; set; }
        //[ForeignKey(nameof(UserId))]
        //public ApplicationUser User { get; set; }

        public List<OrderItem> OrderItems { get; set; }

        // New properties for Transaction History
        public DateTime OrderDate { get; set; } = DateTime.Now;
        public string Status { get; set; } = "Purchased"; // "Purchased", "Cancelled"
        public string PaymentMethod { get; set; } = "Cash"; // "Cash", "PayPal"
        public double TotalPrice { get; set; }
        public double DiscountAmount { get; set; }
        public int PointsRedeemed { get; set; }
    }
}
