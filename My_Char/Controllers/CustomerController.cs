using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
             // <-- ADDED: CustomerAuth lives here
using My_Char.Data;
using My_Char.Data.DbInitializer;
using My_Char.Models.Entities;
using My_Char.ViewModels;
using System.Text.RegularExpressions;

namespace My_Char.Controllers
{
    [AllowAnonymous]
    public class CustomerController : Controller
    {
        private readonly SalonDbContext _db;
        public CustomerController(SalonDbContext db) => _db = db;

        [HttpGet]
        public IActionResult Login() => View(new CustomerLoginVm());

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(CustomerLoginVm vm)
        {
            var phone = (vm.Phone ?? "").Trim();
            var name = (vm.Name ?? "").Trim();

            if (!Regex.IsMatch(phone, @"^0\d{9}$"))
            {
                ModelState.AddModelError("", "Enter a valid SA phone (e.g. 0821234567)");
                return View(vm);
            }
            if (name.Length < 2)
            {
                ModelState.AddModelError("", "Enter your name");
                return View(vm);
            }

            var client = await _db.Clients.FirstOrDefaultAsync(c => c.Phone == phone);
            if (client == null)
            {
                client = new Client { FullName = name, Phone = phone };
                _db.Clients.Add(client);
                await _db.SaveChangesAsync();
            }
            else if (!string.Equals(client.FullName.Trim(), name, StringComparison.OrdinalIgnoreCase))
            {
                ModelState.AddModelError("", "Name and phone don't match our records.");
                return View(vm);
            }

            await CustomerAuth.SignInAsync(HttpContext, client);
            return RedirectToAction(nameof(MyBookings));
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CustomerAuth.Scheme);
            return RedirectToAction("Index", "Dashboard");
        }

        [Authorize(AuthenticationSchemes = CustomerAuth.Scheme)]
        public async Task<IActionResult> MyBookings()
        {
            var id = CustomerAuth.GetClientId(User);
            if (id == null) return Challenge();

            var appts = await _db.Appointments
                .Include(a => a.Service)
                .Where(a => a.ClientId == id)
                .OrderByDescending(a => a.StartUtc)
                .Take(50)
                .ToListAsync();

            var shop = await _db.ShopConfigs.FirstAsync();
            var nowUtc = DateTime.UtcNow;

            return View(new MyBookingsVm
            {
                ShopName = shop.Name,
                ShopPhone = shop.Phone,
                Upcoming = appts
                    .Where(a => a.StartUtc >= nowUtc
                             && a.Status != AppointmentStatus.Cancelled
                             && a.Status != AppointmentStatus.NoShow)
                    .OrderBy(a => a.StartUtc)
                    .Select(ToVm).ToList(),
                Past = appts
                    .Where(a => a.StartUtc < nowUtc
                             || a.Status == AppointmentStatus.Cancelled
                             || a.Status == AppointmentStatus.NoShow)
                    .Select(ToVm).ToList()
            });
        }

        private static BookingVm ToVm(Appointment a) => new()
        {
            Id = a.Id,
            StartLocal = TimeZoneInfo.ConvertTimeFromUtc(a.StartUtc, TimeZoneInfo.Local),
            ServiceName = a.Service.Name,
            Price = a.Price,
            Status = a.Status,
            CanCancel = a.StartUtc > DateTime.UtcNow.AddHours(2)
                     && (a.Status == AppointmentStatus.Booked
                      || a.Status == AppointmentStatus.Confirmed)
        };

        [Authorize(AuthenticationSchemes = CustomerAuth.Scheme)]
        public async Task<IActionResult> Book()
        {
            var shop = await _db.ShopConfigs.FirstAsync();
            var services = await _db.Services.Where(s => s.IsActive).ToListAsync();
            return View(new CustomerBookVm
            {
                ShopName = shop.Name,
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

        [HttpPost, ValidateAntiForgeryToken]
        [Authorize(AuthenticationSchemes = CustomerAuth.Scheme)]
        public async Task<IActionResult> Cancel(int id)
        {
            var clientId = CustomerAuth.GetClientId(User);
            if (clientId == null) return Challenge();

            var appt = await _db.Appointments
                .FirstOrDefaultAsync(a => a.Id == id && a.ClientId == clientId);
            if (appt == null) return NotFound();

            if (appt.StartUtc <= DateTime.UtcNow.AddHours(2))
            {
                TempData["Error"] = "Cannot cancel within 2 hours. Please call the salon.";
                return RedirectToAction(nameof(MyBookings));
            }

            appt.Status = AppointmentStatus.Cancelled;
            await _db.SaveChangesAsync();
            TempData["Ok"] = "Cancelled. The slot is free for someone else.";
            return RedirectToAction(nameof(MyBookings));
        }
    }
}