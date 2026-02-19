using Microsoft.AspNetCore.Mvc.RazorPages;

namespace AtlasFuegoAdmin.Pages;

public class AppModel : PageModel
{
    private readonly IWebHostEnvironment _env;

    public AppModel(IWebHostEnvironment env) => _env = env;

    public List<ApkFile> ApkFiles { get; set; } = new();
    public ApkFile? Latest { get; set; }

    public void OnGet()
    {
        var downloadsPath = Path.Combine(_env.WebRootPath, "downloads");
        if (!Directory.Exists(downloadsPath)) return;

        ApkFiles = Directory.GetFiles(downloadsPath, "*.apk")
            .Where(f => Path.GetFileName(f) != "FUEGOCheckin.apk") // exclude the "latest" alias
            .Select(f => new ApkFile
            {
                FileName = Path.GetFileName(f),
                Size = new FileInfo(f).Length,
                Date = new FileInfo(f).LastWriteTimeUtc,
                Version = ExtractVersion(Path.GetFileName(f))
            })
            .OrderByDescending(f => f.Date)
            .ToList();

        Latest = ApkFiles.FirstOrDefault();
    }

    private static string ExtractVersion(string fileName)
    {
        // FUEGOCheckin_v1_0_14.apk → 1.0.14
        var name = Path.GetFileNameWithoutExtension(fileName);
        var vIdx = name.IndexOf("_v", StringComparison.OrdinalIgnoreCase);
        if (vIdx < 0) vIdx = name.IndexOf("v", StringComparison.OrdinalIgnoreCase);
        if (vIdx < 0) return name;

        var vPart = name[(vIdx + (name[vIdx] == '_' ? 2 : 1))..];
        return vPart.Replace('_', '.');
    }

    public record ApkFile
    {
        public string FileName { get; init; } = "";
        public long Size { get; init; }
        public DateTime Date { get; init; }
        public string Version { get; init; } = "";
    }
}
