# Tenant Registry and Dev Portal Linking Proposal

## Current State
The TenantRegistry and MonitoredDatabases tables are loosely coupled through string matching on TenantId and DatabaseName fields. This creates potential issues:
- Data inconsistency risk
- No referential integrity
- Duplicate data storage
- Manual synchronization required

## Proposed Solution: Direct Integration

### Option 1: Dev Portal Reads from Master Registry (Recommended)

Instead of maintaining a separate `MonitoredDatabases` table, the Dev Portal should directly query the Master Tenant Registry:

```csharp
public class EnhancedDatabaseMonitoringService : IDatabaseMonitoringService
{
    private readonly string _masterConnectionString;
    private readonly DevPortalDbContext _context;
    
    public async Task<List<TenantDatabaseInfo>> GetAllTenantDatabasesAsync()
    {
        using var connection = new SqlConnection(_masterConnectionString);
        
        // Query the master TenantRegistry directly
        var tenants = await connection.QueryAsync<TenantDatabaseInfo>(@"
            SELECT 
                tr.Id as TenantRegistryId,
                tr.TenantId,
                tr.DatabaseName,
                tr.CompanyName,
                tr.CompanyCode,
                tr.IsActive,
                tr.MaxUsers,
                tr.DatabaseServer,
                tr.LastModified as LastChecked,
                tpm.ProductName,
                tpm.LicenseType,
                tpm.IsActive as ModuleIsActive,
                tpm.ExpiryDate,
                tpm.MonthlyPrice
            FROM TenantRegistry tr
            LEFT JOIN TenantProductModule tpm ON tr.Id = tpm.TenantRegistryId
            WHERE tr.IsActive = 1
            ORDER BY tr.CompanyName, tpm.ProductName
        ");
        
        return tenants;
    }
}
```

### Option 2: Synchronized Cache with Foreign Keys

If the Dev Portal needs its own database for performance, properly link the tables:

```sql
-- Enhanced MonitoredDatabases table with proper foreign key
ALTER TABLE MonitoredDatabases ADD TenantRegistryId INT NULL;
ALTER TABLE MonitoredDatabases ADD CONSTRAINT UQ_TenantRegistryId UNIQUE (TenantRegistryId);

-- Sync procedure
CREATE PROCEDURE SyncMonitoredDatabases
AS
BEGIN
    -- Insert new tenants
    INSERT INTO MonitoredDatabases (TenantRegistryId, DatabaseName, TenantId, CompanyName, CompanyCode, IsActive)
    SELECT tr.Id, tr.DatabaseName, tr.TenantId, tr.CompanyName, tr.CompanyCode, tr.IsActive
    FROM [MasterDB].dbo.TenantRegistry tr
    WHERE NOT EXISTS (
        SELECT 1 FROM MonitoredDatabases md 
        WHERE md.TenantRegistryId = tr.Id
    );
    
    -- Update existing tenants
    UPDATE md
    SET 
        md.DatabaseName = tr.DatabaseName,
        md.TenantId = tr.TenantId,
        md.CompanyName = tr.CompanyName,
        md.CompanyCode = tr.CompanyCode,
        md.IsActive = tr.IsActive,
        md.LastChecked = GETUTCDATE()
    FROM MonitoredDatabases md
    INNER JOIN [MasterDB].dbo.TenantRegistry tr ON md.TenantRegistryId = tr.Id;
END
```

### Benefits of Direct Integration

1. **Single Source of Truth**: TenantRegistry is the authoritative source
2. **No Data Duplication**: Avoid storing the same data in two places
3. **Automatic Updates**: Changes to TenantRegistry immediately visible in Dev Portal
4. **Referential Integrity**: Proper foreign key relationships
5. **Module Visibility**: Dev Portal can see module licensing directly

### Implementation Steps

#### Phase 1: Add Cross-Database Access
```sql
-- Grant Dev Portal read access to Master database
GRANT SELECT ON [MasterDB].dbo.TenantRegistry TO devportal_reader;
GRANT SELECT ON [MasterDB].dbo.TenantProductModule TO devportal_reader;
GRANT SELECT ON [MasterDB].dbo.TenantModuleUsage TO devportal_reader;
```

#### Phase 2: Update Dev Portal Services
```csharp
public interface ITenantRegistryService
{
    Task<List<TenantRegistryInfo>> GetAllTenantsAsync();
    Task<TenantRegistryInfo> GetTenantAsync(string tenantId);
    Task<List<TenantModuleInfo>> GetTenantModulesAsync(string tenantId);
    Task<TenantUsageStatistics> GetTenantUsageAsync(string tenantId, DateTime startDate, DateTime endDate);
}
```

#### Phase 3: Update Dev Portal UI
- Replace MonitoredDatabases references with TenantRegistry queries
- Show module-level information in the UI
- Update billing to use module pricing

### Alternative: Linked Server Approach

If direct connection isn't possible, use SQL Server Linked Servers:

```sql
-- Create linked server to Master database server
EXEC sp_addlinkedserver 
    @server = 'MasterTenantRegistry',
    @srvproduct = '',
    @provider = 'SQLNCLI',
    @datasrc = 'master-sql-server.database.windows.net';

-- Now query across databases
CREATE VIEW vw_TenantRegistryWithModules AS
SELECT 
    tr.*,
    tpm.ProductName,
    tpm.LicenseType,
    tpm.IsActive as ModuleIsActive,
    tpm.MonthlyPrice
FROM [MasterTenantRegistry].[MasterDB].[dbo].[TenantRegistry] tr
LEFT JOIN [MasterTenantRegistry].[MasterDB].[dbo].[TenantProductModule] tpm 
    ON tr.Id = tpm.TenantRegistryId;
```

## Recommendation

**Option 1 (Direct Query)** is recommended because:
- Simplest to implement
- No data synchronization issues
- Real-time accurate data
- Follows the principle of single source of truth

The Dev Portal should treat the Master Tenant Registry as the authoritative source and query it directly for all tenant information.