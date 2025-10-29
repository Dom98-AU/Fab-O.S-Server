-- Migration: Add ContactNumber to CustomerContacts table
-- Date: 2025-10-27
-- Description: Adds ContactNumber field to CustomerContacts table for auto-generated contact identifiers

USE [sqldb-steel-estimation-sandbox];
GO

-- Check if column already exists
IF NOT EXISTS (
    SELECT 1
    FROM INFORMATION_SCHEMA.COLUMNS
    WHERE TABLE_NAME = 'CustomerContacts'
    AND COLUMN_NAME = 'ContactNumber'
)
BEGIN
    PRINT 'Adding ContactNumber column to CustomerContacts table...';

    -- Add the ContactNumber column
    ALTER TABLE [dbo].[CustomerContacts]
    ADD [ContactNumber] NVARCHAR(50) NULL;

    PRINT 'ContactNumber column added successfully.';
END
ELSE
BEGIN
    PRINT 'ContactNumber column already exists in CustomerContacts table.';
END
GO

-- Optionally, add an index for better query performance
IF NOT EXISTS (
    SELECT 1
    FROM sys.indexes
    WHERE name = 'IX_CustomerContacts_ContactNumber'
    AND object_id = OBJECT_ID('dbo.CustomerContacts')
)
BEGIN
    PRINT 'Creating index on ContactNumber...';

    CREATE INDEX [IX_CustomerContacts_ContactNumber]
    ON [dbo].[CustomerContacts] ([ContactNumber]);

    PRINT 'Index created successfully.';
END
ELSE
BEGIN
    PRINT 'Index on ContactNumber already exists.';
END
GO

-- Verify the changes
SELECT
    COLUMN_NAME,
    DATA_TYPE,
    CHARACTER_MAXIMUM_LENGTH,
    IS_NULLABLE
FROM INFORMATION_SCHEMA.COLUMNS
WHERE TABLE_NAME = 'CustomerContacts'
AND COLUMN_NAME = 'ContactNumber';
GO

PRINT 'Migration completed successfully!';
GO
