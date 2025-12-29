using FabOS.WebServer.Models.Entities;

namespace FabOS.WebServer.Services.Interfaces
{
    public interface ITakeoffService
    {
        // Takeoff Session Management
        Task<TraceTakeoff> CreateTakeoffSessionAsync(int traceRecordId, string pdfUrl, int? drawingId = null, int? companyId = null);
        Task<TraceTakeoff> GetTakeoffSessionAsync(int takeoffId);
        Task<TraceTakeoff> UpdateTakeoffSessionAsync(TraceTakeoff takeoff);
        Task<bool> DeleteTakeoffSessionAsync(int takeoffId);
        Task<List<TraceTakeoff>> GetTakeoffsByProjectAsync(int projectId, int companyId);

        // Measurement Operations
        Task<TraceTakeoffMeasurement> AddMeasurementAsync(TraceTakeoffMeasurement measurement);
        Task<TraceTakeoffMeasurement> UpdateMeasurementAsync(TraceTakeoffMeasurement measurement);
        Task<bool> DeleteMeasurementAsync(int measurementId);
        Task<List<TraceTakeoffMeasurement>> GetMeasurementsByTakeoffAsync(int takeoffId);
        Task<List<TraceTakeoffMeasurement>> GetMeasurementsByTypeAsync(int takeoffId, string measurementType);

        // Catalogue Item Matching
        Task<List<CatalogueItem>> FindMatchingCatalogueItemsAsync(TraceTakeoffMeasurement measurement);
        Task<CatalogueItem> AutoMatchCatalogueItemAsync(string description, decimal? dimension1 = null, decimal? dimension2 = null);
        Task<TraceTakeoffMeasurement> LinkMeasurementToCatalogueAsync(int measurementId, int catalogueItemId);
        Task<List<CatalogueSuggestion>> SuggestCatalogueItemsFromTextAsync(string extractedText);

        // Quantity Calculations
        Task<TakeoffQuantity> CalculateLinearQuantityAsync(int measurementId, string targetUnit = "m");
        Task<TakeoffQuantity> CalculateAreaQuantityAsync(int measurementId, string targetUnit = "m2");
        Task<TakeoffQuantity> CalculateCountQuantityAsync(int measurementId);
        Task<TakeoffQuantity> CalculateVolumeQuantityAsync(int measurementId, decimal thickness, string targetUnit = "m3");
        Task<List<TakeoffQuantity>> CalculateAllQuantitiesAsync(int takeoffId);

        // Weight Calculations
        Task<WeightCalculation> CalculateWeightFromMeasurementAsync(int measurementId);
        Task<WeightCalculation> CalculateWeightFromCatalogueAsync(int catalogueItemId, decimal quantity, string unit);
        Task<TotalWeightSummary> CalculateTotalWeightAsync(int takeoffId);

        // Bill of Materials Generation
        Task<BillOfMaterials> GenerateBOMAsync(int takeoffId);
        Task<BillOfMaterials> GenerateBOMByPackageAsync(int packageId);
        Task<MaterialSummary> GenerateMaterialSummaryAsync(int takeoffId);
        Task<List<PurchaseRequirement>> GeneratePurchaseRequirementsAsync(int takeoffId);

        // Export Operations
        Task<byte[]> ExportToExcelAsync(int takeoffId);
        Task<byte[]> ExportToCsvAsync(int takeoffId);
        Task<string> ExportToJsonAsync(int takeoffId);
        Task<byte[]> ExportToPdfReportAsync(int takeoffId);

        // OCR and Text Extraction
        Task<List<ExtractedItem>> ExtractItemsFromPdfTextAsync(string pdfText);
        Task<List<DimensionExtraction>> ExtractDimensionsFromTextAsync(string text);
        Task<MaterialSpecification> ExtractMaterialSpecAsync(string text);

        // Validation
        Task<ValidationResult> ValidateMeasurementsAsync(int takeoffId);
        Task<bool> CheckForDuplicateMeasurementsAsync(int takeoffId);
        Task<List<MeasurementConflict>> DetectMeasurementConflictsAsync(int takeoffId);
    }

    // Supporting DTOs
    public class CatalogueSuggestion
    {
        public CatalogueItem Item { get; set; }
        public decimal ConfidenceScore { get; set; }
        public string MatchReason { get; set; }
        public Dictionary<string, string> MatchedProperties { get; set; }
    }

    public class TakeoffQuantity
    {
        public int MeasurementId { get; set; }
        public decimal Quantity { get; set; }
        public string Unit { get; set; }
        public string MeasurementType { get; set; }
        public decimal? ConvertedQuantity { get; set; }
        public string ConvertedUnit { get; set; }
    }

    public class WeightCalculation
    {
        public int ItemId { get; set; }
        public string ItemCode { get; set; }
        public string Description { get; set; }
        public decimal Quantity { get; set; }
        public string QuantityUnit { get; set; }
        public decimal Weight { get; set; }
        public string WeightUnit { get; set; }
        public decimal? UnitWeight { get; set; }
        public string CalculationMethod { get; set; }
    }

    public class TotalWeightSummary
    {
        public int TakeoffId { get; set; }
        public decimal TotalWeight { get; set; }
        public string WeightUnit { get; set; }
        public Dictionary<string, decimal> WeightByCategory { get; set; }
        public Dictionary<string, decimal> WeightByMaterial { get; set; }
        public List<WeightCalculation> ItemWeights { get; set; }
    }

    public class BillOfMaterials
    {
        public int TakeoffId { get; set; }
        public string ProjectName { get; set; }
        public DateTime GeneratedDate { get; set; }
        public List<BOMItem> Items { get; set; }
        public decimal TotalWeight { get; set; }
        public decimal? TotalCost { get; set; }
        public Dictionary<string, object> Summary { get; set; }
    }

    public class BOMItem
    {
        public int ItemNumber { get; set; }
        public int? CatalogueItemId { get; set; }
        public string ItemCode { get; set; }
        public string Description { get; set; }
        public string Category { get; set; }
        public string Material { get; set; }
        public decimal Quantity { get; set; }
        public string Unit { get; set; }
        public decimal? Weight { get; set; }
        public decimal? UnitCost { get; set; }
        public decimal? TotalCost { get; set; }
        public string Source { get; set; } // Which measurements contributed
        public List<int> MeasurementIds { get; set; }
    }

    public class MaterialSummary
    {
        public Dictionary<string, MaterialGroup> MaterialGroups { get; set; }
        public decimal TotalQuantity { get; set; }
        public decimal TotalWeight { get; set; }
        public List<string> MissingCatalogueLinks { get; set; }
    }

    public class MaterialGroup
    {
        public string Material { get; set; }
        public string Category { get; set; }
        public List<BOMItem> Items { get; set; }
        public decimal TotalQuantity { get; set; }
        public decimal TotalWeight { get; set; }
    }

    public class PurchaseRequirement
    {
        public int CatalogueItemId { get; set; }
        public string ItemCode { get; set; }
        public string Description { get; set; }
        public decimal RequiredQuantity { get; set; }
        public string Unit { get; set; }
        public int? PackQty { get; set; }
        public int PacksRequired { get; set; }
        public decimal OrderQuantity { get; set; }
        public string SupplierCode { get; set; }
    }

    public class ExtractedItem
    {
        public string Text { get; set; }
        public string ItemType { get; set; }
        public Dictionary<string, object> Properties { get; set; }
        public decimal ConfidenceScore { get; set; }
    }

    public class DimensionExtraction
    {
        public string OriginalText { get; set; }
        public decimal Value { get; set; }
        public string Unit { get; set; }
        public string DimensionType { get; set; } // Length, Width, Height, Diameter, etc.
    }

    public class MaterialSpecification
    {
        public string Material { get; set; }
        public string Grade { get; set; }
        public string Standard { get; set; }
        public string Finish { get; set; }
        public Dictionary<string, string> Properties { get; set; }
    }

    public class ValidationResult
    {
        public bool IsValid { get; set; }
        public List<string> Errors { get; set; }
        public List<string> Warnings { get; set; }
        public Dictionary<int, List<string>> MeasurementIssues { get; set; }
    }

    public class MeasurementConflict
    {
        public int MeasurementId1 { get; set; }
        public int MeasurementId2 { get; set; }
        public string ConflictType { get; set; }
        public string Description { get; set; }
        public string Resolution { get; set; }
    }
}