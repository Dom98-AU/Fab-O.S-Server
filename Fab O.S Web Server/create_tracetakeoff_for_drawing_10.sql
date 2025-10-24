-- Create missing TraceTakeoff record for TraceDrawing ID 10 (TO0016 - Club Hillsdale)
-- This resolves the foreign key constraint error when saving measurements

PRINT '=== Creating TraceTakeoff for TraceDrawing 10 ===';

-- First, check if TraceDrawing 10 exists
DECLARE @DrawingId INT = 10;
DECLARE @TakeoffNumber NVARCHAR(50);
DECLARE @ProjectName NVARCHAR(MAX);
DECLARE @CompanyId INT = 1; -- Default company

SELECT
    @TakeoffNumber = TakeoffNumber,
    @ProjectName = ProjectName
FROM TraceDrawings
WHERE Id = @DrawingId;

IF @TakeoffNumber IS NULL
BEGIN
    PRINT 'ERROR: TraceDrawing with ID 10 does not exist!';
    RETURN;
END

PRINT 'TraceDrawing found: ' + @TakeoffNumber + ' - ' + @ProjectName;

-- Check if TraceTakeoff already exists with this ID
IF EXISTS (SELECT 1 FROM TraceTakeoffs WHERE Id = @DrawingId)
BEGIN
    PRINT 'TraceTakeoff with ID 10 already exists. No action needed.';
    RETURN;
END

-- Create TraceTakeoff record
-- We need a TraceRecord first
DECLARE @TraceRecordId INT;

-- Check if there's already a TraceRecord for this takeoff
SELECT TOP 1 @TraceRecordId = Id
FROM TraceRecords
WHERE EntityType = 1 -- Assuming 1 = Takeoff entity type
  AND EntityReference = @TakeoffNumber
ORDER BY Id DESC;

IF @TraceRecordId IS NULL
BEGIN
    -- Create a new TraceRecord
    INSERT INTO TraceRecords (
        TraceId,
        TraceNumber,
        EntityType,
        EntityId,
        EntityReference,
        Description,
        CaptureDateTime,
        EventDateTime,
        Status,
        CompanyId,
        CreatedDate
    )
    VALUES (
        NEWID(),
        @TakeoffNumber,
        1, -- Entity type for Takeoff
        @DrawingId,
        @TakeoffNumber,
        @ProjectName,
        GETUTCDATE(),
        GETUTCDATE(),
        2, -- InProgress status
        @CompanyId,
        GETUTCDATE()
    );

    SET @TraceRecordId = SCOPE_IDENTITY();
    PRINT 'Created TraceRecord with ID: ' + CAST(@TraceRecordId AS NVARCHAR(10));
END
ELSE
BEGIN
    PRINT 'Using existing TraceRecord with ID: ' + CAST(@TraceRecordId AS NVARCHAR(10));
END

-- Now create the TraceTakeoff record
SET IDENTITY_INSERT TraceTakeoffs ON;

INSERT INTO TraceTakeoffs (
    Id,
    TraceRecordId,
    DrawingId,
    PdfUrl,
    Status,
    CreatedDate,
    CompanyId
)
VALUES (
    @DrawingId, -- Use the same ID as TraceDrawing
    @TraceRecordId,
    @DrawingId,
    '/api/tracedrawings/' + CAST(@DrawingId AS NVARCHAR(10)) + '/pdf', -- Default PDF URL
    'Draft',
    GETUTCDATE(),
    @CompanyId
);

SET IDENTITY_INSERT TraceTakeoffs OFF;

PRINT 'Successfully created TraceTakeoff with ID: 10';

-- Verify the creation
SELECT
    Id,
    TraceRecordId,
    DrawingId,
    Status,
    CreatedDate
FROM TraceTakeoffs
WHERE Id = @DrawingId;
