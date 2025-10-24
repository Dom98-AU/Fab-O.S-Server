-- Add filtered index on LastHeartbeat for PdfEditLocks
SET QUOTED_IDENTIFIER ON;
GO

IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_PdfEditLocks_LastHeartbeat' AND object_id = OBJECT_ID('PdfEditLocks'))
BEGIN
    CREATE NONCLUSTERED INDEX [IX_PdfEditLocks_LastHeartbeat]
        ON [dbo].[PdfEditLocks]([LastHeartbeat])
        WHERE [IsActive] = 1;
    PRINT 'Index IX_PdfEditLocks_LastHeartbeat created successfully';
END
ELSE
BEGIN
    PRINT 'Index IX_PdfEditLocks_LastHeartbeat already exists - skipping creation';
END
GO
