using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Authorization;
using movieCinema.Data.Services;
using movieCinema.Data.ViewModels;
using movieCinema.Models;
using System.Threading.Tasks;
using System.Linq;
using System;

namespace movieCinema.Controllers
{
    [Authorize(Roles = "Admin")]
    public class ShowtimesController : Controller
    {
        private readonly IShowtimesService _service;
        private readonly IMoviesService _moviesService;
        private readonly ICinemaRoomsService _cinemaRoomsService;

        public ShowtimesController(IShowtimesService service, IMoviesService moviesService, ICinemaRoomsService cinemaRoomsService)
        {
            _service = service;
            _moviesService = moviesService;
            _cinemaRoomsService = cinemaRoomsService;
        }

        // GET: Showtimes
        public async Task<IActionResult> Index()
        {
            var allShowtimes = await _service.GetShowtimesWithDetailsAsync();
            return View(allShowtimes);
        }

        // GET: Showtimes/Schedule
        [AllowAnonymous]
        public async Task<IActionResult> Schedule(DateTime? date, int? cinemaId, bool? onlyAvailable)
        {
            var schedule = await _service.GetShowtimeScheduleAsync();

            // 1. Filter by Date
            if (date.HasValue)
            {
                schedule = schedule.Where(s => s.Showtime.StartTime.Date == date.Value.Date);
            }

            // 2. Filter by Cinema
            if (cinemaId.HasValue)
            {
                schedule = schedule.Where(s => s.Showtime.CinemaRoom?.CinemaId == cinemaId.Value);
            }

            // 3. Filter by Availability
            if (onlyAvailable.HasValue && onlyAvailable.Value)
            {
                schedule = schedule.Where(s => s.AvailableSeats > 0);
            }

            // Pass filters list and active filters to view
            var rooms = await _cinemaRoomsService.GetAllAsync(r => r.Cinema);
            var cinemas = rooms
                .Select(r => r.Cinema)
                .Where(c => c != null)
                .GroupBy(c => c.Id)
                .Select(g => g.First())
                .OrderBy(c => c.Name)
                .ToList();

            ViewBag.Cinemas = cinemas;
            ViewBag.SelectedDate = date?.ToString("yyyy-MM-dd");
            ViewBag.SelectedCinemaId = cinemaId;
            ViewBag.OnlyAvailable = onlyAvailable ?? false;

            return View(schedule);
        }

        // GET: Showtimes/Details/5
        public async Task<IActionResult> Details(int id)
        {
            var showtimeDetails = await _service.GetShowtimeByIdWithDetailsAsync(id);
            if (showtimeDetails == null) return View("NotFound");

            return View(showtimeDetails);
        }

        // GET: Showtimes/Create
        public async Task<IActionResult> Create()
        {
            var movies = await _moviesService.GetAllAsync();
            var rooms = await _cinemaRoomsService.GetAllAsync(r => r.Cinema);

            ViewBag.Movies = new SelectList(movies.OrderBy(m => m.Name), "Id", "Name");
            ViewBag.CinemaRoomsList = rooms.OrderBy(r => r.Name).Select(r => new SelectListItem
            {
                Value = r.Id.ToString(),
                Text = $"{r.Name} ({r.Cinema?.Name ?? "No Cinema"})"
            }).ToList();

            return View();
        }

        // POST: Showtimes/Create
        [HttpPost]
        public async Task<IActionResult> Create([Bind("StartTime,EndTime,Price,MovieId")] Showtime showtime, int[] CinemaRoomIds)
        {
            if (CinemaRoomIds == null || CinemaRoomIds.Length == 0)
            {
                ModelState.AddModelError("CinemaRoomId", "Please select at least one screening room.");
            }

            ModelState.Remove("CinemaRoomId");

            if (!ModelState.IsValid)
            {
                var movies = await _moviesService.GetAllAsync();
                var rooms = await _cinemaRoomsService.GetAllAsync(r => r.Cinema);

                ViewBag.Movies = new SelectList(movies.OrderBy(m => m.Name), "Id", "Name", showtime.MovieId);
                ViewBag.CinemaRoomsList = rooms.OrderBy(r => r.Name).Select(r => new SelectListItem
                {
                    Value = r.Id.ToString(),
                    Text = $"{r.Name} ({r.Cinema?.Name ?? "No Cinema"})",
                    Selected = CinemaRoomIds != null && CinemaRoomIds.Contains(r.Id)
                }).ToList();

                return View(showtime);
            }

            foreach (var roomId in CinemaRoomIds)
            {
                var newShowtime = new Showtime
                {
                    StartTime = showtime.StartTime,
                    EndTime = showtime.EndTime,
                    Price = showtime.Price,
                    MovieId = showtime.MovieId,
                    CinemaRoomId = roomId
                };
                await _service.AddAsync(newShowtime);
            }

            return RedirectToAction(nameof(Index));
        }

        // GET: Showtimes/Edit/5
        public async Task<IActionResult> Edit(int id)
        {
            var showtimeDetails = await _service.GetByIdAsync(id);
            if (showtimeDetails == null) return View("NotFound");

            var movies = await _moviesService.GetAllAsync();
            var rooms = await _cinemaRoomsService.GetAllAsync(r => r.Cinema);

            ViewBag.Movies = new SelectList(movies.OrderBy(m => m.Name), "Id", "Name", showtimeDetails.MovieId);
            ViewBag.CinemaRooms = new SelectList(rooms.OrderBy(r => r.Name).Select(r => new
            {
                Id = r.Id,
                DisplayName = $"{r.Name} ({r.Cinema?.Name ?? "No Cinema"})"
            }), "Id", "DisplayName", showtimeDetails.CinemaRoomId);

            return View(showtimeDetails);
        }

        // POST: Showtimes/Edit/5
        [HttpPost]
        public async Task<IActionResult> Edit(int id, [Bind("Id,StartTime,EndTime,Price,MovieId,CinemaRoomId")] Showtime showtime)
        {
            if (id != showtime.Id) return View("NotFound");

            if (!ModelState.IsValid)
            {
                var movies = await _moviesService.GetAllAsync();
                var rooms = await _cinemaRoomsService.GetAllAsync(r => r.Cinema);

                ViewBag.Movies = new SelectList(movies.OrderBy(m => m.Name), "Id", "Name", showtime.MovieId);
                ViewBag.CinemaRooms = new SelectList(rooms.OrderBy(r => r.Name).Select(r => new
                {
                    Id = r.Id,
                    DisplayName = $"{r.Name} ({r.Cinema?.Name ?? "No Cinema"})"
                }), "Id", "DisplayName", showtime.CinemaRoomId);

                return View(showtime);
            }

            await _service.UpdateAsync(id, showtime);
            return RedirectToAction(nameof(Index));
        }

        // GET: Showtimes/Delete/5
        public async Task<IActionResult> Delete(int id)
        {
            var showtimeDetails = await _service.GetShowtimeByIdWithDetailsAsync(id);
            if (showtimeDetails == null) return View("NotFound");

            return View(showtimeDetails);
        }

        // POST: Showtimes/Delete/5
        [HttpPost, ActionName("Delete")]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var showtimeDetails = await _service.GetByIdAsync(id);
            if (showtimeDetails == null) return View("NotFound");

            await _service.DeleteAsync(id);
            return RedirectToAction(nameof(Index));
        }
    }
}
