using movieCinema.Data.Base;
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace movieCinema.Models
{
    public class Showtime : IEntityBase
    {
        [Key]
        public int Id { get; set; }

        [Display(Name = "Start Time")]
        [Required(ErrorMessage = "Start time is required")]
        public DateTime StartTime { get; set; }

        [Display(Name = "End Time")]
        public DateTime EndTime { get; set; }

        [Display(Name = "Price")]
        [Required(ErrorMessage = "Price is required")]
        [Range(0, 1000000, ErrorMessage = "Price must be positive")]
        public double Price { get; set; }

        [Display(Name = "Movie")]
        [Required(ErrorMessage = "Movie is required")]
        public int MovieId { get; set; }
        [ForeignKey("MovieId")]
        public Movie? Movie { get; set; }

        [Display(Name = "Screening Room")]
        [Required(ErrorMessage = "Cinema Room is required")]
        public int CinemaRoomId { get; set; }
        [ForeignKey("CinemaRoomId")]
        public CinemaRoom? CinemaRoom { get; set; }
    }
}
