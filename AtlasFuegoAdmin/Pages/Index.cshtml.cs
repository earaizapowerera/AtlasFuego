using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using ClosedXML.Excel;
using AtlasFuegoAdmin.Data;
using AtlasFuegoAdmin.Models;
using System.Security.Claims;

namespace AtlasFuegoAdmin.Pages;

[Authorize]
public class IndexModel : PageModel
{
    private readonly AppDbContext _db;

    public IndexModel(AppDbContext db)
    {
        _db = db;
    }

    public List<PreRegistro> Registros { get; set; } = new();
    public string DisplayName { get; set; } = "";
    public string FotoBaseUrl { get; set; } = "";

    public async Task OnGetAsync()
    {
        DisplayName = User.FindFirstValue("DisplayName") ?? User.FindFirstValue(ClaimTypes.Name) ?? "Admin";
        Registros = await _db.PreRegistros.OrderByDescending(r => r.FechaRegistro).ToListAsync();

        FotoBaseUrl = "https://fuego.powerera.com";
    }

    public async Task<IActionResult> OnGetExportExcelAsync()
    {
        var registros = await _db.PreRegistros.OrderByDescending(r => r.FechaRegistro).ToListAsync();

        using var workbook = new XLWorkbook();
        var ws = workbook.Worksheets.Add("Registros");

        ws.Cell(1, 1).Value = "Nombre";
        ws.Cell(1, 2).Value = "Empresa";
        ws.Cell(1, 3).Value = "Email";
        ws.Cell(1, 4).Value = "Tiene Foto";
        ws.Cell(1, 5).Value = "Asistirá";
        ws.Cell(1, 6).Value = "Check-in";
        ws.Cell(1, 7).Value = "Fecha Registro";

        var headerRange = ws.Range(1, 1, 1, 7);
        headerRange.Style.Font.Bold = true;
        headerRange.Style.Fill.BackgroundColor = XLColor.DarkSlateGray;
        headerRange.Style.Font.FontColor = XLColor.White;

        for (int i = 0; i < registros.Count; i++)
        {
            var r = registros[i];
            ws.Cell(i + 2, 1).Value = r.NombreCompleto;
            ws.Cell(i + 2, 2).Value = r.Empresa;
            ws.Cell(i + 2, 3).Value = r.Email;
            ws.Cell(i + 2, 4).Value = r.FotoRuta != null ? 1 : 0;
            ws.Cell(i + 2, 5).Value = r.Asistira ? "Sí" : "No";
            ws.Cell(i + 2, 6).Value = r.Confirmado ? "Llegó" : "Pendiente";
            ws.Cell(i + 2, 7).Value = r.FechaRegistro.ToString("dd/MM/yyyy HH:mm");
        }

        ws.Columns().AdjustToContents();

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        stream.Position = 0;

        return File(stream.ToArray(),
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            $"Registros_FUEGO_{DateTime.Now:yyyyMMdd}.xlsx");
    }
}
