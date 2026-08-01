using movieCinema.Data.Base;
using movieCinema.Models;

namespace movieCinema.Data.Services
{
    public interface ISeatsService : IEntityBaseRepository<Seat>
    {
        Task<IEnumerable<Seat>> GetSeatsByRoomAsync(int cinemaRoomId);
        Task<IEnumerable<Seat>> GetAvailableSeatsByRoomAsync(int cinemaRoomId);
        Task<bool> GenerateSeatsAsync(int cinemaRoomId, int rows, int seatsPerRow);
        Task<int> GetSeatCountByRoomAsync(int cinemaRoomId);
        Task BulkUpdateSeatTypeAsync(IEnumerable<int> seatIds, SeatType seatType);
        Task BulkDeleteAsync(IEnumerable<int> seatIds);
        Task BulkToggleAvailableAsync(IEnumerable<int> seatIds);
    }
}