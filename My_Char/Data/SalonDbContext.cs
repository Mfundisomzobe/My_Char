using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using My_Char.Models.Entities;

namespace My_Char.Data
{
    public class SalonDbContext: IdentityDbContext

    {
        public SalonDbContext(DbContextOptions<SalonDbContext> options) : base(options) { }
        public DbSet<Client> Clients => Set<Client>();
        public DbSet<Service> Services => Set<Service>();
        public DbSet<Appointment> Appointments => Set<Appointment>();
        public DbSet<BlockedSlot> BlockedSlots => Set<BlockedSlot>();
        public DbSet<ShopConfig> ShopConfigs => Set<ShopConfig>();
        protected override void OnModelCreating(ModelBuilder b)
        {
            base.OnModelCreating(b);
            b.Entity<Client>().HasIndex(c => c.Phone).IsUnique();
            b.Entity<Appointment>().HasIndex(a => new { a.StartUtc, a.EndUtc });
            b.Entity<Appointment>().Property(a => a.Price).HasPrecision(10, 2);
            b.Entity<Service>().Property(s => s.Price).HasPrecision(10, 2);
            b.Entity<Appointment>()
            .HasOne(a => a.Client)
            .WithMany(c => c.Appointments)
            .HasForeignKey(a => a.ClientId)
            .OnDelete(DeleteBehavior.Restrict);
            b.Entity<Appointment>()
            .HasOne(a => a.Service)
            .WithMany()
            .HasForeignKey(a => a.ServiceId)
            .OnDelete(DeleteBehavior.Restrict);
            b.Entity<ShopConfig>().HasIndex(s => s.Slug).IsUnique();
        }
    }
}
