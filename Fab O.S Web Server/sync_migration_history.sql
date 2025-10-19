-- Sync migration history with actual database state
-- This marks migrations as applied that have already been manually run

-- Check if migrations already exist before inserting
IF NOT EXISTS (SELECT 1 FROM __EFMigrationsHistory WHERE MigrationId = '20250922020049_UpdateCustomerAndContactTables')
    INSERT INTO __EFMigrationsHistory (MigrationId, ProductVersion)
    VALUES ('20250922020049_UpdateCustomerAndContactTables', '8.0.8');

IF NOT EXISTS (SELECT 1 FROM __EFMigrationsHistory WHERE MigrationId = '20250922043044_AddAddressFieldsToCustomerContact')
    INSERT INTO __EFMigrationsHistory (MigrationId, ProductVersion)
    VALUES ('20250922043044_AddAddressFieldsToCustomerContact', '8.0.8');

IF NOT EXISTS (SELECT 1 FROM __EFMigrationsHistory WHERE MigrationId = '20250929050816_AddShortNameToCompany')
    INSERT INTO __EFMigrationsHistory (MigrationId, ProductVersion)
    VALUES ('20250929050816_AddShortNameToCompany', '8.0.8');

IF NOT EXISTS (SELECT 1 FROM __EFMigrationsHistory WHERE MigrationId = '20250930231103_AddMissingSchemaChanges')
    INSERT INTO __EFMigrationsHistory (MigrationId, ProductVersion)
    VALUES ('20250930231103_AddMissingSchemaChanges', '8.0.8');

PRINT 'Migration history synced successfully';
SELECT MigrationId, ProductVersion FROM __EFMigrationsHistory ORDER BY MigrationId;
