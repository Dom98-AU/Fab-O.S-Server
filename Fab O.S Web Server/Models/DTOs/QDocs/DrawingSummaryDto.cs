using System.ComponentModel.DataAnnotations;

namespace FabOS.WebServer.Models.DTOs.QDocs;

public class DrawingSummaryDto
{
    public int DrawingId { get; set; }
    public string DrawingNumber { get; set; } = string.Empty;
    public string DrawingTitle { get; set; } = string.Empty;
    public int TotalParts { get; set; }
    public decimal TotalWeight { get; set; }
    public int TotalQuantity { get; set; }
    public int RevisionCount { get; set; }
    public string CurrentStage { get; set; } = string.Empty;
}
