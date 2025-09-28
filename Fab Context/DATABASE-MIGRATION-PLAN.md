# Fab.OS Database Migration Plan
## From Simple Project-Based to Complete Workflow System

### Version: 1.0
### Date: September 2025
### Target Database: sqldb-steel-estimation-sandbox

---

## **🎯 Migration Overview**

This migration transforms the current simple project-based database into a complete workflow system supporting the dual-path architecture (Quote vs Estimation).

### **Current State**:
```
Companies → Users → Projects → Packages
```

### **Target State**:
```
Companies → Users → [Quotes OR Estimations] → Orders → [Projects OR Direct Packages] → WorkOrders
```

---

## **📋 Migration Phase 1: Core Workflow Entities**

### **1.1 Quote System (Simple Path)**

```sql
-- Quick Quote System for simple jobs
CREATE TABLE Quotes (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    QuoteNumber NVARCHAR(50) NOT NULL UNIQUE, -- QTE-2025-0001
    CustomerId INT NOT NULL,
    QuoteDate DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    ValidUntil DATETIME2 NOT NULL,
    
    -- Single package concept for simple jobs
    Description NVARCHAR(1000) NOT NULL,
    MaterialCost DECIMAL(18,2) NOT NULL DEFAULT 0,
    LaborHours DECIMAL(10,2) NOT NULL DEFAULT 0,
    LaborRate DECIMAL(10,2) NOT NULL DEFAULT 0,
    OverheadPercentage DECIMAL(5,2) NOT NULL DEFAULT 15.00,
    MarginPercentage DECIMAL(5,2) NOT NULL DEFAULT 20.00,
    TotalAmount DECIMAL(18,2) NOT NULL,
    
    -- Status tracking
    Status NVARCHAR(20) NOT NULL DEFAULT 'Draft', -- Draft, Sent, Accepted, Rejected, Expired
    
    -- Audit fields
    CreatedDate DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    CreatedBy INT NULL,
    LastModified DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    LastModifiedBy INT NULL,
    
    -- Conversion tracking
    OrderId INT NULL, -- Links to Order when accepted
    
    CONSTRAINT FK_Quotes_Customers FOREIGN KEY (CustomerId) REFERENCES Customers(Id),
    CONSTRAINT FK_Quotes_CreatedBy FOREIGN KEY (CreatedBy) REFERENCES Users(Id),
    CONSTRAINT FK_Quotes_LastModifiedBy FOREIGN KEY (LastModifiedBy) REFERENCES Users(Id)
);

-- Quote line items for detailed breakdown
CREATE TABLE QuoteLineItems (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    QuoteId INT NOT NULL,
    LineNumber INT NOT NULL,
    
    -- Item details
    ItemDescription NVARCHAR(500) NOT NULL,
    Quantity DECIMAL(18,4) NOT NULL,
    Unit NVARCHAR(20) NOT NULL, -- EA, KG, M, M2, HR
    UnitPrice DECIMAL(18,4) NOT NULL,
    LineTotal DECIMAL(18,2) NOT NULL,
    
    -- Optional catalog reference
    CatalogueItemId INT NULL,
    
    -- Notes
    Notes NVARCHAR(1000) NULL,
    
    CONSTRAINT FK_QuoteLineItems_Quote FOREIGN KEY (QuoteId) REFERENCES Quotes(Id) ON DELETE CASCADE,
    CONSTRAINT FK_QuoteLineItems_CatalogueItem FOREIGN KEY (CatalogueItemId) REFERENCES CatalogueItems(Id)
);
```

### **1.2 Estimation System (Complex Path)**

```sql
-- Complex estimation system for multi-package projects
CREATE TABLE Estimations (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    EstimationNumber NVARCHAR(50) NOT NULL UNIQUE, -- EST-2025-0001
    CustomerId INT NOT NULL,
    ProjectName NVARCHAR(200) NOT NULL,
    EstimationDate DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    ValidUntil DATETIME2 NOT NULL,
    RevisionNumber INT NOT NULL DEFAULT 1,
    
    -- Detailed cost breakdown
    TotalMaterialCost DECIMAL(18,2) NOT NULL DEFAULT 0,
    TotalLaborHours DECIMAL(18,2) NOT NULL DEFAULT 0,
    TotalLaborCost DECIMAL(18,2) NOT NULL DEFAULT 0,
    OverheadPercentage DECIMAL(5,2) NOT NULL DEFAULT 15.00,
    OverheadAmount DECIMAL(18,2) NOT NULL DEFAULT 0,
    MarginPercentage DECIMAL(5,2) NOT NULL DEFAULT 20.00,
    MarginAmount DECIMAL(18,2) NOT NULL DEFAULT 0,
    TotalAmount DECIMAL(18,2) NOT NULL,
    
    -- Status and approval
    Status NVARCHAR(20) NOT NULL DEFAULT 'Draft', -- Draft, InReview, Sent, Accepted, Rejected, Expired
    ApprovedDate DATETIME2 NULL,
    ApprovedBy INT NULL,
    
    -- Audit fields
    CreatedDate DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    CreatedBy INT NOT NULL,
    LastModified DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    LastModifiedBy INT NOT NULL,
    
    -- Conversion tracking
    OrderId INT NULL, -- Links to Order when accepted
    
    CONSTRAINT FK_Estimations_Customers FOREIGN KEY (CustomerId) REFERENCES Customers(Id),
    CONSTRAINT FK_Estimations_CreatedBy FOREIGN KEY (CreatedBy) REFERENCES Users(Id),
    CONSTRAINT FK_Estimations_LastModifiedBy FOREIGN KEY (LastModifiedBy) REFERENCES Users(Id),
    CONSTRAINT FK_Estimations_ApprovedBy FOREIGN KEY (ApprovedBy) REFERENCES Users(Id)
);

-- Estimation packages for complex project breakdown
CREATE TABLE EstimationPackages (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    EstimationId INT NOT NULL,
    PackageName NVARCHAR(200) NOT NULL,
    Description NVARCHAR(1000) NULL,
    SequenceNumber INT NOT NULL,
    
    -- Package costs
    MaterialCost DECIMAL(18,2) NOT NULL DEFAULT 0,
    LaborHours DECIMAL(18,2) NOT NULL DEFAULT 0,
    LaborCost DECIMAL(18,2) NOT NULL DEFAULT 0,
    PackageTotal DECIMAL(18,2) NOT NULL DEFAULT 0,
    
    -- Schedule estimates
    PlannedStartDate DATETIME2 NULL,
    PlannedEndDate DATETIME2 NULL,
    EstimatedDuration INT NULL, -- Days
    
    CONSTRAINT FK_EstimationPackages_Estimation FOREIGN KEY (EstimationId) REFERENCES Estimations(Id) ON DELETE CASCADE
);
```

### **1.3 Unified Order System**

```sql
-- Unified order system for both simple and complex jobs
CREATE TABLE Orders (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    OrderNumber NVARCHAR(50) NOT NULL UNIQUE, -- ORD-2025-0001
    CustomerId INT NOT NULL,
    
    -- Source tracking
    Source NVARCHAR(20) NOT NULL, -- 'FromQuote', 'FromEstimation', 'Direct'
    QuoteId INT NULL, -- Source quote for simple orders
    EstimationId INT NULL, -- Source estimation for complex orders
    
    -- Customer references
    CustomerPONumber NVARCHAR(100) NULL,
    CustomerReference NVARCHAR(200) NULL,
    
    -- Commercial details
    TotalValue DECIMAL(18,2) NOT NULL,
    PaymentTerms NVARCHAR(100) NOT NULL DEFAULT 'NET30',
    OrderDate DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    RequiredDate DATETIME2 NULL,
    PromisedDate DATETIME2 NULL,
    
    -- Status tracking
    Status NVARCHAR(20) NOT NULL DEFAULT 'Confirmed', -- Confirmed, InProgress, OnHold, Complete, Cancelled, Invoiced, Paid
    
    -- Audit fields
    CreatedDate DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    CreatedBy INT NOT NULL,
    LastModified DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    LastModifiedBy INT NOT NULL,
    
    CONSTRAINT FK_Orders_Customers FOREIGN KEY (CustomerId) REFERENCES Customers(Id),
    CONSTRAINT FK_Orders_Quote FOREIGN KEY (QuoteId) REFERENCES Quotes(Id),
    CONSTRAINT FK_Orders_Estimation FOREIGN KEY (EstimationId) REFERENCES Estimations(Id),
    CONSTRAINT FK_Orders_CreatedBy FOREIGN KEY (CreatedBy) REFERENCES Users(Id),
    CONSTRAINT FK_Orders_LastModifiedBy FOREIGN KEY (LastModifiedBy) REFERENCES Users(Id),
    
    -- Ensure proper source linking
    CONSTRAINT CK_Orders_Source CHECK (
        (Source = 'FromQuote' AND QuoteId IS NOT NULL AND EstimationId IS NULL) OR
        (Source = 'FromEstimation' AND EstimationId IS NOT NULL AND QuoteId IS NULL) OR
        (Source = 'Direct' AND QuoteId IS NULL AND EstimationId IS NULL)
    )
);
```

---

## **📋 Migration Phase 2: Enhanced Existing Entities**

### **2.1 Update Projects Table**

```sql
-- Add Order relationship to existing Projects
ALTER TABLE Projects 
ADD OrderId INT NULL,
    ProjectType NVARCHAR(20) DEFAULT 'Standard'; -- 'Standard', 'FromEstimation'

-- Add foreign key constraint
ALTER TABLE Projects
ADD CONSTRAINT FK_Projects_Order FOREIGN KEY (OrderId) REFERENCES Orders(Id);

-- Add index for performance
CREATE INDEX IX_Projects_OrderId ON Projects(OrderId);
```

### **2.2 Update Packages Table**

```sql
-- Add direct Order relationship for simple packages
ALTER TABLE Packages 
ADD OrderId INT NULL, -- Direct link for simple orders (from quotes)
    PackageSource NVARCHAR(20) DEFAULT 'Project'; -- 'Project', 'DirectOrder'

-- Add foreign key constraint
ALTER TABLE Packages
ADD CONSTRAINT FK_Packages_Order FOREIGN KEY (OrderId) REFERENCES Orders(Id);

-- Add constraint to ensure proper package source
ALTER TABLE Packages
ADD CONSTRAINT CK_Packages_Source CHECK (
    (PackageSource = 'Project' AND ProjectId IS NOT NULL AND OrderId IS NULL) OR
    (PackageSource = 'DirectOrder' AND OrderId IS NOT NULL AND ProjectId IS NULL)
);

-- Add index for performance
CREATE INDEX IX_Packages_OrderId ON Packages(OrderId);
```

---

## **📋 Migration Phase 3: Work Order System**

### **3.1 Work Orders**

```sql
-- Work order system for production management
CREATE TABLE WorkOrders (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    WorkOrderNumber NVARCHAR(50) NOT NULL UNIQUE, -- WO-2025-0001
    PackageId INT NOT NULL,
    
    -- Work order details
    WorkOrderType NVARCHAR(20) NOT NULL DEFAULT 'Mixed', -- PartsProcessing, AssemblyBuilding, Mixed, Finishing, QualityControl
    Description NVARCHAR(1000) NULL,
    
    -- Assignment
    WorkCenterId INT NULL, -- Assigned to work center
    PrimaryResourceId INT NULL, -- Assigned to specific person
    
    -- Priority and scheduling
    Priority NVARCHAR(10) NOT NULL DEFAULT 'Normal', -- Low, Normal, High, Urgent
    ScheduledStartDate DATETIME2 NULL,
    ScheduledEndDate DATETIME2 NULL,
    ActualStartDate DATETIME2 NULL,
    ActualEndDate DATETIME2 NULL,
    
    -- Time tracking
    EstimatedHours DECIMAL(10,2) NOT NULL DEFAULT 0,
    ActualHours DECIMAL(10,2) NOT NULL DEFAULT 0,
    
    -- Status
    Status NVARCHAR(20) NOT NULL DEFAULT 'Created', -- Created, Scheduled, Released, InProgress, OnHold, Complete, Cancelled
    Barcode NVARCHAR(100) NULL, -- For shop floor scanning
    
    -- Quality control
    HasHoldPoints BIT NOT NULL DEFAULT 0,
    RequiresInspection BIT NOT NULL DEFAULT 0,
    
    -- Audit fields
    CreatedDate DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    CreatedBy INT NOT NULL,
    LastModified DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    LastModifiedBy INT NOT NULL,
    
    CONSTRAINT FK_WorkOrders_Package FOREIGN KEY (PackageId) REFERENCES Packages(Id),
    CONSTRAINT FK_WorkOrders_WorkCenter FOREIGN KEY (WorkCenterId) REFERENCES WorkCenters(Id),
    CONSTRAINT FK_WorkOrders_CreatedBy FOREIGN KEY (CreatedBy) REFERENCES Users(Id),
    CONSTRAINT FK_WorkOrders_LastModifiedBy FOREIGN KEY (LastModifiedBy) REFERENCES Users(Id)
);
```

### **3.2 Work Order Operations**

```sql
-- Individual operations within work orders
CREATE TABLE WorkOrderOperations (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    WorkOrderId INT NOT NULL,
    SequenceNumber INT NOT NULL,
    
    -- Operation details
    OperationCode NVARCHAR(20) NOT NULL, -- CUT, DRILL, WELD, PAINT, INSPECT
    OperationName NVARCHAR(200) NOT NULL,
    Description NVARCHAR(1000) NULL,
    
    -- Requirements
    RequiredSkill NVARCHAR(100) NULL,
    RequiredSkillLevel INT NULL, -- 1-5 scale
    RequiredMachine NVARCHAR(100) NULL,
    RequiredTooling NVARCHAR(500) NULL,
    
    -- Time estimates
    SetupTime DECIMAL(10,2) NOT NULL DEFAULT 0, -- Hours
    CycleTime DECIMAL(10,2) NOT NULL DEFAULT 0, -- Hours per unit
    EstimatedHours DECIMAL(10,2) NOT NULL DEFAULT 0,
    ActualHours DECIMAL(10,2) NOT NULL DEFAULT 0,
    
    -- Quality requirements
    RequiresInspection BIT NOT NULL DEFAULT 0,
    InspectionType NVARCHAR(50) NULL, -- Visual, Dimensional, NDT, Functional
    LinkedITPPointId INT NULL,
    
    -- Status tracking
    Status NVARCHAR(20) NOT NULL DEFAULT 'Planned', -- Planned, Ready, InProgress, Complete, OnHold
    StartedAt DATETIME2 NULL,
    CompletedAt DATETIME2 NULL,
    CompletedBy INT NULL,
    
    CONSTRAINT FK_WorkOrderOperations_WorkOrder FOREIGN KEY (WorkOrderId) REFERENCES WorkOrders(Id) ON DELETE CASCADE,
    CONSTRAINT FK_WorkOrderOperations_CompletedBy FOREIGN KEY (CompletedBy) REFERENCES Users(Id)
);
```

---

## **📋 Migration Phase 4: Resource Management**

### **4.1 Resources (Labor)**

```sql
-- Resource management for work assignment
CREATE TABLE Resources (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    EmployeeCode NVARCHAR(50) NOT NULL UNIQUE,
    UserId INT NULL, -- Link to Users table if they have system access
    
    -- Personal details
    FirstName NVARCHAR(100) NOT NULL,
    LastName NVARCHAR(100) NOT NULL,
    JobTitle NVARCHAR(200) NOT NULL,
    
    -- Resource type and skills
    ResourceType NVARCHAR(20) NOT NULL DEFAULT 'Direct', -- Direct, Indirect, Contract, Supervisor
    PrimarySkill NVARCHAR(100) NULL,
    SkillLevel INT NOT NULL DEFAULT 1, -- 1-5 scale
    CertificationLevel NVARCHAR(50) NULL,
    
    -- Availability
    StandardHoursPerDay DECIMAL(4,2) NOT NULL DEFAULT 8.00,
    HourlyRate DECIMAL(10,2) NOT NULL DEFAULT 0,
    IsActive BIT NOT NULL DEFAULT 1,
    
    -- Assignment
    PrimaryWorkCenterId INT NULL,
    
    -- Audit fields
    CreatedDate DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    LastModified DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    
    CONSTRAINT FK_Resources_User FOREIGN KEY (UserId) REFERENCES Users(Id),
    CONSTRAINT FK_Resources_PrimaryWorkCenter FOREIGN KEY (PrimaryWorkCenterId) REFERENCES WorkCenters(Id)
);
```

### **4.2 Work Order Resource Assignments**

```sql
-- Track resource assignments to work orders
CREATE TABLE WorkOrderResources (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    WorkOrderId INT NOT NULL,
    ResourceId INT NOT NULL,
    
    -- Assignment details
    AssignmentType NVARCHAR(20) NOT NULL DEFAULT 'Primary', -- Primary, Secondary, Support
    EstimatedHours DECIMAL(10,2) NOT NULL DEFAULT 0,
    ActualHours DECIMAL(10,2) NOT NULL DEFAULT 0,
    
    -- Time tracking
    AssignedDate DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    StartedDate DATETIME2 NULL,
    CompletedDate DATETIME2 NULL,
    
    CONSTRAINT FK_WorkOrderResources_WorkOrder FOREIGN KEY (WorkOrderId) REFERENCES WorkOrders(Id) ON DELETE CASCADE,
    CONSTRAINT FK_WorkOrderResources_Resource FOREIGN KEY (ResourceId) REFERENCES Resources(Id)
);
```

---

## **📋 Migration Phase 5: Update Foreign Keys**

### **5.1 Add Missing Foreign Key Relationships**

```sql
-- Update Quotes table to link to Orders when accepted
ALTER TABLE Quotes
ADD CONSTRAINT FK_Quotes_Order FOREIGN KEY (OrderId) REFERENCES Orders(Id);

-- Update Estimations table to link to Orders when accepted  
ALTER TABLE Estimations
ADD CONSTRAINT FK_Estimations_Order FOREIGN KEY (OrderId) REFERENCES Orders(Id);

-- Add index for performance
CREATE INDEX IX_Quotes_OrderId ON Quotes(OrderId);
CREATE INDEX IX_Estimations_OrderId ON Estimations(OrderId);
CREATE INDEX IX_WorkOrders_PackageId ON WorkOrders(PackageId);
CREATE INDEX IX_WorkOrderOperations_WorkOrderId ON WorkOrderOperations(WorkOrderId);
```

---

## **📋 Migration Phase 6: Data Migration Scripts**

### **6.1 Migrate Existing Projects to Orders**

```sql
-- Create orders for existing projects
INSERT INTO Orders (
    OrderNumber, 
    CustomerId, 
    Source, 
    TotalValue, 
    OrderDate, 
    Status, 
    CreatedDate, 
    CreatedBy, 
    LastModified, 
    LastModifiedBy
)
SELECT 
    'ORD-' + RIGHT('000000' + CAST(ROW_NUMBER() OVER (ORDER BY p.Id) AS NVARCHAR), 6),
    COALESCE(p.CustomerId, 1), -- Use customer if available, otherwise default
    'Direct',
    0, -- Will be calculated from packages
    p.CreatedDate,
    'InProgress',
    p.CreatedDate,
    COALESCE(p.OwnerId, 1),
    p.LastModified,
    COALESCE(p.LastModifiedBy, 1)
FROM Projects p
WHERE NOT EXISTS (SELECT 1 FROM Orders o WHERE o.Id = p.Id);

-- Update projects with their corresponding OrderId
UPDATE p
SET OrderId = o.Id
FROM Projects p
INNER JOIN Orders o ON o.OrderNumber = 'ORD-' + RIGHT('000000' + CAST(
    (SELECT ROW_NUMBER() OVER (ORDER BY p2.Id) 
     FROM Projects p2 
     WHERE p2.Id <= p.Id), 6
), 6);
```

### **6.2 Set Package Source Types**

```sql
-- Update packages to indicate they come from projects
UPDATE Packages 
SET PackageSource = 'Project'
WHERE ProjectId IS NOT NULL;
```

---

## **📋 Migration Phase 7: Create Indexes and Constraints**

```sql
-- Performance indexes
CREATE INDEX IX_Quotes_CustomerId ON Quotes(CustomerId);
CREATE INDEX IX_Quotes_Status ON Quotes(Status);
CREATE INDEX IX_Quotes_QuoteDate ON Quotes(QuoteDate);

CREATE INDEX IX_Estimations_CustomerId ON Estimations(CustomerId);
CREATE INDEX IX_Estimations_Status ON Estimations(Status);
CREATE INDEX IX_Estimations_EstimationDate ON Estimations(EstimationDate);

CREATE INDEX IX_Orders_CustomerId ON Orders(CustomerId);
CREATE INDEX IX_Orders_Status ON Orders(Status);
CREATE INDEX IX_Orders_OrderDate ON Orders(OrderDate);

CREATE INDEX IX_WorkOrders_Status ON WorkOrders(Status);
CREATE INDEX IX_WorkOrders_WorkCenterId ON WorkOrders(WorkCenterId);
CREATE INDEX IX_WorkOrders_ScheduledStartDate ON WorkOrders(ScheduledStartDate);

-- Business logic constraints
ALTER TABLE Quotes ADD CONSTRAINT CK_Quotes_Status 
CHECK (Status IN ('Draft', 'Sent', 'Accepted', 'Rejected', 'Expired'));

ALTER TABLE Estimations ADD CONSTRAINT CK_Estimations_Status 
CHECK (Status IN ('Draft', 'InReview', 'Sent', 'Accepted', 'Rejected', 'Expired'));

ALTER TABLE Orders ADD CONSTRAINT CK_Orders_Status 
CHECK (Status IN ('Confirmed', 'InProgress', 'OnHold', 'Complete', 'Cancelled', 'Invoiced', 'Paid'));

ALTER TABLE WorkOrders ADD CONSTRAINT CK_WorkOrders_Status 
CHECK (Status IN ('Created', 'Scheduled', 'Released', 'InProgress', 'OnHold', 'Complete', 'Cancelled'));
```

---

## **🚀 Migration Execution Plan**

### **Step-by-Step Execution**:

1. **Backup Database**: Full backup before migration
2. **Phase 1**: Create new workflow tables (Quotes, Estimations, Orders)
3. **Phase 2**: Alter existing tables (Projects, Packages)
4. **Phase 3**: Create work order system tables
5. **Phase 4**: Create resource management tables
6. **Phase 5**: Add foreign key constraints
7. **Phase 6**: Migrate existing data
8. **Phase 7**: Create indexes and constraints
9. **Test**: Verify all relationships and data integrity
10. **Update Application**: Deploy new entity models

### **Rollback Plan**:
- Keep backup available for 30 days
- Document all changes for reversal if needed
- Test rollback procedure in development environment

This migration transforms your database into a complete workflow system while preserving all existing data and maintaining backward compatibility.
