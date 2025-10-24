-- Manual Migration: Add PdfEditLocks Table
-- Created: 2025-10-22
-- Purpose: Add PDF edit lock system for per-drawing locking

SET QUOTED_IDENTIFIER ON;
GO

-- Check if table already exists
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'PdfEditLocks')
BEGIN
    CREATE TABLE [dbo].[PdfEditLocks] (
        [Id] INT IDENTITY(1,1) NOT NULL,
        [PackageDrawingId] INT NOT NULL,
        [SessionId] NVARCHAR(255) NOT NULL,
        [UserId] INT NOT NULL,
        [UserName] NVARCHAR(255) NOT NULL,
        [LockedAt] DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
        [LastHeartbeat] DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
        [LastActivityAt] DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
        [IsActive] BIT NOT NULL DEFAULT 1,
        CONSTRAINT [PK_PdfEditLocks] PRIMARY KEY CLUSTERED ([Id] ASC),
        CONSTRAINT [FK_PdfEditLocks_PackageDrawings_PackageDrawingId]
            FOREIGN KEY ([PackageDrawingId]) REFERENCES [dbo].[PackageDrawings]([Id]) ON DELETE CASCADE,
        CONSTRAINT [FK_PdfEditLocks_Users_UserId]
            FOREIGN KEY ([UserId]) REFERENCES [dbo].[Users]([Id])
    );

    -- Create indexes for performance
    CREATE NONCLUSTERED INDEX [IX_PdfEditLocks_PackageDrawingId_IsActive]
        ON [dbo].[PdfEditLocks]([PackageDrawingId], [IsActive]);

    CREATE NONCLUSTERED INDEX [IX_PdfEditLocks_SessionId]
        ON [dbo].[PdfEditLocks]([SessionId]);

    CREATE NONCLUSTERED INDEX [IX_PdfEditLocks_UserId]
        ON [dbo].[PdfEditLocks]([UserId]);

    CREATE NONCLUSTERED INDEX [IX_PdfEditLocks_LastHeartbeat]
        ON [dbo].[PdfEditLocks]([LastHeartbeat])
        WHERE [IsActive] = 1;

    PRINT 'PdfEditLocks table created successfully';
END
ELSE
BEGIN
    PRINT 'PdfEditLocks table already exists - skipping creation';
END
GO
