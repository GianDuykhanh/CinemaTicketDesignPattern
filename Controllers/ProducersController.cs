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
    public class ProducersController : Controller
    {
        private readonly IProducersService _service;
        private readonly IWebHostEnvironment _webHostEnvironment;
        public ProducersController(IProducersService service, IWebHostEnvironment webHostEnvironment)
        {
            _service = service;
            _webHostEnvironment = webHostEnvironment;
        }
        public async Task<IActionResult> Index()
        {
            var allProducers = await _service.GetAllAsync();
            return View(allProducers);
        }


        // Get: Producers/Details/1
        public async Task<IActionResult> Details(int id)
        {
            var producerDetails = await _service.GetByIdAsync(id);
            if (producerDetails == null) return View("NotFound");
            return View(producerDetails);
        }

        // Get: Producers/Create
        public IActionResult Create()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Create([Bind("FullName,Bio")] Producer producer, IFormFile profilePicture)
        {
            // Remove default Model Binding validation for ProfilePictureURL because we handle it manually
            ModelState.Remove("ProfilePictureURL");

            if (profilePicture == null || profilePicture.Length == 0)
            {
                ModelState.AddModelError("ProfilePictureURL", "Profile Picture is required.");
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
                    string uniqueFileName = Guid.NewGuid().ToString() + "_" + Path.GetFileName(profilePicture.FileName);
                    string filePath = Path.Combine(uploadsFolder, uniqueFileName);
                    using (var fileStream = new FileStream(filePath, FileMode.Create))
                    {
                        await profilePicture.CopyToAsync(fileStream);
                    }
                    producer.ProfilePictureURL = "/images/" + uniqueFileName;
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError("ProfilePictureURL", "An error occurred while uploading the file: " + ex.Message);
                }
            }

            if (!ModelState.IsValid)
            {
                return View(producer);
            }
            await _service.AddAsync(producer);
            return RedirectToAction(nameof(Index));
        }
        // Get: Producers/Edit/1
        public async Task<IActionResult> Edit(int id)
        {
            var producerDetails = await _service.GetByIdAsync(id);
            if (producerDetails == null) return View("NotFound");
            return View(producerDetails);
        }

        [HttpPost]
        public async Task<IActionResult> Edit(int id, [Bind("Id,FullName,Bio,ProfilePictureURL")] Producer producer, IFormFile profilePicture)
        {
            if (profilePicture != null && profilePicture.Length > 0)
            {
                try
                {
                    string uploadsFolder = Path.Combine(_webHostEnvironment.WebRootPath, "images");
                    if (!Directory.Exists(uploadsFolder))
                    {
                        Directory.CreateDirectory(uploadsFolder);
                    }
                    string uniqueFileName = Guid.NewGuid().ToString() + "_" + Path.GetFileName(profilePicture.FileName);
                    string filePath = Path.Combine(uploadsFolder, uniqueFileName);
                    using (var fileStream = new FileStream(filePath, FileMode.Create))
                    {
                        await profilePicture.CopyToAsync(fileStream);
                    }

                    // Delete old profile picture from disk if it was saved locally
                    if (!string.IsNullOrEmpty(producer.ProfilePictureURL) && producer.ProfilePictureURL.StartsWith("/images/"))
                    {
                        string oldFilePath = Path.Combine(_webHostEnvironment.WebRootPath, producer.ProfilePictureURL.TrimStart('/'));
                        if (System.IO.File.Exists(oldFilePath))
                        {
                            System.IO.File.Delete(oldFilePath);
                        }
                    }

                    producer.ProfilePictureURL = "/images/" + uniqueFileName;
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError("ProfilePictureURL", "An error occurred while uploading the file: " + ex.Message);
                }
            }

            ModelState.Remove("ProfilePictureURL");

            if (string.IsNullOrEmpty(producer.ProfilePictureURL))
            {
                ModelState.AddModelError("ProfilePictureURL", "Profile Picture is required.");
            }

            if (!ModelState.IsValid)
            {
                return View(producer);
            }
            if (id == producer.Id)
            {
                await _service.UpdateAsync(id, producer);
                return RedirectToAction(nameof(Index));
            }
            return View(producer);
        }

        // Get: Producers/Delete/1
        public async Task<IActionResult> Delete(int id)
        {
            var producerDetails = await _service.GetByIdAsync(id);
            if (producerDetails == null) return View("NotFound");
            return View(producerDetails);
        }

        [HttpPost, ActionName("Delete")]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var producerDetails = await _service.GetByIdAsync(id);
            if (producerDetails == null) return View("NotFound");

            // Delete physical profile picture from disk when producer is deleted
            if (!string.IsNullOrEmpty(producerDetails.ProfilePictureURL) && producerDetails.ProfilePictureURL.StartsWith("/images/"))
            {
                string filePath = Path.Combine(_webHostEnvironment.WebRootPath, producerDetails.ProfilePictureURL.TrimStart('/'));
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
