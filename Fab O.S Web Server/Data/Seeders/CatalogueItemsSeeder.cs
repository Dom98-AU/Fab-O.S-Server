using FabOS.WebServer.Data.Contexts;
using FabOS.WebServer.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace FabOS.WebServer.Data.Seeders
{
    public class CatalogueItemsSeeder
    {
        private readonly ApplicationDbContext _context;
        private readonly IExcelImportService _excelImportService;
        private readonly ILogger<CatalogueItemsSeeder> _logger;
        private readonly IWebHostEnvironment _environment;

        public CatalogueItemsSeeder(
            ApplicationDbContext context,
            IExcelImportService excelImportService,
            ILogger<CatalogueItemsSeeder> logger,
            IWebHostEnvironment environment)
        {
            _context = context;
            _excelImportService = excelImportService;
            _logger = logger;
            _environment = environment;
        }

        public async Task SeedAsync()
        {
            try
            {
                // Check if catalogue items already exist
                var existingCount = await _context.CatalogueItems.CountAsync();
                if (existingCount >= 7000) // Assuming we have around 7,107 items
                {
                    _logger.LogInformation($"Catalogue items already seeded ({existingCount} items found). Skipping seed.");
                    return;
                }

                _logger.LogInformation("Starting catalogue items seed process...");

                // Define the Excel file path
                string excelFilePath = @"C:\Fab.OS Platform\AU_NZ_Catalogue_Items_PRODUCTION_FINAL.xlsx";

                // Alternative paths to check
                string[] alternativePaths = new[]
                {
                    Path.Combine(_environment.ContentRootPath, "Data", "SeedData", "AU_NZ_Catalogue_Items_PRODUCTION_FINAL.xlsx"),
                    Path.Combine(_environment.WebRootPath, "data", "AU_NZ_Catalogue_Items_PRODUCTION_FINAL.xlsx"),
                    @"/mnt/c/Fab.OS Platform/AU_NZ_Catalogue_Items_PRODUCTION_FINAL.xlsx"
                };

                // Find the Excel file
                string actualPath = excelFilePath;
                if (!File.Exists(actualPath))
                {
                    foreach (var path in alternativePaths)
                    {
                        if (File.Exists(path))
                        {
                            actualPath = path;
                            break;
                        }
                    }
                }

                if (!File.Exists(actualPath))
                {
                    _logger.LogWarning($"Excel file not found at {excelFilePath} or any alternative paths. Skipping catalogue items seed.");
                    return;
                }

                _logger.LogInformation($"Found Excel file at: {actualPath}");

                // Get the default company ID (or create one if needed)
                var defaultCompany = await GetOrCreateDefaultCompany();

                // Import catalogue items from Excel
                var catalogueItems = await _excelImportService.ImportCatalogueItemsFromExcel(actualPath, defaultCompany.Id);

                if (catalogueItems.Any())
                {
                    // Validate the imported data
                    var isValid = await _excelImportService.ValidateCatalogueData(catalogueItems);
                    if (!isValid)
                    {
                        _logger.LogWarning("Some validation issues found in catalogue data. Proceeding with valid items only.");
                    }

                    // Seed to database
                    var totalSeeded = await _excelImportService.SeedCatalogueItemsToDatabase(catalogueItems);
                    _logger.LogInformation($"Successfully seeded {totalSeeded} catalogue items to database.");
                }
                else
                {
                    _logger.LogWarning("No catalogue items were imported from the Excel file.");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error during catalogue items seed: {ex.Message}");
                // Don't throw - allow the application to continue even if seeding fails
            }
        }

        private async Task<Models.Entities.Company> GetOrCreateDefaultCompany()
        {
            var company = await _context.Companies.FirstOrDefaultAsync(c => c.Name == "Default Company");

            if (company == null)
            {
                company = new Models.Entities.Company
                {
                    Name = "Default Company",
                    ShortName = "DEFAULT",
                    IsActive = true,
                    CreatedDate = DateTime.UtcNow
                };

                _context.Companies.Add(company);
                await _context.SaveChangesAsync();
                _logger.LogInformation("Created default company for catalogue items seed.");
            }

            return company;
        }
    }
}