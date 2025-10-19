-- Investigation of Takeoff ID 8
-- Checking if it exists and its deletion status

PRINT '========================================';
PRINT 'Investigating Takeoff ID 8';
PRINT '========================================';
PRINT '';

-- 1. Check TraceDrawings table for ID = 8
PRINT '1. TraceDrawings Record (ID = 8):';
PRINT '----------------------------------------';
SELECT
    Id,
    DrawingNumber,
    DrawingTitle,
    TakeoffNumber,
    ProjectName,
    IsDeleted,
    CreatedDate,
    LastModified,
    FileName,
    ProcessingStatus
FROM TraceDrawings
WHERE Id = 8;
PRINT '';

-- 2. Check if Takeoff ID 8 exists
PRINT '2. Takeoff Record (ID = 8):';
PRINT '----------------------------------------';
SELECT
    Id,
    TakeoffNumber,
    ProjectName,
    CustomerId,
    CreatedAt,
    UpdatedAt,
    IsDeleted,
    DeletedAt
FROM Takeoffs
WHERE Id = 8;
PRINT '';

-- 3. Check TakeoffRevisions for TakeoffId = 8
PRINT '3. TakeoffRevisions for Takeoff ID 8:';
PRINT '----------------------------------------';
SELECT
    Id,
    TakeoffId,
    RevisionLetter,
    CreatedAt,
    IsDeleted,
    DeletedAt
FROM TakeoffRevisions
WHERE TakeoffId = 8
ORDER BY RevisionLetter;
PRINT '';

-- 4. Check related Traces
PRINT '4. Trace Statistics for Takeoff ID 8:';
PRINT '----------------------------------------';
SELECT
    COUNT(*) as TotalTraces,
    SUM(CASE WHEN IsDeleted = 1 THEN 1 ELSE 0 END) as DeletedTraces,
    SUM(CASE WHEN IsDeleted = 0 THEN 1 ELSE 0 END) as ActiveTraces
FROM Traces
WHERE TakeoffId = 8;
PRINT '';

-- 5. Sample traces to see revision assignments
PRINT '5. Sample Traces for Takeoff ID 8 (showing revision info):';
PRINT '----------------------------------------';
SELECT TOP 10
    Id,
    TraceName,
    RevisionLetter,
    IsDeleted,
    DeletedAt
FROM Traces
WHERE TakeoffId = 8
ORDER BY Id;
PRINT '';

-- 6. Check all TraceDrawings records to see the context
PRINT '6. All TraceDrawings records (to see context):';
PRINT '----------------------------------------';
SELECT
    Id,
    DrawingNumber,
    DrawingTitle,
    TakeoffNumber,
    IsDeleted
FROM TraceDrawings
ORDER BY Id;
PRINT '';

-- 7. Check if TakeoffNumber from TraceDrawings matches any Takeoffs
PRINT '7. Looking for matching Takeoffs by TakeoffNumber:';
PRINT '----------------------------------------';
SELECT
    t.Id,
    t.TakeoffNumber,
    t.ProjectName,
    t.IsDeleted,
    td.Id as TraceDrawingId,
    td.TakeoffNumber as TraceDrawingTakeoffNumber
FROM Takeoffs t
LEFT JOIN TraceDrawings td ON t.TakeoffNumber = td.TakeoffNumber
WHERE td.Id = 8 OR t.Id = 8;
