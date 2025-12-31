using FabOS.WebServer.Models.Entities.Assets;

namespace FabOS.WebServer.Models.DTOs.Assets;

public class CreateCertificationRequest
{
    public int EquipmentId { get; set; }
    public string CertificationType { get; set; } = string.Empty;
    public string? CertificateNumber { get; set; }
    public string? IssuingAuthority { get; set; }
    public DateTime IssueDate { get; set; }
    public DateTime? ExpiryDate { get; set; }
    public string? DocumentUrl { get; set; }
    public string? Notes { get; set; }
}
