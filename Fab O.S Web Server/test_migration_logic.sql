-- Test to demonstrate the logic flaw in apply_revision_only.sql

-- The script checks: "IF NOT EXISTS migration in history"
-- This means: "If migration is NOT already applied"
-- When TRUE (migration not applied): Execute the DDL
-- When FALSE (migration IS applied): Skip the DDL

-- Let's check the current state
DECLARE @MigrationExists BIT;

IF EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251004012055_AddTakeoffRevisionSystem'
)
    SET @MigrationExists = 1
ELSE
    SET @MigrationExists = 0;

PRINT 'Migration exists in history: ' + CAST(@MigrationExists AS VARCHAR(1));

-- The problem: The script guards EVERY action with this check
-- So when the script runs the FIRST time:
-- 1. Migration is NOT in history
-- 2. All DDL statements run (table creation, etc.)
-- 3. Migration is added to history
-- 4. Script reports success

-- But if the script was run with the migration ALREADY in history:
-- 1. Migration IS in history
-- 2. ALL DDL statements are SKIPPED (including table creation!)
-- 3. Migration insert is also skipped
-- 4. Script still reports "success" because no errors

PRINT '';
PRINT 'DIAGNOSIS:';
PRINT 'The migration was likely added to __EFMigrationsHistory before the script ran.';
PRINT 'This caused ALL DDL operations to be skipped, including TakeoffRevisions table creation.';
PRINT '';
PRINT 'The script logic assumes the migration does NOT exist in history when it runs.';
PRINT 'If the migration entry was already present, nothing happens except the success message.';
