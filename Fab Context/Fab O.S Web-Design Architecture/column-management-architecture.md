# Column Management Architecture - Fab.OS Implementation

## Executive Summary
The Column Management System provides comprehensive control over table column display, ordering, visibility, and freezing within Fab.OS. It integrates seamlessly with the unified view management system to deliver a powerful, user-customizable data viewing experience with robust frozen column support and state persistence.

## System Overview

### Architecture Diagram
```
┌────────────────────────────────────────────────────────────┐
│                    StandardToolbar                          │
│                                                             │
│  [Search] [Actions]      [ViewSwitcher][Columns][Filter][Views]
│                                           ↓                 │
└────────────────────────────┬───────────────────────────────┘
                             │
┌────────────────────────────▼───────────────────────────────┐
│               ColumnManagerDropdown                        │
│                                                             │
│  • Visibility toggle controls                              │
│  • Up/down reordering buttons                             │
│  • Freeze position management (Left/Right/None)           │
│  • Bulk operations (reset to defaults)                    │
│  • Real-time column state sync                            │
└────────────────────────────┬───────────────────────────────┘
                             │
         ┌───────────────────┼───────────────────┐
         │                   │                   │
┌────────▼────────┐ ┌────────▼────────┐ ┌───────▼──────┐
│  PackagesList   │ │  GenericTable   │ │ JavaScript   │
│      Page       │ │      View       │ │   Interop    │
│                 │ │                 │ │              │
│ • UpdateTable   │ │ • Renders with  │ │ • Calculates │
│   Columns()     │ │   frozen styles │ │   positions  │
│ • OnColumns     │ │ • Data attribs  │ │ • Applies    │
│   Changed()     │ │ • CSS classes   │ │   sticky CSS │
│ • ApplyFrozen   │ │ • Error safe    │ │ • Error      │
│   Columns()     │ │   rendering     │ │   handling   │
└─────────────────┘ └─────────────────┘ └──────────────┘
```

## Core Components

### ColumnDefinition Model
Complete model definition with all properties used in Fab.OS:

```csharp
namespace FabOS.WebServer.Models.Columns
{
    public class ColumnDefinition : ICloneable
    {
        // Core Identity
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string PropertyName { get; set; } = string.Empty;
        public string DisplayName { get; set; } = string.Empty;

        // Visibility & Display
        public bool IsVisible { get; set; } = true;
        public bool IsRequired { get; set; } = false;
        public int Order { get; set; } = 0;
        public int? Width { get; set; } = 150;
        public int MinWidth { get; set; } = 50;
        public int MaxWidth { get; set; } = 500;

        // Freezing Support
        public bool IsFrozen { get; set; } = false;
        public FreezePosition FreezePosition { get; set; } = FreezePosition.None;

        // Functionality
        public bool IsSortable { get; set; } = true;
        public bool IsResizable { get; set; } = true;
        public ColumnType Type { get; set; } = ColumnType.Text;

        // Styling
        public string? CssClass { get; set; }
        public string? HeaderCssClass { get; set; }
        public string? Format { get; set; }
        public string? Tooltip { get; set; }

        // Deep Copy Implementation
        public object Clone()
        {
            return new ColumnDefinition
            {
                Id = this.Id,
                PropertyName = this.PropertyName,
                DisplayName = this.DisplayName,
                // ... all properties copied
            };
        }
    }

    public enum FreezePosition
    {
        None,   // Not frozen (default)
        Left,   // Frozen to left side
        Right   // Frozen to right side
    }
}
```

### ColumnManagerDropdown Component

#### Component Structure
```razor
@namespace FabOS.WebServer.Components.Shared

<div class="column-manager-dropdown @(isOpen ? "open" : "")">
    <button class="dropdown-toggle" @onclick="ToggleDropdown">
        <i class="fas fa-columns"></i>
        <span>Columns</span>
        <i class="fas fa-chevron-down"></i>
    </button>

    @if (isOpen)
    {
        <div class="dropdown-panel">
            <div class="dropdown-header">
                <span>Manage Columns</span>
                <button class="close-btn" @onclick="CloseDropdown">
                    <i class="fas fa-times"></i>
                </button>
            </div>

            <div class="dropdown-body">
                @foreach (var column in workingColumns.OrderBy(c => c.Order))
                {
                    <div class="column-item @(column.IsFrozen ? "frozen" : "")">
                        <div class="column-controls">
                            <input type="checkbox" @bind="column.IsVisible" />
                            <label>
                                @column.DisplayName
                                @if (column.IsFrozen)
                                {
                                    <span class="freeze-indicator">
                                        <i class="fas fa-thumbtack"></i>
                                        @column.FreezePosition.ToString()
                                    </span>
                                }
                            </label>
                        </div>

                        <div class="column-actions">
                            <!-- Up/Down reorder buttons -->
                            <!-- Freeze position dropdown -->
                        </div>
                    </div>
                }
            </div>
        </div>
    }
</div>
```

#### Key Features

1. **Up/Down Reordering**
   - Simple button-based reordering instead of drag-and-drop
   - Maintains order consistency
   - Updates Order property automatically

2. **Visibility Management**
   - Individual column checkboxes
   - Required columns protected from hiding
   - Real-time visual feedback

3. **Freeze Management**
   - Dropdown menu for freeze positions (None/Left/Right)
   - Visual indicators for frozen state
   - Integration with JavaScript positioning

4. **Working Copy Pattern**
   - Changes made to workingColumns copy
   - Applied only when user clicks "Apply"
   - Cancel functionality restores original state

## Page Integration Pattern

### PackagesList Implementation
```csharp
public partial class PackagesList : ComponentBase, IToolbarActionProvider, IDisposable
{
    [Inject] private IJSRuntime JSRuntime { get; set; } = default!;

    private List<ColumnDefinition> columnDefinitions = new();
    private List<GenericTableView<Package>.TableColumn<Package>> tableColumns = new();

    private async Task OnColumnsChanged(List<ColumnDefinition> columns)
    {
        columnDefinitions = columns;
        await UpdateTableColumns();
        hasUnsavedChanges = true;
        StateHasChanged();
    }

    private async Task UpdateTableColumns()
    {
        // Build table columns based on visible column definitions
        tableColumns = columnDefinitions
            .Where(c => c.IsVisible)
            .OrderBy(c => c.Order)
            .Select(c => CreateTableColumn(c))
            .Where(col => col != null)
            .ToList()!;

        // Apply frozen column states after table update
        await ApplyFrozenColumns();
    }

    private GenericTableView<Package>.TableColumn<Package>? CreateTableColumn(ColumnDefinition columnDef)
    {
        var baseColumn = columnDef.PropertyName switch
        {
            "PackageNumber" => new GenericTableView<Package>.TableColumn<Package>
            {
                Header = columnDef.DisplayName,
                ValueSelector = item => item.PackageNumber,
                CssClass = "text-start"
            },
            // ... other column mappings
            _ => null
        };

        if (baseColumn != null)
        {
            // Add frozen column CSS classes
            if (columnDef.IsFrozen)
            {
                var frozenClass = columnDef.FreezePosition switch
                {
                    FreezePosition.Left => "frozen-column frozen-left",
                    FreezePosition.Right => "frozen-column frozen-right",
                    _ => "frozen-column"
                };
                baseColumn.CssClass += $" {frozenClass}";
            }

            // Add data attribute for JavaScript targeting
            baseColumn.PropertyName = columnDef.PropertyName;
        }

        return baseColumn;
    }
}
```

## Frozen Column Implementation

### JavaScript Integration
```csharp
private async Task ApplyFrozenColumns()
{
    try
    {
        if (JSRuntime == null)
        {
            return;
        }

        var frozenColumns = columnDefinitions
            .Where(c => c.IsVisible && c.IsFrozen && !string.IsNullOrEmpty(c.PropertyName))
            .OrderBy(c => c.Order)
            .Select(c => new
            {
                PropertyName = c.PropertyName,
                FreezePosition = c.FreezePosition.ToString(),
                Order = c.Order
            })
            .ToArray();

        if (frozenColumns.Any())
        {
            await JSRuntime.InvokeVoidAsync("applyFrozenColumns", frozenColumns);
        }
        else
        {
            await JSRuntime.InvokeVoidAsync("clearFrozenColumns");
        }
    }
    catch (InvalidOperationException)
    {
        // JSRuntime not available during prerendering - ignore
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Error applying frozen columns: {ex.Message}");
    }
}
```

### Enhanced JavaScript Implementation
```javascript
// Table Column Freeze Management - Enhanced Version
window.applyFrozenColumns = function(frozenColumns) {
    const table = document.querySelector('.fabos-table');
    if (!table) return;

    // Clear any existing frozen states
    clearFrozenColumns();

    if (!frozenColumns || frozenColumns.length === 0) {
        return;
    }

    // Group frozen columns by position
    const leftFrozen = frozenColumns.filter(col => col.FreezePosition === 'Left')
        .sort((a, b) => a.Order - b.Order);
    const rightFrozen = frozenColumns.filter(col => col.FreezePosition === 'Right')
        .sort((a, b) => a.Order - b.Order);

    // Apply left frozen columns
    applyLeftFrozenColumns(table, leftFrozen);

    // Apply right frozen columns
    applyRightFrozenColumns(table, rightFrozen);
};

function applyLeftFrozenColumns(table, leftFrozen) {
    let cumulativeWidth = 0;

    leftFrozen.forEach((column, index) => {
        const columnElements = getColumnElementsByProperty(table, column.PropertyName);
        if (columnElements.length === 0) return;

        const headerCell = columnElements.header;
        const bodyCells = columnElements.bodyCells;
        const width = headerCell ? headerCell.offsetWidth : 0;

        // Apply frozen styles with calculated positions
        if (headerCell) {
            headerCell.classList.add('frozen-column', 'frozen-left');
            headerCell.style.left = `${cumulativeWidth}px`;
            headerCell.style.position = 'sticky';
            headerCell.style.zIndex = `${10 + index}`;
        }

        bodyCells.forEach(cell => {
            cell.classList.add('frozen-column', 'frozen-left');
            cell.style.left = `${cumulativeWidth}px`;
            cell.style.position = 'sticky';
            cell.style.zIndex = `${10 + index}`;
        });

        cumulativeWidth += width;
    });
}

function getColumnElementsByProperty(table, propertyName) {
    const headerCell = table.querySelector(`thead th[data-column="${propertyName}"]`);
    if (!headerCell) {
        return { header: null, bodyCells: [] };
    }

    const headerCells = Array.from(table.querySelectorAll('thead th'));
    const columnIndex = headerCells.indexOf(headerCell);
    const bodyCells = Array.from(table.querySelectorAll(`tbody td:nth-child(${columnIndex + 1})`));

    return {
        header: headerCell,
        bodyCells: bodyCells
    };
}
```

### CSS Styling Architecture
```css
/* Fab.OS Blue Theme for Frozen Columns */
.frozen-column {
    position: sticky !important;
    background: var(--bs-body-bg, white);
    z-index: 10;
    transition: box-shadow 0.2s ease;
}

.frozen-column.frozen-left {
    left: 0;
    border-right: 1px solid var(--fabos-border, #E5E7EB);
    box-shadow: 2px 0 8px rgba(13, 26, 128, 0.1);
}

.frozen-column.frozen-right {
    right: 0;
    border-left: 1px solid var(--fabos-border, #E5E7EB);
    box-shadow: -2px 0 8px rgba(13, 26, 128, 0.1);
}

/* Header cells for frozen columns */
.fabos-table-header-cell.frozen-column {
    background: linear-gradient(135deg, #0066cc 0%, #004499 100%);
    color: white;
    z-index: 11;
}

/* Visual indicator for frozen state */
.frozen-column::after {
    content: '';
    position: absolute;
    top: 0;
    bottom: 0;
    width: 2px;
    background: linear-gradient(135deg, var(--fabos-secondary, #3144CD), var(--fabos-primary, #0D1A80));
    opacity: 0.6;
    z-index: 1;
}

.frozen-column.frozen-left::after {
    right: 0;
}

.frozen-column.frozen-right::after {
    left: 0;
}
```

## State Management Patterns

### Deep Copy Pattern
Critical for preventing reference pollution:

```csharp
private async Task OnColumnsChanged(List<ColumnDefinition> columns)
{
    // NEVER assign by reference - always create new instances
    columnDefinitions = columns.Select(c => (ColumnDefinition)c.Clone()).ToList();

    await UpdateTableColumns();
    hasUnsavedChanges = true;
    StateHasChanged();
}
```

### Error Handling Pattern
```csharp
private async Task ApplyFrozenColumns()
{
    try
    {
        if (JSRuntime == null) return;

        // Safe JavaScript interop with validation
        var frozenColumns = columnDefinitions
            .Where(c => c.IsVisible && c.IsFrozen && !string.IsNullOrEmpty(c.PropertyName))
            .Select(/* mapping */)
            .ToArray();

        await JSRuntime.InvokeVoidAsync("applyFrozenColumns", frozenColumns);
    }
    catch (InvalidOperationException)
    {
        // JSRuntime not available during prerendering - ignore
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Error applying frozen columns: {ex.Message}");
    }
}
```

## Integration with StandardToolbar

### Toolbar Section Usage
```razor
<StandardToolbar ActionProvider="@this" OnSearch="@OnSearchChanged">
    <ColumnManager>
        <ColumnManagerDropdown Columns="@columnDefinitions"
                             OnColumnsChanged="@(async (columns) => await OnColumnsChanged(columns))" />
    </ColumnManager>
</StandardToolbar>
```

## Column Definition Initialization

### Best Practice Pattern
```csharp
private void InitializeColumnDefinitions()
{
    columnDefinitions = new List<ColumnDefinition>
    {
        new ColumnDefinition
        {
            Id = "pkg-number",
            PropertyName = "PackageNumber",
            DisplayName = "Package Number",
            Order = 0,
            IsVisible = true,
            IsRequired = true  // Cannot be hidden
        },
        new ColumnDefinition
        {
            Id = "pkg-name",
            PropertyName = "PackageName",
            DisplayName = "Package Name",
            Order = 1,
            IsVisible = true,
            IsRequired = true
        }
        // ... additional columns
    };
}
```

## ViewState Integration

### Column Configuration Persistence
```json
{
    "ViewMode": "table",
    "Columns": [
        {
            "Id": "pkg-number",
            "PropertyName": "PackageNumber",
            "DisplayName": "Package Number",
            "IsVisible": true,
            "IsFrozen": true,
            "FreezePosition": "Left",
            "Order": 0,
            "Width": 120
        }
    ],
    "Filters": [...],
    "ActiveFilters": [...]
}
```

## Performance Optimizations

### JavaScript Optimization
- Cache DOM element references
- Use `data-column` attributes for reliable targeting
- Debounce position recalculations
- Efficient z-index management

### Blazor Optimization
- Minimize StateHasChanged() calls
- Use deep copy pattern correctly
- Cache JavaScript module references
- Proper disposal of event handlers

## Error Handling and Resilience

### Common Error Scenarios
1. **JSRuntime unavailable during prerendering**
   - Solution: Catch `InvalidOperationException` and ignore

2. **Column property not found during reflection**
   - Solution: Validate property names before reflection

3. **Frozen column calculation failures**
   - Solution: Graceful degradation, log errors

4. **State synchronization issues**
   - Solution: Always use deep copy, explicit state resets

## Troubleshooting Guide

### Frozen Columns Not Positioning Correctly
- **Check**: Data attributes are present on table cells
- **Verify**: JavaScript console for errors
- **Ensure**: Column visibility changes trigger recalculation

### Column Changes Not Persisting
- **Check**: Deep copy pattern implementation
- **Verify**: ViewState serialization includes column data
- **Ensure**: OnColumnsChanged triggers view state updates

### Performance Issues with Large Tables
- **Solution**: Implement virtual scrolling
- **Optimize**: Debounce JavaScript positioning calls
- **Cache**: Column element references

## Migration Guide

### From Legacy Column Management
1. Replace old ColumnReorderManager with ColumnManagerDropdown
2. Update ColumnDefinition model to include new properties
3. Implement deep copy pattern in event handlers
4. Add JavaScript integration for frozen columns
5. Update CSS to use Fab.OS blue theme

### Integration Checklist
- [ ] ColumnDefinition model matches implementation
- [ ] Deep copy pattern used for state changes
- [ ] JavaScript integration with error handling
- [ ] CSS styling uses blue theme
- [ ] ViewState persistence includes column data
- [ ] Toolbar integration follows pattern

## Future Enhancements

### Planned Features
- Column width persistence and resizing
- Advanced freeze constraints (grouping)
- Column templates for custom rendering
- Export/import column configurations
- Performance optimizations for large datasets

## Conclusion

The enhanced Column Management System provides a robust, performant, and user-friendly solution for table customization in Fab.OS. By following the established patterns for state management, error handling, and component integration, developers can implement powerful data viewing experiences that adapt to user preferences while maintaining application stability and performance.