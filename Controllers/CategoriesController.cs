using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using movieCinema.Data.Services;
using movieCinema.Models;
using System.Threading.Tasks;
using System.Linq;

namespace movieCinema.Controllers
{
    [Authorize(Roles = "Admin")]
    public class CategoriesController : Controller
    {
        private readonly ICategoriesService _service;

        public CategoriesController(ICategoriesService service)
        {
            _service = service;
        }

        // GET: Categories
        public async Task<IActionResult> Index()
        {
            var allCategories = await _service.GetAllAsync();
            return View(allCategories);
        }

        // GET: Categories/Details/1
        public async Task<IActionResult> Details(int id)
        {
            var categoryDetails = await _service.GetByIdAsync(id);
            if (categoryDetails == null) return View("NotFound");

            // Fetch with movies if needed. Since movies are related, we can grab them.
            // Let's get the detailed object
            var allCategoriesDetailed = await _service.GetAllAsync(n => n.Movies);
            var detailedCategory = allCategoriesDetailed.FirstOrDefault(c => c.Id == id);

            return View(detailedCategory ?? categoryDetails);
        }

        // GET: Categories/Create
        public IActionResult Create()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Create([Bind("Name,Description")] Category category)
        {
            if (!ModelState.IsValid)
            {
                return View(category);
            }

            await _service.AddAsync(category);
            return RedirectToAction(nameof(Index));
        }

        // GET: Categories/Edit/1
        public async Task<IActionResult> Edit(int id)
        {
            var categoryDetails = await _service.GetByIdAsync(id);
            if (categoryDetails == null) return View("NotFound");

            return View(categoryDetails);
        }

        [HttpPost]
        public async Task<IActionResult> Edit(int id, [Bind("Id,Name,Description")] Category category)
        {
            if (id != category.Id) return View("NotFound");

            if (!ModelState.IsValid)
            {
                return View(category);
            }

            await _service.UpdateAsync(id, category);
            return RedirectToAction(nameof(Index));
        }

        // GET: Categories/Delete/1
        public async Task<IActionResult> Delete(int id)
        {
            var categoryDetails = await _service.GetByIdAsync(id);
            if (categoryDetails == null) return View("NotFound");

            return View(categoryDetails);
        }

        [HttpPost, ActionName("Delete")]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var categoryDetails = await _service.GetByIdAsync(id);
            if (categoryDetails == null) return View("NotFound");

            await _service.DeleteAsync(id);
            return RedirectToAction(nameof(Index));
        }
    }
}
