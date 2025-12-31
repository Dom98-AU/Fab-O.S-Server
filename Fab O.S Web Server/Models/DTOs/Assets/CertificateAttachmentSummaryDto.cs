using FabOS.WebServer.Models.Entities.Assets;

namespace FabOS.WebServer.Models.DTOs.Assets;

/// <summary>
/// Certificate attachment summary for expiry tracking
/// </summary>
public class CertificateAttachmentSummaryDto
{
    public int TotalCertificates { get; set; }
    public int ValidCertificates { get; set; }
    public int ExpiredCertificateCount { get; set; }
    public int ExpiringSoonCertificates { get; set; }
    public List<EquipmentAttachmentDto> ExpiringCertificatesList { get; set; } = new();
    public List<EquipmentAttachmentDto> ExpiredCertificatesList { get; set; } = new();
}
