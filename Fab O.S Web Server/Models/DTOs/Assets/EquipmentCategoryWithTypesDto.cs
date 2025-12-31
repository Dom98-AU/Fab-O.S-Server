using FabOS.WebServer.Models.Entities.Assets;

namespace FabOS.WebServer.Models.DTOs.Assets;

public class EquipmentCategoryWithTypesDto : EquipmentCategoryDto
{
    public List<EquipmentTypeDto> Types { get; set; } = new();
}
