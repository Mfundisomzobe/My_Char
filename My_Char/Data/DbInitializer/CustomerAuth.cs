using Microsoft.AspNetCore.Authentication;
using My_Char.Models.Entities;
using System.Security.Claims;

namespace My_Char.Data.DbInitializer
{
    public static  class CustomerAuth
    {
        public const string Scheme = "CustomerCookie";
        public static async Task SignInAsync(HttpContext ctx, Client client)
        {
            var claims = new List<Claim>
 {
 new(ClaimTypes.NameIdentifier, client.Id.ToString()),
 new(ClaimTypes.Name, client.FullName),
 new("Phone", client.Phone),
 new(ClaimTypes.Role, "Customer"),
 };
            var identity = new ClaimsIdentity(claims, Scheme);
            await ctx.SignInAsync(Scheme, new ClaimsPrincipal(identity),
            new AuthenticationProperties
            {
                IsPersistent = true,
                ExpiresUtc = DateTimeOffset.UtcNow.AddDays(30)
            });
        }
        public static int? GetClientId(ClaimsPrincipal user)
        {
            var val = user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            return int.TryParse(val, out var id) ? id : null;
        }

    }
}
