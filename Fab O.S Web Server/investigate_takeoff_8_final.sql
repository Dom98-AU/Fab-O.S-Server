-- Investigation of Takeoff ID 8
-- Checking TraceDrawings ID 8 and related data

PRINT '========================================';
PRINT 'Investigating Takeoff ID 8 / TraceDrawing ID 8';
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
    ProcessingStatus,
    CustomerId,
    ClientName
FROM TraceDrawings
WHERE Id = 8;
PRINT '';

-- 2. Check if there's a Takeoffs table and if takeoff 8 exists
PRINT '2. Checking if Takeoffs table exists:';
PRINT '----------------------------------------';
IF EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'Takeoffs')
BEGIN
    PRINT 'Takeoffs table exists. Checking for ID = 8:';
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
END
ELSE
BEGIN
    PRINT 'Takeoffs table does NOT exist in this database.';
END
PRINT '';

-- 3. Check TakeoffRevisions for TakeoffId = 8
PRINT '3. TakeoffRevisions for TakeoffId = 8:';
PRINT '----------------------------------------';
SELECT
    Id,
    TakeoffId,
    RevisionCode,
    IsActive,
    IsDeleted,
    Description,
    CreatedDate,
    LastModified
FROM TakeoffRevisions
WHERE TakeoffId = 8
ORDER BY RevisionCode;
PRINT '';

-- 4. Check if there's a Traces table
PRINT '4. Checking if Traces table exists:';
PRINT '----------------------------------------';
IF EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'Traces')
BEGIN
    PRINT 'Traces table exists. Checking for TakeoffId = 8:';
    SELECT
        COUNT(*) as TotalTraces,
        SUM(CASE WHEN IsDeleted = 1 THEN 1 ELSE 0 END) as DeletedTraces,
        SUM(CASE WHEN IsDeleted = 0 THEN 1 ELSE 0 END) as ActiveTraces
    FROM Traces
    WHERE TakeoffId = 8;

    PRINT '';
    PRINT 'Sample Traces for TakeoffId = 8:';
    SELECT TOP 10
        Id,
        TraceName,
        RevisionLetter,
        IsDeleted,
        DeletedAt
    FROM Traces
    WHERE TakeoffId = 8
    ORDER BY Id;
END
ELSE
BEGIN
    PRINT 'Traces table does NOT exist in this database.';
END
PRINT '';

-- 5. Check all TraceDrawings records to see context
PRINT '5. All TraceDrawings records (limited to 20):';
PRINT '----------------------------------------';
SELECT TOP 20
    Id,
    DrawingNumber,
    DrawingTitle,
    TakeoffNumber,
    IsDeleted,
    CreatedDate
FROM TraceDrawings
ORDER BY Id;
PRINT '';

-- 6. Check TraceRecords that might relate to this drawing
PRINT '6. TraceRecords that might relate to TraceDrawing ID 8:';
PRINT '----------------------------------------';
SELECT TOP 10
    tr.Id,
    tr.TraceNumber,
    tr.Description,
    tr.IsActive,
    tr.CreatedDate,
    tt.Id as TraceTakeoffId,
    tt.DrawingId,
    tt.Status
FROM TraceRecords tr
LEFT JOIN TraceTakeoffs tt ON tr.Id = tt.TraceRecordId
WHERE tt.DrawingId = 8
ORDER BY tr.CreatedDate DESC;
PRINT '';

-- 7. Summary of all database tables for context
PRINT '7. All database tables (for context):';
PRINT '----------------------------------------';
SELECT TABLE_NAME
FROM INFORMATION_SCHEMA.TABLES
WHERE TABLE_TYPE = 'BASE TABLE'
ORDER BY TABLE_NAME;
