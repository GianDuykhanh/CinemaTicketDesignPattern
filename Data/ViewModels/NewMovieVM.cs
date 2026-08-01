using movieCinema.Data.Base;
using MovieCinema.Data.Enums;
using MovieCinema.Models;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace movieCinema.Models
{
    public class NewMovieVM
    {
        public int Id { get; set; }
        [Display(Description = "Movie name")]
        [Required(ErrorMessage = "Name is required")]
        public string? Name { get; set; }
        [Display(Description = "Movie description")]
        [Required(ErrorMessage = "Description is required")]
        public string? Description { get; set; }
        [Display(Description = "Price in $")]
        [Required(ErrorMessage = "Price is required")]
        public double Price { get; set; }
        [Display(Description = "Movie poster")]
        public string? ImageURL { get; set; }

        [Display(Name = "Upload Poster")]
        [NotMapped]
        public Microsoft.AspNetCore.Http.IFormFile? ImageUpload { get; set; }
        [Display(Description = "Movie trailer")]
        public string? TrailerURL { get; set; }

        [Display(Name = "Upload Trailer")]
        [NotMapped]
        public Microsoft.AspNetCore.Http.IFormFile? TrailerUpload { get; set; }
        [Display(Description = "Movie duration")]
        [Required(ErrorMessage = "Movie duration is required")]
        public int Duration { get; set; }
        [Display(Description = "Movie start date")]
        [Required(ErrorMessage = "Start date is required")]
        public DateTime StartDate { get; set; }
        [Display(Description = "Movie end date")]
        [Required(ErrorMessage = "End date is required")]
        public DateTime EndDate { get; set; }
        [Display(Description = "Select a category")]
        [Required(ErrorMessage = "Movie category is required")]
        public int CategoryId { get; set; }
        [Display(Description = "Movie status")]
        [Required(ErrorMessage = "Movie status is required")]
        public MovieStatus Status { get; set; } = MovieStatus.ComingSoon;


        // Relationships
        [Display(Description = "Select actor(s)")]
        [Required(ErrorMessage = "Movie actor(s) is required")]
        public List<int> ActorIds { get; set; }

        // Cinema
        [Display(Description = "Movie a cinema")]
        [Required(ErrorMessage = "Movie cinema is required")]
        public int CinemaId { get; set; }

        // Producer
        [Display(Description = "Movie a producer")]
        [Required(ErrorMessage = "Movie producer is required")]
        public int ProducerId { get; set; }

        // CinemaRoom
        [Display(Name = "Select a screening room")]
        public int? CinemaRoomId { get; set; }
    }
}
