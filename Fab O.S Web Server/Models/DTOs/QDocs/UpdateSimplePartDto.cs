using System.ComponentModel.DataAnnotations;

namespace FabOS.WebServer.Models.DTOs.QDocs;

public class UpdateSimplePartDto
{
    public string? Description { get; set; }
    public decimal? Quantity { get; set; }
    public decimal? WeightEach { get; set; }
    public string? MaterialGrade { get; set; }
}
