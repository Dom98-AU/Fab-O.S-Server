/*
 * Migration: Add Cloud Storage Provider Fields to PackageDrawings
 * Date: October 19, 2025
 * Purpose: Add multi-provider cloud storage support (SharePoint, GoogleDrive, Dropbox, Azure Blob)
 * Backward Compatible: YES - All fields nullable, SharePoint remains default
 */

-- Add new cloud storage provider fields
ALTER TABLE PackageDrawings
    ADD StorageProvider NVARCHAR(50) NULL,
        ProviderFileId NVARCHAR(500) NULL,
        ProviderMetadata NVARCHAR(MAX) NULL;

GO

-- Backfill existing records: Set StorageProvider to 'SharePoint' for all existing records
UPDATE PackageDrawings
SET StorageProvider = 'SharePoint',
    ProviderFileId = SharePointItemId,
    ProviderMetadata = (
        SELECT JSON_OBJECT(
            'SharePointItemId': SharePointItemId,
            'SharePointUrl': SharePointUrl,
            'MigratedFrom': 'Legacy'
        )
    )
WHERE StorageProvider IS NULL
  AND SharePointItemId IS NOT NULL
  AND SharePointItemId <> '';

GO

-- Create index on StorageProvider for faster lookups
CREATE NONCLUSTERED INDEX IX_PackageDrawings_StorageProvider
ON PackageDrawings(StorageProvider)
INCLUDE (ProviderFileId);

GO

-- Verify migration
SELECT
    'Migration Summary' AS [Step],
    COUNT(*) AS [Total Records],
    SUM(CASE WHEN StorageProvider IS NOT NULL THEN 1 ELSE 0 END) AS [Records with StorageProvider],
    SUM(CASE WHEN StorageProvider = 'SharePoint' THEN 1 ELSE 0 END) AS [SharePoint Records],
    SUM(CASE WHEN StorageProvider IS NULL THEN 1 ELSE 0 END) AS [Records without Provider]
FROM PackageDrawings;

GO
