using System.Text.Json;
using System.Text.RegularExpressions;
using FabOS.WebServer.Models.Entities;
using FabOS.WebServer.Services.Interfaces.Estimate;
using Microsoft.Extensions.Logging;

namespace FabOS.WebServer.Services.Implementations.Estimate;

/// <summary>
/// Formula engine for evaluating worksheet calculations.
/// Supports arithmetic operations, column references, aggregate functions, and cross-worksheet references.
///
/// Supported syntax:
/// - Column references: qty, unit_cost, total_weight
/// - Arithmetic: +, -, *, /, (, )
/// - Aggregate functions: SUM(column), AVG(column), MIN(column), MAX(column), COUNT(column)
/// - Conditional: IF(condition, true_value, false_value)
/// - Cross-worksheet: WorksheetName.SUM(column), WorksheetName.TotalCost
/// </summary>
public class FormulaEngine : IFormulaEngine
{
    private readonly ILogger<FormulaEngine> _logger;

    // Regex patterns for parsing
    private static readonly Regex ColumnReferencePattern = new(@"\b([a-z_][a-z0-9_]*)\b", RegexOptions.IgnoreCase | RegexOptions.Compiled);
    private static readonly Regex AggregateFunctionPattern = new(@"\b(SUM|AVG|MIN|MAX|COUNT)\s*\(\s*([a-z_][a-z0-9_]*)\s*\)", RegexOptions.IgnoreCase | RegexOptions.Compiled);
    private static readonly Regex CrossWorksheetPattern = new(@"([A-Za-z][A-Za-z0-9_\s]*)\.(SUM|AVG|MIN|MAX|COUNT|TotalCost|TotalMaterialCost|TotalLaborCost|TotalLaborHours)\s*(?:\(\s*([a-z_][a-z0-9_]*)\s*\))?", RegexOptions.IgnoreCase | RegexOptions.Compiled);
    private static readonly Regex IfFunctionPattern = new(@"\bIF\s*\(\s*(.+?)\s*,\s*(.+?)\s*,\s*(.+?)\s*\)", RegexOptions.IgnoreCase | RegexOptions.Compiled);
    private static readonly Regex ComparisonPattern = new(@"(.+?)\s*(>=|<=|!=|<>|>|<|==|=)\s*(.+)", RegexOptions.Compiled);

    // Reserved keywords that are not column references
    private static readonly HashSet<string> ReservedKeywords = new(StringComparer.OrdinalIgnoreCase)
    {
        "SUM", "AVG", "MIN", "MAX", "COUNT", "IF", "AND", "OR", "NOT", "TRUE", "FALSE"
    };

    public FormulaEngine(ILogger<FormulaEngine> logger)
    {
        _logger = logger;
    }

    public decimal? EvaluateRowFormula(
        string formula,
        EstimationWorksheetRow row,
        IList<EstimationWorksheetRow> allRows,
        IList<EstimationWorksheetInstanceColumn> columns)
    {
        if (string.IsNullOrWhiteSpace(formula))
            return null;

        try
        {
            // Parse row data from JSON
            var rowData = ParseRowData(row.RowData);

            // Get column keys for validation
            var columnKeys = columns.Select(c => c.ColumnKey.ToLowerInvariant()).ToHashSet();

            // Process the formula
            var processedFormula = formula;

            // Handle IF functions first (recursive)
            processedFormula = ProcessIfFunctions(processedFormula, rowData, allRows, columnKeys);

            // Handle aggregate functions (SUM, AVG, etc.)
            processedFormula = ProcessAggregateFunctions(processedFormula, allRows, columnKeys);

            // Replace column references with values
            processedFormula = ReplaceColumnReferences(processedFormula, rowData, columnKeys);

            // Evaluate the expression
            return EvaluateExpression(processedFormula);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error evaluating formula '{Formula}' for row {RowId}", formula, row.Id);
            return null;
        }
    }

    /// <summary>
    /// Alias for EvaluateRowFormula to maintain backward compatibility.
    /// </summary>
    public decimal? EvaluateFormula(
        string formula,
        EstimationWorksheetRow row,
        IList<EstimationWorksheetRow> allRows,
        IList<EstimationWorksheetInstanceColumn> columns)
    {
        return EvaluateRowFormula(formula, row, allRows, columns);
    }

    /// <summary>
    /// Evaluate a formula using template columns instead of instance columns.
    /// Converts template columns to instance columns internally.
    /// </summary>
    public decimal? EvaluateFormula(
        string formula,
        EstimationWorksheetRow row,
        IList<EstimationWorksheetRow> allRows,
        IList<EstimationWorksheetColumn> templateColumns)
    {
        // Convert template columns to instance columns for the formula engine
        var instanceColumns = templateColumns.Select(tc => new EstimationWorksheetInstanceColumn
        {
            ColumnKey = tc.ColumnKey,
            ColumnName = tc.DisplayName,
            DataType = tc.DataType,
            Formula = tc.Formula,
            IsReadOnly = !tc.IsEditable,
            IsHidden = !tc.IsVisible,
            SortOrder = tc.DisplayOrder
        }).ToList();

        return EvaluateRowFormula(formula, row, allRows, instanceColumns);
    }

    public decimal EvaluateAggregate(string function, string columnKey, IList<EstimationWorksheetRow> rows)
    {
        var values = new List<decimal>();

        foreach (var row in rows.Where(r => !r.IsDeleted && !r.IsGroupHeader))
        {
            var rowData = ParseRowData(row.RowData);
            if (rowData.TryGetValue(columnKey.ToLowerInvariant(), out var value))
            {
                if (TryParseDecimal(value, out var decimalValue))
                {
                    values.Add(decimalValue);
                }
            }
        }

        if (values.Count == 0)
            return 0;

        return function.ToUpperInvariant() switch
        {
            "SUM" => values.Sum(),
            "AVG" or "AVERAGE" => values.Average(),
            "MIN" => values.Min(),
            "MAX" => values.Max(),
            "COUNT" => values.Count,
            _ => 0
        };
    }

    public decimal? EvaluateCrossWorksheetFormula(
        string formula,
        IList<EstimationWorksheet> packageWorksheets)
    {
        if (string.IsNullOrWhiteSpace(formula))
            return null;

        try
        {
            var processedFormula = formula;

            // Find and replace cross-worksheet references
            var matches = CrossWorksheetPattern.Matches(formula);
            foreach (Match match in matches)
            {
                var worksheetName = match.Groups[1].Value.Trim();
                var functionOrProperty = match.Groups[2].Value;
                var columnKey = match.Groups[3].Success ? match.Groups[3].Value : null;

                // Find the worksheet
                var worksheet = packageWorksheets.FirstOrDefault(w =>
                    w.Name.Equals(worksheetName, StringComparison.OrdinalIgnoreCase));

                if (worksheet == null)
                {
                    _logger.LogWarning("Worksheet '{WorksheetName}' not found for cross-worksheet formula", worksheetName);
                    return null;
                }

                decimal value;

                // Handle worksheet properties
                if (string.IsNullOrEmpty(columnKey))
                {
                    value = functionOrProperty.ToUpperInvariant() switch
                    {
                        "TOTALCOST" => worksheet.TotalCost,
                        "TOTALMATERIALCOST" => worksheet.TotalMaterialCost,
                        "TOTALLABORCOST" => worksheet.TotalLaborCost,
                        "TOTALLABORHOURS" => worksheet.TotalLaborHours,
                        _ => 0
                    };
                }
                else
                {
                    // Handle aggregate function on worksheet column
                    value = EvaluateAggregate(functionOrProperty, columnKey, worksheet.Rows.ToList());
                }

                processedFormula = processedFormula.Replace(match.Value, value.ToString("G"));
            }

            // Evaluate the final expression
            return EvaluateExpression(processedFormula);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error evaluating cross-worksheet formula '{Formula}'", formula);
            return null;
        }
    }

    public FormulaValidationResult ValidateFormula(string formula, IList<EstimationWorksheetInstanceColumn> columns)
    {
        // Extract column keys from instance columns and delegate to common validation
        var columnKeys = columns.Select(c => c.ColumnKey).ToList();
        var formulas = columns.Where(c => !string.IsNullOrEmpty(c.Formula))
            .Select(c => (c.ColumnKey, c.Formula!)).ToList();

        return ValidateFormulaInternal(formula, columnKeys, formulas);
    }

    public FormulaValidationResult ValidateTemplateFormula(string formula, IList<EstimationWorksheetColumn> templateColumns)
    {
        // Extract column keys from template columns and delegate to common validation
        var columnKeys = templateColumns.Select(c => c.ColumnKey).ToList();
        var formulas = templateColumns.Where(c => !string.IsNullOrEmpty(c.Formula))
            .Select(c => (c.ColumnKey, c.Formula!)).ToList();

        return ValidateFormulaInternal(formula, columnKeys, formulas);
    }

    private FormulaValidationResult ValidateFormulaInternal(string formula, IList<string> columnKeys, IList<(string ColumnKey, string Formula)> existingFormulas)
    {
        var result = new FormulaValidationResult { IsValid = true };

        if (string.IsNullOrWhiteSpace(formula))
        {
            result.IsValid = false;
            result.ErrorMessage = "Formula cannot be empty";
            return result;
        }

        try
        {
            var columnKeySet = columnKeys.Select(k => k.ToLowerInvariant()).ToHashSet();

            // Extract dependencies
            result.Dependencies = GetDependencies(formula);
            result.WorksheetReferences = GetWorksheetReferences(formula);

            // Validate column references exist
            foreach (var dependency in result.Dependencies)
            {
                if (!columnKeySet.Contains(dependency.ToLowerInvariant()))
                {
                    result.IsValid = false;
                    result.ErrorMessage = $"Unknown column reference: '{dependency}'";
                    return result;
                }
            }

            // Check for circular references (column referencing itself)
            foreach (var col in existingFormulas)
            {
                var deps = GetDependencies(col.Formula);
                if (deps.Any(d => d.Equals(col.ColumnKey, StringComparison.OrdinalIgnoreCase)))
                {
                    result.IsValid = false;
                    result.HasCircularReference = true;
                    result.ErrorMessage = $"Circular reference detected in column '{col.ColumnKey}'";
                    return result;
                }
            }

            // Try to parse and validate syntax (with dummy values)
            var testFormula = formula;
            foreach (var key in columnKeySet)
            {
                testFormula = Regex.Replace(testFormula, $@"\b{Regex.Escape(key)}\b", "1", RegexOptions.IgnoreCase);
            }

            // Replace aggregate functions with dummy values
            testFormula = AggregateFunctionPattern.Replace(testFormula, "1");
            testFormula = CrossWorksheetPattern.Replace(testFormula, "1");
            testFormula = IfFunctionPattern.Replace(testFormula, "1");

            // Try to evaluate
            EvaluateExpression(testFormula);
        }
        catch (Exception ex)
        {
            result.IsValid = false;
            result.ErrorMessage = $"Invalid formula syntax: {ex.Message}";
        }

        return result;
    }

    public IList<string> GetDependencies(string formula)
    {
        var dependencies = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        if (string.IsNullOrWhiteSpace(formula))
            return dependencies.ToList();

        // Remove aggregate function names and cross-worksheet references
        var cleanFormula = AggregateFunctionPattern.Replace(formula, m =>
        {
            dependencies.Add(m.Groups[2].Value);
            return "";
        });

        cleanFormula = CrossWorksheetPattern.Replace(cleanFormula, "");
        cleanFormula = IfFunctionPattern.Replace(cleanFormula, m =>
        {
            // Process condition, true, and false parts
            return $"{m.Groups[1].Value} {m.Groups[2].Value} {m.Groups[3].Value}";
        });

        // Find all potential column references
        var matches = ColumnReferencePattern.Matches(cleanFormula);
        foreach (Match match in matches)
        {
            var token = match.Groups[1].Value;
            if (!ReservedKeywords.Contains(token) && !decimal.TryParse(token, out _))
            {
                dependencies.Add(token);
            }
        }

        return dependencies.ToList();
    }

    public IList<string> GetWorksheetReferences(string formula)
    {
        var references = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        if (string.IsNullOrWhiteSpace(formula))
            return references.ToList();

        var matches = CrossWorksheetPattern.Matches(formula);
        foreach (Match match in matches)
        {
            references.Add(match.Groups[1].Value.Trim());
        }

        return references.ToList();
    }

    #region Private Helper Methods

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
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error parsing row data JSON: {Json}", rowDataJson);
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

    private string ProcessIfFunctions(string formula, Dictionary<string, object?> rowData, IList<EstimationWorksheetRow> allRows, HashSet<string> columnKeys)
    {
        var result = formula;
        var match = IfFunctionPattern.Match(result);

        while (match.Success)
        {
            var condition = match.Groups[1].Value;
            var trueValue = match.Groups[2].Value;
            var falseValue = match.Groups[3].Value;

            // Evaluate condition
            var conditionResult = EvaluateCondition(condition, rowData, columnKeys);

            // Replace IF with the appropriate value
            var replacement = conditionResult ? trueValue : falseValue;
            result = result.Substring(0, match.Index) + replacement + result.Substring(match.Index + match.Length);

            match = IfFunctionPattern.Match(result);
        }

        return result;
    }

    private bool EvaluateCondition(string condition, Dictionary<string, object?> rowData, HashSet<string> columnKeys)
    {
        var match = ComparisonPattern.Match(condition);
        if (!match.Success)
        {
            // Try to evaluate as boolean expression
            var processedCondition = ReplaceColumnReferences(condition, rowData, columnKeys);
            var result = EvaluateExpression(processedCondition);
            return result.HasValue && result.Value != 0;
        }

        var left = ReplaceColumnReferences(match.Groups[1].Value.Trim(), rowData, columnKeys);
        var op = match.Groups[2].Value;
        var right = ReplaceColumnReferences(match.Groups[3].Value.Trim(), rowData, columnKeys);

        var leftValue = EvaluateExpression(left) ?? 0;
        var rightValue = EvaluateExpression(right) ?? 0;

        return op switch
        {
            ">" => leftValue > rightValue,
            "<" => leftValue < rightValue,
            ">=" => leftValue >= rightValue,
            "<=" => leftValue <= rightValue,
            "=" or "==" => leftValue == rightValue,
            "!=" or "<>" => leftValue != rightValue,
            _ => false
        };
    }

    private string ProcessAggregateFunctions(string formula, IList<EstimationWorksheetRow> allRows, HashSet<string> columnKeys)
    {
        return AggregateFunctionPattern.Replace(formula, match =>
        {
            var function = match.Groups[1].Value;
            var columnKey = match.Groups[2].Value;

            if (columnKeys.Contains(columnKey.ToLowerInvariant()))
            {
                var result = EvaluateAggregate(function, columnKey, allRows);
                return result.ToString("G");
            }

            return "0";
        });
    }

    private string ReplaceColumnReferences(string formula, Dictionary<string, object?> rowData, HashSet<string> columnKeys)
    {
        return ColumnReferencePattern.Replace(formula, match =>
        {
            var token = match.Groups[1].Value;

            // Skip reserved keywords and numbers
            if (ReservedKeywords.Contains(token) || decimal.TryParse(token, out _))
                return token;

            // Check if it's a column reference
            if (columnKeys.Contains(token.ToLowerInvariant()))
            {
                if (rowData.TryGetValue(token, out var value))
                {
                    if (TryParseDecimal(value, out var decimalValue))
                    {
                        return decimalValue.ToString("G");
                    }
                }
                return "0";
            }

            return token;
        });
    }

    private decimal? EvaluateExpression(string expression)
    {
        if (string.IsNullOrWhiteSpace(expression))
            return null;

        try
        {
            // Simple expression evaluator using DataTable.Compute
            // This handles basic arithmetic: +, -, *, /, (, )
            var dataTable = new System.Data.DataTable();
            var result = dataTable.Compute(expression, null);

            if (result == DBNull.Value || result == null)
                return null;

            return Convert.ToDecimal(result);
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Error evaluating expression: {Expression}", expression);
            return null;
        }
    }

    private bool TryParseDecimal(object? value, out decimal result)
    {
        result = 0;

        if (value == null)
            return false;

        if (value is decimal d)
        {
            result = d;
            return true;
        }

        if (value is double dbl)
        {
            result = (decimal)dbl;
            return true;
        }

        if (value is int i)
        {
            result = i;
            return true;
        }

        if (value is long l)
        {
            result = l;
            return true;
        }

        if (value is string s && decimal.TryParse(s, out var parsed))
        {
            result = parsed;
            return true;
        }

        return false;
    }

    #endregion
}
