using Microsoft.AspNetCore.Mvc;
using movieCinema.Data.Services;
using movieCinema.Models;
using System.Linq;
using System.Threading.Tasks;

namespace movieCinema.Controllers
{
    public class SeatsController : Controller
    {
        private readonly ISeatsService _seatsService;
        private readonly ICinemaRoomsService _roomsService;

        public SeatsController(ISeatsService seatsService, ICinemaRoomsService roomsService)
        {
            _seatsService = seatsService;
            _roomsService = roomsService;
        }

        // GET: Seats/Index/1
        public async Task<IActionResult> Index(int roomId)
        {
            var room = await _roomsService.GetByIdAsync(roomId);
            if (room == null) return View("NotFound");

            var seats = await _seatsService.GetSeatsByRoomAsync(roomId);
            var grouped = seats
                .GroupBy(s => s.Row)
                .OrderBy(g => g.Key)
                .ToList();

            ViewBag.Room = room;
            ViewBag.RoomId = roomId;
            return View(grouped);
        }

        // GET: Seats/Create/1
        public async Task<IActionResult> Create(int roomId)
        {
            var room = await _roomsService.GetByIdAsync(roomId);
            if (room == null) return View("NotFound");

            ViewBag.Room = room;
            ViewBag.RoomId = roomId;
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Create(int roomId, int Rows, int SeatsPerRow)
        {
            var room = await _roomsService.GetByIdAsync(roomId);
            if (room == null) return View("NotFound");

            if (Rows < 1 || Rows > 26 || SeatsPerRow < 1 || SeatsPerRow > 50)
            {
                ModelState.AddModelError("", "Rows: 1-26, Seats per row: 1-50");
                ViewBag.Room = room;
                ViewBag.RoomId = roomId;
                return View();
            }

            var generated = await _seatsService.GenerateSeatsAsync(roomId, Rows, SeatsPerRow);
            if (!generated)
            {
                ModelState.AddModelError("", "Seats already exist for this room. Delete existing seats first.");
                ViewBag.Room = room;
                ViewBag.RoomId = roomId;
                return View();
            }

            return RedirectToAction(nameof(Index), new { roomId });
        }

        // GET: Seats/Edit/1?roomId=1
        public async Task<IActionResult> Edit(int id, int roomId)
        {
            var seat = await _seatsService.GetByIdAsync(id);
            if (seat == null) return View("NotFound");

            var room = await _roomsService.GetByIdAsync(roomId);
            ViewBag.Room = room;
            ViewBag.RoomId = roomId;
            return View(seat);
        }

        [HttpPost]
        public async Task<IActionResult> Edit(int id, int roomId, [Bind("Id,Row,Number,SeatType,IsAvailable,CinemaRoomId")] Seat seat)
        {
            if (id != seat.Id) return View("NotFound");

            if (ModelState.IsValid)
            {
                await _seatsService.UpdateAsync(id, seat);
                return RedirectToAction(nameof(Index), new { roomId });
            }

            var room = await _roomsService.GetByIdAsync(roomId);
            ViewBag.Room = room;
            ViewBag.RoomId = roomId;
            return View(seat);
        }

        // GET: Seats/Delete/1?roomId=1
        public async Task<IActionResult> Delete(int id, int roomId)
        {
            var seat = await _seatsService.GetByIdAsync(id);
            if (seat == null) return View("NotFound");

            var room = await _roomsService.GetByIdAsync(roomId);
            ViewBag.Room = room;
            ViewBag.RoomId = roomId;
            return View(seat);
        }

        [HttpPost, ActionName("Delete")]
        public async Task<IActionResult> DeleteConfirmed(int id, int roomId)
        {
            var seat = await _seatsService.GetByIdAsync(id);
            if (seat == null) return View("NotFound");

            await _seatsService.DeleteAsync(id);
            return RedirectToAction(nameof(Index), new { roomId });
        }

        // POST: Seats/DeleteAll/1
        [HttpPost]
        public async Task<IActionResult> DeleteAll(int roomId)
        {
            var seats = await _seatsService.GetSeatsByRoomAsync(roomId);
            foreach (var seat in seats)
            {
                await _seatsService.DeleteAsync(seat.Id);
            }
            return RedirectToAction(nameof(Index), new { roomId });
        }

        // GET: Seats/Manage/1
        public async Task<IActionResult> Manage(int roomId)
        {
            var room = await _roomsService.GetByIdAsync(roomId);
            if (room == null) return View("NotFound");

            var seats = await _seatsService.GetSeatsByRoomAsync(roomId);
            var grouped = seats
                .GroupBy(s => s.Row)
                .OrderBy(g => g.Key)
                .ToList();

            ViewBag.Room = room;
            ViewBag.RoomId = roomId;
            return View(grouped);
        }

        // POST: Seats/ToggleAvailable
        [HttpPost]
        public async Task<IActionResult> ToggleAvailable(int id, int roomId)
        {
            var seat = await _seatsService.GetByIdAsync(id);
            if (seat == null) return Json(new { success = false });

            seat.IsAvailable = !seat.IsAvailable;
            await _seatsService.UpdateAsync(id, seat);
            return Json(new { success = true, isAvailable = seat.IsAvailable });
        }

        // POST: Seats/BulkEdit
        [HttpPost]
        public async Task<IActionResult> BulkEdit(int roomId, [FromForm] List<int> selectedSeats, [FromForm] SeatType seatType)
        {
            if (selectedSeats == null || selectedSeats.Count == 0)
                return RedirectToAction(nameof(Manage), new { roomId });

            await _seatsService.BulkUpdateSeatTypeAsync(selectedSeats, seatType);
            return RedirectToAction(nameof(Manage), new { roomId });
        }

        // POST: Seats/BulkDelete
        [HttpPost]
        public async Task<IActionResult> BulkDelete(int roomId, [FromForm] List<int> selectedSeats)
        {
            if (selectedSeats == null || selectedSeats.Count == 0)
                return RedirectToAction(nameof(Manage), new { roomId });

            await _seatsService.BulkDeleteAsync(selectedSeats);
            return RedirectToAction(nameof(Manage), new { roomId });
        }

        // POST: Seats/BulkToggle
        [HttpPost]
        public async Task<IActionResult> BulkToggle(int roomId, [FromForm] List<int> selectedSeats)
        {
            if (selectedSeats == null || selectedSeats.Count == 0)
                return RedirectToAction(nameof(Manage), new { roomId });

            await _seatsService.BulkToggleAvailableAsync(selectedSeats);
            return RedirectToAction(nameof(Manage), new { roomId });
        }
    }
}