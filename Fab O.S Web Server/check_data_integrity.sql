-- Check data integrity for Package 16, Drawing 4
-- This script diagnoses the foreign key constraint issue

PRINT '=== Checking PackageDrawing 4 ===';
SELECT
    pd.Id AS PackageDrawingId,
    pd.DrawingNumber,
    pd.PackageId,
    p.PackageName,
    p.RevisionId
FROM PackageDrawings pd
INNER JOIN Packages p ON pd.PackageId = p.Id
WHERE pd.Id = 4;

PRINT '';
PRINT '=== Checking Package and Revision ===';
SELECT
    p.Id AS PackageId,
    p.PackageName,
    p.RevisionId,
    tr.Id AS TakeoffRevisionId,
    tr.RevisionCode,
    tr.TakeoffId
FROM Packages p
LEFT JOIN TakeoffRevisions tr ON p.RevisionId = tr.Id
WHERE p.Id = (SELECT PackageId FROM PackageDrawings WHERE Id = 4);

PRINT '';
PRINT '=== Checking if TraceTakeoff (TraceDrawing) exists ===';
SELECT
    td.Id AS TraceTakeoffId,
    td.TakeoffNumber,
    td.ProjectName,
    td.Status
FROM TraceDrawings td
WHERE td.Id = (
    SELECT tr.TakeoffId
    FROM Packages p
    INNER JOIN TakeoffRevisions tr ON p.RevisionId = tr.Id
    WHERE p.Id = (SELECT PackageId FROM PackageDrawings WHERE Id = 4)
);

PRINT '';
PRINT '=== All TraceDrawings (TraceTakeoffs) in database ===';
SELECT
    Id,
    TakeoffNumber,
    ProjectName,
    Status,
    CreatedDate
FROM TraceDrawings
ORDER BY Id;

PRINT '';
PRINT '=== Orphaned TakeoffRevisions (pointing to non-existent TraceTakeoffs) ===';
SELECT
    tr.Id AS RevisionId,
    tr.RevisionCode,
    tr.TakeoffId AS OrphanedTakeoffId,
    tr.CreatedDate
FROM TakeoffRevisions tr
LEFT JOIN TraceDrawings td ON tr.TakeoffId = td.Id
WHERE tr.TakeoffId IS NOT NULL
  AND td.Id IS NULL;
