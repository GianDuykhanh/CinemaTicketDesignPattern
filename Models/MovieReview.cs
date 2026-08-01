using movieCinema.Data.Base;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace movieCinema.Models
{
    public class MovieReview : IEntityBase
    {
        [Key]
        public int Id { get; set; }

        [Required(ErrorMessage = "Tên người đánh giá là bắt buộc")]
        [StringLength(50, ErrorMessage = "Tên không được quá 50 ký tự")]
        [Display(Name = "Tên người đánh giá")]
        public string Name { get; set; }

        [Required(ErrorMessage = "Email là bắt buộc")]
        [EmailAddress(ErrorMessage = "Email không đúng định dạng")]
        [Display(Name = "Email")]
        public string Email { get; set; }

        [Required(ErrorMessage = "Điểm đánh giá là bắt buộc")]
        [Range(1, 5, ErrorMessage = "Điểm đánh giá phải từ 1 đến 5 sao")]
        [Display(Name = "Đánh giá")]
        public int Rating { get; set; }

        [Required(ErrorMessage = "Nội dung nhận xét là bắt buộc")]
        [StringLength(1000, ErrorMessage = "Nội dung nhận xét không được quá 1000 ký tự")]
        [Display(Name = "Nhận xét")]
        public string Comment { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        // Relationship
        public int MovieId { get; set; }
        [ForeignKey("MovieId")]
        public Movie? Movie { get; set; }
    }
}
