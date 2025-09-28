# FilterSystem Component Implementation

## Overview
The FilterSystem is a reusable component that provides intelligent, dynamic filtering capabilities across List and Worksheet page types. It features automatic value detection, a clean rule-builder interface, and follows Fab.OS visual identity guidelines.

## Key Features

### 1. Dynamic Value Detection
- Automatically extracts unique values from actual page data
- No need for hardcoded filter options
- Adapts to the current dataset in real-time
- Caches detected values for performance

### 2. Simplified Single Interface
- Clean rule builder without mode switching
- Intuitive field → operator → value workflow
- Support for multiple filter rules with AND/OR logic
- Active filter pills with easy removal

### 3. IFilterProvider Integration
Pages implement `IFilterProvider<T>` to define available filters and build predicates:

```csharp
@implements IFilterProvider<Product>

public List<FilterDefinition> GetAvailableFilters()
{
    return new List<FilterDefinition>
    {
        new() 
        { 
            PropertyName = "Category", 
            DisplayName = "Category", 
            Type = FilterType.Select
            // No Options - dynamic detection will extract from data
        },
        new() 
        { 
            PropertyName = "Name", 
            DisplayName = "Product Name", 
            Type = FilterType.Text
            // Will show dropdown with actual product names
        }
    };
}
```

## Component Structure

### FilterSystem.razor
```razor
@typeparam TItem

<div class="filter-system">
    <!-- Main Filter Bar -->
    <div class="filter-bar">
        <div class="filter-bar-left">
            <!-- Search Box -->
            @if (ShowSearch)
            {
                <div class="filter-search">
                    <!-- Search input with real-time filtering -->
                </div>
            }
        </div>
        
        <div class="filter-bar-right">
            <!-- Filter Button -->
            <button class="btn-filter @(ShowingFilters || ActiveFilters.Any() ? "active" : "")"
                    @onclick="ToggleFilters">
                <i class="fas fa-filter"></i>
                <span>Filters</span>
                @if (ActiveFilterCount > 0)
                {
                    <span class="filter-badge">@ActiveFilterCount</span>
                }
            </button>
            
            <!-- Results Count -->
            @if (ShowResultsCount)
            {
                <div class="results-count">
                    <span>@FilteredCount results</span>
                </div>
            }
        </div>
    </div>
    
    <!-- Active Filters Pills -->
    @if (ActiveFilters.Any() && !ShowingFilters)
    {
        <div class="active-filters-row">
            <!-- Filter pills with remove buttons -->
        </div>
    }
    
    <!-- Filter Panel (Collapsible) -->
    @if (ShowingFilters)
    {
        <div class="filter-panel">
            <!-- Rule builder interface -->
        </div>
    }
</div>
```

## Filter Rule Builder

The rule builder provides a clean interface for creating complex filter conditions:

```razor
<div class="filter-rule">
    <!-- Logical Operator (AND/OR) for multiple rules -->
    @if (index > 0)
    {
        <select @bind="rule.LogicalOperator">
            <option value="AND">AND</option>
            <option value="OR">OR</option>
        </select>
    }
    
    <!-- Field Selection -->
    <select @bind="rule.Field" @bind:after="() => OnFieldChanged(index)">
        <option value="">Select Field</option>
        @foreach (var filter in AvailableFilters)
        {
            <option value="@filter.PropertyName">@filter.DisplayName</option>
        }
    </select>
    
    <!-- Operator Selection -->
    <select @bind="rule.Operator">
        <!-- Dynamic operators based on field type -->
    </select>
    
    <!-- Value Input -->
    @RenderValueInput(rule, index)
    
    <!-- Remove Rule Button -->
    <button @onclick="() => RemoveFilterRule(index)">
        <i class="fas fa-trash"></i>
    </button>
</div>
```

## Dynamic Value Detection Implementation

```csharp
private Dictionary<string, string> GetUniqueValuesForField(string fieldName)
{
    if (string.IsNullOrEmpty(fieldName) || Items == null || !Items.Any())
        return new Dictionary<string, string>();

    // Check cache first
    if (_fieldValueCache.ContainsKey(fieldName))
        return _fieldValueCache[fieldName];

    var uniqueValues = new Dictionary<string, string>();
    var propertyInfo = typeof(TItem).GetProperty(fieldName);
    
    if (propertyInfo != null)
    {
        var values = Items
            .Select(item => propertyInfo.GetValue(item))
            .Where(value => value != null)
            .Distinct()
            .OrderBy(value => value?.ToString());

        foreach (var value in values)
        {
            var stringValue = value?.ToString() ?? "";
            if (!string.IsNullOrEmpty(stringValue))
            {
                uniqueValues[stringValue] = stringValue;
            }
        }
    }

    // Cache the results
    _fieldValueCache[fieldName] = uniqueValues;
    return uniqueValues;
}
```

## Filter Predicate Building

Filters use a `field_operator` format for precise filtering:

```csharp
public Func<T, bool> BuildFilterPredicate(Dictionary<string, object> filters)
{
    return item =>
    {
        foreach (var filter in filters)
        {
            // Parse field_operator format
            var parts = filter.Key.Split('_');
            if (parts.Length < 2) continue;
            
            var field = parts[0];
            var op = parts[1];
            var value = filter.Value?.ToString() ?? "";
            
            var result = field switch
            {
                "Category" => op switch
                {
                    "equals" => item.Category == value,
                    "contains" => item.Category?.Contains(value, StringComparison.OrdinalIgnoreCase) ?? false,
                    "startsWith" => item.Category?.StartsWith(value, StringComparison.OrdinalIgnoreCase) ?? false,
                    "endsWith" => item.Category?.EndsWith(value, StringComparison.OrdinalIgnoreCase) ?? false,
                    _ => true
                },
                "Price" => op switch
                {
                    "equals" => item.Price == decimal.Parse(value),
                    "greaterThan" => item.Price > decimal.Parse(value),
                    "lessThan" => item.Price < decimal.Parse(value),
                    _ => true
                },
                _ => true
            };
            
            if (!result) return false;
        }
        return true;
    };
}
```

## Filter Types and Operators

### Text Fields
- **Operators**: Contains, Equals, Starts With, Ends With
- **Input**: Dropdown with detected values or text input

### Number Fields
- **Operators**: Equals, Greater Than, Less Than, Between
- **Input**: Numeric input field

### Date Fields
- **Operators**: On Date, After, Before, Between
- **Input**: Date picker

### Boolean Fields
- **Operators**: Is (Yes/No)
- **Input**: Dropdown (Yes/No)

### Select Fields
- **Operators**: Is, Is One Of
- **Input**: Dropdown with detected or provided values

## Usage Example

```razor
@page "/products"
@implements IFilterProvider<Product>

<!-- StandardToolbar -->
<StandardToolbar ActionProvider="@this" />

<!-- FilterSystem -->
<FilterSystem TItem="Product"
             FilterProvider="@this"
             Items="@products"
             OnFilteredItemsChanged="@HandleFilteredItemsChanged"
             ShowSearch="true"
             SearchPlaceholder="Search products..."
             @bind-ActiveFilters="activeFilters" />

<!-- GenericViewSwitcher -->
<GenericViewSwitcher 
    @bind-CurrentView="viewMode"
    ItemCount="@filteredProducts.Count()"
    TableTemplate="@tableTemplate"
    CardTemplate="@cardTemplate"
    ListTemplate="@listTemplate" />

@code {
    private List<Product> products = new();
    private List<Product> filteredProducts = new();
    private Dictionary<string, object> activeFilters = new();
    
    public List<FilterDefinition> GetAvailableFilters()
    {
        return new List<FilterDefinition>
        {
            new() { PropertyName = "Category", DisplayName = "Category", Type = FilterType.Select },
            new() { PropertyName = "Status", DisplayName = "Status", Type = FilterType.Select },
            new() { PropertyName = "Name", DisplayName = "Name", Type = FilterType.Text },
            new() { PropertyName = "Price", DisplayName = "Price", Type = FilterType.Number }
        };
    }
    
    public Func<Product, bool> BuildFilterPredicate(Dictionary<string, object> filters)
    {
        // Implementation as shown above
    }
    
    private async Task HandleFilteredItemsChanged(List<Product> filtered)
    {
        filteredProducts = filtered;
        StateHasChanged();
    }
}
```

## Fab.OS Visual Identity

The FilterSystem follows Fab.OS design guidelines:

### Colors
- **Primary Blue**: #3144CD (filter button active state)
- **Deep Blue**: #0D1A80 (gradient accents)
- **Neutral Gray**: #777777 (text and borders)
- **Light Gray**: #B1B1B1 (inactive elements)

### Styling
```css
.filter-system {
    display: flex;
    flex-direction: column;
    gap: 0.75rem;
    margin-bottom: 1rem;
}

.btn-filter.active {
    background: linear-gradient(135deg, #3144CD 0%, #0D1A80 100%);
    border-color: #3144CD;
    color: white;
    box-shadow: 0 2px 4px rgba(49, 68, 205, 0.2);
}

.filter-pill {
    background: linear-gradient(135deg, rgba(49, 68, 205, 0.1) 0%, rgba(13, 26, 128, 0.15) 100%);
    border: 1px solid #3144CD;
    border-radius: 16px;
    color: #0D1A80;
}
```

## Key Benefits

1. **Automatic Adaptation**: Filters automatically show relevant values from current data
2. **No Maintenance**: No need to update hardcoded filter options as data changes
3. **User-Friendly**: Users only see filter values that actually exist
4. **Performance**: Intelligent caching prevents redundant calculations
5. **Type Safety**: Generic implementation maintains compile-time checking
6. **Consistent UX**: Same filtering experience across all applicable pages

## Migration from Previous Versions

If migrating from an older FilterSystem implementation:

1. Remove any quick filter configurations
2. Remove simple/advanced mode logic
3. Update IFilterProvider implementations to remove hardcoded Options (unless specific values are required)
4. Update BuildFilterPredicate to handle `field_operator` format
5. Test dynamic value detection with actual data

The new FilterSystem provides a cleaner, more maintainable solution that adapts to your data automatically.