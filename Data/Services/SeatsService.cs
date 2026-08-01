using Microsoft.EntityFrameworkCore;
using movieCinema.Data.Base;
using movieCinema.Models;
using MovieCinema.Data;

namespace movieCinema.Data.Services
{
    public class SeatsService : EntityBaseRepository<Seat>, ISeatsService
    {
        public SeatsService(AppDbContext context) : base(context)
        {
        }

        public async Task<IEnumerable<Seat>> GetSeatsByRoomAsync(int cinemaRoomId)
        {
            return await _context.Seats
                .Where(s => s.CinemaRoomId == cinemaRoomId)
                .OrderBy(s => s.Row)
                .ThenBy(s => s.Number)
                .ToListAsync();
        }

        public async Task<IEnumerable<Seat>> GetAvailableSeatsByRoomAsync(int cinemaRoomId)
        {
            return await _context.Seats
                .Where(s => s.CinemaRoomId == cinemaRoomId && s.IsAvailable)
                .OrderBy(s => s.Row)
                .ThenBy(s => s.Number)
                .ToListAsync();
        }

        public async Task<bool> GenerateSeatsAsync(int cinemaRoomId, int rows, int seatsPerRow)
        {
            var existingCount = await _context.Seats.CountAsync(s => s.CinemaRoomId == cinemaRoomId);
            if (existingCount > 0)
                return false;

            var seats = new List<Seat>();
            var rowLabels = new List<string>();

            for (int i = 0; i < rows; i++)
            {
                rowLabels.Add(((char)('A' + i)).ToString());
            }

            var vipRows = new[] { rows - 2, rows - 1 };
            var coupleSeats = new[] { seatsPerRow - 1, seatsPerRow - 2 };

            for (int r = 0; r < rows; r++)
            {
                for (int n = 1; n <= seatsPerRow; n++)
                {
                    var seatType = SeatType.Standard;

                    if (coupleSeats.Contains(n))
                        seatType = SeatType.Couple;
                    else if (vipRows.Contains(r))
                        seatType = SeatType.VIP;
                    else if (r == 0 && n == 1)
                        seatType = SeatType.Disabled;

                    seats.Add(new Seat
                    {
                        Row = rowLabels[r],
                        Number = n,
                        SeatType = seatType,
                        IsAvailable = true,
                        CinemaRoomId = cinemaRoomId
                    });
                }
            }

            await _context.Seats.AddRangeAsync(seats);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<int> GetSeatCountByRoomAsync(int cinemaRoomId)
        {
            return await _context.Seats.CountAsync(s => s.CinemaRoomId == cinemaRoomId);
        }

        public async Task BulkUpdateSeatTypeAsync(IEnumerable<int> seatIds, SeatType seatType)
        {
            var seats = await _context.Seats.Where(s => seatIds.Contains(s.Id)).ToListAsync();
            foreach (var seat in seats)
            {
                seat.SeatType = seatType;
            }
            await _context.SaveChangesAsync();
        }

        public async Task BulkDeleteAsync(IEnumerable<int> seatIds)
        {
            var seats = await _context.Seats.Where(s => seatIds.Contains(s.Id)).ToListAsync();
            _context.Seats.RemoveRange(seats);
            await _context.SaveChangesAsync();
        }

        public async Task BulkToggleAvailableAsync(IEnumerable<int> seatIds)
        {
            var seats = await _context.Seats.Where(s => seatIds.Contains(s.Id)).ToListAsync();
            foreach (var seat in seats)
            {
                seat.IsAvailable = !seat.IsAvailable;
            }
            await _context.SaveChangesAsync();
        }
    }
}