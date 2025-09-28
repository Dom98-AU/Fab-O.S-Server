using Microsoft.Data.SqlClient;
using System.Data;

namespace FabOS.WebServer.Utils
{
    public class DatabaseSchemaInspector
    {
        private readonly string _connectionString;

        public DatabaseSchemaInspector(string connectionString)
        {
            _connectionString = connectionString;
        }

        public static async Task InspectTableSchemaAsync(string tableName)
        {
            string connectionString = "Server=nwiapps.database.windows.net;Database=sqldb-steel-estimation-sandbox;User Id=admin@nwi@nwiapps;Password=Natweigh88;Trusted_Connection=False;MultipleActiveResultSets=true;Encrypt=True;TrustServerCertificate=False;Connection Timeout=30;";
            
            Console.WriteLine($"🔍 Inspecting table: {tableName}");
            
            try
            {
                using var connection = new SqlConnection(connectionString);
                await connection.OpenAsync();
                
                var columnsQuery = @"
                    SELECT 
                        COLUMN_NAME,
                        DATA_TYPE,
                        IS_NULLABLE,
                        CHARACTER_MAXIMUM_LENGTH,
                        COLUMN_DEFAULT
                    FROM INFORMATION_SCHEMA.COLUMNS 
                    WHERE TABLE_NAME = @TableName
                    ORDER BY ORDINAL_POSITION";
                
                using var command = new SqlCommand(columnsQuery, connection);
                command.Parameters.AddWithValue("@TableName", tableName);
                
                using var reader = await command.ExecuteReaderAsync();
                
                Console.WriteLine($"Columns in {tableName}:");
                while (await reader.ReadAsync())
                {
                    var columnName = reader.GetString("COLUMN_NAME");
                    var dataType = reader.GetString("DATA_TYPE");
                    var isNullable = reader.GetString("IS_NULLABLE");
                    var maxLength = reader.IsDBNull("CHARACTER_MAXIMUM_LENGTH") ? "N/A" : reader.GetInt32("CHARACTER_MAXIMUM_LENGTH").ToString();
                    var defaultValue = reader.IsDBNull("COLUMN_DEFAULT") ? "N/A" : reader.GetString("COLUMN_DEFAULT");
                    
                    Console.WriteLine($"  - {columnName} ({dataType}, {isNullable}, MaxLength: {maxLength}, Default: {defaultValue})");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error inspecting table {tableName}: {ex.Message}");
            }
        }
    }
}
