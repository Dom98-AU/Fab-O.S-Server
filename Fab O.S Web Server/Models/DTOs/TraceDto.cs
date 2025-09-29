using FabOS.WebServer.Models.Entities;

namespace FabOS.WebServer.Models.DTOs;

// TraceRecord DTOs
public class TraceRecordDto
{
    public int Id { get; set; }
    public Guid TraceId { get; set; }
    public string TraceNumber { get; set; } = string.Empty;
    public TraceableType EntityType { get; set; }
    public int EntityId { get; set; }
    public string EventType { get; set; } = string.Empty;
    public DateTime EventDate { get; set; }
    public string? Location { get; set; }
    public string? ResponsibleParty { get; set; }
    public string? Description { get; set; }
    public string? Metadata { get; set; }
    public DateTime CreatedDate { get; set; }
    public DateTime? ModifiedDate { get; set; }
    public int CompanyId { get; set; }

    public List<TraceMaterialDto> Materials { get; set; } = new();
    public List<TraceProcessDto> Processes { get; set; } = new();
    public List<TraceDocumentDto> Documents { get; set; } = new();
}

public class CreateTraceRecordDto
{
    public TraceableType EntityType { get; set; }
    public int EntityId { get; set; }
    public string EventType { get; set; } = string.Empty;
    public DateTime EventDate { get; set; }
    public string? Location { get; set; }
    public string? ResponsibleParty { get; set; }
    public string? Description { get; set; }
    public string? Metadata { get; set; }
}

public class UpdateTraceRecordDto
{
    public string EventType { get; set; } = string.Empty;
    public DateTime EventDate { get; set; }
    public string? Location { get; set; }
    public string? ResponsibleParty { get; set; }
    public string? Description { get; set; }
    public string? Metadata { get; set; }
}

// TraceMaterial DTOs
public class TraceMaterialDto
{
    public int Id { get; set; }
    public int TraceRecordId { get; set; }
    public int? CatalogueItemId { get; set; }
    public string? CatalogueItemCode { get; set; }
    public string? CatalogueItemDescription { get; set; }
    public string MaterialCode { get; set; } = string.Empty;
    public string MaterialDescription { get; set; } = string.Empty;
    public decimal Quantity { get; set; }
    public string Unit { get; set; } = string.Empty;
    public string? BatchNumber { get; set; }
    public string? SerialNumber { get; set; }
    public DateTime? ExpiryDate { get; set; }
    public string? Supplier { get; set; }
    public string? CertificateNumber { get; set; }
    public DateTime CreatedDate { get; set; }
}

public class CreateTraceMaterialDto
{
    public int TraceRecordId { get; set; }
    public int? CatalogueItemId { get; set; }
    public string MaterialCode { get; set; } = string.Empty;
    public string MaterialDescription { get; set; } = string.Empty;
    public decimal Quantity { get; set; }
    public string Unit { get; set; } = string.Empty;
    public string? BatchNumber { get; set; }
    public string? SerialNumber { get; set; }
    public DateTime? ExpiryDate { get; set; }
    public string? Supplier { get; set; }
    public string? CertificateNumber { get; set; }
}

// TraceProcess DTOs
public class TraceProcessDto
{
    public int Id { get; set; }
    public int TraceRecordId { get; set; }
    public int? OperationId { get; set; }
    public string OperationName { get; set; } = string.Empty;
    public DateTime StartTime { get; set; }
    public DateTime? EndTime { get; set; }
    public double? Duration { get; set; }
    public int? OperatorId { get; set; }
    public string? OperatorName { get; set; }
    public string? WorkOrderNumber { get; set; }
    public string? MachineId { get; set; }
    public bool PassedInspection { get; set; }
    public string? InspectionNotes { get; set; }
    public DateTime CreatedDate { get; set; }
}

public class CreateTraceProcessDto
{
    public int TraceRecordId { get; set; }
    public int? OperationId { get; set; }
    public string OperationName { get; set; } = string.Empty;
    public DateTime StartTime { get; set; }
    public int? OperatorId { get; set; }
    public string? OperatorName { get; set; }
    public string? WorkOrderNumber { get; set; }
    public string? MachineId { get; set; }
}

// TraceDocument DTOs
public class TraceDocumentDto
{
    public int Id { get; set; }
    public int TraceRecordId { get; set; }
    public string DocumentType { get; set; } = string.Empty;
    public string DocumentNumber { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? FilePath { get; set; }
    public string? FileUrl { get; set; }
    public long? FileSize { get; set; }
    public string? MimeType { get; set; }
    public string? Checksum { get; set; }
    public DateTime UploadedDate { get; set; }
    public int? UploadedByUserId { get; set; }
    public bool IsVerified { get; set; }
    public DateTime? VerifiedDate { get; set; }
    public int? VerifiedByUserId { get; set; }
}

// TraceTakeoff DTOs
public class TraceTakeoffDto
{
    public int Id { get; set; }
    public int TraceRecordId { get; set; }
    public int? DrawingId { get; set; }
    public string PdfUrl { get; set; } = string.Empty;
    public decimal? Scale { get; set; }
    public string? ScaleUnit { get; set; }
    public string? CalibrationData { get; set; }
    public DateTime CreatedDate { get; set; }
    public DateTime? ModifiedDate { get; set; }
    public List<TraceTakeoffMeasurementDto> Measurements { get; set; } = new();
}

public class CreateTraceTakeoffDto
{
    public int TraceRecordId { get; set; }
    public int? DrawingId { get; set; }
    public string PdfUrl { get; set; } = string.Empty;
    public decimal? Scale { get; set; }
    public string? ScaleUnit { get; set; }
    public string? CalibrationData { get; set; }
}

// TraceTakeoffMeasurement DTOs
public class TraceTakeoffMeasurementDto
{
    public int Id { get; set; }
    public int TraceTakeoffId { get; set; }
    public int? CatalogueItemId { get; set; }
    public string MeasurementType { get; set; } = string.Empty;
    public decimal Value { get; set; }
    public string Unit { get; set; } = string.Empty;
    public string? Coordinates { get; set; }
    public string? Label { get; set; }
    public string? Color { get; set; }
    public DateTime CreatedDate { get; set; }
}

public class CreateTraceTakeoffMeasurementDto
{
    public int TraceTakeoffId { get; set; }
    public int? CatalogueItemId { get; set; }
    public string MeasurementType { get; set; } = string.Empty;
    public decimal Value { get; set; }
    public string Unit { get; set; } = string.Empty;
    public string? Coordinates { get; set; }
    public string? Label { get; set; }
    public string? Color { get; set; }
}

public class UpdateTraceTakeoffMeasurementDto
{
    public int? CatalogueItemId { get; set; }
    public string? Label { get; set; }
    public string? Color { get; set; }
}

// Catalogue DTOs
public class CatalogueItemDto
{
    public int Id { get; set; }
    public string ItemCode { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string? Category { get; set; }
    public string? SubCategory { get; set; }
    public string? Brand { get; set; }
    public string? Model { get; set; }
    public string? Unit { get; set; }
    public decimal? Weight { get; set; }
    public string? WeightUnit { get; set; }
    public decimal? UnitPrice { get; set; }
    public string? Currency { get; set; }
    public string? Supplier { get; set; }
    public string? ManufacturerPartNumber { get; set; }
    public string? SupplierPartNumber { get; set; }
    public string? Specifications { get; set; }
    public bool IsActive { get; set; }
}

public class CatalogueItemSummaryDto
{
    public int Id { get; set; }
    public string ItemCode { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string? Category { get; set; }
    public string? Unit { get; set; }
    public decimal? UnitPrice { get; set; }
}

// BOM DTOs (keeping the existing ones but adding missing)
public class BillOfMaterials
{
    public int TakeoffId { get; set; }
    public string ProjectName { get; set; } = string.Empty;
    public DateTime GeneratedDate { get; set; }
    public List<BOMLineItem> LineItems { get; set; } = new();
    public decimal TotalEstimatedCost { get; set; }
}

public class BOMLineItem
{
    public int CatalogueItemId { get; set; }
    public string ItemCode { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public decimal Quantity { get; set; }
    public string Unit { get; set; } = string.Empty;
    public decimal UnitCost { get; set; }
    public decimal TotalCost { get; set; }
    public string? Supplier { get; set; }
}

// Trace Report DTO
public class TraceReport
{
    public Guid TraceId { get; set; }
    public string TraceNumber { get; set; } = string.Empty;
    public DateTime GeneratedDate { get; set; }
    public string GeneratedBy { get; set; } = string.Empty;
    public List<TraceMaterial> Materials { get; set; } = new();
    public List<TraceProcess> Processes { get; set; } = new();
    public List<TraceDocument> Documents { get; set; } = new();
    public string? GenealogyTree { get; set; }
    public string? Summary { get; set; }
}