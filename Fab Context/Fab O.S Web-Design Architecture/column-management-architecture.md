# Column Management Architecture

## Executive Summary
The Column Management System provides comprehensive control over table column display, ordering, visibility, and freezing. It integrates seamlessly with the Generic View System and Save View Preferences to deliver a powerful, user-customizable data viewing experience.

## System Overview

### Architecture Diagram
```
┌────────────────────────────────────────────────────────────┐
│                        Page Level                           │
│            (List/Worksheet Pages with Tables)               │
│                                                             │
│  • Defines List<ColumnDefinition<T>>                       │
│  • Implements OnColumnsChanged handler                     │
│  • Manages deep copy for state isolation                   │
└──────────────────────┬─────────────────────────────────────┘
                       │
┌──────────────────────▼─────────────────────────────────────┐
│               ColumnReorderManager                          │
│                                                             │
│  • Drag-and-drop reordering UI                            │
│  • Visibility toggle controls                              │
│  • Freeze/unfreeze management                             │
│  • Bulk operations (show all, hide all, unfreeze all)     │
│  • Reset to default functionality                          │
└──────────────────────┬─────────────────────────────────────┘
                       │
         ┌─────────────┼─────────────┐
         │             │             │
┌────────▼──────┐ ┌───▼────┐ ┌─────▼──────┐
│   DataTable   │ │ViewState│ │JavaScript │
│               │ │         │ │  Interop   │
│ • Renders     │ │ • Saves │ │            │
│   columns    │ │   config│ │ • Measures │
│ • Applies    │ │ • Loads │ │   widths   │
│   frozen     │ │   state │ │ • Sets     │
│   styles     │ │         │ │   positions│
└───────────────┘ └─────────┘ └────────────┘
```

## Core Components

### ColumnDefinition<T> Model

Complete model definition with all properties:

```csharp
namespace SteelEstimation.Core.Models
{
    public class ColumnDefinition<T>
    {
        // Core Identity
        public string Key { get; set; } = "";              // Unique identifier
        public string Header { get; set; } = "";           // Display name
        public string? Description { get; set; }           // Tooltip/accessibility
        
        // Visibility & Display
        public bool IsVisible { get; set; } = true;        // Show/hide state
        public bool IsRequired { get; set; } = false;      // Cannot be hidden
        public int Order { get; set; } = 0;                // Display sequence
        public int Width { get; set; } = 150;              // Column width (px)
        
        // Freezing
        public bool IsFrozen { get; set; } = false;        // Frozen state
        public FreezePosition FreezePosition { get; set; } // Freeze position
        
        // Functionality
        public bool IsSortable { get; set; } = true;       // Can be sorted
        public bool IsResizable { get; set; } = true;      // Can be resized
        public ColumnType Type { get; set; } = ColumnType.Text; // Data type
        
        // Rendering
        public string? CssClass { get; set; }              // Custom styling
        public string? Format { get; set; }                // Value format
        public Func<T, object?>? ValueSelector { get; set; } // Value extraction
        public RenderFragment<T>? Template { get; set; }   // Custom template
    }
    
    public enum FreezePosition
    {
        None,   // Not frozen (default)
        Left,   // Frozen to left side
        Right   // Frozen to right side (future)
    }
    
    public enum ColumnType
    {
        Text, Number, Currency, Date, DateTime,
        Boolean, Status, Actions, Custom,
        Tag, Badge, Percentage
    }
}
```

### ColumnReorderManager Component

#### Component Structure
```razor
@namespace SteelEstimation.Web.Shared.Components
@typeparam TItem
@implements IAsyncDisposable

<div class="column-reorder-manager">
    <!-- Trigger Button -->
    <button class="btn-column-reorder" @onclick="TogglePanel">
        <i class="fas fa-columns"></i> Columns
        @if (HasCustomizations)
        {
            <span class="custom-indicator"></span>
        }
    </button>
    
    <!-- Management Panel -->
    @if (ShowingPanel)
    {
        <div class="column-reorder-panel">
            <!-- Drag-and-drop column list -->
            <!-- Visibility toggles -->
            <!-- Freeze controls -->
            <!-- Bulk operations -->
        </div>
    }
</div>
```

#### Key Features

1. **Drag-and-Drop Reordering**
   - HTML5 drag-and-drop API
   - Visual feedback during drag
   - Maintains frozen column constraints

2. **Visibility Management**
   - Individual column toggles
   - Bulk show/hide operations
   - Required columns protection

3. **Freeze Management**
   - Single-click freeze toggle
   - Visual freeze indicators
   - Consecutive column enforcement

4. **State Persistence**
   - Integrates with ViewState system
   - Saves user preferences
   - Restores on page load

### DataTable Integration

The DataTable component reads ColumnDefinition properties to render appropriately:

```csharp
@foreach (var column in Columns)
{
    // Check frozen state from ColumnDefinitions
    var colDef = ColumnDefinitions?.FirstOrDefault(c => c.Key == column.Field);
    var isFrozen = colDef?.IsFrozen == true && 
                   colDef.FreezePosition == FreezePosition.Left;
    
    <th class="@(isFrozen ? "frozen-column" : "")"
        data-column-key="@column.Field"
        data-is-frozen="@(isFrozen ? "true" : "false")"
        style="@(isFrozen ? GetFrozenStyle() : "")">
        @column.Title
    </th>
}
```

## Frozen Column Implementation

### CSS Architecture

#### Sticky Positioning
```css
.frozen-column {
    position: -webkit-sticky !important;
    position: sticky !important;
    background: white !important;
    z-index: 10 !important;
    
    /* Visual indicators */
    border-right: 2px solid #10b981 !important;
    box-shadow: 2px 0 4px rgba(16, 185, 129, 0.1) !important;
}

/* Header cells have higher z-index */
thead .frozen-column {
    z-index: 200 !important;
    background: linear-gradient(135deg, #3144CD 0%, #0D1A80 100%) !important;
}

/* Body cells */
tbody .frozen-column {
    z-index: 190 !important;
}
```

### JavaScript Dynamic Positioning

The system uses JavaScript to measure actual column widths and calculate positions:

```javascript
function updateFrozenColumnPositions(tableElement) {
    if (!tableElement) return;
    
    const headers = tableElement.querySelectorAll('thead th[data-column-key]');
    const bodyRows = tableElement.querySelectorAll('tbody tr');
    
    let cumulativeLeft = 0;
    let frozenCount = 0;
    
    headers.forEach((th) => {
        const isFrozen = th.dataset.isFrozen === 'true';
        const columnKey = th.dataset.columnKey;
        
        if (isFrozen && columnKey) {
            // Get actual rendered width
            const rect = th.getBoundingClientRect();
            const width = rect.width;
            
            // Set left position for header
            th.style.setProperty('left', cumulativeLeft + 'px', 'important');
            
            // Update all body cells with same column key
            bodyRows.forEach(row => {
                const td = row.querySelector(`td[data-column-key="${columnKey}"]`);
                if (td && td.dataset.isFrozen === 'true') {
                    td.style.setProperty('left', cumulativeLeft + 'px', 'important');
                }
            });
            
            // Add width to cumulative offset
            cumulativeLeft += width;
            frozenCount++;
        }
    });
    
    console.log(`Updated ${frozenCount} frozen columns, total width: ${cumulativeLeft}px`);
}
```

### Blazor JavaScript Interop

```csharp
protected override async Task OnAfterRenderAsync(bool firstRender)
{
    if (firstRender)
    {
        // Initialize JavaScript module
        jsModule = await JS.InvokeAsync<IJSObjectReference>("eval", JS_CODE);
    }
    
    // Update positions after each render
    await UpdateFrozenColumnPositions();
}

private async Task UpdateFrozenColumnPositions()
{
    if (jsModule != null && tableElement.Id != null)
    {
        await jsModule.InvokeVoidAsync("updateFrozenColumnPositions", tableElement);
    }
}
```

## State Management

### Deep Copy Pattern

Prevents reference pollution when column state changes:

```csharp
private async Task OnColumnsChanged(List<ColumnDefinition<TItem>> columns)
{
    // Create deep copy - NEVER assign by reference
    columnDefinitions = columns.Select(c => new ColumnDefinition<TItem>
    {
        Key = c.Key,
        Header = c.Header,
        Description = c.Description,
        IsVisible = c.IsVisible,
        IsSortable = c.IsSortable,
        IsResizable = c.IsResizable,
        IsRequired = c.IsRequired,
        IsFrozen = c.IsFrozen,
        FreezePosition = c.FreezePosition,
        Type = c.Type,
        Width = c.Width,
        Order = c.Order,
        CssClass = c.CssClass,
        ValueSelector = c.ValueSelector,
        Template = c.Template,
        Format = c.Format
    }).ToList();
    
    // Notify view state system
    TriggerViewStateChanged();
    StateHasChanged();
}
```

### State Reset Pattern

Ensures clean transitions when loading saved views:

```csharp
public async Task ApplyViewState(string viewState)
{
    var state = ViewStateExtensions.DeserializeViewState(viewState);
    if (state != null)
    {
        // ALWAYS reset frozen states first
        foreach (var column in columnDefinitions)
        {
            column.IsFrozen = false;
            column.FreezePosition = FreezePosition.None;
        }
        
        // Then apply saved configuration
        state.ApplyColumnConfiguration(columnDefinitions);
        
        // Refresh visual state
        if (viewSwitcher != null)
        {
            await viewSwitcher.RefreshFrozenColumns();
        }
        
        StateHasChanged();
    }
}
```

## Event Flow

### User Interaction to State Persistence

```
1. User Action
   └─> Clicks freeze toggle in ColumnReorderManager
   
2. Component Update
   └─> ToggleColumnFreeze(column)
       ├─> column.IsFrozen = !column.IsFrozen
       └─> column.FreezePosition = IsFrozen ? Left : None
       
3. Event Propagation
   └─> ColumnsChanged.InvokeAsync(OrderedColumns)
   
4. Page Handler
   └─> OnColumnsChanged(columns)
       ├─> Deep copy columns to columnDefinitions
       └─> TriggerViewStateChanged()
       
5. View State System
   └─> SaveViewPreferences detects change
       └─> Shows unsaved indicator
       
6. Visual Update
   └─> DataTable re-renders with frozen classes
       └─> JavaScript measures and positions columns
```

## ViewState Integration

### Column Configuration Structure

```json
{
    "ViewMode": "table",
    "ActiveFilters": { ... },
    "ColumnConfig": {
        "ColumnOrder": ["id", "name", "status", "date"],
        "ColumnVisibility": {
            "id": true,
            "name": true,
            "status": true,
            "date": false
        },
        "ColumnFreeze": {
            "id": "Left",
            "name": "Left",
            "status": "None",
            "date": "None"
        },
        "ColumnWidths": {
            "id": 100,
            "name": 200,
            "status": 150,
            "date": 120
        }
    }
}
```

### Persistence Methods

```csharp
// Extract configuration from columns
public static ColumnConfiguration ExtractConfiguration<T>(
    this List<ColumnDefinition<T>> columns)
{
    return new ColumnConfiguration
    {
        ColumnOrder = columns.OrderBy(c => c.Order).Select(c => c.Key).ToList(),
        ColumnVisibility = columns.ToDictionary(c => c.Key, c => c.IsVisible),
        ColumnFreeze = columns
            .Where(c => c.FreezePosition != FreezePosition.None)
            .ToDictionary(c => c.Key, c => c.FreezePosition),
        ColumnWidths = columns.ToDictionary(c => c.Key, c => c.Width)
    };
}

// Apply configuration to columns
public static void ApplyConfiguration<T>(
    this List<ColumnDefinition<T>> columns,
    ColumnConfiguration config)
{
    // Apply order
    if (config.ColumnOrder?.Any() == true)
    {
        for (int i = 0; i < config.ColumnOrder.Count; i++)
        {
            var column = columns.FirstOrDefault(c => c.Key == config.ColumnOrder[i]);
            if (column != null) column.Order = i;
        }
    }
    
    // Apply visibility
    foreach (var vis in config.ColumnVisibility ?? new())
    {
        var column = columns.FirstOrDefault(c => c.Key == vis.Key);
        if (column != null && !column.IsRequired)
            column.IsVisible = vis.Value;
    }
    
    // Apply freeze positions
    foreach (var freeze in config.ColumnFreeze ?? new())
    {
        var column = columns.FirstOrDefault(c => c.Key == freeze.Key);
        if (column != null)
        {
            column.FreezePosition = freeze.Value;
            column.IsFrozen = freeze.Value != FreezePosition.None;
        }
    }
}
```

## Performance Optimizations

### JavaScript Optimization
- Cache DOM element references
- Batch position updates
- Debounce scroll events (10ms)
- Throttle resize events (100ms)

### Blazor Optimization
- Minimize StateHasChanged() calls
- Use @key for list items
- Implement IAsyncDisposable for cleanup
- Cache JavaScript module references

### CSS Optimization
- Use CSS containment for frozen columns
- Minimize repaints with transform instead of left
- Use will-change for animated properties

## Accessibility

### Keyboard Navigation
- Tab through column controls
- Space to toggle visibility/freeze
- Arrow keys for reordering
- Escape to close panel

### Screen Reader Support
- ARIA labels for all controls
- Live regions for state changes
- Descriptive button titles
- Role attributes for drag handles

### Focus Management
- Trap focus in panel when open
- Return focus on close
- Visual focus indicators
- Skip links for navigation

## Browser Compatibility

### Supported Browsers
- Chrome 90+ (Full support)
- Edge 90+ (Full support)
- Firefox 88+ (Full support)
- Safari 14+ (Requires -webkit-sticky prefix)

### Polyfills Required
- ResizeObserver for older browsers
- IntersectionObserver for performance

## Troubleshooting Guide

### Common Issues

#### Frozen Columns Not Sticking
**Cause**: Parent container has overflow hidden
**Solution**: Ensure table container allows overflow-x: auto

#### Gap Between Frozen Columns
**Cause**: Fixed width assumptions
**Solution**: Use dynamic width calculation

#### Frozen State Not Persisting
**Cause**: Reference pollution in state management
**Solution**: Implement deep copy pattern

#### Scrollbar Glitching
**Cause**: Shadow indicators overlapping
**Solution**: Adjust shadow positioning, debounce events

### Debugging Tips

1. **Check Console**: Look for JavaScript errors
2. **Inspect Elements**: Verify data-* attributes
3. **Check State**: Log columnDefinitions changes
4. **Verify Events**: Ensure handlers are called
5. **Test Isolation**: Try with minimal columns

## Migration Guide

### From Legacy Implementation

#### Step 1: Update Page Components
```csharp
// OLD: Direct column assignment
columnDefinitions = columns;

// NEW: Deep copy pattern
columnDefinitions = columns.Select(c => new ColumnDefinition<T> { ... }).ToList();
```

#### Step 2: Add State Reset
```csharp
// Add to ApplyViewState
foreach (var column in columnDefinitions)
{
    column.IsFrozen = false;
    column.FreezePosition = FreezePosition.None;
}
```

#### Step 3: Update DataTable Usage
```razor
<!-- OLD -->
<DataTable Columns="@tableColumns" />

<!-- NEW -->
<DataTable Columns="@tableColumns" 
           ColumnDefinitions="@columnDefinitions" />
```

#### Step 4: Test Thoroughly
- Create saved views with frozen columns
- Switch between views rapidly
- Test with varying content widths
- Verify horizontal scroll behavior

## API Reference

### ColumnReorderManager

| Parameter | Type | Required | Description |
|-----------|------|----------|-------------|
| Columns | List<ColumnDefinition<TItem>> | Yes | Column definitions |
| DefaultColumnOrder | List<string>? | No | Original order for reset |
| OnColumnsChanged | EventCallback<List<ColumnDefinition<TItem>>> | No | Change handler |
| OnColumnOrderChanged | EventCallback<ColumnOrderChangedArgs> | No | Order change event |
| OnColumnVisibilityChanged | EventCallback<ColumnVisibilityChangedArgs> | No | Visibility event |
| OnColumnFreezeChanged | EventCallback<ColumnFreezeChangedArgs> | No | Freeze event |

### Event Arguments

```csharp
public class ColumnOrderChangedArgs
{
    public List<string> NewOrder { get; set; }
    public int FromIndex { get; set; }
    public int ToIndex { get; set; }
}

public class ColumnVisibilityChangedArgs
{
    public string ColumnKey { get; set; }
    public bool IsVisible { get; set; }
    public List<string> VisibleColumns { get; set; }
}

public class ColumnFreezeChangedArgs
{
    public string ColumnKey { get; set; }
    public FreezePosition FreezePosition { get; set; }
    public List<string> LeftFrozenColumns { get; set; }
    public List<string> RightFrozenColumns { get; set; }
}
```

## Best Practices

### Do's
- ✅ Always use deep copy for column lists
- ✅ Reset state before applying saved configuration
- ✅ Use ColumnDefinitions as single source of truth
- ✅ Measure widths dynamically
- ✅ Debounce/throttle events
- ✅ Test with various content widths
- ✅ Provide visual feedback for frozen columns

### Don'ts
- ❌ Don't assign column lists by reference
- ❌ Don't hardcode column widths for frozen columns
- ❌ Don't skip state reset when loading views
- ❌ Don't ignore browser compatibility
- ❌ Don't forget accessibility features
- ❌ Don't mix CSS classes with ColumnDefinition state

## Future Enhancements

### Planned Features
- Right-side column freezing
- Column width persistence
- Resize handle for frozen columns
- Multi-column selection for bulk freeze
- Column grouping with freeze support
- Export view configurations
- Column templates library

### Performance Improvements
- Virtual scrolling for large tables
- Lazy loading for column content
- Web Worker for position calculations
- RequestAnimationFrame for smooth updates

## Conclusion

The Column Management System provides a robust, performant, and accessible solution for table column customization. By following the patterns and practices outlined in this document, developers can implement powerful data viewing experiences that adapt to user preferences while maintaining application performance and stability.