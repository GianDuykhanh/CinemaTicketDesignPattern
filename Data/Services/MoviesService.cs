using Microsoft.EntityFrameworkCore;
using movieCinema.Data.Base;
using movieCinema.Data.ViewModels;
using movieCinema.Models;
using MovieCinema.Data;
using MovieCinema.Models;

namespace movieCinema.Data.Services
{
    public class MoviesService : EntityBaseRepository<Movie>, IMoviesService
    {
        private readonly AppDbContext _context;
        public MoviesService(AppDbContext context) : base(context)
        {
            _context = context;
        }

        public async Task AddNewMovieAsync(NewMovieVM data)
        {
            var newMovie = new Movie()
            {
                Name = data.Name,
                Description = data.Description,
                Price = data.Price,
                ImageURL = data.ImageURL,
                TrailerURL = data.TrailerURL,
                Duration = data.Duration,
                CinemaId = data.CinemaId,
                CinemaRoomId = data.CinemaRoomId,
                StartDate = data.StartDate,
                EndDate = data.EndDate,
                CategoryId = data.CategoryId,
                Status = data.Status,
                ProducerId = data.ProducerId,
            };
            await _context.Movies.AddAsync(newMovie);
            await _context.SaveChangesAsync();

            // Add movie actors
            foreach(var actorId in data.ActorIds)
            {
                var newActorMovie = new Actor_Movie()
                {
                    MovieId = newMovie.Id,
                    ActorId = actorId
                };
                await _context.Actors_Movies.AddAsync(newActorMovie);
            }
            await _context.SaveChangesAsync();
        }

        public async Task<Movie> GetMovieByIdAsync(int id)
        {
            var movieDetails = await _context.Movies
                .Include(c => c.Cinema)
                .Include(cr => cr.CinemaRoom)
                .Include(p => p.Producer)
                .Include(cat => cat.Category)
                .Include(am => am.Actors_Movies).ThenInclude(a => a.Actor)
                .Include(r => r.MovieReviews)
                .FirstOrDefaultAsync(n => n.Id == id);
            return movieDetails;
        }

        public async Task<NewMovieDropdownsVM> GetNewMovieDropdownsValues()
        {
            var response = new NewMovieDropdownsVM()
            {
                Actors = await _context.Actors.OrderBy(n => n.FullName).ToListAsync(),
                Cinemas = await _context.Cinemas.OrderBy(n => n.Name).ToListAsync(),
                Producers = await _context.Producers.OrderBy(n => n.FullName).ToListAsync(),
                CinemaRooms = await _context.CinemaRooms.Include(cr => cr.Cinema).OrderBy(n => n.Name).ToListAsync(),
                Categories = await _context.Categories.OrderBy(n => n.Name).ToListAsync()
            };
            

            return response;
        }

        public async Task UpdateMovieAsync(NewMovieVM data)
        {
            var dbMovie = await _context.Movies.FirstOrDefaultAsync(n => n.Id == data.Id);

            if (dbMovie != null)
            {
                dbMovie.Name = data.Name;
                dbMovie.Description = data.Description;
                dbMovie.Price = data.Price;
                dbMovie.ImageURL = data.ImageURL;
                dbMovie.TrailerURL = data.TrailerURL;
                dbMovie.Duration = data.Duration;
                dbMovie.Status = data.Status;
                dbMovie.CinemaId = data.CinemaId;
                dbMovie.CinemaRoomId = data.CinemaRoomId;
                dbMovie.StartDate = data.StartDate;
                dbMovie.EndDate = data.EndDate;
                dbMovie.CategoryId = data.CategoryId;
                dbMovie.ProducerId = data.ProducerId;
                await _context.SaveChangesAsync();
            }

            //Remove existing actors
            var existingActorsDb = _context.Actors_Movies.Where(n => n.MovieId == data.Id).ToList();
            _context.Actors_Movies.RemoveRange(existingActorsDb);
            await _context.SaveChangesAsync();

            //Add Movie Actors
            foreach (var actorId in data.ActorIds)
            {
                var newActorMovie = new Actor_Movie()
                {
                    MovieId = data.Id,
                    ActorId = actorId
                };
                await _context.Actors_Movies.AddAsync(newActorMovie);
            }
            await _context.SaveChangesAsync();
        }

        public async Task AddReviewAsync(MovieReview review)
        {
            await _context.MovieReviews.AddAsync(review);
            await _context.SaveChangesAsync();
        }
    }
}
