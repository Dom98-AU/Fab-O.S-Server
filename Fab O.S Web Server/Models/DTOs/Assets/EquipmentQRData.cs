using FabOS.WebServer.Models.Entities.Assets;

namespace FabOS.WebServer.Models.DTOs.Assets;

public class EquipmentQRData
{
    public int Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Category { get; set; }
    public string? Type { get; set; }
    public string? Location { get; set; }
    public DateTime? NextServiceDate { get; set; }
    public string? SerialNumber { get; set; }
    public string GeneratedAt { get; set; } = DateTime.UtcNow.ToString("O");
}
