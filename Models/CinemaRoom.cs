using movieCinema.Data.Base;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace movieCinema.Models
{
    public class CinemaRoom : IEntityBase
    {
        [Key]
        public int Id { get; set; }

        [Display(Name = "Room Name")]
        [Required(ErrorMessage = "Room name is required")]
        public string Name { get; set; }

        [Display(Name = "Seat Capacity")]
        [Required(ErrorMessage = "Capacity is required")]
        [Range(1, 1000, ErrorMessage = "Capacity must be between 1 and 1000")]
        public int Capacity { get; set; }

        // Relationship with Cinema
        [Display(Name = "Cinema")]
        [Required(ErrorMessage = "Cinema is required")]
        public int CinemaId { get; set; }

        [ForeignKey("CinemaId")]
        public Cinema? Cinema { get; set; }

        // Relationship with Movies
        public List<Movie>? Movies { get; set; }

        // Relationship with Seats
        public List<Seat>? Seats { get; set; }
    }
}
