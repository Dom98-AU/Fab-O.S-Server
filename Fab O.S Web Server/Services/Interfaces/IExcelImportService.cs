using FabOS.WebServer.Models.Entities;

namespace FabOS.WebServer.Services.Interfaces
{
    public interface IExcelImportService
    {
        Task<List<CatalogueItem>> ImportCatalogueItemsFromExcel(string filePath, int companyId);
        Task<int> SeedCatalogueItemsToDatabase(List<CatalogueItem> items);
        Task<CatalogueItem> ParseCatalogueItemFromRow(Dictionary<string, object> rowData, int companyId);
        Task<bool> ValidateCatalogueData(List<CatalogueItem> items);
    }
}