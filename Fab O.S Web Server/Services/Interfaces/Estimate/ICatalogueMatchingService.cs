using FabOS.WebServer.Models.Entities;

namespace FabOS.WebServer.Services.Interfaces.Estimate;

/// <summary>
/// Service for matching imported data to catalogue items and extracting field values.
/// </summary>
public interface ICatalogueMatchingService
{
    /// <summary>
    /// Match imported rows to catalogue items by Description/Profile.
    /// </summary>
    /// <param name="companyId">The company ID for scoping catalogue items</param>
    /// <param name="rows">The rows to match</param>
    /// <param name="matchField">The field to match on (Description or Profile)</param>
    /// <returns>List of match results with confidence scores</returns>
    Task<List<CatalogueMatchResult>> MatchRowsAsync(
        int companyId,
        List<ImportedRow> rows,
        string matchField = "Description");

    /// <summary>
    /// Match a single value to a catalogue item.
    /// </summary>
    Task<CatalogueMatchResult> MatchSingleAsync(
        int companyId,
        string value,
        string matchField = "Description");

    /// <summary>
    /// Get catalogue item field value by field name using reflection.
    /// </summary>
    object? GetFieldValue(CatalogueItem item, string fieldName);

    /// <summary>
    /// Search catalogue items for picker UI.
    /// </summary>
    Task<List<CatalogueItem>> SearchItemsAsync(
        int companyId,
        string searchTerm,
        int maxResults = 20);

    /// <summary>
    /// Get a catalogue item by ID.
    /// </summary>
    Task<CatalogueItem?> GetItemByIdAsync(int companyId, int itemId);
}

/// <summary>
/// Result of matching an imported row to a catalogue item.
/// </summary>
public class CatalogueMatchResult
{
    public int RowIndex { get; set; }
    public string ImportedValue { get; set; } = string.Empty;
    public CatalogueItem? MatchedItem { get; set; }
    public double MatchConfidence { get; set; }
    public bool IsExactMatch { get; set; }
    public List<CatalogueItem> AlternativeMatches { get; set; } = new();
    public string? MatchField { get; set; }
}

/// <summary>
/// Represents an imported row for matching purposes.
/// </summary>
public class ImportedRow
{
    public int RowIndex { get; set; }
    public string? Description { get; set; }
    public string? Profile { get; set; }
    public Dictionary<string, string?> Values { get; set; } = new();
}
