using movieCinema.Data.Base;
using movieCinema.Models;
using MovieCinema.Data;

namespace movieCinema.Data.Services
{
    public class VouchersService : EntityBaseRepository<Voucher>, IVouchersService
    {
        public VouchersService(AppDbContext context) : base(context)
        {
        }
    }
}
