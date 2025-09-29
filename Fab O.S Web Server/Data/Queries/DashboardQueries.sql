-- =============================================
-- Fab.OS Trace & Takeoff Dashboard Queries
-- =============================================

-- 1. PROJECT OVERVIEW SUMMARY
-- Returns high-level metrics for all active projects
SELECT
    COUNT(DISTINCT tr.Id) as ActiveTraces,
    COUNT(DISTINCT tt.Id) as ActiveTakeoffs,
    COUNT(DISTINCT ttm.Id) as TotalMeasurements,
    COALESCE(SUM(ttm.CalculatedWeight), 0) as TotalWeight_kg,
    COUNT(DISTINCT ttm.CatalogueItemId) as UniqueMaterials
FROM dbo.TraceRecords tr
LEFT JOIN dbo.TraceTakeoffs tt ON tr.Id = tt.TraceRecordId
LEFT JOIN dbo.TraceTakeoffMeasurements ttm ON tt.Id = ttm.TraceTakeoffId
WHERE tr.IsActive = 1
AND tr.CompanyId = @CompanyId;

-- 2. MATERIAL USAGE BY CATEGORY
-- Shows total consumption grouped by material category
SELECT
    ci.Category,
    COUNT(DISTINCT ttm.Id) as MeasurementCount,
    COUNT(DISTINCT ci.Id) as UniqueItems,
    ROUND(SUM(ttm.CalculatedWeight), 2) as TotalWeight_kg,
    ROUND(AVG(ttm.CalculatedWeight), 2) as AvgWeight_kg
FROM dbo.TraceTakeoffMeasurements ttm
INNER JOIN dbo.CatalogueItems ci ON ttm.CatalogueItemId = ci.Id
INNER JOIN dbo.TraceTakeoffs tt ON ttm.TraceTakeoffId = tt.Id
WHERE tt.CompanyId = @CompanyId
GROUP BY ci.Category
ORDER BY TotalWeight_kg DESC;

-- 3. TOP MATERIALS BY USAGE
-- Identifies most frequently used materials
SELECT TOP 10
    ci.ItemCode,
    ci.Description,
    ci.Category,
    COUNT(ttm.Id) as UsageCount,
    SUM(ttm.Value) as TotalQuantity,
    MAX(ttm.Unit) as Unit,
    ROUND(SUM(ttm.CalculatedWeight), 2) as TotalWeight_kg
FROM dbo.TraceTakeoffMeasurements ttm
INNER JOIN dbo.CatalogueItems ci ON ttm.CatalogueItemId = ci.Id
INNER JOIN dbo.TraceTakeoffs tt ON ttm.TraceTakeoffId = tt.Id
WHERE tt.CompanyId = @CompanyId
GROUP BY ci.ItemCode, ci.Description, ci.Category
ORDER BY UsageCount DESC, TotalWeight_kg DESC;

-- 4. TAKEOFF STATUS SUMMARY
-- Shows progress of all takeoffs
SELECT
    tt.Status,
    COUNT(*) as Count,
    ROUND(AVG(MeasurementCount), 0) as AvgMeasurements,
    ROUND(AVG(TotalWeight), 2) as AvgWeight_kg
FROM (
    SELECT
        tt.Id,
        tt.Status,
        COUNT(ttm.Id) as MeasurementCount,
        SUM(ttm.CalculatedWeight) as TotalWeight
    FROM dbo.TraceTakeoffs tt
    LEFT JOIN dbo.TraceTakeoffMeasurements ttm ON tt.Id = ttm.TraceTakeoffId
    WHERE tt.CompanyId = @CompanyId
    GROUP BY tt.Id, tt.Status
) tt
GROUP BY tt.Status;

-- 5. RECENT TRACE ACTIVITY
-- Shows latest trace records with details
SELECT TOP 10
    tr.TraceNumber,
    tr.Description,
    tr.EntityType,
    CASE tr.EntityType
        WHEN 1 THEN 'Raw Material'
        WHEN 2 THEN 'Processed Material'
        WHEN 3 THEN 'Operation'
        WHEN 4 THEN 'Assembly'
        WHEN 5 THEN 'Product'
        WHEN 6 THEN 'Package'
        WHEN 7 THEN 'Shipment'
        ELSE 'Unknown'
    END as EntityTypeName,
    tr.CreatedDate,
    tt.PdfUrl,
    (SELECT COUNT(*) FROM dbo.TraceTakeoffMeasurements WHERE TraceTakeoffId = tt.Id) as MeasurementCount,
    (SELECT SUM(CalculatedWeight) FROM dbo.TraceTakeoffMeasurements WHERE TraceTakeoffId = tt.Id) as TotalWeight_kg
FROM dbo.TraceRecords tr
LEFT JOIN dbo.TraceTakeoffs tt ON tr.Id = tt.TraceRecordId
WHERE tr.CompanyId = @CompanyId
ORDER BY tr.CreatedDate DESC;

-- 6. WEIGHT BY MEASUREMENT TYPE
-- Analysis of measurement types and their weights
SELECT
    ttm.MeasurementType,
    COUNT(*) as Count,
    ROUND(SUM(ttm.CalculatedWeight), 2) as TotalWeight_kg,
    ROUND(AVG(ttm.CalculatedWeight), 2) as AvgWeight_kg,
    MIN(ttm.Unit) as CommonUnit
FROM dbo.TraceTakeoffMeasurements ttm
INNER JOIN dbo.TraceTakeoffs tt ON ttm.TraceTakeoffId = tt.Id
WHERE tt.CompanyId = @CompanyId
GROUP BY ttm.MeasurementType
ORDER BY Count DESC;

-- 7. CATALOGUE COVERAGE
-- Shows which catalogue items are being used
SELECT
    ci.Category,
    COUNT(DISTINCT ci.Id) as TotalItems,
    COUNT(DISTINCT ttm.CatalogueItemId) as UsedItems,
    CAST(COUNT(DISTINCT ttm.CatalogueItemId) * 100.0 / COUNT(DISTINCT ci.Id) as DECIMAL(5,2)) as UsagePercent
FROM dbo.CatalogueItems ci
LEFT JOIN dbo.TraceTakeoffMeasurements ttm ON ci.Id = ttm.CatalogueItemId
WHERE ci.CompanyId = @CompanyId
GROUP BY ci.Category
ORDER BY UsagePercent DESC;

-- 8. TRACE HIERARCHY ANALYSIS
-- Shows parent-child relationships in traces
WITH TraceHierarchy AS (
    SELECT
        tr.Id,
        tr.TraceNumber,
        tr.Description,
        tr.ParentTraceId,
        0 as Level,
        CAST(tr.TraceNumber as NVARCHAR(MAX)) as Path
    FROM dbo.TraceRecords tr
    WHERE tr.ParentTraceId IS NULL
    AND tr.CompanyId = @CompanyId

    UNION ALL

    SELECT
        tr.Id,
        tr.TraceNumber,
        tr.Description,
        tr.ParentTraceId,
        th.Level + 1,
        th.Path + ' > ' + tr.TraceNumber
    FROM dbo.TraceRecords tr
    INNER JOIN TraceHierarchy th ON tr.ParentTraceId = th.TraceId
)
SELECT
    Level,
    TraceNumber,
    Description,
    Path
FROM TraceHierarchy
ORDER BY Path;

-- 9. DAILY ACTIVITY TREND
-- Shows trace and takeoff activity over time
SELECT
    CAST(CreatedDate as DATE) as Date,
    SUM(CASE WHEN TableName = 'Trace' THEN 1 ELSE 0 END) as NewTraces,
    SUM(CASE WHEN TableName = 'Takeoff' THEN 1 ELSE 0 END) as NewTakeoffs,
    SUM(CASE WHEN TableName = 'Measurement' THEN 1 ELSE 0 END) as NewMeasurements
FROM (
    SELECT 'Trace' as TableName, CreatedDate FROM dbo.TraceRecords WHERE CompanyId = @CompanyId
    UNION ALL
    SELECT 'Takeoff', CreatedDate FROM dbo.TraceTakeoffs WHERE CompanyId = @CompanyId
    UNION ALL
    SELECT 'Measurement', CreatedDate FROM dbo.TraceTakeoffMeasurements ttm
    INNER JOIN dbo.TraceTakeoffs tt ON ttm.TraceTakeoffId = tt.Id
    WHERE tt.CompanyId = @CompanyId
) Activity
WHERE CreatedDate >= DATEADD(day, -30, GETUTCDATE())
GROUP BY CAST(CreatedDate as DATE)
ORDER BY Date DESC;

-- 10. MATERIAL REQUIREMENTS PLANNING
-- Aggregates all material needs across active projects
SELECT
    ci.ItemCode,
    ci.Description,
    ci.Category,
    ci.Material,
    SUM(ttm.Value) as RequiredQuantity,
    MAX(ttm.Unit) as Unit,
    ROUND(SUM(ttm.CalculatedWeight), 2) as RequiredWeight_kg,
    COUNT(DISTINCT tt.TraceRecordId) as ProjectCount,
    STRING_AGG(DISTINCT tr.TraceNumber, ', ') as TraceNumbers
FROM dbo.TraceTakeoffMeasurements ttm
INNER JOIN dbo.CatalogueItems ci ON ttm.CatalogueItemId = ci.Id
INNER JOIN dbo.TraceTakeoffs tt ON ttm.TraceTakeoffId = tt.Id
INNER JOIN dbo.TraceRecords tr ON tt.TraceRecordId = tr.Id
WHERE tt.Status IN ('In Progress', 'Draft')
AND tr.IsActive = 1
AND tt.CompanyId = @CompanyId
GROUP BY ci.ItemCode, ci.Description, ci.Category, ci.Material
ORDER BY RequiredWeight_kg DESC;