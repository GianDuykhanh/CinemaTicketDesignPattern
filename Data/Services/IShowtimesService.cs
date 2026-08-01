using movieCinema.Data.Base;
using movieCinema.Data.ViewModels;
using movieCinema.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace movieCinema.Data.Services
{
    public interface IShowtimesService : IEntityBaseRepository<Showtime>
    {
        Task<IEnumerable<Showtime>> GetShowtimesByMovieIdAsync(int movieId);
        Task<IEnumerable<Showtime>> GetShowtimesWithDetailsAsync();
        Task<Showtime> GetShowtimeByIdWithDetailsAsync(int id);
        Task<IEnumerable<ShowtimeScheduleVM>> GetShowtimeScheduleAsync();
    }
}

