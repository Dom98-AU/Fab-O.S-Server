using FabOS.WebServer.Models.Entities;

namespace FabOS.WebServer.Services.Interfaces
{
    /// <summary>
    /// Service for managing takeoff catalogue operations and measurement calculations
    /// </summary>
    public interface ITakeoffCatalogueService
    {
        /// <summary>
        /// Get all unique materials for a company
        /// </summary>
        Task<List<string>> GetMaterialsAsync(int companyId);

        /// <summary>
        /// Get all unique catalogue categories for a company
        /// </summary>
        Task<List<string>> GetCategoriesAsync(int companyId);

        /// <summary>
        /// Get categories for a specific material
        /// </summary>
        Task<List<string>> GetCategoriesByMaterialAsync(string material, int companyId);

        /// <summary>
        /// Get catalogue items by category
        /// </summary>
        Task<List<CatalogueItem>> GetItemsByCategoryAsync(string category, int companyId);

        /// <summary>
        /// Get catalogue items by material and category
        /// </summary>
        Task<List<CatalogueItem>> GetItemsByMaterialAndCategoryAsync(string material, string category, int companyId);

        /// <summary>
        /// Get a specific catalogue item by ID
        /// </summary>
        Task<CatalogueItem?> GetItemByIdAsync(int id, int companyId);

        /// <summary>
        /// Search catalogue items by keyword
        /// </summary>
        Task<List<CatalogueItem>> SearchItemsAsync(string searchTerm, int companyId, int maxResults = 50);

        /// <summary>
        /// Calculate measurement results based on catalogue item and measurement data
        /// </summary>
        Task<MeasurementCalculationResult> CalculateMeasurementAsync(
            int catalogueItemId,
            string measurementType,
            decimal measurementValue,
            string unit,
            int companyId);

        /// <summary>
        /// Create a new takeoff measurement with catalogue linkage
        /// </summary>
        Task<TraceTakeoffMeasurement> CreateMeasurementAsync(
            int traceTakeoffId,
            int packageDrawingId,
            int catalogueItemId,
            string measurementType,
            decimal value,
            string unit,
            string? coordinates,
            int companyId,
            int? userId = null,
            string? annotationId = null);

        /// <summary>
        /// Get all measurements for a specific drawing
        /// </summary>
        Task<List<TraceTakeoffMeasurement>> GetMeasurementsByDrawingAsync(int packageDrawingId, int companyId);

        /// <summary>
        /// Update an existing measurement
        /// </summary>
        Task<TraceTakeoffMeasurement?> UpdateMeasurementAsync(
            int measurementId,
            decimal value,
            string? coordinates,
            int companyId,
            int? userId = null);

        /// <summary>
        /// Delete a measurement
        /// Returns list of annotation IDs that were deleted (so they can be removed from PDF viewer)
        /// </summary>
        Task<List<string>> DeleteMeasurementAsync(int measurementId, int companyId, int? userId = null);

        /// <summary>
        /// Get summary statistics for all measurements on a drawing
        /// </summary>
        Task<DrawingMeasurementSummary> GetDrawingSummaryAsync(int packageDrawingId, int companyId);

        /// <summary>
        /// Get the TraceTakeoffId by following the hierarchy: PackageDrawing → Package → Revision → Takeoff
        /// </summary>
        Task<int?> GetTraceTakeoffIdFromPackageDrawingAsync(int packageDrawingId);

        /// <summary>
        /// Get a single measurement by ID with all related data (CatalogueItem, SurfaceCoating, etc.)
        /// </summary>
        Task<TraceTakeoffMeasurement?> GetMeasurementByIdAsync(int measurementId, int companyId);

        /// <summary>
        /// Update measurement details (surface coating, status, notes, label, description, color)
        /// </summary>
        Task<TraceTakeoffMeasurement?> UpdateMeasurementDetailsAsync(
            int measurementId,
            int? surfaceCoatingId,
            string? status,
            string? notes,
            string? label,
            string? description,
            string? color,
            int? userId,
            int companyId);
    }

    /// <summary>
    /// Result of measurement calculation including quantity, weight, and cost
    /// </summary>
    public class MeasurementCalculationResult
    {
        public int CatalogueItemId { get; set; }
        public string ItemCode { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string MeasurementType { get; set; } = string.Empty;
        public decimal MeasurementValue { get; set; }
        public string Unit { get; set; } = string.Empty;

        // Annotation link (set by JavaScript after calculation)
        public string? AnnotationId { get; set; }

        // Calculated results
        public decimal Quantity { get; set; }
        public string QuantityUnit { get; set; } = string.Empty;
        public decimal? Weight { get; set; }
        public string? WeightUnit { get; set; }
        public decimal? Cost { get; set; }

        // Item details for display
        public string? Material { get; set; }
        public string? Grade { get; set; }
        public decimal? Mass_kg_m { get; set; }
        public decimal? Mass_kg_m2 { get; set; }
        public decimal? Weight_kg { get; set; }

        public string CalculationFormula { get; set; } = string.Empty;
    }

    /// <summary>
    /// Summary of all measurements for a drawing
    /// </summary>
    public class DrawingMeasurementSummary
    {
        public int PackageDrawingId { get; set; }
        public int TotalMeasurements { get; set; }
        public decimal TotalWeight { get; set; }
        public string WeightUnit { get; set; } = "kg";
        public decimal? TotalCost { get; set; }

        public Dictionary<string, int> MeasurementTypeBreakdown { get; set; } = new();
        public Dictionary<string, decimal> CategoryWeightBreakdown { get; set; } = new();

        public List<TraceTakeoffMeasurement> Measurements { get; set; } = new();
    }
}
