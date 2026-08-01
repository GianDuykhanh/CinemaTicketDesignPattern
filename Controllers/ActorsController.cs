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
using System.Threading.Tasks;

namespace movieCinema.Controllers
{
    [Authorize(Roles = "Admin")]
    public class ActorsController : Controller
    {
        private readonly IActorsService _service;
        private readonly IWebHostEnvironment _webHostEnvironment;

        public ActorsController(IActorsService service, IWebHostEnvironment webHostEnvironment)
        {
            _service = service;
            _webHostEnvironment = webHostEnvironment;
        }

        public async Task<IActionResult> Index(string searchString)
        {
            var allActors = await _service.GetAllAsync();
            if (!string.IsNullOrEmpty(searchString))
            {
                allActors = allActors.Where(a => a.FullName.ToLower().Contains(searchString.ToLower())).ToList();
                ViewData["CurrentFilter"] = searchString;
            }
            return View(allActors);
        }

        // Get: Actors/Create
        public IActionResult Create()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Create([Bind("FullName,Bio")] Actor actor, IFormFile profilePicture)
        {
            // Remove default Model Binding validation for ProfilePictureURL because we handle it manually
            ModelState.Remove("ProfilePictureURL");

            // Custom validation for file upload
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
                    actor.ProfilePictureURL = "/images/" + uniqueFileName;
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError("ProfilePictureURL", "An error occurred while uploading the file: " + ex.Message);
                }
            }

            if (!ModelState.IsValid)
            {
                return View(actor);
            }
            await _service.AddAsync(actor);
            return RedirectToAction(nameof(Index));
        }

        // Get: Actors/Details/1
        public async Task<IActionResult> Details(int id)
        {
            var actorDetails = await _service.GetByIdAsync(id);
            if (actorDetails == null) return View("NotFound");
            return View(actorDetails);
        }

        // Get: Actors/Edit/1
        public async Task<IActionResult> Edit(int id)
        {
            var actorDetails = await _service.GetByIdAsync(id);
            if (actorDetails == null) return View("NotFound");
            return View(actorDetails);
        }

        [HttpPost]
        public async Task<IActionResult> Edit(int id, [Bind("Id,FullName,Bio,ProfilePictureURL")] Actor actor, IFormFile profilePicture)
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
                    if (!string.IsNullOrEmpty(actor.ProfilePictureURL) && actor.ProfilePictureURL.StartsWith("/images/"))
                    {
                        string oldFilePath = Path.Combine(_webHostEnvironment.WebRootPath, actor.ProfilePictureURL.TrimStart('/'));
                        if (System.IO.File.Exists(oldFilePath))
                        {
                            System.IO.File.Delete(oldFilePath);
                        }
                    }

                    actor.ProfilePictureURL = "/images/" + uniqueFileName;
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError("ProfilePictureURL", "An error occurred while uploading the file: " + ex.Message);
                }
            }

            // Remove default Model Binding validation for ProfilePictureURL because we handle it manually
            ModelState.Remove("ProfilePictureURL");

            if (string.IsNullOrEmpty(actor.ProfilePictureURL))
            {
                ModelState.AddModelError("ProfilePictureURL", "Profile Picture is required.");
            }

            if (!ModelState.IsValid)
            {
                return View(actor);
            }
            await _service.UpdateAsync(id, actor);
            return RedirectToAction(nameof(Index));
        }

        // Get: Actors/Delete/1
        public async Task<IActionResult> Delete(int id)
        {
            var actorDetails = await _service.GetByIdAsync(id);
            if (actorDetails == null) return View("NotFound");
            return View(actorDetails);
        }

        [HttpPost, ActionName("Delete")]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var actorDetails = await _service.GetByIdAsync(id);
            if (actorDetails == null) return View("NotFound");

            // Optional: Delete physical profile picture from disk when actor is deleted
            if (!string.IsNullOrEmpty(actorDetails.ProfilePictureURL) && actorDetails.ProfilePictureURL.StartsWith("/images/"))
            {
                string filePath = Path.Combine(_webHostEnvironment.WebRootPath, actorDetails.ProfilePictureURL.TrimStart('/'));
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

