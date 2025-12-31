using FabOS.WebServer.Models.Entities.Assets;

namespace FabOS.WebServer.Models.DTOs.Assets;

public class KitTemplateItemDto
{
    public int Id { get; set; }
    public int KitTemplateId { get; set; }
    public int EquipmentTypeId { get; set; }
    public string? EquipmentTypeName { get; set; }
    public string? EquipmentCategoryName { get; set; }
    public int Quantity { get; set; }
    public bool IsMandatory { get; set; }
    public int DisplayOrder { get; set; }
    public string? Notes { get; set; }
}
