using movieCinema.Data.Base;
using System.ComponentModel.DataAnnotations;

namespace movieCinema.Models
{
    public class Cinema : IEntityBase
    {
        [Key]
        public int Id { get; set; }
        [Display(Name = "CinemaLogo")]
        [Required(ErrorMessage = "Cinema logo is required")]
        public string Logo { get; set; }
        [Display(Name = "Cinema Name")]
        [Required(ErrorMessage = "Cinema name is required")]
        public string Name { get; set; }
        [Display(Name = "Description")]
        [Required(ErrorMessage = "Cinema description is required")]
        public string Description { get; set; }

        // Relationships
        public List<Movie> Movies { get; set; } = new List<Movie>();
        public List<CinemaRoom> CinemaRooms { get; set; } = new List<CinemaRoom>();
    }
}
