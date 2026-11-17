using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using My_Char.Data;
using My_Char.Models.Entities;

namespace My_Char.Controllers
{
    [Authorize(Roles = "Admin")]

    public class ClientsController : Controller
    {
        private readonly SalonDbContext _db;
        public ClientsController(SalonDbContext db) => _db = db;
        public async Task<IActionResult> Index(string? search)
        {
            var q = _db.Clients.AsQueryable();
            if (!string.IsNullOrWhiteSpace(search))
                q = q.Where(c => c.FullName.Contains(search) || c.Phone.Contains(search));
            ViewData["Search"] = search;
            return View(await q.OrderBy(c => c.FullName).ToListAsync());
        }
        public async Task<IActionResult> Details(int id)
        {
            var c = await _db.Clients
            .Include(x => x.Appointments.OrderByDescending(a => a.StartUtc).Take(30))
            .ThenInclude(a => a.Service)
            .FirstOrDefaultAsync(x => x.Id == id);
            return c == null ? NotFound() : View(c);
        }
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> SaveNotes(int id, string? notes)
        {
            var c = await _db.Clients.FindAsync(id);
            if (c != null) { c.Notes = notes; await _db.SaveChangesAsync(); }
            return RedirectToAction(nameof(Details), new { id });
        }
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> ResetNoShows(int id)
        {
            var c = await _db.Clients.FindAsync(id);
            if (c != null) { c.NoShowCount = 0; await _db.SaveChangesAsync(); }
            return RedirectToAction(nameof(Details), new { id });
        }
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var hasActive = await _db.Appointments.AnyAsync(a =>
            a.ClientId == id && a.StartUtc > DateTime.UtcNow
            && a.Status != AppointmentStatus.Cancelled && a.Status != AppointmentStatus.NoShow);
            if (hasActive)
            {
                TempData["Error"] = "Cannot delete - client has upcoming bookings.";
                return RedirectToAction(nameof(Details), new { id });
            }
            var c = await _db.Clients.FindAsync(id);
            if (c != null) { _db.Clients.Remove(c); await _db.SaveChangesAsync(); }
            return RedirectToAction(nameof(Index));
        }


    }
}
