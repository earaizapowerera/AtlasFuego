using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using AtlasFuegoAdmin.Data;
using AtlasFuegoAdmin.Models;

namespace AtlasFuegoAdmin.Pages;

[Authorize]
public class CheckinModel : PageModel
{
    private readonly AppDbContext _db;

    public CheckinModel(AppDbContext db)
    {
        _db = db;
    }

    public void OnGet() { }

    public async Task<IActionResult> OnPostBuscarAsync([FromBody] QrRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.QrContent))
            return new JsonResult(new { success = false, message = "QR vacío" });

        // QR format: "yyyyMMdd HH:mm:ss NombreCompleto"
        var registros = await _db.PreRegistros.ToListAsync();
        var registro = registros.FirstOrDefault(r =>
        {
            var expected = $"{r.FechaRegistro:yyyyMMdd HH:mm:ss} {r.NombreCompleto}";
            return expected == request.QrContent;
        });

        if (registro == null)
            return new JsonResult(new { success = false, message = "Registro no encontrado" });

        return new JsonResult(new
        {
            success = true,
            id = registro.Id,
            nombre = registro.NombreCompleto,
            empresa = registro.Empresa,
            foto = registro.FotoRuta != null ? $"https://fuego.powerera.com{registro.FotoRuta}" : null,
            confirmado = registro.Confirmado,
            fechaConfirmacion = registro.FechaConfirmacion?.ToString("dd/MM/yyyy HH:mm")
        });
    }

    public async Task<IActionResult> OnPostConfirmarAsync([FromBody] ConfirmarRequest request)
    {
        var registro = await _db.PreRegistros.FindAsync(request.Id);
        if (registro == null)
            return new JsonResult(new { success = false, message = "Registro no encontrado" });

        registro.Confirmado = true;
        registro.FechaConfirmacion = DateTime.Now;
        await _db.SaveChangesAsync();

        return new JsonResult(new { success = true, nombre = registro.NombreCompleto });
    }

    public class QrRequest { public string QrContent { get; set; } = ""; }
    public class ConfirmarRequest { public int Id { get; set; } }
}
