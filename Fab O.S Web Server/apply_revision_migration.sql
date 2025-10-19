IF OBJECT_ID(N'[__EFMigrationsHistory]') IS NULL
BEGIN
    CREATE TABLE [__EFMigrationsHistory] (
        [MigrationId] nvarchar(150) NOT NULL,
        [ProductVersion] nvarchar(32) NOT NULL,
        CONSTRAINT [PK___EFMigrationsHistory] PRIMARY KEY ([MigrationId])
    );
END;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250922020049_UpdateCustomerAndContactTables'
)
BEGIN
    CREATE TABLE [Companies] (
        [Id] int NOT NULL IDENTITY,
        [Name] nvarchar(200) NOT NULL,
        [Code] nvarchar(50) NOT NULL,
        [IsActive] bit NOT NULL DEFAULT CAST(1 AS bit),
        [SubscriptionLevel] nvarchar(50) NOT NULL DEFAULT N'Standard',
        [MaxUsers] int NOT NULL DEFAULT 10,
        [CreatedDate] datetime2 NOT NULL,
        [LastModified] datetime2 NOT NULL,
        [Domain] nvarchar(100) NULL,
        CONSTRAINT [PK_Companies] PRIMARY KEY ([Id])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250922020049_UpdateCustomerAndContactTables'
)
BEGIN
    CREATE TABLE [Customers] (
        [Id] int NOT NULL IDENTITY,
        [Name] nvarchar(200) NOT NULL,
        [Code] nvarchar(50) NULL,
        [ABN] nvarchar(20) NULL,
        [ContactPerson] nvarchar(200) NULL,
        [Email] nvarchar(200) NULL,
        [PhoneNumber] nvarchar(20) NULL,
        [Address] nvarchar(500) NULL,
        [Website] nvarchar(100) NULL,
        [Industry] nvarchar(50) NULL,
        [Notes] nvarchar(1000) NULL,
        [IsActive] bit NOT NULL,
        [CreatedDate] datetime2 NOT NULL,
        [LastModified] datetime2 NOT NULL,
        CONSTRAINT [PK_Customers] PRIMARY KEY ([Id])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250922020049_UpdateCustomerAndContactTables'
)
BEGIN
    CREATE TABLE [EfficiencyRates] (
        [Id] int NOT NULL IDENTITY,
        [Name] nvarchar(100) NOT NULL,
        [Description] nvarchar(500) NULL,
        [Rate] decimal(5,2) NOT NULL,
        [IsActive] bit NOT NULL,
        [CreatedDate] datetime2 NOT NULL,
        [LastModified] datetime2 NOT NULL,
        CONSTRAINT [PK_EfficiencyRates] PRIMARY KEY ([Id])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250922020049_UpdateCustomerAndContactTables'
)
BEGIN
    CREATE TABLE [RoutingTemplates] (
        [Id] int NOT NULL IDENTITY,
        [Name] nvarchar(100) NOT NULL,
        [Description] nvarchar(500) NULL,
        [IsActive] bit NOT NULL,
        [CreatedDate] datetime2 NOT NULL,
        [LastModified] datetime2 NOT NULL,
        CONSTRAINT [PK_RoutingTemplates] PRIMARY KEY ([Id])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250922020049_UpdateCustomerAndContactTables'
)
BEGIN
    CREATE TABLE [Assemblies] (
        [Id] int NOT NULL IDENTITY,
        [AssemblyNumber] nvarchar(100) NOT NULL,
        [Description] nvarchar(200) NOT NULL,
        [Version] nvarchar(50) NULL,
        [DrawingNumber] nvarchar(100) NULL,
        [ParentAssemblyId] int NULL,
        [EstimatedWeight] decimal(18,4) NULL,
        [EstimatedCost] decimal(18,4) NULL,
        [EstimatedHours] decimal(18,2) NULL,
        [IsActive] bit NOT NULL,
        [CreatedDate] datetime2 NOT NULL DEFAULT (getutcdate()),
        [ModifiedDate] datetime2 NULL,
        [CompanyId] int NOT NULL,
        CONSTRAINT [PK_Assemblies] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_Assemblies_Assemblies_ParentAssemblyId] FOREIGN KEY ([ParentAssemblyId]) REFERENCES [Assemblies] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_Assemblies_Companies_CompanyId] FOREIGN KEY ([CompanyId]) REFERENCES [Companies] ([Id]) ON DELETE CASCADE
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250922020049_UpdateCustomerAndContactTables'
)
BEGIN
    CREATE TABLE [CatalogueItems] (
        [Id] int NOT NULL IDENTITY,
        [ItemCode] nvarchar(50) NOT NULL,
        [Description] nvarchar(500) NOT NULL,
        [Category] nvarchar(50) NOT NULL,
        [Material] nvarchar(50) NOT NULL,
        [Profile] nvarchar(50) NULL,
        [Length_mm] decimal(10,2) NULL,
        [Width_mm] decimal(10,2) NULL,
        [Height_mm] decimal(10,2) NULL,
        [Depth_mm] decimal(10,2) NULL,
        [Thickness_mm] decimal(10,2) NULL,
        [Diameter_mm] decimal(10,2) NULL,
        [OD_mm] decimal(10,2) NULL,
        [ID_mm] decimal(10,2) NULL,
        [WallThickness_mm] decimal(10,2) NULL,
        [Wall_mm] decimal(10,2) NULL,
        [NominalBore] nvarchar(20) NULL,
        [ImperialEquiv] nvarchar(20) NULL,
        [Web_mm] decimal(10,2) NULL,
        [Flange_mm] decimal(10,2) NULL,
        [A_mm] decimal(10,2) NULL,
        [B_mm] decimal(10,2) NULL,
        [Size_mm] nvarchar(50) NULL,
        [Size] decimal(10,2) NULL,
        [Size_inch] nvarchar(20) NULL,
        [BMT_mm] decimal(10,2) NULL,
        [BaseThickness_mm] decimal(10,2) NULL,
        [RaisedThickness_mm] decimal(10,2) NULL,
        [Mass_kg_m] decimal(10,3) NULL,
        [Mass_kg_m2] decimal(10,3) NULL,
        [Mass_kg_length] decimal(10,3) NULL,
        [Weight_kg] decimal(10,3) NULL,
        [Weight_kg_m2] decimal(10,3) NULL,
        [SurfaceArea_m2] decimal(10,4) NULL,
        [SurfaceArea_m2_per_m] decimal(10,4) NULL,
        [SurfaceArea_m2_per_m2] decimal(10,4) NULL,
        [Surface] nvarchar(50) NULL,
        [Standard] nvarchar(50) NULL,
        [Grade] nvarchar(50) NULL,
        [Alloy] nvarchar(50) NULL,
        [Temper] nvarchar(50) NULL,
        [Finish] nvarchar(100) NULL,
        [Finish_Standard] nvarchar(50) NULL,
        [Coating] nvarchar(50) NULL,
        [StandardLengths] nvarchar(200) NULL,
        [StandardLength_m] int NULL,
        [Cut_To_Size] nvarchar(20) NULL,
        [Type] nvarchar(50) NULL,
        [ProductType] nvarchar(50) NULL,
        [Pattern] nvarchar(50) NULL,
        [Features] nvarchar(500) NULL,
        [Tolerance] nvarchar(50) NULL,
        [Pressure] nvarchar(50) NULL,
        [SupplierCode] nvarchar(100) NULL,
        [PackQty] int NULL,
        [Unit] nvarchar(20) NULL,
        [Compliance] nvarchar(200) NULL,
        [Duty_Rating] nvarchar(50) NULL,
        [IsActive] bit NOT NULL,
        [CreatedDate] datetime2 NOT NULL DEFAULT (getutcdate()),
        [ModifiedDate] datetime2 NULL,
        [CompanyId] int NOT NULL,
        CONSTRAINT [PK_CatalogueItems] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_CatalogueItems_Companies_CompanyId] FOREIGN KEY ([CompanyId]) REFERENCES [Companies] ([Id]) ON DELETE CASCADE
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250922020049_UpdateCustomerAndContactTables'
)
BEGIN
    CREATE TABLE [Users] (
        [Id] int NOT NULL IDENTITY,
        [Username] nvarchar(100) NOT NULL,
        [Email] nvarchar(200) NOT NULL,
        [PasswordHash] nvarchar(500) NOT NULL,
        [SecurityStamp] nvarchar(500) NOT NULL,
        [FirstName] nvarchar(100) NULL,
        [LastName] nvarchar(100) NULL,
        [CompanyName] nvarchar(200) NULL,
        [JobTitle] nvarchar(100) NULL,
        [PhoneNumber] nvarchar(20) NULL,
        [IsActive] bit NOT NULL,
        [IsEmailConfirmed] bit NOT NULL,
        [EmailConfirmationToken] nvarchar(max) NULL,
        [PasswordResetToken] nvarchar(max) NULL,
        [PasswordResetExpiry] datetime2 NULL,
        [LastLoginDate] datetime2 NULL,
        [LastLoginAt] datetime2 NULL,
        [FailedLoginAttempts] int NOT NULL,
        [LockedOutUntil] datetime2 NULL,
        [CreatedDate] datetime2 NOT NULL,
        [LastModified] datetime2 NOT NULL,
        [CompanyId] int NULL,
        [PasswordSalt] nvarchar(100) NULL,
        [AuthProvider] nvarchar(50) NULL,
        [ExternalUserId] nvarchar(256) NULL,
        CONSTRAINT [PK_Users] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_Users_Companies_CompanyId] FOREIGN KEY ([CompanyId]) REFERENCES [Companies] ([Id])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250922020049_UpdateCustomerAndContactTables'
)
BEGIN
    CREATE TABLE [WorkCenters] (
        [Id] int NOT NULL IDENTITY,
        [WorkCenterCode] nvarchar(50) NOT NULL,
        [WorkCenterName] nvarchar(200) NOT NULL,
        [Description] nvarchar(500) NULL,
        [CompanyId] int NOT NULL,
        [Department] nvarchar(100) NULL,
        [IsActive] bit NOT NULL,
        [CreatedDate] datetime2 NOT NULL,
        [LastModified] datetime2 NOT NULL,
        CONSTRAINT [PK_WorkCenters] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_WorkCenters_Companies_CompanyId] FOREIGN KEY ([CompanyId]) REFERENCES [Companies] ([Id]) ON DELETE CASCADE
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250922020049_UpdateCustomerAndContactTables'
)
BEGIN
    CREATE TABLE [CustomerAddresses] (
        [Id] int NOT NULL IDENTITY,
        [CustomerId] int NOT NULL,
        [AddressType] nvarchar(50) NOT NULL,
        [AddressLine1] nvarchar(200) NOT NULL,
        [AddressLine2] nvarchar(200) NULL,
        [City] nvarchar(100) NOT NULL,
        [State] nvarchar(100) NOT NULL,
        [PostalCode] nvarchar(20) NOT NULL,
        [Country] nvarchar(100) NOT NULL,
        [GooglePlaceId] nvarchar(500) NULL,
        [Latitude] decimal(10,7) NULL,
        [Longitude] decimal(10,7) NULL,
        [FormattedAddress] nvarchar(500) NULL,
        [IsPrimary] bit NOT NULL,
        [IsActive] bit NOT NULL,
        [Notes] nvarchar(500) NULL,
        [CreatedDate] datetime2 NOT NULL,
        [LastModified] datetime2 NOT NULL,
        CONSTRAINT [PK_CustomerAddresses] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_CustomerAddresses_Customers_CustomerId] FOREIGN KEY ([CustomerId]) REFERENCES [Customers] ([Id]) ON DELETE CASCADE
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250922020049_UpdateCustomerAndContactTables'
)
BEGIN
    CREATE TABLE [CustomerContacts] (
        [Id] int NOT NULL IDENTITY,
        [CustomerId] int NOT NULL,
        [FirstName] nvarchar(100) NOT NULL,
        [LastName] nvarchar(100) NOT NULL,
        [Title] nvarchar(50) NULL,
        [Department] nvarchar(100) NULL,
        [Email] nvarchar(200) NOT NULL,
        [PhoneNumber] nvarchar(20) NULL,
        [MobileNumber] nvarchar(20) NULL,
        [IsPrimary] bit NOT NULL,
        [IsActive] bit NOT NULL,
        [Notes] nvarchar(500) NULL,
        [CreatedDate] datetime2 NOT NULL,
        [LastModified] datetime2 NOT NULL,
        CONSTRAINT [PK_CustomerContacts] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_CustomerContacts_Customers_CustomerId] FOREIGN KEY ([CustomerId]) REFERENCES [Customers] ([Id]) ON DELETE CASCADE
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250922020049_UpdateCustomerAndContactTables'
)
BEGIN
    CREATE TABLE [RoutingOperations] (
        [Id] int NOT NULL IDENTITY,
        [RoutingTemplateId] int NOT NULL,
        [OperationName] nvarchar(100) NOT NULL,
        [Description] nvarchar(500) NULL,
        [Sequence] int NOT NULL,
        [SetupTime] decimal(10,2) NULL,
        [RunTime] decimal(10,2) NULL,
        [IsActive] bit NOT NULL,
        [CreatedDate] datetime2 NOT NULL,
        [LastModified] datetime2 NOT NULL,
        CONSTRAINT [PK_RoutingOperations] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_RoutingOperations_RoutingTemplates_RoutingTemplateId] FOREIGN KEY ([RoutingTemplateId]) REFERENCES [RoutingTemplates] ([Id]) ON DELETE CASCADE
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250922020049_UpdateCustomerAndContactTables'
)
BEGIN
    CREATE TABLE [AssemblyComponents] (
        [Id] int NOT NULL IDENTITY,
        [AssemblyId] int NOT NULL,
        [CatalogueItemId] int NULL,
        [ComponentAssemblyId] int NULL,
        [ComponentReference] nvarchar(100) NOT NULL,
        [Quantity] decimal(18,4) NOT NULL,
        [Unit] nvarchar(20) NOT NULL,
        [Notes] nvarchar(500) NULL,
        [AlternateItems] nvarchar(100) NULL,
        [IsActive] bit NOT NULL,
        [CreatedDate] datetime2 NOT NULL DEFAULT (getutcdate()),
        CONSTRAINT [PK_AssemblyComponents] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_AssemblyComponents_Assemblies_AssemblyId] FOREIGN KEY ([AssemblyId]) REFERENCES [Assemblies] ([Id]) ON DELETE CASCADE,
        CONSTRAINT [FK_AssemblyComponents_Assemblies_ComponentAssemblyId] FOREIGN KEY ([ComponentAssemblyId]) REFERENCES [Assemblies] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_AssemblyComponents_CatalogueItems_CatalogueItemId] FOREIGN KEY ([CatalogueItemId]) REFERENCES [CatalogueItems] ([Id]) ON DELETE NO ACTION
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250922020049_UpdateCustomerAndContactTables'
)
BEGIN
    CREATE TABLE [GratingSpecifications] (
        [Id] int NOT NULL IDENTITY,
        [CatalogueItemId] int NOT NULL,
        [LoadBar_Height_mm] decimal(10,2) NULL,
        [LoadBar_Spacing_mm] decimal(10,2) NULL,
        [LoadBar_Thickness_mm] decimal(10,2) NULL,
        [CrossBar_Spacing_mm] decimal(10,2) NULL,
        [Standard_Panel_Length_mm] decimal(10,2) NULL,
        [Standard_Panel_Width_mm] decimal(10,2) NULL,
        [Dimensions] nvarchar(50) NULL,
        CONSTRAINT [PK_GratingSpecifications] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_GratingSpecifications_CatalogueItems_CatalogueItemId] FOREIGN KEY ([CatalogueItemId]) REFERENCES [CatalogueItems] ([Id]) ON DELETE CASCADE
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250922020049_UpdateCustomerAndContactTables'
)
BEGIN
    CREATE TABLE [InventoryItems] (
        [Id] int NOT NULL IDENTITY,
        [CatalogueItemId] int NOT NULL,
        [InventoryCode] nvarchar(50) NOT NULL,
        [WarehouseCode] nvarchar(50) NULL,
        [Location] nvarchar(50) NULL,
        [BinLocation] nvarchar(50) NULL,
        [QuantityOnHand] decimal(18,4) NOT NULL,
        [QuantityReserved] decimal(18,4) NULL,
        [QuantityAvailable] decimal(18,4) NULL,
        [Unit] nvarchar(20) NOT NULL,
        [LotNumber] nvarchar(50) NULL,
        [HeatNumber] nvarchar(50) NULL,
        [MillCertificate] nvarchar(100) NULL,
        [Supplier] nvarchar(100) NULL,
        [ReceivedDate] datetime2 NULL,
        [ExpiryDate] datetime2 NULL,
        [UnitCost] decimal(18,4) NULL,
        [TotalCost] decimal(18,4) NULL,
        [IsActive] bit NOT NULL,
        [CreatedDate] datetime2 NOT NULL DEFAULT (getutcdate()),
        [ModifiedDate] datetime2 NULL,
        [CompanyId] int NOT NULL,
        CONSTRAINT [PK_InventoryItems] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_InventoryItems_CatalogueItems_CatalogueItemId] FOREIGN KEY ([CatalogueItemId]) REFERENCES [CatalogueItems] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_InventoryItems_Companies_CompanyId] FOREIGN KEY ([CompanyId]) REFERENCES [Companies] ([Id]) ON DELETE CASCADE
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250922020049_UpdateCustomerAndContactTables'
)
BEGIN
    CREATE TABLE [AuthAuditLogs] (
        [Id] int NOT NULL IDENTITY,
        [UserId] int NULL,
        [Email] nvarchar(100) NOT NULL,
        [Action] nvarchar(50) NOT NULL,
        [AuthMethod] nvarchar(50) NOT NULL,
        [Success] bit NOT NULL,
        [ErrorMessage] nvarchar(500) NULL,
        [IpAddress] nvarchar(45) NULL,
        [UserAgent] nvarchar(500) NULL,
        [Timestamp] datetime2 NOT NULL DEFAULT (getutcdate()),
        [SessionId] nvarchar(100) NULL,
        CONSTRAINT [PK_AuthAuditLogs] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_AuthAuditLogs_Users_UserId] FOREIGN KEY ([UserId]) REFERENCES [Users] ([Id]) ON DELETE SET NULL
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250922020049_UpdateCustomerAndContactTables'
)
BEGIN
    CREATE TABLE [RefreshTokens] (
        [Id] int NOT NULL IDENTITY,
        [UserId] int NOT NULL,
        [Token] nvarchar(500) NOT NULL,
        [CreatedAt] datetime2 NOT NULL DEFAULT (getutcdate()),
        [ExpiresAt] datetime2 NOT NULL,
        [IsRevoked] bit NOT NULL,
        [DeviceInfo] nvarchar(50) NULL,
        [IpAddress] nvarchar(45) NULL,
        [UserAgent] nvarchar(500) NULL,
        CONSTRAINT [PK_RefreshTokens] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_RefreshTokens_Users_UserId] FOREIGN KEY ([UserId]) REFERENCES [Users] ([Id]) ON DELETE CASCADE
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250922020049_UpdateCustomerAndContactTables'
)
BEGIN
    CREATE TABLE [UserAuthMethods] (
        [Id] int NOT NULL IDENTITY,
        [UserId] int NOT NULL,
        [Provider] nvarchar(50) NOT NULL,
        [ExternalId] nvarchar(200) NULL,
        [ExternalEmail] nvarchar(200) NULL,
        [IsActive] bit NOT NULL,
        [CreatedAt] datetime2 NOT NULL DEFAULT (getutcdate()),
        [LastUsedAt] datetime2 NULL,
        CONSTRAINT [PK_UserAuthMethods] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_UserAuthMethods_Users_UserId] FOREIGN KEY ([UserId]) REFERENCES [Users] ([Id]) ON DELETE CASCADE
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250922020049_UpdateCustomerAndContactTables'
)
BEGIN
    CREATE TABLE [MachineCenters] (
        [Id] int NOT NULL IDENTITY,
        [MachineCode] nvarchar(50) NOT NULL,
        [MachineName] nvarchar(200) NOT NULL,
        [Description] nvarchar(500) NULL,
        [WorkCenterId] int NOT NULL,
        [CompanyId] int NOT NULL,
        [Manufacturer] nvarchar(100) NULL,
        [Model] nvarchar(100) NULL,
        [SerialNumber] nvarchar(50) NULL,
        [PurchaseDate] datetime2 NULL,
        [PurchasePrice] decimal(12,2) NULL,
        [MachineType] nvarchar(50) NOT NULL,
        [MachineSubType] nvarchar(100) NULL,
        [MaxCapacity] decimal(10,2) NOT NULL,
        [CapacityUnit] nvarchar(20) NULL,
        [SetupTimeMinutes] decimal(10,2) NOT NULL,
        [WarmupTimeMinutes] decimal(10,2) NOT NULL,
        [CooldownTimeMinutes] decimal(10,2) NOT NULL,
        [HourlyRate] decimal(10,2) NOT NULL,
        [PowerConsumptionKwh] decimal(10,2) NOT NULL,
        [PowerCostPerKwh] decimal(10,2) NOT NULL,
        [EfficiencyPercentage] decimal(5,2) NOT NULL,
        [QualityRate] decimal(5,2) NOT NULL,
        [AvailabilityRate] decimal(5,2) NOT NULL,
        [IsActive] bit NOT NULL,
        [IsDeleted] bit NOT NULL,
        [CurrentStatus] nvarchar(50) NOT NULL,
        [LastMaintenanceDate] datetime2 NULL,
        [NextMaintenanceDate] datetime2 NULL,
        [MaintenanceIntervalHours] int NOT NULL,
        [CurrentOperatingHours] int NOT NULL,
        [RequiresTooling] bit NOT NULL,
        [ToolingRequirements] nvarchar(500) NULL,
        [CreatedDate] datetime2 NOT NULL,
        [CreatedByUserId] int NULL,
        [LastModified] datetime2 NOT NULL,
        [LastModifiedByUserId] int NULL,
        CONSTRAINT [PK_MachineCenters] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_MachineCenters_Companies_CompanyId] FOREIGN KEY ([CompanyId]) REFERENCES [Companies] ([Id]) ON DELETE CASCADE,
        CONSTRAINT [FK_MachineCenters_Users_CreatedByUserId] FOREIGN KEY ([CreatedByUserId]) REFERENCES [Users] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_MachineCenters_Users_LastModifiedByUserId] FOREIGN KEY ([LastModifiedByUserId]) REFERENCES [Users] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_MachineCenters_WorkCenters_WorkCenterId] FOREIGN KEY ([WorkCenterId]) REFERENCES [WorkCenters] ([Id]) ON DELETE CASCADE
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250922020049_UpdateCustomerAndContactTables'
)
BEGIN
    CREATE TABLE [Resources] (
        [Id] int NOT NULL IDENTITY,
        [EmployeeCode] nvarchar(50) NOT NULL,
        [UserId] int NULL,
        [FirstName] nvarchar(100) NOT NULL,
        [LastName] nvarchar(100) NOT NULL,
        [JobTitle] nvarchar(200) NOT NULL,
        [ResourceType] nvarchar(20) NOT NULL,
        [PrimarySkill] nvarchar(100) NULL,
        [SkillLevel] int NOT NULL,
        [CertificationLevel] nvarchar(50) NULL,
        [StandardHoursPerDay] decimal(4,2) NOT NULL,
        [HourlyRate] decimal(10,2) NOT NULL,
        [IsActive] bit NOT NULL,
        [PrimaryWorkCenterId] int NULL,
        [CreatedDate] datetime2 NOT NULL,
        [LastModified] datetime2 NOT NULL,
        CONSTRAINT [PK_Resources] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_Resources_Users_UserId] FOREIGN KEY ([UserId]) REFERENCES [Users] ([Id]),
        CONSTRAINT [FK_Resources_WorkCenters_PrimaryWorkCenterId] FOREIGN KEY ([PrimaryWorkCenterId]) REFERENCES [WorkCenters] ([Id])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250922020049_UpdateCustomerAndContactTables'
)
BEGIN
    CREATE TABLE [WorkCenterShifts] (
        [Id] int NOT NULL IDENTITY,
        [WorkCenterId] int NOT NULL,
        [ShiftName] nvarchar(50) NOT NULL,
        [StartTime] time NOT NULL,
        [EndTime] time NOT NULL,
        [IsActive] bit NOT NULL,
        [CreatedDate] datetime2 NOT NULL,
        [LastModified] datetime2 NOT NULL,
        CONSTRAINT [PK_WorkCenterShifts] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_WorkCenterShifts_WorkCenters_WorkCenterId] FOREIGN KEY ([WorkCenterId]) REFERENCES [WorkCenters] ([Id]) ON DELETE CASCADE
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250922020049_UpdateCustomerAndContactTables'
)
BEGIN
    CREATE TABLE [InventoryTransactions] (
        [Id] int NOT NULL IDENTITY,
        [InventoryItemId] int NOT NULL,
        [TransactionNumber] nvarchar(50) NOT NULL,
        [TransactionType] nvarchar(50) NOT NULL,
        [Quantity] decimal(18,4) NOT NULL,
        [Unit] nvarchar(20) NOT NULL,
        [Reference] nvarchar(100) NULL,
        [Notes] nvarchar(500) NULL,
        [TransactionDate] datetime2 NOT NULL DEFAULT (getutcdate()),
        [UserId] int NULL,
        [CompanyId] int NOT NULL,
        CONSTRAINT [PK_InventoryTransactions] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_InventoryTransactions_Companies_CompanyId] FOREIGN KEY ([CompanyId]) REFERENCES [Companies] ([Id]) ON DELETE CASCADE,
        CONSTRAINT [FK_InventoryTransactions_InventoryItems_InventoryItemId] FOREIGN KEY ([InventoryItemId]) REFERENCES [InventoryItems] ([Id]) ON DELETE CASCADE,
        CONSTRAINT [FK_InventoryTransactions_Users_UserId] FOREIGN KEY ([UserId]) REFERENCES [Users] ([Id]) ON DELETE NO ACTION
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250922020049_UpdateCustomerAndContactTables'
)
BEGIN
    CREATE TABLE [MachineCapabilities] (
        [Id] int NOT NULL IDENTITY,
        [MachineCenterId] int NOT NULL,
        [CapabilityName] nvarchar(100) NOT NULL,
        [Description] nvarchar(500) NULL,
        [MinValue] decimal(10,2) NULL,
        [MaxValue] decimal(10,2) NULL,
        [Units] nvarchar(20) NULL,
        [IsActive] bit NOT NULL,
        [CreatedDate] datetime2 NOT NULL,
        [LastModified] datetime2 NOT NULL,
        CONSTRAINT [PK_MachineCapabilities] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_MachineCapabilities_MachineCenters_MachineCenterId] FOREIGN KEY ([MachineCenterId]) REFERENCES [MachineCenters] ([Id]) ON DELETE CASCADE
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250922020049_UpdateCustomerAndContactTables'
)
BEGIN
    CREATE TABLE [MachineOperators] (
        [Id] int NOT NULL IDENTITY,
        [MachineCenterId] int NOT NULL,
        [UserId] int NOT NULL,
        [SkillLevel] nvarchar(50) NOT NULL,
        [EfficiencyRating] decimal(5,2) NULL,
        [CertificationDate] datetime2 NOT NULL,
        [CertificationExpiryDate] datetime2 NULL,
        [IsActive] bit NOT NULL,
        [CreatedDate] datetime2 NOT NULL,
        [LastModified] datetime2 NOT NULL,
        CONSTRAINT [PK_MachineOperators] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_MachineOperators_MachineCenters_MachineCenterId] FOREIGN KEY ([MachineCenterId]) REFERENCES [MachineCenters] ([Id]) ON DELETE CASCADE,
        CONSTRAINT [FK_MachineOperators_Users_UserId] FOREIGN KEY ([UserId]) REFERENCES [Users] ([Id]) ON DELETE CASCADE
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250922020049_UpdateCustomerAndContactTables'
)
BEGIN
    CREATE TABLE [EstimationPackages] (
        [Id] int NOT NULL IDENTITY,
        [EstimationId] int NOT NULL,
        [PackageName] nvarchar(200) NOT NULL,
        [Description] nvarchar(1000) NULL,
        [SequenceNumber] int NOT NULL,
        [MaterialCost] decimal(18,2) NOT NULL,
        [LaborHours] decimal(18,2) NOT NULL,
        [LaborCost] decimal(18,2) NOT NULL,
        [PackageTotal] decimal(18,2) NOT NULL,
        [PlannedStartDate] datetime2 NULL,
        [PlannedEndDate] datetime2 NULL,
        [EstimatedDuration] int NULL,
        CONSTRAINT [PK_EstimationPackages] PRIMARY KEY ([Id])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250922020049_UpdateCustomerAndContactTables'
)
BEGIN
    CREATE TABLE [Estimations] (
        [Id] int NOT NULL IDENTITY,
        [EstimationNumber] nvarchar(50) NOT NULL,
        [CustomerId] int NOT NULL,
        [ProjectName] nvarchar(200) NOT NULL,
        [EstimationDate] datetime2 NOT NULL,
        [ValidUntil] datetime2 NOT NULL,
        [RevisionNumber] int NOT NULL,
        [TotalMaterialCost] decimal(18,2) NOT NULL,
        [TotalLaborHours] decimal(18,2) NOT NULL,
        [TotalLaborCost] decimal(18,2) NOT NULL,
        [OverheadPercentage] decimal(5,2) NOT NULL,
        [OverheadAmount] decimal(18,2) NOT NULL,
        [MarginPercentage] decimal(5,2) NOT NULL,
        [MarginAmount] decimal(18,2) NOT NULL,
        [TotalAmount] decimal(18,2) NOT NULL,
        [Status] nvarchar(20) NOT NULL DEFAULT N'Draft',
        [ApprovedDate] datetime2 NULL,
        [ApprovedBy] int NULL,
        [CreatedDate] datetime2 NOT NULL DEFAULT (getutcdate()),
        [CreatedBy] int NOT NULL,
        [LastModified] datetime2 NOT NULL,
        [LastModifiedBy] int NOT NULL,
        [OrderId] int NULL,
        CONSTRAINT [PK_Estimations] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_Estimations_Customers_CustomerId] FOREIGN KEY ([CustomerId]) REFERENCES [Customers] ([Id]) ON DELETE CASCADE,
        CONSTRAINT [FK_Estimations_Users_ApprovedBy] FOREIGN KEY ([ApprovedBy]) REFERENCES [Users] ([Id]),
        CONSTRAINT [FK_Estimations_Users_CreatedBy] FOREIGN KEY ([CreatedBy]) REFERENCES [Users] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_Estimations_Users_LastModifiedBy] FOREIGN KEY ([LastModifiedBy]) REFERENCES [Users] ([Id]) ON DELETE CASCADE
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250922020049_UpdateCustomerAndContactTables'
)
BEGIN
    CREATE TABLE [Orders] (
        [Id] int NOT NULL IDENTITY,
        [OrderNumber] nvarchar(50) NOT NULL,
        [CustomerId] int NOT NULL,
        [Source] nvarchar(20) NOT NULL,
        [QuoteId] int NULL,
        [EstimationId] int NULL,
        [CustomerPONumber] nvarchar(100) NULL,
        [CustomerReference] nvarchar(200) NULL,
        [TotalValue] decimal(18,2) NOT NULL,
        [PaymentTerms] nvarchar(100) NOT NULL,
        [OrderDate] datetime2 NOT NULL DEFAULT (getutcdate()),
        [RequiredDate] datetime2 NULL,
        [PromisedDate] datetime2 NULL,
        [Status] nvarchar(20) NOT NULL DEFAULT N'Pending',
        [CreatedDate] datetime2 NOT NULL,
        [CreatedBy] int NOT NULL,
        [LastModified] datetime2 NOT NULL,
        [LastModifiedBy] int NOT NULL,
        CONSTRAINT [PK_Orders] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_Orders_Customers_CustomerId] FOREIGN KEY ([CustomerId]) REFERENCES [Customers] ([Id]) ON DELETE CASCADE,
        CONSTRAINT [FK_Orders_Estimations_EstimationId] FOREIGN KEY ([EstimationId]) REFERENCES [Estimations] ([Id]),
        CONSTRAINT [FK_Orders_Users_CreatedBy] FOREIGN KEY ([CreatedBy]) REFERENCES [Users] ([Id]) ON DELETE CASCADE,
        CONSTRAINT [FK_Orders_Users_LastModifiedBy] FOREIGN KEY ([LastModifiedBy]) REFERENCES [Users] ([Id]) ON DELETE CASCADE
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250922020049_UpdateCustomerAndContactTables'
)
BEGIN
    CREATE TABLE [Projects] (
        [Id] int NOT NULL IDENTITY,
        [ProjectName] nvarchar(200) NOT NULL,
        [JobNumber] nvarchar(50) NOT NULL,
        [CustomerName] nvarchar(200) NULL,
        [ProjectLocation] nvarchar(200) NULL,
        [EstimationStage] nvarchar(20) NOT NULL,
        [LaborRate] decimal(10,2) NOT NULL,
        [ContingencyPercentage] decimal(5,2) NOT NULL,
        [Notes] nvarchar(max) NULL,
        [OwnerId] int NULL,
        [LastModifiedBy] int NULL,
        [CreatedDate] datetime2 NOT NULL,
        [LastModified] datetime2 NOT NULL,
        [IsDeleted] bit NOT NULL,
        [Description] nvarchar(max) NULL,
        [EstimatedHours] decimal(18,2) NULL,
        [EstimatedCompletionDate] datetime2 NULL,
        [CustomerId] int NULL,
        [OrderId] int NULL,
        [ProjectType] nvarchar(20) NOT NULL,
        CONSTRAINT [PK_Projects] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_Projects_Customers_CustomerId] FOREIGN KEY ([CustomerId]) REFERENCES [Customers] ([Id]),
        CONSTRAINT [FK_Projects_Orders_OrderId] FOREIGN KEY ([OrderId]) REFERENCES [Orders] ([Id]),
        CONSTRAINT [FK_Projects_Users_LastModifiedBy] FOREIGN KEY ([LastModifiedBy]) REFERENCES [Users] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_Projects_Users_OwnerId] FOREIGN KEY ([OwnerId]) REFERENCES [Users] ([Id]) ON DELETE NO ACTION
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250922020049_UpdateCustomerAndContactTables'
)
BEGIN
    CREATE TABLE [Quotes] (
        [Id] int NOT NULL IDENTITY,
        [QuoteNumber] nvarchar(50) NOT NULL,
        [CustomerId] int NOT NULL,
        [QuoteDate] datetime2 NOT NULL DEFAULT (getutcdate()),
        [ValidUntil] datetime2 NOT NULL DEFAULT (dateadd(day, 30, getutcdate())),
        [Description] nvarchar(1000) NOT NULL,
        [MaterialCost] decimal(18,2) NOT NULL,
        [LaborHours] decimal(10,2) NOT NULL,
        [LaborRate] decimal(10,2) NOT NULL,
        [OverheadPercentage] decimal(5,2) NOT NULL,
        [MarginPercentage] decimal(5,2) NOT NULL,
        [TotalAmount] decimal(18,2) NOT NULL,
        [Status] nvarchar(20) NOT NULL DEFAULT N'Draft',
        [CreatedDate] datetime2 NOT NULL,
        [CreatedBy] int NULL,
        [LastModified] datetime2 NOT NULL,
        [LastModifiedBy] int NULL,
        [OrderId] int NULL,
        CONSTRAINT [PK_Quotes] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_Quotes_Customers_CustomerId] FOREIGN KEY ([CustomerId]) REFERENCES [Customers] ([Id]) ON DELETE CASCADE,
        CONSTRAINT [FK_Quotes_Orders_OrderId] FOREIGN KEY ([OrderId]) REFERENCES [Orders] ([Id]),
        CONSTRAINT [FK_Quotes_Users_CreatedBy] FOREIGN KEY ([CreatedBy]) REFERENCES [Users] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_Quotes_Users_LastModifiedBy] FOREIGN KEY ([LastModifiedBy]) REFERENCES [Users] ([Id])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250922020049_UpdateCustomerAndContactTables'
)
BEGIN
    CREATE TABLE [Packages] (
        [Id] int NOT NULL IDENTITY,
        [ProjectId] int NOT NULL,
        [OrderId] int NULL,
        [PackageSource] nvarchar(20) NOT NULL DEFAULT N'Project',
        [PackageNumber] nvarchar(50) NOT NULL,
        [PackageName] nvarchar(200) NOT NULL,
        [Description] nvarchar(500) NULL,
        [Status] nvarchar(50) NOT NULL,
        [StartDate] datetime2 NULL,
        [EndDate] datetime2 NULL,
        [EstimatedHours] decimal(10,2) NOT NULL,
        [EstimatedCost] decimal(18,2) NOT NULL,
        [ActualHours] decimal(10,2) NOT NULL,
        [ActualCost] decimal(18,2) NOT NULL,
        [CreatedBy] int NULL,
        [LastModifiedBy] int NULL,
        [CreatedDate] datetime2 NOT NULL,
        [LastModified] datetime2 NOT NULL,
        [IsDeleted] bit NOT NULL,
        [LaborRatePerHour] decimal(10,2) NOT NULL,
        [ProcessingEfficiency] decimal(18,2) NULL,
        [EfficiencyRateId] int NULL,
        [RoutingId] int NULL,
        CONSTRAINT [PK_Packages] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_Packages_EfficiencyRates_EfficiencyRateId] FOREIGN KEY ([EfficiencyRateId]) REFERENCES [EfficiencyRates] ([Id]),
        CONSTRAINT [FK_Packages_Orders_OrderId] FOREIGN KEY ([OrderId]) REFERENCES [Orders] ([Id]),
        CONSTRAINT [FK_Packages_Projects_ProjectId] FOREIGN KEY ([ProjectId]) REFERENCES [Projects] ([Id]) ON DELETE CASCADE,
        CONSTRAINT [FK_Packages_RoutingTemplates_RoutingId] FOREIGN KEY ([RoutingId]) REFERENCES [RoutingTemplates] ([Id]),
        CONSTRAINT [FK_Packages_Users_CreatedBy] FOREIGN KEY ([CreatedBy]) REFERENCES [Users] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_Packages_Users_LastModifiedBy] FOREIGN KEY ([LastModifiedBy]) REFERENCES [Users] ([Id]) ON DELETE NO ACTION
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250922020049_UpdateCustomerAndContactTables'
)
BEGIN
    CREATE TABLE [QuoteLineItems] (
        [Id] int NOT NULL IDENTITY,
        [QuoteId] int NOT NULL,
        [LineNumber] int NOT NULL,
        [ItemDescription] nvarchar(500) NOT NULL,
        [Quantity] decimal(18,4) NOT NULL,
        [Unit] nvarchar(20) NOT NULL,
        [UnitPrice] decimal(18,4) NOT NULL,
        [LineTotal] decimal(18,2) NOT NULL,
        [CatalogueItemId] int NULL,
        [Notes] nvarchar(1000) NULL,
        CONSTRAINT [PK_QuoteLineItems] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_QuoteLineItems_Quotes_QuoteId] FOREIGN KEY ([QuoteId]) REFERENCES [Quotes] ([Id]) ON DELETE CASCADE
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250922020049_UpdateCustomerAndContactTables'
)
BEGIN
    CREATE TABLE [TraceDrawings] (
        [Id] int NOT NULL IDENTITY,
        [CompanyId] int NOT NULL,
        [ProjectId] int NOT NULL,
        [PackageId] int NULL,
        [DrawingNumber] nvarchar(100) NULL,
        [FileName] nvarchar(255) NOT NULL,
        [FileType] nvarchar(50) NOT NULL,
        [BlobUrl] nvarchar(500) NOT NULL,
        [ThumbnailUrl] nvarchar(500) NULL,
        [UploadDate] datetime2 NOT NULL DEFAULT (getutcdate()),
        [UploadedBy] int NOT NULL,
        [PageCount] int NULL,
        [ProcessingStatus] nvarchar(50) NOT NULL DEFAULT N'Pending',
        [Scale] decimal(10,4) NULL,
        [ScaleUnit] nvarchar(20) NULL DEFAULT N'mm',
        [CalibrationData] nvarchar(max) NULL,
        [DrawingTitle] nvarchar(200) NULL,
        [DrawingType] nvarchar(50) NULL,
        [Discipline] nvarchar(50) NULL,
        [Revision] nvarchar(20) NULL,
        [CreatedDate] datetime2 NOT NULL,
        [LastModified] datetime2 NOT NULL,
        [IsDeleted] bit NOT NULL,
        [OCRStatus] nvarchar(50) NULL DEFAULT N'NotProcessed',
        [OCRProcessedDate] datetime2 NULL,
        [OCRResultId] int NULL,
        [CustomerId] int NULL,
        [TraceName] nvarchar(200) NULL,
        [ProjectName] nvarchar(200) NULL,
        [ClientName] nvarchar(200) NULL,
        [Status] nvarchar(50) NULL,
        [OcrConfidence] int NULL,
        [ProjectNumber] nvarchar(50) NULL,
        CONSTRAINT [PK_TraceDrawings] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_TraceDrawings_Companies_CompanyId] FOREIGN KEY ([CompanyId]) REFERENCES [Companies] ([Id]) ON DELETE CASCADE,
        CONSTRAINT [FK_TraceDrawings_Customers_CustomerId] FOREIGN KEY ([CustomerId]) REFERENCES [Customers] ([Id]),
        CONSTRAINT [FK_TraceDrawings_Packages_PackageId] FOREIGN KEY ([PackageId]) REFERENCES [Packages] ([Id]),
        CONSTRAINT [FK_TraceDrawings_Projects_ProjectId] FOREIGN KEY ([ProjectId]) REFERENCES [Projects] ([Id]) ON DELETE CASCADE,
        CONSTRAINT [FK_TraceDrawings_Users_UploadedBy] FOREIGN KEY ([UploadedBy]) REFERENCES [Users] ([Id]) ON DELETE CASCADE
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250922020049_UpdateCustomerAndContactTables'
)
BEGIN
    CREATE TABLE [WeldingConnections] (
        [Id] int NOT NULL IDENTITY,
        [Name] nvarchar(200) NOT NULL,
        [Category] nvarchar(100) NOT NULL,
        [DefaultAssembleFitTack] decimal(18,2) NOT NULL,
        [DefaultWeld] decimal(18,2) NOT NULL,
        [DefaultWeldCheck] decimal(18,2) NOT NULL,
        [IsActive] bit NOT NULL,
        [DisplayOrder] int NOT NULL,
        [CreatedDate] datetime2 NOT NULL,
        [ModifiedDate] datetime2 NULL,
        [DefaultWeldTest] decimal(10,2) NOT NULL,
        [LastModified] datetime2 NOT NULL,
        [PackageId] int NULL,
        [Size] nvarchar(20) NOT NULL,
        CONSTRAINT [PK_WeldingConnections] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_WeldingConnections_Packages_PackageId] FOREIGN KEY ([PackageId]) REFERENCES [Packages] ([Id])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250922020049_UpdateCustomerAndContactTables'
)
BEGIN
    CREATE TABLE [WorkOrders] (
        [Id] int NOT NULL IDENTITY,
        [WorkOrderNumber] nvarchar(50) NOT NULL,
        [PackageId] int NOT NULL,
        [WorkOrderType] nvarchar(20) NOT NULL,
        [Description] nvarchar(1000) NULL,
        [WorkCenterId] int NULL,
        [PrimaryResourceId] int NULL,
        [Priority] nvarchar(10) NOT NULL,
        [ScheduledStartDate] datetime2 NULL,
        [ScheduledEndDate] datetime2 NULL,
        [ActualStartDate] datetime2 NULL,
        [ActualEndDate] datetime2 NULL,
        [EstimatedHours] decimal(10,2) NOT NULL,
        [ActualHours] decimal(10,2) NOT NULL,
        [Status] nvarchar(20) NOT NULL DEFAULT N'Planning',
        [Barcode] nvarchar(100) NULL,
        [HasHoldPoints] bit NOT NULL,
        [RequiresInspection] bit NOT NULL,
        [CreatedDate] datetime2 NOT NULL DEFAULT (getutcdate()),
        [CreatedBy] int NOT NULL,
        [LastModified] datetime2 NOT NULL,
        [LastModifiedBy] int NOT NULL,
        CONSTRAINT [PK_WorkOrders] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_WorkOrders_Packages_PackageId] FOREIGN KEY ([PackageId]) REFERENCES [Packages] ([Id]) ON DELETE CASCADE,
        CONSTRAINT [FK_WorkOrders_Resources_PrimaryResourceId] FOREIGN KEY ([PrimaryResourceId]) REFERENCES [Resources] ([Id]),
        CONSTRAINT [FK_WorkOrders_Users_CreatedBy] FOREIGN KEY ([CreatedBy]) REFERENCES [Users] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_WorkOrders_Users_LastModifiedBy] FOREIGN KEY ([LastModifiedBy]) REFERENCES [Users] ([Id]) ON DELETE CASCADE,
        CONSTRAINT [FK_WorkOrders_WorkCenters_WorkCenterId] FOREIGN KEY ([WorkCenterId]) REFERENCES [WorkCenters] ([Id])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250922020049_UpdateCustomerAndContactTables'
)
BEGIN
    CREATE TABLE [TraceBeamDetections] (
        [Id] int NOT NULL IDENTITY,
        [TraceDrawingId] int NOT NULL,
        [BeamType] nvarchar(100) NULL,
        [BeamSize] nvarchar(50) NULL,
        [Length] decimal(10,4) NULL,
        [Weight] decimal(10,4) NULL,
        [CreatedDate] datetime2 NOT NULL,
        [LastModified] datetime2 NOT NULL,
        CONSTRAINT [PK_TraceBeamDetections] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_TraceBeamDetections_TraceDrawings_TraceDrawingId] FOREIGN KEY ([TraceDrawingId]) REFERENCES [TraceDrawings] ([Id]) ON DELETE CASCADE
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250922020049_UpdateCustomerAndContactTables'
)
BEGIN
    CREATE TABLE [TraceMeasurements] (
        [Id] int NOT NULL IDENTITY,
        [TraceDrawingId] int NOT NULL,
        [ElementType] nvarchar(100) NULL,
        [Length] decimal(10,4) NULL,
        [Width] decimal(10,4) NULL,
        [Height] decimal(10,4) NULL,
        [Units] nvarchar(50) NULL,
        [CreatedDate] datetime2 NOT NULL,
        [LastModified] datetime2 NOT NULL,
        CONSTRAINT [PK_TraceMeasurements] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_TraceMeasurements_TraceDrawings_TraceDrawingId] FOREIGN KEY ([TraceDrawingId]) REFERENCES [TraceDrawings] ([Id]) ON DELETE CASCADE
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250922020049_UpdateCustomerAndContactTables'
)
BEGIN
    CREATE TABLE [TraceTakeoffItems] (
        [Id] int NOT NULL IDENTITY,
        [TraceDrawingId] int NOT NULL,
        [ItemType] nvarchar(100) NULL,
        [Description] nvarchar(200) NULL,
        [Quantity] decimal(10,2) NULL,
        [Units] nvarchar(20) NULL,
        [CreatedDate] datetime2 NOT NULL,
        [LastModified] datetime2 NOT NULL,
        CONSTRAINT [PK_TraceTakeoffItems] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_TraceTakeoffItems_TraceDrawings_TraceDrawingId] FOREIGN KEY ([TraceDrawingId]) REFERENCES [TraceDrawings] ([Id]) ON DELETE CASCADE
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250922020049_UpdateCustomerAndContactTables'
)
BEGIN
    CREATE TABLE [WorkOrderAssembly] (
        [Id] int NOT NULL IDENTITY,
        [WorkOrderId] int NOT NULL,
        [PackageAssemblyId] int NOT NULL,
        [AssemblyId] int NOT NULL,
        [QuantityToBuild] int NOT NULL,
        [QuantityCompleted] int NOT NULL,
        [Status] nvarchar(20) NOT NULL,
        CONSTRAINT [PK_WorkOrderAssembly] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_WorkOrderAssembly_WorkOrders_WorkOrderId] FOREIGN KEY ([WorkOrderId]) REFERENCES [WorkOrders] ([Id]) ON DELETE CASCADE
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250922020049_UpdateCustomerAndContactTables'
)
BEGIN
    CREATE TABLE [WorkOrderInventoryItem] (
        [Id] int NOT NULL IDENTITY,
        [WorkOrderId] int NOT NULL,
        [PackageItemId] int NOT NULL,
        [CatalogueItemId] int NOT NULL,
        [RequiredQuantity] decimal(18,4) NOT NULL,
        [IssuedQuantity] decimal(18,4) NOT NULL,
        [ProcessedQuantity] decimal(18,4) NOT NULL,
        [Unit] nvarchar(20) NOT NULL,
        [RequiredOperations] nvarchar(200) NOT NULL,
        [Status] nvarchar(20) NOT NULL,
        [HeatNumber] nvarchar(100) NULL,
        [Certificate] nvarchar(200) NULL,
        [InventoryItemId] int NULL,
        CONSTRAINT [PK_WorkOrderInventoryItem] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_WorkOrderInventoryItem_WorkOrders_WorkOrderId] FOREIGN KEY ([WorkOrderId]) REFERENCES [WorkOrders] ([Id]) ON DELETE CASCADE
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250922020049_UpdateCustomerAndContactTables'
)
BEGIN
    CREATE TABLE [WorkOrderOperations] (
        [Id] int NOT NULL IDENTITY,
        [WorkOrderId] int NOT NULL,
        [SequenceNumber] int NOT NULL,
        [OperationCode] nvarchar(20) NOT NULL,
        [OperationName] nvarchar(200) NOT NULL,
        [Description] nvarchar(1000) NULL,
        [RequiredSkill] nvarchar(100) NULL,
        [RequiredSkillLevel] int NULL,
        [RequiredMachine] nvarchar(100) NULL,
        [RequiredTooling] nvarchar(500) NULL,
        [SetupTime] decimal(10,2) NOT NULL,
        [CycleTime] decimal(10,2) NOT NULL,
        [EstimatedHours] decimal(10,2) NOT NULL,
        [ActualHours] decimal(10,2) NOT NULL,
        [RequiresInspection] bit NOT NULL,
        [InspectionType] nvarchar(50) NULL,
        [LinkedITPPointId] int NULL,
        [Status] nvarchar(20) NOT NULL,
        [StartedAt] datetime2 NULL,
        [CompletedAt] datetime2 NULL,
        [CompletedBy] int NULL,
        CONSTRAINT [PK_WorkOrderOperations] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_WorkOrderOperations_Users_CompletedBy] FOREIGN KEY ([CompletedBy]) REFERENCES [Users] ([Id]),
        CONSTRAINT [FK_WorkOrderOperations_WorkOrders_WorkOrderId] FOREIGN KEY ([WorkOrderId]) REFERENCES [WorkOrders] ([Id]) ON DELETE CASCADE
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250922020049_UpdateCustomerAndContactTables'
)
BEGIN
    CREATE TABLE [WorkOrderResources] (
        [Id] int NOT NULL IDENTITY,
        [WorkOrderId] int NOT NULL,
        [ResourceId] int NOT NULL,
        [AssignmentType] nvarchar(20) NOT NULL,
        [EstimatedHours] decimal(10,2) NOT NULL,
        [ActualHours] decimal(10,2) NOT NULL,
        [AssignedDate] datetime2 NOT NULL,
        [StartedDate] datetime2 NULL,
        [CompletedDate] datetime2 NULL,
        CONSTRAINT [PK_WorkOrderResources] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_WorkOrderResources_Resources_ResourceId] FOREIGN KEY ([ResourceId]) REFERENCES [Resources] ([Id]) ON DELETE CASCADE,
        CONSTRAINT [FK_WorkOrderResources_WorkOrders_WorkOrderId] FOREIGN KEY ([WorkOrderId]) REFERENCES [WorkOrders] ([Id]) ON DELETE CASCADE
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250922020049_UpdateCustomerAndContactTables'
)
BEGIN
    CREATE UNIQUE INDEX [IX_Assemblies_AssemblyNumber] ON [Assemblies] ([AssemblyNumber]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250922020049_UpdateCustomerAndContactTables'
)
BEGIN
    CREATE INDEX [IX_Assemblies_CompanyId] ON [Assemblies] ([CompanyId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250922020049_UpdateCustomerAndContactTables'
)
BEGIN
    CREATE INDEX [IX_Assemblies_ParentAssemblyId] ON [Assemblies] ([ParentAssemblyId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250922020049_UpdateCustomerAndContactTables'
)
BEGIN
    CREATE INDEX [IX_AssemblyComponents_AssemblyId] ON [AssemblyComponents] ([AssemblyId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250922020049_UpdateCustomerAndContactTables'
)
BEGIN
    CREATE INDEX [IX_AssemblyComponents_CatalogueItemId] ON [AssemblyComponents] ([CatalogueItemId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250922020049_UpdateCustomerAndContactTables'
)
BEGIN
    CREATE INDEX [IX_AssemblyComponents_ComponentAssemblyId] ON [AssemblyComponents] ([ComponentAssemblyId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250922020049_UpdateCustomerAndContactTables'
)
BEGIN
    CREATE INDEX [IX_AuthAuditLogs_Email_Timestamp] ON [AuthAuditLogs] ([Email], [Timestamp]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250922020049_UpdateCustomerAndContactTables'
)
BEGIN
    CREATE INDEX [IX_AuthAuditLogs_Timestamp] ON [AuthAuditLogs] ([Timestamp]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250922020049_UpdateCustomerAndContactTables'
)
BEGIN
    CREATE INDEX [IX_AuthAuditLogs_UserId] ON [AuthAuditLogs] ([UserId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250922020049_UpdateCustomerAndContactTables'
)
BEGIN
    CREATE INDEX [IX_CatalogueItems_Category] ON [CatalogueItems] ([Category]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250922020049_UpdateCustomerAndContactTables'
)
BEGIN
    CREATE INDEX [IX_CatalogueItems_CompanyId] ON [CatalogueItems] ([CompanyId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250922020049_UpdateCustomerAndContactTables'
)
BEGIN
    CREATE INDEX [IX_CatalogueItems_Finish] ON [CatalogueItems] ([Finish]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250922020049_UpdateCustomerAndContactTables'
)
BEGIN
    CREATE INDEX [IX_CatalogueItems_Grade] ON [CatalogueItems] ([Grade]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250922020049_UpdateCustomerAndContactTables'
)
BEGIN
    CREATE UNIQUE INDEX [IX_CatalogueItems_ItemCode] ON [CatalogueItems] ([ItemCode]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250922020049_UpdateCustomerAndContactTables'
)
BEGIN
    CREATE INDEX [IX_CatalogueItems_Material] ON [CatalogueItems] ([Material]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250922020049_UpdateCustomerAndContactTables'
)
BEGIN
    CREATE INDEX [IX_CatalogueItems_Profile] ON [CatalogueItems] ([Profile]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250922020049_UpdateCustomerAndContactTables'
)
BEGIN
    CREATE UNIQUE INDEX [IX_Companies_Code] ON [Companies] ([Code]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250922020049_UpdateCustomerAndContactTables'
)
BEGIN
    CREATE INDEX [IX_CustomerAddresses_CustomerId] ON [CustomerAddresses] ([CustomerId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250922020049_UpdateCustomerAndContactTables'
)
BEGIN
    CREATE INDEX [IX_CustomerContacts_CustomerId] ON [CustomerContacts] ([CustomerId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250922020049_UpdateCustomerAndContactTables'
)
BEGIN
    CREATE INDEX [IX_EstimationPackages_EstimationId] ON [EstimationPackages] ([EstimationId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250922020049_UpdateCustomerAndContactTables'
)
BEGIN
    CREATE INDEX [IX_Estimations_ApprovedBy] ON [Estimations] ([ApprovedBy]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250922020049_UpdateCustomerAndContactTables'
)
BEGIN
    CREATE INDEX [IX_Estimations_CreatedBy] ON [Estimations] ([CreatedBy]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250922020049_UpdateCustomerAndContactTables'
)
BEGIN
    CREATE INDEX [IX_Estimations_CustomerId] ON [Estimations] ([CustomerId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250922020049_UpdateCustomerAndContactTables'
)
BEGIN
    CREATE INDEX [IX_Estimations_LastModifiedBy] ON [Estimations] ([LastModifiedBy]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250922020049_UpdateCustomerAndContactTables'
)
BEGIN
    CREATE INDEX [IX_Estimations_OrderId] ON [Estimations] ([OrderId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250922020049_UpdateCustomerAndContactTables'
)
BEGIN
    CREATE UNIQUE INDEX [IX_GratingSpecifications_CatalogueItemId] ON [GratingSpecifications] ([CatalogueItemId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250922020049_UpdateCustomerAndContactTables'
)
BEGIN
    CREATE INDEX [IX_InventoryItems_CatalogueItemId] ON [InventoryItems] ([CatalogueItemId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250922020049_UpdateCustomerAndContactTables'
)
BEGIN
    CREATE INDEX [IX_InventoryItems_CompanyId] ON [InventoryItems] ([CompanyId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250922020049_UpdateCustomerAndContactTables'
)
BEGIN
    CREATE INDEX [IX_InventoryItems_HeatNumber] ON [InventoryItems] ([HeatNumber]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250922020049_UpdateCustomerAndContactTables'
)
BEGIN
    CREATE UNIQUE INDEX [IX_InventoryItems_InventoryCode] ON [InventoryItems] ([InventoryCode]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250922020049_UpdateCustomerAndContactTables'
)
BEGIN
    CREATE INDEX [IX_InventoryItems_LotNumber] ON [InventoryItems] ([LotNumber]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250922020049_UpdateCustomerAndContactTables'
)
BEGIN
    CREATE INDEX [IX_InventoryTransactions_CompanyId] ON [InventoryTransactions] ([CompanyId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250922020049_UpdateCustomerAndContactTables'
)
BEGIN
    CREATE INDEX [IX_InventoryTransactions_InventoryItemId] ON [InventoryTransactions] ([InventoryItemId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250922020049_UpdateCustomerAndContactTables'
)
BEGIN
    CREATE UNIQUE INDEX [IX_InventoryTransactions_TransactionNumber] ON [InventoryTransactions] ([TransactionNumber]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250922020049_UpdateCustomerAndContactTables'
)
BEGIN
    CREATE INDEX [IX_InventoryTransactions_UserId] ON [InventoryTransactions] ([UserId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250922020049_UpdateCustomerAndContactTables'
)
BEGIN
    CREATE INDEX [IX_MachineCapabilities_MachineCenterId] ON [MachineCapabilities] ([MachineCenterId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250922020049_UpdateCustomerAndContactTables'
)
BEGIN
    CREATE INDEX [IX_MachineCenters_CompanyId] ON [MachineCenters] ([CompanyId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250922020049_UpdateCustomerAndContactTables'
)
BEGIN
    CREATE INDEX [IX_MachineCenters_CreatedByUserId] ON [MachineCenters] ([CreatedByUserId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250922020049_UpdateCustomerAndContactTables'
)
BEGIN
    CREATE INDEX [IX_MachineCenters_LastModifiedByUserId] ON [MachineCenters] ([LastModifiedByUserId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250922020049_UpdateCustomerAndContactTables'
)
BEGIN
    CREATE UNIQUE INDEX [IX_MachineCenters_MachineCode] ON [MachineCenters] ([MachineCode]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250922020049_UpdateCustomerAndContactTables'
)
BEGIN
    CREATE INDEX [IX_MachineCenters_WorkCenterId] ON [MachineCenters] ([WorkCenterId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250922020049_UpdateCustomerAndContactTables'
)
BEGIN
    CREATE INDEX [IX_MachineOperators_MachineCenterId] ON [MachineOperators] ([MachineCenterId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250922020049_UpdateCustomerAndContactTables'
)
BEGIN
    CREATE INDEX [IX_MachineOperators_UserId] ON [MachineOperators] ([UserId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250922020049_UpdateCustomerAndContactTables'
)
BEGIN
    CREATE INDEX [IX_Orders_CreatedBy] ON [Orders] ([CreatedBy]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250922020049_UpdateCustomerAndContactTables'
)
BEGIN
    CREATE INDEX [IX_Orders_CustomerId] ON [Orders] ([CustomerId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250922020049_UpdateCustomerAndContactTables'
)
BEGIN
    CREATE INDEX [IX_Orders_EstimationId] ON [Orders] ([EstimationId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250922020049_UpdateCustomerAndContactTables'
)
BEGIN
    CREATE INDEX [IX_Orders_LastModifiedBy] ON [Orders] ([LastModifiedBy]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250922020049_UpdateCustomerAndContactTables'
)
BEGIN
    CREATE INDEX [IX_Orders_QuoteId] ON [Orders] ([QuoteId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250922020049_UpdateCustomerAndContactTables'
)
BEGIN
    CREATE INDEX [IX_Packages_CreatedBy] ON [Packages] ([CreatedBy]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250922020049_UpdateCustomerAndContactTables'
)
BEGIN
    CREATE INDEX [IX_Packages_EfficiencyRateId] ON [Packages] ([EfficiencyRateId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250922020049_UpdateCustomerAndContactTables'
)
BEGIN
    CREATE INDEX [IX_Packages_LastModifiedBy] ON [Packages] ([LastModifiedBy]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250922020049_UpdateCustomerAndContactTables'
)
BEGIN
    CREATE INDEX [IX_Packages_OrderId] ON [Packages] ([OrderId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250922020049_UpdateCustomerAndContactTables'
)
BEGIN
    CREATE INDEX [IX_Packages_ProjectId] ON [Packages] ([ProjectId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250922020049_UpdateCustomerAndContactTables'
)
BEGIN
    CREATE INDEX [IX_Packages_RoutingId] ON [Packages] ([RoutingId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250922020049_UpdateCustomerAndContactTables'
)
BEGIN
    CREATE INDEX [IX_Projects_CustomerId] ON [Projects] ([CustomerId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250922020049_UpdateCustomerAndContactTables'
)
BEGIN
    CREATE UNIQUE INDEX [IX_Projects_JobNumber] ON [Projects] ([JobNumber]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250922020049_UpdateCustomerAndContactTables'
)
BEGIN
    CREATE INDEX [IX_Projects_LastModifiedBy] ON [Projects] ([LastModifiedBy]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250922020049_UpdateCustomerAndContactTables'
)
BEGIN
    CREATE INDEX [IX_Projects_OrderId] ON [Projects] ([OrderId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250922020049_UpdateCustomerAndContactTables'
)
BEGIN
    CREATE INDEX [IX_Projects_OwnerId] ON [Projects] ([OwnerId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250922020049_UpdateCustomerAndContactTables'
)
BEGIN
    CREATE INDEX [IX_QuoteLineItems_QuoteId] ON [QuoteLineItems] ([QuoteId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250922020049_UpdateCustomerAndContactTables'
)
BEGIN
    CREATE INDEX [IX_Quotes_CreatedBy] ON [Quotes] ([CreatedBy]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250922020049_UpdateCustomerAndContactTables'
)
BEGIN
    CREATE INDEX [IX_Quotes_CustomerId] ON [Quotes] ([CustomerId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250922020049_UpdateCustomerAndContactTables'
)
BEGIN
    CREATE INDEX [IX_Quotes_LastModifiedBy] ON [Quotes] ([LastModifiedBy]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250922020049_UpdateCustomerAndContactTables'
)
BEGIN
    CREATE INDEX [IX_Quotes_OrderId] ON [Quotes] ([OrderId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250922020049_UpdateCustomerAndContactTables'
)
BEGIN
    CREATE UNIQUE INDEX [IX_RefreshTokens_Token] ON [RefreshTokens] ([Token]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250922020049_UpdateCustomerAndContactTables'
)
BEGIN
    CREATE INDEX [IX_RefreshTokens_UserId_ExpiresAt] ON [RefreshTokens] ([UserId], [ExpiresAt]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250922020049_UpdateCustomerAndContactTables'
)
BEGIN
    CREATE INDEX [IX_Resources_PrimaryWorkCenterId] ON [Resources] ([PrimaryWorkCenterId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250922020049_UpdateCustomerAndContactTables'
)
BEGIN
    CREATE INDEX [IX_Resources_UserId] ON [Resources] ([UserId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250922020049_UpdateCustomerAndContactTables'
)
BEGIN
    CREATE INDEX [IX_RoutingOperations_RoutingTemplateId] ON [RoutingOperations] ([RoutingTemplateId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250922020049_UpdateCustomerAndContactTables'
)
BEGIN
    CREATE INDEX [IX_TraceBeamDetections_TraceDrawingId] ON [TraceBeamDetections] ([TraceDrawingId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250922020049_UpdateCustomerAndContactTables'
)
BEGIN
    CREATE INDEX [IX_TraceDrawings_CompanyId] ON [TraceDrawings] ([CompanyId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250922020049_UpdateCustomerAndContactTables'
)
BEGIN
    CREATE INDEX [IX_TraceDrawings_CustomerId] ON [TraceDrawings] ([CustomerId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250922020049_UpdateCustomerAndContactTables'
)
BEGIN
    CREATE INDEX [IX_TraceDrawings_PackageId] ON [TraceDrawings] ([PackageId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250922020049_UpdateCustomerAndContactTables'
)
BEGIN
    CREATE INDEX [IX_TraceDrawings_ProjectId] ON [TraceDrawings] ([ProjectId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250922020049_UpdateCustomerAndContactTables'
)
BEGIN
    CREATE INDEX [IX_TraceDrawings_UploadedBy] ON [TraceDrawings] ([UploadedBy]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250922020049_UpdateCustomerAndContactTables'
)
BEGIN
    CREATE INDEX [IX_TraceMeasurements_TraceDrawingId] ON [TraceMeasurements] ([TraceDrawingId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250922020049_UpdateCustomerAndContactTables'
)
BEGIN
    CREATE INDEX [IX_TraceTakeoffItems_TraceDrawingId] ON [TraceTakeoffItems] ([TraceDrawingId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250922020049_UpdateCustomerAndContactTables'
)
BEGIN
    CREATE UNIQUE INDEX [IX_UserAuthMethods_UserId_Provider] ON [UserAuthMethods] ([UserId], [Provider]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250922020049_UpdateCustomerAndContactTables'
)
BEGIN
    CREATE INDEX [IX_Users_CompanyId] ON [Users] ([CompanyId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250922020049_UpdateCustomerAndContactTables'
)
BEGIN
    CREATE UNIQUE INDEX [IX_Users_Email] ON [Users] ([Email]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250922020049_UpdateCustomerAndContactTables'
)
BEGIN
    CREATE UNIQUE INDEX [IX_Users_Username] ON [Users] ([Username]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250922020049_UpdateCustomerAndContactTables'
)
BEGIN
    CREATE INDEX [IX_WeldingConnections_PackageId] ON [WeldingConnections] ([PackageId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250922020049_UpdateCustomerAndContactTables'
)
BEGIN
    CREATE INDEX [IX_WorkCenters_CompanyId] ON [WorkCenters] ([CompanyId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250922020049_UpdateCustomerAndContactTables'
)
BEGIN
    CREATE UNIQUE INDEX [IX_WorkCenters_WorkCenterCode] ON [WorkCenters] ([WorkCenterCode]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250922020049_UpdateCustomerAndContactTables'
)
BEGIN
    CREATE INDEX [IX_WorkCenterShifts_WorkCenterId] ON [WorkCenterShifts] ([WorkCenterId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250922020049_UpdateCustomerAndContactTables'
)
BEGIN
    CREATE INDEX [IX_WorkOrderAssembly_WorkOrderId] ON [WorkOrderAssembly] ([WorkOrderId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250922020049_UpdateCustomerAndContactTables'
)
BEGIN
    CREATE INDEX [IX_WorkOrderInventoryItem_WorkOrderId] ON [WorkOrderInventoryItem] ([WorkOrderId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250922020049_UpdateCustomerAndContactTables'
)
BEGIN
    CREATE INDEX [IX_WorkOrderOperations_CompletedBy] ON [WorkOrderOperations] ([CompletedBy]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250922020049_UpdateCustomerAndContactTables'
)
BEGIN
    CREATE INDEX [IX_WorkOrderOperations_WorkOrderId] ON [WorkOrderOperations] ([WorkOrderId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250922020049_UpdateCustomerAndContactTables'
)
BEGIN
    CREATE INDEX [IX_WorkOrderResources_ResourceId] ON [WorkOrderResources] ([ResourceId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250922020049_UpdateCustomerAndContactTables'
)
BEGIN
    CREATE INDEX [IX_WorkOrderResources_WorkOrderId] ON [WorkOrderResources] ([WorkOrderId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250922020049_UpdateCustomerAndContactTables'
)
BEGIN
    CREATE INDEX [IX_WorkOrders_CreatedBy] ON [WorkOrders] ([CreatedBy]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250922020049_UpdateCustomerAndContactTables'
)
BEGIN
    CREATE INDEX [IX_WorkOrders_LastModifiedBy] ON [WorkOrders] ([LastModifiedBy]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250922020049_UpdateCustomerAndContactTables'
)
BEGIN
    CREATE INDEX [IX_WorkOrders_PackageId] ON [WorkOrders] ([PackageId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250922020049_UpdateCustomerAndContactTables'
)
BEGIN
    CREATE INDEX [IX_WorkOrders_PrimaryResourceId] ON [WorkOrders] ([PrimaryResourceId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250922020049_UpdateCustomerAndContactTables'
)
BEGIN
    CREATE INDEX [IX_WorkOrders_WorkCenterId] ON [WorkOrders] ([WorkCenterId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250922020049_UpdateCustomerAndContactTables'
)
BEGIN
    ALTER TABLE [EstimationPackages] ADD CONSTRAINT [FK_EstimationPackages_Estimations_EstimationId] FOREIGN KEY ([EstimationId]) REFERENCES [Estimations] ([Id]) ON DELETE CASCADE;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250922020049_UpdateCustomerAndContactTables'
)
BEGIN
    ALTER TABLE [Estimations] ADD CONSTRAINT [FK_Estimations_Orders_OrderId] FOREIGN KEY ([OrderId]) REFERENCES [Orders] ([Id]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250922020049_UpdateCustomerAndContactTables'
)
BEGIN
    ALTER TABLE [Orders] ADD CONSTRAINT [FK_Orders_Quotes_QuoteId] FOREIGN KEY ([QuoteId]) REFERENCES [Quotes] ([Id]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250922020049_UpdateCustomerAndContactTables'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20250922020049_UpdateCustomerAndContactTables', N'8.0.0');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250922043044_AddAddressFieldsToCustomerContact'
)
BEGIN
    ALTER TABLE [CustomerContacts] ADD [AddressLine1] nvarchar(200) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250922043044_AddAddressFieldsToCustomerContact'
)
BEGIN
    ALTER TABLE [CustomerContacts] ADD [AddressLine2] nvarchar(200) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250922043044_AddAddressFieldsToCustomerContact'
)
BEGIN
    ALTER TABLE [CustomerContacts] ADD [City] nvarchar(100) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250922043044_AddAddressFieldsToCustomerContact'
)
BEGIN
    ALTER TABLE [CustomerContacts] ADD [Country] nvarchar(100) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250922043044_AddAddressFieldsToCustomerContact'
)
BEGIN
    ALTER TABLE [CustomerContacts] ADD [FormattedAddress] nvarchar(500) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250922043044_AddAddressFieldsToCustomerContact'
)
BEGIN
    ALTER TABLE [CustomerContacts] ADD [GooglePlaceId] nvarchar(500) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250922043044_AddAddressFieldsToCustomerContact'
)
BEGIN
    ALTER TABLE [CustomerContacts] ADD [InheritCustomerAddress] bit NOT NULL DEFAULT CAST(0 AS bit);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250922043044_AddAddressFieldsToCustomerContact'
)
BEGIN
    ALTER TABLE [CustomerContacts] ADD [PostalCode] nvarchar(20) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250922043044_AddAddressFieldsToCustomerContact'
)
BEGIN
    ALTER TABLE [CustomerContacts] ADD [State] nvarchar(100) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250922043044_AddAddressFieldsToCustomerContact'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20250922043044_AddAddressFieldsToCustomerContact', N'8.0.0');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250929050816_AddShortNameToCompany'
)
BEGIN
    ALTER TABLE [TraceDrawings] ADD [ContactId] int NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250929050816_AddShortNameToCompany'
)
BEGIN
    ALTER TABLE [Companies] ADD [ShortName] nvarchar(50) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250929050816_AddShortNameToCompany'
)
BEGIN
    CREATE TABLE [PackageDrawings] (
        [Id] int NOT NULL IDENTITY,
        [PackageId] int NOT NULL,
        [DrawingNumber] nvarchar(100) NOT NULL,
        [DrawingTitle] nvarchar(500) NOT NULL,
        [SharePointItemId] nvarchar(500) NOT NULL,
        [SharePointUrl] nvarchar(1000) NOT NULL,
        [FileType] nvarchar(50) NOT NULL,
        [FileSize] bigint NOT NULL,
        [UploadedDate] datetime2 NOT NULL,
        [UploadedBy] int NOT NULL,
        [IsActive] bit NOT NULL,
        [Notes] nvarchar(500) NULL,
        CONSTRAINT [PK_PackageDrawings] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_PackageDrawings_Packages_PackageId] FOREIGN KEY ([PackageId]) REFERENCES [Packages] ([Id]) ON DELETE CASCADE,
        CONSTRAINT [FK_PackageDrawings_Users_UploadedBy] FOREIGN KEY ([UploadedBy]) REFERENCES [Users] ([Id]) ON DELETE CASCADE
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250929050816_AddShortNameToCompany'
)
BEGIN
    CREATE TABLE [SavedViewPreferences] (
        [Id] int NOT NULL IDENTITY,
        [Name] nvarchar(max) NOT NULL,
        [EntityType] nvarchar(max) NOT NULL,
        [ViewType] nvarchar(max) NOT NULL,
        [UserId] nvarchar(max) NOT NULL,
        [CompanyId] int NULL,
        [IsDefault] bit NOT NULL,
        [IsShared] bit NOT NULL,
        [ViewStateJson] nvarchar(max) NULL,
        [CreatedDate] datetime2 NOT NULL,
        [LastModified] datetime2 NOT NULL,
        CONSTRAINT [PK_SavedViewPreferences] PRIMARY KEY ([Id])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250929050816_AddShortNameToCompany'
)
BEGIN
    CREATE TABLE [TraceRecords] (
        [Id] int NOT NULL IDENTITY,
        [TraceId] uniqueidentifier NOT NULL,
        [TraceNumber] nvarchar(50) NOT NULL,
        [EntityType] int NOT NULL,
        [EntityId] int NOT NULL,
        [EntityReference] nvarchar(100) NULL,
        [Description] nvarchar(500) NULL,
        [ParentTraceId] uniqueidentifier NULL,
        [CaptureDateTime] datetime2 NOT NULL,
        [EventDateTime] datetime2 NULL,
        [UserId] int NULL,
        [OperatorName] nvarchar(100) NULL,
        [WorkCenterId] int NULL,
        [Location] nvarchar(100) NULL,
        [MachineId] nvarchar(50) NULL,
        [Status] int NOT NULL,
        [IsActive] bit NOT NULL,
        [CreatedDate] datetime2 NOT NULL,
        [ModifiedDate] datetime2 NULL,
        [CompanyId] int NOT NULL,
        [TraceRecordId] int NULL,
        CONSTRAINT [PK_TraceRecords] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_TraceRecords_Companies_CompanyId] FOREIGN KEY ([CompanyId]) REFERENCES [Companies] ([Id]) ON DELETE CASCADE,
        CONSTRAINT [FK_TraceRecords_TraceRecords_TraceRecordId] FOREIGN KEY ([TraceRecordId]) REFERENCES [TraceRecords] ([Id]),
        CONSTRAINT [FK_TraceRecords_Users_UserId] FOREIGN KEY ([UserId]) REFERENCES [Users] ([Id]),
        CONSTRAINT [FK_TraceRecords_WorkCenters_WorkCenterId] FOREIGN KEY ([WorkCenterId]) REFERENCES [WorkCenters] ([Id])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250929050816_AddShortNameToCompany'
)
BEGIN
    CREATE TABLE [Calibrations] (
        [Id] int NOT NULL IDENTITY,
        [PackageDrawingId] int NOT NULL,
        [PixelsPerUnit] float NOT NULL,
        [ScaleRatio] decimal(18,2) NOT NULL,
        [KnownDistance] float NOT NULL,
        [MeasuredPixels] float NOT NULL,
        [Point1Json] nvarchar(max) NOT NULL,
        [Point2Json] nvarchar(max) NOT NULL,
        [Units] nvarchar(10) NOT NULL,
        [CreatedDate] datetime2 NOT NULL,
        [CreatedBy] int NOT NULL,
        [IsActive] bit NOT NULL,
        [Notes] nvarchar(500) NULL,
        CONSTRAINT [PK_Calibrations] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_Calibrations_PackageDrawings_PackageDrawingId] FOREIGN KEY ([PackageDrawingId]) REFERENCES [PackageDrawings] ([Id]) ON DELETE CASCADE
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250929050816_AddShortNameToCompany'
)
BEGIN
    CREATE TABLE [TraceAssemblies] (
        [Id] int NOT NULL IDENTITY,
        [TraceRecordId] int NOT NULL,
        [AssemblyId] int NULL,
        [AssemblyNumber] nvarchar(100) NOT NULL,
        [SerialNumber] nvarchar(50) NULL,
        [AssemblyDate] datetime2 NOT NULL,
        [BuildOperatorId] int NULL,
        [BuildOperatorName] nvarchar(100) NULL,
        [BuildWorkCenterId] int NULL,
        [BuildLocation] nvarchar(100) NULL,
        [BuildNotes] nvarchar(1000) NULL,
        [CreatedDate] datetime2 NOT NULL,
        CONSTRAINT [PK_TraceAssemblies] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_TraceAssemblies_Assemblies_AssemblyId] FOREIGN KEY ([AssemblyId]) REFERENCES [Assemblies] ([Id]),
        CONSTRAINT [FK_TraceAssemblies_TraceRecords_TraceRecordId] FOREIGN KEY ([TraceRecordId]) REFERENCES [TraceRecords] ([Id]) ON DELETE CASCADE,
        CONSTRAINT [FK_TraceAssemblies_Users_BuildOperatorId] FOREIGN KEY ([BuildOperatorId]) REFERENCES [Users] ([Id]),
        CONSTRAINT [FK_TraceAssemblies_WorkCenters_BuildWorkCenterId] FOREIGN KEY ([BuildWorkCenterId]) REFERENCES [WorkCenters] ([Id])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250929050816_AddShortNameToCompany'
)
BEGIN
    CREATE TABLE [TraceDocuments] (
        [Id] int NOT NULL IDENTITY,
        [TraceRecordId] int NOT NULL,
        [DocumentType] int NOT NULL,
        [DocumentName] nvarchar(200) NOT NULL,
        [Description] nvarchar(500) NULL,
        [FilePath] nvarchar(500) NOT NULL,
        [FileHash] nvarchar(100) NULL,
        [FileSize] bigint NOT NULL,
        [MimeType] nvarchar(100) NOT NULL,
        [IsVerified] bit NOT NULL,
        [VerifiedDate] datetime2 NULL,
        [VerifiedBy] int NULL,
        [UploadDate] datetime2 NOT NULL,
        [UploadedBy] int NULL,
        [CompanyId] int NOT NULL,
        [VerifiedByUserId] int NULL,
        [UploadedByUserId] int NULL,
        CONSTRAINT [PK_TraceDocuments] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_TraceDocuments_Companies_CompanyId] FOREIGN KEY ([CompanyId]) REFERENCES [Companies] ([Id]) ON DELETE CASCADE,
        CONSTRAINT [FK_TraceDocuments_TraceRecords_TraceRecordId] FOREIGN KEY ([TraceRecordId]) REFERENCES [TraceRecords] ([Id]) ON DELETE CASCADE,
        CONSTRAINT [FK_TraceDocuments_Users_UploadedByUserId] FOREIGN KEY ([UploadedByUserId]) REFERENCES [Users] ([Id]),
        CONSTRAINT [FK_TraceDocuments_Users_VerifiedByUserId] FOREIGN KEY ([VerifiedByUserId]) REFERENCES [Users] ([Id])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250929050816_AddShortNameToCompany'
)
BEGIN
    CREATE TABLE [TraceMaterials] (
        [Id] int NOT NULL IDENTITY,
        [TraceRecordId] int NOT NULL,
        [CatalogueItemId] int NULL,
        [MaterialCode] nvarchar(50) NOT NULL,
        [Description] nvarchar(200) NOT NULL,
        [HeatNumber] nvarchar(50) NULL,
        [BatchNumber] nvarchar(50) NULL,
        [SerialNumber] nvarchar(50) NULL,
        [Supplier] nvarchar(100) NULL,
        [SupplierBatch] nvarchar(50) NULL,
        [MillCertificate] nvarchar(100) NULL,
        [CertType] int NULL,
        [CertDate] datetime2 NULL,
        [Quantity] decimal(18,4) NOT NULL,
        [Unit] nvarchar(20) NOT NULL,
        [Weight] decimal(18,4) NULL,
        [ChemicalComposition] nvarchar(500) NULL,
        [MechanicalProperties] nvarchar(500) NULL,
        [TestResults] nvarchar(1000) NULL,
        [CreatedDate] datetime2 NOT NULL,
        CONSTRAINT [PK_TraceMaterials] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_TraceMaterials_CatalogueItems_CatalogueItemId] FOREIGN KEY ([CatalogueItemId]) REFERENCES [CatalogueItems] ([Id]),
        CONSTRAINT [FK_TraceMaterials_TraceRecords_TraceRecordId] FOREIGN KEY ([TraceRecordId]) REFERENCES [TraceRecords] ([Id]) ON DELETE CASCADE
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250929050816_AddShortNameToCompany'
)
BEGIN
    CREATE TABLE [TraceProcesses] (
        [Id] int NOT NULL IDENTITY,
        [TraceRecordId] int NOT NULL,
        [WorkOrderOperationId] int NULL,
        [OperationCode] nvarchar(50) NOT NULL,
        [OperationDescription] nvarchar(200) NOT NULL,
        [StartTime] datetime2 NOT NULL,
        [EndTime] datetime2 NULL,
        [DurationMinutes] decimal(18,2) NULL,
        [OperatorId] int NULL,
        [OperatorName] nvarchar(100) NULL,
        [MachineId] int NULL,
        [MachineName] nvarchar(100) NULL,
        [PassedInspection] bit NULL,
        [InspectionNotes] nvarchar(500) NULL,
        [CreatedDate] datetime2 NOT NULL,
        CONSTRAINT [PK_TraceProcesses] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_TraceProcesses_TraceRecords_TraceRecordId] FOREIGN KEY ([TraceRecordId]) REFERENCES [TraceRecords] ([Id]) ON DELETE CASCADE,
        CONSTRAINT [FK_TraceProcesses_Users_OperatorId] FOREIGN KEY ([OperatorId]) REFERENCES [Users] ([Id]),
        CONSTRAINT [FK_TraceProcesses_WorkOrderOperations_WorkOrderOperationId] FOREIGN KEY ([WorkOrderOperationId]) REFERENCES [WorkOrderOperations] ([Id])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250929050816_AddShortNameToCompany'
)
BEGIN
    CREATE TABLE [TraceTakeoffs] (
        [Id] int NOT NULL IDENTITY,
        [TraceRecordId] int NOT NULL,
        [DrawingId] int NULL,
        [PdfUrl] nvarchar(500) NOT NULL,
        [Scale] decimal(10,4) NULL,
        [ScaleUnit] nvarchar(20) NULL,
        [CalibrationData] nvarchar(max) NULL,
        [Status] nvarchar(50) NULL,
        [CreatedDate] datetime2 NOT NULL,
        [ModifiedDate] datetime2 NULL,
        [CompanyId] int NOT NULL,
        CONSTRAINT [PK_TraceTakeoffs] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_TraceTakeoffs_Companies_CompanyId] FOREIGN KEY ([CompanyId]) REFERENCES [Companies] ([Id]) ON DELETE CASCADE,
        CONSTRAINT [FK_TraceTakeoffs_TraceDrawings_DrawingId] FOREIGN KEY ([DrawingId]) REFERENCES [TraceDrawings] ([Id]),
        CONSTRAINT [FK_TraceTakeoffs_TraceRecords_TraceRecordId] FOREIGN KEY ([TraceRecordId]) REFERENCES [TraceRecords] ([Id]) ON DELETE CASCADE
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250929050816_AddShortNameToCompany'
)
BEGIN
    CREATE TABLE [TraceComponents] (
        [Id] int NOT NULL IDENTITY,
        [TraceAssemblyId] int NOT NULL,
        [ComponentTraceId] uniqueidentifier NOT NULL,
        [ComponentReference] nvarchar(100) NOT NULL,
        [QuantityUsed] decimal(18,4) NOT NULL,
        [Unit] nvarchar(20) NOT NULL,
        [UsageNotes] nvarchar(500) NULL,
        [CreatedDate] datetime2 NOT NULL,
        CONSTRAINT [PK_TraceComponents] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_TraceComponents_TraceAssemblies_TraceAssemblyId] FOREIGN KEY ([TraceAssemblyId]) REFERENCES [TraceAssemblies] ([Id]) ON DELETE CASCADE
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250929050816_AddShortNameToCompany'
)
BEGIN
    CREATE TABLE [TraceMaterialCatalogueLinks] (
        [Id] int NOT NULL IDENTITY,
        [TraceMaterialId] int NOT NULL,
        [CatalogueItemId] int NOT NULL,
        [Quantity] decimal(18,4) NOT NULL,
        [Unit] nvarchar(20) NOT NULL,
        [CalculatedWeight] decimal(18,4) NULL,
        [CreatedDate] datetime2 NOT NULL,
        CONSTRAINT [PK_TraceMaterialCatalogueLinks] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_TraceMaterialCatalogueLinks_CatalogueItems_CatalogueItemId] FOREIGN KEY ([CatalogueItemId]) REFERENCES [CatalogueItems] ([Id]) ON DELETE CASCADE,
        CONSTRAINT [FK_TraceMaterialCatalogueLinks_TraceMaterials_TraceMaterialId] FOREIGN KEY ([TraceMaterialId]) REFERENCES [TraceMaterials] ([Id]) ON DELETE CASCADE
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250929050816_AddShortNameToCompany'
)
BEGIN
    CREATE TABLE [TraceParameters] (
        [Id] int NOT NULL IDENTITY,
        [TraceProcessId] int NOT NULL,
        [ParameterName] nvarchar(100) NOT NULL,
        [ParameterValue] nvarchar(200) NOT NULL,
        [Unit] nvarchar(20) NULL,
        [NumericValue] decimal(18,4) NULL,
        [Category] nvarchar(50) NULL,
        [CreatedDate] datetime2 NOT NULL,
        CONSTRAINT [PK_TraceParameters] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_TraceParameters_TraceProcesses_TraceProcessId] FOREIGN KEY ([TraceProcessId]) REFERENCES [TraceProcesses] ([Id]) ON DELETE CASCADE
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250929050816_AddShortNameToCompany'
)
BEGIN
    CREATE TABLE [TraceTakeoffAnnotations] (
        [Id] int NOT NULL IDENTITY,
        [TraceTakeoffId] int NOT NULL,
        [AnnotationType] nvarchar(50) NOT NULL,
        [AnnotationData] nvarchar(max) NULL,
        [Coordinates] nvarchar(max) NULL,
        [Text] nvarchar(500) NULL,
        [Color] nvarchar(50) NULL,
        [PageNumber] int NULL,
        [CreatedDate] datetime2 NOT NULL,
        [CreatedBy] int NULL,
        [CreatedByUserId] int NULL,
        CONSTRAINT [PK_TraceTakeoffAnnotations] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_TraceTakeoffAnnotations_TraceTakeoffs_TraceTakeoffId] FOREIGN KEY ([TraceTakeoffId]) REFERENCES [TraceTakeoffs] ([Id]) ON DELETE CASCADE,
        CONSTRAINT [FK_TraceTakeoffAnnotations_Users_CreatedByUserId] FOREIGN KEY ([CreatedByUserId]) REFERENCES [Users] ([Id])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250929050816_AddShortNameToCompany'
)
BEGIN
    CREATE TABLE [TraceTakeoffMeasurements] (
        [Id] int NOT NULL IDENTITY,
        [TraceTakeoffId] int NOT NULL,
        [PackageDrawingId] int NULL,
        [CatalogueItemId] int NULL,
        [MeasurementType] nvarchar(50) NOT NULL,
        [Value] decimal(18,4) NOT NULL,
        [Unit] nvarchar(20) NOT NULL,
        [Coordinates] nvarchar(max) NULL,
        [Label] nvarchar(200) NULL,
        [Description] nvarchar(500) NULL,
        [Color] nvarchar(50) NULL,
        [PageNumber] int NULL,
        [CalculatedWeight] decimal(18,4) NULL,
        [CreatedDate] datetime2 NOT NULL,
        CONSTRAINT [PK_TraceTakeoffMeasurements] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_TraceTakeoffMeasurements_CatalogueItems_CatalogueItemId] FOREIGN KEY ([CatalogueItemId]) REFERENCES [CatalogueItems] ([Id]),
        CONSTRAINT [FK_TraceTakeoffMeasurements_PackageDrawings_PackageDrawingId] FOREIGN KEY ([PackageDrawingId]) REFERENCES [PackageDrawings] ([Id]),
        CONSTRAINT [FK_TraceTakeoffMeasurements_TraceTakeoffs_TraceTakeoffId] FOREIGN KEY ([TraceTakeoffId]) REFERENCES [TraceTakeoffs] ([Id]) ON DELETE CASCADE
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250929050816_AddShortNameToCompany'
)
BEGIN
    CREATE INDEX [IX_Calibrations_PackageDrawingId] ON [Calibrations] ([PackageDrawingId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250929050816_AddShortNameToCompany'
)
BEGIN
    CREATE INDEX [IX_PackageDrawings_PackageId] ON [PackageDrawings] ([PackageId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250929050816_AddShortNameToCompany'
)
BEGIN
    CREATE INDEX [IX_PackageDrawings_UploadedBy] ON [PackageDrawings] ([UploadedBy]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250929050816_AddShortNameToCompany'
)
BEGIN
    CREATE INDEX [IX_TraceAssemblies_AssemblyId] ON [TraceAssemblies] ([AssemblyId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250929050816_AddShortNameToCompany'
)
BEGIN
    CREATE INDEX [IX_TraceAssemblies_BuildOperatorId] ON [TraceAssemblies] ([BuildOperatorId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250929050816_AddShortNameToCompany'
)
BEGIN
    CREATE INDEX [IX_TraceAssemblies_BuildWorkCenterId] ON [TraceAssemblies] ([BuildWorkCenterId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250929050816_AddShortNameToCompany'
)
BEGIN
    CREATE INDEX [IX_TraceAssemblies_TraceRecordId] ON [TraceAssemblies] ([TraceRecordId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250929050816_AddShortNameToCompany'
)
BEGIN
    CREATE INDEX [IX_TraceComponents_TraceAssemblyId] ON [TraceComponents] ([TraceAssemblyId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250929050816_AddShortNameToCompany'
)
BEGIN
    CREATE INDEX [IX_TraceDocuments_CompanyId] ON [TraceDocuments] ([CompanyId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250929050816_AddShortNameToCompany'
)
BEGIN
    CREATE INDEX [IX_TraceDocuments_TraceRecordId] ON [TraceDocuments] ([TraceRecordId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250929050816_AddShortNameToCompany'
)
BEGIN
    CREATE INDEX [IX_TraceDocuments_UploadedByUserId] ON [TraceDocuments] ([UploadedByUserId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250929050816_AddShortNameToCompany'
)
BEGIN
    CREATE INDEX [IX_TraceDocuments_VerifiedByUserId] ON [TraceDocuments] ([VerifiedByUserId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250929050816_AddShortNameToCompany'
)
BEGIN
    CREATE INDEX [IX_TraceMaterialCatalogueLinks_CatalogueItemId] ON [TraceMaterialCatalogueLinks] ([CatalogueItemId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250929050816_AddShortNameToCompany'
)
BEGIN
    CREATE INDEX [IX_TraceMaterialCatalogueLinks_TraceMaterialId] ON [TraceMaterialCatalogueLinks] ([TraceMaterialId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250929050816_AddShortNameToCompany'
)
BEGIN
    CREATE INDEX [IX_TraceMaterials_CatalogueItemId] ON [TraceMaterials] ([CatalogueItemId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250929050816_AddShortNameToCompany'
)
BEGIN
    CREATE INDEX [IX_TraceMaterials_TraceRecordId] ON [TraceMaterials] ([TraceRecordId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250929050816_AddShortNameToCompany'
)
BEGIN
    CREATE INDEX [IX_TraceParameters_TraceProcessId] ON [TraceParameters] ([TraceProcessId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250929050816_AddShortNameToCompany'
)
BEGIN
    CREATE INDEX [IX_TraceProcesses_OperatorId] ON [TraceProcesses] ([OperatorId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250929050816_AddShortNameToCompany'
)
BEGIN
    CREATE INDEX [IX_TraceProcesses_TraceRecordId] ON [TraceProcesses] ([TraceRecordId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250929050816_AddShortNameToCompany'
)
BEGIN
    CREATE INDEX [IX_TraceProcesses_WorkOrderOperationId] ON [TraceProcesses] ([WorkOrderOperationId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250929050816_AddShortNameToCompany'
)
BEGIN
    CREATE INDEX [IX_TraceRecords_CompanyId] ON [TraceRecords] ([CompanyId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250929050816_AddShortNameToCompany'
)
BEGIN
    CREATE INDEX [IX_TraceRecords_TraceRecordId] ON [TraceRecords] ([TraceRecordId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250929050816_AddShortNameToCompany'
)
BEGIN
    CREATE INDEX [IX_TraceRecords_UserId] ON [TraceRecords] ([UserId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250929050816_AddShortNameToCompany'
)
BEGIN
    CREATE INDEX [IX_TraceRecords_WorkCenterId] ON [TraceRecords] ([WorkCenterId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250929050816_AddShortNameToCompany'
)
BEGIN
    CREATE INDEX [IX_TraceTakeoffAnnotations_CreatedByUserId] ON [TraceTakeoffAnnotations] ([CreatedByUserId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250929050816_AddShortNameToCompany'
)
BEGIN
    CREATE INDEX [IX_TraceTakeoffAnnotations_TraceTakeoffId] ON [TraceTakeoffAnnotations] ([TraceTakeoffId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250929050816_AddShortNameToCompany'
)
BEGIN
    CREATE INDEX [IX_TraceTakeoffMeasurements_CatalogueItemId] ON [TraceTakeoffMeasurements] ([CatalogueItemId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250929050816_AddShortNameToCompany'
)
BEGIN
    CREATE INDEX [IX_TraceTakeoffMeasurements_PackageDrawingId] ON [TraceTakeoffMeasurements] ([PackageDrawingId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250929050816_AddShortNameToCompany'
)
BEGIN
    CREATE INDEX [IX_TraceTakeoffMeasurements_TraceTakeoffId] ON [TraceTakeoffMeasurements] ([TraceTakeoffId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250929050816_AddShortNameToCompany'
)
BEGIN
    CREATE INDEX [IX_TraceTakeoffs_CompanyId] ON [TraceTakeoffs] ([CompanyId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250929050816_AddShortNameToCompany'
)
BEGIN
    CREATE INDEX [IX_TraceTakeoffs_DrawingId] ON [TraceTakeoffs] ([DrawingId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250929050816_AddShortNameToCompany'
)
BEGIN
    CREATE INDEX [IX_TraceTakeoffs_TraceRecordId] ON [TraceTakeoffs] ([TraceRecordId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250929050816_AddShortNameToCompany'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20250929050816_AddShortNameToCompany', N'8.0.0');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250930231103_AddMissingSchemaChanges'
)
BEGIN
    EXEC sp_rename N'[TraceDrawings].[ProjectNumber]', N'TakeoffNumber', N'COLUMN';
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250930231103_AddMissingSchemaChanges'
)
BEGIN
    ALTER TABLE [TraceDrawings] ADD [ContactPerson] nvarchar(200) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250930231103_AddMissingSchemaChanges'
)
BEGIN
    ALTER TABLE [TraceDrawings] ADD [ProjectArchitect] nvarchar(200) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250930231103_AddMissingSchemaChanges'
)
BEGIN
    ALTER TABLE [TraceDrawings] ADD [ProjectEngineer] nvarchar(200) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250930231103_AddMissingSchemaChanges'
)
BEGIN
    CREATE TABLE [GlobalSettings] (
        [Id] int NOT NULL IDENTITY,
        [SettingKey] nvarchar(100) NOT NULL,
        [SettingValue] nvarchar(max) NOT NULL,
        [SettingType] nvarchar(50) NULL,
        [Category] nvarchar(50) NULL,
        [Description] nvarchar(500) NULL,
        [IsSystemSetting] bit NOT NULL,
        [RequiresRestart] bit NOT NULL,
        [IsEncrypted] bit NOT NULL,
        [ValidationRule] nvarchar(500) NULL,
        [DefaultValue] nvarchar(max) NULL,
        [IsActive] bit NOT NULL,
        [CreatedDate] datetime2 NOT NULL,
        [LastModified] datetime2 NOT NULL,
        [LastModifiedByUserId] int NULL,
        CONSTRAINT [PK_GlobalSettings] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_GlobalSettings_Users_LastModifiedByUserId] FOREIGN KEY ([LastModifiedByUserId]) REFERENCES [Users] ([Id])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250930231103_AddMissingSchemaChanges'
)
BEGIN
    CREATE TABLE [ModuleSettings] (
        [Id] int NOT NULL IDENTITY,
        [ModuleName] nvarchar(50) NOT NULL,
        [CompanyId] int NOT NULL,
        [SettingKey] nvarchar(100) NOT NULL,
        [SettingValue] nvarchar(max) NOT NULL,
        [SettingType] nvarchar(50) NULL,
        [Description] nvarchar(500) NULL,
        [IsUserSpecific] bit NOT NULL,
        [UserId] int NULL,
        [IsActive] bit NOT NULL,
        [CreatedDate] datetime2 NOT NULL,
        [LastModified] datetime2 NOT NULL,
        [CreatedByUserId] int NULL,
        [LastModifiedByUserId] int NULL,
        CONSTRAINT [PK_ModuleSettings] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_ModuleSettings_Companies_CompanyId] FOREIGN KEY ([CompanyId]) REFERENCES [Companies] ([Id]) ON DELETE CASCADE,
        CONSTRAINT [FK_ModuleSettings_Users_CreatedByUserId] FOREIGN KEY ([CreatedByUserId]) REFERENCES [Users] ([Id]),
        CONSTRAINT [FK_ModuleSettings_Users_LastModifiedByUserId] FOREIGN KEY ([LastModifiedByUserId]) REFERENCES [Users] ([Id]),
        CONSTRAINT [FK_ModuleSettings_Users_UserId] FOREIGN KEY ([UserId]) REFERENCES [Users] ([Id])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250930231103_AddMissingSchemaChanges'
)
BEGIN
    CREATE TABLE [NumberSeries] (
        [Id] int NOT NULL IDENTITY,
        [CompanyId] int NOT NULL,
        [EntityType] nvarchar(50) NOT NULL,
        [Prefix] nvarchar(20) NULL,
        [Suffix] nvarchar(20) NULL,
        [CurrentNumber] int NOT NULL,
        [StartingNumber] int NOT NULL,
        [IncrementBy] int NOT NULL,
        [MinDigits] int NOT NULL,
        [Format] nvarchar(100) NULL,
        [IncludeYear] bit NOT NULL,
        [IncludeMonth] bit NOT NULL,
        [IncludeCompanyCode] bit NOT NULL,
        [ResetYearly] bit NOT NULL,
        [ResetMonthly] bit NOT NULL,
        [LastResetYear] int NULL,
        [LastResetMonth] int NULL,
        [IsActive] bit NOT NULL,
        [AllowManualEntry] bit NOT NULL,
        [Description] nvarchar(200) NULL,
        [PreviewExample] nvarchar(50) NULL,
        [LastUsed] datetime2 NOT NULL,
        [CreatedDate] datetime2 NOT NULL,
        [LastModified] datetime2 NOT NULL,
        [CreatedByUserId] int NULL,
        [LastModifiedByUserId] int NULL,
        CONSTRAINT [PK_NumberSeries] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_NumberSeries_Users_CreatedByUserId] FOREIGN KEY ([CreatedByUserId]) REFERENCES [Users] ([Id]),
        CONSTRAINT [FK_NumberSeries_Users_LastModifiedByUserId] FOREIGN KEY ([LastModifiedByUserId]) REFERENCES [Users] ([Id])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250930231103_AddMissingSchemaChanges'
)
BEGIN
    CREATE INDEX [IX_GlobalSettings_LastModifiedByUserId] ON [GlobalSettings] ([LastModifiedByUserId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250930231103_AddMissingSchemaChanges'
)
BEGIN
    CREATE INDEX [IX_ModuleSettings_CompanyId] ON [ModuleSettings] ([CompanyId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250930231103_AddMissingSchemaChanges'
)
BEGIN
    CREATE INDEX [IX_ModuleSettings_CreatedByUserId] ON [ModuleSettings] ([CreatedByUserId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250930231103_AddMissingSchemaChanges'
)
BEGIN
    CREATE INDEX [IX_ModuleSettings_LastModifiedByUserId] ON [ModuleSettings] ([LastModifiedByUserId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250930231103_AddMissingSchemaChanges'
)
BEGIN
    CREATE INDEX [IX_ModuleSettings_UserId] ON [ModuleSettings] ([UserId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250930231103_AddMissingSchemaChanges'
)
BEGIN
    CREATE INDEX [IX_NumberSeries_CreatedByUserId] ON [NumberSeries] ([CreatedByUserId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250930231103_AddMissingSchemaChanges'
)
BEGIN
    CREATE INDEX [IX_NumberSeries_LastModifiedByUserId] ON [NumberSeries] ([LastModifiedByUserId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250930231103_AddMissingSchemaChanges'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20250930231103_AddMissingSchemaChanges', N'8.0.0');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

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

