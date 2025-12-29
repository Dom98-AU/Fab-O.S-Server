using FabOS.WebServer.Models.Entities;

namespace FabOS.WebServer.Services.Interfaces.Estimate;

/// <summary>
/// Formula engine for evaluating worksheet calculations.
/// Supports row-level formulas, column aggregates, and cross-worksheet references.
/// </summary>
public interface IFormulaEngine
{
    /// <summary>
    /// Evaluate a formula for a single row.
    /// </summary>
    /// <param name="formula">Formula expression (e.g., "qty * unit_cost")</param>
    /// <param name="row">The row containing column values</param>
    /// <param name="allRows">All rows in the worksheet (for aggregate functions)</param>
    /// <param name="columns">Column definitions for the worksheet</param>
    /// <returns>Calculated value or null if formula is invalid</returns>
    decimal? EvaluateRowFormula(
        string formula,
        EstimationWorksheetRow row,
        IList<EstimationWorksheetRow> allRows,
        IList<EstimationWorksheetInstanceColumn> columns);

    /// <summary>
    /// Evaluate a formula for a single row (alias for EvaluateRowFormula).
    /// </summary>
    decimal? EvaluateFormula(
        string formula,
        EstimationWorksheetRow row,
        IList<EstimationWorksheetRow> allRows,
        IList<EstimationWorksheetInstanceColumn> columns);

    /// <summary>
    /// Evaluate a formula for a single row using template columns.
    /// </summary>
    decimal? EvaluateFormula(
        string formula,
        EstimationWorksheetRow row,
        IList<EstimationWorksheetRow> allRows,
        IList<EstimationWorksheetColumn> templateColumns);

    /// <summary>
    /// Evaluate a column aggregate function (SUM, AVG, MIN, MAX, COUNT).
    /// </summary>
    /// <param name="function">Aggregate function name</param>
    /// <param name="columnKey">Column key to aggregate</param>
    /// <param name="rows">All rows in the worksheet</param>
    /// <returns>Aggregated value</returns>
    decimal EvaluateAggregate(string function, string columnKey, IList<EstimationWorksheetRow> rows);

    /// <summary>
    /// Evaluate a cross-worksheet formula reference.
    /// </summary>
    /// <param name="formula">Formula with worksheet reference (e.g., "MaterialCosts.SUM(total_cost)")</param>
    /// <param name="packageWorksheets">All worksheets in the package</param>
    /// <returns>Calculated value or null if reference is invalid</returns>
    decimal? EvaluateCrossWorksheetFormula(
        string formula,
        IList<EstimationWorksheet> packageWorksheets);

    /// <summary>
    /// Validate a formula's syntax and dependencies.
    /// </summary>
    /// <param name="formula">Formula expression to validate</param>
    /// <param name="columns">Available columns in the worksheet instance</param>
    /// <returns>Validation result with error details if invalid</returns>
    FormulaValidationResult ValidateFormula(string formula, IList<EstimationWorksheetInstanceColumn> columns);

    /// <summary>
    /// Validate a formula's syntax and dependencies using template columns.
    /// </summary>
    /// <param name="formula">Formula expression to validate</param>
    /// <param name="templateColumns">Available columns in the worksheet template</param>
    /// <returns>Validation result with error details if invalid</returns>
    FormulaValidationResult ValidateTemplateFormula(string formula, IList<EstimationWorksheetColumn> templateColumns);

    /// <summary>
    /// Get all column dependencies for a formula.
    /// Used to determine recalculation order.
    /// </summary>
    /// <param name="formula">Formula expression</param>
    /// <returns>List of column keys the formula depends on</returns>
    IList<string> GetDependencies(string formula);

    /// <summary>
    /// Parse a formula and extract worksheet references for cross-worksheet formulas.
    /// </summary>
    /// <param name="formula">Formula expression</param>
    /// <returns>List of worksheet names referenced</returns>
    IList<string> GetWorksheetReferences(string formula);
}

/// <summary>
/// Result of formula validation.
/// </summary>
public class FormulaValidationResult
{
    public bool IsValid { get; set; }
    public string? ErrorMessage { get; set; }
    public string? ErrorPosition { get; set; }
    public IList<string> Dependencies { get; set; } = new List<string>();
    public IList<string> WorksheetReferences { get; set; } = new List<string>();
    public bool HasCircularReference { get; set; }
}

/// <summary>
/// Supported aggregate functions.
/// </summary>
public enum AggregateFunction
{
    Sum,
    Average,
    Min,
    Max,
    Count
}
