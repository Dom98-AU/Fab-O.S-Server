using FabOS.WebServer.Models.Entities.Assets;

namespace FabOS.WebServer.Models.DTOs.Assets;

public class EquipmentKitDto
{
    public int Id { get; set; }
    public int CompanyId { get; set; }
    public int? KitTemplateId { get; set; }
    public string? KitTemplateName { get; set; }
    public string KitCode { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public KitStatus Status { get; set; }
    public string StatusName => Status.ToString();
    public string? Location { get; set; }
    public int? AssignedToUserId { get; set; }
    public string? AssignedToUserName { get; set; }
    public string? QRCodeData { get; set; }
    public string? QRCodeIdentifier { get; set; }
    public bool HasMaintenanceFlag { get; set; }
    public string? MaintenanceFlagNotes { get; set; }
    public DateTime CreatedDate { get; set; }
    public int? CreatedByUserId { get; set; }
    public DateTime? LastModified { get; set; }
    public int? LastModifiedByUserId { get; set; }
    public int KitItemCount { get; set; }
    public List<EquipmentKitItemDto> KitItems { get; set; } = new();
}
