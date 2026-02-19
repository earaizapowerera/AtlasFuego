using System.Globalization;
using System.Text;
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

// CORS for mobile app
builder.Services.AddCors(options =>
    options.AddPolicy("MobileApp", b => b.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader()));

// DbContext for PreRegistros
builder.Services.AddDbContext<AtlasFuegoAdmin.Data.AppDbContext>(options =>
    options.UseSqlServer("Server=dbdev.powerera.com;Database=Atlas2026;User Id=earaiza;Password=VgfN-n4ju?H1Z4#JFRE;TrustServerCertificate=true;"));

builder.WebHost.UseUrls("http://localhost:5077");

var app = builder.Build();

var mobileApiKey = app.Configuration["MobileApiKey"] ?? "FUEGO2026-CHK-a7f3b9c1d4e6";

// PathBase removed - now runs at root of fuegoadmin.powerera.com
var provider = new Microsoft.AspNetCore.StaticFiles.FileExtensionContentTypeProvider();
provider.Mappings[".apk"] = "application/vnd.android.package-archive";
app.UseStaticFiles(new StaticFileOptions { ContentTypeProvider = provider });
app.UseRouting();
app.UseCors("MobileApp");

// API Key middleware for /api/mobile endpoints
app.Use(async (context, next) =>
{
    if (context.Request.Path.StartsWithSegments("/api/mobile"))
    {
        if (!context.Request.Headers.TryGetValue("X-Api-Key", out var key) || key != mobileApiKey)
        {
            context.Response.StatusCode = 401;
            await context.Response.WriteAsJsonAsync(new { error = "API Key inválida" });
            return;
        }
    }
    await next();
});

app.UseUserPortal();
app.UseAuthentication();
app.UseAuthorization();

app.MapRazorPages();
app.MapControllers();

// === Mobile API Endpoints ===

app.MapPost("/api/mobile/buscar", async (MobileQrRequest req, AtlasFuegoAdmin.Data.AppDbContext db) =>
{
    if (string.IsNullOrWhiteSpace(req.QrContent))
        return Results.Json(new { success = false, message = "QR vacío" });

    var registros = await db.PreRegistros.ToListAsync();
    var registro = registros.FirstOrDefault(r =>
        $"{r.FechaRegistro:yyyyMMdd HH:mm:ss} {r.NombreCompleto}" == req.QrContent);

    // Fuzzy match if exact match fails
    if (registro == null)
    {
        var qrDigits = new string(req.QrContent.Where(char.IsDigit).ToArray());
        if (qrDigits.Length >= 14)
        {
            var qrDateDigits = qrDigits[..14];
            var candidates = registros.Where(r =>
            {
                var rd = new string(r.FechaRegistro.ToString("yyyyMMddHHmmss").Where(char.IsDigit).ToArray());
                return rd.Length >= 14 && rd[..14] == qrDateDigits;
            }).ToList();

            if (candidates.Count == 1)
                registro = candidates[0];
            else if (candidates.Count > 1)
            {
                var qrName = req.QrContent.Length > 17 ? StripToAscii(req.QrContent[17..]) : "";
                registro = candidates.OrderByDescending(r => LetterOverlap(StripToAscii(r.NombreCompleto), qrName)).First();
            }
        }
    }

    if (registro == null)
        return Results.Json(new { success = false, message = "Registro no encontrado" });

    return Results.Json(new
    {
        success = true,
        id = registro.Id,
        nombre = registro.NombreCompleto,
        empresa = registro.Empresa,
        foto = registro.FotoRuta != null ? $"https://fuego.powerera.com{registro.FotoRuta}" : (string?)null,
        confirmado = registro.Confirmado,
        fechaConfirmacion = registro.FechaConfirmacion?.ToString("o") // ISO 8601 UTC
    });
});

app.MapPost("/api/mobile/confirmar", async (MobileConfirmarRequest req, AtlasFuegoAdmin.Data.AppDbContext db) =>
{
    var registro = await db.PreRegistros.FindAsync(req.Id);
    if (registro == null)
        return Results.Json(new { success = false, message = "Registro no encontrado" });

    if (registro.Confirmado)
        return Results.Json(new { success = false, message = "Ya tiene check-in",
            fechaConfirmacion = registro.FechaConfirmacion?.ToString("o") });

    registro.Confirmado = true;
    registro.FechaConfirmacion = DateTime.UtcNow;
    await db.SaveChangesAsync();

    return Results.Json(new { success = true, nombre = registro.NombreCompleto });
});

app.MapGet("/api/mobile/stats", async (AtlasFuegoAdmin.Data.AppDbContext db) =>
{
    var registros = await db.PreRegistros.ToListAsync();
    return Results.Json(new
    {
        total = registros.Count,
        asistiran = registros.Count(r => r.Asistira),
        conFoto = registros.Count(r => r.FotoRuta != null),
        checkedIn = registros.Count(r => r.Confirmado)
    });
});

app.MapGet("/api/mobile/registros", async (AtlasFuegoAdmin.Data.AppDbContext db) =>
{
    var registros = await db.PreRegistros.Select(r => new
    {
        r.Id,
        r.NombreCompleto,
        r.Empresa,
        r.Email,
        r.Asistira,
        r.FotoRuta,
        fechaRegistro = r.FechaRegistro.ToString("yyyyMMdd HH:mm:ss"),
        r.Confirmado,
        fechaConfirmacion = r.FechaConfirmacion != null
            ? r.FechaConfirmacion.Value.ToString("o") : null
    }).ToListAsync();

    return Results.Json(new { success = true, count = registros.Count, registros });
});

app.MapGet("/api/mobile/health", () =>
    Results.Json(new { status = "ok", timestamp = DateTime.UtcNow }));

// === End Mobile API ===

await app.Services.InitializeUserPortalDatabase();

// Auto-create columns if not exists
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AtlasFuegoAdmin.Data.AppDbContext>();
    await db.Database.ExecuteSqlRawAsync(@"
        IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS
                       WHERE TABLE_NAME='PreRegistros' AND COLUMN_NAME='CorreoNoFotoEnviado')
        BEGIN
            ALTER TABLE PreRegistros ADD CorreoNoFotoEnviado bit NOT NULL DEFAULT 0
        END");
    await db.Database.ExecuteSqlRawAsync(@"
        IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS
                       WHERE TABLE_NAME='PreRegistros' AND COLUMN_NAME='CorreoRecordatorioEnviado')
        BEGIN
            ALTER TABLE PreRegistros ADD CorreoRecordatorioEnviado bit NOT NULL DEFAULT 0
        END");
}

app.Run();

// Fuzzy QR match helpers
static string StripToAscii(string s)
{
    var n = s.Normalize(NormalizationForm.FormD);
    var sb = new StringBuilder(n.Length);
    foreach (var c in n)
    {
        if (char.GetUnicodeCategory(c) == UnicodeCategory.NonSpacingMark) continue;
        if (char.IsLetter(c)) sb.Append(char.ToLowerInvariant(c));
    }
    return sb.ToString();
}

static int LetterOverlap(string dbName, string qrName)
{
    int di = 0, score = 0;
    foreach (var c in qrName)
    {
        while (di < dbName.Length)
        {
            if (dbName[di] == c) { score++; di++; break; }
            di++;
        }
        if (di >= dbName.Length) break;
    }
    return score;
}

record MobileQrRequest(string QrContent);
record MobileConfirmarRequest(int Id);
