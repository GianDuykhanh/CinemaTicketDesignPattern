using Microsoft.EntityFrameworkCore;
using movieCinema.Data.Base;
using movieCinema.Data.ViewModels;
using movieCinema.Models;
using MovieCinema.Data;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace movieCinema.Data.Services
{
    public class ShowtimesService : EntityBaseRepository<Showtime>, IShowtimesService
    {
        public ShowtimesService(AppDbContext context) : base(context)
        {
        }

        public async Task<IEnumerable<Showtime>> GetShowtimesByMovieIdAsync(int movieId)
        {
            return await _context.Showtimes
                .Include(s => s.CinemaRoom)
                .ThenInclude(r => r.Cinema)
                .Include(s => s.Movie)
                .Where(s => s.MovieId == movieId)
                .OrderBy(s => s.StartTime)
                .ToListAsync();
        }

        public async Task<IEnumerable<Showtime>> GetShowtimesWithDetailsAsync()
        {
            return await _context.Showtimes
                .Include(s => s.CinemaRoom)
                .ThenInclude(r => r.Cinema)
                .Include(s => s.Movie)
                .OrderBy(s => s.StartTime)
                .ToListAsync();
        }

        public async Task<Showtime> GetShowtimeByIdWithDetailsAsync(int id)
        {
            return await _context.Showtimes
                .Include(s => s.CinemaRoom)
                .ThenInclude(r => r.Cinema)
                .Include(s => s.Movie)
                .FirstOrDefaultAsync(s => s.Id == id);
        }

        public async Task<IEnumerable<ShowtimeScheduleVM>> GetShowtimeScheduleAsync()
        {
            var showtimes = await _context.Showtimes
                .Include(s => s.Movie)
                .Include(s => s.CinemaRoom)
                    .ThenInclude(r => r.Cinema)
                .OrderBy(s => s.StartTime)
                .ToListAsync();

            var bookedCounts = await _context.OrdersItems
                .GroupBy(oi => oi.ShowtimeId)
                .Select(g => new { ShowtimeId = g.Key, BookedCount = g.Sum(oi => oi.Amount) })
                .ToDictionaryAsync(x => x.ShowtimeId, x => x.BookedCount);

            return showtimes.Select(s => {
                int booked = bookedCounts.GetValueOrDefault(s.Id, 0);
                int capacity = s.CinemaRoom?.Capacity ?? 0;
                int available = capacity - booked;
                return new ShowtimeScheduleVM
                {
                    Showtime = s,
                    BookedSeats = booked,
                    AvailableSeats = available > 0 ? available : 0
                };
            }).ToList();
        }
    }
}

