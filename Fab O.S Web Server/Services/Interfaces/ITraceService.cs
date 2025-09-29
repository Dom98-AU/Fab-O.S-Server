using FabOS.WebServer.Models.Entities;
using FabOS.WebServer.Models.DTOs;

namespace FabOS.WebServer.Services.Interfaces
{
    public interface ITraceService
    {
        // Core trace operations
        Task<TraceRecord> CreateTraceRecordAsync(TraceRecord traceRecord);
        Task<TraceRecord> GetTraceRecordAsync(Guid traceId);
        Task<List<TraceRecord>> GetTraceRecordsByEntityAsync(TraceableType entityType, int entityId);
        Task<TraceRecord> UpdateTraceRecordAsync(TraceRecord traceRecord);
        Task<bool> DeleteTraceRecordAsync(Guid traceId);

        // Material tracking
        Task<TraceMaterial> RecordMaterialReceiptAsync(TraceMaterial material);
        Task<TraceMaterial> RecordMaterialIssueAsync(int traceRecordId, int catalogueItemId, decimal quantity, string unit);
        Task<List<TraceMaterial>> GetMaterialsByTraceAsync(int traceRecordId);
        Task<TraceMaterial> LinkMaterialToCatalogueAsync(int traceMaterialId, int catalogueItemId);

        // Process tracking
        Task<TraceProcess> RecordProcessStartAsync(int traceRecordId, int operationId, int? operatorId);
        Task<TraceProcess> RecordProcessCompleteAsync(int traceProcessId, DateTime endTime, bool passedInspection);
        Task<TraceParameter> AddProcessParameterAsync(int traceProcessId, string name, string value, string unit);
        Task<List<TraceProcess>> GetProcessesByTraceAsync(int traceRecordId);

        // Assembly tracking
        Task<TraceAssembly> CreateAssemblyTraceAsync(TraceAssembly assembly);
        Task<TraceComponent> AddComponentToAssemblyAsync(int assemblyId, Guid componentTraceId, decimal quantity);
        Task<List<TraceComponent>> GetAssemblyComponentsAsync(int assemblyId);

        // Document management
        Task<TraceDocument> AttachDocumentAsync(int traceRecordId, TraceDocument document);
        Task<List<TraceDocument>> GetDocumentsByTraceAsync(int traceRecordId);
        Task<bool> VerifyDocumentAsync(int documentId, int verifiedByUserId);

        // Traceability queries
        Task<List<TraceRecord>> TraceForwardAsync(Guid sourceTraceId);
        Task<List<TraceRecord>> TraceBackwardAsync(Guid targetTraceId);
        Task<List<TraceRecord>> GetTraceHierarchyAsync(Guid traceId);
        Task<TraceReportDto> GenerateTraceReportAsync(Guid traceId);

        // PDF and Takeoff operations
        Task<TraceTakeoff> CreateTakeoffFromPdfAsync(int traceRecordId, string pdfUrl, int? drawingId);
        Task<TraceTakeoffMeasurement> AddMeasurementAsync(int takeoffId, TraceTakeoffMeasurement measurement);
        Task<List<TraceTakeoffMeasurement>> GetTakeoffMeasurementsAsync(int takeoffId);
        Task<TraceTakeoffMeasurement> LinkMeasurementToCatalogueAsync(int measurementId, int catalogueItemId);
        Task<BillOfMaterialsDto> GenerateBOMFromTakeoffAsync(int takeoffId);

        // Catalogue integration
        Task<List<CatalogueItem>> SuggestCatalogueItemsAsync(string searchText, string category = null);
        Task<decimal> CalculateWeightFromCatalogueAsync(int catalogueItemId, decimal quantity, string unit);
        Task<MaterialRequirementDto> GenerateMaterialRequirementAsync(int traceRecordId);
    }

    // DTOs for complex operations
    public class TraceReportDto
    {
        public Guid TraceId { get; set; }
        public string TraceNumber { get; set; }
        public DateTime GeneratedDate { get; set; }
        public string GeneratedBy { get; set; }
        public List<TraceMaterial> Materials { get; set; }
        public List<TraceProcess> Processes { get; set; }
        public List<TraceDocument> Documents { get; set; }
        public string GenealogyTree { get; set; } // JSON representation
        public string Summary { get; set; }
    }

    public class BillOfMaterialsDto
    {
        public int TakeoffId { get; set; }
        public DateTime GeneratedDate { get; set; }
        public List<BOMLineItem> LineItems { get; set; }
        public decimal TotalWeight { get; set; }
        public decimal TotalCost { get; set; }
    }

    public class BOMLineItem
    {
        public int CatalogueItemId { get; set; }
        public string ItemCode { get; set; }
        public string Description { get; set; }
        public decimal Quantity { get; set; }
        public string Unit { get; set; }
        public decimal? Weight { get; set; }
        public decimal? UnitCost { get; set; }
        public decimal? TotalCost { get; set; }
    }

    public class MaterialRequirementDto
    {
        public int TraceRecordId { get; set; }
        public List<MaterialRequirement> Requirements { get; set; }
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

    // New entities for Takeoff functionality
    public class TraceTakeoff
    {
        public int Id { get; set; }
        public int TraceRecordId { get; set; }
        public int? DrawingId { get; set; }
        public string PdfUrl { get; set; }
        public decimal? Scale { get; set; }
        public string ScaleUnit { get; set; }
        public string CalibrationData { get; set; }
        public DateTime CreatedDate { get; set; }
        public DateTime? ModifiedDate { get; set; }
        public int CompanyId { get; set; }

        // Navigation properties
        public virtual TraceRecord TraceRecord { get; set; }
        public virtual TraceDrawing Drawing { get; set; }
        public virtual ICollection<TraceTakeoffMeasurement> Measurements { get; set; }
    }

    public class TraceTakeoffMeasurement
    {
        public int Id { get; set; }
        public int TraceTakeoffId { get; set; }
        public int? CatalogueItemId { get; set; }
        public string MeasurementType { get; set; } // Linear, Area, Count, Volume
        public decimal Value { get; set; }
        public string Unit { get; set; }
        public string Coordinates { get; set; } // JSON array of points
        public string Label { get; set; }
        public string Color { get; set; }
        public DateTime CreatedDate { get; set; }

        // Navigation properties
        public virtual TraceTakeoff TraceTakeoff { get; set; }
        public virtual CatalogueItem CatalogueItem { get; set; }
    }
}