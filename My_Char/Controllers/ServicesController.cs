using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using My_Char.Data;
using My_Char.Models.Entities;

namespace My_Char.Controllers
{
    [Authorize(Roles = "Admin")]
    public class ServicesController : Controller
    {
        private readonly SalonDbContext _db;
        public ServicesController(SalonDbContext db) => _db = db;
        public async Task<IActionResult> Index() =>
        View(await _db.Services.OrderBy(s => s.Name).ToListAsync());
        public IActionResult Create() => View(new Service());
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Service s)
        {
            if (!ModelState.IsValid) return View(s);
            _db.Services.Add(s);
            await _db.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }
        public async Task<IActionResult> Edit(int id)
        {
            var s = await _db.Services.FindAsync(id);
            return s == null ? NotFound() : View(s);
        }
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(Service s)
        {
            if (!ModelState.IsValid) return View(s);
            _db.Services.Update(s);
            await _db.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleActive(int id)
        {
            var s = await _db.Services.FindAsync(id);
            if (s != null) { s.IsActive = !s.IsActive; await _db.SaveChangesAsync(); }
            return RedirectToAction(nameof(Index));
        }

    }
}
