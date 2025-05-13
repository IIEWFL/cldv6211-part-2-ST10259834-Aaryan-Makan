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
    public class EventController : Controller
    {
        private readonly EventSystemDbContext _context;
        private readonly ILogger<EventController> _logger;

        public EventController(EventSystemDbContext context, ILogger<EventController> logger)
        {
            _context = context;
            _logger = logger;
        }

        // GET: Event
        public async Task<IActionResult> Index()
        {
            try
            {
                return View(await _context.Events.ToListAsync());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving events.");
                TempData["ErrorMessage"] = "An error occurred while retrieving events. Please try again later.";
                return RedirectToAction(nameof(Index));
            }
        }

        // GET: Event/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            try
            {
                var @event = await _context.Events.FirstOrDefaultAsync(m => m.EventId == id);
                if (@event == null)
                {
                    return NotFound();
                }
                return View(@event);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error retrieving event with ID {id}.");
                TempData["ErrorMessage"] = "An error occurred while retrieving the event details.";
                return RedirectToAction(nameof(Index));
            }
        }

        // GET: Event/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: Event/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("EventId,EventName,EventDate,Description")] Event @event)
        {
            try
            {
                if (ModelState.IsValid)
                {
                    _context.Add(@event);
                    await _context.SaveChangesAsync();
                    TempData["SuccessMessage"] = $"Event {@event.EventName} created successfully.";
                    return RedirectToAction(nameof(Index));
                }

                TempData["ErrorMessage"] = "Please correct the errors below.";
                return View(@event);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating event.");
                TempData["ErrorMessage"] = "An error occurred while creating the event. Please try again later.";
                return View(@event);
            }
        }

        // GET: Event/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            try
            {
                var @event = await _context.Events.FindAsync(id);
                if (@event == null)
                {
                    return NotFound();
                }
                return View(@event);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error retrieving event with ID {id} for editing.");
                TempData["ErrorMessage"] = "An error occurred while retrieving the event for editing.";
                return RedirectToAction(nameof(Index));
            }
        }

        // POST: Event/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("EventId,EventName,EventDate,Description")] Event @event)
        {
            if (id != @event.EventId)
            {
                return NotFound();
            }

            try
            {
                if (ModelState.IsValid)
                {
                    try
                    {
                        _context.Update(@event);
                        await _context.SaveChangesAsync();
                        TempData["SuccessMessage"] = $"Event {@event.EventName} updated successfully.";
                    }
                    catch (DbUpdateConcurrencyException)
                    {
                        if (!EventExists(@event.EventId))
                        {
                            return NotFound();
                        }
                        else
                        {
                            throw;
                        }
                    }
                    return RedirectToAction(nameof(Index));
                }

                TempData["ErrorMessage"] = "Please correct the errors below.";
                return View(@event);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error updating event with ID {id}.");
                TempData["ErrorMessage"] = "An error occurred while updating the event. Please try again later.";
                return View(@event);
            }
        }

        // GET: Event/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            try
            {
                var @event = await _context.Events
                    .Include(e => e.Bookings)
                    .FirstOrDefaultAsync(m => m.EventId == id);
                if (@event == null)
                {
                    return NotFound();
                }
                return View(@event);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error retrieving event with ID {id} for deletion.");
                TempData["ErrorMessage"] = "An error occurred while retrieving the event for deletion.";
                return RedirectToAction(nameof(Index));
            }
        }

        // POST: Event/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            try
            {
                var @event = await _context.Events
                    .Include(e => e.Bookings)
                    .FirstOrDefaultAsync(e => e.EventId == id);
                if (@event == null)
                {
                    return NotFound();
                }

                // Check if event has bookings
                if (@event.Bookings.Any())
                {
                    TempData["ErrorMessage"] = "Cannot delete this event because it has associated bookings.";
                    return RedirectToAction(nameof(Index));
                }

                _context.Events.Remove(@event);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Event deleted successfully.";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error deleting event with ID {id}.");
                TempData["ErrorMessage"] = "An error occurred while deleting the event. Please try again later.";
                return RedirectToAction(nameof(Index));
            }
        }

        private bool EventExists(int id)
        {
            return _context.Events.Any(e => e.EventId == id);
        }
    }
}