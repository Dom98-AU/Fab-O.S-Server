using FabOS.WebServer.Models.Entities.Assets;

namespace FabOS.WebServer.Models.DTOs.Assets;

public class EquipmentQRCodeResponse
{
    public int EquipmentId { get; set; }
    public string EquipmentCode { get; set; } = string.Empty;
    public string QRCodeIdentifier { get; set; } = string.Empty;
    public string QRCodeDataUrl { get; set; } = string.Empty;
}
