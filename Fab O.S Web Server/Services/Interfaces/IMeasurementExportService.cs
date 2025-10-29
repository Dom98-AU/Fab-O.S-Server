namespace FabOS.WebServer.Services.Interfaces;

/// <summary>
/// Service for exporting takeoff measurements to various formats
/// </summary>
public interface IMeasurementExportService
{
    /// <summary>
    /// Exports measurements for a package drawing to Excel with full context
    /// including takeoff, revision, package, and drawing details
    /// </summary>
    /// <param name="packageDrawingId">The package drawing ID containing the measurements</param>
    /// <param name="groupByItem">Group measurements by catalogue item and sum quantities (default: true)</param>
    /// <returns>Excel file (.xlsx) as byte array</returns>
    Task<byte[]> ExportMeasurementsToExcelAsync(int packageDrawingId, bool groupByItem = true);
}
