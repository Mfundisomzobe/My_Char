using Microsoft.AspNetCore.Identity;
using My_Char.Models.Entities;

namespace My_Char.Data
{
    public static class SeedData
    {
        public static async Task EnsureSeededAsync(
 SalonDbContext db,
 UserManager<IdentityUser> users,
 RoleManager<IdentityRole> roles,
 IConfiguration config)
        {
            if (!db.ShopConfigs.Any())
            {
                db.ShopConfigs.Add(new ShopConfig
                {
                    Name = "Nolu's Salon",
                    OwnerName = "Noluthando",
                    Phone = "0820000000",
                    Slug = "nolus-salon",
                    OpenHour = 8,
                    CloseHour = 18,
                    SlotStepMinutes = 30,
                    NoShowLimit = 3
                });
                await db.SaveChangesAsync();
            }
            if (!db.Services.Any())
            {
                db.Services.AddRange(
                new Service { Name = "Knotless", DurationMinutes = 180, Price = 350, IsActive = true },
                new Service { Name = "Braids", DurationMinutes = 150, Price = 300, IsActive = true },
                new Service { Name = "Relax", DurationMinutes = 90, Price = 250, IsActive = true });
                await db.SaveChangesAsync();
            }
            if (!await roles.RoleExistsAsync("Admin"))
                await roles.CreateAsync(new IdentityRole("Admin"));
            var email = config["Seed:AdminEmail"] ?? "owner@mychair.local";
            var pass = config["Seed:AdminPassword"] ?? "Admin#12345";
            var admin = await users.FindByEmailAsync(email);
            if (admin == null)
            {
                admin = new IdentityUser { UserName = email, Email = email, EmailConfirmed = true };
                await users.CreateAsync(admin, pass);
                await users.AddToRoleAsync(admin, "Admin");
            }
        }
    }
}
