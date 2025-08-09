using FitBarbs.Web.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace FitBarbs.Web.Services;

public static class DbSeeder
{
    public static async Task SeedAsync(IServiceProvider services)
    {
        using var scope = services.CreateScope();
        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<IdentityUser>>();

        // Ensure roles
        if (!await roleManager.RoleExistsAsync(ApplicationRoles.Instructor))
            await roleManager.CreateAsync(new IdentityRole(ApplicationRoles.Instructor));
        if (!await roleManager.RoleExistsAsync(ApplicationRoles.User))
            await roleManager.CreateAsync(new IdentityRole(ApplicationRoles.User));

        // Ensure Barbie account (instructor)
        var barbieEmail = "barbie@fitbarbs.app";
        var barbie = await userManager.Users.FirstOrDefaultAsync(u => u.Email == barbieEmail);
        if (barbie == null)
        {
            barbie = new IdentityUser
            {
                UserName = barbieEmail,
                Email = barbieEmail,
                EmailConfirmed = true
            };
            var result = await userManager.CreateAsync(barbie, "Barbie!123");
            if (result.Succeeded)
            {
                await userManager.AddToRoleAsync(barbie, ApplicationRoles.Instructor);
            }
        }
    }
}


