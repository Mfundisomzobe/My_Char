using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using My_Char.Data;
using My_Char.Data.DbInitializer;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();
builder.Services.AddDbContext<SalonDbContext>(o =>
 o.UseSqlServer(builder.Configuration.GetConnectionString("Default")));

builder.Services.AddDefaultIdentity<IdentityUser>(opt =>
{
    opt.SignIn.RequireConfirmedAccount = false;
    opt.Password.RequireNonAlphanumeric = false;
})
.AddRoles<IdentityRole>()
.AddEntityFrameworkStores<SalonDbContext>();
builder.Services.AddAuthentication(options =>
{
    options.DefaultScheme = "AdminOrCustomer";
    options.DefaultChallengeScheme = "AdminOrCustomer";
})
.AddPolicyScheme("AdminOrCustomer", "Admin or Customer", o =>
{
    o.ForwardDefaultSelector = ctx =>
    {
        var path = ctx.Request.Path.Value ?? "";
        if (path.StartsWith("/My") || path.StartsWith("/Customer"))
            return CustomerAuth.Scheme;
        return IdentityConstants.ApplicationScheme;
    };
})
.AddCookie(CustomerAuth.Scheme, o =>
{
    o.LoginPath = "/login";
    o.LogoutPath = "/logout";
    o.AccessDeniedPath = "/login";
    o.Cookie.Name = "MyChair.Customer";
    o.ExpireTimeSpan = TimeSpan.FromDays(30);
    o.SlidingExpiration = true;
});
builder.Services.AddControllersWithViews();
builder.Services.AddRazorPages();


var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthorization();

// Routes - specific first
app.MapControllerRoute("login", "login",
 new { controller = "Customer", action = "Login" });
app.MapControllerRoute("logout", "logout",
 new { controller = "Customer", action = "Logout" });
app.MapControllerRoute("my", "My/{action=MyBookings}/{id?}",
 new { controller = "Customer" });
app.MapControllerRoute("bookPublic", "book/{slug}",
 new { controller = "Public", action = "Book" });
app.MapControllerRoute("default", "{controller=Dashboard}/{action=Index}/{id?}");
app.MapRazorPages();
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<SalonDbContext>();
    var users = scope.ServiceProvider.GetRequiredService<UserManager<IdentityUser>>();
    var roles = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
    var cfg = scope.ServiceProvider.GetRequiredService<IConfiguration>();
    await db.Database.MigrateAsync();
    await SeedData.EnsureSeededAsync(db, users, roles, cfg);

}
app.Run();
