using movieCinema.Models;

namespace movieCinema.Data.ViewModels
{
    public class ShowtimeScheduleVM
    {
        public Showtime Showtime { get; set; }
        public int BookedSeats { get; set; }
        public int AvailableSeats { get; set; }
    }
}
