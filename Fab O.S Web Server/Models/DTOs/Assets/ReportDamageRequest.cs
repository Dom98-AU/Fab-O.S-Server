using FabOS.WebServer.Models.Entities.Assets;

namespace FabOS.WebServer.Models.DTOs.Assets;

public class ReportDamageRequest
{
    public string DamageDescription { get; set; } = string.Empty;
}
