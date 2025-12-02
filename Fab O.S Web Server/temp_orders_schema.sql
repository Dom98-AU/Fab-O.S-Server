CREATE TABLE [Companies] (
    [Id] int NOT NULL IDENTITY,
    [Name] nvarchar(200) NOT NULL,
    [Code] nvarchar(50) NOT NULL,
    [ShortName] nvarchar(50) NULL,
    [IsActive] bit NOT NULL DEFAULT CAST(1 AS bit),
    [SubscriptionLevel] nvarchar(50) NOT NULL DEFAULT N'Standard',
    [MaxUsers] int NOT NULL DEFAULT 10,
    [CreatedDate] datetime2 NOT NULL,
    [LastModified] datetime2 NOT NULL,
    [Domain] nvarchar(100) NULL,
    CONSTRAINT [PK_Companies] PRIMARY KEY ([Id])
);
GO


CREATE TABLE [Customers] (
    [Id] int NOT NULL IDENTITY,
    [CompanyId] int NOT NULL,
    [CompanyName] nvarchar(200) NOT NULL,
    [CreatedById] int NULL,
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
GO


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
GO


CREATE TABLE [RoutingTemplates] (
    [Id] int NOT NULL IDENTITY,
    [Name] nvarchar(100) NOT NULL,
    [Description] nvarchar(500) NULL,
    [IsActive] bit NOT NULL,
    [CreatedDate] datetime2 NOT NULL,
    [LastModified] datetime2 NOT NULL,
    CONSTRAINT [PK_RoutingTemplates] PRIMARY KEY ([Id])
);
GO


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
GO


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
GO


CREATE TABLE [SurfaceCoatings] (
    [Id] int NOT NULL IDENTITY,
    [CompanyId] int NOT NULL,
    [CoatingCode] nvarchar(50) NOT NULL,
    [CoatingName] nvarchar(200) NOT NULL,
    [Description] nvarchar(500) NULL,
    [IsActive] bit NOT NULL,
    [DisplayOrder] int NOT NULL,
    [CreatedDate] datetime2 NOT NULL DEFAULT (getutcdate()),
    [ModifiedDate] datetime2 NULL,
    CONSTRAINT [PK_SurfaceCoatings] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_SurfaceCoatings_Companies_CompanyId] FOREIGN KEY ([CompanyId]) REFERENCES [Companies] ([Id]) ON DELETE NO ACTION
);
GO


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
    [CompanyId] int NOT NULL DEFAULT 1,
    [PasswordSalt] nvarchar(100) NULL,
    [AuthProvider] nvarchar(50) NULL,
    [ExternalUserId] nvarchar(256) NULL,
    CONSTRAINT [PK_Users] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_Users_Companies_CompanyId] FOREIGN KEY ([CompanyId]) REFERENCES [Companies] ([Id]) ON DELETE CASCADE
);
GO


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
GO


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
GO


CREATE TABLE [CustomerContacts] (
    [Id] int NOT NULL IDENTITY,
    [ContactNumber] nvarchar(50) NULL,
    [CustomerId] int NOT NULL,
    [FirstName] nvarchar(100) NOT NULL,
    [LastName] nvarchar(100) NOT NULL,
    [Title] nvarchar(50) NULL,
    [Department] nvarchar(100) NULL,
    [Email] nvarchar(200) NOT NULL,
    [PhoneNumber] nvarchar(20) NULL,
    [MobileNumber] nvarchar(20) NULL,
    [AddressLine1] nvarchar(200) NULL,
    [AddressLine2] nvarchar(200) NULL,
    [City] nvarchar(100) NULL,
    [State] nvarchar(100) NULL,
    [PostalCode] nvarchar(20) NULL,
    [Country] nvarchar(100) NULL,
    [GooglePlaceId] nvarchar(500) NULL,
    [FormattedAddress] nvarchar(500) NULL,
    [InheritCustomerAddress] bit NOT NULL,
    [IsPrimary] bit NOT NULL,
    [IsActive] bit NOT NULL,
    [Notes] nvarchar(500) NULL,
    [CreatedDate] datetime2 NOT NULL,
    [LastModified] datetime2 NOT NULL,
    CONSTRAINT [PK_CustomerContacts] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_CustomerContacts_Customers_CustomerId] FOREIGN KEY ([CustomerId]) REFERENCES [Customers] ([Id]) ON DELETE CASCADE
);
GO


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
GO


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
GO


CREATE TABLE [Catalogues] (
    [Id] int NOT NULL IDENTITY,
    [Name] nvarchar(100) NOT NULL,
    [Description] nvarchar(500) NULL,
    [IsSystemCatalogue] bit NOT NULL,
    [CompanyId] int NOT NULL,
    [IsActive] bit NOT NULL,
    [CreatedDate] datetime2 NOT NULL,
    [CreatedBy] int NULL,
    [ModifiedDate] datetime2 NULL,
    [ModifiedBy] int NULL,
    [CreatorId] int NULL,
    [ModifierId] int NULL,
    CONSTRAINT [PK_Catalogues] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_Catalogues_Companies_CompanyId] FOREIGN KEY ([CompanyId]) REFERENCES [Companies] ([Id]) ON DELETE CASCADE,
    CONSTRAINT [FK_Catalogues_Users_CreatorId] FOREIGN KEY ([CreatorId]) REFERENCES [Users] ([Id]),
    CONSTRAINT [FK_Catalogues_Users_ModifierId] FOREIGN KEY ([ModifierId]) REFERENCES [Users] ([Id])
);
GO


CREATE TABLE [CompanySharePointSettings] (
    [Id] int NOT NULL IDENTITY,
    [CompanyId] int NOT NULL,
    [TenantId] nvarchar(100) NOT NULL,
    [ClientId] nvarchar(100) NOT NULL,
    [ClientSecret] nvarchar(max) NOT NULL,
    [SiteUrl] nvarchar(500) NOT NULL,
    [DocumentLibrary] nvarchar(200) NOT NULL,
    [TakeoffsRootFolder] nvarchar(200) NOT NULL,
    [IsEnabled] bit NOT NULL,
    [UseMockData] bit NOT NULL,
    [MaxFileSizeMB] int NOT NULL,
    [IsClientSecretEncrypted] bit NOT NULL,
    [CreatedDate] datetime2 NOT NULL DEFAULT (getutcdate()),
    [LastModifiedDate] datetime2 NOT NULL DEFAULT (getutcdate()),
    [CreatedByUserId] int NULL,
    [LastModifiedByUserId] int NULL,
    CONSTRAINT [PK_CompanySharePointSettings] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_CompanySharePointSettings_Companies_CompanyId] FOREIGN KEY ([CompanyId]) REFERENCES [Companies] ([Id]) ON DELETE CASCADE,
    CONSTRAINT [FK_CompanySharePointSettings_Users_CreatedByUserId] FOREIGN KEY ([CreatedByUserId]) REFERENCES [Users] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_CompanySharePointSettings_Users_LastModifiedByUserId] FOREIGN KEY ([LastModifiedByUserId]) REFERENCES [Users] ([Id]) ON DELETE NO ACTION
);
GO


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
GO


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
GO


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
GO


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
    CONSTRAINT [PK_Projects] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_Projects_Customers_CustomerId] FOREIGN KEY ([CustomerId]) REFERENCES [Customers] ([Id]),
    CONSTRAINT [FK_Projects_Users_LastModifiedBy] FOREIGN KEY ([LastModifiedBy]) REFERENCES [Users] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_Projects_Users_OwnerId] FOREIGN KEY ([OwnerId]) REFERENCES [Users] ([Id]) ON DELETE NO ACTION
);
GO


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
GO


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
GO


CREATE TABLE [UserInvitations] (
    [Id] int NOT NULL IDENTITY,
    [Email] nvarchar(255) NOT NULL,
    [InvitedByUserId] int NOT NULL,
    [CompanyId] int NOT NULL,
    [Token] nvarchar(50) NOT NULL,
    [Status] int NOT NULL,
    [AuthMethod] int NOT NULL,
    [CreatedAt] datetime2 NOT NULL,
    [ExpiresAt] datetime2 NOT NULL,
    [AcceptedAt] datetime2 NULL,
    [InvitedById] int NULL,
    CONSTRAINT [PK_UserInvitations] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_UserInvitations_Companies_CompanyId] FOREIGN KEY ([CompanyId]) REFERENCES [Companies] ([Id]) ON DELETE CASCADE,
    CONSTRAINT [FK_UserInvitations_Users_InvitedById] FOREIGN KEY ([InvitedById]) REFERENCES [Users] ([Id])
);
GO


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
GO


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
GO


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
GO


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
GO


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
    [CatalogueId] int NOT NULL,
    CONSTRAINT [PK_CatalogueItems] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_CatalogueItems_Catalogues_CatalogueId] FOREIGN KEY ([CatalogueId]) REFERENCES [Catalogues] ([Id]) ON DELETE CASCADE,
    CONSTRAINT [FK_CatalogueItems_Companies_CompanyId] FOREIGN KEY ([CompanyId]) REFERENCES [Companies] ([Id]) ON DELETE CASCADE
);
GO


CREATE TABLE [TraceDrawings] (
    [Id] int NOT NULL IDENTITY,
    [CompanyId] int NOT NULL,
    [ProjectId] int NULL,
    [TakeoffNumber] nvarchar(50) NULL,
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
    [ContactId] int NULL,
    [ContactPerson] nvarchar(200) NULL,
    [TraceName] nvarchar(200) NULL,
    [ProjectName] nvarchar(200) NULL,
    [ClientName] nvarchar(200) NULL,
    [Status] nvarchar(50) NULL,
    [Description] nvarchar(1000) NULL,
    [OcrConfidence] int NULL,
    [ProjectArchitect] nvarchar(200) NULL,
    [ProjectEngineer] nvarchar(200) NULL,
    CONSTRAINT [PK_TraceDrawings] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_TraceDrawings_Companies_CompanyId] FOREIGN KEY ([CompanyId]) REFERENCES [Companies] ([Id]) ON DELETE CASCADE,
    CONSTRAINT [FK_TraceDrawings_Customers_CustomerId] FOREIGN KEY ([CustomerId]) REFERENCES [Customers] ([Id]),
    CONSTRAINT [FK_TraceDrawings_Projects_ProjectId] FOREIGN KEY ([ProjectId]) REFERENCES [Projects] ([Id]),
    CONSTRAINT [FK_TraceDrawings_Users_UploadedBy] FOREIGN KEY ([UploadedBy]) REFERENCES [Users] ([Id]) ON DELETE CASCADE
);
GO


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
GO


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
GO


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
GO


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
GO


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
GO


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
GO


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
GO


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
GO


CREATE TABLE [TakeoffRevisions] (
    [Id] int NOT NULL IDENTITY,
    [TakeoffId] int NOT NULL,
    [RevisionCode] nvarchar(5) NOT NULL,
    [IsActive] bit NOT NULL,
    [Description] nvarchar(500) NULL,
    [CopiedFromRevisionId] int NULL,
    [CreatedBy] int NULL,
    [CreatedDate] datetime2 NOT NULL,
    [LastModified] datetime2 NOT NULL,
    [IsDeleted] bit NOT NULL,
    CONSTRAINT [PK_TakeoffRevisions] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_TakeoffRevisions_TakeoffRevisions_CopiedFromRevisionId] FOREIGN KEY ([CopiedFromRevisionId]) REFERENCES [TakeoffRevisions] ([Id]),
    CONSTRAINT [FK_TakeoffRevisions_TraceDrawings_TakeoffId] FOREIGN KEY ([TakeoffId]) REFERENCES [TraceDrawings] ([Id]) ON DELETE CASCADE,
    CONSTRAINT [FK_TakeoffRevisions_Users_CreatedBy] FOREIGN KEY ([CreatedBy]) REFERENCES [Users] ([Id])
);
GO


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
GO


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
GO


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
GO


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
GO


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
GO


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
GO


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
GO


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
GO


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
    CONSTRAINT [PK_Calibrations] PRIMARY KEY ([Id])
);
GO


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
GO


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
GO


CREATE TABLE [InspectionTestPlans] (
    [Id] int NOT NULL IDENTITY,
    [ITPNumber] nvarchar(50) NOT NULL,
    [OrderId] int NOT NULL,
    [PackageId] int NOT NULL,
    [WorkOrderId] int NULL,
    [Title] nvarchar(200) NOT NULL,
    [Description] nvarchar(2000) NULL,
    [TemplateId] int NULL,
    [CustomerRequirements] nvarchar(1000) NULL,
    [ApplicableStandards] nvarchar(500) NULL,
    [Status] nvarchar(20) NOT NULL DEFAULT N'Draft',
    [CreatedDate] datetime2 NOT NULL DEFAULT (getutcdate()),
    [ApprovedDate] datetime2 NULL,
    [ApprovedById] int NULL,
    [CompletedDate] datetime2 NULL,
    [CreatedBy] int NOT NULL,
    [CompanyId] int NOT NULL,
    CONSTRAINT [PK_InspectionTestPlans] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_InspectionTestPlans_Companies_CompanyId] FOREIGN KEY ([CompanyId]) REFERENCES [Companies] ([Id]) ON DELETE CASCADE,
    CONSTRAINT [FK_InspectionTestPlans_Users_ApprovedById] FOREIGN KEY ([ApprovedById]) REFERENCES [Users] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_InspectionTestPlans_Users_CreatedBy] FOREIGN KEY ([CreatedBy]) REFERENCES [Users] ([Id]) ON DELETE NO ACTION
);
GO


CREATE TABLE [ITPAssemblies] (
    [Id] int NOT NULL IDENTITY,
    [ITPId] int NOT NULL,
    [WorkOrderAssemblyId] int NOT NULL,
    [AssemblyId] int NOT NULL,
    [QuantityToBuild] int NOT NULL,
    [QuantityInspected] int NOT NULL,
    [QuantityPassed] int NOT NULL,
    [QuantityFailed] int NOT NULL,
    CONSTRAINT [PK_ITPAssemblies] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_ITPAssemblies_Assemblies_AssemblyId] FOREIGN KEY ([AssemblyId]) REFERENCES [Assemblies] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_ITPAssemblies_InspectionTestPlans_ITPId] FOREIGN KEY ([ITPId]) REFERENCES [InspectionTestPlans] ([Id]) ON DELETE CASCADE
);
GO


CREATE TABLE [ITPInspectionPoints] (
    [Id] int NOT NULL IDENTITY,
    [ITPId] int NOT NULL,
    [Sequence] int NOT NULL,
    [ActivityName] nvarchar(200) NOT NULL,
    [ActivityDescription] nvarchar(1000) NULL,
    [ReferenceStandard] nvarchar(200) NULL,
    [AcceptanceCriteria] nvarchar(1000) NULL,
    [InspectionType] nvarchar(20) NOT NULL DEFAULT N'Review',
    [ClientLevel] nvarchar(10) NULL,
    [ContractorLevel] nvarchar(10) NULL,
    [ThirdPartyLevel] nvarchar(10) NULL,
    [RequiredDocuments] nvarchar(1000) NULL,
    [RequiredTests] nvarchar(1000) NULL,
    [WorkOrderOperationId] int NULL,
    [AssemblyId] int NULL,
    [IsHoldPoint] bit NOT NULL,
    [HoldReleasedDate] datetime2 NULL,
    [ReleasedById] int NULL,
    [ReleaseComments] nvarchar(1000) NULL,
    [Status] nvarchar(20) NOT NULL DEFAULT N'NotStarted',
    [ScheduledDate] datetime2 NULL,
    [ActualDate] datetime2 NULL,
    [InspectorId] int NULL,
    [InspectorName] nvarchar(100) NULL,
    [Result] nvarchar(20) NULL,
    [ResultComments] nvarchar(2000) NULL,
    [NCRNumber] nvarchar(50) NULL,
    [InspectionReportId] int NULL,
    [HoldReleaseDocumentId] int NULL,
    CONSTRAINT [PK_ITPInspectionPoints] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_ITPInspectionPoints_Assemblies_AssemblyId] FOREIGN KEY ([AssemblyId]) REFERENCES [Assemblies] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_ITPInspectionPoints_InspectionTestPlans_ITPId] FOREIGN KEY ([ITPId]) REFERENCES [InspectionTestPlans] ([Id]) ON DELETE CASCADE,
    CONSTRAINT [FK_ITPInspectionPoints_Users_InspectorId] FOREIGN KEY ([InspectorId]) REFERENCES [Users] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_ITPInspectionPoints_Users_ReleasedById] FOREIGN KEY ([ReleasedById]) REFERENCES [Users] ([Id]) ON DELETE NO ACTION
);
GO


CREATE TABLE [MaterialTraceability] (
    [Id] int NOT NULL IDENTITY,
    [TraceNumber] nvarchar(50) NOT NULL,
    [CatalogueItemId] int NOT NULL,
    [InventoryItemId] int NULL,
    [WorkOrderInventoryItemId] int NULL,
    [MaterialCode] nvarchar(100) NOT NULL,
    [Description] nvarchar(500) NULL,
    [HeatNumber] nvarchar(100) NOT NULL,
    [BatchNumber] nvarchar(100) NULL,
    [MillCertificateNumber] nvarchar(100) NULL,
    [MillDate] datetime2 NULL,
    [Supplier] nvarchar(200) NULL,
    [SupplierBatchNumber] nvarchar(100) NULL,
    [Carbon] decimal(6,4) NULL,
    [Manganese] decimal(6,4) NULL,
    [Silicon] decimal(6,4) NULL,
    [Phosphorus] decimal(6,4) NULL,
    [Sulfur] decimal(6,4) NULL,
    [Chromium] decimal(6,4) NULL,
    [Nickel] decimal(6,4) NULL,
    [Molybdenum] decimal(6,4) NULL,
    [OtherElements] nvarchar(1000) NULL,
    [YieldStrength] decimal(10,2) NULL,
    [TensileStrength] decimal(10,2) NULL,
    [Elongation] decimal(6,2) NULL,
    [ReductionOfArea] decimal(6,2) NULL,
    [Hardness] decimal(10,2) NULL,
    [HardnessScale] nvarchar(20) NULL,
    [ImpactValue] decimal(10,2) NULL,
    [ImpactTemperature] decimal(6,2) NULL,
    [ImpactType] nvarchar(50) NULL,
    [ParentTraceId] int NULL,
    [CreatedDate] datetime2 NOT NULL DEFAULT (getutcdate()),
    [CompanyId] int NOT NULL,
    CONSTRAINT [PK_MaterialTraceability] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_MaterialTraceability_CatalogueItems_CatalogueItemId] FOREIGN KEY ([CatalogueItemId]) REFERENCES [CatalogueItems] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_MaterialTraceability_Companies_CompanyId] FOREIGN KEY ([CompanyId]) REFERENCES [Companies] ([Id]) ON DELETE CASCADE,
    CONSTRAINT [FK_MaterialTraceability_InventoryItems_InventoryItemId] FOREIGN KEY ([InventoryItemId]) REFERENCES [InventoryItems] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_MaterialTraceability_MaterialTraceability_ParentTraceId] FOREIGN KEY ([ParentTraceId]) REFERENCES [MaterialTraceability] ([Id]) ON DELETE NO ACTION
);
GO


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
GO


CREATE TABLE [Packages] (
    [Id] int NOT NULL IDENTITY,
    [CompanyId] int NOT NULL,
    [ProjectId] int NULL,
    [OrderId] int NULL,
    [RevisionId] int NULL,
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
    CONSTRAINT [FK_Packages_Companies_CompanyId] FOREIGN KEY ([CompanyId]) REFERENCES [Companies] ([Id]) ON DELETE CASCADE,
    CONSTRAINT [FK_Packages_EfficiencyRates_EfficiencyRateId] FOREIGN KEY ([EfficiencyRateId]) REFERENCES [EfficiencyRates] ([Id]),
    CONSTRAINT [FK_Packages_Orders_OrderId] FOREIGN KEY ([OrderId]) REFERENCES [Orders] ([Id]),
    CONSTRAINT [FK_Packages_Projects_ProjectId] FOREIGN KEY ([ProjectId]) REFERENCES [Projects] ([Id]),
    CONSTRAINT [FK_Packages_RoutingTemplates_RoutingId] FOREIGN KEY ([RoutingId]) REFERENCES [RoutingTemplates] ([Id]),
    CONSTRAINT [FK_Packages_TakeoffRevisions_RevisionId] FOREIGN KEY ([RevisionId]) REFERENCES [TakeoffRevisions] ([Id]),
    CONSTRAINT [FK_Packages_Users_CreatedBy] FOREIGN KEY ([CreatedBy]) REFERENCES [Users] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_Packages_Users_LastModifiedBy] FOREIGN KEY ([LastModifiedBy]) REFERENCES [Users] ([Id]) ON DELETE NO ACTION
);
GO


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
GO


CREATE TABLE [WorkPackages] (
    [Id] int NOT NULL IDENTITY,
    [PackageNumber] nvarchar(50) NOT NULL,
    [PackageName] nvarchar(200) NOT NULL,
    [OrderId] int NOT NULL,
    [Description] nvarchar(2000) NULL,
    [Priority] nvarchar(20) NOT NULL,
    [PackageType] nvarchar(50) NULL,
    [PlannedStartDate] datetime2 NULL,
    [PlannedEndDate] datetime2 NULL,
    [ActualStartDate] datetime2 NULL,
    [ActualEndDate] datetime2 NULL,
    [EstimatedHours] decimal(18,2) NOT NULL,
    [ActualHours] decimal(18,2) NOT NULL,
    [EstimatedCost] decimal(18,2) NOT NULL,
    [ActualCost] decimal(18,2) NOT NULL,
    [LaborRatePerHour] decimal(10,2) NOT NULL,
    [Status] nvarchar(20) NOT NULL,
    [PercentComplete] decimal(5,2) NOT NULL,
    [RequiresITP] bit NOT NULL,
    [ITPNumber] nvarchar(50) NULL,
    [CreatedDate] datetime2 NOT NULL,
    [LastModified] datetime2 NOT NULL,
    [IsDeleted] bit NOT NULL,
    [CompanyId] int NOT NULL,
    CONSTRAINT [PK_WorkPackages] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_WorkPackages_Companies_CompanyId] FOREIGN KEY ([CompanyId]) REFERENCES [Companies] ([Id]) ON DELETE CASCADE,
    CONSTRAINT [FK_WorkPackages_Orders_OrderId] FOREIGN KEY ([OrderId]) REFERENCES [Orders] ([Id]) ON DELETE CASCADE
);
GO


CREATE TABLE [PackageDrawings] (
    [Id] int NOT NULL IDENTITY,
    [PackageId] int NOT NULL,
    [DrawingNumber] nvarchar(100) NOT NULL,
    [DrawingTitle] nvarchar(500) NULL,
    [SharePointItemId] nvarchar(500) NOT NULL,
    [SharePointUrl] nvarchar(1000) NOT NULL,
    [StorageProvider] nvarchar(50) NULL,
    [ProviderFileId] nvarchar(500) NULL,
    [ProviderMetadata] nvarchar(max) NULL,
    [FileType] nvarchar(50) NOT NULL,
    [FileSize] bigint NOT NULL,
    [UploadedDate] datetime2 NOT NULL,
    [UploadedBy] int NOT NULL,
    [IsActive] bit NOT NULL,
    [Notes] nvarchar(500) NULL,
    [InstantJson] nvarchar(max) NULL,
    [InstantJsonLastUpdated] datetime2 NULL,
    [CalibrationConfig] nvarchar(max) NULL,
    [CalibrationConfigLastUpdated] datetime2 NULL,
    CONSTRAINT [PK_PackageDrawings] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_PackageDrawings_Packages_PackageId] FOREIGN KEY ([PackageId]) REFERENCES [Packages] ([Id]) ON DELETE CASCADE,
    CONSTRAINT [FK_PackageDrawings_Users_UploadedBy] FOREIGN KEY ([UploadedBy]) REFERENCES [Users] ([Id]) ON DELETE CASCADE
);
GO


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
    [Size] nvarchar(20) NOT NULL,
    [PackageId] int NULL,
    CONSTRAINT [PK_WeldingConnections] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_WeldingConnections_Packages_PackageId] FOREIGN KEY ([PackageId]) REFERENCES [Packages] ([Id])
);
GO


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
GO


CREATE TABLE [WorkOrders] (
    [Id] int NOT NULL IDENTITY,
    [WorkOrderNumber] nvarchar(50) NOT NULL,
    [PackageId] int NOT NULL,
    [WorkOrderType] nvarchar(50) NOT NULL,
    [Description] nvarchar(2000) NULL,
    [WorkCenterId] int NULL,
    [PrimaryResourceId] int NULL,
    [Priority] nvarchar(20) NOT NULL,
    [ScheduledStartDate] datetime2 NULL,
    [ScheduledEndDate] datetime2 NULL,
    [ActualStartDate] datetime2 NULL,
    [ActualEndDate] datetime2 NULL,
    [EstimatedHours] decimal(18,2) NOT NULL,
    [ActualHours] decimal(18,2) NOT NULL,
    [EstimatedCost] decimal(18,2) NOT NULL,
    [ActualCost] decimal(18,2) NOT NULL,
    [Status] nvarchar(20) NOT NULL DEFAULT N'Planning',
    [PercentComplete] decimal(5,2) NOT NULL,
    [Barcode] nvarchar(100) NULL,
    [HasHoldPoints] bit NOT NULL,
    [RequiresInspection] bit NOT NULL,
    [InspectionStatus] nvarchar(20) NULL,
    [WorkInstructions] nvarchar(max) NULL,
    [CreatedDate] datetime2 NOT NULL DEFAULT (getutcdate()),
    [LastModified] datetime2 NOT NULL,
    [CompletedDate] datetime2 NULL,
    [CompanyId] int NOT NULL,
    [PackageId1] int NOT NULL,
    CONSTRAINT [PK_WorkOrders] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_WorkOrders_Companies_CompanyId] FOREIGN KEY ([CompanyId]) REFERENCES [Companies] ([Id]) ON DELETE CASCADE,
    CONSTRAINT [FK_WorkOrders_Packages_PackageId1] FOREIGN KEY ([PackageId1]) REFERENCES [Packages] ([Id]) ON DELETE CASCADE,
    CONSTRAINT [FK_WorkOrders_Resources_PrimaryResourceId] FOREIGN KEY ([PrimaryResourceId]) REFERENCES [Resources] ([Id]),
    CONSTRAINT [FK_WorkOrders_WorkCenters_WorkCenterId] FOREIGN KEY ([WorkCenterId]) REFERENCES [WorkCenters] ([Id]),
    CONSTRAINT [FK_WorkOrders_WorkPackages_PackageId] FOREIGN KEY ([PackageId]) REFERENCES [WorkPackages] ([Id]) ON DELETE CASCADE
);
GO


CREATE TABLE [PdfEditLocks] (
    [Id] int NOT NULL IDENTITY,
    [PackageDrawingId] int NOT NULL,
    [SessionId] nvarchar(255) NOT NULL,
    [UserId] int NOT NULL,
    [UserName] nvarchar(255) NOT NULL,
    [LockedAt] datetime2 NOT NULL DEFAULT (getutcdate()),
    [LastHeartbeat] datetime2 NOT NULL DEFAULT (getutcdate()),
    [LastActivityAt] datetime2 NOT NULL DEFAULT (getutcdate()),
    [IsActive] bit NOT NULL DEFAULT CAST(1 AS bit),
    CONSTRAINT [PK_PdfEditLocks] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_PdfEditLocks_PackageDrawings_PackageDrawingId] FOREIGN KEY ([PackageDrawingId]) REFERENCES [PackageDrawings] ([Id]) ON DELETE CASCADE,
    CONSTRAINT [FK_PdfEditLocks_Users_UserId] FOREIGN KEY ([UserId]) REFERENCES [Users] ([Id]) ON DELETE NO ACTION
);
GO


CREATE TABLE [PdfScaleCalibrations] (
    [Id] int NOT NULL IDENTITY,
    [PackageDrawingId] int NOT NULL,
    [Scale] decimal(10,2) NOT NULL,
    [Unit] nvarchar(10) NOT NULL,
    [KnownDistance] decimal(18,4) NULL,
    [MeasuredDistance] decimal(18,4) NULL,
    [PageIndex] int NOT NULL,
    [CalibrationLineStart] nvarchar(200) NULL,
    [CalibrationLineEnd] nvarchar(200) NULL,
    [CreatedByUserId] int NULL,
    [CreatedDate] datetime2 NOT NULL DEFAULT (getutcdate()),
    [ModifiedDate] datetime2 NULL,
    [CompanyId] int NOT NULL,
    CONSTRAINT [PK_PdfScaleCalibrations] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_PdfScaleCalibrations_Companies_CompanyId] FOREIGN KEY ([CompanyId]) REFERENCES [Companies] ([Id]) ON DELETE CASCADE,
    CONSTRAINT [FK_PdfScaleCalibrations_PackageDrawings_PackageDrawingId] FOREIGN KEY ([PackageDrawingId]) REFERENCES [PackageDrawings] ([Id]) ON DELETE CASCADE,
    CONSTRAINT [FK_PdfScaleCalibrations_Users_CreatedByUserId] FOREIGN KEY ([CreatedByUserId]) REFERENCES [Users] ([Id]) ON DELETE NO ACTION
);
GO


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
    [SurfaceCoatingId] int NULL,
    [Status] nvarchar(50) NULL,
    [Notes] nvarchar(2000) NULL,
    [CreatedDate] datetime2 NOT NULL DEFAULT (getutcdate()),
    [ModifiedDate] datetime2 NULL,
    [CreatedBy] int NULL,
    [ModifiedBy] int NULL,
    CONSTRAINT [PK_TraceTakeoffMeasurements] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_TraceTakeoffMeasurements_CatalogueItems_CatalogueItemId] FOREIGN KEY ([CatalogueItemId]) REFERENCES [CatalogueItems] ([Id]),
    CONSTRAINT [FK_TraceTakeoffMeasurements_PackageDrawings_PackageDrawingId] FOREIGN KEY ([PackageDrawingId]) REFERENCES [PackageDrawings] ([Id]),
    CONSTRAINT [FK_TraceTakeoffMeasurements_SurfaceCoatings_SurfaceCoatingId] FOREIGN KEY ([SurfaceCoatingId]) REFERENCES [SurfaceCoatings] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_TraceTakeoffMeasurements_TraceTakeoffs_TraceTakeoffId] FOREIGN KEY ([TraceTakeoffId]) REFERENCES [TraceTakeoffs] ([Id]) ON DELETE CASCADE,
    CONSTRAINT [FK_TraceTakeoffMeasurements_Users_CreatedBy] FOREIGN KEY ([CreatedBy]) REFERENCES [Users] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_TraceTakeoffMeasurements_Users_ModifiedBy] FOREIGN KEY ([ModifiedBy]) REFERENCES [Users] ([Id]) ON DELETE NO ACTION
);
GO


CREATE TABLE [QualityDocuments] (
    [Id] int NOT NULL IDENTITY,
    [DocumentNumber] nvarchar(50) NOT NULL,
    [DocumentType] nvarchar(50) NOT NULL,
    [OrderId] int NULL,
    [PackageId] int NULL,
    [WorkOrderId] int NULL,
    [AssemblyId] int NULL,
    [MaterialTraceabilityId] int NULL,
    [ITPInspectionPointId] int NULL,
    [FileName] nvarchar(500) NOT NULL,
    [Title] nvarchar(200) NULL,
    [Description] nvarchar(2000) NULL,
    [Standard] nvarchar(200) NULL,
    [DocumentDate] datetime2 NOT NULL,
    [CloudProvider] nvarchar(50) NOT NULL,
    [CloudFileId] nvarchar(500) NULL,
    [CloudFilePath] nvarchar(1000) NULL,
    [CloudUrl] nvarchar(200) NULL,
    [FileSizeKB] decimal(18,4) NULL,
    [MimeType] nvarchar(50) NULL,
    [Status] nvarchar(20) NOT NULL DEFAULT N'Draft',
    [ApprovedBy] nvarchar(200) NULL,
    [ApprovedDate] datetime2 NULL,
    [ApprovedById] int NULL,
    [ApprovalComments] nvarchar(1000) NULL,
    [Version] int NOT NULL DEFAULT 1,
    [SupersededById] int NULL,
    [CreatedDate] datetime2 NOT NULL DEFAULT (getutcdate()),
    [CreatedBy] int NOT NULL,
    [ModifiedDate] datetime2 NULL,
    [CompanyId] int NOT NULL,
    CONSTRAINT [PK_QualityDocuments] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_QualityDocuments_Assemblies_AssemblyId] FOREIGN KEY ([AssemblyId]) REFERENCES [Assemblies] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_QualityDocuments_Companies_CompanyId] FOREIGN KEY ([CompanyId]) REFERENCES [Companies] ([Id]) ON DELETE CASCADE,
    CONSTRAINT [FK_QualityDocuments_ITPInspectionPoints_ITPInspectionPointId] FOREIGN KEY ([ITPInspectionPointId]) REFERENCES [ITPInspectionPoints] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_QualityDocuments_MaterialTraceability_MaterialTraceabilityId] FOREIGN KEY ([MaterialTraceabilityId]) REFERENCES [MaterialTraceability] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_QualityDocuments_Orders_OrderId] FOREIGN KEY ([OrderId]) REFERENCES [Orders] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_QualityDocuments_Packages_PackageId] FOREIGN KEY ([PackageId]) REFERENCES [Packages] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_QualityDocuments_QualityDocuments_SupersededById] FOREIGN KEY ([SupersededById]) REFERENCES [QualityDocuments] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_QualityDocuments_Users_ApprovedById] FOREIGN KEY ([ApprovedById]) REFERENCES [Users] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_QualityDocuments_Users_CreatedBy] FOREIGN KEY ([CreatedBy]) REFERENCES [Users] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_QualityDocuments_WorkOrders_WorkOrderId] FOREIGN KEY ([WorkOrderId]) REFERENCES [WorkOrders] ([Id]) ON DELETE NO ACTION
);
GO


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
GO


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
GO


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
GO


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
GO


CREATE TABLE [PdfAnnotations] (
    [Id] int NOT NULL IDENTITY,
    [PackageDrawingId] int NOT NULL,
    [AnnotationId] nvarchar(100) NOT NULL,
    [AnnotationType] nvarchar(100) NOT NULL,
    [PageIndex] int NOT NULL,
    [InstantJson] nvarchar(max) NOT NULL,
    [IsMeasurement] bit NOT NULL,
    [IsCalibration] bit NOT NULL,
    [TraceTakeoffMeasurementId] int NULL,
    [MeasurementValue] nvarchar(100) NULL,
    [MeasurementScale] nvarchar(500) NULL,
    [MeasurementPrecision] nvarchar(20) NULL,
    [CoordinatesData] nvarchar(max) NULL,
    [CreatedByUserId] int NULL,
    [CreatedDate] datetime2 NOT NULL DEFAULT (getutcdate()),
    [ModifiedDate] datetime2 NULL,
    [CompanyId] int NOT NULL,
    CONSTRAINT [PK_PdfAnnotations] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_PdfAnnotations_Companies_CompanyId] FOREIGN KEY ([CompanyId]) REFERENCES [Companies] ([Id]) ON DELETE CASCADE,
    CONSTRAINT [FK_PdfAnnotations_PackageDrawings_PackageDrawingId] FOREIGN KEY ([PackageDrawingId]) REFERENCES [PackageDrawings] ([Id]) ON DELETE CASCADE,
    CONSTRAINT [FK_PdfAnnotations_TraceTakeoffMeasurements_TraceTakeoffMeasurementId] FOREIGN KEY ([TraceTakeoffMeasurementId]) REFERENCES [TraceTakeoffMeasurements] ([Id]) ON DELETE SET NULL,
    CONSTRAINT [FK_PdfAnnotations_Users_CreatedByUserId] FOREIGN KEY ([CreatedByUserId]) REFERENCES [Users] ([Id]) ON DELETE NO ACTION
);
GO


CREATE TABLE [TestResults] (
    [Id] int NOT NULL IDENTITY,
    [TestNumber] nvarchar(50) NOT NULL,
    [QualityDocumentId] int NOT NULL,
    [ITPInspectionPointId] int NULL,
    [MaterialTraceabilityId] int NULL,
    [TestType] nvarchar(50) NOT NULL,
    [TestStandard] nvarchar(200) NULL,
    [TestProcedure] nvarchar(200) NULL,
    [TestLocation] nvarchar(100) NULL,
    [HeatNumber] nvarchar(100) NULL,
    [SampleId] nvarchar(100) NULL,
    [TestParameters] nvarchar(4000) NULL,
    [AmbientTemperature] decimal(6,2) NULL,
    [Humidity] decimal(6,2) NULL,
    [AtmosphericPressure] decimal(8,2) NULL,
    [EnvironmentalNotes] nvarchar(500) NULL,
    [EquipmentId] nvarchar(100) NULL,
    [EquipmentName] nvarchar(200) NULL,
    [CalibrationCertificate] nvarchar(100) NULL,
    [CalibrationExpiry] datetime2 NULL,
    [EquipmentSettings] nvarchar(1000) NULL,
    [TesterId] int NOT NULL,
    [TesterName] nvarchar(100) NULL,
    [TesterQualification] nvarchar(200) NULL,
    [TesterCertification] nvarchar(100) NULL,
    [CertificationExpiry] datetime2 NULL,
    [WitnessId] int NULL,
    [WitnessName] nvarchar(100) NULL,
    [WitnessOrganization] nvarchar(200) NULL,
    [TestStartTime] datetime2 NOT NULL,
    [TestEndTime] datetime2 NOT NULL,
    [NumberOfSamples] int NOT NULL DEFAULT 1,
    [NumberOfRetests] int NOT NULL DEFAULT 0,
    [Result] nvarchar(20) NOT NULL DEFAULT N'Pending',
    [ResultSummary] nvarchar(1000) NULL,
    [DetailedResults] nvarchar(4000) NULL,
    [CreatedDate] datetime2 NOT NULL DEFAULT (getutcdate()),
    [CompanyId] int NOT NULL,
    CONSTRAINT [PK_TestResults] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_TestResults_Companies_CompanyId] FOREIGN KEY ([CompanyId]) REFERENCES [Companies] ([Id]) ON DELETE CASCADE,
    CONSTRAINT [FK_TestResults_ITPInspectionPoints_ITPInspectionPointId] FOREIGN KEY ([ITPInspectionPointId]) REFERENCES [ITPInspectionPoints] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_TestResults_MaterialTraceability_MaterialTraceabilityId] FOREIGN KEY ([MaterialTraceabilityId]) REFERENCES [MaterialTraceability] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_TestResults_QualityDocuments_QualityDocumentId] FOREIGN KEY ([QualityDocumentId]) REFERENCES [QualityDocuments] ([Id]) ON DELETE CASCADE,
    CONSTRAINT [FK_TestResults_Users_TesterId] FOREIGN KEY ([TesterId]) REFERENCES [Users] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_TestResults_Users_WitnessId] FOREIGN KEY ([WitnessId]) REFERENCES [Users] ([Id]) ON DELETE NO ACTION
);
GO


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
GO


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
GO


CREATE UNIQUE INDEX [IX_Assemblies_AssemblyNumber] ON [Assemblies] ([AssemblyNumber]);
GO


CREATE INDEX [IX_Assemblies_CompanyId] ON [Assemblies] ([CompanyId]);
GO


CREATE INDEX [IX_Assemblies_ParentAssemblyId] ON [Assemblies] ([ParentAssemblyId]);
GO


CREATE INDEX [IX_AssemblyComponents_AssemblyId] ON [AssemblyComponents] ([AssemblyId]);
GO


CREATE INDEX [IX_AssemblyComponents_CatalogueItemId] ON [AssemblyComponents] ([CatalogueItemId]);
GO


CREATE INDEX [IX_AssemblyComponents_ComponentAssemblyId] ON [AssemblyComponents] ([ComponentAssemblyId]);
GO


CREATE INDEX [IX_AuthAuditLogs_Email_Timestamp] ON [AuthAuditLogs] ([Email], [Timestamp]);
GO


CREATE INDEX [IX_AuthAuditLogs_Timestamp] ON [AuthAuditLogs] ([Timestamp]);
GO


CREATE INDEX [IX_AuthAuditLogs_UserId] ON [AuthAuditLogs] ([UserId]);
GO


CREATE INDEX [IX_Calibrations_PackageDrawingId] ON [Calibrations] ([PackageDrawingId]);
GO


CREATE INDEX [IX_CatalogueItems_CatalogueId] ON [CatalogueItems] ([CatalogueId]);
GO


CREATE INDEX [IX_CatalogueItems_Category] ON [CatalogueItems] ([Category]);
GO


CREATE INDEX [IX_CatalogueItems_CompanyId] ON [CatalogueItems] ([CompanyId]);
GO


CREATE INDEX [IX_CatalogueItems_Finish] ON [CatalogueItems] ([Finish]);
GO


CREATE INDEX [IX_CatalogueItems_Grade] ON [CatalogueItems] ([Grade]);
GO


CREATE UNIQUE INDEX [IX_CatalogueItems_ItemCode] ON [CatalogueItems] ([ItemCode]);
GO


CREATE INDEX [IX_CatalogueItems_Material] ON [CatalogueItems] ([Material]);
GO


CREATE INDEX [IX_CatalogueItems_Profile] ON [CatalogueItems] ([Profile]);
GO


CREATE INDEX [IX_Catalogues_CompanyId] ON [Catalogues] ([CompanyId]);
GO


CREATE INDEX [IX_Catalogues_CreatorId] ON [Catalogues] ([CreatorId]);
GO


CREATE INDEX [IX_Catalogues_ModifierId] ON [Catalogues] ([ModifierId]);
GO


CREATE UNIQUE INDEX [IX_Companies_Code] ON [Companies] ([Code]);
GO


CREATE UNIQUE INDEX [IX_CompanySharePointSettings_CompanyId] ON [CompanySharePointSettings] ([CompanyId]);
GO


CREATE INDEX [IX_CompanySharePointSettings_CreatedByUserId] ON [CompanySharePointSettings] ([CreatedByUserId]);
GO


CREATE INDEX [IX_CompanySharePointSettings_LastModifiedByUserId] ON [CompanySharePointSettings] ([LastModifiedByUserId]);
GO


CREATE INDEX [IX_CustomerAddresses_CustomerId] ON [CustomerAddresses] ([CustomerId]);
GO


CREATE INDEX [IX_CustomerContacts_CustomerId] ON [CustomerContacts] ([CustomerId]);
GO


CREATE INDEX [IX_EstimationPackages_EstimationId] ON [EstimationPackages] ([EstimationId]);
GO


CREATE INDEX [IX_Estimations_ApprovedBy] ON [Estimations] ([ApprovedBy]);
GO


CREATE INDEX [IX_Estimations_CreatedBy] ON [Estimations] ([CreatedBy]);
GO


CREATE INDEX [IX_Estimations_CustomerId] ON [Estimations] ([CustomerId]);
GO


CREATE INDEX [IX_Estimations_LastModifiedBy] ON [Estimations] ([LastModifiedBy]);
GO


CREATE INDEX [IX_Estimations_OrderId] ON [Estimations] ([OrderId]);
GO


CREATE INDEX [IX_GlobalSettings_LastModifiedByUserId] ON [GlobalSettings] ([LastModifiedByUserId]);
GO


CREATE UNIQUE INDEX [IX_GratingSpecifications_CatalogueItemId] ON [GratingSpecifications] ([CatalogueItemId]);
GO


CREATE INDEX [IX_InspectionTestPlans_ApprovedById] ON [InspectionTestPlans] ([ApprovedById]);
GO


CREATE INDEX [IX_InspectionTestPlans_CompanyId] ON [InspectionTestPlans] ([CompanyId]);
GO


CREATE INDEX [IX_InspectionTestPlans_CreatedBy] ON [InspectionTestPlans] ([CreatedBy]);
GO


CREATE UNIQUE INDEX [IX_InspectionTestPlans_ITPNumber] ON [InspectionTestPlans] ([ITPNumber]);
GO


CREATE INDEX [IX_InspectionTestPlans_OrderId] ON [InspectionTestPlans] ([OrderId]);
GO


CREATE INDEX [IX_InspectionTestPlans_PackageId] ON [InspectionTestPlans] ([PackageId]);
GO


CREATE INDEX [IX_InspectionTestPlans_WorkOrderId] ON [InspectionTestPlans] ([WorkOrderId]);
GO


CREATE INDEX [IX_InventoryItems_CatalogueItemId] ON [InventoryItems] ([CatalogueItemId]);
GO


CREATE INDEX [IX_InventoryItems_CompanyId] ON [InventoryItems] ([CompanyId]);
GO


CREATE INDEX [IX_InventoryItems_HeatNumber] ON [InventoryItems] ([HeatNumber]);
GO


CREATE UNIQUE INDEX [IX_InventoryItems_InventoryCode] ON [InventoryItems] ([InventoryCode]);
GO


CREATE INDEX [IX_InventoryItems_LotNumber] ON [InventoryItems] ([LotNumber]);
GO


CREATE INDEX [IX_InventoryTransactions_CompanyId] ON [InventoryTransactions] ([CompanyId]);
GO


CREATE INDEX [IX_InventoryTransactions_InventoryItemId] ON [InventoryTransactions] ([InventoryItemId]);
GO


CREATE UNIQUE INDEX [IX_InventoryTransactions_TransactionNumber] ON [InventoryTransactions] ([TransactionNumber]);
GO


CREATE INDEX [IX_InventoryTransactions_UserId] ON [InventoryTransactions] ([UserId]);
GO


CREATE INDEX [IX_ITPAssemblies_AssemblyId] ON [ITPAssemblies] ([AssemblyId]);
GO


CREATE INDEX [IX_ITPAssemblies_ITPId] ON [ITPAssemblies] ([ITPId]);
GO


CREATE INDEX [IX_ITPAssemblies_WorkOrderAssemblyId] ON [ITPAssemblies] ([WorkOrderAssemblyId]);
GO


CREATE INDEX [IX_ITPInspectionPoints_AssemblyId] ON [ITPInspectionPoints] ([AssemblyId]);
GO


CREATE INDEX [IX_ITPInspectionPoints_InspectorId] ON [ITPInspectionPoints] ([InspectorId]);
GO


CREATE INDEX [IX_ITPInspectionPoints_ITPId] ON [ITPInspectionPoints] ([ITPId]);
GO


CREATE INDEX [IX_ITPInspectionPoints_ReleasedById] ON [ITPInspectionPoints] ([ReleasedById]);
GO


CREATE INDEX [IX_ITPInspectionPoints_WorkOrderOperationId] ON [ITPInspectionPoints] ([WorkOrderOperationId]);
GO


CREATE INDEX [IX_MachineCapabilities_MachineCenterId] ON [MachineCapabilities] ([MachineCenterId]);
GO


CREATE INDEX [IX_MachineCenters_CompanyId] ON [MachineCenters] ([CompanyId]);
GO


CREATE INDEX [IX_MachineCenters_CreatedByUserId] ON [MachineCenters] ([CreatedByUserId]);
GO


CREATE INDEX [IX_MachineCenters_LastModifiedByUserId] ON [MachineCenters] ([LastModifiedByUserId]);
GO


CREATE UNIQUE INDEX [IX_MachineCenters_MachineCode] ON [MachineCenters] ([MachineCode]);
GO


CREATE INDEX [IX_MachineCenters_WorkCenterId] ON [MachineCenters] ([WorkCenterId]);
GO


CREATE INDEX [IX_MachineOperators_MachineCenterId] ON [MachineOperators] ([MachineCenterId]);
GO


CREATE INDEX [IX_MachineOperators_UserId] ON [MachineOperators] ([UserId]);
GO


CREATE INDEX [IX_MaterialTraceability_CatalogueItemId] ON [MaterialTraceability] ([CatalogueItemId]);
GO


CREATE INDEX [IX_MaterialTraceability_CompanyId] ON [MaterialTraceability] ([CompanyId]);
GO


CREATE INDEX [IX_MaterialTraceability_HeatNumber] ON [MaterialTraceability] ([HeatNumber]);
GO


CREATE INDEX [IX_MaterialTraceability_InventoryItemId] ON [MaterialTraceability] ([InventoryItemId]);
GO


CREATE INDEX [IX_MaterialTraceability_ParentTraceId] ON [MaterialTraceability] ([ParentTraceId]);
GO


CREATE UNIQUE INDEX [IX_MaterialTraceability_TraceNumber] ON [MaterialTraceability] ([TraceNumber]);
GO


CREATE INDEX [IX_MaterialTraceability_WorkOrderInventoryItemId] ON [MaterialTraceability] ([WorkOrderInventoryItemId]);
GO


CREATE INDEX [IX_ModuleSettings_CompanyId] ON [ModuleSettings] ([CompanyId]);
GO


CREATE INDEX [IX_ModuleSettings_CreatedByUserId] ON [ModuleSettings] ([CreatedByUserId]);
GO


CREATE INDEX [IX_ModuleSettings_LastModifiedByUserId] ON [ModuleSettings] ([LastModifiedByUserId]);
GO


CREATE INDEX [IX_ModuleSettings_UserId] ON [ModuleSettings] ([UserId]);
GO


CREATE INDEX [IX_NumberSeries_CreatedByUserId] ON [NumberSeries] ([CreatedByUserId]);
GO


CREATE INDEX [IX_NumberSeries_LastModifiedByUserId] ON [NumberSeries] ([LastModifiedByUserId]);
GO


CREATE INDEX [IX_Orders_CreatedBy] ON [Orders] ([CreatedBy]);
GO


CREATE INDEX [IX_Orders_CustomerId] ON [Orders] ([CustomerId]);
GO


CREATE INDEX [IX_Orders_EstimationId] ON [Orders] ([EstimationId]);
GO


CREATE INDEX [IX_Orders_LastModifiedBy] ON [Orders] ([LastModifiedBy]);
GO


CREATE INDEX [IX_Orders_QuoteId] ON [Orders] ([QuoteId]);
GO


CREATE INDEX [IX_PackageDrawings_PackageId] ON [PackageDrawings] ([PackageId]);
GO


CREATE INDEX [IX_PackageDrawings_UploadedBy] ON [PackageDrawings] ([UploadedBy]);
GO


CREATE INDEX [IX_Packages_CompanyId] ON [Packages] ([CompanyId]);
GO


CREATE INDEX [IX_Packages_CreatedBy] ON [Packages] ([CreatedBy]);
GO


CREATE INDEX [IX_Packages_EfficiencyRateId] ON [Packages] ([EfficiencyRateId]);
GO


CREATE INDEX [IX_Packages_LastModifiedBy] ON [Packages] ([LastModifiedBy]);
GO


CREATE INDEX [IX_Packages_OrderId] ON [Packages] ([OrderId]);
GO


CREATE INDEX [IX_Packages_ProjectId] ON [Packages] ([ProjectId]);
GO


CREATE INDEX [IX_Packages_RevisionId] ON [Packages] ([RevisionId]);
GO


CREATE INDEX [IX_Packages_RoutingId] ON [Packages] ([RoutingId]);
GO


CREATE INDEX [IX_PdfAnnotations_CompanyId] ON [PdfAnnotations] ([CompanyId]);
GO


CREATE INDEX [IX_PdfAnnotations_CreatedByUserId] ON [PdfAnnotations] ([CreatedByUserId]);
GO


CREATE INDEX [IX_PdfAnnotations_PackageDrawingId] ON [PdfAnnotations] ([PackageDrawingId]);
GO


CREATE INDEX [IX_PdfAnnotations_PackageDrawingId_AnnotationId] ON [PdfAnnotations] ([PackageDrawingId], [AnnotationId]);
GO


CREATE INDEX [IX_PdfAnnotations_TraceTakeoffMeasurementId] ON [PdfAnnotations] ([TraceTakeoffMeasurementId]);
GO


CREATE INDEX [IX_PdfEditLocks_LastHeartbeat] ON [PdfEditLocks] ([LastHeartbeat]) WHERE [IsActive] = 1;
GO


CREATE INDEX [IX_PdfEditLocks_PackageDrawingId_IsActive] ON [PdfEditLocks] ([PackageDrawingId], [IsActive]);
GO


CREATE INDEX [IX_PdfEditLocks_SessionId] ON [PdfEditLocks] ([SessionId]);
GO


CREATE INDEX [IX_PdfEditLocks_UserId] ON [PdfEditLocks] ([UserId]);
GO


CREATE INDEX [IX_PdfScaleCalibrations_CompanyId] ON [PdfScaleCalibrations] ([CompanyId]);
GO


CREATE INDEX [IX_PdfScaleCalibrations_CreatedByUserId] ON [PdfScaleCalibrations] ([CreatedByUserId]);
GO


CREATE INDEX [IX_PdfScaleCalibrations_PackageDrawingId] ON [PdfScaleCalibrations] ([PackageDrawingId]);
GO


CREATE INDEX [IX_Projects_CustomerId] ON [Projects] ([CustomerId]);
GO


CREATE UNIQUE INDEX [IX_Projects_JobNumber] ON [Projects] ([JobNumber]);
GO


CREATE INDEX [IX_Projects_LastModifiedBy] ON [Projects] ([LastModifiedBy]);
GO


CREATE INDEX [IX_Projects_OwnerId] ON [Projects] ([OwnerId]);
GO


CREATE INDEX [IX_QualityDocuments_ApprovedById] ON [QualityDocuments] ([ApprovedById]);
GO


CREATE INDEX [IX_QualityDocuments_AssemblyId] ON [QualityDocuments] ([AssemblyId]);
GO


CREATE INDEX [IX_QualityDocuments_CompanyId] ON [QualityDocuments] ([CompanyId]);
GO


CREATE INDEX [IX_QualityDocuments_CreatedBy] ON [QualityDocuments] ([CreatedBy]);
GO


CREATE UNIQUE INDEX [IX_QualityDocuments_DocumentNumber] ON [QualityDocuments] ([DocumentNumber]);
GO


CREATE INDEX [IX_QualityDocuments_DocumentType] ON [QualityDocuments] ([DocumentType]);
GO


CREATE INDEX [IX_QualityDocuments_ITPInspectionPointId] ON [QualityDocuments] ([ITPInspectionPointId]);
GO


CREATE INDEX [IX_QualityDocuments_MaterialTraceabilityId] ON [QualityDocuments] ([MaterialTraceabilityId]);
GO


CREATE INDEX [IX_QualityDocuments_OrderId] ON [QualityDocuments] ([OrderId]);
GO


CREATE INDEX [IX_QualityDocuments_PackageId] ON [QualityDocuments] ([PackageId]);
GO


CREATE INDEX [IX_QualityDocuments_SupersededById] ON [QualityDocuments] ([SupersededById]);
GO


CREATE INDEX [IX_QualityDocuments_WorkOrderId] ON [QualityDocuments] ([WorkOrderId]);
GO


CREATE INDEX [IX_QuoteLineItems_QuoteId] ON [QuoteLineItems] ([QuoteId]);
GO


CREATE INDEX [IX_Quotes_CreatedBy] ON [Quotes] ([CreatedBy]);
GO


CREATE INDEX [IX_Quotes_CustomerId] ON [Quotes] ([CustomerId]);
GO


CREATE INDEX [IX_Quotes_LastModifiedBy] ON [Quotes] ([LastModifiedBy]);
GO


CREATE INDEX [IX_Quotes_OrderId] ON [Quotes] ([OrderId]);
GO


CREATE UNIQUE INDEX [IX_RefreshTokens_Token] ON [RefreshTokens] ([Token]);
GO


CREATE INDEX [IX_RefreshTokens_UserId_ExpiresAt] ON [RefreshTokens] ([UserId], [ExpiresAt]);
GO


CREATE INDEX [IX_Resources_PrimaryWorkCenterId] ON [Resources] ([PrimaryWorkCenterId]);
GO


CREATE INDEX [IX_Resources_UserId] ON [Resources] ([UserId]);
GO


CREATE INDEX [IX_RoutingOperations_RoutingTemplateId] ON [RoutingOperations] ([RoutingTemplateId]);
GO


CREATE UNIQUE INDEX [IX_SurfaceCoatings_CompanyId_CoatingCode] ON [SurfaceCoatings] ([CompanyId], [CoatingCode]);
GO


CREATE INDEX [IX_SurfaceCoatings_DisplayOrder] ON [SurfaceCoatings] ([DisplayOrder]);
GO


CREATE INDEX [IX_TakeoffRevisions_CopiedFromRevisionId] ON [TakeoffRevisions] ([CopiedFromRevisionId]);
GO


CREATE INDEX [IX_TakeoffRevisions_CreatedBy] ON [TakeoffRevisions] ([CreatedBy]);
GO


CREATE INDEX [IX_TakeoffRevisions_TakeoffId] ON [TakeoffRevisions] ([TakeoffId]);
GO


CREATE INDEX [IX_TestResults_CompanyId] ON [TestResults] ([CompanyId]);
GO


CREATE INDEX [IX_TestResults_HeatNumber] ON [TestResults] ([HeatNumber]);
GO


CREATE INDEX [IX_TestResults_ITPInspectionPointId] ON [TestResults] ([ITPInspectionPointId]);
GO


CREATE INDEX [IX_TestResults_MaterialTraceabilityId] ON [TestResults] ([MaterialTraceabilityId]);
GO


CREATE INDEX [IX_TestResults_QualityDocumentId] ON [TestResults] ([QualityDocumentId]);
GO


CREATE INDEX [IX_TestResults_TesterId] ON [TestResults] ([TesterId]);
GO


CREATE UNIQUE INDEX [IX_TestResults_TestNumber] ON [TestResults] ([TestNumber]);
GO


CREATE INDEX [IX_TestResults_TestType] ON [TestResults] ([TestType]);
GO


CREATE INDEX [IX_TestResults_WitnessId] ON [TestResults] ([WitnessId]);
GO


CREATE INDEX [IX_TraceAssemblies_AssemblyId] ON [TraceAssemblies] ([AssemblyId]);
GO


CREATE INDEX [IX_TraceAssemblies_BuildOperatorId] ON [TraceAssemblies] ([BuildOperatorId]);
GO


CREATE INDEX [IX_TraceAssemblies_BuildWorkCenterId] ON [TraceAssemblies] ([BuildWorkCenterId]);
GO


CREATE INDEX [IX_TraceAssemblies_TraceRecordId] ON [TraceAssemblies] ([TraceRecordId]);
GO


CREATE INDEX [IX_TraceBeamDetections_TraceDrawingId] ON [TraceBeamDetections] ([TraceDrawingId]);
GO


CREATE INDEX [IX_TraceComponents_TraceAssemblyId] ON [TraceComponents] ([TraceAssemblyId]);
GO


CREATE INDEX [IX_TraceDocuments_CompanyId] ON [TraceDocuments] ([CompanyId]);
GO


CREATE INDEX [IX_TraceDocuments_TraceRecordId] ON [TraceDocuments] ([TraceRecordId]);
GO


CREATE INDEX [IX_TraceDocuments_UploadedByUserId] ON [TraceDocuments] ([UploadedByUserId]);
GO


CREATE INDEX [IX_TraceDocuments_VerifiedByUserId] ON [TraceDocuments] ([VerifiedByUserId]);
GO


CREATE INDEX [IX_TraceDrawings_CompanyId] ON [TraceDrawings] ([CompanyId]);
GO


CREATE INDEX [IX_TraceDrawings_CustomerId] ON [TraceDrawings] ([CustomerId]);
GO


CREATE INDEX [IX_TraceDrawings_ProjectId] ON [TraceDrawings] ([ProjectId]);
GO


CREATE INDEX [IX_TraceDrawings_UploadedBy] ON [TraceDrawings] ([UploadedBy]);
GO


CREATE INDEX [IX_TraceMaterialCatalogueLinks_CatalogueItemId] ON [TraceMaterialCatalogueLinks] ([CatalogueItemId]);
GO


CREATE INDEX [IX_TraceMaterialCatalogueLinks_TraceMaterialId] ON [TraceMaterialCatalogueLinks] ([TraceMaterialId]);
GO


CREATE INDEX [IX_TraceMaterials_CatalogueItemId] ON [TraceMaterials] ([CatalogueItemId]);
GO


CREATE INDEX [IX_TraceMaterials_TraceRecordId] ON [TraceMaterials] ([TraceRecordId]);
GO


CREATE INDEX [IX_TraceMeasurements_TraceDrawingId] ON [TraceMeasurements] ([TraceDrawingId]);
GO


CREATE INDEX [IX_TraceParameters_TraceProcessId] ON [TraceParameters] ([TraceProcessId]);
GO


CREATE INDEX [IX_TraceProcesses_OperatorId] ON [TraceProcesses] ([OperatorId]);
GO


CREATE INDEX [IX_TraceProcesses_TraceRecordId] ON [TraceProcesses] ([TraceRecordId]);
GO


CREATE INDEX [IX_TraceProcesses_WorkOrderOperationId] ON [TraceProcesses] ([WorkOrderOperationId]);
GO


CREATE INDEX [IX_TraceRecords_CompanyId] ON [TraceRecords] ([CompanyId]);
GO


CREATE INDEX [IX_TraceRecords_TraceRecordId] ON [TraceRecords] ([TraceRecordId]);
GO


CREATE INDEX [IX_TraceRecords_UserId] ON [TraceRecords] ([UserId]);
GO


CREATE INDEX [IX_TraceRecords_WorkCenterId] ON [TraceRecords] ([WorkCenterId]);
GO


CREATE INDEX [IX_TraceTakeoffAnnotations_CreatedByUserId] ON [TraceTakeoffAnnotations] ([CreatedByUserId]);
GO


CREATE INDEX [IX_TraceTakeoffAnnotations_TraceTakeoffId] ON [TraceTakeoffAnnotations] ([TraceTakeoffId]);
GO


CREATE INDEX [IX_TraceTakeoffItems_TraceDrawingId] ON [TraceTakeoffItems] ([TraceDrawingId]);
GO


CREATE INDEX [IX_TraceTakeoffMeasurements_CatalogueItemId] ON [TraceTakeoffMeasurements] ([CatalogueItemId]);
GO


CREATE INDEX [IX_TraceTakeoffMeasurements_CreatedBy] ON [TraceTakeoffMeasurements] ([CreatedBy]);
GO


CREATE INDEX [IX_TraceTakeoffMeasurements_ModifiedBy] ON [TraceTakeoffMeasurements] ([ModifiedBy]);
GO


CREATE INDEX [IX_TraceTakeoffMeasurements_PackageDrawingId] ON [TraceTakeoffMeasurements] ([PackageDrawingId]);
GO


CREATE INDEX [IX_TraceTakeoffMeasurements_SurfaceCoatingId] ON [TraceTakeoffMeasurements] ([SurfaceCoatingId]);
GO


CREATE INDEX [IX_TraceTakeoffMeasurements_TraceTakeoffId] ON [TraceTakeoffMeasurements] ([TraceTakeoffId]);
GO


CREATE INDEX [IX_TraceTakeoffs_CompanyId] ON [TraceTakeoffs] ([CompanyId]);
GO


CREATE INDEX [IX_TraceTakeoffs_DrawingId] ON [TraceTakeoffs] ([DrawingId]);
GO


CREATE INDEX [IX_TraceTakeoffs_TraceRecordId] ON [TraceTakeoffs] ([TraceRecordId]);
GO


CREATE UNIQUE INDEX [IX_UserAuthMethods_UserId_Provider] ON [UserAuthMethods] ([UserId], [Provider]);
GO


CREATE INDEX [IX_UserInvitations_CompanyId] ON [UserInvitations] ([CompanyId]);
GO


CREATE INDEX [IX_UserInvitations_InvitedById] ON [UserInvitations] ([InvitedById]);
GO


CREATE INDEX [IX_Users_CompanyId] ON [Users] ([CompanyId]);
GO


CREATE UNIQUE INDEX [IX_Users_Email] ON [Users] ([Email]);
GO


CREATE UNIQUE INDEX [IX_Users_Username] ON [Users] ([Username]);
GO


CREATE INDEX [IX_WeldingConnections_PackageId] ON [WeldingConnections] ([PackageId]);
GO


CREATE INDEX [IX_WorkCenters_CompanyId] ON [WorkCenters] ([CompanyId]);
GO


CREATE UNIQUE INDEX [IX_WorkCenters_WorkCenterCode] ON [WorkCenters] ([WorkCenterCode]);
GO


CREATE INDEX [IX_WorkCenterShifts_WorkCenterId] ON [WorkCenterShifts] ([WorkCenterId]);
GO


CREATE INDEX [IX_WorkOrderAssembly_WorkOrderId] ON [WorkOrderAssembly] ([WorkOrderId]);
GO


CREATE INDEX [IX_WorkOrderInventoryItem_WorkOrderId] ON [WorkOrderInventoryItem] ([WorkOrderId]);
GO


CREATE INDEX [IX_WorkOrderOperations_CompletedBy] ON [WorkOrderOperations] ([CompletedBy]);
GO


CREATE INDEX [IX_WorkOrderOperations_WorkOrderId] ON [WorkOrderOperations] ([WorkOrderId]);
GO


CREATE INDEX [IX_WorkOrderResources_ResourceId] ON [WorkOrderResources] ([ResourceId]);
GO


CREATE INDEX [IX_WorkOrderResources_WorkOrderId] ON [WorkOrderResources] ([WorkOrderId]);
GO


CREATE INDEX [IX_WorkOrders_CompanyId] ON [WorkOrders] ([CompanyId]);
GO


CREATE INDEX [IX_WorkOrders_PackageId] ON [WorkOrders] ([PackageId]);
GO


CREATE INDEX [IX_WorkOrders_PackageId1] ON [WorkOrders] ([PackageId1]);
GO


CREATE INDEX [IX_WorkOrders_PrimaryResourceId] ON [WorkOrders] ([PrimaryResourceId]);
GO


CREATE INDEX [IX_WorkOrders_WorkCenterId] ON [WorkOrders] ([WorkCenterId]);
GO


CREATE INDEX [IX_WorkPackages_CompanyId] ON [WorkPackages] ([CompanyId]);
GO


CREATE INDEX [IX_WorkPackages_OrderId] ON [WorkPackages] ([OrderId]);
GO


ALTER TABLE [Calibrations] ADD CONSTRAINT [FK_Calibrations_PackageDrawings_PackageDrawingId] FOREIGN KEY ([PackageDrawingId]) REFERENCES [PackageDrawings] ([Id]) ON DELETE CASCADE;
GO


ALTER TABLE [EstimationPackages] ADD CONSTRAINT [FK_EstimationPackages_Estimations_EstimationId] FOREIGN KEY ([EstimationId]) REFERENCES [Estimations] ([Id]) ON DELETE CASCADE;
GO


ALTER TABLE [Estimations] ADD CONSTRAINT [FK_Estimations_Orders_OrderId] FOREIGN KEY ([OrderId]) REFERENCES [Orders] ([Id]);
GO


ALTER TABLE [InspectionTestPlans] ADD CONSTRAINT [FK_InspectionTestPlans_Orders_OrderId] FOREIGN KEY ([OrderId]) REFERENCES [Orders] ([Id]) ON DELETE NO ACTION;
GO


ALTER TABLE [InspectionTestPlans] ADD CONSTRAINT [FK_InspectionTestPlans_Packages_PackageId] FOREIGN KEY ([PackageId]) REFERENCES [Packages] ([Id]) ON DELETE NO ACTION;
GO


ALTER TABLE [InspectionTestPlans] ADD CONSTRAINT [FK_InspectionTestPlans_WorkOrders_WorkOrderId] FOREIGN KEY ([WorkOrderId]) REFERENCES [WorkOrders] ([Id]) ON DELETE NO ACTION;
GO


ALTER TABLE [ITPAssemblies] ADD CONSTRAINT [FK_ITPAssemblies_WorkOrderAssembly_WorkOrderAssemblyId] FOREIGN KEY ([WorkOrderAssemblyId]) REFERENCES [WorkOrderAssembly] ([Id]) ON DELETE NO ACTION;
GO


ALTER TABLE [ITPInspectionPoints] ADD CONSTRAINT [FK_ITPInspectionPoints_WorkOrderOperations_WorkOrderOperationId] FOREIGN KEY ([WorkOrderOperationId]) REFERENCES [WorkOrderOperations] ([Id]) ON DELETE NO ACTION;
GO


ALTER TABLE [MaterialTraceability] ADD CONSTRAINT [FK_MaterialTraceability_WorkOrderInventoryItem_WorkOrderInventoryItemId] FOREIGN KEY ([WorkOrderInventoryItemId]) REFERENCES [WorkOrderInventoryItem] ([Id]) ON DELETE NO ACTION;
GO


ALTER TABLE [Orders] ADD CONSTRAINT [FK_Orders_Quotes_QuoteId] FOREIGN KEY ([QuoteId]) REFERENCES [Quotes] ([Id]);
GO


