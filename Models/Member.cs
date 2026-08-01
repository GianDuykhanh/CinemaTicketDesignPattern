using System.ComponentModel.DataAnnotations;

namespace movieCinema.Models
{
    public class Member
    {
        [Key]
        public string Email { get; set; }

        [Required]
        public string Name { get; set; }

        public int Points { get; set; }
    }
}
