using System.ComponentModel.DataAnnotations;

namespace FabOS.WebServer.Models.DTOs.QDocs;

public class DrawingUploadResultDto
{
    public int DrawingId { get; set; }
    public int DrawingRevisionId { get; set; }
    public int DrawingFileId { get; set; }
    public string FileName { get; set; } = string.Empty;
    public string FileType { get; set; } = string.Empty; // SMLX, IFC, DXF, PDF
    public string ParseStatus { get; set; } = string.Empty; // Completed, Failed, Pending
    public string? ParseErrors { get; set; }

    public int AssemblyCount { get; set; }
    public int PartCount { get; set; }
    public int LoosePartCount { get; set; } // Parts not in any assembly

    public List<DrawingAssemblyDto> Assemblies { get; set; } = new();
    public List<DrawingPartDto> LooseParts { get; set; } = new();

    public decimal TotalWeight { get; set; } // kg
    public DateTime UploadedDate { get; set; }
}
