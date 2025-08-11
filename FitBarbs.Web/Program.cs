using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using FitBarbs.Web.Data;
using FitBarbs.Web.Services;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Http;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlite(connectionString));
builder.Services.AddDatabaseDeveloperPageExceptionFilter();

builder.Services.AddDefaultIdentity<IdentityUser>(options => options.SignIn.RequireConfirmedAccount = true)
    .AddRoles<IdentityRole>()
    .AddEntityFrameworkStores<ApplicationDbContext>();
builder.Services.AddControllersWithViews();

// In development, allow insecure cookies so login works over HTTP (localhost)
if (builder.Environment.IsDevelopment())
{
    builder.Services.ConfigureApplicationCookie(options =>
    {
        options.Cookie.SecurePolicy = CookieSecurePolicy.None;
        options.LoginPath = "/Identity/Account/Login";
        options.AccessDeniedPath = "/Identity/Account/AccessDenied";
    });
}

// Increase multipart/form-data limits for large video uploads (up to 2GB)
builder.Services.Configure<FormOptions>(options =>
{
    options.MultipartBodyLengthLimit = 2L * 1024 * 1024 * 1024; // 2GB
});

builder.WebHost.ConfigureKestrel(options =>
{
    options.Limits.MaxRequestBodySize = 2L * 1024 * 1024 * 1024; // 2GB
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseMigrationsEndPoint();
}
else
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

// In development, allow HTTP to simplify local testing
if (!app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
}
app.UseStaticFiles();

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

// Enable attribute-routed controllers (e.g., [HttpGet("/privacy")])
app.MapControllers();

// Fallback redirect for lowercase URL (kept only if attribute route isn't hit in some environments)
// Note: Avoid duplicate route definitions
app.MapGet("/privacy-fallback", () => Results.Redirect("/Home/Privacy"));

app.MapControllerRoute(
    name: "privacy_short",
    pattern: "privacy",
    defaults: new { controller = "Home", action = "Privacy" });

app.MapControllerRoute(
    name: "onas_short",
    pattern: "o-nas",
    defaults: new { controller = "Home", action = "ONas" });

app.MapControllerRoute(
    name: "kontakt_short",
    pattern: "kontakt",
    defaults: new { controller = "Home", action = "Kontakt" });

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");
app.MapRazorPages();

// Ensure database is up to date and seed required data
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    await db.Database.MigrateAsync();
    // Ensure Lessons table has ThumbnailPath even if redundant migrations were removed
    try
    {
        await db.Database.ExecuteSqlRawAsync("ALTER TABLE Lessons ADD COLUMN ThumbnailPath TEXT NULL;");
    }
    catch (Exception)
    {
        // ignore if column already exists
    }
}
await DbSeeder.SeedAsync(app.Services);

app.Run();
