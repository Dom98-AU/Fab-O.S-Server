using System.Collections.Generic;

namespace FabOS.WebServer.Models.Filtering;

public interface IFilterProvider
{
    List<FilterDefinition> GetFilterDefinitions();
    List<FilterRule> GetActiveFilters();
    void ApplyFilter(FilterRule rule);
    void RemoveFilter(string ruleId);
    void ClearFilters();
    void UpdateFilter(FilterRule rule);
    bool ValidateFilterValue(FilterDefinition definition, object? value);
    object? ConvertFilterValue(FilterDefinition definition, string input);
    Dictionary<string, List<object>> GetDistinctValues(string propertyName, int maxCount = 100);
}