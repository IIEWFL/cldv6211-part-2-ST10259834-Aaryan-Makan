using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using EventSystem.Data;
using EventSystem.Models;
using Azure.Storage.Blobs;
using Azure.Storage;
using Azure.Storage.Blobs.Models;
using Azure.Storage.Sas;
using Microsoft.Extensions.Logging;

namespace Assignment2_Trial.Controllers
{
    public class VenueController : Controller
    {
        private readonly EventSystemDbContext _context;
        private readonly string storageAccountName;
        private readonly string storageAccountKey;
        private readonly string containerName;
        private readonly ILogger<VenueController> _logger;

        public VenueController(EventSystemDbContext context, IConfiguration config, ILogger<VenueController> logger)
        {
            _context = context;
            _logger = logger;
            storageAccountName = config["AzureBlob:storageAccountName"] ?? throw new ArgumentNullException("AzureBlob:storageAccountName");
            storageAccountKey = config["AzureBlob:storageAccountKey"] ?? throw new ArgumentNullException("AzureBlob:storageAccountKey");
            containerName = config["AzureBlob:containername"] ?? "venuepic";
        }

        // Set up blob container client
        private BlobContainerClient GetContainerClient()
        {
            var serviceUri = new Uri($"https://{storageAccountName}.blob.core.windows.net");
            var serviceClient = new BlobServiceClient(serviceUri, new StorageSharedKeyCredential(storageAccountName, storageAccountKey));
            return serviceClient.GetBlobContainerClient(containerName);
        }

        // Set up image upload method with validation
        private async Task<string> UploadImageToBlobAsync(IFormFile file)
        {
            if (file == null || file.Length == 0)
            {
                _logger.LogWarning("No file provided for upload.");
                return null;
            }

            // Validate file size (e.g., max 5MB)
            const long maxFileSize = 5 * 1024 * 1024; // 5MB
            if (file.Length > maxFileSize)
            {
                _logger.LogWarning($"File size exceeds limit: {file.Length} bytes.");
                return null;
            }

            // Validate file type (e.g., only images)
            var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif" };
            var extension = Path.GetExtension(file.FileName).ToLower();
            if (!allowedExtensions.Contains(extension))
            {
                _logger.LogWarning($"Invalid file type: {extension}. Allowed types: {string.Join(", ", allowedExtensions)}.");
                return null;
            }

            try
            {
                var containerClient = GetContainerClient();
                await containerClient.CreateIfNotExistsAsync(PublicAccessType.None);

                var blobName = $"{Guid.NewGuid()}{extension}";
                var blobClient = containerClient.GetBlobClient(blobName);

                using var stream = file.OpenReadStream();
                await blobClient.UploadAsync(stream, new BlobHttpHeaders { ContentType = file.ContentType });

                _logger.LogInformation($"Uploaded blob: {blobName}");
                return blobName;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error uploading to Blob Storage.");
                return null;
            }
        }

        // Set up Shared Access Signature (SAS) token
        private string GenerateSASurl(string blobName)
        {
            if (string.IsNullOrEmpty(blobName))
            {
                throw new ArgumentNullException(nameof(blobName), "Blob name cannot be null or empty.");
            }

            var blobUri = new Uri($"https://{storageAccountName}.blob.core.windows.net/{containerName}/{blobName}");
            var sasBuilder = new BlobSasBuilder
            {
                BlobContainerName = containerName,
                BlobName = blobName,
                Resource = "b",
                ExpiresOn = DateTimeOffset.UtcNow.AddHours(1)
            };
            sasBuilder.SetPermissions(BlobSasPermissions.Read);

            var sasToken = sasBuilder.ToSasQueryParameters(new StorageSharedKeyCredential(storageAccountName, storageAccountKey)).ToString();
            return $"{blobUri}?{sasToken}";
        }

        // GET: Venue
        public async Task<IActionResult> Index()
        {
            try
            {
                return View(await _context.Venues.ToListAsync());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving venues.");
                TempData["ErrorMessage"] = "An error occurred while retrieving venues. Please try again later.";
                return RedirectToAction(nameof(Index));
            }
        }

        // GET: Venue/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            try
            {
                var venue = await _context.Venues.FirstOrDefaultAsync(m => m.VenueId == id);
                if (venue == null)
                {
                    return NotFound();
                }
                return View(venue);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error retrieving venue with ID {id}.");
                TempData["ErrorMessage"] = "An error occurred while retrieving the venue details.";
                return RedirectToAction(nameof(Index));
            }
        }

        // GET: Venue/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: Venue/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("VenueId,VenueName,Location,Capacity")] Venue venue, IFormFile ImageUrl)
        {
            try
            {
                // Clear ModelState errors for ImageUrl to handle manually
                ModelState.Remove("ImageUrl");

                // Validate image presence and type
                if (ImageUrl == null || ImageUrl.Length == 0)
                {
                    ModelState.AddModelError("ImageUrl", "An image is required.");
                }
                else
                {
                    var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif" };
                    var extension = Path.GetExtension(ImageUrl.FileName).ToLower();
                    if (!allowedExtensions.Contains(extension))
                    {
                        ModelState.AddModelError("ImageUrl", "Only .jpg, .jpeg, .png, and .gif files are allowed.");
                    }

                    const long maxFileSize = 5 * 1024 * 1024; // 5MB
                    if (ImageUrl.Length > maxFileSize)
                    {
                        ModelState.AddModelError("ImageUrl", "The image file size must not exceed 5MB.");
                    }
                }

                // Check for duplicate venue name
                if (_context.Venues.Any(v => v.VenueName == venue.VenueName && v.VenueId != venue.VenueId))
                {
                    ModelState.AddModelError("VenueName", "A venue with this name already exists.");
                }

                // Upload image to Azure Blob Storage if no validation errors
                string blobName = null;
                if (ImageUrl != null && ImageUrl.Length > 0 && ModelState.IsValid)
                {
                    blobName = await UploadImageToBlobAsync(ImageUrl);
                    if (blobName == null)
                    {
                        ModelState.AddModelError("ImageUrl", "Failed to upload image to Azure Blob Storage.");
                    }
                }

                if (ModelState.IsValid && blobName != null)
                {
                    var venueWithLinks = new Venue
                    {
                        VenueName = venue.VenueName,
                        Location = venue.Location,
                        Capacity = venue.Capacity,
                        ImageUrl = GenerateSASurl(blobName)
                    };

                    _context.Add(venueWithLinks);
                    await _context.SaveChangesAsync();
                    TempData["SuccessMessage"] = $"Venue {venue.VenueName} was added successfully.";
                    return RedirectToAction(nameof(Index));
                }

                TempData["ErrorMessage"] = "Please correct the errors below.";
                return View(venue);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating venue.");
                TempData["ErrorMessage"] = "An error occurred while creating the venue. Please try again later.";
                return View(venue);
            }
        }

        // GET: Venue/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            try
            {
                var venue = await _context.Venues.FindAsync(id);
                if (venue == null)
                {
                    return NotFound();
                }
                return View(venue);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error retrieving venue with ID {id} for editing.");
                TempData["ErrorMessage"] = "An error occurred while retrieving the venue for editing.";
                return RedirectToAction(nameof(Index));
            }
        }

        // POST: Venue/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("VenueId,VenueName,Location,Capacity,ImageUrl")] Venue venue, IFormFile ImageUrl)
        {
            if (id != venue.VenueId)
            {
                return NotFound();
            }

            try
            {
                // Clear ModelState errors for ImageUrl to handle manually
                ModelState.Remove("ImageUrl");

                // Validate image if provided
                if (ImageUrl != null && ImageUrl.Length > 0)
                {
                    var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif" };
                    var extension = Path.GetExtension(ImageUrl.FileName).ToLower();
                    if (!allowedExtensions.Contains(extension))
                    {
                        ModelState.AddModelError("ImageUrl", "Only .jpg, .jpeg, .png, and .gif files are allowed.");
                    }

                    const long maxFileSize = 5 * 1024 * 1024; // 5MB
                    if (ImageUrl.Length > maxFileSize)
                    {
                        ModelState.AddModelError("ImageUrl", "The image file size must not exceed 5MB.");
                    }
                }

                // Check for duplicate venue name
                if (_context.Venues.Any(v => v.VenueName == venue.VenueName && v.VenueId != venue.VenueId))
                {
                    ModelState.AddModelError("VenueName", "A venue with this name already exists.");
                }

                // Upload new image if provided and no validation errors
                if (ImageUrl != null && ImageUrl.Length > 0 && ModelState.IsValid)
                {
                    var blobName = await UploadImageToBlobAsync(ImageUrl);
                    if (blobName == null)
                    {
                        ModelState.AddModelError("ImageUrl", "Failed to upload image to Azure Blob Storage.");
                    }
                    else
                    {
                        venue.ImageUrl = GenerateSASurl(blobName);
                    }
                }

                if (ModelState.IsValid)
                {
                    _context.Update(venue);
                    await _context.SaveChangesAsync();
                    TempData["SuccessMessage"] = $"Venue {venue.VenueName} was updated successfully.";
                    return RedirectToAction(nameof(Index));
                }

                TempData["ErrorMessage"] = "Please correct the errors below.";
                return View(venue);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error updating venue with ID {id}.");
                TempData["ErrorMessage"] = "An error occurred while updating the venue. Please try again later.";
                return View(venue);
            }
        }

        // GET: Venue/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            try
            {
                var venue = await _context.Venues.Include(v => v.Bookings).FirstOrDefaultAsync(m => m.VenueId == id);
                if (venue == null)
                {
                    return NotFound();
                }
                return View(venue);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error retrieving venue with ID {id} for deletion.");
                TempData["ErrorMessage"] = "An error occurred while retrieving the venue for deletion.";
                return RedirectToAction(nameof(Index));
            }
        }

        // POST: Venue/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            try
            {
                var venue = await _context.Venues.Include(v => v.Bookings).FirstOrDefaultAsync(v => v.VenueId == id);
                if (venue == null)
                {
                    return NotFound();
                }

                // Check if venue has bookings
                if (venue.Bookings.Any())
                {
                    TempData["ErrorMessage"] = "Cannot delete this venue because it has associated bookings.";
                    return RedirectToAction(nameof(Index));
                }

                _context.Venues.Remove(venue);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Venue deleted successfully.";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error deleting venue with ID {id}.");
                TempData["ErrorMessage"] = "An error occurred while deleting the venue. Please try again later.";
                return RedirectToAction(nameof(Index));
            }
        }

        private bool VenueExists(int id)
        {
            return _context.Venues.Any(e => e.VenueId == id);
        }
    }
}