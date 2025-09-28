using FabOS.WebServer.Models.Entities;

namespace FabOS.WebServer.Models.ViewModels;

// Trace Capture DTOs
public class TraceCaptureDto
{
    public TraceableType EntityType { get; set; }
    public int EntityId { get; set; }
    public string? EntityReference { get; set; }
    public Guid? ParentTraceId { get; set; }
    public DateTime? EventDateTime { get; set; }
    public int? UserId { get; set; }
    public string? OperatorName { get; set; }
    public int? WorkCenterId { get; set; }
    public string? Location { get; set; }
    public string? MachineId { get; set; }
    public TraceStatus Status { get; set; } = TraceStatus.Active;
}

// Drawing Takeoff DTOs (replaces obsolete material tracing)
public class DrawingTakeoffDto
{
    public int DrawingId { get; set; }
    public string DrawingTitle { get; set; } = string.Empty;
    public string FileName { get; set; } = string.Empty;
    public string? DrawingNumber { get; set; }
    public string? ProjectNumber { get; set; }
    public decimal? Scale { get; set; }
    public string? ScaleUnit { get; set; }
    public List<TakeoffMeasurementDto> Measurements { get; set; } = new List<TakeoffMeasurementDto>();
    public List<TakeoffMarkupDto> Markups { get; set; } = new List<TakeoffMarkupDto>();
    public string? Notes { get; set; }
}

public class TakeoffMeasurementDto
{
    public string Id { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty; // Linear, Area, Count, Angle
    public decimal Value { get; set; }
    public string Units { get; set; } = string.Empty;
    public decimal[] Coordinates { get; set; } = Array.Empty<decimal>();
    public string? Notes { get; set; }
    public string? Color { get; set; }
}

public class TakeoffMarkupDto
{
    public string Id { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty; // Highlight, Text, Arrow
    public string Text { get; set; } = string.Empty;
    public string Color { get; set; } = string.Empty;
    public decimal[] Coordinates { get; set; } = Array.Empty<decimal>();
}

public class TakeoffExportDto
{
    public string DrawingTitle { get; set; } = string.Empty;
    public string? DrawingNumber { get; set; }
    public DateTime ExportDate { get; set; }
    public string ExportedBy { get; set; } = string.Empty;
    public decimal TotalLinear { get; set; }
    public decimal TotalArea { get; set; }
    public int TotalCount { get; set; }
    public string Units { get; set; } = string.Empty;
    public List<TakeoffMeasurementDto> Measurements { get; set; } = new List<TakeoffMeasurementDto>();
}

public class MaterialIssueDto
{
    public int TraceRecordId { get; set; }
    public string MaterialCode { get; set; } = string.Empty;
    public string? HeatNumber { get; set; }
    public decimal QuantityIssued { get; set; }
    public string Unit { get; set; } = string.Empty;
    public string? IssuedTo { get; set; }
    public string? WorkOrderNumber { get; set; }
    public string? Notes { get; set; }
}

// Process DTOs
public class ProcessResultDto
{
    public DateTime? EndTime { get; set; }
    public bool? PassedInspection { get; set; }
    public string? InspectionNotes { get; set; }
    public List<TraceParameterDto> Parameters { get; set; } = new List<TraceParameterDto>();
}

public class TraceParameterDto
{
    public string ParameterName { get; set; } = string.Empty;
    public string ParameterValue { get; set; } = string.Empty;
    public string? Unit { get; set; }
    public decimal? NumericValue { get; set; }
    public string? Category { get; set; }
}

// Assembly DTOs
public class AssemblyBuildDto
{
    public int TraceRecordId { get; set; }
    public int? AssemblyId { get; set; }
    public string AssemblyNumber { get; set; } = string.Empty;
    public string? SerialNumber { get; set; }
    public DateTime AssemblyDate { get; set; }
    public int? BuildOperatorId { get; set; }
    public string? BuildOperatorName { get; set; }
    public int? BuildWorkCenterId { get; set; }
    public string? BuildLocation { get; set; }
    public string? BuildNotes { get; set; }
    public List<TraceComponentDto> Components { get; set; } = new List<TraceComponentDto>();
}

public class TraceComponentDto
{
    public Guid ComponentTraceId { get; set; }
    public string ComponentReference { get; set; } = string.Empty;
    public decimal QuantityUsed { get; set; }
    public string Unit { get; set; } = string.Empty;
    public string? UsageNotes { get; set; }
}

// Document DTOs
public class DocumentUploadDto
{
    public DocumentType DocumentType { get; set; }
    public string DocumentName { get; set; } = string.Empty;
    public string? Description { get; set; }
    public IFormFile File { get; set; } = null!;
}

// Search DTOs
public class TraceSearchDto
{
    public string? TraceNumber { get; set; }
    public TraceableType? EntityType { get; set; }
    public string? EntityReference { get; set; }
    public DateTime? FromDate { get; set; }
    public DateTime? ToDate { get; set; }
    public string? OperatorName { get; set; }
    public int? WorkCenterId { get; set; }
    public TraceStatus? Status { get; set; }
    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 50;
}

public class MaterialSearchDto
{
    public string? MaterialCode { get; set; }
    public string? HeatNumber { get; set; }
    public string? BatchNumber { get; set; }
    public string? Supplier { get; set; }
    public string? MillCertificate { get; set; }
    public DateTime? FromDate { get; set; }
    public DateTime? ToDate { get; set; }
    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 50;
}

// Reporting DTOs
public class TraceReportDto
{
    public Guid TraceId { get; set; }
    public string TraceNumber { get; set; } = string.Empty;
    public DateTime GeneratedDate { get; set; }
    public string GeneratedBy { get; set; } = string.Empty;
    public TraceGenealogyDto GenealogyTree { get; set; } = new TraceGenealogyDto();
    public List<TraceDocumentDto> Documents { get; set; } = new List<TraceDocumentDto>();
    public TraceSummaryDto Summary { get; set; } = new TraceSummaryDto();
}

public class TraceGenealogyDto
{
    public Guid TraceId { get; set; }
    public string TraceNumber { get; set; } = string.Empty;
    public TraceableType EntityType { get; set; }
    public string EntityReference { get; set; } = string.Empty;
    public DateTime CaptureDateTime { get; set; }
    public List<TraceGenealogyDto> Children { get; set; } = new List<TraceGenealogyDto>();
    public TraceMaterialDto? Material { get; set; }
    public List<TraceProcessDto> Processes { get; set; } = new List<TraceProcessDto>();
}

public class TraceMaterialDto
{
    public string MaterialCode { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string? HeatNumber { get; set; }
    public string? BatchNumber { get; set; }
    public string? Supplier { get; set; }
    public string? MillCertificate { get; set; }
    public CertificateType? CertType { get; set; }
    public decimal Quantity { get; set; }
    public string Unit { get; set; } = string.Empty;
}

public class TraceProcessDto
{
    public string OperationCode { get; set; } = string.Empty;
    public string OperationDescription { get; set; } = string.Empty;
    public DateTime StartTime { get; set; }
    public DateTime? EndTime { get; set; }
    public string? OperatorName { get; set; }
    public string? MachineName { get; set; }
    public bool? PassedInspection { get; set; }
    public List<TraceParameterDto> Parameters { get; set; } = new List<TraceParameterDto>();
}

public class TraceDocumentDto
{
    public int Id { get; set; }
    public DocumentType DocumentType { get; set; }
    public string DocumentName { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string FilePath { get; set; } = string.Empty;
    public long FileSize { get; set; }
    public string MimeType { get; set; } = string.Empty;
    public bool IsVerified { get; set; }
    public DateTime UploadDate { get; set; }
}

public class TraceSummaryDto
{
    public int TotalMaterials { get; set; }
    public int TotalProcesses { get; set; }
    public int TotalDocuments { get; set; }
    public int TotalChildren { get; set; }
    public DateTime? FirstEvent { get; set; }
    public DateTime? LastEvent { get; set; }
    public string CurrentStatus { get; set; } = string.Empty;
}

// Compliance DTOs
public class ComplianceReportDto
{
    public string ReportTitle { get; set; } = string.Empty;
    public DateTime GeneratedDate { get; set; }
    public string GeneratedBy { get; set; } = string.Empty;
    public ComplianceReportRequest Request { get; set; } = new ComplianceReportRequest();
    public List<ComplianceItemDto> Items { get; set; } = new List<ComplianceItemDto>();
    public ComplianceSummaryDto Summary { get; set; } = new ComplianceSummaryDto();
}

public class ComplianceReportRequest
{
    public string? ProjectNumber { get; set; }
    public string? OrderNumber { get; set; }
    public string? CustomerName { get; set; }
    public DateTime? FromDate { get; set; }
    public DateTime? ToDate { get; set; }
    public List<string> RequiredCertificates { get; set; } = new List<string>();
    public bool IncludePhotos { get; set; } = true;
    public bool IncludeTestReports { get; set; } = true;
    public bool As5131Compliance { get; set; } = true;
}

public class ComplianceItemDto
{
    public Guid TraceId { get; set; }
    public string TraceNumber { get; set; } = string.Empty;
    public string ItemDescription { get; set; } = string.Empty;
    public string? HeatNumber { get; set; }
    public string? MillCertificate { get; set; }
    public bool IsCompliant { get; set; }
    public List<string> ComplianceIssues { get; set; } = new List<string>();
    public List<TraceDocumentDto> SupportingDocuments { get; set; } = new List<TraceDocumentDto>();
}

public class ComplianceSummaryDto
{
    public int TotalItems { get; set; }
    public int CompliantItems { get; set; }
    public int NonCompliantItems { get; set; }
    public decimal CompliancePercentage { get; set; }
    public List<string> CommonIssues { get; set; } = new List<string>();
}

public class ComplianceValidationDto
{
    public Guid TraceId { get; set; }
    public bool IsAs5131Compliant { get; set; }
    public List<ComplianceCheckDto> Checks { get; set; } = new List<ComplianceCheckDto>();
    public List<string> RequiredActions { get; set; } = new List<string>();
}

public class ComplianceCheckDto
{
    public string CheckName { get; set; } = string.Empty;
    public string CheckDescription { get; set; } = string.Empty;
    public bool Passed { get; set; }
    public string? Notes { get; set; }
    public string? RequiredAction { get; set; }
}

// Catalogue Item DTOs
public class CatalogueItemDto
{
    public string ItemCode { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public string Material { get; set; } = string.Empty;
    public string? Profile { get; set; }
    public decimal? Length_mm { get; set; }
    public decimal? Width_mm { get; set; }
    public decimal? Height_mm { get; set; }
    public decimal? Thickness_mm { get; set; }
    public decimal? Mass_kg_m { get; set; }
    public decimal? SurfaceArea_m2_per_m { get; set; }
    public string? Standard { get; set; }
    public string? Grade { get; set; }
    public string? Finish { get; set; }
    public string? Unit { get; set; } = "EA";
    // Additional properties as needed for import
}

public class CatalogueSearchDto
{
    public string? SearchTerm { get; set; }
    public string? Category { get; set; }
    public string? Material { get; set; }
    public string? Profile { get; set; }
    public string? Grade { get; set; }
    public string? Finish { get; set; }
    public bool ActiveOnly { get; set; } = true;
    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 50;
}

public class BulkImportResultDto
{
    public int TotalRecords { get; set; }
    public int SuccessfulImports { get; set; }
    public int FailedImports { get; set; }
    public List<string> Errors { get; set; } = new List<string>();
    public TimeSpan ImportDuration { get; set; }
}

// Inventory DTOs
public class InventoryItemDto
{
    public int CatalogueItemId { get; set; }
    public string InventoryCode { get; set; } = string.Empty;
    public string? WarehouseCode { get; set; }
    public string? Location { get; set; }
    public decimal QuantityOnHand { get; set; }
    public string Unit { get; set; } = string.Empty;
    public string? LotNumber { get; set; }
    public string? HeatNumber { get; set; }
    public string? MillCertificate { get; set; }
    public string? Supplier { get; set; }
    public DateTime? ReceivedDate { get; set; }
    public decimal? UnitCost { get; set; }
}

public class InventorySearchDto
{
    public string? SearchTerm { get; set; }
    public string? WarehouseCode { get; set; }
    public string? MaterialCode { get; set; }
    public string? HeatNumber { get; set; }
    public bool ActiveOnly { get; set; } = true;
    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 50;
}

public class InventoryReceiptDto
{
    public int CatalogueItemId { get; set; }
    public decimal Quantity { get; set; }
    public string Unit { get; set; } = string.Empty;
    public string? LotNumber { get; set; }
    public string? HeatNumber { get; set; }
    public string? MillCertificate { get; set; }
    public string? Supplier { get; set; }
    public string? PurchaseOrderNumber { get; set; }
    public decimal? UnitCost { get; set; }
    public string? WarehouseCode { get; set; }
    public string? Location { get; set; }
}

public class InventoryIssueDto
{
    public int InventoryItemId { get; set; }
    public decimal Quantity { get; set; }
    public string Unit { get; set; } = string.Empty;
    public string? WorkOrderNumber { get; set; }
    public string? IssuedTo { get; set; }
    public string? Notes { get; set; }
}

public class InventoryTransferDto
{
    public int InventoryItemId { get; set; }
    public decimal Quantity { get; set; }
    public string FromLocation { get; set; } = string.Empty;
    public string ToLocation { get; set; } = string.Empty;
    public string? Notes { get; set; }
}

public class InventoryAdjustmentDto
{
    public int InventoryItemId { get; set; }
    public decimal AdjustmentQuantity { get; set; }
    public string AdjustmentType { get; set; } = string.Empty; // "Increase", "Decrease", "Set"
    public string Reason { get; set; } = string.Empty;
    public string? Notes { get; set; }
}

// Assembly DTOs
public class AssemblyDto
{
    public string AssemblyNumber { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string? Version { get; set; }
    public string? DrawingNumber { get; set; }
    public int? ParentAssemblyId { get; set; }
    public List<AssemblyComponentDto> Components { get; set; } = new List<AssemblyComponentDto>();
}

public class AssemblyComponentDto
{
    public int? CatalogueItemId { get; set; }
    public int? ComponentAssemblyId { get; set; }
    public string ComponentReference { get; set; } = string.Empty;
    public decimal Quantity { get; set; }
    public string Unit { get; set; } = string.Empty;
    public string? Notes { get; set; }
    public string? AlternateItems { get; set; }
}

public class AssemblySearchDto
{
    public string? SearchTerm { get; set; }
    public string? AssemblyNumber { get; set; }
    public string? DrawingNumber { get; set; }
    public int? ParentAssemblyId { get; set; }
    public bool ActiveOnly { get; set; } = true;
    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 50;
}

public class AssemblyTreeDto
{
    public int AssemblyId { get; set; }
    public string AssemblyNumber { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public List<AssemblyComponentDto> Components { get; set; } = new List<AssemblyComponentDto>();
    public List<AssemblyTreeDto> SubAssemblies { get; set; } = new List<AssemblyTreeDto>();
}

public class AssemblyCostDto
{
    public int AssemblyId { get; set; }
    public decimal MaterialCost { get; set; }
    public decimal LaborCost { get; set; }
    public decimal OverheadCost { get; set; }
    public decimal TotalCost { get; set; }
    public List<ComponentCostDto> ComponentCosts { get; set; } = new List<ComponentCostDto>();
}

public class ComponentCostDto
{
    public string ComponentReference { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public decimal Quantity { get; set; }
    public string Unit { get; set; } = string.Empty;
    public decimal UnitCost { get; set; }
    public decimal TotalCost { get; set; }
}
