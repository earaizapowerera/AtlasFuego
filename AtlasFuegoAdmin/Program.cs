using Microsoft.EntityFrameworkCore;
using PowerEra.UserPortal.Component.Extensions;

var builder = WebApplication.CreateBuilder(args);

// UserPortal Component for authentication
builder.Services.AddUserPortalComponent(options =>
{
    options.ConnectionString = "Server=dbdev.powerera.com;Database=Atlas2026;User Id=earaiza;Password=VgfN-n4ju?H1Z4#JFRE;Encrypt=no;TrustServerCertificate=yes;";
    options.AutoInitializeDatabase = true;
    options.DefaultAdminPassword = "u38a8fk3j0!";
    options.SessionExpirationMinutes = 120;
});

builder.Services.AddRazorPages();

// DbContext for PreRegistros
builder.Services.AddDbContext<AtlasFuegoAdmin.Data.AppDbContext>(options =>
    options.UseSqlServer("Server=dbdev.powerera.com;Database=Atlas2026;User Id=earaiza;Password=VgfN-n4ju?H1Z4#JFRE;TrustServerCertificate=true;"));

builder.WebHost.UseUrls("http://localhost:5077");

var app = builder.Build();

// PathBase removed - now runs at root of fuegoadmin.powerera.com
app.UseStaticFiles();
app.UseRouting();

app.UseUserPortal();
app.UseAuthentication();
app.UseAuthorization();

app.MapRazorPages();
app.MapControllers();

await app.Services.InitializeUserPortalDatabase();

app.Run();
