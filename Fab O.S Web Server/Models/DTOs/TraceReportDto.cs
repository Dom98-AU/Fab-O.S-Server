namespace FabOS.WebServer.Models.DTOs;

public class TraceReportDto
{
    public Guid TraceId { get; set; }
    public string TraceNumber { get; set; } = string.Empty;
    public string EntityType { get; set; } = string.Empty;
    public int EntityId { get; set; }
    public string ReportTitle { get; set; } = string.Empty;
    public DateTime GeneratedDate { get; set; }
    public string GeneratedBy { get; set; } = string.Empty;
    public List<TraceRecordSummaryDto> TraceRecords { get; set; } = new();
    public List<MaterialSummaryDto> Materials { get; set; } = new();
    public List<ProcessSummaryDto> Processes { get; set; } = new();
    public List<DocumentSummaryDto> Documents { get; set; } = new();
    public string? Summary { get; set; }
}

public class TraceRecordSummaryDto
{
    public Guid TraceId { get; set; }
    public string EntityType { get; set; }
    public int EntityId { get; set; }
    public string EntityDescription { get; set; }
    public string EventType { get; set; }
    public DateTime EventDate { get; set; }
    public string Location { get; set; }
    public string ResponsibleParty { get; set; }
}

public class MaterialSummaryDto
{
    public int MaterialId { get; set; }
    public string MaterialCode { get; set; }
    public string MaterialDescription { get; set; }
    public decimal Quantity { get; set; }
    public string Unit { get; set; }
    public string BatchNumber { get; set; }
    public DateTime ReceivedDate { get; set; }
}

public class ProcessSummaryDto
{
    public int ProcessId { get; set; }
    public string OperationName { get; set; }
    public DateTime StartTime { get; set; }
    public DateTime? EndTime { get; set; }
    public string OperatorName { get; set; }
    public bool PassedInspection { get; set; }
    public Dictionary<string, string> Parameters { get; set; } = new();
}

public class DocumentSummaryDto
{
    public int DocumentId { get; set; }
    public string DocumentType { get; set; }
    public string DocumentNumber { get; set; }
    public string Title { get; set; }
    public DateTime UploadedDate { get; set; }
    public bool IsVerified { get; set; }
}

public class BillOfMaterialsDto
{
    public int TakeoffId { get; set; }
    public string ProjectName { get; set; } = string.Empty;
    public DateTime GeneratedDate { get; set; }
    public List<BOMLineItemDto> LineItems { get; set; } = new();
    public decimal TotalEstimatedCost { get; set; }
    public decimal? TotalWeight { get; set; }
}

public class BOMLineItemDto
{
    public int CatalogueItemId { get; set; }
    public string ItemCode { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public decimal Quantity { get; set; }
    public string Unit { get; set; } = string.Empty;
    public decimal UnitCost { get; set; }
    public decimal TotalCost { get; set; }
    public string? Supplier { get; set; }
    public decimal? Weight { get; set; }
}

public class MaterialRequirementDto
{
    public int TraceRecordId { get; set; }
    public List<MaterialRequirement> Requirements { get; set; } = new();
    public decimal TotalWeight { get; set; }
}

public class MaterialRequirement
{
    public int CatalogueItemId { get; set; }
    public string ItemCode { get; set; }
    public string Description { get; set; }
    public decimal RequiredQuantity { get; set; }
    public decimal OnHandQuantity { get; set; }
    public decimal ToOrderQuantity { get; set; }
    public string Unit { get; set; }
}