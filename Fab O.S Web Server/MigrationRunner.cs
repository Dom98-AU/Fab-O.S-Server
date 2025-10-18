using FabOS.WebServer.Data.Contexts;
using Microsoft.EntityFrameworkCore;

namespace FabOS.WebServer;

/// <summary>
/// Utility class to manually run SQL migration scripts
/// </summary>
public class MigrationRunner
{
    private readonly ApplicationDbContext _context;

    public MigrationRunner(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<bool> ApplyRevisionMigrationAsync()
    {
        try
        {
            // Try bin directory first, then fall back to project directory
            var sqlFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "apply_revision_only.sql");

            if (!File.Exists(sqlFilePath))
            {
                // Try project directory (go up from bin/Debug/net8.0)
                var projectDir = Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "..", ".."));
                sqlFilePath = Path.Combine(projectDir, "apply_revision_only.sql");
            }

            if (!File.Exists(sqlFilePath))
            {
                Console.WriteLine($"SQL file not found: {sqlFilePath}");
                return false;
            }

            var sql = await File.ReadAllTextAsync(sqlFilePath);

            // Split by GO statements and execute each batch
            var batches = sql.Split(new[] { "\nGO\n", "\nGO\r\n", "\r\nGO\r\n", "\r\nGO\n" },
                StringSplitOptions.RemoveEmptyEntries);

            Console.WriteLine($"Executing {batches.Length} SQL batches...");

            foreach (var batch in batches)
            {
                var trimmedBatch = batch.Trim();
                if (!string.IsNullOrWhiteSpace(trimmedBatch) &&
                    !trimmedBatch.StartsWith("--") &&
                    !trimmedBatch.Equals("BEGIN TRANSACTION;", StringComparison.OrdinalIgnoreCase) &&
                    !trimmedBatch.Equals("COMMIT;", StringComparison.OrdinalIgnoreCase))
                {
                    await _context.Database.ExecuteSqlRawAsync(trimmedBatch);
                }
            }

            Console.WriteLine("TakeoffRevisionSystem migration applied successfully");
            return true;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error applying migration: {ex.Message}");
            Console.WriteLine($"Stack trace: {ex.StackTrace}");
            return false;
        }
    }

    public async Task<bool> SyncMigrationHistoryAsync()
    {
        try
        {
            // Manually mark the earlier migrations as applied
            var migrations = new[]
            {
                "20250922020049_UpdateCustomerAndContactTables",
                "20250922043044_AddAddressFieldsToCustomerContact",
                "20250929050816_AddShortNameToCompany",
                "20250930231103_AddMissingSchemaChanges"
            };

            foreach (var migration in migrations)
            {
                var exists = await _context.Database
                    .ExecuteSqlRawAsync($@"
                        IF NOT EXISTS (SELECT 1 FROM __EFMigrationsHistory WHERE MigrationId = '{migration}')
                        BEGIN
                            INSERT INTO __EFMigrationsHistory (MigrationId, ProductVersion)
                            VALUES ('{migration}', '8.0.8')
                        END
                    ");
            }

            Console.WriteLine("Migration history synced successfully");
            return true;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error syncing migration history: {ex.Message}");
            return false;
        }
    }
}
