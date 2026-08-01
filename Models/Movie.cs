using movieCinema.Data.Base;
using MovieCinema.Data.Enums;
using MovieCinema.Models;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace movieCinema.Models
{
    public class Movie : IEntityBase
    {
        [Key]
        public int Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public double Price { get; set; }
        public string ImageURL { get; set; }
        public string TrailerURL { get; set; }
        public int Duration { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        [Display(Name = "Category")]
        public int CategoryId { get; set; }
        [ForeignKey("CategoryId")]
        public Category? Category { get; set; }

        public MovieStatus Status { get; set; } = MovieStatus.ComingSoon;


        // Relationships
        public List<Actor_Movie> Actors_Movies { get; set; } = new List<Actor_Movie>();
        public List<MovieReview> MovieReviews { get; set; } = new List<MovieReview>();

        // Cinema
        public int CinemaId { get; set; }
        [ForeignKey("CinemaId")]
        public Cinema? Cinema { get; set; }

        // Producer
        public int ProducerId { get; set; }
        [ForeignKey("ProducerId")]
        public Producer? Producer { get; set; }

        // CinemaRoom
        [Display(Name = "Screening Room")]
        public int? CinemaRoomId { get; set; }
        [ForeignKey("CinemaRoomId")]
        public CinemaRoom? CinemaRoom { get; set; }
    }
}
