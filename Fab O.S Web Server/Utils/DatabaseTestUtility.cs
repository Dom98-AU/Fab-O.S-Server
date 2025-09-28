using Microsoft.Data.SqlClient;
using System.Data;

namespace Fab_O.S_Web_Server.Utils;

public static class DatabaseTestUtility
{
    public static async Task<(bool Success, string Message, List<string> Tables)> TestDatabaseConnectionAsync(string connectionString)
    {
        try
        {
            var tables = new List<string>();
            
            using var connection = new SqlConnection(connectionString);
            await connection.OpenAsync();
            
            // Test basic connection
            var connectionMessage = $"✅ Successfully connected to database: {connection.Database} on server: {connection.DataSource}";
            
            // Get table list
            var query = @"
                SELECT TABLE_NAME 
                FROM INFORMATION_SCHEMA.TABLES 
                WHERE TABLE_TYPE = 'BASE TABLE' 
                ORDER BY TABLE_NAME";
                
            using var command = new SqlCommand(query, connection);
            using var reader = await command.ExecuteReaderAsync();
            
            while (await reader.ReadAsync())
            {
                tables.Add(reader.GetString(0));
            }
            
            var fullMessage = $"{connectionMessage}\n📊 Found {tables.Count} tables: {string.Join(", ", tables.Take(10))}{(tables.Count > 10 ? "..." : "")}";
            
            return (true, fullMessage, tables);
        }
        catch (SqlException sqlEx)
        {
            return (false, $"❌ SQL Error: {sqlEx.Message} (Error Number: {sqlEx.Number})", new List<string>());
        }
        catch (Exception ex)
        {
            return (false, $"❌ Connection Error: {ex.Message}", new List<string>());
        }
    }
    
    public static async Task<(bool Success, string Message, DataTable Data)> QueryTableAsync(string connectionString, string tableName, int maxRows = 100)
    {
        try
        {
            using var connection = new SqlConnection(connectionString);
            await connection.OpenAsync();
            
            var query = $"SELECT TOP {maxRows} * FROM [{tableName}]";
            using var command = new SqlCommand(query, connection);
            using var adapter = new SqlDataAdapter(command);
            
            var dataTable = new DataTable();
            adapter.Fill(dataTable);
            
            var message = $"✅ Retrieved {dataTable.Rows.Count} rows from table '{tableName}'";
            return (true, message, dataTable);
        }
        catch (Exception ex)
        {
            return (false, $"❌ Query Error: {ex.Message}", new DataTable());
        }
    }
}
