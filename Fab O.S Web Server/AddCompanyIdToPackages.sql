-- Add CompanyId column to Packages table
-- This is required for multi-tenant support

-- Step 1: Add CompanyId column as nullable first
ALTER TABLE Packages
ADD CompanyId INT NULL;
GO

-- Step 2: Set default value for existing packages (use CompanyId = 1 for Steel Estimation Platform)
UPDATE Packages
SET CompanyId = 1
WHERE CompanyId IS NULL;
GO

-- Step 3: Make CompanyId NOT NULL now that all rows have a value
ALTER TABLE Packages
ALTER COLUMN CompanyId INT NOT NULL;
GO

-- Step 4: Add foreign key constraint (NO ACTION to avoid cascade conflicts)
ALTER TABLE Packages
ADD CONSTRAINT FK_Packages_Companies_CompanyId
FOREIGN KEY (CompanyId) REFERENCES Companies(Id) ON DELETE NO ACTION;
GO

-- Step 5: Add index on CompanyId for query performance
CREATE INDEX IX_Packages_CompanyId ON Packages(CompanyId);
GO
