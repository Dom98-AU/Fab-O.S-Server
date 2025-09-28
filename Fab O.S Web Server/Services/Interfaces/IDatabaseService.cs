using System.Data;

namespace FabOS.WebServer.Services.Interfaces;

public interface IDatabaseService
{
    Task<bool> TestConnectionAsync();
    Task<List<string>> GetTableNamesAsync();
    Task<DataTable> ExecuteQueryAsync(string query);
    Task<List<Dictionary<string, object>>> GetTableDataAsync(string tableName, int maxRows = 100);
}
