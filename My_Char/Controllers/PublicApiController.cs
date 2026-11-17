using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;                // <-- ADDED for CustomerAuth
using My_Char.Data;
using My_Char.Data.DbInitializer;
using My_Char.Models.Entities;
using System.Text.RegularExpressions;

namespace My_Char.Controllers
{
    [AllowAnonymous, Route("api/public")]
    [IgnoreAntiforgeryToken]
    public class PublicApiController : ControllerBase
    {
        private readonly SalonDbContext _db;
        public PublicApiController(SalonDbContext db) => _db = db;

        [HttpGet("slots")]
        public async Task<IActionResult> Slots(string slug, int serviceId, DateTime day)
        {
            var shop = await _db.ShopConfigs.FirstOrDefaultAsync(s => s.Slug == slug);
            if (shop == null) return NotFound(new { error = "Shop not found" });

            var service = await _db.Services.FindAsync(serviceId);
            if (service == null || !service.IsActive)
                return NotFound(new { error = "Service not found" });

            var dayStart = day.Date.AddHours(shop.OpenHour);
            var dayEnd = day.Date.AddHours(shop.CloseHour);

            var busy = await _db.Appointments
                .Where(a => a.StartUtc >= dayStart.ToUniversalTime()
                         && a.StartUtc < dayEnd.ToUniversalTime()
                         && a.Status != AppointmentStatus.Cancelled
                         && a.Status != AppointmentStatus.NoShow)
                .Select(a => new { a.StartUtc, a.EndUtc })
                .ToListAsync();

            var blocked = await _db.BlockedSlots
                .Where(b => b.StartUtc < dayEnd.ToUniversalTime()
                         && b.EndUtc > dayStart.ToUniversalTime())
                .ToListAsync();

            var slots = new List<object>();
            var now = DateTime.UtcNow;
            var step = TimeSpan.FromMinutes(shop.SlotStepMinutes);
            var dur = TimeSpan.FromMinutes(service.DurationMinutes);

            for (var s = dayStart; s + dur <= dayEnd; s += step)
            {
                var e = s + dur;
                var sUtc = s.ToUniversalTime();
                var eUtc = e.ToUniversalTime();
                if (sUtc < now) continue;

                bool clash = busy.Any(b => b.StartUtc < eUtc && sUtc < b.EndUtc)
                          || blocked.Any(b => b.StartUtc < eUtc && sUtc < b.EndUtc);
                if (!clash)
                    slots.Add(new { startUtc = sUtc.ToString("o"), label = s.ToString("HH:mm") });
            }
            return Ok(new { slots });
        }

        [HttpPost("book")]
        public async Task<IActionResult> AnonymousBook([FromBody] PublicBookRequest req)
        {
            if (req == null) return BadRequest(new { error = "Invalid request" });

            var shop = await _db.ShopConfigs.FirstOrDefaultAsync(s => s.Slug == req.Slug);
            if (shop == null) return NotFound(new { error = "Shop not found" });

            var service = await _db.Services.FindAsync(req.ServiceId);
            if (service == null) return BadRequest(new { error = "Service not found" });

            var phone = (req.Phone ?? "").Trim();
            if (!Regex.IsMatch(phone, @"^0\d{9}$"))
                return BadRequest(new { error = "Enter a valid SA phone (e.g. 0821234567)" });

            var name = (req.Name ?? "").Trim();
            if (name.Length < 2) return BadRequest(new { error = "Enter your name" });

            if (!DateTime.TryParse(req.StartUtc, out var parsed))
                return BadRequest(new { error = "Invalid time" });

            var startUtc = parsed.ToUniversalTime();
            var endUtc = startUtc.AddMinutes(service.DurationMinutes);
            if (startUtc < DateTime.UtcNow) return BadRequest(new { error = "That time is in the past" });

            await using var tx = await _db.Database
                .BeginTransactionAsync(System.Data.IsolationLevel.Serializable);
            try
            {
                bool clash = await _db.Appointments.AnyAsync(a =>
                    a.Status != AppointmentStatus.Cancelled &&
                    a.Status != AppointmentStatus.NoShow &&
                    a.StartUtc < endUtc && startUtc < a.EndUtc);
                if (clash)
                {
                    await tx.RollbackAsync();
                    return Conflict(new { error = "Slot just taken. Pick another." });
                }

                var client = await _db.Clients.FirstOrDefaultAsync(c => c.Phone == phone);
                if (client == null)
                {
                    client = new Client { FullName = name, Phone = phone };
                    _db.Clients.Add(client);
                    await _db.SaveChangesAsync();
                }
                else if (client.NoShowCount >= shop.NoShowLimit)
                {
                    await tx.RollbackAsync();
                    return BadRequest(new { error = "Please WhatsApp the salon directly to book." });
                }

                _db.Appointments.Add(new Appointment
                {
                    ClientId = client.Id,
                    ServiceId = service.Id,
                    StartUtc = startUtc,
                    EndUtc = endUtc,
                    Status = AppointmentStatus.Booked,
                    Price = service.Price
                });
                await _db.SaveChangesAsync();
                await tx.CommitAsync();

                var local = TimeZoneInfo.ConvertTimeFromUtc(startUtc, TimeZoneInfo.Local);
                return Ok(new
                {
                    message = $"Booked! {service.Name} on {local:ddd dd MMM} at {local:HH:mm}.",
                    shopPhone = new string(shop.Phone.Where(char.IsDigit).ToArray()),
                    confirmMsg = $"Hi {shop.Name}, I booked {service.Name} on {local:ddd dd MMM} at {local:HH:mm}. My name is {client.FullName}."
                });
            }
            catch (DbUpdateException)
            {
                await tx.RollbackAsync();
                return Conflict(new { error = "Slot taken. Try another." });
            }
        }

        [HttpPost("customer-book")]
        [Authorize(AuthenticationSchemes = CustomerAuth.Scheme)]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CustomerBook([FromBody] CustomerBookRequest req)
        {
            var clientId = CustomerAuth.GetClientId(User);
            if (clientId == null) return Unauthorized(new { error = "Please log in again." });

            var shop = await _db.ShopConfigs.FirstAsync();
            var service = await _db.Services.FindAsync(req.ServiceId);
            if (service == null) return BadRequest(new { error = "Service not found" });

            if (!DateTime.TryParse(req.StartUtc, out var parsed))
                return BadRequest(new { error = "Invalid time" });

            var startUtc = parsed.ToUniversalTime();
            var endUtc = startUtc.AddMinutes(service.DurationMinutes);
            if (startUtc < DateTime.UtcNow) return BadRequest(new { error = "That time is in the past" });

            var client = await _db.Clients.FindAsync(clientId.Value);
            if (client == null) return Unauthorized();
            if (client.NoShowCount >= shop.NoShowLimit)
                return BadRequest(new { error = "Please WhatsApp the salon directly to book." });

            await using var tx = await _db.Database
                .BeginTransactionAsync(System.Data.IsolationLevel.Serializable);
            try
            {
                bool clash = await _db.Appointments.AnyAsync(a =>
                    a.Status != AppointmentStatus.Cancelled &&
                    a.Status != AppointmentStatus.NoShow &&
                    a.StartUtc < endUtc && startUtc < a.EndUtc);
                if (clash)
                {
                    await tx.RollbackAsync();
                    return Conflict(new { error = "Slot just taken. Pick another." });
                }

                _db.Appointments.Add(new Appointment
                {
                    ClientId = client.Id,
                    ServiceId = service.Id,
                    StartUtc = startUtc,
                    EndUtc = endUtc,
                    Status = AppointmentStatus.Booked,
                    Price = service.Price
                });
                await _db.SaveChangesAsync();
                await tx.CommitAsync();

                var local = TimeZoneInfo.ConvertTimeFromUtc(startUtc, TimeZoneInfo.Local);
                return Ok(new
                {
                    message = $"Booked! {service.Name} on {local:ddd dd MMM} at {local:HH:mm}.",
                    shopPhone = new string(shop.Phone.Where(char.IsDigit).ToArray()),
                    confirmMsg = $"Hi {shop.Name}, I booked {service.Name} on {local:ddd dd MMM} at {local:HH:mm}. My name is {client.FullName}."
                });
            }
            catch (DbUpdateException)
            {
                await tx.RollbackAsync();
                return Conflict(new { error = "Slot taken. Try another." });
            }
        }
    }

    public record PublicBookRequest(string Slug, int ServiceId, string StartUtc, string Name, string Phone);
    public record CustomerBookRequest(int ServiceId, string StartUtc);
}