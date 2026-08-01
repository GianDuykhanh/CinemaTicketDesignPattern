using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using movieCinema.Data.Services;
using movieCinema.Models;
using System.Threading.Tasks;
using System.Linq;

namespace movieCinema.Controllers
{
    [Authorize(Roles = "Admin")]
    public class CinemaRoomsController : Controller
    {
        private readonly ICinemaRoomsService _service;
        private readonly ICinemasService _cinemasService;

        public CinemaRoomsController(ICinemaRoomsService service, ICinemasService cinemasService)
        {
            _service = service;
            _cinemasService = cinemasService;
        }

        // GET: CinemaRooms
        public async Task<IActionResult> Index()
        {
            var allCinemaRooms = await _service.GetAllAsync(n => n.Cinema);
            return View(allCinemaRooms);
        }

        // GET: CinemaRooms/Create
        public async Task<IActionResult> Create()
        {
            var cinemas = await _cinemasService.GetAllAsync();
            ViewBag.Cinemas = new SelectList(cinemas, "Id", "Name");
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Create([Bind("Name,Capacity,CinemaId")] CinemaRoom cinemaRoom)
        {
            if (!ModelState.IsValid)
            {
                var cinemas = await _cinemasService.GetAllAsync();
                ViewBag.Cinemas = new SelectList(cinemas, "Id", "Name", cinemaRoom.CinemaId);
                return View(cinemaRoom);
            }

            await _service.AddAsync(cinemaRoom);
            return RedirectToAction(nameof(Index));
        }

        // GET: CinemaRooms/Details/1
        public async Task<IActionResult> Details(int id)
        {
            var cinemaRoomDetails = await _service.GetByIdAsync(id);
            if (cinemaRoomDetails == null) return View("NotFound");

            var allRooms = await _service.GetAllAsync(n => n.Cinema, n => n.Movies, n => n.Seats);
            var detailedRoom = allRooms.FirstOrDefault(r => r.Id == id);
            
            return View(detailedRoom ?? cinemaRoomDetails);
        }

        // GET: CinemaRooms/Edit/1
        public async Task<IActionResult> Edit(int id)
        {
            var cinemaRoomDetails = await _service.GetByIdAsync(id);
            if (cinemaRoomDetails == null) return View("NotFound");

            var cinemas = await _cinemasService.GetAllAsync();
            ViewBag.Cinemas = new SelectList(cinemas, "Id", "Name", cinemaRoomDetails.CinemaId);
            return View(cinemaRoomDetails);
        }

        [HttpPost]
        public async Task<IActionResult> Edit(int id, [Bind("Id,Name,Capacity,CinemaId")] CinemaRoom cinemaRoom)
        {
            if (id != cinemaRoom.Id) return View("NotFound");

            if (!ModelState.IsValid)
            {
                var cinemas = await _cinemasService.GetAllAsync();
                ViewBag.Cinemas = new SelectList(cinemas, "Id", "Name", cinemaRoom.CinemaId);
                return View(cinemaRoom);
            }

            await _service.UpdateAsync(id, cinemaRoom);
            return RedirectToAction(nameof(Index));
        }

        // GET: CinemaRooms/Delete/1
        public async Task<IActionResult> Delete(int id)
        {
            var allRooms = await _service.GetAllAsync(n => n.Cinema);
            var cinemaRoomDetails = allRooms.FirstOrDefault(r => r.Id == id);
            if (cinemaRoomDetails == null) return View("NotFound");

            return View(cinemaRoomDetails);
        }

        [HttpPost, ActionName("Delete")]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var cinemaRoomDetails = await _service.GetByIdAsync(id);
            if (cinemaRoomDetails == null) return View("NotFound");

            await _service.DeleteAsync(id);
            return RedirectToAction(nameof(Index));
        }
    }
}
