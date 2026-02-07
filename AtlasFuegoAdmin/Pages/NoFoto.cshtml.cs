using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using AtlasFuegoAdmin.Data;
using AtlasFuegoAdmin.Models;
using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;

namespace AtlasFuegoAdmin.Pages;

[Authorize]
[IgnoreAntiforgeryToken]
public class NoFotoModel : PageModel
{
    private readonly AppDbContext _db;
    private readonly IConfiguration _config;

    public NoFotoModel(AppDbContext db, IConfiguration config)
    {
        _db = db;
        _config = config;
    }

    public List<PreRegistro> SinFoto { get; set; } = new();
    public int TotalSinFoto => SinFoto.Count;
    public int CorreoEnviado => SinFoto.Count(r => r.CorreoNoFotoEnviado);
    public int CorreoPendiente => SinFoto.Count(r => !r.CorreoNoFotoEnviado);

    public async Task OnGetAsync()
    {
        SinFoto = await _db.PreRegistros
            .Where(r => r.FotoRuta == null && r.Asistira)
            .OrderByDescending(r => r.FechaRegistro)
            .ToListAsync();
    }

    public async Task<IActionResult> OnPostEnviarAsync([FromBody] EnviarRequest request)
    {
        var registro = await _db.PreRegistros.FindAsync(request.Id);
        if (registro == null)
            return new JsonResult(new { success = false, message = "Registro no encontrado" });

        if (registro.FotoRuta != null)
            return new JsonResult(new { success = false, message = "Este registro ya tiene foto" });

        try
        {
            await EnviarCorreoNoFoto(registro);
            registro.CorreoNoFotoEnviado = true;
            await _db.SaveChangesAsync();
            return new JsonResult(new { success = true, nombre = registro.NombreCompleto });
        }
        catch (Exception ex)
        {
            return new JsonResult(new { success = false, message = ex.Message });
        }
    }

    public async Task<IActionResult> OnPostEnviarTodosAsync()
    {
        var pendientes = await _db.PreRegistros
            .Where(r => r.FotoRuta == null && r.Asistira && !r.CorreoNoFotoEnviado)
            .ToListAsync();

        int enviados = 0;
        var errores = new List<string>();

        foreach (var registro in pendientes)
        {
            try
            {
                await EnviarCorreoNoFoto(registro);
                registro.CorreoNoFotoEnviado = true;
                enviados++;
            }
            catch (Exception ex)
            {
                errores.Add($"{registro.NombreCompleto}: {ex.Message}");
            }
        }

        await _db.SaveChangesAsync();
        return new JsonResult(new { success = true, enviados, errores });
    }

    private async Task EnviarCorreoNoFoto(PreRegistro registro)
    {
        var message = new MimeMessage();
        message.From.Add(new MailboxAddress("Atlas FUEGO", "delivery@powerera.com"));
        message.To.Add(new MailboxAddress(registro.NombreCompleto, registro.Email));
        message.Subject = "Completa tu registro - Cóctel de Agentes FUEGO 2026";

        var html = $@"
<!DOCTYPE html>
<html>
<head><meta charset='utf-8'></head>
<body style='margin:0; padding:0; background-color:#1a1a1a; font-family:Arial,sans-serif;'>
<table width='100%' cellpadding='0' cellspacing='0' style='background-color:#1a1a1a;'>
<tr><td align='center' style='padding:30px 0;'>
<table width='600' cellpadding='0' cellspacing='0' style='background-color:#2d2d2d; border-radius:12px; overflow:hidden;'>
    <tr>
        <td style='background: linear-gradient(135deg, #cc4400, #ff6600); padding:30px; text-align:center;'>
            <h1 style='color:#fff; margin:0; font-size:28px;'>ATLAS FUEGO</h1>
            <p style='color:#ffe0cc; margin:5px 0 0; font-size:14px;'>Cóctel de Agentes 2026</p>
        </td>
    </tr>
    <tr>
        <td style='padding:30px; color:#e0e0e0;'>
            <p style='font-size:16px; margin-bottom:20px;'>Estimado(a) <strong style='color:#ff6600;'>{System.Net.WebUtility.HtmlEncode(registro.NombreCompleto)}</strong>,</p>
            <p style='font-size:15px; line-height:1.6;'>Tu registro para el <strong>Cóctel de Agentes 2026</strong> de Seguros Atlas no se ha completado correctamente.</p>
            <p style='font-size:15px; line-height:1.6;'>Para poder asistir al evento, es indispensable que finalices tu registro incluyendo tu <strong style='color:#ff6600;'>fotografía</strong>.</p>
            <p style='font-size:15px; line-height:1.6;'>Por favor, realiza nuevamente tu registro en el siguiente enlace:</p>
            <table width='100%' cellpadding='0' cellspacing='0' style='margin:25px 0;'>
                <tr><td align='center'>
                    <a href='https://fuego.powerera.com/' style='display:inline-block; background:#cc4400; color:#fff; text-decoration:none; padding:14px 40px; border-radius:8px; font-size:16px; font-weight:bold;'>Completar Registro</a>
                </td></tr>
            </table>
            <p style='font-size:15px; line-height:1.6;'>¡Te esperamos!</p>
        </td>
    </tr>
    <tr>
        <td style='background-color:#222; padding:20px; text-align:center;'>
            <p style='color:#888; font-size:12px; margin:0;'>Seguros Atlas &bull; Cóctel de Agentes FUEGO 2026</p>
            <p style='color:#666; font-size:11px; margin:5px 0 0;'>19 de febrero de 2026 &bull; Terraza Virreyes, Hotel Camino Real Polanco</p>
        </td>
    </tr>
</table>
</td></tr>
</table>
</body>
</html>";

        message.Body = new TextPart("html") { Text = html };

        using var smtp = new SmtpClient();
        await smtp.ConnectAsync("smtp.sendgrid.net", 587, SecureSocketOptions.StartTls);
        await smtp.AuthenticateAsync("apikey", _config["SendGrid:ApiKey"]!);
        await smtp.SendAsync(message);
        await smtp.DisconnectAsync(true);
    }

    public class EnviarRequest
    {
        public int Id { get; set; }
    }
}
