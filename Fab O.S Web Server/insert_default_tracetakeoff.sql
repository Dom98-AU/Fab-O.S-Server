-- Insert a default TraceTakeoff record for package-based takeoffs
-- This is needed because TraceTakeoffMeasurements has a required foreign key to TraceTakeoffs

-- Check if ID 1 already exists
IF NOT EXISTS (SELECT 1 FROM TraceTakeoffs WHERE Id = 1)
BEGIN
    SET IDENTITY_INSERT TraceTakeoffs ON;

    INSERT INTO TraceTakeoffs (
        Id,
        TakeoffNumber,
        ProjectName,
        ProjectId,
        Status,
        CreatedDate,
        CompanyId
    )
    VALUES (
        1,
        'PACKAGE-DEFAULT',
        'Package-Based Takeoffs (Default)',
        NULL,
        'Active',
        GETUTCDATE(),
        1
    );

    SET IDENTITY_INSERT TraceTakeoffs OFF;

    PRINT 'Default TraceTakeoff record created with ID 1';
END
ELSE
BEGIN
    PRINT 'Default TraceTakeoff record with ID 1 already exists';
END
GO
