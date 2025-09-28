# Generic View System Architecture

## Executive Summary
The Generic View System provides a consistent, reusable framework for displaying collections of data across List and Worksheet pages in the SteelEstimation application. It allows pages to switch between table, card, and list views while maintaining consistent visual design and behavior.

## Architecture Overview

### System Components
```
┌─────────────────────────────────────────────────────┐
│                    Page Level                        │
│  (ListPageSample.razor, WorksheetPageSample.razor)   │
│  (StandardListPage.razor - Template Component)       │
│                                                       │
│  • Implements IFilterProvider<T>                     │
│  • Defines GenericDisplayConfig                      │
│  • Defines ColumnDefinition<T> instances             │
│  • Provides RenderFragment templates                 │
└───────────────────────┬─────────────────────────────┘
                        │
        ┌───────────────┼───────────────┐
        │               │               │
┌───────▼──────┐ ┌─────▼──────┐ ┌─────▼──────┐
│Column Reorder│ │GenericView │ │FilterSystem│
│   Manager    │ │  Switcher  │ │            │
│              │ │            │ │            │
│ • Reorder    │ │ • View     │ │ • Search   │
│ • Freeze     │ │   modes    │ │ • Filters  │
│ • Visibility │ │ • Templates│ │ • Dynamic  │
└──────────────┘ └──────┬──────┘ └────────────┘
                        │
        ┌───────────────┼───────────────┐
        │               │               │
┌───────▼──────┐ ┌─────▼──────┐ ┌─────▼──────┐
│GenericTable  │ │GenericCard │ │GenericList │
│    View      │ │    View    │ │    View    │
│              │ │            │ │            │
│ • DataTable  │ │ • Grid     │ │ • Compact  │
│   wrapper    │ │   layout   │ │   items    │
│ • Columns    │ │ • Cards    │ │ • Horizontal│
│ • Freeze     │ │ • Stats    │ │   layout   │
└──────────────┘ └─────┬──────┘ └────────────┘
                       │
                ┌──────▼──────┐
                │ GenericCard │
                │             │
                │ • Reflection│
                │ • Rendering │
                └─────────────┘
```

## Core Concepts

### 1. GenericDisplayConfig
Defines how data properties map to visual elements:
```csharp
public class GenericDisplayConfig
{
    public string? PrimaryProperty { get; set; }      // Main title
    public string? SecondaryProperty { get; set; }    // Subtitle
    public string? TertiaryProperty { get; set; }     // Additional info
    public string? StatusProperty { get; set; }       // Status badges
    public string DefaultIcon { get; set; } = "";     // Default icon
    public List<StatMapping> Stats { get; set; }      // Statistical displays
}
```

### 2. Enhanced ColumnDefinition<T>
Comprehensive column configuration with freeze support:
```csharp
public class ColumnDefinition<T>
{
    public string Key { get; set; }                   // Unique identifier
    public string Header { get; set; }                // Display name
    public bool IsVisible { get; set; } = true;       // Visibility state
    public bool IsFrozen { get; set; } = false;       // Frozen state
    public FreezePosition FreezePosition { get; set; } // None, Left, Right
    public int Order { get; set; }                    // Display order
    public int Width { get; set; } = 150;             // Column width
    public bool IsSortable { get; set; } = true;      // Sort capability
    public bool IsRequired { get; set; } = false;     // Cannot be hidden
    public RenderFragment<T>? Template { get; set; }  // Custom template
}

public enum FreezePosition
{
    None,   // Not frozen
    Left,   // Freeze to left side
    Right   // Freeze to right side (future)
}
```

### 2. View Modes
Three consistent view modes across all applicable pages:
- **Table View**: Traditional grid with sortable columns
- **Card View**: Visual cards in responsive grid layout
- **List View**: Compact horizontal items with key information

### 3. Reflection-Based Rendering
Components use reflection to extract data from any entity type:
```csharp
private object? GetPropertyValue(string propertyName)
{
    var property = typeof(TItem).GetProperty(propertyName);
    return property?.GetValue(Item);
}
```

## Component Details

### DataTable Component
**Purpose**: Core table rendering component with advanced features
**Location**: `/SteelEstimation.Web/Shared/Components/DataTable.razor`

**Key Features**:
- Generic type support for any entity
- Column-based rendering with ColumnDefinition<T>
- Built-in sorting, filtering, and pagination
- Row selection with checkbox support
- Frozen column support for horizontal scrolling
- Responsive design with mobile optimization
- Custom cell templates via RenderFragment

**Integration with Column Management**:
DataTable works seamlessly with ColumnReorderManager to provide:
- Dynamic column visibility
- Drag-and-drop column reordering
- Column freeze positioning
- Persistent column preferences

### ColumnReorderManager
**Purpose**: Provides comprehensive column management UI
**Location**: `/SteelEstimation.Web/Shared/Components/ColumnReorderManager.razor`

**Features**:
- **Visual Column List**: Drag-and-drop interface for reordering
- **Visibility Controls**: Toggle columns on/off with checkboxes
- **Freeze Management**: Set columns to freeze left or right
- **Bulk Operations**: Show all, hide all, unfreeze all
- **Reset to Default**: Restore original column configuration
- **JavaScript Integration**: Dynamic positioning calculations for frozen columns

**Z-Index Management**:
Proper layering for dropdowns in modals:
```css
--z-frozen-columns: 10-15;
--z-column-panel: 100;
--z-dropdown-menu: 1060;
--z-modal: 1050;
--z-tooltip: 1070;
```

For complete details, see [Column Management Architecture](../documentation/column-management-architecture.md).

### GenericViewSwitcher
**Purpose**: Orchestrates view switching and template rendering
**Location**: `/SteelEstimation.Web/Shared/Components/GenericViewSwitcher.razor`

**Key Features**:
- Manages current view state
- Renders appropriate template based on selection
- Provides visual controls following Fab.OS design
- Maintains view preference (can be extended for persistence)

**Parameters**:
```csharp
[Parameter] public string CurrentView { get; set; } = "table";
[Parameter] public EventCallback<string> CurrentViewChanged { get; set; }
[Parameter] public RenderFragment? TableTemplate { get; set; }
[Parameter] public RenderFragment? CardTemplate { get; set; }
[Parameter] public RenderFragment? ListTemplate { get; set; }
[Parameter] public int ItemCount { get; set; }
```

### GenericTableView
**Purpose**: Wrapper for DataTable with generic type support and column management
**Location**: `/SteelEstimation.Web/Shared/Components/GenericTableView.razor`

**Features**:
- Configurable columns via `ColumnDefinition<T>` (enhanced from `TableColumn<T>`)
- Column freezing support (left/right sticky positioning)
- Selection support with checkboxes
- Row click handling
- Action buttons per row
- Empty state messaging
- Integration with ColumnReorderManager for dynamic column control

**Column Freezing Architecture**:
The table view implements advanced frozen column support with dynamic positioning:

1. **CSS Sticky Positioning**: Uses `position: sticky` with calculated left positions
2. **Dynamic Width Calculation**: JavaScript measures actual rendered column widths
3. **Multi-Column Support**: Consecutive frozen columns stack correctly
4. **Visual Indicators**: Green border and shadow for frozen columns

```css
.frozen-column {
    position: sticky !important;
    left: [dynamically-calculated]px !important;
    z-index: 10-15;
    background: white;
    border-right: 2px solid #10b981;
    box-shadow: 2px 0 4px rgba(16, 185, 129, 0.1);
}
```

**JavaScript Width Calculation**:
```javascript
// Measure and position frozen columns dynamically
const headers = tableElement.querySelectorAll('thead th[data-column-key]');
let cumulativeLeft = 0;

headers.forEach((th) => {
    if (th.dataset.isFrozen === 'true') {
        const width = th.getBoundingClientRect().width;
        th.style.left = `${cumulativeLeft}px`;
        cumulativeLeft += width;
    }
});
```

This ensures proper positioning regardless of content width or column resizing.

### GenericCardView
**Purpose**: Grid layout with card components
**Location**: `/SteelEstimation.Web/Shared/Components/GenericCardView.razor`

**Features**:
- Responsive grid (configurable columns)
- Uses GenericCard for individual items
- Selection support
- Click handling
- Statistical displays

### GenericListView
**Purpose**: Compact list items in horizontal layout
**Location**: `/SteelEstimation.Web/Shared/Components/GenericListView.razor`

**Features**:
- Space-efficient design
- Shows primary/secondary properties
- Optional selection
- Action buttons
- Status indicators

### GenericCard
**Purpose**: Individual card component with reflection-based rendering
**Location**: `/SteelEstimation.Web/Shared/Components/GenericCard.razor`

**Features**:
- Dynamic property extraction
- Formatted statistics display
- Icon management
- Selection state
- Click handling

## Integration Pattern

### 1. Page Setup
Pages define their display configuration and column mappings:
```csharp
@implements IFilterProvider<Product>

private static readonly GenericDisplayConfig ProductDisplayConfig = new()
{
    PrimaryProperty = nameof(Product.Name),
    SecondaryProperty = nameof(Product.Description),
    StatusProperty = nameof(Product.Status),
    DefaultIcon = "fas fa-cube",
    Stats = new List<StatMapping>
    {
        new() { PropertyName = nameof(Product.Price), Label = "Price", Format = "C" },
        new() { PropertyName = nameof(Product.StockLevel), Label = "Stock" }
    }
};
```

### 2. Template Definition
Pages provide RenderFragments for each view mode with correct parameter usage:

**Table Template**:
```csharp
private RenderFragment tableTemplate => @<GenericTableView TItem="Product" 
                                           Items="@filteredProducts"
                                           Columns="@tableColumns"
                                           ShowSelection="true"
                                           SelectedItems="@selectedRows"
                                           OnSelectionChanged="@HandleSelectionChanged" />;
```

**Card Template**:
```csharp
private RenderFragment cardTemplate => @<GenericCardView TItem="Product"
                                         Items="@filteredProducts"
                                         DisplayConfig="@ProductDisplayConfig"
                                         OnItemClick="@ViewProduct" />;
```

**List Template**:
```csharp
private RenderFragment listTemplate => @<GenericListView TItem="Product"
                                         Items="@filteredProducts"
                                         DisplayConfig="@ProductDisplayConfig"
                                         ShowSelection="true"
                                         SelectedItems="@selectedRows"
                                         OnSelectionChanged="@HandleSelectionChanged" />;
```

**⚠️ Parameter Validation**: Ensure only valid parameters are used. Invalid parameters like `ShowActions`, `OnEdit`, or `OnView` will cause Blazor circuit crashes.

### 3. View Switcher Usage
**Standardized @bind-CurrentView Pattern**:
```razor
<GenericViewSwitcher 
    @bind-CurrentView="viewMode"
    ItemCount="@filteredProducts.Count()"
    TableTemplate="@tableTemplate"
    CardTemplate="@cardTemplate"
    ListTemplate="@listTemplate" />
```

**Important**: Always use `@bind-CurrentView` for two-way binding. Do not use separate `CurrentView` and `CurrentViewChanged` parameters as this can cause binding issues.

## Data Flow

### Property Extraction Flow
```
1. Page defines: DisplayConfig.PrimaryProperty = "ProductName"
2. GenericCard receives: Item = productInstance, Config = DisplayConfig
3. Reflection extracts: GetPropertyValue("ProductName") → "Steel Beam"
4. Rendered as: <h5 class="item-title">Steel Beam</h5>
```

### Selection Flow
```
1. User clicks checkbox in card/list/table
2. Component updates: SelectedItems.Add(item)
3. Event callback: OnSelectionChanged.InvokeAsync(SelectedItems)
4. Page updates: Toolbar actions refresh with selection count
```

### Frozen Column State Flow
```
1. User toggles freeze in ColumnReorderManager
2. ColumnDefinition.IsFrozen = true, FreezePosition = Left
3. ColumnsChanged.InvokeAsync(columns) triggered
4. Page.OnColumnsChanged creates deep copy (prevents reference pollution)
5. TriggerViewStateChanged() notifies SaveViewPreferences
6. DataTable applies CSS and calculates positions
7. JavaScript measures widths and sets left positions
```

## State Management Patterns

### Deep Copy Pattern for Column Definitions
Prevents reference pollution between view switches:
```csharp
private async Task OnColumnsChanged(List<ColumnDefinition<T>> columns)
{
    // Create new instances to avoid reference sharing
    columnDefinitions = columns.Select(c => new ColumnDefinition<T>
    {
        Key = c.Key,
        Header = c.Header,
        IsVisible = c.IsVisible,
        IsFrozen = c.IsFrozen,
        FreezePosition = c.FreezePosition,
        Order = c.Order,
        // ... copy all properties
    }).ToList();
    
    TriggerViewStateChanged();
    StateHasChanged();
}
```

### Explicit State Reset Pattern
Ensures clean transitions when loading saved views:
```csharp
public async Task ApplyViewState(string viewState)
{
    var state = DeserializeViewState(viewState);
    
    // Reset frozen states before applying saved configuration
    foreach (var column in columnDefinitions)
    {
        column.IsFrozen = false;
        column.FreezePosition = FreezePosition.None;
    }
    
    // Apply saved configuration
    state.ApplyColumnConfiguration(columnDefinitions);
    
    // Refresh visual state
    await viewSwitcher.RefreshFrozenColumns();
}
```

## Styling Architecture

### CSS Organization
Each component has its own scoped CSS file:
- `GenericViewSwitcher.razor.css` - View controls and container
- `GenericTableView.razor.css` - Table-specific styles
- `GenericCardView.razor.css` - Grid layout styles
- `GenericListView.razor.css` - List item styles
- `GenericCard.razor.css` - Card component styles

### Fab.OS Visual Identity
Components follow the Fab.OS design system:
- **Primary Blue**: #3144CD (active states, selections)
- **Deep Blue**: #0D1A80 (gradients, hover states)
- **Neutral Gray**: #777777 (text, borders)
- **Light Gray**: #B1B1B1 (inactive elements)

### Responsive Design
- Cards: 1-4 columns based on viewport
- Lists: Stack on mobile
- Tables: Horizontal scroll on small screens
- View switcher: Adapts button layout

## Benefits

### Consistency
- Uniform appearance across all list/worksheet pages
- Standardized interaction patterns
- Predictable user experience

### Maintainability
- Single source of truth for view rendering
- Centralized styling updates
- Reduced code duplication

### Extensibility
- Easy to add new view modes
- Simple property mapping for new entities
- Pluggable formatting and rendering

### Performance
- Efficient reflection with caching
- Lazy loading support ready
- Optimized re-rendering

## Usage Guidelines

### When to Use
- **List Pages**: Any page showing browsable collections
- **Worksheet Pages**: Data processing and batch operations
- **Search Results**: Unified display of search results

### When Not to Use
- **Card Pages**: Single record editing (use forms)
- **Document Pages**: Complex transactions (custom layouts)
- **Dashboards**: Specialized visualizations needed

### Best Practices
1. Always define GenericDisplayConfig as static readonly
2. Use nameof() for property references (compile-time safety)
3. Limit stats to 4 items for optimal card layout
4. Provide meaningful empty messages
5. Include proper icons for visual hierarchy

## Troubleshooting

### Common Issues and Solutions

#### GenericViewSwitcher Buttons Not Responding
**Symptoms**: View switcher buttons appear but don't change views
**Causes and Solutions**:
1. **Invalid Component Parameters**: Check for non-existent parameters like `ShowActions`, `OnEdit`, `OnView`
   - **Fix**: Remove invalid parameters from RenderFragment templates
   - **Error**: "Object of type 'GenericCardView' does not have a property matching the name 'ShowActions'"

2. **Incorrect Binding Pattern**: Using separate CurrentView/CurrentViewChanged instead of @bind
   - **Fix**: Change to `@bind-CurrentView="viewMode"`
   - **Remove**: Any manual `HandleViewModeChanged` methods

3. **Blazor Circuit Crash**: Invalid parameters cause silent failures
   - **Fix**: Check browser console for Blazor errors
   - **Solution**: Rebuild Docker container after Razor component changes

#### Parameter Validation Errors
**Error**: "Object of type 'ComponentName' does not have a property matching the name 'PropertyName'"
**Solution**: Verify all parameters exist on the target component using API reference above

#### Changes Not Reflecting
**For Razor Component Changes**:
1. Run Docker rebuild: `./rebuild.ps1`
2. Wait for full rebuild (~2-3 minutes)
3. Refresh browser

**For CSS/JS Changes**:
1. Hard refresh browser (Ctrl+F5)
2. Clear browser cache if needed

## Implementation Validation

To ensure correct implementation:

1. **Check Binding Pattern**: Verify `@bind-CurrentView` usage
2. **Validate Parameters**: Compare with API reference
3. **Test All Views**: Ensure table, card, and list modes work
4. **Browser Console**: Check for Blazor circuit errors
5. **Component Templates**: Verify RenderFragments use valid parameters only

## Future Enhancements

### Planned Features
- Advanced sorting within views
- Inline editing in table view
- Drag-and-drop reordering
- Export functionality per view
- Enhanced accessibility features

### Extension Points
- Custom card templates via RenderFragment
- Additional view modes (gallery, timeline)
- Theme variations
- Performance optimizations for large datasets

## Migration from Legacy Components

### From DataViewSwitcher
1. Replace component reference with GenericViewSwitcher
2. Convert hardcoded templates to RenderFragments
3. Create GenericDisplayConfig for entity
4. Update imports and namespaces

### From Custom Views
1. Identify common patterns
2. Map properties to GenericDisplayConfig
3. Replace custom HTML with generic components
4. Test all view modes

## Component API Reference

### GenericViewSwitcher
| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| CurrentView | string | "table" | Active view mode (use with @bind-CurrentView) |
| CurrentViewChanged | EventCallback<string> | - | View change callback (automatically handled by @bind) |
| TableTemplate | RenderFragment? | null | Table view template |
| CardTemplate | RenderFragment? | null | Card view template |
| ListTemplate | RenderFragment? | null | List view template |
| ItemCount | int | 0 | Total items count |
| ShowLabels | bool | false | Show view labels |
| ShowCount | bool | true | Show item count |

**Binding Pattern**: Always use `@bind-CurrentView="viewMode"` for proper two-way binding.

### GenericTableView
| Parameter | Type | Description |
|-----------|------|--------------|
| Items | IEnumerable<TItem> | Data collection |
| Columns | List<TableColumn<TItem>> | Column definitions |
| ShowSelection | bool | Enable row selection |
| SelectedItems | HashSet<TItem>? | Selected items collection |
| OnSelectionChanged | EventCallback<HashSet<TItem>>? | Selection change callback |
| OnRowClick | EventCallback<TItem>? | Row click callback |

### GenericCardView
| Parameter | Type | Description |
|-----------|------|--------------|
| Items | IEnumerable<TItem> | Data collection |
| DisplayConfig | GenericDisplayConfig | Property mapping configuration |
| OnItemClick | EventCallback<TItem>? | Card click callback |
| GridColumns | int | Grid columns (1-4, responsive) |

### GenericListView
| Parameter | Type | Description |
|-----------|------|--------------|
| Items | IEnumerable<TItem> | Data collection |
| DisplayConfig | GenericDisplayConfig | Property mapping configuration |
| ShowSelection | bool | Enable item selection |
| SelectedItems | HashSet<TItem>? | Selected items collection |
| OnSelectionChanged | EventCallback<HashSet<TItem>>? | Selection change callback |
| OnItemClick | EventCallback<TItem>? | Item click callback |

### GenericDisplayConfig
| Property | Type | Purpose |
|----------|------|---------|
| PrimaryProperty | string? | Main display text |
| SecondaryProperty | string? | Supporting text |
| TertiaryProperty | string? | Additional info |
| StatusProperty | string? | Status indicator |
| DefaultIcon | string | Fallback icon class |
| Stats | List<StatMapping> | Statistical displays |

### StatMapping
| Property | Type | Purpose |
|----------|------|---------|
| PropertyName | string | Property to display |
| Label | string | Display label |
| Icon | string | Icon class |
| Format | string? | Format string (C, P, etc.) |

## Conclusion

The Generic View System provides a robust, maintainable solution for displaying data collections consistently across the application. By separating view logic from data presentation and using reflection-based rendering, it achieves both flexibility and standardization while maintaining the Fab.OS visual identity.