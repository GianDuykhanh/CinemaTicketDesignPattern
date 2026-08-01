using movieCinema.Data.Base;
using movieCinema.Data.ViewModels;
using movieCinema.Models;

namespace movieCinema.Data.Services
{
    public interface IMoviesService : IEntityBaseRepository<Movie>
    {
        Task<Movie> GetMovieByIdAsync(int id);
        Task<NewMovieDropdownsVM> GetNewMovieDropdownsValues();
        Task AddNewMovieAsync(NewMovieVM data);
        Task UpdateMovieAsync(NewMovieVM data);
        Task AddReviewAsync(MovieReview review);
    }
}
