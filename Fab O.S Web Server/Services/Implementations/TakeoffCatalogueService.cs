using FabOS.WebServer.Data.Contexts;
using FabOS.WebServer.Models.Entities;
using FabOS.WebServer.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace FabOS.WebServer.Services.Implementations
{
    /// <summary>
    /// Implementation of takeoff catalogue service with measurement calculations
    /// </summary>
    public class TakeoffCatalogueService : ITakeoffCatalogueService
    {
        private readonly IDbContextFactory<ApplicationDbContext> _contextFactory;
        private readonly ILogger<TakeoffCatalogueService> _logger;

        public TakeoffCatalogueService(
            IDbContextFactory<ApplicationDbContext> contextFactory,
            ILogger<TakeoffCatalogueService> logger)
        {
            _contextFactory = contextFactory;
            _logger = logger;
        }

        public async Task<List<string>> GetMaterialsAsync(int companyId)
        {
            try
            {
                await using var context = await _contextFactory.CreateDbContextAsync();

                var materials = await context.CatalogueItems
                    .Where(c => c.CompanyId == companyId)
                    .Select(c => c.Material)
                    .Distinct()
                    .OrderBy(m => m)
                    .ToListAsync();

                _logger.LogInformation("Retrieved {Count} materials for company {CompanyId}",
                    materials.Count, companyId);

                return materials;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving catalogue materials for company {CompanyId}", companyId);
                throw;
            }
        }

        public async Task<List<string>> GetCategoriesAsync(int companyId)
        {
            try
            {
                await using var context = await _contextFactory.CreateDbContextAsync();

                var categories = await context.CatalogueItems
                    .Where(c => c.CompanyId == companyId)
                    .Select(c => c.Category)
                    .Distinct()
                    .OrderBy(c => c)
                    .ToListAsync();

                _logger.LogInformation("Retrieved {Count} categories for company {CompanyId}",
                    categories.Count, companyId);

                return categories;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving catalogue categories for company {CompanyId}", companyId);
                throw;
            }
        }

        public async Task<List<string>> GetCategoriesByMaterialAsync(string material, int companyId)
        {
            try
            {
                await using var context = await _contextFactory.CreateDbContextAsync();

                var categories = await context.CatalogueItems
                    .Where(c => c.CompanyId == companyId && c.Material == material)
                    .Select(c => c.Category)
                    .Distinct()
                    .OrderBy(c => c)
                    .ToListAsync();

                _logger.LogInformation("Retrieved {Count} categories for material '{Material}' in company {CompanyId}",
                    categories.Count, material, companyId);

                return categories;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving categories for material '{Material}' in company {CompanyId}",
                    material, companyId);
                throw;
            }
        }

        public async Task<List<CatalogueItem>> GetItemsByCategoryAsync(string category, int companyId)
        {
            try
            {
                await using var context = await _contextFactory.CreateDbContextAsync();

                var items = await context.CatalogueItems
                    .Where(c => c.CompanyId == companyId && c.Category == category)
                    .OrderBy(c => c.ItemCode)
                    .ToListAsync();

                _logger.LogInformation("Retrieved {Count} items in category '{Category}' for company {CompanyId}",
                    items.Count, category, companyId);

                return items;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving catalogue items for category '{Category}' and company {CompanyId}",
                    category, companyId);
                throw;
            }
        }

        public async Task<List<CatalogueItem>> GetItemsByMaterialAndCategoryAsync(string material, string category, int companyId)
        {
            try
            {
                await using var context = await _contextFactory.CreateDbContextAsync();

                var items = await context.CatalogueItems
                    .Where(c => c.CompanyId == companyId && c.Material == material && c.Category == category)
                    .OrderBy(c => c.ItemCode)
                    .ToListAsync();

                _logger.LogInformation("Retrieved {Count} items for material '{Material}' and category '{Category}' in company {CompanyId}",
                    items.Count, material, category, companyId);

                return items;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving catalogue items for material '{Material}' and category '{Category}' in company {CompanyId}",
                    material, category, companyId);
                throw;
            }
        }

        public async Task<CatalogueItem?> GetItemByIdAsync(int id, int companyId)
        {
            try
            {
                await using var context = await _contextFactory.CreateDbContextAsync();

                // First, check if the item exists at all (for better diagnostic messaging)
                var anyItem = await context.CatalogueItems
                    .FirstOrDefaultAsync(c => c.Id == id);

                if (anyItem != null && anyItem.CompanyId != companyId)
                {
                    _logger.LogWarning(
                        "Catalogue item {Id} exists but belongs to company {ActualCompanyId}, user is in company {RequestedCompanyId}",
                        id, anyItem.CompanyId, companyId);

                    // If companyId is 0 or invalid, try to return the item anyway for development
                    if (companyId == 0)
                    {
                        _logger.LogWarning("CompanyId is 0 - returning item anyway for development purposes");
                        return anyItem;
                    }
                }

                var item = await context.CatalogueItems
                    .FirstOrDefaultAsync(c => c.Id == id && c.CompanyId == companyId);

                if (item == null)
                {
                    _logger.LogWarning("Catalogue item {Id} not found for company {CompanyId}", id, companyId);
                }

                return item;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving catalogue item {Id} for company {CompanyId}", id, companyId);
                throw;
            }
        }

        public async Task<List<CatalogueItem>> SearchItemsAsync(string searchTerm, int companyId, int maxResults = 50)
        {
            try
            {
                await using var context = await _contextFactory.CreateDbContextAsync();

                var items = await context.CatalogueItems
                    .Where(c => c.CompanyId == companyId &&
                        (c.ItemCode.Contains(searchTerm) ||
                         c.Description.Contains(searchTerm) ||
                         c.Material.Contains(searchTerm)))
                    .OrderBy(c => c.ItemCode)
                    .Take(maxResults)
                    .ToListAsync();

                _logger.LogInformation("Search for '{SearchTerm}' returned {Count} items for company {CompanyId}",
                    searchTerm, items.Count, companyId);

                return items;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error searching catalogue items for '{SearchTerm}' and company {CompanyId}",
                    searchTerm, companyId);
                throw;
            }
        }

        public async Task<MeasurementCalculationResult> CalculateMeasurementAsync(
            int catalogueItemId,
            string measurementType,
            decimal measurementValue,
            string unit,
            int companyId)
        {
            try
            {
                var item = await GetItemByIdAsync(catalogueItemId, companyId);
                if (item == null)
                {
                    throw new ArgumentException($"Catalogue item {catalogueItemId} not found");
                }

                var result = new MeasurementCalculationResult
                {
                    CatalogueItemId = item.Id,
                    ItemCode = item.ItemCode,
                    Description = item.Description,
                    MeasurementType = measurementType,
                    MeasurementValue = measurementValue,
                    Unit = unit,
                    Material = item.Material,
                    Grade = item.Grade,
                    Mass_kg_m = item.Mass_kg_m,
                    Mass_kg_m2 = item.Mass_kg_m2,
                    Weight_kg = item.Weight_kg
                };

                // Perform calculation based on measurement type
                switch (measurementType.ToLower())
                {
                    case "linear":
                    case "distance":
                    case "perimeter":
                        // For linear measurements, use Mass_kg_m
                        result.Quantity = measurementValue; // meters
                        result.QuantityUnit = "m";

                        if (item.Mass_kg_m.HasValue)
                        {
                            result.Weight = result.Quantity * item.Mass_kg_m.Value;
                            result.WeightUnit = "kg";
                            result.CalculationFormula = $"{result.Quantity:F2} m × {item.Mass_kg_m.Value:F3} kg/m = {result.Weight:F2} kg";
                        }
                        else
                        {
                            result.CalculationFormula = "No mass per meter data available";
                        }
                        break;

                    case "area":
                    case "polygon":
                        // For area measurements, use Mass_kg_m2
                        result.Quantity = measurementValue; // square meters
                        result.QuantityUnit = "m²";

                        if (item.Mass_kg_m2.HasValue)
                        {
                            result.Weight = result.Quantity * item.Mass_kg_m2.Value;
                            result.WeightUnit = "kg";
                            result.CalculationFormula = $"{result.Quantity:F2} m² × {item.Mass_kg_m2.Value:F3} kg/m² = {result.Weight:F2} kg";
                        }
                        else
                        {
                            result.CalculationFormula = "No mass per square meter data available";
                        }
                        break;

                    case "count":
                    case "point":
                        // For count measurements, use Weight_kg directly
                        result.Quantity = measurementValue; // count
                        result.QuantityUnit = "pcs";

                        if (item.Weight_kg.HasValue)
                        {
                            result.Weight = result.Quantity * item.Weight_kg.Value;
                            result.WeightUnit = "kg";
                            result.CalculationFormula = $"{result.Quantity:F0} pcs × {item.Weight_kg.Value:F3} kg = {result.Weight:F2} kg";
                        }
                        else
                        {
                            result.CalculationFormula = "No weight per piece data available";
                        }
                        break;

                    default:
                        result.CalculationFormula = $"Unknown measurement type: {measurementType}";
                        _logger.LogWarning("Unknown measurement type: {MeasurementType}", measurementType);
                        break;
                }

                // TODO: Add cost calculation when pricing data is available
                // result.Cost = result.Weight * item.CostPerKg;

                _logger.LogInformation("Calculated measurement: {ItemCode} - {MeasurementType} - {Value} {Unit} = {Weight} kg",
                    item.ItemCode, measurementType, measurementValue, unit, result.Weight);

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error calculating measurement for item {CatalogueItemId}", catalogueItemId);
                throw;
            }
        }

        public async Task<TraceTakeoffMeasurement> CreateMeasurementAsync(
            int traceTakeoffId,
            int packageDrawingId,
            int catalogueItemId,
            string measurementType,
            decimal value,
            string unit,
            string? coordinates,
            int companyId,
            string? annotationId = null)
        {
            try
            {
                _logger.LogInformation("====== CreateMeasurementAsync CALLED ======");
                _logger.LogInformation("AnnotationId parameter: '{AnnotationId}'", annotationId ?? "NULL");

                // Calculate weight based on catalogue item
                var calculation = await CalculateMeasurementAsync(catalogueItemId, measurementType, value, unit, companyId);

                await using var context = await _contextFactory.CreateDbContextAsync();

                var measurement = new TraceTakeoffMeasurement
                {
                    TraceTakeoffId = traceTakeoffId,
                    PackageDrawingId = packageDrawingId,
                    CatalogueItemId = catalogueItemId,
                    MeasurementType = measurementType,
                    Value = value,
                    Unit = unit,
                    Coordinates = coordinates,
                    CalculatedWeight = calculation.Weight,
                    CreatedDate = DateTime.UtcNow
                };

                context.TraceTakeoffMeasurements.Add(measurement);
                await context.SaveChangesAsync();

                // If an annotation ID was provided, link it to this measurement
                if (!string.IsNullOrWhiteSpace(annotationId))
                {
                    var annotation = await context.PdfAnnotations
                        .FirstOrDefaultAsync(a => a.AnnotationId == annotationId && a.PackageDrawingId == packageDrawingId);

                    if (annotation != null)
                    {
                        annotation.TraceTakeoffMeasurementId = measurement.Id;
                        annotation.IsMeasurement = true;
                        annotation.ModifiedDate = DateTime.UtcNow;
                        await context.SaveChangesAsync();

                        _logger.LogInformation("✓ Linked annotation {AnnotationId} to measurement {MeasurementId}",
                            annotationId, measurement.Id);
                    }
                    else
                    {
                        _logger.LogWarning("Annotation {AnnotationId} not found for linking to measurement {MeasurementId}",
                            annotationId, measurement.Id);
                    }
                }

                // Load navigation properties
                await context.Entry(measurement)
                    .Reference(m => m.CatalogueItem)
                    .LoadAsync();

                _logger.LogInformation("Created measurement {Id} for drawing {DrawingId} with catalogue item {CatalogueItemId}",
                    measurement.Id, packageDrawingId, catalogueItemId);

                return measurement;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating measurement for drawing {DrawingId}", packageDrawingId);
                throw;
            }
        }

        public async Task<List<TraceTakeoffMeasurement>> GetMeasurementsByDrawingAsync(int packageDrawingId, int companyId)
        {
            try
            {
                await using var context = await _contextFactory.CreateDbContextAsync();

                var measurements = await context.TraceTakeoffMeasurements
                    .Include(m => m.CatalogueItem)
                    .Include(m => m.PackageDrawing)
                    .Where(m => m.PackageDrawingId == packageDrawingId)
                    .OrderByDescending(m => m.CreatedDate)
                    .ToListAsync();

                _logger.LogInformation("Retrieved {Count} measurements for drawing {DrawingId}",
                    measurements.Count, packageDrawingId);

                return measurements;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving measurements for drawing {DrawingId}", packageDrawingId);
                throw;
            }
        }

        public async Task<TraceTakeoffMeasurement?> UpdateMeasurementAsync(
            int measurementId,
            decimal value,
            string? coordinates,
            int companyId)
        {
            try
            {
                await using var context = await _contextFactory.CreateDbContextAsync();

                var measurement = await context.TraceTakeoffMeasurements
                    .Include(m => m.PackageDrawing)
                        .ThenInclude(pd => pd!.Package)
                    .FirstOrDefaultAsync(m => m.Id == measurementId);

                if (measurement == null)
                {
                    _logger.LogWarning("Measurement {Id} not found for company {CompanyId}", measurementId, companyId);
                    return null;
                }

                measurement.Value = value;
                measurement.Coordinates = coordinates;

                // Recalculate weight
                if (measurement.CatalogueItemId.HasValue)
                {
                    var calculation = await CalculateMeasurementAsync(
                        measurement.CatalogueItemId.Value,
                        measurement.MeasurementType,
                        value,
                        measurement.Unit,
                        companyId);

                    measurement.CalculatedWeight = calculation.Weight;
                }

                await context.SaveChangesAsync();

                _logger.LogInformation("Updated measurement {Id} with new value {Value}", measurementId, value);

                return measurement;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating measurement {Id}", measurementId);
                throw;
            }
        }

        public async Task<List<string>> DeleteMeasurementAsync(int measurementId, int companyId)
        {
            try
            {
                await using var context = await _contextFactory.CreateDbContextAsync();

                var measurement = await context.TraceTakeoffMeasurements
                    .Include(m => m.PackageDrawing)
                        .ThenInclude(pd => pd!.Package)
                    .FirstOrDefaultAsync(m => m.Id == measurementId);

                if (measurement == null)
                {
                    _logger.LogWarning("Measurement {Id} not found for company {CompanyId}", measurementId, companyId);
                    return new List<string>();
                }

                // CASCADE DELETE: Find and delete all annotations linked to this measurement
                var linkedAnnotations = await context.PdfAnnotations
                    .Where(a => a.TraceTakeoffMeasurementId == measurementId)
                    .ToListAsync();

                // Store annotation IDs to return (so they can be deleted from PDF viewer)
                var annotationIds = linkedAnnotations.Select(a => a.AnnotationId).ToList();

                if (linkedAnnotations.Any())
                {
                    context.PdfAnnotations.RemoveRange(linkedAnnotations);
                    _logger.LogInformation("Cascade deleting {Count} linked annotation(s) for measurement {MeasurementId}",
                        linkedAnnotations.Count, measurementId);
                }

                // Now delete the measurement
                context.TraceTakeoffMeasurements.Remove(measurement);
                await context.SaveChangesAsync();

                _logger.LogInformation("Deleted measurement {Id}", measurementId);

                return annotationIds;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting measurement {Id}", measurementId);
                throw;
            }
        }

        public async Task<DrawingMeasurementSummary> GetDrawingSummaryAsync(int packageDrawingId, int companyId)
        {
            try
            {
                var measurements = await GetMeasurementsByDrawingAsync(packageDrawingId, companyId);

                var summary = new DrawingMeasurementSummary
                {
                    PackageDrawingId = packageDrawingId,
                    TotalMeasurements = measurements.Count,
                    TotalWeight = measurements.Sum(m => m.CalculatedWeight ?? 0),
                    WeightUnit = "kg",
                    Measurements = measurements
                };

                // Breakdown by measurement type
                summary.MeasurementTypeBreakdown = measurements
                    .GroupBy(m => m.MeasurementType)
                    .ToDictionary(g => g.Key, g => g.Count());

                // Breakdown by category
                summary.CategoryWeightBreakdown = measurements
                    .Where(m => m.CatalogueItem != null)
                    .GroupBy(m => m.CatalogueItem!.Category)
                    .ToDictionary(
                        g => g.Key,
                        g => g.Sum(m => m.CalculatedWeight ?? 0)
                    );

                // TODO: Calculate total cost when pricing data is available
                // summary.TotalCost = measurements.Sum(m => m.CalculatedCost ?? 0);

                _logger.LogInformation("Generated summary for drawing {DrawingId}: {Count} measurements, {Weight} kg",
                    packageDrawingId, summary.TotalMeasurements, summary.TotalWeight);

                return summary;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating summary for drawing {DrawingId}", packageDrawingId);
                throw;
            }
        }

        public async Task<int?> GetTraceTakeoffIdFromPackageDrawingAsync(int packageDrawingId)
        {
            try
            {
                await using var context = await _contextFactory.CreateDbContextAsync();

                // Follow the hierarchy: PackageDrawing → Package → TakeoffRevision → TraceTakeoff
                var drawing = await context.PackageDrawings
                    .Include(pd => pd.Package)
                        .ThenInclude(p => p!.Revision)
                            .ThenInclude(r => r!.Takeoff)
                    .FirstOrDefaultAsync(pd => pd.Id == packageDrawingId);

                if (drawing == null)
                {
                    _logger.LogWarning("PackageDrawing {DrawingId} not found", packageDrawingId);
                    return null;
                }

                if (drawing.Package == null)
                {
                    _logger.LogWarning("PackageDrawing {DrawingId} is not linked to a Package", packageDrawingId);
                    return null;
                }

                if (drawing.Package.Revision == null)
                {
                    _logger.LogWarning("Package {PackageId} is not linked to a TakeoffRevision", drawing.PackageId);
                    return null;
                }

                if (drawing.Package.Revision.Takeoff == null)
                {
                    _logger.LogWarning("TakeoffRevision {RevisionId} is not linked to a TraceTakeoff", drawing.Package.RevisionId);
                    return null;
                }

                var takeoffId = drawing.Package.Revision.Takeoff.Id;
                _logger.LogInformation("Resolved TraceTakeoffId {TakeoffId} for PackageDrawing {DrawingId}", takeoffId, packageDrawingId);

                return takeoffId;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting TraceTakeoffId for PackageDrawing {DrawingId}", packageDrawingId);
                throw;
            }
        }
    }
}
