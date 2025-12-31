using FabOS.WebServer.Models.Entities.Assets;

namespace FabOS.WebServer.Models.DTOs.Assets;

public class EquipmentKitItemDto
{
    public int Id { get; set; }
    public int KitId { get; set; }
    public int EquipmentId { get; set; }
    public string? EquipmentCode { get; set; }
    public string? EquipmentName { get; set; }
    public string? EquipmentTypeName { get; set; }
    public string? EquipmentCategoryName { get; set; }
    public int? TemplateItemId { get; set; }
    public int DisplayOrder { get; set; }
    public bool NeedsMaintenance { get; set; }
    public string? Notes { get; set; }
    public DateTime AddedDate { get; set; }
    public int? AddedByUserId { get; set; }
}
