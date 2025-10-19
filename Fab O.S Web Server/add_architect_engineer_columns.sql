-- Check if TakeoffNumber column exists before adding it
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('TraceDrawings') AND name = 'TakeoffNumber')
BEGIN
    ALTER TABLE TraceDrawings ADD TakeoffNumber NVARCHAR(50) NULL;
END

-- Check if columns exist before adding them
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('TraceDrawings') AND name = 'ProjectArchitect')
BEGIN
    ALTER TABLE TraceDrawings ADD ProjectArchitect NVARCHAR(200) NULL;
END

IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('TraceDrawings') AND name = 'ProjectEngineer')
BEGIN
    ALTER TABLE TraceDrawings ADD ProjectEngineer NVARCHAR(200) NULL;
END

-- Remove ProjectNumber column if it exists (as requested by user)
IF EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('TraceDrawings') AND name = 'ProjectNumber')
BEGIN
    ALTER TABLE TraceDrawings DROP COLUMN ProjectNumber;
END

PRINT 'Architect and Engineer columns added successfully';