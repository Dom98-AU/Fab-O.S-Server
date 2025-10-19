-- Final comprehensive diagnosis query

PRINT '========================================';
PRINT 'FINAL DIAGNOSIS REPORT';
PRINT '========================================';
PRINT '';

-- Check 1: Migration History Status
PRINT '1. MIGRATION HISTORY STATUS:';
PRINT '   Expected Migration: 20251004012055_AddTakeoffRevisionSystem';
IF EXISTS (SELECT * FROM [__EFMigrationsHistory] WHERE [MigrationId] = N'20251004012055_AddTakeoffRevisionSystem')
    PRINT '   Status: PRESENT in __EFMigrationsHistory'
ELSE
    PRINT '   Status: NOT PRESENT in __EFMigrationsHistory';
PRINT '';

-- Check 2: Database Objects Status
PRINT '2. DATABASE OBJECTS STATUS:';

-- TakeoffRevisions Table
PRINT '   TakeoffRevisions table:';
IF EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'TakeoffRevisions')
    PRINT '      ✓ EXISTS'
ELSE
    PRINT '      ✗ DOES NOT EXIST';

-- RevisionId Column in Packages
PRINT '   Packages.RevisionId column:';
IF EXISTS (SELECT * FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'Packages' AND COLUMN_NAME = 'RevisionId')
    PRINT '      ✓ EXISTS'
ELSE
    PRINT '      ✗ DOES NOT EXIST';

-- PackageId Column in TraceDrawings (should NOT exist after migration)
PRINT '   TraceDrawings.PackageId column (should be removed):';
IF EXISTS (SELECT * FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'TraceDrawings' AND COLUMN_NAME = 'PackageId')
    PRINT '      ✗ STILL EXISTS (should have been removed)'
ELSE
    PRINT '      ✓ REMOVED';

-- Foreign Key Constraint
PRINT '   FK_Packages_TakeoffRevisions_RevisionId constraint:';
IF EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLE_CONSTRAINTS WHERE CONSTRAINT_NAME = 'FK_Packages_TakeoffRevisions_RevisionId')
    PRINT '      ✓ EXISTS'
ELSE
    PRINT '      ✗ DOES NOT EXIST';

PRINT '';

-- Check 3: Previous Migration Status
PRINT '3. LATEST APPLIED MIGRATION:';
SELECT TOP 1 MigrationId, ProductVersion
FROM [__EFMigrationsHistory]
ORDER BY MigrationId DESC;
PRINT '';

-- Check 4: Diagnosis Summary
PRINT '========================================';
PRINT 'DIAGNOSIS SUMMARY:';
PRINT '========================================';

DECLARE @MigrationInHistory BIT = 0;
DECLARE @TableExists BIT = 0;
DECLARE @ColumnExists BIT = 0;

IF EXISTS (SELECT * FROM [__EFMigrationsHistory] WHERE [MigrationId] = N'20251004012055_AddTakeoffRevisionSystem')
    SET @MigrationInHistory = 1;

IF EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'TakeoffRevisions')
    SET @TableExists = 1;

IF EXISTS (SELECT * FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'Packages' AND COLUMN_NAME = 'RevisionId')
    SET @ColumnExists = 1;

PRINT '';
IF @MigrationInHistory = 0 AND @TableExists = 0 AND @ColumnExists = 0
BEGIN
    PRINT 'SCENARIO A: Migration never ran';
    PRINT '  - Migration not in history';
    PRINT '  - No database objects created';
    PRINT '  - Solution: Run the migration script or dotnet ef database update';
END
ELSE IF @MigrationInHistory = 1 AND @TableExists = 0
BEGIN
    PRINT 'SCENARIO B: Migration recorded but DDL never executed (CURRENT ISSUE)';
    PRINT '  - Migration IS in history table';
    PRINT '  - Database objects NOT created';
    PRINT '  - Likely cause: Migration was manually added to history before running DDL';
    PRINT '  - OR: SQL script logic flaw - all DDL was guarded by migration check';
    PRINT '  - Solution: Remove migration from history and re-run, OR run DDL directly';
END
ELSE IF @MigrationInHistory = 0 AND @TableExists = 1
BEGIN
    PRINT 'SCENARIO C: DDL ran but not recorded in history';
    PRINT '  - Objects exist but migration not recorded';
    PRINT '  - Solution: Add migration entry to __EFMigrationsHistory';
END
ELSE IF @MigrationInHistory = 1 AND @TableExists = 1
BEGIN
    PRINT 'SCENARIO D: Migration completed successfully';
    PRINT '  - Migration recorded AND objects created';
    PRINT '  - Status: OK (no issues)';
END

PRINT '';
PRINT '========================================';
