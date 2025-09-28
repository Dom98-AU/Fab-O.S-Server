using Microsoft.AspNetCore.Components;
using FabOS.WebServer.Models.Filtering;
using System.Collections.Generic;
using System.Linq;

namespace FabOS.WebServer.Components.Shared;

public partial class FilterSystem : ComponentBase
{
    [Parameter] public IFilterProvider? FilterProvider { get; set; }
    [Parameter] public List<FilterDefinition> FilterDefinitions { get; set; } = new();
    [Parameter] public List<FilterRule> ActiveFilters { get; set; } = new();
    [Parameter] public EventCallback<List<FilterRule>> OnFiltersChanged { get; set; }
    [Parameter] public string CssClass { get; set; } = "";

    private bool IsOpen = false;
    private List<FilterRule> tempRules = new();
    private Dictionary<string, List<string>> multiSelectValues = new();

    protected override void OnInitialized()
    {
        if (FilterProvider != null)
        {
            FilterDefinitions = FilterProvider.GetFilterDefinitions();
            ActiveFilters = FilterProvider.GetActiveFilters();
        }

        tempRules = new List<FilterRule>();
    }

    private void ToggleFilter()
    {
        IsOpen = !IsOpen;
        if (IsOpen)
        {
            tempRules = ActiveFilters.Select(f => new FilterRule
            {
                Id = f.Id,
                Field = f.Field,
                Operator = f.Operator,
                Value = f.Value,
                SecondValue = f.SecondValue,
                LogicalOperator = f.LogicalOperator,
                IsActive = f.IsActive
            }).ToList();
        }
    }

    private void OnOperatorChange(string field, string? operatorValue)
    {
        if (string.IsNullOrEmpty(operatorValue))
        {
            tempRules.RemoveAll(r => r.Field == field);
            return;
        }

        if (Enum.TryParse<FilterOperator>(operatorValue, out var op))
        {
            var existingRule = tempRules.FirstOrDefault(r => r.Field == field);
            if (existingRule != null)
            {
                existingRule.Operator = op;
                if (IsNullOperator(op))
                {
                    existingRule.Value = null;
                    existingRule.SecondValue = null;
                }
            }
            else
            {
                tempRules.Add(new FilterRule
                {
                    Field = field,
                    Operator = op,
                    IsActive = true
                });
            }
        }
        StateHasChanged();
    }

    private bool IsNullOperator(FilterOperator op)
    {
        return op == FilterOperator.IsNull ||
               op == FilterOperator.IsNotNull ||
               op == FilterOperator.IsTrue ||
               op == FilterOperator.IsFalse;
    }

    private void OnMultiSelectChange(FilterRule rule, string optionKey, bool isChecked)
    {
        if (!multiSelectValues.ContainsKey(rule.Field))
        {
            multiSelectValues[rule.Field] = new List<string>();
        }

        if (isChecked)
        {
            if (!multiSelectValues[rule.Field].Contains(optionKey))
            {
                multiSelectValues[rule.Field].Add(optionKey);
            }
        }
        else
        {
            multiSelectValues[rule.Field].Remove(optionKey);
        }

        rule.Value = string.Join(",", multiSelectValues[rule.Field]);
    }

    private async Task ApplyFieldFilter(FilterDefinition definition, FilterRule rule)
    {
        if (rule.Value != null || IsNullOperator(rule.Operator))
        {
            var newFilter = new FilterRule
            {
                Id = Guid.NewGuid().ToString(),
                Field = rule.Field,
                Operator = rule.Operator,
                Value = rule.Value,
                SecondValue = rule.SecondValue,
                IsActive = true
            };

            ActiveFilters.Add(newFilter);
            tempRules.Remove(rule);

            if (FilterProvider != null)
            {
                FilterProvider.ApplyFilter(newFilter);
            }

            if (OnFiltersChanged.HasDelegate)
            {
                await OnFiltersChanged.InvokeAsync(ActiveFilters);
            }

            StateHasChanged();
        }
    }

    private async Task RemoveFilter(string filterId)
    {
        var filter = ActiveFilters.FirstOrDefault(f => f.Id == filterId);
        if (filter != null)
        {
            ActiveFilters.Remove(filter);

            if (FilterProvider != null)
            {
                FilterProvider.RemoveFilter(filterId);
            }

            if (OnFiltersChanged.HasDelegate)
            {
                await OnFiltersChanged.InvokeAsync(ActiveFilters);
            }
        }
    }

    private async Task ClearAllFilters()
    {
        ActiveFilters.Clear();
        tempRules.Clear();
        multiSelectValues.Clear();

        if (FilterProvider != null)
        {
            FilterProvider.ClearFilters();
        }

        if (OnFiltersChanged.HasDelegate)
        {
            await OnFiltersChanged.InvokeAsync(ActiveFilters);
        }
    }

    private void CancelFilter()
    {
        tempRules.Clear();
        IsOpen = false;
    }

    private async Task ApplyFilters()
    {
        IsOpen = false;

        if (OnFiltersChanged.HasDelegate)
        {
            await OnFiltersChanged.InvokeAsync(ActiveFilters);
        }
    }

    private string GetOperatorDisplayName(FilterOperator op)
    {
        return op switch
        {
            FilterOperator.Equals => "equals",
            FilterOperator.NotEquals => "not equals",
            FilterOperator.Contains => "contains",
            FilterOperator.NotContains => "doesn't contain",
            FilterOperator.StartsWith => "starts with",
            FilterOperator.EndsWith => "ends with",
            FilterOperator.GreaterThan => "greater than",
            FilterOperator.LessThan => "less than",
            FilterOperator.GreaterThanOrEqual => "greater than or equal",
            FilterOperator.LessThanOrEqual => "less than or equal",
            FilterOperator.Between => "between",
            FilterOperator.In => "is one of",
            FilterOperator.NotIn => "is not one of",
            FilterOperator.IsNull => "is empty",
            FilterOperator.IsNotNull => "is not empty",
            FilterOperator.IsTrue => "is yes",
            FilterOperator.IsFalse => "is no",
            _ => "equals"
        };
    }
}