-- Investigation Script for TakeoffRevisions Table Issue
-- This script checks:
-- 1. If TakeoffRevisions table exists
-- 2. Migration history
-- 3. All tables in database
-- 4. Schema details

PRINT '=== 1. CHECKING IF TakeoffRevisions TABLE EXISTS ===';
IF EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'TakeoffRevisions')
    PRINT 'TakeoffRevisions table EXISTS'
ELSE
    PRINT 'TakeoffRevisions table DOES NOT EXIST';
PRINT '';

PRINT '=== 2. CHECKING MIGRATION HISTORY ===';
SELECT
    MigrationId,
    ProductVersion,
    CASE
        WHEN MigrationId = N'20251004012055_AddTakeoffRevisionSystem' THEN '<<< TARGET MIGRATION'
        ELSE ''
    END as Notes
FROM [__EFMigrationsHistory]
ORDER BY MigrationId;
PRINT '';

PRINT '=== 3. ALL TABLES IN DATABASE ===';
SELECT TABLE_NAME
FROM INFORMATION_SCHEMA.TABLES
WHERE TABLE_TYPE = 'BASE TABLE'
ORDER BY TABLE_NAME;
PRINT '';

PRINT '=== 4. CHECKING FOR TABLES WITH "REVISION" IN NAME ===';
SELECT TABLE_NAME
FROM INFORMATION_SCHEMA.TABLES
WHERE TABLE_TYPE = 'BASE TABLE'
  AND TABLE_NAME LIKE '%Revision%'
ORDER BY TABLE_NAME;
PRINT '';

PRINT '=== 5. CHECKING RevisionId COLUMN IN Packages TABLE ===';
IF EXISTS (SELECT * FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'Packages' AND COLUMN_NAME = 'RevisionId')
    PRINT 'RevisionId column EXISTS in Packages table'
ELSE
    PRINT 'RevisionId column DOES NOT EXIST in Packages table';
PRINT '';

PRINT '=== 6. CHECKING TraceDrawings TABLE FOR PackageId COLUMN ===';
IF EXISTS (SELECT * FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'TraceDrawings' AND COLUMN_NAME = 'PackageId')
    PRINT 'PackageId column EXISTS in TraceDrawings table (SHOULD NOT EXIST after migration)'
ELSE
    PRINT 'PackageId column DOES NOT EXIST in TraceDrawings table (CORRECT)';
PRINT '';

PRINT '=== INVESTIGATION COMPLETE ===';
