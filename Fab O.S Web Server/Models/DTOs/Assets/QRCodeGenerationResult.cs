using FabOS.WebServer.Models.Entities.Assets;

namespace FabOS.WebServer.Models.DTOs.Assets;

public class QRCodeGenerationResult
{
    public bool Success { get; set; }
    public string? QRCodeDataUrl { get; set; }
    public string? QRCodeIdentifier { get; set; }
    public string? ErrorMessage { get; set; }
}
