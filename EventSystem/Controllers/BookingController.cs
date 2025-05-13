using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using EventSystem.Data;
using EventSystem.Models;
using Microsoft.Extensions.Logging;

namespace EventSystem.Controllers
{
    public class BookingController : Controller
    {
        private readonly EventSystemDbContext _context;
        private readonly ILogger<BookingController> _logger;

        public BookingController(EventSystemDbContext context, ILogger<BookingController> logger)
        {
            _context = context;
            _logger = logger;
        }

        // GET: Booking
        public async Task<IActionResult> Index(string searchString)
        {
            try
            {
                var bookings = _context.BookingViews
                    .AsQueryable();

                if (!string.IsNullOrEmpty(searchString))
                {
                    bookings = bookings.Where(b =>
                        b.EventName.Contains(searchString) ||
                        b.VenueName.Contains(searchString) ||
                        b.BookingId.ToString().Contains(searchString));
                }

                return View(await bookings.ToListAsync());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving bookings.");
                TempData["ErrorMessage"] = "An error occurred while retrieving bookings. Please try again later.";
                return RedirectToAction(nameof(Index));
            }
        }

        // GET: Booking/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            try
            {
                var booking = await _context.Bookings
                    .Include(b => b.Event)
                    .Include(b => b.Venue)
                    .FirstOrDefaultAsync(m => m.BookingId == id);

                if (booking == null) return NotFound();

                return View(booking);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error retrieving booking with ID {id}.");
                TempData["ErrorMessage"] = "An error occurred while retrieving the booking details.";
                return RedirectToAction(nameof(Index));
            }
        }

        // GET: Booking/Create
        public IActionResult Create()
        {
            ViewData["EventId"] = new SelectList(_context.Events, "EventId", "EventName");
            ViewData["VenueId"] = new SelectList(_context.Venues, "VenueId", "VenueName");
            return View();
        }

        // POST: Booking/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("BookingId,VenueId,EventId,BookingDate")] Booking booking)
        {
            try
            {
                // Check for double booking
                if (booking.VenueId.HasValue && _context.Bookings.Any(b => b.VenueId == booking.VenueId && b.BookingDate == booking.BookingDate && b.BookingId != booking.BookingId))
                {
                    ModelState.AddModelError("BookingDate", "This venue is already booked on the selected date.");
                }

                if (ModelState.IsValid)
                {
                    _context.Add(booking);
                    await _context.SaveChangesAsync();
                    TempData["SuccessMessage"] = "Booking created successfully.";
                    return RedirectToAction(nameof(Index));
                }

                TempData["ErrorMessage"] = "Please correct the errors below.";
                ViewData["EventId"] = new SelectList(_context.Events, "EventId", "EventName", booking.EventId);
                ViewData["VenueId"] = new SelectList(_context.Venues, "VenueId", "VenueName", booking.VenueId);
                return View(booking);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating booking.");
                TempData["ErrorMessage"] = "An error occurred while creating the booking. Please try again later.";
                ViewData["EventId"] = new SelectList(_context.Events, "EventId", "EventName", booking.EventId);
                ViewData["VenueId"] = new SelectList(_context.Venues, "VenueId", "VenueName", booking.VenueId);
                return View(booking);
            }
        }

        // GET: Booking/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            try
            {
                var booking = await _context.Bookings.FindAsync(id);
                if (booking == null) return NotFound();

                ViewData["EventId"] = new SelectList(_context.Events, "EventId", "EventName", booking.EventId);
                ViewData["VenueId"] = new SelectList(_context.Venues, "VenueId", "VenueName", booking.VenueId);
                return View(booking);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error retrieving booking with ID {id} for editing.");
                TempData["ErrorMessage"] = "An error occurred while retrieving the booking for editing.";
                return RedirectToAction(nameof(Index));
            }
        }

        // POST: Booking/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("BookingId,VenueId,EventId,BookingDate")] Booking booking)
        {
            if (id != booking.BookingId) return NotFound();

            try
            {
                // Check for double booking
                if (booking.VenueId.HasValue && _context.Bookings.Any(b => b.VenueId == booking.VenueId && b.BookingDate == booking.BookingDate && b.BookingId != booking.BookingId))
                {
                    ModelState.AddModelError("BookingDate", "This venue is already booked on the selected date.");
                }

                if (ModelState.IsValid)
                {
                    try
                    {
                        _context.Update(booking);
                        await _context.SaveChangesAsync();
                        TempData["SuccessMessage"] = "Booking updated successfully.";
                    }
                    catch (DbUpdateConcurrencyException)
                    {
                        if (!BookingExists(booking.BookingId)) return NotFound();
                        else throw;
                    }
                    return RedirectToAction(nameof(Index));
                }

                TempData["ErrorMessage"] = "Please correct the errors below.";
                ViewData["EventId"] = new SelectList(_context.Events, "EventId", "EventName", booking.EventId);
                ViewData["VenueId"] = new SelectList(_context.Venues, "VenueId", "VenueName", booking.VenueId);
                return View(booking);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error updating booking with ID {id}.");
                TempData["ErrorMessage"] = "An error occurred while updating the booking. Please try again later.";
                ViewData["EventId"] = new SelectList(_context.Events, "EventId", "EventName", booking.EventId);
                ViewData["VenueId"] = new SelectList(_context.Venues, "VenueId", "VenueName", booking.VenueId);
                return View(booking);
            }
        }

        // GET: Booking/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();

            try
            {
                var booking = await _context.Bookings
                    .Include(b => b.Event)
                    .Include(b => b.Venue)
                    .FirstOrDefaultAsync(m => m.BookingId == id);

                if (booking == null) return NotFound();

                return View(booking);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error retrieving booking with ID {id} for deletion.");
                TempData["ErrorMessage"] = "An error occurred while retrieving the booking for deletion.";
                return RedirectToAction(nameof(Index));
            }
        }

        // POST: Booking/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            try
            {
                var booking = await _context.Bookings.FindAsync(id);
                if (booking != null)
                {
                    _context.Bookings.Remove(booking);
                    await _context.SaveChangesAsync();
                    TempData["SuccessMessage"] = "Booking deleted successfully.";
                }
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error deleting booking with ID {id}.");
                TempData["ErrorMessage"] = "An error occurred while deleting the booking. Please try again later.";
                return RedirectToAction(nameof(Index));
            }
        }

        private bool BookingExists(int id)
        {
            return _context.Bookings.Any(e => e.BookingId == id);
        }
    }
}