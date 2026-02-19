namespace AtlasFuegoAdmin.Models;

public class PreRegistro
{
    public int Id { get; set; }
    public string NombreCompleto { get; set; } = "";
    public string Empresa { get; set; } = "";
    public string Email { get; set; } = "";
    public bool Asistira { get; set; } = true;
    public string? FotoRuta { get; set; }
    public DateTime FechaRegistro { get; set; } = DateTime.Now;
    public bool Confirmado { get; set; } = false;
    public DateTime? FechaConfirmacion { get; set; }
    public bool CorreoNoFotoEnviado { get; set; } = false;
    public bool CorreoRecordatorioEnviado { get; set; } = false;
}
