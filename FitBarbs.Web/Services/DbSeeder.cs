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
            if (!result.Succeeded)
            {
                throw new Exception($"Failed to create seed user {barbieEmail}: {string.Join(", ", result.Errors.Select(e => e.Description))}");
            }
        }
        else
        {
            // Ensure the existing user is confirmed and not locked out
            if (!barbie.EmailConfirmed)
            {
                barbie.EmailConfirmed = true;
                await userManager.UpdateAsync(barbie);
            }
#pragma warning disable CS0618
            // Clear potential lockout issues (IdentityUser has these props)
            barbie.LockoutEnd = null;
            barbie.AccessFailedCount = 0;
            await userManager.UpdateAsync(barbie);
#pragma warning restore CS0618

            // Reset password to known dev password to avoid invalid login loops
            var resetToken = await userManager.GeneratePasswordResetTokenAsync(barbie);
            var resetResult = await userManager.ResetPasswordAsync(barbie, resetToken, "Barbie!1234!");
            if (!resetResult.Succeeded)
            {
                throw new Exception($"Failed to reset seed user password: {string.Join(", ", resetResult.Errors.Select(e => e.Description))}");
            }
            if (!await userManager.CheckPasswordAsync(barbie, "Barbie!1234!"))
            {
                throw new Exception("Password check failed after reset for barbie@fitbarbs.app");
            }
        }
        // Ensure the Barbie account is in the Instructor role even if it already existed
        if (!await userManager.IsInRoleAsync(barbie, ApplicationRoles.Instructor))
        {
            await userManager.AddToRoleAsync(barbie, ApplicationRoles.Instructor);
        }
    }
}


