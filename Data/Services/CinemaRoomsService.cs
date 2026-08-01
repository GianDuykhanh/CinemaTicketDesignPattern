using movieCinema.Data.Base;
using movieCinema.Models;
using MovieCinema.Data;

namespace movieCinema.Data.Services
{
    public class CinemaRoomsService : EntityBaseRepository<CinemaRoom>, ICinemaRoomsService
    {
        public CinemaRoomsService(AppDbContext context) : base(context)
        {
        }
    }
}
