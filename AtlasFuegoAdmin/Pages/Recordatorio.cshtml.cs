using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using AtlasFuegoAdmin.Data;
using AtlasFuegoAdmin.Models;
using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;
using QRCoder;

namespace AtlasFuegoAdmin.Pages;

[Authorize]
[IgnoreAntiforgeryToken]
public class RecordatorioModel : PageModel
{
    private readonly AppDbContext _db;
    private readonly IConfiguration _config;
    private readonly IWebHostEnvironment _env;

    public RecordatorioModel(AppDbContext db, IConfiguration config, IWebHostEnvironment env)
    {
        _db = db;
        _config = config;
        _env = env;
    }

    public List<PreRegistro> Registros { get; set; } = new();
    public int TotalConFoto => Registros.Count;
    public int Enviados => Registros.Count(r => r.CorreoRecordatorioEnviado);
    public int Pendientes => Registros.Count(r => !r.CorreoRecordatorioEnviado);

    public async Task OnGetAsync()
    {
        Registros = await _db.PreRegistros
            .Where(r => r.FotoRuta != null && r.Asistira)
            .OrderByDescending(r => r.FechaRegistro)
            .ToListAsync();
    }

    public async Task<IActionResult> OnPostEnviarAsync([FromBody] EnviarRequest request)
    {
        var registro = await _db.PreRegistros.FindAsync(request.Id);
        if (registro == null)
            return new JsonResult(new { success = false, message = "Registro no encontrado" });

        try
        {
            await EnviarCorreoRecordatorio(registro);
            registro.CorreoRecordatorioEnviado = true;
            await _db.SaveChangesAsync();
            return new JsonResult(new { success = true, nombre = registro.NombreCompleto });
        }
        catch (Exception ex)
        {
            return new JsonResult(new { success = false, message = ex.Message });
        }
    }

    public async Task<IActionResult> OnPostEnviarSeleccionadosAsync([FromBody] EnviarSeleccionadosRequest request)
    {
        if (request.Ids == null || request.Ids.Count == 0)
            return new JsonResult(new { success = false, message = "No se seleccionaron registros" });

        var registros = await _db.PreRegistros
            .Where(r => request.Ids.Contains(r.Id) && !r.CorreoRecordatorioEnviado)
            .ToListAsync();

        int enviados = 0;
        var errores = new List<string>();

        foreach (var registro in registros)
        {
            try
            {
                await EnviarCorreoRecordatorio(registro);
                registro.CorreoRecordatorioEnviado = true;
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

    private async Task EnviarCorreoRecordatorio(PreRegistro registro)
    {
        // Generate QR with same format as original invitation
        var fechaStr = registro.FechaRegistro.ToString("yyyyMMdd HH:mm:ss");
        var qrContent = $"{fechaStr} {registro.NombreCompleto}";

        using var qrGenerator = new QRCodeGenerator();
        var qrCodeData = qrGenerator.CreateQrCode(qrContent, QRCodeGenerator.ECCLevel.M);
        using var qrCode = new PngByteQRCode(qrCodeData);
        var qrBytes = qrCode.GetGraphic(12);

        // Load header image
        var logoPath = Path.Combine(_env.WebRootPath, "images", "header-email.jpg");
        byte[]? logoBytes = null;
        if (System.IO.File.Exists(logoPath))
            logoBytes = await System.IO.File.ReadAllBytesAsync(logoPath);

        var nombreMayus = registro.NombreCompleto.ToUpper();
        var empresaMayus = registro.Empresa.ToUpper();

        var message = new MimeMessage();
        message.From.Add(new MailboxAddress("Cóctel de Agentes FUEGO - Atlas", "delivery@powerera.com"));
        message.To.Add(new MailboxAddress(nombreMayus, registro.Email));
        message.Subject = "¡¡NOS VEMOS HOY!! - Cóctel de Agentes FUEGO";

        var bodyBuilder = new BodyBuilder();

        var logoImgTag = logoBytes != null
            ? "<img src='cid:logo-fuego' alt='Cóctel de Agentes FUEGO - Atlas' width='600' style='width: 100%; height: auto; display: block; margin: 0 auto;' />"
            : "<h1 style='text-align: center; color: #d4a574; margin: 0;'>CÓCTEL DE AGENTES FUEGO</h1>";

        bodyBuilder.HtmlBody = $@"
<!DOCTYPE html>
<html>
<head>
    <meta charset='utf-8'>
    <meta name='viewport' content='width=device-width, initial-scale=1.0'>
</head>
<body style='margin: 0; padding: 0; font-family: Arial, Helvetica, sans-serif; background-color: #000000;' bgcolor='#000000'>
    <table role='presentation' width='100%' cellspacing='0' cellpadding='0' border='0' style='background-color: #000000;' bgcolor='#000000'>
        <tr>
            <td align='center' style='padding: 20px 0;'>
                <table role='presentation' width='600' cellspacing='0' cellpadding='0' border='0' style='max-width: 600px; width: 100%; background-color: #000000;' bgcolor='#000000'>
                    <!-- Header con imagen completa full width -->
                    <tr>
                        <td align='center' style='padding: 0; background-color: #000000;' bgcolor='#000000'>
                            {logoImgTag}
                        </td>
                    </tr>
                    <!-- Saludo -->
                    <tr>
                        <td style='background-color: #000000; padding: 30px 25px 10px 25px;' bgcolor='#000000'>
                            <h2 style='color: #d4a574; text-align: center; margin: 0 0 20px 0; font-size: 24px;'>¡¡NOS VEMOS HOY!! {nombreMayus}</h2>
                            <p style='color: #e8dcd0; text-align: center; font-size: 16px; margin: 0 0 25px 0; line-height: 1.5;'>
                                Te esperamos esta noche. Presenta tu código QR de acceso al llegar.
                            </p>
                        </td>
                    </tr>
                    <!-- Datos del invitado -->
                    <tr>
                        <td style='background-color: #000000; padding: 0 25px 20px 25px;' bgcolor='#000000'>
                            <table role='presentation' width='100%' cellspacing='0' cellpadding='0' border='0' style='background-color: #111111;' bgcolor='#111111'>
                                <tr>
                                    <td style='padding: 20px;'>
                                        <p style='color: #b8967a; margin: 5px 0; font-size: 14px;'><strong style='color: #d4a574;'>Invitado:</strong> {nombreMayus}</p>
                                        <p style='color: #b8967a; margin: 5px 0; font-size: 14px;'><strong style='color: #d4a574;'>Empresa:</strong> {empresaMayus}</p>
                                    </td>
                                </tr>
                            </table>
                        </td>
                    </tr>
                    <!-- QR Code -->
                    <tr>
                        <td style='background-color: #000000; padding: 10px 25px 20px 25px; text-align: center;' bgcolor='#000000'>
                            <p style='color: #d4a574; margin: 0 0 15px 0; font-size: 14px;'>Presenta este código QR al llegar:</p>
                            <table role='presentation' cellspacing='0' cellpadding='0' border='0' align='center'>
                                <tr>
                                    <td style='background-color: #ffffff; padding: 15px;' bgcolor='#ffffff'>
                                        <img src='cid:qr-code' alt='QR Code' width='200' height='200' style='display: block;' />
                                    </td>
                                </tr>
                            </table>
                            <p style='color: #c17f59; font-size: 11px; font-weight: bold; text-transform: uppercase; letter-spacing: 1px; margin: 15px 0 0 0;'>Invitación personal e intransferible</p>
                        </td>
                    </tr>
                    <!-- Detalles del evento -->
                    <tr>
                        <td style='background-color: #000000; padding: 0 25px 25px 25px;' bgcolor='#000000'>
                            <table role='presentation' width='100%' cellspacing='0' cellpadding='0' border='0' style='background-color: #111111; border: 1px solid #8b4513;' bgcolor='#111111'>
                                <tr>
                                    <td style='padding: 25px; text-align: center;'>
                                        <p style='color: #d4a574; font-size: 18px; font-weight: bold; margin: 0 0 15px 0;'>DETALLES DEL EVENTO</p>
                                        <p style='color: #e8dcd0; font-size: 16px; margin: 8px 0;'><strong>19 de Febrero de 2026</strong></p>
                                        <p style='color: #e8dcd0; font-size: 16px; margin: 8px 0;'><strong>19:00 hrs</strong></p>
                                        <p style='color: #b8967a; font-size: 14px; margin: 15px 0 5px 0;'>Terraza Virreyes</p>
                                        <p style='color: #b8967a; font-size: 14px; margin: 5px 0;'>Hotel Camino Real Polanco</p>
                                        <p style='color: #8b7355; font-size: 12px; margin: 15px 0 0 0;'>Mariano Escobedo 700, Polanco, Miguel Hidalgo, 11560 CDMX</p>
                                    </td>
                                </tr>
                            </table>
                        </td>
                    </tr>
                    <!-- Footer -->
                    <tr>
                        <td style='background-color: #111111; padding: 20px; text-align: center;' bgcolor='#111111'>
                            <p style='color: #555555; font-size: 12px; margin: 0;'>Este correo fue enviado automáticamente. No responder a esta dirección.</p>
                        </td>
                    </tr>
                </table>
            </td>
        </tr>
    </table>
</body>
</html>";

        // Attach QR as embedded resource
        var qrImage = bodyBuilder.LinkedResources.Add("qr-code.png", qrBytes, new ContentType("image", "png"));
        qrImage.ContentId = "qr-code";

        // Attach logo as embedded resource
        if (logoBytes != null)
        {
            var logoImage = bodyBuilder.LinkedResources.Add("logo-fuego.jpg", logoBytes, new ContentType("image", "jpeg"));
            logoImage.ContentId = "logo-fuego";
        }

        message.Body = bodyBuilder.ToMessageBody();

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

    public class EnviarSeleccionadosRequest
    {
        public List<int> Ids { get; set; } = new();
    }
}
