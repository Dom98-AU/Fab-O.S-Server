using FabOS.WebServer.Services.Interfaces;
using FabOS.WebServer.Models.Entities;
using FabOS.WebServer.Data.Contexts;
using Microsoft.EntityFrameworkCore;
using OfficeOpenXml;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace FabOS.WebServer.Services.Implementations
{
    public class TakeoffService : ITakeoffService
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<TakeoffService> _logger;

        public TakeoffService(ApplicationDbContext context, ILogger<TakeoffService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<TraceTakeoff> CreateTakeoffSessionAsync(int traceRecordId, string pdfUrl, int? drawingId = null, int? companyId = null)
        {
            // Validate companyId is provided
            if (!companyId.HasValue || companyId.Value <= 0)
            {
                throw new ArgumentException("CompanyId is required for creating a takeoff session", nameof(companyId));
            }

            var takeoff = new TraceTakeoff
            {
                TraceRecordId = traceRecordId,
                PdfUrl = pdfUrl,
                DrawingId = drawingId,
                Status = "Draft",
                CreatedDate = DateTime.UtcNow,
                CompanyId = companyId.Value
            };

            _context.TraceTakeoffs.Add(takeoff);
            await _context.SaveChangesAsync();
            return takeoff;
        }

        public async Task<TraceTakeoff> GetTakeoffSessionAsync(int takeoffId)
        {
            return await _context.TraceTakeoffs
                .Include(t => t.Measurements)
                .FirstOrDefaultAsync(t => t.Id == takeoffId);
        }

        public async Task<TraceTakeoff> UpdateTakeoffSessionAsync(TraceTakeoff takeoff)
        {
            takeoff.ModifiedDate = DateTime.UtcNow;
            _context.TraceTakeoffs.Update(takeoff);
            await _context.SaveChangesAsync();
            return takeoff;
        }

        public async Task<bool> DeleteTakeoffSessionAsync(int takeoffId)
        {
            var takeoff = await _context.TraceTakeoffs.FindAsync(takeoffId);
            if (takeoff == null) return false;

            _context.TraceTakeoffs.Remove(takeoff);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<List<TraceTakeoff>> GetTakeoffsByProjectAsync(int projectId, int companyId)
        {
            if (companyId <= 0)
            {
                throw new ArgumentException("CompanyId is required", nameof(companyId));
            }

            return await _context.TraceTakeoffs
                .Where(t => t.CompanyId == companyId)
                .ToListAsync();
        }

        public async Task<TraceTakeoffMeasurement> AddMeasurementAsync(TraceTakeoffMeasurement measurement)
        {
            // Validate measurement
            if (measurement.Value < 0)
            {
                throw new ArgumentException("Measurement value cannot be negative", nameof(measurement.Value));
            }

            if (string.IsNullOrWhiteSpace(measurement.Unit))
            {
                measurement.Unit = "mm"; // Default to mm for Australian standards
            }

            // Validate JSON coordinates if provided
            if (!string.IsNullOrWhiteSpace(measurement.Coordinates))
            {
                try
                {
                    // Validate JSON structure
                    System.Text.Json.JsonDocument.Parse(measurement.Coordinates);

                    // Limit JSON length to prevent database issues
                    if (measurement.Coordinates.Length > 4000)
                    {
                        _logger.LogWarning($"Coordinates JSON exceeds maximum length for measurement {measurement.Id}");
                        measurement.Coordinates = measurement.Coordinates.Substring(0, 4000);
                    }
                }
                catch (System.Text.Json.JsonException ex)
                {
                    _logger.LogError($"Invalid JSON in measurement coordinates: {ex.Message}");
                    measurement.Coordinates = null; // Clear invalid JSON
                }
            }

            measurement.CreatedDate = DateTime.UtcNow;
            _context.TraceTakeoffMeasurements.Add(measurement);
            await _context.SaveChangesAsync();
            return measurement;
        }

        public async Task<TraceTakeoffMeasurement> UpdateMeasurementAsync(TraceTakeoffMeasurement measurement)
        {
            // Validate measurement
            if (measurement.Value < 0)
            {
                throw new ArgumentException("Measurement value cannot be negative", nameof(measurement.Value));
            }

            // Validate JSON coordinates if provided
            if (!string.IsNullOrWhiteSpace(measurement.Coordinates))
            {
                try
                {
                    System.Text.Json.JsonDocument.Parse(measurement.Coordinates);

                    if (measurement.Coordinates.Length > 4000)
                    {
                        measurement.Coordinates = measurement.Coordinates.Substring(0, 4000);
                    }
                }
                catch (System.Text.Json.JsonException)
                {
                    measurement.Coordinates = null;
                }
            }

            _context.TraceTakeoffMeasurements.Update(measurement);
            await _context.SaveChangesAsync();
            return measurement;
        }

        public async Task<bool> DeleteMeasurementAsync(int measurementId)
        {
            var measurement = await _context.TraceTakeoffMeasurements.FindAsync(measurementId);
            if (measurement == null) return false;

            _context.TraceTakeoffMeasurements.Remove(measurement);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<List<TraceTakeoffMeasurement>> GetMeasurementsByTakeoffAsync(int takeoffId)
        {
            return await _context.TraceTakeoffMeasurements
                .Where(m => m.TraceTakeoffId == takeoffId)
                .Include(m => m.CatalogueItem)
                .ToListAsync();
        }

        public async Task<List<TraceTakeoffMeasurement>> GetMeasurementsByTypeAsync(int takeoffId, string measurementType)
        {
            return await _context.TraceTakeoffMeasurements
                .Where(m => m.TraceTakeoffId == takeoffId && m.MeasurementType == measurementType)
                .Include(m => m.CatalogueItem)
                .ToListAsync();
        }

        public async Task<List<CatalogueItem>> FindMatchingCatalogueItemsAsync(TraceTakeoffMeasurement measurement)
        {
            var query = _context.CatalogueItems.AsQueryable();

            // Convert measurement value to mm for consistent comparison
            decimal valueMm = ConvertLength(measurement.Value, measurement.Unit ?? "mm", "mm");
            decimal tolerance = 5; // 5mm tolerance

            // Match based on measurement type and value
            if (measurement.MeasurementType == "Linear")
            {
                // Look for items with matching dimensions within tolerance
                query = query.Where(c =>
                    (c.Length_mm.HasValue && Math.Abs(c.Length_mm.Value - valueMm) <= tolerance) ||
                    (c.Width_mm.HasValue && Math.Abs(c.Width_mm.Value - valueMm) <= tolerance) ||
                    (c.Height_mm.HasValue && Math.Abs(c.Height_mm.Value - valueMm) <= tolerance) ||
                    (c.Depth_mm.HasValue && Math.Abs(c.Depth_mm.Value - valueMm) <= tolerance) ||
                    (c.StandardLength_m.HasValue && Math.Abs(c.StandardLength_m.Value * 1000 - valueMm) <= tolerance));
            }
            else if (measurement.MeasurementType == "Area")
            {
                // For area measurements, look for plates or sheets
                query = query.Where(c =>
                    c.Category == "Plates" ||
                    c.Category == "Cold Rolled Sheet" ||
                    c.Category == "Galvanized Sheet" ||
                    c.Category == "Tread Plates");
            }

            // If there's a description, use it for additional filtering
            if (!string.IsNullOrEmpty(measurement.Description))
            {
                var spec = ParseMaterialSpecification(measurement.Description);

                if (!string.IsNullOrEmpty(spec.Material))
                {
                    query = query.Where(c => c.Material.Contains(spec.Material));
                }

                if (!string.IsNullOrEmpty(spec.Grade))
                {
                    query = query.Where(c => c.Grade == spec.Grade);
                }

                if (spec.Properties.ContainsKey("ProductType"))
                {
                    var productType = spec.Properties["ProductType"];
                    query = query.Where(c => c.Category.Contains(productType));
                }
            }

            return await query.Take(20).ToListAsync();
        }

        public async Task<CatalogueItem> AutoMatchCatalogueItemAsync(string description, decimal? dimension1 = null, decimal? dimension2 = null)
        {
            // Extract dimensions from description if not provided
            if (!dimension1.HasValue && !string.IsNullOrEmpty(description))
            {
                var extractedDims = ExtractDimensionsFromText(description);
                if (extractedDims.Count > 0)
                {
                    dimension1 = extractedDims[0];
                    if (extractedDims.Count > 1)
                        dimension2 = extractedDims[1];
                }
            }

            // Parse material specification from description
            var spec = ParseMaterialSpecification(description);

            var query = _context.CatalogueItems.AsQueryable();

            // Filter by material type if detected
            if (!string.IsNullOrEmpty(spec.Material))
            {
                query = query.Where(c => c.Material.ToLower() == spec.Material.ToLower());
            }

            // Filter by grade if detected
            if (!string.IsNullOrEmpty(spec.Grade))
            {
                query = query.Where(c => c.Grade == spec.Grade);
            }

            // Filter by product type if detected (RHS, SHS, CHS, etc.)
            if (spec.Properties.ContainsKey("ProductType"))
            {
                var productType = spec.Properties["ProductType"];
                query = query.Where(c => c.Category.Contains(productType));
            }

            // Match by dimensions with tolerance
            decimal tolerance = 5; // 5mm tolerance
            if (dimension1.HasValue)
            {
                decimal dim1Mm = dimension1.Value; // Assume already in mm

                if (dimension2.HasValue)
                {
                    decimal dim2Mm = dimension2.Value;

                    // Match both dimensions (for rectangular sections)
                    query = query.Where(c =>
                        ((c.Width_mm.HasValue && Math.Abs(c.Width_mm.Value - dim1Mm) <= tolerance &&
                          c.Height_mm.HasValue && Math.Abs(c.Height_mm.Value - dim2Mm) <= tolerance) ||
                         (c.Width_mm.HasValue && Math.Abs(c.Width_mm.Value - dim2Mm) <= tolerance &&
                          c.Height_mm.HasValue && Math.Abs(c.Height_mm.Value - dim1Mm) <= tolerance)) ||
                        ((c.Width_mm.HasValue && Math.Abs(c.Width_mm.Value - dim1Mm) <= tolerance &&
                          c.Depth_mm.HasValue && Math.Abs(c.Depth_mm.Value - dim2Mm) <= tolerance) ||
                         (c.Width_mm.HasValue && Math.Abs(c.Width_mm.Value - dim2Mm) <= tolerance &&
                          c.Depth_mm.HasValue && Math.Abs(c.Depth_mm.Value - dim1Mm) <= tolerance)));
                }
                else
                {
                    // Match single dimension (for square sections or diameter)
                    query = query.Where(c =>
                        (c.Diameter_mm.HasValue && Math.Abs(c.Diameter_mm.Value - dim1Mm) <= tolerance) ||
                        (c.Width_mm.HasValue && c.Height_mm.HasValue &&
                         c.Width_mm == c.Height_mm && Math.Abs(c.Width_mm.Value - dim1Mm) <= tolerance) ||
                        (c.Width_mm.HasValue && Math.Abs(c.Width_mm.Value - dim1Mm) <= tolerance));
                }
            }
            else if (!string.IsNullOrEmpty(description))
            {
                // Fallback to fuzzy text matching if no dimensions
                var items = await query.ToListAsync();

                if (items.Any())
                {
                    var bestMatch = items
                        .Select(item => new
                        {
                            Item = item,
                            Score = CalculateSimilarityScore(description, item.Description)
                        })
                        .OrderByDescending(x => x.Score)
                        .FirstOrDefault(x => x.Score > 60); // At least 60% similarity

                    return bestMatch?.Item;
                }
            }

            return await query.FirstOrDefaultAsync();
        }

        public async Task<TraceTakeoffMeasurement> LinkMeasurementToCatalogueAsync(int measurementId, int catalogueItemId)
        {
            var measurement = await _context.TraceTakeoffMeasurements.FindAsync(measurementId);
            if (measurement != null)
            {
                measurement.CatalogueItemId = catalogueItemId;

                // Calculate weight
                var catalogueItem = await _context.CatalogueItems.FindAsync(catalogueItemId);
                if (catalogueItem != null)
                {
                    measurement.CalculatedWeight = CalculateWeight(catalogueItem, measurement.Value, measurement.Unit);
                }

                await _context.SaveChangesAsync();
            }
            return measurement;
        }

        public async Task<List<CatalogueSuggestion>> SuggestCatalogueItemsFromTextAsync(string extractedText)
        {
            var suggestions = new List<CatalogueSuggestion>();

            if (string.IsNullOrWhiteSpace(extractedText))
                return suggestions;

            // Parse the text for material specifications
            var spec = ParseMaterialSpecification(extractedText);

            // Extract dimensions from the text
            var dimensions = ExtractDimensionsFromText(extractedText);

            // Build query based on parsed information
            var query = _context.CatalogueItems.AsQueryable();

            // Apply filters based on parsed specification
            if (!string.IsNullOrEmpty(spec.Material))
            {
                query = query.Where(c => c.Material.ToLower().Contains(spec.Material.ToLower()));
            }

            if (!string.IsNullOrEmpty(spec.Grade))
            {
                query = query.Where(c => c.Grade == spec.Grade || (c.Grade != null && c.Grade.Contains(spec.Grade)));
            }

            if (!string.IsNullOrEmpty(spec.Standard))
            {
                query = query.Where(c => c.Standard == spec.Standard || (c.Standard != null && c.Standard.Contains(spec.Standard)));
            }

            if (!string.IsNullOrEmpty(spec.Finish))
            {
                query = query.Where(c => c.Finish != null && c.Finish.ToLower().Contains(spec.Finish.ToLower()));
            }

            // Apply dimension matching if dimensions were extracted
            if (dimensions.Count > 0)
            {
                decimal tolerance = 10; // 10mm tolerance for suggestions
                decimal dim1 = dimensions[0];

                if (dimensions.Count >= 3)
                {
                    // Three dimensions (likely width x height x thickness)
                    decimal dim2 = dimensions[1];
                    decimal dim3 = dimensions[2];

                    query = query.Where(c =>
                        (c.Width_mm.HasValue && Math.Abs(c.Width_mm.Value - dim1) <= tolerance &&
                         c.Height_mm.HasValue && Math.Abs(c.Height_mm.Value - dim2) <= tolerance &&
                         c.Thickness_mm.HasValue && Math.Abs(c.Thickness_mm.Value - dim3) <= tolerance) ||
                        (c.Width_mm.HasValue && Math.Abs(c.Width_mm.Value - dim1) <= tolerance &&
                         c.Depth_mm.HasValue && Math.Abs(c.Depth_mm.Value - dim2) <= tolerance &&
                         c.Thickness_mm.HasValue && Math.Abs(c.Thickness_mm.Value - dim3) <= tolerance));
                }
                else if (dimensions.Count == 2)
                {
                    // Two dimensions (likely width x height)
                    decimal dim2 = dimensions[1];

                    query = query.Where(c =>
                        (c.Width_mm.HasValue && Math.Abs(c.Width_mm.Value - dim1) <= tolerance &&
                         c.Height_mm.HasValue && Math.Abs(c.Height_mm.Value - dim2) <= tolerance) ||
                        (c.Width_mm.HasValue && Math.Abs(c.Width_mm.Value - dim2) <= tolerance &&
                         c.Height_mm.HasValue && Math.Abs(c.Height_mm.Value - dim1) <= tolerance));
                }
                else
                {
                    // Single dimension (diameter or square section)
                    query = query.Where(c =>
                        (c.Diameter_mm.HasValue && Math.Abs(c.Diameter_mm.Value - dim1) <= tolerance) ||
                        (c.Width_mm.HasValue && c.Height_mm.HasValue && c.Width_mm == c.Height_mm &&
                         Math.Abs(c.Width_mm.Value - dim1) <= tolerance));
                }
            }

            // Execute query and score results
            var items = await query.Take(50).ToListAsync();

            foreach (var item in items)
            {
                decimal confidenceScore = 0;
                var matchReasons = new List<string>();
                var matchedProps = new Dictionary<string, string>();

                // Calculate confidence based on different matches
                if (!string.IsNullOrEmpty(spec.Material) && item.Material?.ToLower() == spec.Material.ToLower())
                {
                    confidenceScore += 20;
                    matchReasons.Add("Material match");
                    matchedProps["Material"] = item.Material;
                }

                if (!string.IsNullOrEmpty(spec.Grade) && item.Grade == spec.Grade)
                {
                    confidenceScore += 25;
                    matchReasons.Add("Grade match");
                    matchedProps["Grade"] = item.Grade;
                }

                if (dimensions.Count > 0)
                {
                    bool dimensionMatch = false;
                    if (dimensions.Count >= 3 && item.Width_mm.HasValue && item.Height_mm.HasValue && item.Thickness_mm.HasValue)
                    {
                        if (DimensionsMatchWithTolerance(dimensions[0], item.Width_mm.Value, 5) &&
                            DimensionsMatchWithTolerance(dimensions[1], item.Height_mm.Value, 5) &&
                            DimensionsMatchWithTolerance(dimensions[2], item.Thickness_mm.Value, 2))
                        {
                            dimensionMatch = true;
                            confidenceScore += 40;
                            matchReasons.Add($"Dimensions match ({item.Width_mm}x{item.Height_mm}x{item.Thickness_mm}mm)");
                        }
                    }
                    else if (dimensions.Count == 2 && item.Width_mm.HasValue && item.Height_mm.HasValue)
                    {
                        if (DimensionsMatchWithTolerance(dimensions[0], item.Width_mm.Value, 5) &&
                            DimensionsMatchWithTolerance(dimensions[1], item.Height_mm.Value, 5))
                        {
                            dimensionMatch = true;
                            confidenceScore += 35;
                            matchReasons.Add($"Dimensions match ({item.Width_mm}x{item.Height_mm}mm)");
                        }
                    }
                    else if (dimensions.Count == 1 && item.Diameter_mm.HasValue)
                    {
                        if (DimensionsMatchWithTolerance(dimensions[0], item.Diameter_mm.Value, 5))
                        {
                            dimensionMatch = true;
                            confidenceScore += 30;
                            matchReasons.Add($"Diameter match ({item.Diameter_mm}mm)");
                        }
                    }

                    if (dimensionMatch)
                    {
                        matchedProps["Dimensions"] = "Matched";
                    }
                }

                // Text similarity check
                decimal textSimilarity = CalculateSimilarityScore(extractedText, item.Description);
                if (textSimilarity > 50)
                {
                    confidenceScore += textSimilarity / 5; // Max 20 points from text similarity
                    matchReasons.Add($"Text similarity: {textSimilarity:F0}%");
                }

                // Normalize confidence score to 0-1 range
                confidenceScore = Math.Min(100, confidenceScore) / 100;

                if (confidenceScore > 0.3m) // Only include if confidence > 30%
                {
                    suggestions.Add(new CatalogueSuggestion
                    {
                        Item = item,
                        ConfidenceScore = confidenceScore,
                        MatchReason = string.Join(", ", matchReasons),
                        MatchedProperties = matchedProps
                    });
                }
            }

            // Sort by confidence score and return top suggestions
            return suggestions
                .OrderByDescending(s => s.ConfidenceScore)
                .Take(10)
                .ToList();
        }

        public async Task<TakeoffQuantity> CalculateLinearQuantityAsync(int measurementId, string targetUnit = "m")
        {
            var measurement = await _context.TraceTakeoffMeasurements.FindAsync(measurementId);
            if (measurement == null) return null;

            return new TakeoffQuantity
            {
                MeasurementId = measurementId,
                Quantity = measurement.Value,
                Unit = measurement.Unit,
                MeasurementType = "Linear",
                ConvertedQuantity = ConvertLength(measurement.Value, measurement.Unit, targetUnit),
                ConvertedUnit = targetUnit
            };
        }

        public async Task<TakeoffQuantity> CalculateAreaQuantityAsync(int measurementId, string targetUnit = "m2")
        {
            var measurement = await _context.TraceTakeoffMeasurements.FindAsync(measurementId);
            if (measurement == null) return null;

            return new TakeoffQuantity
            {
                MeasurementId = measurementId,
                Quantity = measurement.Value,
                Unit = measurement.Unit,
                MeasurementType = "Area",
                ConvertedQuantity = ConvertArea(measurement.Value, measurement.Unit, targetUnit),
                ConvertedUnit = targetUnit
            };
        }

        public async Task<TakeoffQuantity> CalculateCountQuantityAsync(int measurementId)
        {
            var measurement = await _context.TraceTakeoffMeasurements.FindAsync(measurementId);
            if (measurement == null) return null;

            return new TakeoffQuantity
            {
                MeasurementId = measurementId,
                Quantity = measurement.Value,
                Unit = "EA",
                MeasurementType = "Count"
            };
        }

        public async Task<TakeoffQuantity> CalculateVolumeQuantityAsync(int measurementId, decimal thickness, string targetUnit = "m3")
        {
            var measurement = await _context.TraceTakeoffMeasurements.FindAsync(measurementId);
            if (measurement == null) return null;

            decimal volume = measurement.Value * thickness;

            return new TakeoffQuantity
            {
                MeasurementId = measurementId,
                Quantity = volume,
                Unit = targetUnit,
                MeasurementType = "Volume",
                ConvertedQuantity = volume,
                ConvertedUnit = targetUnit
            };
        }

        public async Task<List<TakeoffQuantity>> CalculateAllQuantitiesAsync(int takeoffId)
        {
            var measurements = await GetMeasurementsByTakeoffAsync(takeoffId);
            var quantities = new List<TakeoffQuantity>();

            foreach (var measurement in measurements)
            {
                TakeoffQuantity quantity = measurement.MeasurementType switch
                {
                    "Linear" => await CalculateLinearQuantityAsync(measurement.Id),
                    "Area" => await CalculateAreaQuantityAsync(measurement.Id),
                    "Count" => await CalculateCountQuantityAsync(measurement.Id),
                    _ => null
                };

                if (quantity != null)
                    quantities.Add(quantity);
            }

            return quantities;
        }

        public async Task<WeightCalculation> CalculateWeightFromMeasurementAsync(int measurementId)
        {
            var measurement = await _context.TraceTakeoffMeasurements
                .Include(m => m.CatalogueItem)
                .FirstOrDefaultAsync(m => m.Id == measurementId);

            if (measurement?.CatalogueItem == null) return null;

            var weight = CalculateWeight(measurement.CatalogueItem, measurement.Value, measurement.Unit);

            return new WeightCalculation
            {
                ItemId = measurement.CatalogueItem.Id,
                ItemCode = measurement.CatalogueItem.ItemCode,
                Description = measurement.CatalogueItem.Description,
                Quantity = measurement.Value,
                QuantityUnit = measurement.Unit,
                Weight = weight,
                WeightUnit = "kg",
                UnitWeight = measurement.Value > 0 ? weight / measurement.Value : 0,
                CalculationMethod = "From catalogue"
            };
        }

        public async Task<WeightCalculation> CalculateWeightFromCatalogueAsync(int catalogueItemId, decimal quantity, string unit)
        {
            var item = await _context.CatalogueItems.FindAsync(catalogueItemId);
            if (item == null) return null;

            var weight = CalculateWeight(item, quantity, unit);

            return new WeightCalculation
            {
                ItemId = catalogueItemId,
                ItemCode = item.ItemCode,
                Description = item.Description,
                Quantity = quantity,
                QuantityUnit = unit,
                Weight = weight,
                WeightUnit = "kg",
                UnitWeight = quantity > 0 ? weight / quantity : 0,
                CalculationMethod = "From catalogue"
            };
        }

        public async Task<TotalWeightSummary> CalculateTotalWeightAsync(int takeoffId)
        {
            var measurements = await GetMeasurementsByTakeoffAsync(takeoffId);

            var summary = new TotalWeightSummary
            {
                TakeoffId = takeoffId,
                WeightByCategory = new Dictionary<string, decimal>(),
                WeightByMaterial = new Dictionary<string, decimal>(),
                ItemWeights = new List<WeightCalculation>()
            };

            foreach (var measurement in measurements.Where(m => m.CatalogueItem != null))
            {
                var weightCalc = await CalculateWeightFromMeasurementAsync(measurement.Id);
                if (weightCalc != null)
                {
                    summary.ItemWeights.Add(weightCalc);
                    summary.TotalWeight += weightCalc.Weight;

                    // Group by category
                    var category = measurement.CatalogueItem.Category ?? "Other";
                    if (!summary.WeightByCategory.ContainsKey(category))
                        summary.WeightByCategory[category] = 0;
                    summary.WeightByCategory[category] += weightCalc.Weight;

                    // Group by material
                    var material = measurement.CatalogueItem.Material ?? "Unknown";
                    if (!summary.WeightByMaterial.ContainsKey(material))
                        summary.WeightByMaterial[material] = 0;
                    summary.WeightByMaterial[material] += weightCalc.Weight;
                }
            }

            summary.WeightUnit = "kg";
            return summary;
        }

        public async Task<BillOfMaterials> GenerateBOMAsync(int takeoffId)
        {
            var measurements = await GetMeasurementsByTakeoffAsync(takeoffId);
            var takeoff = await GetTakeoffSessionAsync(takeoffId);

            var bom = new BillOfMaterials
            {
                TakeoffId = takeoffId,
                ProjectName = $"Takeoff {takeoffId}",
                GeneratedDate = DateTime.UtcNow,
                Items = new List<BOMItem>()
            };

            // Group measurements by catalogue item
            var groupedItems = measurements
                .Where(m => m.CatalogueItemId.HasValue)
                .GroupBy(m => m.CatalogueItemId.Value);

            int itemNumber = 1;
            foreach (var group in groupedItems)
            {
                var firstMeasurement = group.First();
                var catalogueItem = firstMeasurement.CatalogueItem;

                // Skip if catalogue item wasn't loaded properly
                if (catalogueItem == null)
                {
                    _logger.LogWarning($"Catalogue item {group.Key} not found for measurements in takeoff {takeoffId}");
                    continue;
                }

                var totalQuantity = group.Sum(m => m.Value);
                var weight = CalculateWeight(catalogueItem, totalQuantity, firstMeasurement.Unit);

                var bomItem = new BOMItem
                {
                    ItemNumber = itemNumber++,
                    CatalogueItemId = group.Key,
                    ItemCode = catalogueItem.ItemCode ?? "Unknown",
                    Description = catalogueItem.Description ?? "No description",
                    Category = catalogueItem.Category ?? "Uncategorized",
                    Material = catalogueItem.Material ?? "Unknown",
                    Quantity = totalQuantity,
                    Unit = firstMeasurement.Unit,
                    Weight = weight,
                    MeasurementIds = group.Select(m => m.Id).ToList()
                };

                bom.Items.Add(bomItem);
                bom.TotalWeight += weight;
            }

            return bom;
        }

        public async Task<BillOfMaterials> GenerateBOMByPackageAsync(int packageId)
        {
            // TODO: Implement when package relationship is established
            throw new NotImplementedException();
        }

        public async Task<MaterialSummary> GenerateMaterialSummaryAsync(int takeoffId)
        {
            var bom = await GenerateBOMAsync(takeoffId);

            var summary = new MaterialSummary
            {
                MaterialGroups = new Dictionary<string, MaterialGroup>(),
                MissingCatalogueLinks = new List<string>()
            };

            foreach (var item in bom.Items)
            {
                var materialKey = $"{item.Material}_{item.Category}";

                if (!summary.MaterialGroups.ContainsKey(materialKey))
                {
                    summary.MaterialGroups[materialKey] = new MaterialGroup
                    {
                        Material = item.Material,
                        Category = item.Category,
                        Items = new List<BOMItem>()
                    };
                }

                summary.MaterialGroups[materialKey].Items.Add(item);
                summary.MaterialGroups[materialKey].TotalQuantity += item.Quantity;
                summary.MaterialGroups[materialKey].TotalWeight += item.Weight ?? 0;

                summary.TotalQuantity += item.Quantity;
                summary.TotalWeight += item.Weight ?? 0;
            }

            return summary;
        }

        public async Task<List<PurchaseRequirement>> GeneratePurchaseRequirementsAsync(int takeoffId)
        {
            var bom = await GenerateBOMAsync(takeoffId);
            var requirements = new List<PurchaseRequirement>();

            // Batch load all catalogue items to avoid N+1 queries
            var catalogueItemIds = bom.Items
                .Where(i => i.CatalogueItemId.HasValue)
                .Select(i => i.CatalogueItemId.Value)
                .Distinct()
                .ToList();

            var catalogueItems = await _context.CatalogueItems
                .Where(c => catalogueItemIds.Contains(c.Id))
                .ToDictionaryAsync(c => c.Id);

            foreach (var item in bom.Items)
            {
                if (!item.CatalogueItemId.HasValue ||
                    !catalogueItems.TryGetValue(item.CatalogueItemId.Value, out var catalogueItem))
                {
                    _logger.LogWarning($"Catalogue item {item.CatalogueItemId} not found for purchase requirements");
                    continue;
                }

                var requirement = new PurchaseRequirement
                {
                    CatalogueItemId = catalogueItem.Id,
                    ItemCode = catalogueItem.ItemCode ?? item.ItemCode,
                    Description = catalogueItem.Description ?? item.Description,
                    RequiredQuantity = item.Quantity,
                    Unit = item.Unit,
                    PackQty = catalogueItem.PackQty,
                    SupplierCode = catalogueItem.SupplierCode
                };

                // Calculate packs required
                if (catalogueItem.PackQty.HasValue && catalogueItem.PackQty.Value > 0)
                {
                    requirement.PacksRequired = (int)Math.Ceiling(item.Quantity / catalogueItem.PackQty.Value);
                    requirement.OrderQuantity = requirement.PacksRequired * catalogueItem.PackQty.Value;
                }
                else
                {
                    requirement.OrderQuantity = item.Quantity;
                }

                requirements.Add(requirement);
            }

            return requirements;
        }

        public async Task<byte[]> ExportToExcelAsync(int takeoffId)
        {
            var bom = await GenerateBOMAsync(takeoffId);

            using (var package = new ExcelPackage())
            {
                var worksheet = package.Workbook.Worksheets.Add("Bill of Materials");

                // Headers
                worksheet.Cells[1, 1].Value = "Item #";
                worksheet.Cells[1, 2].Value = "Item Code";
                worksheet.Cells[1, 3].Value = "Description";
                worksheet.Cells[1, 4].Value = "Category";
                worksheet.Cells[1, 5].Value = "Material";
                worksheet.Cells[1, 6].Value = "Quantity";
                worksheet.Cells[1, 7].Value = "Unit";
                worksheet.Cells[1, 8].Value = "Weight (kg)";

                // Data
                int row = 2;
                foreach (var item in bom.Items)
                {
                    worksheet.Cells[row, 1].Value = item.ItemNumber;
                    worksheet.Cells[row, 2].Value = item.ItemCode;
                    worksheet.Cells[row, 3].Value = item.Description;
                    worksheet.Cells[row, 4].Value = item.Category;
                    worksheet.Cells[row, 5].Value = item.Material;
                    worksheet.Cells[row, 6].Value = item.Quantity;
                    worksheet.Cells[row, 7].Value = item.Unit;
                    worksheet.Cells[row, 8].Value = item.Weight;
                    row++;
                }

                // Totals
                worksheet.Cells[row + 1, 6].Value = "Total Weight:";
                worksheet.Cells[row + 1, 8].Value = bom.TotalWeight;

                // Formatting
                worksheet.Cells[1, 1, 1, 8].Style.Font.Bold = true;
                worksheet.Cells.AutoFitColumns();

                return package.GetAsByteArray();
            }
        }

        public async Task<byte[]> ExportToCsvAsync(int takeoffId)
        {
            var bom = await GenerateBOMAsync(takeoffId);
            var csv = new StringBuilder();

            csv.AppendLine("Item #,Item Code,Description,Category,Material,Quantity,Unit,Weight (kg)");

            foreach (var item in bom.Items)
            {
                csv.AppendLine($"{item.ItemNumber},{item.ItemCode},{item.Description},{item.Category},{item.Material},{item.Quantity},{item.Unit},{item.Weight}");
            }

            csv.AppendLine($",,,,,Total Weight:,{bom.TotalWeight}");

            return Encoding.UTF8.GetBytes(csv.ToString());
        }

        public async Task<string> ExportToJsonAsync(int takeoffId)
        {
            var bom = await GenerateBOMAsync(takeoffId);
            return JsonSerializer.Serialize(bom, new JsonSerializerOptions { WriteIndented = true });
        }

        public async Task<byte[]> ExportToPdfReportAsync(int takeoffId)
        {
            // TODO: Implement PDF report generation
            throw new NotImplementedException("PDF report generation will be implemented in next phase");
        }

        public async Task<List<ExtractedItem>> ExtractItemsFromPdfTextAsync(string pdfText)
        {
            var extractedItems = new List<ExtractedItem>();

            if (string.IsNullOrWhiteSpace(pdfText))
                return extractedItems;

            // Split text into lines for processing
            var lines = pdfText.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);

            foreach (var line in lines)
            {
                // Extract material specifications
                var spec = ParseMaterialSpecification(line);

                // Extract dimensions
                var dimensions = ExtractDimensionsFromText(line);

                if (!string.IsNullOrEmpty(spec.Material) || dimensions.Count > 0)
                {
                    var properties = new Dictionary<string, object>();

                    if (!string.IsNullOrEmpty(spec.Material))
                        properties["Material"] = spec.Material;
                    if (!string.IsNullOrEmpty(spec.Grade))
                        properties["Grade"] = spec.Grade;
                    if (!string.IsNullOrEmpty(spec.Standard))
                        properties["Standard"] = spec.Standard;
                    if (!string.IsNullOrEmpty(spec.Finish))
                        properties["Finish"] = spec.Finish;
                    if (dimensions.Count > 0)
                        properties["Dimensions"] = dimensions;

                    // Merge additional properties from spec
                    foreach (var prop in spec.Properties)
                    {
                        properties[prop.Key] = prop.Value;
                    }

                    extractedItems.Add(new ExtractedItem
                    {
                        Text = line,
                        ItemType = DetermineItemType(spec, dimensions),
                        Properties = properties,
                        ConfidenceScore = CalculateExtractionConfidence(spec, dimensions)
                    });
                }
            }

            return await Task.FromResult(extractedItems);
        }

        public async Task<List<DimensionExtraction>> ExtractDimensionsFromTextAsync(string text)
        {
            var extractions = new List<DimensionExtraction>();

            if (string.IsNullOrWhiteSpace(text))
                return extractions;

            var dimensions = ExtractDimensionsFromText(text);

            // Convert each dimension to a separate extraction
            for (int i = 0; i < dimensions.Count; i++)
            {
                string dimensionType;

                if (dimensions.Count == 1)
                {
                    dimensionType = "Diameter or Width";
                }
                else if (dimensions.Count == 2)
                {
                    dimensionType = i == 0 ? "Width" : "Height";
                }
                else if (dimensions.Count >= 3)
                {
                    if (i == 0) dimensionType = "Width";
                    else if (i == 1) dimensionType = "Height";
                    else dimensionType = "Thickness";
                }
                else
                {
                    dimensionType = $"Dimension {i + 1}";
                }

                extractions.Add(new DimensionExtraction
                {
                    OriginalText = text,
                    Value = dimensions[i],
                    Unit = "mm",
                    DimensionType = dimensionType
                });
            }

            return await Task.FromResult(extractions);
        }

        public async Task<MaterialSpecification> ExtractMaterialSpecAsync(string text)
        {
            return await Task.FromResult(ParseMaterialSpecification(text));
        }

        public async Task<ValidationResult> ValidateMeasurementsAsync(int takeoffId)
        {
            var measurements = await GetMeasurementsByTakeoffAsync(takeoffId);

            var result = new ValidationResult
            {
                IsValid = true,
                Errors = new List<string>(),
                Warnings = new List<string>(),
                MeasurementIssues = new Dictionary<int, List<string>>()
            };

            foreach (var measurement in measurements)
            {
                var issues = new List<string>();

                // Check for catalogue link
                if (!measurement.CatalogueItemId.HasValue)
                {
                    issues.Add("No catalogue item linked");
                    result.Warnings.Add($"Measurement {measurement.Id} has no catalogue item linked");
                }

                // Check for valid value
                if (measurement.Value <= 0)
                {
                    issues.Add("Invalid value");
                    result.Errors.Add($"Measurement {measurement.Id} has invalid value");
                    result.IsValid = false;
                }

                if (issues.Any())
                {
                    result.MeasurementIssues[measurement.Id] = issues;
                }
            }

            return result;
        }

        public async Task<bool> CheckForDuplicateMeasurementsAsync(int takeoffId)
        {
            var measurements = await GetMeasurementsByTakeoffAsync(takeoffId);

            var duplicates = measurements
                .GroupBy(m => new { m.MeasurementType, m.Value, m.Unit })
                .Where(g => g.Count() > 1);

            return duplicates.Any();
        }

        public async Task<List<MeasurementConflict>> DetectMeasurementConflictsAsync(int takeoffId)
        {
            // TODO: Implement conflict detection logic
            return new List<MeasurementConflict>();
        }

        // Helper methods
        private decimal CalculateWeight(CatalogueItem item, decimal quantity, string unit)
        {
            if (item == null) return 0;

            // Convert quantity to appropriate unit if needed
            string normalizedUnit = unit.ToLower().Replace("²", "2");

            // Handle different weight calculation methods based on available data and unit
            decimal weight = 0;

            switch (normalizedUnit)
            {
                case "m" or "meter" or "metre":
                    if (item.Mass_kg_m.HasValue)
                    {
                        weight = quantity * item.Mass_kg_m.Value;
                    }
                    else if (item.Weight_kg.HasValue && item.StandardLength_m.HasValue && item.StandardLength_m.Value > 0)
                    {
                        // Calculate weight per meter from piece weight
                        decimal weightPerMeter = item.Weight_kg.Value / item.StandardLength_m.Value;
                        weight = quantity * weightPerMeter;
                    }
                    break;

                case "mm":
                    // Convert mm to m and calculate
                    decimal quantityInMeters = quantity / 1000;
                    if (item.Mass_kg_m.HasValue)
                    {
                        weight = quantityInMeters * item.Mass_kg_m.Value;
                    }
                    break;

                case "m2" or "m²" or "sqm":
                    if (item.Mass_kg_m2.HasValue)
                    {
                        weight = quantity * item.Mass_kg_m2.Value;
                    }
                    else if (item.Weight_kg.HasValue && item.Width_mm.HasValue && item.Length_mm.HasValue)
                    {
                        // Calculate weight per m² from piece dimensions
                        decimal areaMm2 = item.Width_mm.Value * item.Length_mm.Value;
                        decimal areaM2 = areaMm2 / 1000000; // Convert mm² to m²
                        if (areaM2 > 0)
                        {
                            decimal weightPerM2 = item.Weight_kg.Value / areaM2;
                            weight = quantity * weightPerM2;
                        }
                    }
                    break;

                case "mm2" or "mm²":
                    // Convert mm² to m² and calculate
                    decimal quantityInM2 = quantity / 1000000;
                    if (item.Mass_kg_m2.HasValue)
                    {
                        weight = quantityInM2 * item.Mass_kg_m2.Value;
                    }
                    break;

                case "ea" or "each" or "piece" or "count":
                    if (item.Weight_kg.HasValue)
                    {
                        weight = quantity * item.Weight_kg.Value;
                    }
                    else if (item.Mass_kg_m.HasValue && item.StandardLength_m.HasValue)
                    {
                        // Calculate piece weight from mass per meter
                        weight = quantity * (item.Mass_kg_m.Value * item.StandardLength_m.Value);
                    }
                    break;

                case "kg":
                    // If unit is already in kg, return quantity as is
                    weight = quantity;
                    break;

                default:
                    // Try to calculate from dimensions if available
                    if (item.Weight_kg.HasValue)
                    {
                        weight = quantity * item.Weight_kg.Value;
                    }
                    break;
            }

            return weight;
        }

        private decimal ConvertLength(decimal value, string fromUnit, string toUnit)
        {
            if (string.IsNullOrEmpty(fromUnit) || string.IsNullOrEmpty(toUnit))
            {
                _logger.LogWarning($"Invalid units provided for length conversion: from '{fromUnit}' to '{toUnit}'");
                return value;
            }

            if (fromUnit.Equals(toUnit, StringComparison.OrdinalIgnoreCase))
                return value;

            // Validate units are supported
            if (!IsValidLengthUnit(fromUnit) || !IsValidLengthUnit(toUnit))
            {
                _logger.LogWarning($"Unsupported length units: from '{fromUnit}' to '{toUnit}'");
                return value;
            }

            // Convert to meters first (metric only)
            decimal valueInMeters = fromUnit.ToLower() switch
            {
                "mm" => value / 1000m,
                "cm" => value / 100m,
                "m" => value,
                "km" => value * 1000m,
                _ => value
            };

            // Convert to target unit (metric only)
            return toUnit.ToLower() switch
            {
                "mm" => valueInMeters * 1000m,
                "cm" => valueInMeters * 100m,
                "m" => valueInMeters,
                "km" => valueInMeters / 1000m,
                _ => valueInMeters
            };
        }

        private bool IsValidLengthUnit(string unit)
        {
            var validUnits = new[] { "mm", "cm", "m", "km" };
            return validUnits.Contains(unit.ToLower());
        }

        private decimal ConvertArea(decimal value, string fromUnit, string toUnit)
        {
            // Simple area conversion
            decimal linearConversion = ConvertLength(1, fromUnit.Replace("2", ""), toUnit.Replace("2", ""));
            return value * linearConversion * linearConversion;
        }

        // New methods for enhanced functionality

        private decimal ConvertWeight(decimal value, string fromUnit, string toUnit)
        {
            if (fromUnit == toUnit) return value;

            // Convert to kg first
            decimal valueInKg = fromUnit.ToLower() switch
            {
                "g" => value / 1000,
                "kg" => value,
                "t" or "tonne" => value * 1000,
                _ => value
            };

            // Convert to target unit
            return toUnit.ToLower() switch
            {
                "g" => valueInKg * 1000,
                "kg" => valueInKg,
                "t" or "tonne" => valueInKg / 1000,
                _ => valueInKg
            };
        }

        // Extract dimensions from text like "100x50x3mm" or "150 x 150 x 10"
        private List<decimal> ExtractDimensionsFromText(string text)
        {
            var dimensions = new List<decimal>();

            // Pattern for dimensions like "100x50x3" or "100 x 50 x 3" with optional mm
            if (string.IsNullOrEmpty(text))
                return dimensions;

            var pattern = @"(\d+(?:\.\d+)?)\s*[xX×]\s*(\d+(?:\.\d+)?)(?:\s*[xX×]\s*(\d+(?:\.\d+)?))?(?:\s*mm)?";
            var match = Regex.Match(text, pattern);

            if (match.Success)
            {
                for (int i = 1; i < match.Groups.Count; i++)
                {
                    if (match.Groups[i].Success && decimal.TryParse(match.Groups[i].Value, out decimal dimension))
                    {
                        dimensions.Add(dimension);
                    }
                }
            }

            // Also try pattern for single dimensions like "10mm", "3.5mm"
            if (dimensions.Count == 0)
            {
                pattern = @"(\d+(?:\.\d+)?)\s*mm";
                var matches = Regex.Matches(text, pattern);
                foreach (Match m in matches)
                {
                    if (decimal.TryParse(m.Groups[1].Value, out decimal dimension))
                    {
                        dimensions.Add(dimension);
                    }
                }
            }

            return dimensions;
        }

        // Parse material specification from text
        private MaterialSpecification ParseMaterialSpecification(string text)
        {
            var spec = new MaterialSpecification
            {
                Properties = new Dictionary<string, string>()
            };

            // Extract material type
            if (Regex.IsMatch(text, @"\b(mild steel|stainless steel|aluminum|aluminium)\b", RegexOptions.IgnoreCase))
            {
                var materialMatch = Regex.Match(text, @"\b(mild steel|stainless steel|aluminum|aluminium)\b", RegexOptions.IgnoreCase);
                spec.Material = materialMatch.Value;
            }

            // Extract grade (Grade 250, Grade 350, 304, 316, etc.)
            var gradePattern = @"(?:Grade\s*)?(\d{3,4}[A-Z]?)|(?:AS\/?NZS?\s*\d{4})";
            var gradeMatch = Regex.Match(text, gradePattern, RegexOptions.IgnoreCase);
            if (gradeMatch.Success)
            {
                spec.Grade = gradeMatch.Value;
            }

            // Extract standard
            var standardPattern = @"AS\/?NZS?\s*\d{4}(?:\.\d+)?";
            var standardMatch = Regex.Match(text, standardPattern, RegexOptions.IgnoreCase);
            if (standardMatch.Success)
            {
                spec.Standard = standardMatch.Value;
            }

            // Extract finish
            var finishPattern = @"\b(galvan[iz]s?ed|painted|mill finish|black|primed|powder coat(?:ed)?)\b";
            var finishMatch = Regex.Match(text, finishPattern, RegexOptions.IgnoreCase);
            if (finishMatch.Success)
            {
                spec.Finish = finishMatch.Value;
            }

            // Extract product type (RHS, SHS, CHS, UB, UC, PFC, etc.)
            var typePattern = @"\b(RHS|SHS|CHS|UB|UC|PFC|EA|UA|FB)\b";
            var typeMatch = Regex.Match(text, typePattern, RegexOptions.IgnoreCase);
            if (typeMatch.Success)
            {
                spec.Properties["ProductType"] = typeMatch.Value.ToUpper();
            }

            return spec;
        }

        // Calculate Levenshtein distance for fuzzy matching
        private int LevenshteinDistance(string s1, string s2)
        {
            if (string.IsNullOrEmpty(s1))
                return string.IsNullOrEmpty(s2) ? 0 : s2.Length;
            if (string.IsNullOrEmpty(s2))
                return s1.Length;

            int[,] d = new int[s1.Length + 1, s2.Length + 1];

            for (int i = 0; i <= s1.Length; i++)
                d[i, 0] = i;
            for (int j = 0; j <= s2.Length; j++)
                d[0, j] = j;

            for (int i = 1; i <= s1.Length; i++)
            {
                for (int j = 1; j <= s2.Length; j++)
                {
                    int cost = (s1[i - 1] == s2[j - 1]) ? 0 : 1;
                    d[i, j] = Math.Min(
                        Math.Min(d[i - 1, j] + 1, d[i, j - 1] + 1),
                        d[i - 1, j - 1] + cost
                    );
                }
            }

            return d[s1.Length, s2.Length];
        }

        // Calculate similarity score (0-100%)
        private decimal CalculateSimilarityScore(string s1, string s2)
        {
            if (string.IsNullOrEmpty(s1) || string.IsNullOrEmpty(s2))
                return 0;

            int distance = LevenshteinDistance(s1.ToLower(), s2.ToLower());
            int maxLength = Math.Max(s1.Length, s2.Length);

            if (maxLength == 0)
                return 100;

            decimal similarity = (1.0m - (decimal)distance / maxLength) * 100;
            return Math.Max(0, similarity);
        }

        // Calculate scale from known distance
        private ScaleInfo CalculateScaleFromKnownDistance(decimal pixelDistance, decimal realDistance, string unit)
        {
            // Validate inputs
            if (pixelDistance <= 0 || realDistance <= 0)
            {
                _logger.LogWarning($"Invalid scale calculation inputs: pixels={pixelDistance}, real={realDistance}");
                return new ScaleInfo
                {
                    Scale = 1,
                    Unit = "mm",
                    ScaleText = "1:1",
                    PixelsPerUnit = 1,
                    IsMetric = true
                };
            }

            // Convert real distance to mm for consistency
            decimal realDistanceMm = ConvertLength(realDistance, unit, "mm");

            // Calculate scale ratio (how many real mm per pixel)
            // If 100 pixels = 1000mm real, then scale = 1000/100 = 10 (1:10 scale)
            decimal mmPerPixel = realDistanceMm / pixelDistance;

            // Common architectural scales in metric
            var commonScales = new[] { 1, 2, 5, 10, 20, 25, 50, 100, 200, 250, 500, 1000 };
            decimal bestScale = 1;
            decimal minDiff = decimal.MaxValue;

            foreach (var testScale in commonScales)
            {
                // For a 1:50 scale, 1mm on drawing = 50mm real
                decimal testMmPerPixel = testScale;
                decimal diff = Math.Abs(mmPerPixel - testMmPerPixel);
                if (diff < minDiff)
                {
                    minDiff = diff;
                    bestScale = testScale;
                }
            }

            string scaleText = $"1:{bestScale}";

            // Calculate pixels per real-world unit for display
            decimal pixelsPerMm = pixelDistance / realDistanceMm;

            return new ScaleInfo
            {
                Scale = (float)bestScale,
                Unit = "mm",
                ScaleText = scaleText,
                PixelsPerUnit = (float)(pixelsPerMm * 1000), // pixels per meter
                IsMetric = true
            };
        }

        // Enhanced weight calculation with more options
        private decimal CalculateWeightFromDimensions(decimal length, decimal width, decimal thickness, string material = "steel")
        {
            // Calculate volume in cubic meters
            decimal volumeM3 = (length / 1000) * (width / 1000) * (thickness / 1000);

            // Material densities in kg/m³
            decimal density = material.ToLower() switch
            {
                "steel" or "mild steel" => 7850,
                "stainless steel" or "stainless" => 7900,
                "aluminum" or "aluminium" => 2700,
                "copper" => 8960,
                "brass" => 8400,
                _ => 7850 // default to steel
            };

            return volumeM3 * density;
        }

        // Normalize dimensions to mm for comparison
        private List<decimal> NormalizeDimensionsToMm(List<decimal> dimensions, string currentUnit = "mm")
        {
            if (currentUnit.ToLower() == "mm")
                return dimensions;

            var normalized = new List<decimal>();
            foreach (var dim in dimensions)
            {
                normalized.Add(ConvertLength(dim, currentUnit, "mm"));
            }

            return normalized;
        }

        // Check if dimensions match with tolerance
        private bool DimensionsMatchWithTolerance(decimal dim1, decimal dim2, decimal toleranceMm = 5)
        {
            return Math.Abs(dim1 - dim2) <= toleranceMm;
        }

        // Determine item type based on specification and dimensions
        private string DetermineItemType(MaterialSpecification spec, List<decimal> dimensions)
        {
            if (spec.Properties.ContainsKey("ProductType"))
                return spec.Properties["ProductType"];

            if (dimensions.Count == 0)
                return "Material";

            if (dimensions.Count == 1)
                return "Round Bar or Square Section";
            else if (dimensions.Count == 2)
                return "Rectangular Section";
            else if (dimensions.Count >= 3)
            {
                // Check if it's likely a plate (large width/height, small thickness)
                if (dimensions[2] < dimensions[0] / 10 && dimensions[2] < dimensions[1] / 10)
                    return "Plate";
                else
                    return "Structural Section";
            }

            return "Unknown";
        }

        // Calculate extraction confidence based on what was found
        private decimal CalculateExtractionConfidence(MaterialSpecification spec, List<decimal> dimensions)
        {
            decimal confidence = 0;

            // Start with base confidence
            if (!string.IsNullOrEmpty(spec.Material))
                confidence += 30;
            if (!string.IsNullOrEmpty(spec.Grade))
                confidence += 25;
            if (!string.IsNullOrEmpty(spec.Standard))
                confidence += 20;
            if (dimensions.Count > 0)
                confidence += 25;

            // Cap at 100
            return Math.Min(confidence, 100);
        }
    }
}