using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using movieCinema.Data.Services;
using movieCinema.Models;
using MovieCinema.Data;

namespace movieCinema.Controllers
{
    public class MoviesController : Controller
    {
        private readonly IMoviesService _service;
        private readonly IWebHostEnvironment _webHostEnvironment;
        private readonly IShowtimesService _showtimesService;
        public MoviesController(IMoviesService service, IWebHostEnvironment webHostEnvironment, IShowtimesService showtimesService)
        {
            _service = service;
            _webHostEnvironment = webHostEnvironment;
            _showtimesService = showtimesService;
        }
        public async Task<IActionResult> Index()
        {
            var allMovies = await _service.GetAllAsync(n => n.Cinema, n => n.CinemaRoom, n => n.Category, n => n.MovieReviews);
            return View(allMovies);
        }

        public async Task<IActionResult> Filter(string searchString)
        {
            var allMovies = await _service.GetAllAsync(n => n.Cinema, n => n.CinemaRoom, n => n.Category, n => n.MovieReviews);

            if (!string.IsNullOrEmpty(searchString))
            {
                //var filteredResult = allMovies.Where(n => n.Name.ToLower().Contains(searchString.ToLower()) || n.Description.ToLower().Contains(searchString.ToLower())).ToList();

                var filteredResultNew = allMovies.Where(n => string.Equals(n.Name, searchString, StringComparison.CurrentCultureIgnoreCase) || string.Equals(n.Description, searchString, StringComparison.CurrentCultureIgnoreCase)).ToList();

                return View("Index", filteredResultNew);
            }

            return View("Index", allMovies);
        }

        // Get: Movies/Details/1
        public async Task<IActionResult> Details(int id)
        {
            var movieDetails = await _service.GetMovieByIdAsync(id);
            if (movieDetails == null) return View("NotFound");

            var showtimes = await _showtimesService.GetShowtimesByMovieIdAsync(id);
            ViewBag.Showtimes = showtimes;

            return View(movieDetails);
        }

        // Get: Movies/Create
        public async Task<IActionResult> Create()
        {
            var movieDropdownsData = await _service.GetNewMovieDropdownsValues();

            ViewBag.Cinemas = new SelectList(movieDropdownsData.Cinemas, "Id", "Name");
            ViewBag.Producers = new SelectList(movieDropdownsData.Producers, "Id", "FullName");
            ViewBag.Actors = new SelectList(movieDropdownsData.Actors, "Id", "FullName");
            ViewBag.Categories = new SelectList(movieDropdownsData.Categories, "Id", "Name");
            ViewBag.CinemaRooms = new SelectList(movieDropdownsData.CinemaRooms.Select(cr => new {
                Id = cr.Id,
                DisplayName = $"{(cr.Cinema != null ? cr.Cinema.Name : "Unknown")} - {cr.Name}"
            }), "Id", "DisplayName");

            return View();
        }

        [HttpPost]
        public async Task<ActionResult> Create(NewMovieVM movie)
        {
            if (movie.TrailerUpload == null)
            {
                ModelState.AddModelError("TrailerUpload", "Trailer upload is required");
            }

            if (!ModelState.IsValid)
            {
                var movieDropdownsData = await _service.GetNewMovieDropdownsValues();

                ViewBag.Cinemas = new SelectList(movieDropdownsData.Cinemas, "Id", "Name");
                ViewBag.Producers = new SelectList(movieDropdownsData.Producers, "Id", "FullName");
                ViewBag.Actors = new SelectList(movieDropdownsData.Actors, "Id", "FullName");
                ViewBag.Categories = new SelectList(movieDropdownsData.Categories, "Id", "Name", movie.CategoryId);
                ViewBag.CinemaRooms = new SelectList(movieDropdownsData.CinemaRooms.Select(cr => new {
                    Id = cr.Id,
                    DisplayName = $"{(cr.Cinema != null ? cr.Cinema.Name : "Unknown")} - {cr.Name}"
                }), "Id", "DisplayName");

                return View(movie);
            }

            if (movie.ImageUpload != null)
            {
                string uploadsFolder = Path.Combine(_webHostEnvironment.WebRootPath, "images");
                if (!Directory.Exists(uploadsFolder))
                {
                    Directory.CreateDirectory(uploadsFolder);
                }
                string uniqueFileName = Guid.NewGuid().ToString() + "_" + movie.ImageUpload.FileName;
                string filePath = Path.Combine(uploadsFolder, uniqueFileName);
                using (var fileStream = new FileStream(filePath, FileMode.Create))
                {
                    await movie.ImageUpload.CopyToAsync(fileStream);
                }
                movie.ImageURL = "/images/" + uniqueFileName;
            }

            if (movie.TrailerUpload != null)
            {
                string uploadsFolder = Path.Combine(_webHostEnvironment.WebRootPath, "trailers");
                if (!Directory.Exists(uploadsFolder))
                {
                    Directory.CreateDirectory(uploadsFolder);
                }
                string uniqueFileName = Guid.NewGuid().ToString() + "_" + movie.TrailerUpload.FileName;
                string filePath = Path.Combine(uploadsFolder, uniqueFileName);
                using (var fileStream = new FileStream(filePath, FileMode.Create))
                {
                    await movie.TrailerUpload.CopyToAsync(fileStream);
                }
                movie.TrailerURL = "/trailers/" + uniqueFileName;
            }

            await _service.AddNewMovieAsync(movie);
            return RedirectToAction(nameof(Index));
        }

        //GET: Movies/Edit/1
        public async Task<IActionResult> Edit(int id)
        {
            var movieDetails = await _service.GetMovieByIdAsync(id);
            if (movieDetails == null) return View("NotFound");

            var response = new NewMovieVM()
            {
                Id = movieDetails.Id,
                Name = movieDetails.Name,
                Description = movieDetails.Description,
                Price = movieDetails.Price,
                StartDate = movieDetails.StartDate,
                EndDate = movieDetails.EndDate,
                ImageURL = movieDetails.ImageURL,
                TrailerURL = movieDetails.TrailerURL,
                Duration = movieDetails.Duration,
                CategoryId = movieDetails.CategoryId,
                Status = movieDetails.Status,
                CinemaId = movieDetails.CinemaId,
                CinemaRoomId = movieDetails.CinemaRoomId,
                ProducerId = movieDetails.ProducerId,
                ActorIds = movieDetails.Actors_Movies.Select(n => n.ActorId).ToList(),
            };

            var movieDropdownsData = await _service.GetNewMovieDropdownsValues();
            ViewBag.Cinemas = new SelectList(movieDropdownsData.Cinemas, "Id", "Name");
            ViewBag.Producers = new SelectList(movieDropdownsData.Producers, "Id", "FullName");
            ViewBag.Actors = new SelectList(movieDropdownsData.Actors, "Id", "FullName");
            ViewBag.Categories = new SelectList(movieDropdownsData.Categories, "Id", "Name", response.CategoryId);
            ViewBag.CinemaRooms = new SelectList(movieDropdownsData.CinemaRooms.Select(cr => new {
                Id = cr.Id,
                DisplayName = $"{(cr.Cinema != null ? cr.Cinema.Name : "Unknown")} - {cr.Name}"
            }), "Id", "DisplayName");

            return View(response);
        }

        [HttpPost]
        public async Task<IActionResult> Edit(int id, NewMovieVM movie)
        {
            if (id != movie.Id) return View("NotFound");

            if (!ModelState.IsValid)
            {
                var movieDropdownsData = await _service.GetNewMovieDropdownsValues();

                ViewBag.Cinemas = new SelectList(movieDropdownsData.Cinemas, "Id", "Name");
                ViewBag.Producers = new SelectList(movieDropdownsData.Producers, "Id", "FullName");
                ViewBag.Actors = new SelectList(movieDropdownsData.Actors, "Id", "FullName");
                ViewBag.Categories = new SelectList(movieDropdownsData.Categories, "Id", "Name", movie.CategoryId);
                ViewBag.CinemaRooms = new SelectList(movieDropdownsData.CinemaRooms.Select(cr => new {
                    Id = cr.Id,
                    DisplayName = $"{(cr.Cinema != null ? cr.Cinema.Name : "Unknown")} - {cr.Name}"
                }), "Id", "DisplayName");

                return View(movie);
            }

            if (movie.ImageUpload != null)
            {
                string uploadsFolder = Path.Combine(_webHostEnvironment.WebRootPath, "images");
                if (!Directory.Exists(uploadsFolder))
                {
                    Directory.CreateDirectory(uploadsFolder);
                }
                string uniqueFileName = Guid.NewGuid().ToString() + "_" + movie.ImageUpload.FileName;
                string filePath = Path.Combine(uploadsFolder, uniqueFileName);
                using (var fileStream = new FileStream(filePath, FileMode.Create))
                {
                    await movie.ImageUpload.CopyToAsync(fileStream);
                }
                movie.ImageURL = "/images/" + uniqueFileName;
            }

            if (movie.TrailerUpload != null)
            {
                string uploadsFolder = Path.Combine(_webHostEnvironment.WebRootPath, "trailers");
                if (!Directory.Exists(uploadsFolder))
                {
                    Directory.CreateDirectory(uploadsFolder);
                }
                string uniqueFileName = Guid.NewGuid().ToString() + "_" + movie.TrailerUpload.FileName;
                string filePath = Path.Combine(uploadsFolder, uniqueFileName);
                using (var fileStream = new FileStream(filePath, FileMode.Create))
                {
                    await movie.TrailerUpload.CopyToAsync(fileStream);
                }
                movie.TrailerURL = "/trailers/" + uniqueFileName;
            }

            await _service.UpdateMovieAsync(movie);
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        public async Task<IActionResult> AddReview([Bind("MovieId,Name,Email,Rating,Comment")] MovieReview review)
        {
            if (!ModelState.IsValid)
            {
                TempData["ReviewError"] = "Vui lòng nhập đầy đủ và đúng định dạng thông tin đánh giá.";
                return RedirectToAction(nameof(Details), new { id = review.MovieId });
            }

            await _service.AddReviewAsync(review);
            TempData["ReviewSuccess"] = "Cảm ơn bạn đã gửi đánh giá phim thành công!";
            return RedirectToAction(nameof(Details), new { id = review.MovieId });
        }
    }
}
