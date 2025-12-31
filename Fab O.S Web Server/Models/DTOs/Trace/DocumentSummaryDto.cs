namespace FabOS.WebServer.Models.DTOs.Trace;

public class DocumentSummaryDto
{
    public int DocumentId { get; set; }
    public string DocumentType { get; set; }
    public string DocumentNumber { get; set; }
    public string Title { get; set; }
    public DateTime UploadedDate { get; set; }
    public bool IsVerified { get; set; }
}
