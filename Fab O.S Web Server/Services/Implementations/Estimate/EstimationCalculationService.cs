using System.Text.Json;
using FabOS.WebServer.Data.Contexts;
using FabOS.WebServer.Models.Entities;
using FabOS.WebServer.Services.Interfaces.Estimate;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace FabOS.WebServer.Services.Implementations.Estimate;

/// <summary>
/// Service for managing estimation calculations and total aggregation.
/// Handles recalculation of worksheets, packages, revisions, and estimations.
/// </summary>
public class EstimationCalculationService : IEstimationCalculationService
{
    private readonly ApplicationDbContext _context;
    private readonly IFormulaEngine _formulaEngine;
    private readonly ILogger<EstimationCalculationService> _logger;

    // Standard column keys for cost aggregation
    private const string TotalCostColumnKey = "total_cost";
    private const string MaterialCostColumnKey = "material_cost";
    private const string LaborCostColumnKey = "labor_cost";
    private const string LaborHoursColumnKey = "labor_hours";

    public EstimationCalculationService(
        ApplicationDbContext context,
        IFormulaEngine formulaEngine,
        ILogger<EstimationCalculationService> logger)
    {
        _context = context;
        _formulaEngine = formulaEngine;
        _logger = logger;
    }

    public async Task<EstimationWorksheetRow> RecalculateRowAsync(
        EstimationWorksheetRow row,
        EstimationWorksheet worksheet,
        IList<EstimationWorksheetRow> allRows,
        IList<EstimationWorksheetInstanceColumn> columns)
    {
        if (row.IsGroupHeader || row.IsDeleted)
            return row;

        try
        {
            // Parse existing row data
            var rowData = ParseRowData(row.RowData);

            // Get computed columns sorted by dependency order
            var computedColumns = GetComputedColumnsInOrder(columns);

            // Calculate each computed column
            foreach (var column in computedColumns)
            {
                if (string.IsNullOrEmpty(column.Formula))
                    continue;

                var result = _formulaEngine.EvaluateRowFormula(
                    column.Formula,
                    row,
                    allRows,
                    columns);

                if (result.HasValue)
                {
                    rowData[column.ColumnKey] = result.Value;
                }
            }

            // Serialize updated row data back
            row.RowData = JsonSerializer.Serialize(rowData);

            // Update the CalculatedTotal if there's a total_cost column
            if (rowData.TryGetValue(TotalCostColumnKey, out var totalValue))
            {
                row.CalculatedTotal = Convert.ToDecimal(totalValue);
            }

            return row;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error recalculating row {RowId} in worksheet {WorksheetId}",
                row.Id, worksheet.Id);
            return row;
        }
    }

    public async Task<EstimationWorksheet?> RecalculateWorksheetAsync(int worksheetId)
    {
        var worksheet = await _context.EstimationWorksheets
            .Include(w => w.Columns.Where(c => !c.IsDeleted).OrderBy(c => c.SortOrder))
            .Include(w => w.Rows.Where(r => !r.IsDeleted).OrderBy(r => r.SortOrder))
            .FirstOrDefaultAsync(w => w.Id == worksheetId && !w.IsDeleted);

        if (worksheet == null)
        {
            // Worksheet not found or was deleted - gracefully return null
            _logger.LogWarning("Worksheet {WorksheetId} not found for recalculation", worksheetId);
            return null;
        }

        var columns = worksheet.Columns.ToList();
        var rows = worksheet.Rows.ToList();

        // Recalculate all rows
        foreach (var row in rows.Where(r => !r.IsGroupHeader))
        {
            await RecalculateRowAsync(row, worksheet, rows, columns);
        }

        // Calculate worksheet totals
        var summary = CalculateWorksheetSummary(worksheet, rows, columns);

        worksheet.TotalMaterialCost = summary.TotalMaterialCost;
        worksheet.TotalLaborHours = summary.TotalLaborHours;
        worksheet.TotalLaborCost = summary.TotalLaborCost;
        worksheet.TotalCost = summary.TotalCost;
        worksheet.ModifiedDate = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        _logger.LogInformation("Recalculated worksheet {WorksheetId}: TotalCost={TotalCost}",
            worksheetId, worksheet.TotalCost);

        return worksheet;
    }

    public async Task<EstimationRevisionPackage?> RecalculatePackageAsync(int packageId)
    {
        var package = await _context.EstimationRevisionPackages
            .Include(p => p.Worksheets.Where(w => !w.IsDeleted))
            .FirstOrDefaultAsync(p => p.Id == packageId && !p.IsDeleted);

        if (package == null)
        {
            // Package not found or was deleted - gracefully return null
            _logger.LogWarning("Package {PackageId} not found for recalculation", packageId);
            return null;
        }

        // Recalculate each worksheet
        foreach (var worksheet in package.Worksheets)
        {
            await RecalculateWorksheetAsync(worksheet.Id);
        }

        // Reload to get updated worksheet totals
        await _context.Entry(package).ReloadAsync();
        await _context.Entry(package).Collection(p => p.Worksheets).LoadAsync();

        // Aggregate package totals from worksheets
        var worksheets = package.Worksheets.Where(w => !w.IsDeleted).ToList();

        package.MaterialCost = worksheets.Sum(w => w.TotalMaterialCost);
        package.LaborHours = worksheets.Sum(w => w.TotalLaborHours);
        package.LaborCost = worksheets.Sum(w => w.TotalLaborCost);

        // Package total = Material + Labor + Overhead
        var subtotal = package.MaterialCost + package.LaborCost;
        package.OverheadCost = subtotal * (package.OverheadPercentage / 100m);
        package.PackageTotal = subtotal + package.OverheadCost;

        package.ModifiedDate = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        _logger.LogInformation("Recalculated package {PackageId}: Total={PackageTotal}",
            packageId, package.PackageTotal);

        return package;
    }

    public async Task<EstimationRevision?> RecalculateRevisionAsync(int revisionId)
    {
        var revision = await _context.EstimationRevisions
            .Include(r => r.Packages.Where(p => !p.IsDeleted))
            .FirstOrDefaultAsync(r => r.Id == revisionId && !r.IsDeleted);

        if (revision == null)
        {
            // Revision not found or was deleted - gracefully return null
            _logger.LogWarning("Revision {RevisionId} not found for recalculation", revisionId);
            return null;
        }

        // Recalculate each package
        foreach (var package in revision.Packages)
        {
            await RecalculatePackageAsync(package.Id);
        }

        // Reload to get updated package totals
        await _context.Entry(revision).ReloadAsync();
        await _context.Entry(revision).Collection(r => r.Packages).LoadAsync();

        // Aggregate revision totals from packages
        var packages = revision.Packages.Where(p => !p.IsDeleted).ToList();

        revision.TotalMaterialCost = packages.Sum(p => p.MaterialCost);
        revision.TotalLaborHours = packages.Sum(p => p.LaborHours);
        revision.TotalLaborCost = packages.Sum(p => p.LaborCost);

        // Calculate cost breakdown with overhead and margin
        var subtotal = revision.TotalMaterialCost + revision.TotalLaborCost;
        var breakdown = CalculateCostBreakdown(subtotal, revision.OverheadPercentage, revision.MarginPercentage);

        revision.Subtotal = breakdown.Subtotal;
        revision.OverheadAmount = breakdown.OverheadAmount;
        revision.MarginAmount = breakdown.MarginAmount;
        revision.TotalAmount = breakdown.TotalAmount;

        revision.ModifiedDate = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        // Also update parent estimation if needed
        await UpdateEstimationTotalAsync(revision.EstimationId);

        _logger.LogInformation("Recalculated revision {RevisionId}: Total={TotalAmount}",
            revisionId, revision.TotalAmount);

        return revision;
    }

    public async Task<Dictionary<string, decimal>> GetColumnTotalsAsync(int worksheetId)
    {
        var worksheet = await _context.EstimationWorksheets
            .Include(w => w.Columns.Where(c => !c.IsDeleted))
            .Include(w => w.Rows.Where(r => !r.IsDeleted && !r.IsGroupHeader))
            .FirstOrDefaultAsync(w => w.Id == worksheetId && !w.IsDeleted);

        if (worksheet == null)
        {
            return new Dictionary<string, decimal>();
        }

        var totals = new Dictionary<string, decimal>(StringComparer.OrdinalIgnoreCase);
        var numericColumns = worksheet.Columns
            .Where(c => c.DataType is "number" or "currency" or "computed")
            .ToList();

        foreach (var column in numericColumns)
        {
            var columnTotal = _formulaEngine.EvaluateAggregate(
                "SUM",
                column.ColumnKey,
                worksheet.Rows.ToList());

            totals[column.ColumnKey] = columnTotal;
        }

        return totals;
    }

    public async Task<WorksheetSummary> GetWorksheetSummaryAsync(int worksheetId)
    {
        var worksheet = await _context.EstimationWorksheets
            .Include(w => w.Columns.Where(c => !c.IsDeleted))
            .Include(w => w.Rows.Where(r => !r.IsDeleted && !r.IsGroupHeader))
            .FirstOrDefaultAsync(w => w.Id == worksheetId && !w.IsDeleted);

        if (worksheet == null)
        {
            throw new ArgumentException($"Worksheet {worksheetId} not found");
        }

        var columnTotals = await GetColumnTotalsAsync(worksheetId);

        return new WorksheetSummary
        {
            WorksheetId = worksheet.Id,
            WorksheetName = worksheet.Name,
            WorksheetType = worksheet.WorksheetType,
            TotalMaterialCost = worksheet.TotalMaterialCost,
            TotalLaborHours = worksheet.TotalLaborHours,
            TotalLaborCost = worksheet.TotalLaborCost,
            TotalCost = worksheet.TotalCost,
            RowCount = worksheet.Rows.Count(r => !r.IsGroupHeader),
            ColumnTotals = columnTotals
        };
    }

    public async Task<PackageSummary> GetPackageSummaryAsync(int packageId)
    {
        var package = await _context.EstimationRevisionPackages
            .Include(p => p.Worksheets.Where(w => !w.IsDeleted))
            .FirstOrDefaultAsync(p => p.Id == packageId && !p.IsDeleted);

        if (package == null)
        {
            throw new ArgumentException($"Package {packageId} not found");
        }

        var worksheetSummaries = new List<WorksheetSummary>();
        foreach (var worksheet in package.Worksheets)
        {
            var summary = await GetWorksheetSummaryAsync(worksheet.Id);
            worksheetSummaries.Add(summary);
        }

        return new PackageSummary
        {
            PackageId = package.Id,
            PackageName = package.Name,
            MaterialCost = package.MaterialCost,
            LaborHours = package.LaborHours,
            LaborCost = package.LaborCost,
            OverheadCost = package.OverheadCost,
            PackageTotal = package.PackageTotal,
            WorksheetCount = worksheetSummaries.Count,
            Worksheets = worksheetSummaries
        };
    }

    public async Task<RevisionSummary> GetRevisionSummaryAsync(int revisionId)
    {
        var revision = await _context.EstimationRevisions
            .Include(r => r.Packages.Where(p => !p.IsDeleted))
            .FirstOrDefaultAsync(r => r.Id == revisionId && !r.IsDeleted);

        if (revision == null)
        {
            throw new ArgumentException($"Revision {revisionId} not found");
        }

        var packageSummaries = new List<PackageSummary>();
        foreach (var package in revision.Packages)
        {
            var summary = await GetPackageSummaryAsync(package.Id);
            packageSummaries.Add(summary);
        }

        return new RevisionSummary
        {
            RevisionId = revision.Id,
            RevisionLetter = revision.RevisionLetter,
            Status = revision.Status,
            TotalMaterialCost = revision.TotalMaterialCost,
            TotalLaborHours = revision.TotalLaborHours,
            TotalLaborCost = revision.TotalLaborCost,
            Subtotal = revision.Subtotal,
            OverheadPercentage = revision.OverheadPercentage,
            OverheadAmount = revision.OverheadAmount,
            MarginPercentage = revision.MarginPercentage,
            MarginAmount = revision.MarginAmount,
            TotalAmount = revision.TotalAmount,
            PackageCount = packageSummaries.Count,
            Packages = packageSummaries
        };
    }

    public CostBreakdown CalculateCostBreakdown(decimal subtotal, decimal overheadPercentage, decimal marginPercentage)
    {
        // Calculate overhead on subtotal
        var overheadAmount = subtotal * (overheadPercentage / 100m);
        var subtotalWithOverhead = subtotal + overheadAmount;

        // Calculate margin on subtotal + overhead
        var marginAmount = subtotalWithOverhead * (marginPercentage / 100m);
        var totalAmount = subtotalWithOverhead + marginAmount;

        return new CostBreakdown
        {
            Subtotal = subtotal,
            OverheadPercentage = overheadPercentage,
            OverheadAmount = overheadAmount,
            SubtotalWithOverhead = subtotalWithOverhead,
            MarginPercentage = marginPercentage,
            MarginAmount = marginAmount,
            TotalAmount = totalAmount
        };
    }

    #region Private Helper Methods

    private async Task UpdateEstimationTotalAsync(int estimationId)
    {
        var estimation = await _context.Estimations
            .Include(e => e.Revisions.Where(r => !r.IsDeleted))
            .FirstOrDefaultAsync(e => e.Id == estimationId);

        if (estimation == null)
            return;

        // Find the current (active) revision - typically the latest approved or the latest draft
        var currentRevision = estimation.Revisions
            .Where(r => !r.IsDeleted)
            .OrderByDescending(r => r.Status == "Approved")
            .ThenByDescending(r => r.RevisionLetter)
            .FirstOrDefault();

        if (currentRevision != null)
        {
            estimation.CurrentRevisionLetter = currentRevision.RevisionLetter;
            estimation.CurrentTotal = currentRevision.TotalAmount;
            estimation.ModifiedDate = DateTime.UtcNow;

            await _context.SaveChangesAsync();
        }
    }

    private WorksheetSummary CalculateWorksheetSummary(
        EstimationWorksheet worksheet,
        IList<EstimationWorksheetRow> rows,
        IList<EstimationWorksheetInstanceColumn> columns)
    {
        var columnTotals = new Dictionary<string, decimal>(StringComparer.OrdinalIgnoreCase);

        // Calculate totals for numeric columns
        var numericColumns = columns
            .Where(c => c.DataType is "number" or "currency" or "computed")
            .ToList();

        foreach (var column in numericColumns)
        {
            var total = _formulaEngine.EvaluateAggregate("SUM", column.ColumnKey, rows);
            columnTotals[column.ColumnKey] = total;
        }

        // Extract standard cost values
        var materialCost = columnTotals.GetValueOrDefault(MaterialCostColumnKey, 0m);
        var laborCost = columnTotals.GetValueOrDefault(LaborCostColumnKey, 0m);
        var laborHours = columnTotals.GetValueOrDefault(LaborHoursColumnKey, 0m);
        var totalCost = columnTotals.GetValueOrDefault(TotalCostColumnKey, 0m);

        // If no explicit total_cost, sum material + labor
        if (totalCost == 0 && (materialCost > 0 || laborCost > 0))
        {
            totalCost = materialCost + laborCost;
        }

        return new WorksheetSummary
        {
            WorksheetId = worksheet.Id,
            WorksheetName = worksheet.Name,
            WorksheetType = worksheet.WorksheetType,
            TotalMaterialCost = materialCost,
            TotalLaborHours = laborHours,
            TotalLaborCost = laborCost,
            TotalCost = totalCost,
            RowCount = rows.Count(r => !r.IsGroupHeader),
            ColumnTotals = columnTotals
        };
    }

    private IList<EstimationWorksheetInstanceColumn> GetComputedColumnsInOrder(IList<EstimationWorksheetInstanceColumn> columns)
    {
        // Get computed columns
        var computedColumns = columns
            .Where(c => !string.IsNullOrEmpty(c.Formula) && !c.IsDeleted)
            .ToList();

        // Simple topological sort based on dependencies
        var sorted = new List<EstimationWorksheetInstanceColumn>();
        var visited = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var column in computedColumns)
        {
            VisitColumn(column, computedColumns, sorted, visited);
        }

        return sorted;
    }

    private void VisitColumn(
        EstimationWorksheetInstanceColumn column,
        IList<EstimationWorksheetInstanceColumn> allComputed,
        IList<EstimationWorksheetInstanceColumn> sorted,
        HashSet<string> visited)
    {
        if (visited.Contains(column.ColumnKey))
            return;

        visited.Add(column.ColumnKey);

        // Get dependencies for this column
        var dependencies = _formulaEngine.GetDependencies(column.Formula!);

        // Visit dependencies first
        foreach (var dep in dependencies)
        {
            var depColumn = allComputed.FirstOrDefault(c =>
                c.ColumnKey.Equals(dep, StringComparison.OrdinalIgnoreCase));

            if (depColumn != null)
            {
                VisitColumn(depColumn, allComputed, sorted, visited);
            }
        }

        sorted.Add(column);
    }

    private Dictionary<string, object?> ParseRowData(string rowDataJson)
    {
        if (string.IsNullOrWhiteSpace(rowDataJson) || rowDataJson == "{}")
            return new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);

        try
        {
            var data = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(rowDataJson);
            if (data == null)
                return new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);

            var result = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
            foreach (var kvp in data)
            {
                result[kvp.Key] = GetJsonElementValue(kvp.Value);
            }
            return result;
        }
        catch
        {
            return new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
        }
    }

    private object? GetJsonElementValue(JsonElement element)
    {
        return element.ValueKind switch
        {
            JsonValueKind.Number => element.GetDecimal(),
            JsonValueKind.String => element.GetString(),
            JsonValueKind.True => true,
            JsonValueKind.False => false,
            JsonValueKind.Null => null,
            _ => element.ToString()
        };
    }

    #endregion
}
