-- Add ContactPerson column to TraceDrawings table if it doesn't exist
IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'TraceDrawings' AND COLUMN_NAME = 'ContactPerson')
BEGIN
    ALTER TABLE TraceDrawings ADD ContactPerson nvarchar(200) NULL;
END

-- Add ProjectArchitect column to TraceDrawings table if it doesn't exist
IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'TraceDrawings' AND COLUMN_NAME = 'ProjectArchitect')
BEGIN
    ALTER TABLE TraceDrawings ADD ProjectArchitect nvarchar(200) NULL;
END

-- Add ProjectEngineer column to TraceDrawings table if it doesn't exist
IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'TraceDrawings' AND COLUMN_NAME = 'ProjectEngineer')
BEGIN
    ALTER TABLE TraceDrawings ADD ProjectEngineer nvarchar(200) NULL;
END

-- Create ModuleSettings table if it doesn't exist
IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'ModuleSettings')
BEGIN
    CREATE TABLE ModuleSettings (
        Id int NOT NULL IDENTITY(1,1),
        ModuleName nvarchar(50) NOT NULL,
        CompanyId int NOT NULL,
        SettingKey nvarchar(100) NOT NULL,
        SettingValue nvarchar(max) NOT NULL,
        SettingType nvarchar(50) NULL,
        Description nvarchar(500) NULL,
        IsUserSpecific bit NOT NULL DEFAULT 0,
        UserId int NULL,
        IsActive bit NOT NULL DEFAULT 1,
        CreatedDate datetime2 NOT NULL DEFAULT GETDATE(),
        LastModified datetime2 NOT NULL DEFAULT GETDATE(),
        CreatedByUserId int NULL,
        LastModifiedByUserId int NULL,
        CONSTRAINT PK_ModuleSettings PRIMARY KEY (Id)
    );
END

-- Create NumberSeries table if it doesn't exist
IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'NumberSeries')
BEGIN
    CREATE TABLE NumberSeries (
        Id int NOT NULL IDENTITY(1,1),
        CompanyId int NOT NULL,
        EntityType nvarchar(50) NOT NULL,
        Prefix nvarchar(20) NULL,
        Suffix nvarchar(20) NULL,
        CurrentNumber int NOT NULL DEFAULT 1,
        StartingNumber int NOT NULL DEFAULT 1,
        IncrementBy int NOT NULL DEFAULT 1,
        MinDigits int NOT NULL DEFAULT 1,
        Format nvarchar(100) NULL,
        IncludeYear bit NOT NULL DEFAULT 0,
        IncludeMonth bit NOT NULL DEFAULT 0,
        IncludeCompanyCode bit NOT NULL DEFAULT 0,
        ResetYearly bit NOT NULL DEFAULT 0,
        ResetMonthly bit NOT NULL DEFAULT 0,
        LastResetYear int NULL,
        LastResetMonth int NULL,
        IsActive bit NOT NULL DEFAULT 1,
        AllowManualEntry bit NOT NULL DEFAULT 1,
        Description nvarchar(200) NULL,
        PreviewExample nvarchar(50) NULL,
        LastUsed datetime2 NOT NULL DEFAULT GETDATE(),
        CreatedDate datetime2 NOT NULL DEFAULT GETDATE(),
        LastModified datetime2 NOT NULL DEFAULT GETDATE(),
        CreatedByUserId int NULL,
        LastModifiedByUserId int NULL,
        CONSTRAINT PK_NumberSeries PRIMARY KEY (Id)
    );
END

-- Create GlobalSettings table if it doesn't exist
IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'GlobalSettings')
BEGIN
    CREATE TABLE GlobalSettings (
        Id int NOT NULL IDENTITY(1,1),
        SettingKey nvarchar(100) NOT NULL,
        SettingValue nvarchar(max) NOT NULL,
        SettingType nvarchar(50) NULL,
        Category nvarchar(50) NULL,
        Description nvarchar(500) NULL,
        IsSystemSetting bit NOT NULL DEFAULT 0,
        RequiresRestart bit NOT NULL DEFAULT 0,
        IsEncrypted bit NOT NULL DEFAULT 0,
        ValidationRule nvarchar(500) NULL,
        DefaultValue nvarchar(max) NULL,
        IsActive bit NOT NULL DEFAULT 1,
        CreatedDate datetime2 NOT NULL DEFAULT GETDATE(),
        LastModified datetime2 NOT NULL DEFAULT GETDATE(),
        LastModifiedByUserId int NULL,
        CONSTRAINT PK_GlobalSettings PRIMARY KEY (Id)
    );
END

PRINT 'Schema fixes applied successfully';