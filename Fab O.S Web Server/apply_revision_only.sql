-- Apply only the TakeoffRevisionSystem migration
-- This script is defensive and checks for existing objects

BEGIN TRANSACTION;
GO

-- Drop PackageId column from TraceDrawings if it exists
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251004012055_AddTakeoffRevisionSystem'
)
BEGIN
    IF EXISTS (SELECT * FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'TraceDrawings' AND COLUMN_NAME = 'PackageId')
    BEGIN
        ALTER TABLE TraceDrawings DROP COLUMN PackageId;
    END
END;
GO

-- Add RevisionId column to Packages if it doesn't exist
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251004012055_AddTakeoffRevisionSystem'
)
BEGIN
    IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'Packages' AND COLUMN_NAME = 'RevisionId')
    BEGIN
        ALTER TABLE Packages ADD RevisionId int NULL;
    END
END;
GO

-- Create TakeoffRevisions table if it doesn't exist
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251004012055_AddTakeoffRevisionSystem'
)
BEGIN
    IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'TakeoffRevisions')
    BEGIN
        CREATE TABLE TakeoffRevisions (
            Id int NOT NULL IDENTITY(1,1),
            TakeoffId int NOT NULL,
            RevisionCode nvarchar(5) NOT NULL,
            IsActive bit NOT NULL,
            Description nvarchar(500) NULL,
            CopiedFromRevisionId int NULL,
            CreatedBy int NOT NULL,
            CreatedDate datetime2 NOT NULL,
            LastModified datetime2 NOT NULL,
            IsDeleted bit NOT NULL,
            CONSTRAINT PK_TakeoffRevisions PRIMARY KEY (Id),
            CONSTRAINT FK_TakeoffRevisions_TakeoffRevisions_CopiedFromRevisionId FOREIGN KEY (CopiedFromRevisionId) REFERENCES TakeoffRevisions(Id),
            CONSTRAINT FK_TakeoffRevisions_TraceDrawings_TakeoffId FOREIGN KEY (TakeoffId) REFERENCES TraceDrawings(Id) ON DELETE CASCADE,
            CONSTRAINT FK_TakeoffRevisions_Users_CreatedBy FOREIGN KEY (CreatedBy) REFERENCES Users(Id) ON DELETE CASCADE
        );

        CREATE INDEX IX_TakeoffRevisions_CopiedFromRevisionId ON TakeoffRevisions(CopiedFromRevisionId);
        CREATE INDEX IX_TakeoffRevisions_CreatedBy ON TakeoffRevisions(CreatedBy);
        CREATE INDEX IX_TakeoffRevisions_TakeoffId ON TakeoffRevisions(TakeoffId);
    END
END;
GO

-- Create index on Packages.RevisionId if it doesn't exist
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251004012055_AddTakeoffRevisionSystem'
)
BEGIN
    IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_Packages_RevisionId' AND object_id = OBJECT_ID('Packages'))
    BEGIN
        CREATE INDEX IX_Packages_RevisionId ON Packages(RevisionId);
    END
END;
GO

-- Add foreign key constraint if it doesn't exist
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251004012055_AddTakeoffRevisionSystem'
)
BEGIN
    IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLE_CONSTRAINTS WHERE CONSTRAINT_NAME = 'FK_Packages_TakeoffRevisions_RevisionId')
    BEGIN
        ALTER TABLE Packages ADD CONSTRAINT FK_Packages_TakeoffRevisions_RevisionId FOREIGN KEY (RevisionId) REFERENCES TakeoffRevisions(Id);
    END
END;
GO

-- Data migration: Create Revision "A" for all existing takeoffs that don't have one
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251004012055_AddTakeoffRevisionSystem'
)
BEGIN
    INSERT INTO TakeoffRevisions (TakeoffId, RevisionCode, IsActive, Description, CreatedBy, CreatedDate, LastModified, IsDeleted)
    SELECT
        t.Id as TakeoffId,
        'A' as RevisionCode,
        1 as IsActive,
        'Initial revision' as Description,
        t.UploadedBy as CreatedBy,
        GETUTCDATE() as CreatedDate,
        GETUTCDATE() as LastModified,
        0 as IsDeleted
    FROM TraceDrawings t
    WHERE t.IsDeleted = 0
    AND NOT EXISTS (
        SELECT 1 FROM TakeoffRevisions r
        WHERE r.TakeoffId = t.Id AND r.RevisionCode = 'A'
    )
END;
GO

-- Data migration: Link existing packages to their takeoff's Revision A
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251004012055_AddTakeoffRevisionSystem'
)
BEGIN
    UPDATE p
    SET p.RevisionId = r.Id
    FROM Packages p
    INNER JOIN TraceDrawings t ON p.ProjectId = t.ProjectId
    INNER JOIN TakeoffRevisions r ON r.TakeoffId = t.Id AND r.RevisionCode = 'A'
    WHERE p.PackageSource = 'Takeoff'
    AND p.RevisionId IS NULL
END;
GO

-- Mark migration as applied
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251004012055_AddTakeoffRevisionSystem'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20251004012055_AddTakeoffRevisionSystem', N'8.0.0');
END;
GO

COMMIT;
GO

PRINT 'TakeoffRevisionSystem migration applied successfully';
