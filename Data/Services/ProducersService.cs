using movieCinema.Data.Base;
using movieCinema.Models;
using MovieCinema.Data;

namespace movieCinema.Data.Services
{
    public class ProducersService : EntityBaseRepository<Producer>, IProducersService
    {
        public ProducersService(AppDbContext context) : base(context)
        {
        }
    }
}
