-- ============================================
-- Script: Make User.CompanyId Column NOT NULL
-- Purpose: Enforce CompanyId requirement at database level
-- Author: Claude Code
-- Date: 2025-10-23
-- IMPORTANT: Run UpdateExistingUsersWithCompanyId.sql FIRST!
-- ============================================

SET XACT_ABORT ON;
GO

BEGIN TRANSACTION;

PRINT '========================================';
PRINT 'Making User.CompanyId Required (NOT NULL)';
PRINT '========================================';
PRINT '';

-- Step 1: Verify no NULL CompanyId values exist
PRINT 'Step 1: Verifying all users have CompanyId...';

DECLARE @NullCount INT;
SELECT @NullCount = COUNT(*)
FROM Users
WHERE CompanyId IS NULL OR CompanyId = 0;

IF @NullCount > 0
BEGIN
    PRINT '  ✗ ERROR: Found ' + CAST(@NullCount AS VARCHAR(10)) + ' user(s) with NULL or 0 CompanyId!';
    PRINT '  Please run UpdateExistingUsersWithCompanyId.sql first!';
    ROLLBACK TRANSACTION;
    RETURN;
END

PRINT '  ✓ All users have valid CompanyId';
PRINT '';

-- Step 2: Drop dependent index
PRINT 'Step 2: Dropping dependent index...';

IF EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Users_CompanyId' AND object_id = OBJECT_ID('Users'))
BEGIN
    DROP INDEX IX_Users_CompanyId ON Users;
    PRINT '  ✓ Dropped index IX_Users_CompanyId';
END
ELSE
BEGIN
    PRINT '  ℹ Index IX_Users_CompanyId does not exist (already dropped)';
END
PRINT '';

-- Step 3: Alter column to NOT NULL with default value
PRINT 'Step 3: Altering Users.CompanyId column...';

-- Add default constraint first
IF NOT EXISTS (SELECT 1 FROM sys.default_constraints WHERE name = 'DF_Users_CompanyId')
BEGIN
    ALTER TABLE Users
    ADD CONSTRAINT DF_Users_CompanyId DEFAULT 1 FOR CompanyId;
    PRINT '  ✓ Added default constraint (DEFAULT 1)';
END
ELSE
BEGIN
    PRINT '  ℹ Default constraint already exists';
END

-- Alter column to NOT NULL
ALTER TABLE Users
ALTER COLUMN CompanyId INT NOT NULL;

PRINT '  ✓ Column altered to NOT NULL';
PRINT '';

-- Step 4: Recreate index
PRINT 'Step 4: Recreating index...';

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Users_CompanyId' AND object_id = OBJECT_ID('Users'))
BEGIN
    CREATE NONCLUSTERED INDEX IX_Users_CompanyId
    ON Users(CompanyId);
    PRINT '  ✓ Recreated index IX_Users_CompanyId';
END
ELSE
BEGIN
    PRINT '  ℹ Index already exists';
END
PRINT '';

-- Step 5: Verify schema change
PRINT 'Step 5: Verifying schema change...';

-- Check if column is NOT NULL
IF EXISTS (
    SELECT 1
    FROM INFORMATION_SCHEMA.COLUMNS
    WHERE TABLE_NAME = 'Users'
      AND COLUMN_NAME = 'CompanyId'
      AND IS_NULLABLE = 'NO'
)
BEGIN
    PRINT '  ✓ SUCCESS: Users.CompanyId is now NOT NULL';
END
ELSE
BEGIN
    PRINT '  ✗ ERROR: Schema change failed!';
    ROLLBACK TRANSACTION;
    RETURN;
END

-- Check if default constraint exists
IF EXISTS (
    SELECT 1
    FROM sys.default_constraints
    WHERE name = 'DF_Users_CompanyId'
      AND parent_object_id = OBJECT_ID('Users')
)
BEGIN
    PRINT '  ✓ SUCCESS: Default constraint (DF_Users_CompanyId) created';
END
ELSE
BEGIN
    PRINT '  ⚠ WARNING: Default constraint not found (may have different name)';
END

PRINT '';

-- Commit transaction
COMMIT TRANSACTION;

PRINT '========================================';
PRINT '✓ Schema Migration Complete!';
PRINT '========================================';
PRINT '';
PRINT 'CompanyId is now REQUIRED for all users:';
PRINT '  - Column: NOT NULL';
PRINT '  - Default: 1 (default company)';
PRINT '  - Future INSERTs without CompanyId will default to 1';
PRINT '  - Application enforces CompanyId requirement';
PRINT '';

GO