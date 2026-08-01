using System.ComponentModel.DataAnnotations;

namespace movieCinema.Data.ViewModels
{
    public class BookTicketsVM
    {
        [Required]
        public int ShowtimeId { get; set; }

        [Required(ErrorMessage = "Tên người đặt là bắt buộc")]
        [StringLength(50, ErrorMessage = "Tên không được quá 50 ký tự")]
        [Display(Name = "Tên người đặt")]
        public string Name { get; set; }

        [Required(ErrorMessage = "Email là bắt buộc")]
        [EmailAddress(ErrorMessage = "Email không đúng định dạng")]
        [Display(Name = "Email")]
        public string Email { get; set; }

        [Required(ErrorMessage = "Vui lòng chọn ít nhất một ghế ngồi")]
        [Display(Name = "Ghế đã chọn")]
        public string SelectedSeats { get; set; }

        public int SeatCount { get; set; }
        public double TotalPrice { get; set; }

        public double DiscountAmount { get; set; }
        public int PointsRedeemed { get; set; }
        public string? VoucherCode { get; set; }
        public string PaymentMethod { get; set; } = "direct";
    }
}
