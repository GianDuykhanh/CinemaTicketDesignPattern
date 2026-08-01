using movieCinema.Data.Base;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace movieCinema.Models
{
    public class Category : IEntityBase
    {
        [Key]
        public int Id { get; set; }

        [Display(Name = "Category Name")]
        [Required(ErrorMessage = "Category Name is required")]
        [StringLength(50, MinimumLength = 3, ErrorMessage = "Category name must be between 3 and 50 characters")]
        public string Name { get; set; }

        [Display(Name = "Description")]
        [Required(ErrorMessage = "Category Description is required")]
        public string Description { get; set; }

        // Relationship
        public List<Movie>? Movies { get; set; }
    }
}
