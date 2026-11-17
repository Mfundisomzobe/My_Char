using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using My_Char.Data;
using My_Char.Models.Entities;
using My_Char.ViewModels;

namespace My_Char.Controllers
{
    [Authorize(Roles = "Admin")]
    public class DashboardController : Controller
    {

        private readonly SalonDbContext _db;
        public DashboardController(SalonDbContext db) => _db = db;
        public async Task<IActionResult> Index()
        {
            var shop = await _db.ShopConfigs.FirstAsync();
            var tz = TimeZoneInfo.Local;
            var todayStart = DateTime.Today.ToUniversalTime();
            var todayEnd = DateTime.Today.AddDays(1).ToUniversalTime();
            var appts = await _db.Appointments
            .Include(a => a.Client).Include(a => a.Service)
            .Where(a => a.StartUtc >= todayStart && a.StartUtc < todayEnd)
            .OrderBy(a => a.StartUtc).ToListAsync();
            var completed = appts.Where(a => a.Status == AppointmentStatus.Completed).ToList();
            var noShows = appts.Where(a => a.Status == AppointmentStatus.NoShow).ToList();
            return View(new DashboardViewModel
            {
                BookingLink = $"{Request.Scheme}://{Request.Host}/book/{shop.Slug}",
                MoneyMadeToday = completed.Sum(a => a.Price),
                CashToday = completed.Where(a => a.PaymentMethod == "Cash").Sum(a => a.Price),
                EftToday = completed.Where(a => a.PaymentMethod == "EFT").Sum(a => a.Price),
                NoShowsToday = noShows.Count,
                LostToNoShows = noShows.Sum(a => a.Price),
                Appointments = appts.Select(a => new DashboardAppt
                {
                    Id = a.Id,
                    StartLocal = TimeZoneInfo.ConvertTimeFromUtc(a.StartUtc, tz),
                    ClientName = a.Client.FullName,
                    ClientPhone = a.Client.Phone,
                    ServiceName = a.Service.Name,
                    DurationMin = a.Service.DurationMinutes,
                    Price = a.Price,
                    Status = a.Status,
                    PaymentMethod = a.PaymentMethod
                }).ToList()
            });
        }
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Start(int id)
        {
            var a = await _db.Appointments.FindAsync(id);
            if (a != null) { a.Status = AppointmentStatus.InProgress; await _db.SaveChangesAsync(); }
            return RedirectToAction(nameof(Index));
        }
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Done(int id, string method)
        {
            var a = await _db.Appointments.Include(x => x.Client).FirstOrDefaultAsync(x => x.Id == id);
            if (a != null)
            {
                a.Status = AppointmentStatus.Completed;
                a.PaymentMethod = method;
                a.Client.VisitCount++;
                await _db.SaveChangesAsync();
            }
            return RedirectToAction(nameof(Index));
        }
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> NoShow(int id)
        {
            var a = await _db.Appointments.Include(x => x.Client).FirstOrDefaultAsync(x => x.Id == id);
            if (a != null)
            {
                a.Status = AppointmentStatus.NoShow;
                a.Client.NoShowCount++;
                await _db.SaveChangesAsync();
            }
            return RedirectToAction(nameof(Index));
        }
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Cancel(int id)
        {
            var a = await _db.Appointments.FindAsync(id);
            if (a != null) { a.Status = AppointmentStatus.Cancelled; await _db.SaveChangesAsync(); }
            return RedirectToAction(nameof(Index));
        }
        public async Task<IActionResult> Remind(int id)
        {
            var a = await _db.Appointments
            .Include(x => x.Client).Include(x => x.Service)
            .FirstOrDefaultAsync(x => x.Id == id);
            if (a == null) return NotFound();
            var shop = await _db.ShopConfigs.FirstAsync();
            var phone = new string(a.Client.Phone.Where(char.IsDigit).ToArray());
            if (phone.StartsWith("0")) phone = "27" + phone[1..];
            var local = TimeZoneInfo.ConvertTimeFromUtc(a.StartUtc, TimeZoneInfo.Local);
            var msg = $"Hi {a.Client.FullName}, reminder: {a.Service.Name} on " +
            $"{local:ddd dd MMM} at {local:HH:mm} at {shop.Name}. " +
            $"R{a.Price:0}. Reply 1 to confirm, 2 to cancel.";
            return Redirect($"https://wa.me/{phone}?text={Uri.EscapeDataString(msg)}");
        }

        public async Task<IActionResult> Stats(int? month, int? year)
        {
            var now = DateTime.Now;
            var m = month ?? now.Month;
            var y = year ?? now.Year;
            var startLocal = new DateTime(y, m, 1);
            var endLocal = startLocal.AddMonths(1);
            var startUtc = startLocal.ToUniversalTime();
            var endUtc = endLocal.ToUniversalTime();
            var appts = await _db.Appointments
            .Include(a => a.Service)
            .Where(a => a.StartUtc >= startUtc && a.StartUtc < endUtc)
            .ToListAsync();
            var completed = appts.Where(a => a.Status == AppointmentStatus.Completed).ToList();
            var noShows = appts.Where(a => a.Status == AppointmentStatus.NoShow).ToList();
            return View(new StatsViewModel
            {
                MonthLabel = startLocal.ToString("MMMM yyyy"),
                TotalEarned = completed.Sum(a => a.Price),
                TotalBookings = completed.Count,
                NoShows = noShows.Count,
                LostToNoShows = noShows.Sum(a => a.Price),
                TopServices = completed
            .GroupBy(a => a.Service.Name)
            .Select(g => new ServiceStat
            {
                Name = g.Key,
                Count = g.Count(),
                Revenue = g.Sum(x => x.Price)
            })
            .OrderByDescending(x => x.Revenue).ToList(),
                ByDayOfWeek = appts
            .GroupBy(a => a.StartUtc.ToLocalTime().DayOfWeek)
            .Select(g => new DayStat
            {
                Day = g.Key.ToString(),
                Total = g.Count(),
                NoShows = g.Count(x => x.Status == AppointmentStatus.NoShow)
            }).OrderBy(x => x.Day).ToList()
            });
        }

    }
}
