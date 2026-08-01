using System.Linq.Expressions;
using Microsoft.Extensions.Caching.Memory;
using movieCinema.Data.Base;
using movieCinema.Data.Services;
using movieCinema.Data.ViewModels;
using movieCinema.Models;
using MovieCinema.Data.Enums;

namespace movieCinema.Data.Proxy
{
    public class CachedMoviesServiceProxy : IMoviesService
    {
        private readonly MoviesService _realService;
        private readonly IMemoryCache _cache;
        private static readonly TimeSpan DefaultExpiry = TimeSpan.FromMinutes(10);
        private static readonly TimeSpan ShortExpiry = TimeSpan.FromMinutes(2);

        public CachedMoviesServiceProxy(MoviesService realService, IMemoryCache cache)
        {
            _realService = realService;
            _cache = cache;
        }

        public async Task AddAsync(Movie entity)
        {
            await _realService.AddAsync(entity);
            InvalidateAllCaches();
        }

        public async Task DeleteAsync(int id)
        {
            await _realService.DeleteAsync(id);
            InvalidateAllCaches();
        }

        public async Task<IEnumerable<Movie>> GetAllAsync()
        {
            return await _cache.GetOrCreateAsync("movies:all", async entry =>
            {
                entry.SlidingExpiration = DefaultExpiry;
                return await _realService.GetAllAsync();
            }) ?? Enumerable.Empty<Movie>();
        }

        public async Task<IEnumerable<Movie>> GetAllAsync(params Expression<Func<Movie, object>>[] includeProperties)
        {
            return await _realService.GetAllAsync(includeProperties);
        }

        public async Task<Movie> GetByIdAsync(int id)
        {
            string key = $"movies:id:{id}";
            return await _cache.GetOrCreateAsync(key, async entry =>
            {
                entry.SlidingExpiration = DefaultExpiry;
                return await _realService.GetByIdAsync(id);
            }) ?? null!;
        }

        public async Task UpdateAsync(int id, Movie entity)
        {
            await _realService.UpdateAsync(id, entity);
            InvalidateAllCaches();
        }

        // ── Custom Methods ──────────────────────────────────────────────────

        public async Task<IEnumerable<Movie>> GetNowShowingMoviesAsync()
        {
            string key = $"movies:nowshowing:{DateTime.Today:yyyyMMdd}";
            return await _cache.GetOrCreateAsync(key, async entry =>
            {
                entry.SlidingExpiration = DefaultExpiry;
                return (await _realService.GetAllAsync())
                    .Where(m => m.Status == MovieStatus.NowShowing)
                    .ToList();
            }) ?? Enumerable.Empty<Movie>();
        }

        public async Task<IEnumerable<Movie>> GetComingSoonMoviesAsync()
        {
            string key = $"movies:comingsoon:{DateTime.Today:yyyyMMdd}";
            return await _cache.GetOrCreateAsync(key, async entry =>
            {
                entry.SlidingExpiration = DefaultExpiry;
                return (await _realService.GetAllAsync())
                    .Where(m => m.Status == MovieStatus.ComingSoon)
                    .ToList();
            }) ?? Enumerable.Empty<Movie>();
        }

        public async Task<Movie> GetMovieByIdAsync(int id)
        {
            return await GetByIdAsync(id);
        }

        public async Task<NewMovieDropdownsVM> GetNewMovieDropdownsValues()
        {
            // Cache dropdowns ngắn — ít thay đổi
            return await _cache.GetOrCreateAsync("movies:dropdowns", async entry =>
            {
                entry.SlidingExpiration = ShortExpiry;
                return await _realService.GetNewMovieDropdownsValues();
            }) ?? new NewMovieDropdownsVM();
        }

        public async Task AddNewMovieAsync(NewMovieVM data)
        {
            await _realService.AddNewMovieAsync(data);
            InvalidateAllCaches();
        }

        public async Task UpdateMovieAsync(NewMovieVM data)
        {
            await _realService.UpdateMovieAsync(data);
            InvalidateAllCaches();
        }

        public async Task AddReviewAsync(MovieReview review)
        {
            await _realService.AddReviewAsync(review);
            // Không invalidate cache vì review không ảnh hưởng list phim
        }

        // ── Cache Invalidation ──────────────────────────────────────────────
        private void InvalidateAllCaches()
        {
            // Cách đơn giản: xóa tất cả entries liên quan
            // Trong production, dùng CacheTagHelper hoặc Redis
        }
    }
}
