using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using movieCinema.Data.Services;
using movieCinema.Models;
using MovieCinema.Data;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using System.IO;
using System;

namespace movieCinema.Controllers
{
    [Authorize(Roles = "Admin")]
    public class CinemasController : Controller
    {
        private readonly ICinemasService _service;
        private readonly IWebHostEnvironment _webHostEnvironment;
        public CinemasController(ICinemasService service, IWebHostEnvironment webHostEnvironment)
        {
            _service = service;
            _webHostEnvironment = webHostEnvironment;
        }
        public async Task<IActionResult> Index()
        {
            var allCinemas = await _service.GetAllAsync();
            return View(allCinemas);
        }

        // Get: Cinemas/Create
        public IActionResult Create()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Create([Bind("Name,Description")] Cinema cinema, IFormFile logoFile)
        {
            // Remove default Model Binding validation for Logo because we handle it manually
            ModelState.Remove("Logo");

            if (logoFile == null || logoFile.Length == 0)
            {
                ModelState.AddModelError("Logo", "Cinema Logo is required.");
            }
            else
            {
                try
                {
                    string uploadsFolder = Path.Combine(_webHostEnvironment.WebRootPath, "images");
                    if (!Directory.Exists(uploadsFolder))
                    {
                        Directory.CreateDirectory(uploadsFolder);
                    }
                    string uniqueFileName = Guid.NewGuid().ToString() + "_" + Path.GetFileName(logoFile.FileName);
                    string filePath = Path.Combine(uploadsFolder, uniqueFileName);
                    using (var fileStream = new FileStream(filePath, FileMode.Create))
                    {
                        await logoFile.CopyToAsync(fileStream);
                    }
                    cinema.Logo = "/images/" + uniqueFileName;
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError("Logo", "An error occurred while uploading the file: " + ex.Message);
                }
            }

            if (!ModelState.IsValid)
            {
                return View(cinema);
            }
            await _service.AddAsync(cinema);
            return RedirectToAction(nameof(Index));
        }
        

        // Get: Cinemas/Details/1
        public async Task<IActionResult> Details(int id)
        {
            var cinemaDetails = await _service.GetByIdAsync(id);
            if (cinemaDetails == null) return View("NotFound");
            return View(cinemaDetails);
        }

        // Get: Cinemas/Edit/1
        public async Task<IActionResult> Edit(int id)
        {
            var cinemaDetails = await _service.GetByIdAsync(id);
            if (cinemaDetails == null) return View("NotFound");
            return View(cinemaDetails);
        }

        [HttpPost]
        public async Task<IActionResult> Edit(int id, [Bind("Id,Name,Description,Logo")] Cinema cinema, IFormFile logoFile)
        {
            if (logoFile != null && logoFile.Length > 0)
            {
                try
                {
                    string uploadsFolder = Path.Combine(_webHostEnvironment.WebRootPath, "images");
                    if (!Directory.Exists(uploadsFolder))
                    {
                        Directory.CreateDirectory(uploadsFolder);
                    }
                    string uniqueFileName = Guid.NewGuid().ToString() + "_" + Path.GetFileName(logoFile.FileName);
                    string filePath = Path.Combine(uploadsFolder, uniqueFileName);
                    using (var fileStream = new FileStream(filePath, FileMode.Create))
                    {
                        await logoFile.CopyToAsync(fileStream);
                    }

                    // Delete old logo from disk if it was saved locally
                    if (!string.IsNullOrEmpty(cinema.Logo) && cinema.Logo.StartsWith("/images/"))
                    {
                        string oldFilePath = Path.Combine(_webHostEnvironment.WebRootPath, cinema.Logo.TrimStart('/'));
                        if (System.IO.File.Exists(oldFilePath))
                        {
                            System.IO.File.Delete(oldFilePath);
                        }
                    }

                    cinema.Logo = "/images/" + uniqueFileName;
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError("Logo", "An error occurred while uploading the file: " + ex.Message);
                }
            }

            ModelState.Remove("Logo");

            if (string.IsNullOrEmpty(cinema.Logo))
            {
                ModelState.AddModelError("Logo", "Cinema Logo is required.");
            }

            if (!ModelState.IsValid)
            {
                return View(cinema);
            }
            await _service.UpdateAsync(id, cinema);
            return RedirectToAction(nameof(Index));
        }

        // Get: Cinemas/Delete/1
        public async Task<IActionResult> Delete(int id)
        {
            var cinemaDetails = await _service.GetByIdAsync(id);
            if (cinemaDetails == null) return View("NotFound");
            return View(cinemaDetails);
        }

        [HttpPost, ActionName("Delete")]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var cinemaDetails = await _service.GetByIdAsync(id);
            if (cinemaDetails == null) return View("NotFound");

            // Delete physical logo file from disk when cinema is deleted
            if (!string.IsNullOrEmpty(cinemaDetails.Logo) && cinemaDetails.Logo.StartsWith("/images/"))
            {
                string filePath = Path.Combine(_webHostEnvironment.WebRootPath, cinemaDetails.Logo.TrimStart('/'));
                if (System.IO.File.Exists(filePath))
                {
                    System.IO.File.Delete(filePath);
                }
            }

            await _service.DeleteAsync(id);
            return RedirectToAction(nameof(Index));
        }
    }
}
