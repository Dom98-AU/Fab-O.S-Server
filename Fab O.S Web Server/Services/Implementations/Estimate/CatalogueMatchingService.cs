using Microsoft.EntityFrameworkCore;
using FabOS.WebServer.Data.Contexts;
using FabOS.WebServer.Models.Entities;
using FabOS.WebServer.Services.Interfaces.Estimate;
using FabOS.WebServer.Services.Estimate;

namespace FabOS.WebServer.Services.Implementations.Estimate;

/// <summary>
/// Service for matching imported data to catalogue items.
/// Supports exact and fuzzy matching by Description or Profile fields.
/// </summary>
public class CatalogueMatchingService : ICatalogueMatchingService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<CatalogueMatchingService> _logger;

    public CatalogueMatchingService(
        ApplicationDbContext context,
        ILogger<CatalogueMatchingService> logger)
    {
        _context = context;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<List<CatalogueMatchResult>> MatchRowsAsync(
        int companyId,
        List<ImportedRow> rows,
        string matchField = "Description")
    {
        var results = new List<CatalogueMatchResult>();

        // Get all catalogue items for this company for matching
        var catalogueItems = await _context.CatalogueItems
            .Where(c => c.CompanyId == companyId && c.IsActive)
            .ToListAsync();

        foreach (var row in rows)
        {
            var valueToMatch = matchField.Equals("Profile", StringComparison.OrdinalIgnoreCase)
                ? row.Profile
                : row.Description;

            var result = MatchValue(catalogueItems, valueToMatch, row.RowIndex, matchField);
            results.Add(result);
        }

        return results;
    }

    /// <inheritdoc />
    public async Task<CatalogueMatchResult> MatchSingleAsync(
        int companyId,
        string value,
        string matchField = "Description")
    {
        var catalogueItems = await _context.CatalogueItems
            .Where(c => c.CompanyId == companyId && c.IsActive)
            .ToListAsync();

        return MatchValue(catalogueItems, value, 0, matchField);
    }

    private CatalogueMatchResult MatchValue(
        List<CatalogueItem> catalogueItems,
        string? value,
        int rowIndex,
        string matchField)
    {
        var result = new CatalogueMatchResult
        {
            RowIndex = rowIndex,
            ImportedValue = value ?? string.Empty,
            MatchField = matchField
        };

        if (string.IsNullOrWhiteSpace(value))
        {
            return result;
        }

        var normalizedValue = NormalizeString(value);

        // Try exact match first
        CatalogueItem? exactMatch = null;
        if (matchField.Equals("Profile", StringComparison.OrdinalIgnoreCase))
        {
            exactMatch = catalogueItems.FirstOrDefault(c =>
                NormalizeString(c.Profile) == normalizedValue);
        }
        else
        {
            exactMatch = catalogueItems.FirstOrDefault(c =>
                NormalizeString(c.Description) == normalizedValue);
        }

        if (exactMatch != null)
        {
            result.MatchedItem = exactMatch;
            result.MatchConfidence = 1.0;
            result.IsExactMatch = true;
            return result;
        }

        // Try fuzzy matching
        var scoredMatches = catalogueItems
            .Select(item =>
            {
                var fieldValue = matchField.Equals("Profile", StringComparison.OrdinalIgnoreCase)
                    ? item.Profile
                    : item.Description;

                var score = CalculateSimilarity(normalizedValue, NormalizeString(fieldValue));
                return (Item: item, Score: score);
            })
            .Where(x => x.Score > 0.5) // Minimum 50% similarity
            .OrderByDescending(x => x.Score)
            .Take(5)
            .ToList();

        if (scoredMatches.Any())
        {
            var bestMatch = scoredMatches.First();
            result.MatchedItem = bestMatch.Item;
            result.MatchConfidence = bestMatch.Score;
            result.IsExactMatch = false;

            // Add alternatives (excluding the best match)
            result.AlternativeMatches = scoredMatches.Skip(1).Select(x => x.Item).ToList();
        }

        return result;
    }

    /// <inheritdoc />
    public object? GetFieldValue(CatalogueItem item, string fieldName)
    {
        return CatalogueFieldRegistry.GetFieldValue(item, fieldName);
    }

    /// <inheritdoc />
    public async Task<List<CatalogueItem>> SearchItemsAsync(
        int companyId,
        string searchTerm,
        int maxResults = 20)
    {
        if (string.IsNullOrWhiteSpace(searchTerm))
        {
            return await _context.CatalogueItems
                .Where(c => c.CompanyId == companyId && c.IsActive)
                .Take(maxResults)
                .ToListAsync();
        }

        var normalizedSearch = searchTerm.Trim().ToLower();

        return await _context.CatalogueItems
            .Where(c => c.CompanyId == companyId && c.IsActive &&
                (c.Description.ToLower().Contains(normalizedSearch) ||
                 c.Profile.ToLower().Contains(normalizedSearch) ||
                 c.ItemCode.ToLower().Contains(normalizedSearch) ||
                 c.Category.ToLower().Contains(normalizedSearch)))
            .OrderBy(c => c.Description)
            .Take(maxResults)
            .ToListAsync();
    }

    /// <inheritdoc />
    public async Task<CatalogueItem?> GetItemByIdAsync(int companyId, int itemId)
    {
        return await _context.CatalogueItems
            .FirstOrDefaultAsync(c => c.Id == itemId && c.CompanyId == companyId);
    }

    /// <summary>
    /// Normalize a string for comparison (lowercase, trim, remove extra whitespace).
    /// </summary>
    private static string NormalizeString(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return string.Empty;

        return System.Text.RegularExpressions.Regex
            .Replace(value.Trim().ToLower(), @"\s+", " ");
    }

    /// <summary>
    /// Calculate similarity between two strings using Levenshtein distance.
    /// Returns a value between 0 (no match) and 1 (exact match).
    /// </summary>
    private static double CalculateSimilarity(string source, string target)
    {
        if (string.IsNullOrEmpty(source) || string.IsNullOrEmpty(target))
            return 0;

        if (source == target)
            return 1.0;

        // Check if one contains the other
        if (source.Contains(target) || target.Contains(source))
        {
            var shorter = source.Length < target.Length ? source : target;
            var longer = source.Length < target.Length ? target : source;
            return (double)shorter.Length / longer.Length * 0.9; // 90% max for contains
        }

        // Levenshtein distance
        var distance = LevenshteinDistance(source, target);
        var maxLength = Math.Max(source.Length, target.Length);

        return 1.0 - ((double)distance / maxLength);
    }

    /// <summary>
    /// Calculate Levenshtein distance between two strings.
    /// </summary>
    private static int LevenshteinDistance(string source, string target)
    {
        if (string.IsNullOrEmpty(source))
            return target?.Length ?? 0;

        if (string.IsNullOrEmpty(target))
            return source.Length;

        var matrix = new int[source.Length + 1, target.Length + 1];

        for (var i = 0; i <= source.Length; i++)
            matrix[i, 0] = i;

        for (var j = 0; j <= target.Length; j++)
            matrix[0, j] = j;

        for (var i = 1; i <= source.Length; i++)
        {
            for (var j = 1; j <= target.Length; j++)
            {
                var cost = (source[i - 1] == target[j - 1]) ? 0 : 1;

                matrix[i, j] = Math.Min(
                    Math.Min(matrix[i - 1, j] + 1, matrix[i, j - 1] + 1),
                    matrix[i - 1, j - 1] + cost);
            }
        }

        return matrix[source.Length, target.Length];
    }
}
