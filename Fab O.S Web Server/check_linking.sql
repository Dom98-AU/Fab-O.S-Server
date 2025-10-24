SELECT TOP 10
    pa.Id as AnnotationDbId,
    pa.AnnotationId,
    pa.TraceTakeoffMeasurementId,
    pa.IsMeasurement,
    pa.CreatedDate as AnnotationCreated,
    ttm.Id as MeasurementId,
    ttm.Value as MeasurementValue,
    ttm.Unit,
    ttm.CreatedDate as MeasurementCreated
FROM PdfAnnotations pa
LEFT JOIN TraceTakeoffMeasurements ttm ON pa.TraceTakeoffMeasurementId = ttm.Id
WHERE pa.PackageDrawingId = 4
ORDER BY pa.CreatedDate DESC
