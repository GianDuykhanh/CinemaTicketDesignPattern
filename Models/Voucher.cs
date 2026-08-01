using System;
using System.ComponentModel.DataAnnotations;
using movieCinema.Data.Base;

namespace movieCinema.Models
{
    public class Voucher : IEntityBase
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public string Code { get; set; }

        public double DiscountAmount { get; set; }

        public double DiscountPercentage { get; set; }

        public bool IsPercentage { get; set; }

        public double MinOrderAmount { get; set; }

        public DateTime ExpiryDate { get; set; }

        public bool IsActive { get; set; }
    }
}
