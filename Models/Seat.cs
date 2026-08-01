using movieCinema.Data.Base;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace movieCinema.Models
{
    public class Seat : IEntityBase
    {
        [Key]
        public int Id { get; set; }

        [Display(Name = "Row")]
        [Required(ErrorMessage = "Row is required")]
        [StringLength(5, ErrorMessage = "Row cannot exceed 5 characters")]
        public string Row { get; set; }

        [Display(Name = "Seat Number")]
        [Required(ErrorMessage = "Seat number is required")]
        [Range(1, 50, ErrorMessage = "Seat number must be between 1 and 50")]
        public int Number { get; set; }

        [Display(Name = "Seat Type")]
        public SeatType SeatType { get; set; } = SeatType.Standard;

        [Display(Name = "Is Available")]
        public bool IsAvailable { get; set; } = true;

        [Display(Name = "Cinema Room")]
        [Required(ErrorMessage = "Cinema room is required")]
        public int CinemaRoomId { get; set; }

        [ForeignKey("CinemaRoomId")]
        public CinemaRoom? CinemaRoom { get; set; }
    }
}