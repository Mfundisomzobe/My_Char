using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using My_Char.Data;
using My_Char.ViewModels;

namespace My_Char.Controllers
{
    public class PublicController : Controller
    {
        private readonly SalonDbContext _db;
        public PublicController(SalonDbContext db) => _db = db;
        public async Task<IActionResult> Book(string slug)
        {
            var shop = await _db.ShopConfigs.FirstOrDefaultAsync(s => s.Slug == slug);
            if (shop == null) return NotFound("Shop not found");
            var services = await _db.Services
            .Where(s => s.IsActive).OrderBy(s => s.Price).ToListAsync();
            return View(new PublicBookingViewModel
            {
                ShopName = shop.Name,
                OwnerName = shop.OwnerName,
                Phone = shop.Phone,
                Slug = shop.Slug,
                Services = services.Select(s => new ServiceDto
                {
                    Id = s.Id,
                    Name = s.Name,
                    DurationMinutes = s.DurationMinutes,
                    Price = s.Price
                }).ToList()
            });
        }

    }
}
