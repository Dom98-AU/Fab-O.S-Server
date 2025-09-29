using OfficeOpenXml;
using FabOS.WebServer.Models.Entities;
using FabOS.WebServer.Services.Interfaces;
using FabOS.WebServer.Data.Contexts;
using Microsoft.EntityFrameworkCore;
using System.Globalization;

namespace FabOS.WebServer.Services.Implementations
{
    public class ExcelImportService : IExcelImportService
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<ExcelImportService> _logger;

        public ExcelImportService(ApplicationDbContext context, ILogger<ExcelImportService> logger)
        {
            _context = context;
            _logger = logger;
            ExcelPackage.LicenseContext = LicenseContext.NonCommercial; // Set EPPlus license context
        }

        public async Task<List<CatalogueItem>> ImportCatalogueItemsFromExcel(string filePath, int companyId)
        {
            var catalogueItems = new List<CatalogueItem>();

            try
            {
                using (var package = new ExcelPackage(new FileInfo(filePath)))
                {
                    var worksheet = package.Workbook.Worksheets[0]; // Assuming data is in the first worksheet
                    var rowCount = worksheet.Dimension?.Rows ?? 0;
                    var colCount = worksheet.Dimension?.Columns ?? 0;

                    if (rowCount <= 1 || colCount == 0)
                    {
                        _logger.LogWarning("Excel file appears to be empty or has no data rows");
                        return catalogueItems;
                    }

                    // Get header row to map column names
                    var headers = new Dictionary<string, int>();
                    for (int col = 1; col <= colCount; col++)
                    {
                        var headerValue = worksheet.Cells[1, col].Value?.ToString()?.Trim();
                        if (!string.IsNullOrEmpty(headerValue))
                        {
                            headers[headerValue] = col;
                        }
                    }

                    // Process data rows
                    for (int row = 2; row <= rowCount; row++)
                    {
                        try
                        {
                            var rowData = new Dictionary<string, object>();
                            foreach (var header in headers)
                            {
                                var cellValue = worksheet.Cells[row, header.Value].Value;
                                rowData[header.Key] = cellValue ?? string.Empty;
                            }

                            var catalogueItem = await ParseCatalogueItemFromRow(rowData, companyId);
                            if (catalogueItem != null)
                            {
                                catalogueItems.Add(catalogueItem);
                            }
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError($"Error processing row {row}: {ex.Message}");
                        }
                    }
                }

                _logger.LogInformation($"Successfully imported {catalogueItems.Count} catalogue items from Excel");
                return catalogueItems;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error importing Excel file: {ex.Message}");
                throw;
            }
        }

        public async Task<CatalogueItem> ParseCatalogueItemFromRow(Dictionary<string, object> rowData, int companyId)
        {
            try
            {
                var item = new CatalogueItem
                {
                    CompanyId = companyId,
                    CreatedDate = DateTime.UtcNow,
                    IsActive = true
                };

                // Primary fields (required)
                item.ItemCode = GetStringValue(rowData, "ItemCode", "Item Code", "Code") ?? GenerateItemCode(rowData);
                item.Description = GetStringValue(rowData, "Description", "Desc", "Product Description") ?? string.Empty;
                item.Category = GetStringValue(rowData, "Category", "Product Category", "Type") ?? "General";
                item.Material = GetStringValue(rowData, "Material", "Material Type", "Mat") ?? "Steel";

                // Profile and specifications
                item.Profile = GetStringValue(rowData, "Profile", "Section", "Shape");
                item.Standard = GetStringValue(rowData, "Standard", "Specification", "Spec");
                item.Grade = GetStringValue(rowData, "Grade", "Material Grade");
                item.Alloy = GetStringValue(rowData, "Alloy", "Alloy Type");
                item.Temper = GetStringValue(rowData, "Temper");

                // Primary dimensions (mm)
                item.Length_mm = GetDecimalValue(rowData, "Length_mm", "Length (mm)", "Length");
                item.Width_mm = GetDecimalValue(rowData, "Width_mm", "Width (mm)", "Width");
                item.Height_mm = GetDecimalValue(rowData, "Height_mm", "Height (mm)", "Height");
                item.Depth_mm = GetDecimalValue(rowData, "Depth_mm", "Depth (mm)", "Depth");
                item.Thickness_mm = GetDecimalValue(rowData, "Thickness_mm", "Thickness (mm)", "Thickness", "THK");
                item.Diameter_mm = GetDecimalValue(rowData, "Diameter_mm", "Diameter (mm)", "Dia");

                // Pipe/Tube specific
                item.OD_mm = GetDecimalValue(rowData, "OD_mm", "OD (mm)", "Outside Diameter", "OD");
                item.ID_mm = GetDecimalValue(rowData, "ID_mm", "ID (mm)", "Inside Diameter", "ID");
                item.WallThickness_mm = GetDecimalValue(rowData, "WallThickness_mm", "Wall Thickness (mm)", "Wall THK");
                item.Wall_mm = GetDecimalValue(rowData, "Wall_mm", "Wall (mm)");
                item.NominalBore = GetStringValue(rowData, "NominalBore", "NB", "Nominal Bore");
                item.ImperialEquiv = GetStringValue(rowData, "ImperialEquiv", "Imperial", "Inch Size");

                // Beam/Column specific
                item.Web_mm = GetDecimalValue(rowData, "Web_mm", "Web (mm)", "Web Thickness");
                item.Flange_mm = GetDecimalValue(rowData, "Flange_mm", "Flange (mm)", "Flange Thickness");

                // Angle specific
                item.A_mm = GetDecimalValue(rowData, "A_mm", "A (mm)", "Leg A");
                item.B_mm = GetDecimalValue(rowData, "B_mm", "B (mm)", "Leg B");

                // Size fields
                item.Size_mm = GetStringValue(rowData, "Size_mm", "Size (mm)", "Metric Size");
                item.Size = GetDecimalValue(rowData, "Size", "Numeric Size");
                item.Size_inch = GetStringValue(rowData, "Size_inch", "Size (inch)", "Imperial Size");

                // Sheet/Plate specific
                item.BMT_mm = GetDecimalValue(rowData, "BMT_mm", "BMT (mm)", "Base Metal Thickness");
                item.BaseThickness_mm = GetDecimalValue(rowData, "BaseThickness_mm", "Base Thickness (mm)");
                item.RaisedThickness_mm = GetDecimalValue(rowData, "RaisedThickness_mm", "Raised Thickness (mm)");

                // Weight and Mass
                item.Mass_kg_m = GetDecimalValue(rowData, "Mass_kg_m", "Mass (kg/m)", "Weight per meter", "kg/m");
                item.Mass_kg_m2 = GetDecimalValue(rowData, "Mass_kg_m2", "Mass (kg/m2)", "Weight per m2", "kg/m2");
                item.Mass_kg_length = GetDecimalValue(rowData, "Mass_kg_length", "Mass per Length", "Weight per Length");
                item.Weight_kg = GetDecimalValue(rowData, "Weight_kg", "Weight (kg)", "Unit Weight");
                item.Weight_kg_m2 = GetDecimalValue(rowData, "Weight_kg_m2", "Weight (kg/m2)");

                // Surface Area
                item.SurfaceArea_m2 = GetDecimalValue(rowData, "SurfaceArea_m2", "Surface Area (m2)");
                item.SurfaceArea_m2_per_m = GetDecimalValue(rowData, "SurfaceArea_m2_per_m", "Surface Area per m");
                item.SurfaceArea_m2_per_m2 = GetDecimalValue(rowData, "SurfaceArea_m2_per_m2", "Surface Area per m2");
                item.Surface = GetStringValue(rowData, "Surface", "Surface Type", "Surface Texture");

                // Finish specifications
                item.Finish = GetStringValue(rowData, "Finish", "Surface Finish", "Finish Type");
                item.Finish_Standard = GetStringValue(rowData, "Finish_Standard", "Finish Standard");
                item.Coating = GetStringValue(rowData, "Coating", "Coating Type");

                // Availability
                item.StandardLengths = GetStringValue(rowData, "StandardLengths", "Standard Lengths", "Available Lengths");
                item.StandardLength_m = GetIntValue(rowData, "StandardLength_m", "Standard Length (m)");
                item.Cut_To_Size = GetStringValue(rowData, "Cut_To_Size", "Cut to Size", "Custom Cut");

                // Product type and features
                item.Type = GetStringValue(rowData, "Type", "Product Type");
                item.ProductType = GetStringValue(rowData, "ProductType", "Product Category");
                item.Pattern = GetStringValue(rowData, "Pattern", "Tread Pattern");
                item.Features = GetStringValue(rowData, "Features", "Special Features", "Notes");
                item.Tolerance = GetStringValue(rowData, "Tolerance", "Dimensional Tolerance");

                // Pipe classifications
                item.Pressure = GetStringValue(rowData, "Pressure", "Pressure Class", "Class");

                // Supplier information
                item.SupplierCode = GetStringValue(rowData, "SupplierCode", "Supplier Code", "Vendor Code");
                item.PackQty = GetIntValue(rowData, "PackQty", "Pack Quantity", "Bundle Qty");
                item.Unit = GetStringValue(rowData, "Unit", "UOM", "Unit of Measure") ?? "EA";

                // Compliance
                item.Compliance = GetStringValue(rowData, "Compliance", "Standards Compliance", "Certifications");
                item.Duty_Rating = GetStringValue(rowData, "Duty_Rating", "Duty Rating", "Load Rating");

                return item;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error parsing catalogue item from row: {ex.Message}");
                return null;
            }
        }

        public async Task<int> SeedCatalogueItemsToDatabase(List<CatalogueItem> items)
        {
            int insertedCount = 0;
            int updatedCount = 0;

            try
            {
                foreach (var item in items)
                {
                    var existingItem = await _context.CatalogueItems
                        .FirstOrDefaultAsync(ci => ci.ItemCode == item.ItemCode && ci.CompanyId == item.CompanyId);

                    if (existingItem != null)
                    {
                        // Update existing item
                        UpdateExistingItem(existingItem, item);
                        updatedCount++;
                    }
                    else
                    {
                        // Insert new item
                        _context.CatalogueItems.Add(item);
                        insertedCount++;
                    }

                    // Save in batches for performance
                    if ((insertedCount + updatedCount) % 100 == 0)
                    {
                        await _context.SaveChangesAsync();
                        _logger.LogInformation($"Progress: {insertedCount} inserted, {updatedCount} updated");
                    }
                }

                await _context.SaveChangesAsync();
                _logger.LogInformation($"Seeding complete: {insertedCount} items inserted, {updatedCount} items updated");

                return insertedCount + updatedCount;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error seeding catalogue items to database: {ex.Message}");
                throw;
            }
        }

        public async Task<bool> ValidateCatalogueData(List<CatalogueItem> items)
        {
            bool isValid = true;
            var duplicates = items.GroupBy(x => x.ItemCode)
                .Where(g => g.Count() > 1)
                .Select(g => g.Key)
                .ToList();

            if (duplicates.Any())
            {
                _logger.LogWarning($"Found {duplicates.Count} duplicate ItemCodes: {string.Join(", ", duplicates.Take(10))}");
                isValid = false;
            }

            var itemsWithoutCode = items.Where(x => string.IsNullOrEmpty(x.ItemCode)).ToList();
            if (itemsWithoutCode.Any())
            {
                _logger.LogWarning($"Found {itemsWithoutCode.Count} items without ItemCode");
                isValid = false;
            }

            var itemsWithoutDescription = items.Where(x => string.IsNullOrEmpty(x.Description)).ToList();
            if (itemsWithoutDescription.Any())
            {
                _logger.LogWarning($"Found {itemsWithoutDescription.Count} items without Description");
            }

            return isValid;
        }

        #region Helper Methods

        private string GetStringValue(Dictionary<string, object> rowData, params string[] possibleKeys)
        {
            foreach (var key in possibleKeys)
            {
                if (rowData.ContainsKey(key) && rowData[key] != null)
                {
                    var value = rowData[key].ToString()?.Trim();
                    if (!string.IsNullOrEmpty(value))
                        return value;
                }
            }
            return null;
        }

        private decimal? GetDecimalValue(Dictionary<string, object> rowData, params string[] possibleKeys)
        {
            foreach (var key in possibleKeys)
            {
                if (rowData.ContainsKey(key) && rowData[key] != null)
                {
                    var value = rowData[key].ToString();
                    if (decimal.TryParse(value, NumberStyles.Any, CultureInfo.InvariantCulture, out decimal result))
                        return result;
                }
            }
            return null;
        }

        private int? GetIntValue(Dictionary<string, object> rowData, params string[] possibleKeys)
        {
            foreach (var key in possibleKeys)
            {
                if (rowData.ContainsKey(key) && rowData[key] != null)
                {
                    var value = rowData[key].ToString();
                    if (int.TryParse(value, out int result))
                        return result;
                }
            }
            return null;
        }

        private string GenerateItemCode(Dictionary<string, object> rowData)
        {
            // Generate a unique item code based on available data
            var category = GetStringValue(rowData, "Category")?.Replace(" ", "").ToUpper() ?? "ITEM";
            var material = GetStringValue(rowData, "Material")?.Substring(0, Math.Min(3, GetStringValue(rowData, "Material").Length)).ToUpper() ?? "GEN";
            var timestamp = DateTime.UtcNow.Ticks.ToString().Substring(10, 6);

            return $"{category.Substring(0, Math.Min(4, category.Length))}-{material}-{timestamp}";
        }

        private void UpdateExistingItem(CatalogueItem existing, CatalogueItem updated)
        {
            // Update all properties except Id, ItemCode, CompanyId, and CreatedDate
            existing.Description = updated.Description;
            existing.Category = updated.Category;
            existing.Material = updated.Material;
            existing.Profile = updated.Profile;
            existing.Length_mm = updated.Length_mm;
            existing.Width_mm = updated.Width_mm;
            existing.Height_mm = updated.Height_mm;
            existing.Depth_mm = updated.Depth_mm;
            existing.Thickness_mm = updated.Thickness_mm;
            existing.Diameter_mm = updated.Diameter_mm;
            existing.OD_mm = updated.OD_mm;
            existing.ID_mm = updated.ID_mm;
            existing.WallThickness_mm = updated.WallThickness_mm;
            existing.Wall_mm = updated.Wall_mm;
            existing.NominalBore = updated.NominalBore;
            existing.ImperialEquiv = updated.ImperialEquiv;
            existing.Web_mm = updated.Web_mm;
            existing.Flange_mm = updated.Flange_mm;
            existing.A_mm = updated.A_mm;
            existing.B_mm = updated.B_mm;
            existing.Size_mm = updated.Size_mm;
            existing.Size = updated.Size;
            existing.Size_inch = updated.Size_inch;
            existing.BMT_mm = updated.BMT_mm;
            existing.BaseThickness_mm = updated.BaseThickness_mm;
            existing.RaisedThickness_mm = updated.RaisedThickness_mm;
            existing.Mass_kg_m = updated.Mass_kg_m;
            existing.Mass_kg_m2 = updated.Mass_kg_m2;
            existing.Mass_kg_length = updated.Mass_kg_length;
            existing.Weight_kg = updated.Weight_kg;
            existing.Weight_kg_m2 = updated.Weight_kg_m2;
            existing.SurfaceArea_m2 = updated.SurfaceArea_m2;
            existing.SurfaceArea_m2_per_m = updated.SurfaceArea_m2_per_m;
            existing.SurfaceArea_m2_per_m2 = updated.SurfaceArea_m2_per_m2;
            existing.Surface = updated.Surface;
            existing.Standard = updated.Standard;
            existing.Grade = updated.Grade;
            existing.Alloy = updated.Alloy;
            existing.Temper = updated.Temper;
            existing.Finish = updated.Finish;
            existing.Finish_Standard = updated.Finish_Standard;
            existing.Coating = updated.Coating;
            existing.StandardLengths = updated.StandardLengths;
            existing.StandardLength_m = updated.StandardLength_m;
            existing.Cut_To_Size = updated.Cut_To_Size;
            existing.Type = updated.Type;
            existing.ProductType = updated.ProductType;
            existing.Pattern = updated.Pattern;
            existing.Features = updated.Features;
            existing.Tolerance = updated.Tolerance;
            existing.Pressure = updated.Pressure;
            existing.SupplierCode = updated.SupplierCode;
            existing.PackQty = updated.PackQty;
            existing.Unit = updated.Unit;
            existing.Compliance = updated.Compliance;
            existing.Duty_Rating = updated.Duty_Rating;
            existing.ModifiedDate = DateTime.UtcNow;
        }

        #endregion
    }
}