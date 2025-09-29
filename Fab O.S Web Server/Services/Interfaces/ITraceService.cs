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

}