using movieCinema.Models;

namespace movieCinema.Data.ViewModels
{
    public class NewMovieDropdownsVM
    {
        public NewMovieDropdownsVM()
        {
            Producers = new List<Producer>();
            Cinemas = new List<Cinema>();
            Actors = new List<Actor>();
            CinemaRooms = new List<CinemaRoom>();
            Categories = new List<Category>();
        }

        public List<Producer> Producers { get; set; }
        public List<Cinema> Cinemas { get; set; }
        public List<Actor> Actors { get; set; }
        public List<CinemaRoom> CinemaRooms { get; set; }
        public List<Category> Categories { get; set; }
    }
}
