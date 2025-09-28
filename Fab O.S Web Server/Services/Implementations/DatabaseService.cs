using FabOS.WebServer.Data.Contexts;
using FabOS.WebServer.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using System.Data;
using Microsoft.Data.SqlClient;

namespace FabOS.WebServer.Services.Implementations;

public class DatabaseService : IDatabaseService
{
    private readonly ApplicationDbContext _context;
    private readonly IConfiguration _configuration;
    private readonly ILogger<DatabaseService> _logger;

    public DatabaseService(ApplicationDbContext context, IConfiguration configuration, ILogger<DatabaseService> logger)
    {
        _context = context;
        _configuration = configuration;
        _logger = logger;
    }

    public async Task<bool> TestConnectionAsync()
    {
        try
        {
            await _context.Database.OpenConnectionAsync();
            await _context.Database.CloseConnectionAsync();
            _logger.LogInformation("Database connection test successful");
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Database connection test failed");
            return false;
        }
    }

    public async Task<List<string>> GetTableNamesAsync()
    {
        try
        {
            var query = @"
                SELECT TABLE_NAME 
                FROM INFORMATION_SCHEMA.TABLES 
                WHERE TABLE_TYPE = 'BASE TABLE' 
                ORDER BY TABLE_NAME";

            var tables = new List<string>();
            using var connection = new SqlConnection(_configuration.GetConnectionString("DefaultConnection"));
            using var command = new SqlCommand(query, connection);
            
            await connection.OpenAsync();
            using var reader = await command.ExecuteReaderAsync();
            
            while (await reader.ReadAsync())
            {
                tables.Add(reader.GetString(0));
            }

            return tables;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving table names");
            throw;
        }
    }

    public async Task<DataTable> ExecuteQueryAsync(string query)
    {
        try
        {
            var dataTable = new DataTable();
            using var connection = new SqlConnection(_configuration.GetConnectionString("DefaultConnection"));
            using var command = new SqlCommand(query, connection);
            using var adapter = new SqlDataAdapter(command);
            
            await connection.OpenAsync();
            adapter.Fill(dataTable);
            
            return dataTable;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error executing query: {Query}", query);
            throw;
        }
    }

    public async Task<List<Dictionary<string, object>>> GetTableDataAsync(string tableName, int maxRows = 100)
    {
        try
        {
            var query = $"SELECT TOP {maxRows} * FROM [{tableName}]";
            var results = new List<Dictionary<string, object>>();
            
            using var connection = new SqlConnection(_configuration.GetConnectionString("DefaultConnection"));
            using var command = new SqlCommand(query, connection);
            
            await connection.OpenAsync();
            using var reader = await command.ExecuteReaderAsync();
            
            while (await reader.ReadAsync())
            {
                var row = new Dictionary<string, object>();
                for (int i = 0; i < reader.FieldCount; i++)
                {
                    row[reader.GetName(i)] = reader.GetValue(i) ?? DBNull.Value;
                }
                results.Add(row);
            }

            return results;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving data from table: {TableName}", tableName);
            throw;
        }
    }
}
