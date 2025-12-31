using System.ComponentModel.DataAnnotations;

namespace FabOS.WebServer.Models.DTOs.QDocs;

public class CreateManualPartDto
{
    public string PartMark { get; set; } = string.Empty;
    public string PartType { get; set; } = string.Empty;
    public string? Description { get; set; }
    public decimal Quantity { get; set; }
    public decimal? WeightEach { get; set; }
    public string? MaterialGrade { get; set; }
    public int OrderId { get; set; }
}
