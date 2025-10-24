/*
 * Migration: Add Measurement Fields to PdfAnnotations Table
 * Date: October 19, 2025
 * Purpose: Add measurement-specific fields for Nutrient measurement annotations
 * Backward Compatible: YES - All fields nullable
 */

-- Add measurement-specific columns to PdfAnnotations table
ALTER TABLE PdfAnnotations
    ADD MeasurementValue NVARCHAR(100) NULL,
        MeasurementScale NVARCHAR(500) NULL,
        MeasurementPrecision NVARCHAR(20) NULL,
        CoordinatesData NVARCHAR(MAX) NULL;

GO

-- Create index on IsMeasurement for faster filtering of measurement annotations
CREATE NONCLUSTERED INDEX IX_PdfAnnotations_IsMeasurement
ON PdfAnnotations(IsMeasurement)
WHERE IsMeasurement = 1
INCLUDE (PackageDrawingId, PageIndex, MeasurementValue);

GO

-- Create index on PageIndex for faster page-based queries
CREATE NONCLUSTERED INDEX IX_PdfAnnotations_PageIndex
ON PdfAnnotations(PackageDrawingId, PageIndex)
INCLUDE (AnnotationId, AnnotationType, IsMeasurement);

GO

-- Verify migration
SELECT
    'Migration Summary' AS [Step],
    COUNT(*) AS [Total Annotations],
    SUM(CASE WHEN IsMeasurement = 1 THEN 1 ELSE 0 END) AS [Measurement Annotations],
    SUM(CASE WHEN IsCalibration = 1 THEN 1 ELSE 0 END) AS [Calibration Annotations],
    SUM(CASE WHEN IsMeasurement = 0 AND IsCalibration = 0 THEN 1 ELSE 0 END) AS [Other Annotations]
FROM PdfAnnotations;

GO
