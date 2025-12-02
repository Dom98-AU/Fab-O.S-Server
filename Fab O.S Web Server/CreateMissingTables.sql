-- ============================================================================
-- CREATE MISSING ITEM MANAGEMENT TABLES
-- Generated: 2025-11-04
-- Purpose: Create 8 missing tables for Item Management System
-- ============================================================================

-- 1. GratingSpecifications - Extended attributes for grating catalogue items
CREATE TABLE [GratingSpecifications] (
    [Id] int NOT NULL IDENTITY(1,1),
    [CatalogueItemId] int NOT NULL,
    [LoadBar_Height_mm] decimal(10,2) NULL,
    [LoadBar_Spacing_mm] decimal(10,2) NULL,
    [LoadBar_Thickness_mm] decimal(10,2) NULL,
    [CrossBar_Spacing_mm] decimal(10,2) NULL,
    [Standard_Panel_Length_mm] decimal(10,2) NULL,
    [Standard_Panel_Width_mm] decimal(10,2) NULL,
    [Dimensions] nvarchar(50) NULL,
    CONSTRAINT [PK_GratingSpecifications] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_GratingSpecifications_CatalogueItems] FOREIGN KEY ([CatalogueItemId]) REFERENCES [CatalogueItems] ([Id]) ON DELETE CASCADE
);

CREATE INDEX [IX_GratingSpecifications_CatalogueItemId] ON [GratingSpecifications] ([CatalogueItemId]);

-- 2. Assemblies - Manufactured items containing other items
CREATE TABLE [Assemblies] (
    [Id] int NOT NULL IDENTITY(1,1),
    [AssemblyNumber] nvarchar(100) NOT NULL,
    [Description] nvarchar(200) NOT NULL,
    [Version] nvarchar(50) NULL,
    [DrawingNumber] nvarchar(100) NULL,
    [ParentAssemblyId] int NULL,
    [WorkPackageId] int NULL,
    [QuantityRequired] int NOT NULL DEFAULT 1,
    [QDocsAssemblyId] int NULL,
    [IFCRevisionId] int NULL,
    [Source] nvarchar(20) NOT NULL DEFAULT 'Direct',
    [EstimatedWeight] decimal(18,4) NULL,
    [EstimatedCost] decimal(18,4) NULL,
    [EstimatedHours] decimal(18,2) NULL,
    [IsActive] bit NOT NULL DEFAULT 1,
    [CreatedDate] datetime2 NOT NULL DEFAULT GETUTCDATE(),
    [ModifiedDate] datetime2 NULL,
    [CompanyId] int NOT NULL,
    CONSTRAINT [PK_Assemblies] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_Assemblies_Assemblies] FOREIGN KEY ([ParentAssemblyId]) REFERENCES [Assemblies] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_Assemblies_WorkPackages] FOREIGN KEY ([WorkPackageId]) REFERENCES [WorkPackages] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_Assemblies_Companies] FOREIGN KEY ([CompanyId]) REFERENCES [Companies] ([Id]) ON DELETE CASCADE
);

CREATE INDEX [IX_Assemblies_ParentAssemblyId] ON [Assemblies] ([ParentAssemblyId]);
CREATE INDEX [IX_Assemblies_WorkPackageId] ON [Assemblies] ([WorkPackageId]);
CREATE INDEX [IX_Assemblies_CompanyId] ON [Assemblies] ([CompanyId]);
CREATE INDEX [IX_Assemblies_AssemblyNumber] ON [Assemblies] ([AssemblyNumber]);

-- 3. AssemblyComponents - Bill of Materials
CREATE TABLE [AssemblyComponents] (
    [Id] int NOT NULL IDENTITY(1,1),
    [AssemblyId] int NOT NULL,
    [CatalogueItemId] int NULL,
    [ComponentAssemblyId] int NULL,
    [ComponentReference] nvarchar(100) NOT NULL,
    [Quantity] decimal(18,4) NOT NULL,
    [Unit] nvarchar(20) NOT NULL,
    [Notes] nvarchar(500) NULL,
    [AlternateItems] nvarchar(100) NULL,
    [IsActive] bit NOT NULL DEFAULT 1,
    [CreatedDate] datetime2 NOT NULL DEFAULT GETUTCDATE(),
    CONSTRAINT [PK_AssemblyComponents] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_AssemblyComponents_Assemblies] FOREIGN KEY ([AssemblyId]) REFERENCES [Assemblies] ([Id]) ON DELETE CASCADE,
    CONSTRAINT [FK_AssemblyComponents_CatalogueItems] FOREIGN KEY ([CatalogueItemId]) REFERENCES [CatalogueItems] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_AssemblyComponents_Assemblies_Component] FOREIGN KEY ([ComponentAssemblyId]) REFERENCES [Assemblies] ([Id]) ON DELETE NO ACTION
);

CREATE INDEX [IX_AssemblyComponents_AssemblyId] ON [AssemblyComponents] ([AssemblyId]);
CREATE INDEX [IX_AssemblyComponents_CatalogueItemId] ON [AssemblyComponents] ([CatalogueItemId]);
CREATE INDEX [IX_AssemblyComponents_ComponentAssemblyId] ON [AssemblyComponents] ([ComponentAssemblyId]);

-- 4. InventoryItems - Physical stock in warehouses
CREATE TABLE [InventoryItems] (
    [Id] int NOT NULL IDENTITY(1,1),
    [CatalogueItemId] int NOT NULL,
    [InventoryCode] nvarchar(50) NOT NULL,
    [QRCode] nvarchar(500) NULL,
    [PurchaseOrderLineId] int NULL,
    [AssemblyId] int NULL,
    [WorkOrderId] int NULL,
    [PartReference] nvarchar(100) NULL,
    [DrawingPartId] int NULL,
    [DrawingRevisionId] int NULL,
    [Status] nvarchar(20) NOT NULL DEFAULT 'Available',
    [ConditionOnReceipt] nvarchar(20) NULL,
    [ReceiptNotes] nvarchar(2000) NULL,
    [ReceivedByUserId] int NULL,
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
    [IsActive] bit NOT NULL DEFAULT 1,
    [CreatedDate] datetime2 NOT NULL DEFAULT GETUTCDATE(),
    [ModifiedDate] datetime2 NULL,
    [CompanyId] int NOT NULL,
    CONSTRAINT [PK_InventoryItems] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_InventoryItems_CatalogueItems] FOREIGN KEY ([CatalogueItemId]) REFERENCES [CatalogueItems] ([Id]) ON DELETE CASCADE,
    CONSTRAINT [FK_InventoryItems_Assemblies] FOREIGN KEY ([AssemblyId]) REFERENCES [Assemblies] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_InventoryItems_WorkOrders] FOREIGN KEY ([WorkOrderId]) REFERENCES [WorkOrders] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_InventoryItems_Users] FOREIGN KEY ([ReceivedByUserId]) REFERENCES [Users] ([Id]) ON DELETE SET NULL,
    CONSTRAINT [FK_InventoryItems_Companies] FOREIGN KEY ([CompanyId]) REFERENCES [Companies] ([Id]) ON DELETE CASCADE
);

CREATE INDEX [IX_InventoryItems_CatalogueItemId] ON [InventoryItems] ([CatalogueItemId]);
CREATE INDEX [IX_InventoryItems_AssemblyId] ON [InventoryItems] ([AssemblyId]);
CREATE INDEX [IX_InventoryItems_WorkOrderId] ON [InventoryItems] ([WorkOrderId]);
CREATE INDEX [IX_InventoryItems_ReceivedByUserId] ON [InventoryItems] ([ReceivedByUserId]);
CREATE INDEX [IX_InventoryItems_CompanyId] ON [InventoryItems] ([CompanyId]);
CREATE INDEX [IX_InventoryItems_InventoryCode] ON [InventoryItems] ([InventoryCode]);
CREATE INDEX [IX_InventoryItems_Status] ON [InventoryItems] ([Status]);

-- 5. InventoryTransactions - Stock movements
CREATE TABLE [InventoryTransactions] (
    [Id] int NOT NULL IDENTITY(1,1),
    [InventoryItemId] int NOT NULL,
    [TransactionNumber] nvarchar(50) NOT NULL,
    [TransactionType] nvarchar(50) NOT NULL,
    [Quantity] decimal(18,4) NOT NULL,
    [Unit] nvarchar(20) NOT NULL,
    [Reference] nvarchar(100) NULL,
    [Notes] nvarchar(500) NULL,
    [TransactionDate] datetime2 NOT NULL DEFAULT GETUTCDATE(),
    [UserId] int NULL,
    [CompanyId] int NOT NULL,
    CONSTRAINT [PK_InventoryTransactions] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_InventoryTransactions_InventoryItems] FOREIGN KEY ([InventoryItemId]) REFERENCES [InventoryItems] ([Id]) ON DELETE CASCADE,
    CONSTRAINT [FK_InventoryTransactions_Users] FOREIGN KEY ([UserId]) REFERENCES [Users] ([Id]) ON DELETE SET NULL,
    CONSTRAINT [FK_InventoryTransactions_Companies] FOREIGN KEY ([CompanyId]) REFERENCES [Companies] ([Id]) ON DELETE NO ACTION
);

CREATE INDEX [IX_InventoryTransactions_InventoryItemId] ON [InventoryTransactions] ([InventoryItemId]);
CREATE INDEX [IX_InventoryTransactions_UserId] ON [InventoryTransactions] ([UserId]);
CREATE INDEX [IX_InventoryTransactions_CompanyId] ON [InventoryTransactions] ([CompanyId]);
CREATE INDEX [IX_InventoryTransactions_TransactionNumber] ON [InventoryTransactions] ([TransactionNumber]);

-- 6. PurchaseOrders - Purchase order headers
CREATE TABLE [PurchaseOrders] (
    [Id] int NOT NULL IDENTITY(1,1),
    [PONumber] nvarchar(50) NOT NULL,
    [WorkPackageId] int NOT NULL,
    [SupplierId] int NOT NULL,
    [SupplierReference] nvarchar(200) NULL,
    [OrderDate] datetime2 NOT NULL DEFAULT GETUTCDATE(),
    [RequiredDate] datetime2 NULL,
    [DeliveryDate] datetime2 NULL,
    [Status] nvarchar(20) NOT NULL DEFAULT 'Draft',
    [TotalValue] decimal(18,2) NOT NULL,
    [QRCode] nvarchar(500) NULL,
    [CreateITP] bit NOT NULL DEFAULT 0,
    [ITPId] int NULL,
    [Notes] nvarchar(2000) NULL,
    [CreatedDate] datetime2 NOT NULL DEFAULT GETUTCDATE(),
    [CreatedBy] int NULL,
    [LastModified] datetime2 NOT NULL DEFAULT GETUTCDATE(),
    [LastModifiedBy] int NULL,
    [CompanyId] int NOT NULL,
    CONSTRAINT [PK_PurchaseOrders] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_PurchaseOrders_WorkPackages] FOREIGN KEY ([WorkPackageId]) REFERENCES [WorkPackages] ([Id]) ON DELETE CASCADE,
    CONSTRAINT [FK_PurchaseOrders_Customers] FOREIGN KEY ([SupplierId]) REFERENCES [Customers] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_PurchaseOrders_Companies] FOREIGN KEY ([CompanyId]) REFERENCES [Companies] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_PurchaseOrders_Users_CreatedBy] FOREIGN KEY ([CreatedBy]) REFERENCES [Users] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_PurchaseOrders_Users_LastModifiedBy] FOREIGN KEY ([LastModifiedBy]) REFERENCES [Users] ([Id]) ON DELETE NO ACTION
);

CREATE INDEX [IX_PurchaseOrders_WorkPackageId] ON [PurchaseOrders] ([WorkPackageId]);
CREATE INDEX [IX_PurchaseOrders_SupplierId] ON [PurchaseOrders] ([SupplierId]);
CREATE INDEX [IX_PurchaseOrders_CompanyId] ON [PurchaseOrders] ([CompanyId]);
CREATE INDEX [IX_PurchaseOrders_CreatedBy] ON [PurchaseOrders] ([CreatedBy]);
CREATE INDEX [IX_PurchaseOrders_LastModifiedBy] ON [PurchaseOrders] ([LastModifiedBy]);
CREATE INDEX [IX_PurchaseOrders_PONumber] ON [PurchaseOrders] ([PONumber]);

-- 7. PurchaseOrderLineItems - Purchase order lines
CREATE TABLE [PurchaseOrderLineItems] (
    [Id] int NOT NULL IDENTITY(1,1),
    [PurchaseOrderId] int NOT NULL,
    [LineNumber] int NOT NULL,
    [CatalogueItemId] int NOT NULL,
    [AssemblyId] int NULL,
    [DrawingPartId] int NULL,
    [Description] nvarchar(500) NOT NULL,
    [QuantityOrdered] decimal(18,4) NOT NULL,
    [QuantityReceived] decimal(18,4) NOT NULL DEFAULT 0,
    [Unit] nvarchar(20) NOT NULL,
    [UnitPrice] decimal(18,4) NOT NULL,
    [LineTotal] decimal(18,2) NOT NULL,
    [Status] nvarchar(20) NOT NULL DEFAULT 'Pending',
    [Notes] nvarchar(1000) NULL,
    CONSTRAINT [PK_PurchaseOrderLineItems] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_PurchaseOrderLineItems_PurchaseOrders] FOREIGN KEY ([PurchaseOrderId]) REFERENCES [PurchaseOrders] ([Id]) ON DELETE CASCADE,
    CONSTRAINT [FK_PurchaseOrderLineItems_CatalogueItems] FOREIGN KEY ([CatalogueItemId]) REFERENCES [CatalogueItems] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_PurchaseOrderLineItems_Assemblies] FOREIGN KEY ([AssemblyId]) REFERENCES [Assemblies] ([Id]) ON DELETE NO ACTION
);

CREATE INDEX [IX_PurchaseOrderLineItems_PurchaseOrderId] ON [PurchaseOrderLineItems] ([PurchaseOrderId]);
CREATE INDEX [IX_PurchaseOrderLineItems_CatalogueItemId] ON [PurchaseOrderLineItems] ([CatalogueItemId]);
CREATE INDEX [IX_PurchaseOrderLineItems_AssemblyId] ON [PurchaseOrderLineItems] ([AssemblyId]);

-- 8. ITPMaterialInspections - Material quality inspection records
CREATE TABLE [ITPMaterialInspections] (
    [Id] int NOT NULL IDENTITY(1,1),
    [ITPId] int NOT NULL,
    [InventoryItemId] int NOT NULL,
    [PurchaseOrderLineId] int NULL,
    [InspectionDate] datetime2 NOT NULL DEFAULT GETUTCDATE(),
    [InspectorName] nvarchar(100) NOT NULL,
    [InspectorUserId] int NULL,
    [MeasuredLength] decimal(18,2) NULL,
    [MeasuredWidth] decimal(18,2) NULL,
    [MeasuredThickness] decimal(18,2) NULL,
    [MeasuredDiameter] decimal(18,2) NULL,
    [DimensionsPass] bit NULL,
    [DimensionNotes] nvarchar(1000) NULL,
    [VisualInspectionPass] bit NULL,
    [DamageObserved] nvarchar(500) NULL,
    [VisualInspectionNotes] nvarchar(2000) NULL,
    [SurfaceFinish] nvarchar(20) NULL,
    [SurfaceFinishNotes] nvarchar(1000) NULL,
    [MillCertificatesReceived] bit NULL,
    [MillCertificateReference] nvarchar(100) NULL,
    [CertificateNotes] nvarchar(2000) NULL,
    [PhotoUrls] nvarchar(4000) NULL,
    [InspectorSignature] nvarchar(MAX) NULL,
    [InspectionResult] nvarchar(20) NOT NULL DEFAULT 'Pending',
    [InspectionNotes] nvarchar(2000) NULL,
    [CompanyId] int NOT NULL,
    [CreatedDate] datetime2 NOT NULL DEFAULT GETUTCDATE(),
    [ModifiedDate] datetime2 NULL,
    CONSTRAINT [PK_ITPMaterialInspections] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_ITPMaterialInspections_InventoryItems] FOREIGN KEY ([InventoryItemId]) REFERENCES [InventoryItems] ([Id]) ON DELETE CASCADE,
    CONSTRAINT [FK_ITPMaterialInspections_PurchaseOrderLineItems] FOREIGN KEY ([PurchaseOrderLineId]) REFERENCES [PurchaseOrderLineItems] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_ITPMaterialInspections_Users] FOREIGN KEY ([InspectorUserId]) REFERENCES [Users] ([Id]) ON DELETE SET NULL,
    CONSTRAINT [FK_ITPMaterialInspections_Companies] FOREIGN KEY ([CompanyId]) REFERENCES [Companies] ([Id]) ON DELETE NO ACTION
);

CREATE INDEX [IX_ITPMaterialInspections_InventoryItemId] ON [ITPMaterialInspections] ([InventoryItemId]);
CREATE INDEX [IX_ITPMaterialInspections_PurchaseOrderLineId] ON [ITPMaterialInspections] ([PurchaseOrderLineId]);
CREATE INDEX [IX_ITPMaterialInspections_InspectorUserId] ON [ITPMaterialInspections] ([InspectorUserId]);
CREATE INDEX [IX_ITPMaterialInspections_CompanyId] ON [ITPMaterialInspections] ([CompanyId]);
CREATE INDEX [IX_ITPMaterialInspections_ITPId] ON [ITPMaterialInspections] ([ITPId]);

-- Update PurchaseOrderLineItems FK to InventoryItems after InventoryItems table is created
ALTER TABLE [InventoryItems]
ADD CONSTRAINT [FK_InventoryItems_PurchaseOrderLineItems]
FOREIGN KEY ([PurchaseOrderLineId]) REFERENCES [PurchaseOrderLineItems] ([Id]) ON DELETE NO ACTION;

CREATE INDEX [IX_InventoryItems_PurchaseOrderLineId] ON [InventoryItems] ([PurchaseOrderLineId]);

GO

PRINT 'Successfully created 8 missing Item Management tables:';
PRINT '1. GratingSpecifications';
PRINT '2. Assemblies';
PRINT '3. AssemblyComponents';
PRINT '4. InventoryItems';
PRINT '5. InventoryTransactions';
PRINT '6. PurchaseOrders';
PRINT '7. PurchaseOrderLineItems';
PRINT '8. ITPMaterialInspections';
