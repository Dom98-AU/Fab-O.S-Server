-- ============================================
-- Script: Update Existing Users with Company ID
-- Purpose: Ensure all existing users are assigned to default company (ID=1)
-- Author: Claude Code
-- Date: 2025-10-23
-- ============================================

-- Enable XACT_ABORT to ensure transaction rollback on error
SET XACT_ABORT ON;
GO

BEGIN TRANSACTION;

PRINT '========================================';
PRINT 'Starting User CompanyId Update Process';
PRINT '========================================';
PRINT '';

-- Step 1: Verify default company exists
PRINT 'Step 1: Verifying default company exists...';

IF NOT EXISTS (SELECT 1 FROM Companies WHERE Id = 1)
BEGIN
    PRINT '  ⚠ Default company (ID=1) does not exist. Creating it now...';

    SET IDENTITY_INSERT Companies ON;

    INSERT INTO Companies (Id, Name, ShortName, Code, IsActive, SubscriptionLevel, MaxUsers, CreatedDate, LastModified, Domain)
    VALUES (
        1,
        'Default Company',
        'DEFAULT',
        'DEF',
        1,
        'Standard',
        50,
        GETUTCDATE(),
        GETUTCDATE(),
        NULL
    );

    SET IDENTITY_INSERT Companies OFF;

    PRINT '  ✓ Default company created successfully';
END
ELSE
BEGIN
    PRINT '  ✓ Default company already exists';
END
PRINT '';

-- Step 2: Count users that need updating
DECLARE @UsersToUpdate INT;
DECLARE @TotalUsers INT;

SELECT @TotalUsers = COUNT(*) FROM Users;
SELECT @UsersToUpdate = COUNT(*)
FROM Users
WHERE CompanyId IS NULL OR CompanyId = 0;

PRINT 'Step 2: Analyzing users...';
PRINT '  Total users in database: ' + CAST(@TotalUsers AS VARCHAR(10));
PRINT '  Users with NULL or 0 CompanyId: ' + CAST(@UsersToUpdate AS VARCHAR(10));
PRINT '';

-- Step 3: Display users that will be updated
IF @UsersToUpdate > 0
BEGIN
    PRINT 'Step 3: Users to be updated:';
    PRINT '  ----------------------------------------';

    SELECT
        '  User ID: ' + CAST(Id AS VARCHAR(10)) +
        ', Username: ' + COALESCE(Username, 'N/A') +
        ', Email: ' + COALESCE(Email, 'N/A') +
        ', Current CompanyId: ' + COALESCE(CAST(CompanyId AS VARCHAR(10)), 'NULL') AS UserInfo
    FROM Users
    WHERE CompanyId IS NULL OR CompanyId = 0
    ORDER BY Id;

    PRINT '  ----------------------------------------';
    PRINT '';
END

-- Step 4: Update users
PRINT 'Step 4: Updating users...';

UPDATE Users
SET
    CompanyId = 1,
    LastModified = GETUTCDATE()
WHERE CompanyId IS NULL OR CompanyId = 0;

DECLARE @RowsAffected INT = @@ROWCOUNT;

IF @RowsAffected > 0
BEGIN
    PRINT '  ✓ Successfully updated ' + CAST(@RowsAffected AS VARCHAR(10)) + ' user(s) to CompanyId = 1';
END
ELSE
BEGIN
    PRINT '  ℹ No users needed updating - all users already have valid CompanyId';
END
PRINT '';

-- Step 5: Verify all users now have CompanyId
PRINT 'Step 5: Verification...';

DECLARE @UsersWithoutCompany INT;
SELECT @UsersWithoutCompany = COUNT(*)
FROM Users
WHERE CompanyId IS NULL OR CompanyId = 0;

IF @UsersWithoutCompany = 0
BEGIN
    PRINT '  ✓ SUCCESS: All users now have valid CompanyId assigned';
    PRINT '';
    PRINT 'Summary of users by company:';
    PRINT '  ----------------------------------------';

    SELECT
        '  Company ID: ' + CAST(c.Id AS VARCHAR(10)) +
        ', Name: ' + c.Name +
        ', User Count: ' + CAST(COUNT(u.Id) AS VARCHAR(10)) AS CompanySummary
    FROM Companies c
    LEFT JOIN Users u ON u.CompanyId = c.Id
    WHERE c.IsActive = 1
    GROUP BY c.Id, c.Name
    ORDER BY c.Id;

    PRINT '  ----------------------------------------';
    PRINT '';
    PRINT '✓ Transaction will be COMMITTED';
END
ELSE
BEGIN
    PRINT '  ✗ ERROR: ' + CAST(@UsersWithoutCompany AS VARCHAR(10)) + ' user(s) still have NULL or 0 CompanyId!';
    PRINT '  Rolling back transaction...';
    ROLLBACK TRANSACTION;
    PRINT '  ✗ Transaction ROLLED BACK';
    RETURN;
END

-- Commit the transaction
COMMIT TRANSACTION;

PRINT '';
PRINT '========================================';
PRINT '✓ User CompanyId Update Complete!';
PRINT '========================================';
PRINT '';
PRINT 'Next steps:';
PRINT '  1. Run the migration: dotnet ef database update';
PRINT '  2. This will make CompanyId NOT NULL in the schema';
PRINT '  3. All future users MUST have CompanyId assigned';
PRINT '';

GO
