using FabOS.WebServer.Models.Entities.Assets;

namespace FabOS.WebServer.Models.DTOs.Assets;

public class EquipmentCategoryDto
{
    public int Id { get; set; }
    public int CompanyId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? IconClass { get; set; }
    public int DisplayOrder { get; set; }
    public bool IsActive { get; set; }
    public bool IsSystemCategory { get; set; }
    public DateTime CreatedDate { get; set; }
    public DateTime? LastModified { get; set; }
    public int EquipmentCount { get; set; }
    public int TypeCount { get; set; }
}
