using FabOS.WebServer.Models.Entities.Assets;

namespace FabOS.WebServer.Models.DTOs.Assets;

public class KitTemplateDto
{
    public int Id { get; set; }
    public int CompanyId { get; set; }
    public string TemplateCode { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? Category { get; set; }
    public string? IconClass { get; set; }
    public int DefaultCheckoutDays { get; set; }
    public bool RequiresSignature { get; set; }
    public bool RequiresConditionCheck { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedDate { get; set; }
    public int? CreatedByUserId { get; set; }
    public DateTime? LastModified { get; set; }
    public int? LastModifiedByUserId { get; set; }
    public int TemplateItemCount { get; set; }
    public int KitCount { get; set; }
    public List<KitTemplateItemDto> TemplateItems { get; set; } = new();
}
