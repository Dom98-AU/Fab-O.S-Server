using FabOS.WebServer.Models.Entities;

namespace FabOS.WebServer.Services.Interfaces.Estimate;

/// <summary>
/// Service for managing estimation calculations and total aggregation.
/// Handles recalculation of worksheets, packages, revisions, and estimations.
/// </summary>
public interface IEstimationCalculationService
{
    /// <summary>
    /// Recalculate all computed columns for a single row.
    /// </summary>
    /// <param name="row">The row to recalculate</param>
    /// <param name="worksheet">The worksheet containing the row</param>
    /// <param name="allRows">All rows in the worksheet</param>
    /// <param name="columns">Column definitions</param>
    /// <returns>Updated row with recalculated values</returns>
    Task<EstimationWorksheetRow> RecalculateRowAsync(
        EstimationWorksheetRow row,
        EstimationWorksheet worksheet,
        IList<EstimationWorksheetRow> allRows,
        IList<EstimationWorksheetInstanceColumn> columns);

    /// <summary>
    /// Recalculate all rows and totals for a worksheet.
    /// </summary>
    /// <param name="worksheetId">Worksheet ID</param>
    /// <returns>Updated worksheet with recalculated totals, or null if worksheet not found/deleted</returns>
    Task<EstimationWorksheet?> RecalculateWorksheetAsync(int worksheetId);

    /// <summary>
    /// Recalculate all worksheets and totals for a package.
    /// </summary>
    /// <param name="packageId">Package ID</param>
    /// <returns>Updated package with recalculated totals, or null if package not found/deleted</returns>
    Task<EstimationRevisionPackage?> RecalculatePackageAsync(int packageId);

    /// <summary>
    /// Recalculate all packages and totals for a revision.
    /// </summary>
    /// <param name="revisionId">Revision ID</param>
    /// <returns>Updated revision with recalculated totals, or null if revision not found/deleted</returns>
    Task<EstimationRevision?> RecalculateRevisionAsync(int revisionId);

    /// <summary>
    /// Get the column totals for a worksheet.
    /// </summary>
    /// <param name="worksheetId">Worksheet ID</param>
    /// <returns>Dictionary of column key to total value</returns>
    Task<Dictionary<string, decimal>> GetColumnTotalsAsync(int worksheetId);

    /// <summary>
    /// Get worksheet summary (total material cost, labor cost, labor hours, total cost).
    /// </summary>
    /// <param name="worksheetId">Worksheet ID</param>
    /// <returns>Worksheet summary</returns>
    Task<WorksheetSummary> GetWorksheetSummaryAsync(int worksheetId);

    /// <summary>
    /// Get package summary aggregated from all worksheets.
    /// </summary>
    /// <param name="packageId">Package ID</param>
    /// <returns>Package summary</returns>
    Task<PackageSummary> GetPackageSummaryAsync(int packageId);

    /// <summary>
    /// Get revision summary aggregated from all packages with overhead and margin.
    /// </summary>
    /// <param name="revisionId">Revision ID</param>
    /// <returns>Revision summary</returns>
    Task<RevisionSummary> GetRevisionSummaryAsync(int revisionId);

    /// <summary>
    /// Apply overhead and margin percentages to calculate final amounts.
    /// </summary>
    /// <param name="subtotal">Subtotal before overhead and margin</param>
    /// <param name="overheadPercentage">Overhead percentage (e.g., 15.00)</param>
    /// <param name="marginPercentage">Margin percentage (e.g., 20.00)</param>
    /// <returns>Calculation breakdown</returns>
    CostBreakdown CalculateCostBreakdown(decimal subtotal, decimal overheadPercentage, decimal marginPercentage);
}

/// <summary>
/// Summary of worksheet totals.
/// </summary>
public class WorksheetSummary
{
    public int WorksheetId { get; set; }
    public string WorksheetName { get; set; } = string.Empty;
    public string WorksheetType { get; set; } = string.Empty;
    public decimal TotalMaterialCost { get; set; }
    public decimal TotalLaborHours { get; set; }
    public decimal TotalLaborCost { get; set; }
    public decimal TotalCost { get; set; }
    public int RowCount { get; set; }
    public Dictionary<string, decimal> ColumnTotals { get; set; } = new();
}

/// <summary>
/// Summary of package totals aggregated from worksheets.
/// </summary>
public class PackageSummary
{
    public int PackageId { get; set; }
    public string PackageName { get; set; } = string.Empty;
    public decimal MaterialCost { get; set; }
    public decimal LaborHours { get; set; }
    public decimal LaborCost { get; set; }
    public decimal OverheadCost { get; set; }
    public decimal PackageTotal { get; set; }
    public int WorksheetCount { get; set; }
    public IList<WorksheetSummary> Worksheets { get; set; } = new List<WorksheetSummary>();
}

/// <summary>
/// Summary of revision totals with overhead and margin calculations.
/// </summary>
public class RevisionSummary
{
    public int RevisionId { get; set; }
    public string RevisionLetter { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public decimal TotalMaterialCost { get; set; }
    public decimal TotalLaborHours { get; set; }
    public decimal TotalLaborCost { get; set; }
    public decimal Subtotal { get; set; }
    public decimal OverheadPercentage { get; set; }
    public decimal OverheadAmount { get; set; }
    public decimal MarginPercentage { get; set; }
    public decimal MarginAmount { get; set; }
    public decimal TotalAmount { get; set; }
    public int PackageCount { get; set; }
    public IList<PackageSummary> Packages { get; set; } = new List<PackageSummary>();
}

/// <summary>
/// Cost breakdown showing overhead and margin calculations.
/// </summary>
public class CostBreakdown
{
    public decimal Subtotal { get; set; }
    public decimal OverheadPercentage { get; set; }
    public decimal OverheadAmount { get; set; }
    public decimal SubtotalWithOverhead { get; set; }
    public decimal MarginPercentage { get; set; }
    public decimal MarginAmount { get; set; }
    public decimal TotalAmount { get; set; }
}
