using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace movieCinema.Models
{
    public class ShoppingCartItem
    {
        [Key]
        public int Id { get; set; }

        public int ShowtimeId { get; set; }
        [ForeignKey("ShowtimeId")]
        public Showtime Showtime { get; set; }

        public int Amount { get; set; }

        public string ShoppingCartId { get; set; }
    }
}