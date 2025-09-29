# Generic View System Architecture - Fab.OS

## Executive Summary
The Generic View System provides a comprehensive, reusable framework for displaying collections of data across all list and worksheet pages in Fab.OS. It delivers seamless view switching between table, card, and list views with advanced features including frozen columns, dynamic filtering, persistent view preferences, and integrated column management - all while maintaining the Fab.OS visual identity.

## Architecture Overview

### System Components
```
┌─────────────────────────────────────────────────────────────────────┐
│                           Page Level                                 │
│         (PackagesList.razor, other list pages)                       │
│                                                                       │
│  • StandardToolbar integration with RenderFragment sections          │
│  • Unified view state management with ViewState model                │
│  • Async column management with IJSRuntime injection                 │
│  • Combined search and filter logic integration                      │
└───────────────────────────┬─────────────────────────────────────────┘
                            │
┌───────────────────────────▼─────────────────────────────────────────┐
│                     StandardToolbar                                  │
│                                                                       │
│  ┌─────────────┐  ┌───────────────┐  ┌────────────┐  ┌─────────────┐ │
│  │ViewSwitcher │  │ColumnManager  │  │FilterButton│  │ ViewSaving  │ │
│  │             │  │               │  │            │  │             │ │
│  │ • Table     │  │ • Visibility  │  │ • Dialog   │  │ • Load      │ │
│  │ • Card      │  │ • Reordering  │  │ • Operators│  │ • Save      │ │
│  │ • List      │  │ • Freezing    │  │ • Reflection│  │ • Default   │ │
│  └─────────────┘  └───────────────┘  └────────────┘  └─────────────┘ │
└───────────────────────────┬─────────────────────────────────────────┘
                            │
        ┌───────────────────┼───────────────────┐
        │                   │                   │
┌───────▼──────┐     ┌─────▼──────┐     ┌─────▼──────┐
│GenericTable  │     │GenericCard │     │GenericList │
│    View      │     │    View    │     │    View    │
│              │     │            │     │            │
│ • Advanced   │     │ • Grid     │     │ • Compact  │
│   features   │     │   layout   │     │   items    │
│ • Frozen     │     │ • Custom   │     │ • Custom   │
│   columns    │     │   content  │     │   content  │
│ • JS Interop │     │ • Selection│     │ • Selection│
└──────────────┘     └────────────┘     └────────────┘
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

### 2. Enhanced ColumnDefinition Model
Comprehensive column configuration with freeze support and proper state management:
```csharp
namespace FabOS.WebServer.Models.Columns
{
    public class ColumnDefinition
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string PropertyName { get; set; } = "";
        public string DisplayName { get; set; } = "";
        public bool IsVisible { get; set; } = true;
        public bool IsFrozen { get; set; } = false;
        public FreezePosition FreezePosition { get; set; } = FreezePosition.None;
        public int Order { get; set; } = 0;
        public int? Width { get; set; }
        public bool IsRequired { get; set; } = false;
        public string DataType { get; set; } = "string";
    }

    public enum FreezePosition
    {
        None,   // Not frozen
        Left,   // Freeze to left side
        Right   // Freeze to right side
    }
}
```

### 3. TableColumn Model for Table View
Bridge between ColumnDefinition and table rendering:
```csharp
public class TableColumn<T>
{
    public string Title { get; set; } = "";
    public string Header { get; set; } = "";
    public Func<T, object?>? ValueSelector { get; set; }
    public string PropertyName { get; set; } = "";
    public string CssClass { get; set; } = "";
    public bool IsSortable { get; set; } = true;
    public bool IsFilterable { get; set; } = false;
    public RenderFragment<T>? Template { get; set; }
}
```

### 4. View Modes
Three consistent view modes with advanced feature support:
- **Table View**: Advanced grid with frozen columns, sorting, filtering, and selection
- **Card View**: Responsive grid layout with custom content and selection support
- **List View**: Compact horizontal items with custom content templates

### 5. Async State Management
Advanced asynchronous patterns for JavaScript interop and state persistence:
```csharp
// Async column management with IJSRuntime
private async Task OnColumnsChanged(List<ColumnDefinition>? columns)
{
    if (columns != null)
    {
        columnDefinitions = columns;
        await UpdateTableColumns();
        hasUnsavedChanges = true;
    }
    StateHasChanged();
}

// Safe JavaScript interop with error handling
private async Task ApplyFrozenColumns()
{
    try
    {
        if (JSRuntime != null)
        {
            var frozenColumns = columnDefinitions
                .Where(c => c.IsVisible && c.IsFrozen)
                .OrderBy(c => c.Order)
                .Select(c => new { PropertyName = c.PropertyName, FreezePosition = c.FreezePosition.ToString() })
                .ToArray();

            await JSRuntime.InvokeVoidAsync("applyFrozenColumns", frozenColumns);
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

### 6. Reflection-Based Rendering with Error Handling
Components use reflection to extract data with robust error handling:
```csharp
private object? GetPropertyValue(string propertyName)
{
    try
    {
        var property = typeof(TItem).GetProperty(propertyName);
        return property?.GetValue(Item);
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Error getting property value for {propertyName}: {ex.Message}");
        return null;
    }
}
```

## Component Details

### GenericTableView Component
**Purpose**: Advanced table rendering with frozen columns and integrated features
**Location**: `/Components/Shared/GenericTableView.razor`

**Key Features**:
- Generic type support with `TItem` parameter
- Advanced column management integration with ColumnDefinition model
- Frozen column support with JavaScript positioning
- Built-in filtering through FilterDialog integration
- Row selection with multi-select support
- Responsive design with horizontal scrolling
- Custom cell templates via RenderFragment
- Async state management patterns

**Advanced Features Integration**:
```csharp
[Parameter] public bool EnableAdvancedFeatures { get; set; } = false;
[Parameter] public bool EnableColumnManagement { get; set; } = false;
[Parameter] public bool EnableFiltering { get; set; } = false;
[Parameter] public bool EnableViewPreferences { get; set; } = false;
[Parameter] public string EntityType { get; set; } = "";
```

**Frozen Column Implementation**:
The table view implements advanced frozen column support with dynamic JavaScript positioning:

1. **CSS Sticky Positioning**: Uses `position: sticky` with calculated left positions
2. **Dynamic Width Calculation**: JavaScript measures actual rendered column widths
3. **Multi-Column Support**: Consecutive frozen columns stack correctly
4. **Fab.OS Visual Identity**: Blue borders and shadows for frozen columns

```css
.frozen-column {
    position: sticky !important;
    left: [dynamically-calculated]px !important;
    z-index: 10-15;
    background: white;
    border-right: 2px solid var(--fabos-secondary);
    box-shadow: 2px 0 4px rgba(49, 68, 205, 0.1);
}
```

**JavaScript Width Calculation**:
```javascript
// Enhanced frozen column positioning - table-column-freeze.js
window.applyFrozenColumns = function(frozenColumns) {
    const table = document.querySelector('.fabos-table');
    if (!table) return;

    clearFrozenColumns();

    const leftFrozen = frozenColumns.filter(col => col.FreezePosition === 'Left')
                                   .sort((a, b) => a.Order - b.Order);
    const rightFrozen = frozenColumns.filter(col => col.FreezePosition === 'Right')
                                    .sort((a, b) => a.Order - b.Order);

    let leftOffset = 0;
    leftFrozen.forEach(column => {
        applyFrozenColumnStyles(table, column.PropertyName, leftOffset, 'left');
        leftOffset += getColumnWidth(table, column.PropertyName);
    });

    let rightOffset = 0;
    rightFrozen.reverse().forEach(column => {
        applyFrozenColumnStyles(table, column.PropertyName, rightOffset, 'right');
        rightOffset += getColumnWidth(table, column.PropertyName);
    });
};
```

### ColumnManagerDropdown Component
**Purpose**: Unified column management with visibility, reordering, and freeze options
**Location**: `/Components/Shared/ColumnManagerDropdown.razor`

**Features**:
- **Unified Interface**: Single dropdown for all column management needs
- **Visibility Controls**: Toggle columns on/off with checkboxes
- **Reordering**: Up/down buttons for changing column order
- **Freeze Management**: Freeze left/right options for each column
- **Bulk Operations**: Show all, hide all, reset to defaults
- **Fab.OS Styling**: Consistent blue theme with gradients

**Integration Pattern**:
```razor
<ColumnManagerDropdown Columns="@columnDefinitions"
                     OnColumnsChanged="@(async (columns) => await OnColumnsChanged(columns))" />
```

For complete details, see [Column Management Architecture](column-management-architecture.md).

### GenericViewSwitcher Component
**Purpose**: Orchestrates view switching with integrated toolbar placement
**Location**: `/Components/Shared/GenericViewSwitcher.razor`

**Key Features**:
- Manages current view state with ViewType enum
- Renders appropriate view component based on selection
- Integrates with StandardToolbar for consistent layout
- Maintains view preferences through ViewState system
- Supports custom content templates for each view type

**Enhanced Parameters**:
```csharp
[Parameter] public ViewType CurrentView { get; set; } = ViewType.Table;
[Parameter] public EventCallback<ViewType> CurrentViewChanged { get; set; }
[Parameter] public bool ShowViewPreferences { get; set; } = true;
[Parameter] public string CssClass { get; set; } = "";

public enum ViewType
{
    Table,
    Card,
    List
}
```

**Toolbar Integration Pattern**:
```razor
<StandardToolbar ActionProvider="@this" OnSearch="@OnSearchChanged">
    <ViewSwitcher>
        <GenericViewSwitcher TItem="Package"
                           CurrentView="@currentView"
                           CurrentViewChanged="@OnViewChanged"
                           ShowViewPreferences="false" />
    </ViewSwitcher>
</StandardToolbar>
```

### Enhanced GenericTableView Implementation
**Current Implementation**: Already covered above with advanced features integration
See the GenericTableView Component section for complete implementation details including:
- Advanced features parameters
- Frozen column support with JavaScript positioning
- Integration with ColumnManagerDropdown
- Async state management patterns
- Error handling for JavaScript interop

### GenericCardView Component
**Purpose**: Responsive grid layout with custom card content
**Location**: `/Components/Shared/GenericCardView.razor`

**Enhanced Features**:
- Responsive grid layout (1-4 columns based on viewport)
- Custom content templates via `CustomCardContent` RenderFragment
- Selection support with multi-select capabilities
- Item click and double-click handling
- Integration with view state management
- Consistent Fab.OS styling

**Custom Content Integration**:
```razor
<GenericCardView TItem="Package"
                Items="@filteredPackages"
                OnItemClick="@HandleRowClick"
                OnItemDoubleClick="@HandleRowDoubleClick"
                AllowSelection="true"
                SelectedItems="@selectedCardItems"
                SelectedItemsChanged="@HandleCardSelectionChanged">
    <CustomCardContent Context="package">
        <div class="package-card">
            <div class="package-card-header">
                <h4>@package.PackageName</h4>
                <span class="badge">@package.PackageNumber</span>
            </div>
            <p class="package-card-description">@(package.Description ?? "No description")</p>
            <div class="package-card-footer">
                <span class="package-status">@(package.Status ?? "Active")</span>
                <span class="package-cost">$@package.EstimatedCost.ToString("N2")</span>
            </div>
        </div>
    </CustomCardContent>
</GenericCardView>
```

### GenericListView Component
**Purpose**: Compact list items with custom content templates
**Location**: `/Components/Shared/GenericListView.razor`

**Enhanced Features**:
- Space-efficient horizontal layout design
- Custom list item templates via `CustomListItemContent` RenderFragment
- Selection support with checkboxes
- Item click and double-click handling
- Mobile-responsive design

**Custom Content Integration**:
```razor
<GenericListView TItem="Package"
                Items="@filteredPackages"
                OnItemClick="@HandleRowClick"
                OnItemDoubleClick="@HandleRowDoubleClick"
                AllowSelection="true"
                SelectedItems="@selectedListItems"
                SelectedItemsChanged="@HandleListSelectionChanged">
    <CustomListItemContent Context="package">
        <div class="package-list-item">
            <div class="package-header">
                <h3 class="package-title">@package.PackageName</h3>
                <span class="package-number">@package.PackageNumber</span>
            </div>
            <p class="package-description">@(package.Description ?? "No description available")</p>
            <div class="package-meta">
                <span class="package-status">@(package.Status ?? "Active")</span>
                <span class="package-hours">@package.EstimatedHours.ToString("N2") hours</span>
                <span class="package-cost">$@package.EstimatedCost.ToString("N2")</span>
            </div>
        </div>
    </CustomListItemContent>
</GenericListView>
```

## Integration Pattern

### 1. Enhanced Page Setup with StandardToolbar
Pages integrate with the unified toolbar system for consistent layout:
```razor
@page "/packages"
@rendermode InteractiveServer
@using FabOS.WebServer.Components.Shared
@using FabOS.WebServer.Models.Entities
@using FabOS.WebServer.Models.Columns
@using FabOS.WebServer.Models.Filtering
@using FabOS.WebServer.Models.ViewState

<!-- Standard Toolbar with View Controls -->
<StandardToolbar ActionProvider="@this" OnSearch="@OnSearchChanged"
                SearchPlaceholder="Search packages..." PageType="PageType.List">
    <ViewSwitcher>
        <GenericViewSwitcher TItem="Package"
                           CurrentView="@currentView"
                           CurrentViewChanged="@OnViewChanged"
                           ShowViewPreferences="false" />
    </ViewSwitcher>
    <ColumnManager>
        <ColumnManagerDropdown Columns="@columnDefinitions"
                             OnColumnsChanged="@(async (columns) => await OnColumnsChanged(columns))" />
    </ColumnManager>
    <FilterButton>
        <FilterDialog TItem="Package"
                    OnFiltersChanged="@OnFiltersChanged" />
    </FilterButton>
    <ViewSaving>
        <ViewSavingDropdown EntityType="Packages"
                          CurrentState="@currentViewState"
                          OnViewLoaded="@(async (state) => await OnViewLoaded(state))"
                          HasUnsavedChanges="@hasUnsavedChanges" />
    </ViewSaving>
</StandardToolbar>
```

### 2. Async State Management Implementation
Pages implement comprehensive async patterns for state management:
```csharp
[Inject] private IJSRuntime JSRuntime { get; set; } = default!;

private List<ColumnDefinition> columnDefinitions = new();
private List<FilterRule> activeFilters = new();
private ViewState currentViewState = new();
private bool hasUnsavedChanges = false;

// Async column management
private async Task OnColumnsChanged(List<ColumnDefinition>? columns)
{
    if (columns != null)
    {
        columnDefinitions = columns;
        await UpdateTableColumns();
        hasUnsavedChanges = true;
    }
    StateHasChanged();
}

// Combined search and filter logic
private void FilterPackages()
{
    var result = packages.AsEnumerable();

    // Apply search filter
    if (!string.IsNullOrEmpty(searchTerm))
    {
        var searchLower = searchTerm.ToLower();
        result = result.Where(p =>
            (p.PackageNumber?.ToLower().Contains(searchLower) ?? false) ||
            (p.PackageName?.ToLower().Contains(searchLower) ?? false) ||
            (p.Description?.ToLower().Contains(searchLower) ?? false) ||
            (p.Status?.ToLower().Contains(searchLower) ?? false)
        );
    }

    // Apply active filters with error handling
    if (activeFilters.Any())
    {
        result = result.Where(package =>
        {
            try
            {
                foreach (var filter in activeFilters)
                {
                    var fieldName = filter.Field ?? filter.FieldName;
                    if (string.IsNullOrEmpty(fieldName))
                        continue;

                    var propertyInfo = typeof(Package).GetProperty(fieldName);
                    if (propertyInfo != null && propertyInfo.CanRead)
                    {
                        var value = propertyInfo.GetValue(package);
                        if (!MatchesFilter(value, filter))
                            return false;
                    }
                }
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error filtering package {package?.Id}: {ex.Message}");
                return true; // Include item if filtering fails
            }
        });
    }

    filteredPackages = result.ToList();
}
```

### 3. View Implementation with Custom Content
Views are implemented directly in the page markup with custom content templates:

**Table View**:
```razor
@if (currentView == GenericViewSwitcher<Package>.ViewType.Table)
{
    <GenericTableView TItem="Package"
                     Items="@filteredPackages"
                     Columns="@tableColumns"
                     AllowSelection="true"
                     ShowCheckboxes="true"
                     SelectedItems="@selectedTableItems"
                     SelectedItemsChanged="@HandleTableSelectionChanged"
                     OnRowClick="@HandleRowClick"
                     OnRowDoubleClick="@HandleRowDoubleClick" />
}
```

**Card View with Custom Content**:
```razor
@if (currentView == GenericViewSwitcher<Package>.ViewType.Card)
{
    <GenericCardView TItem="Package"
                    Items="@filteredPackages"
                    OnItemClick="@HandleRowClick"
                    OnItemDoubleClick="@HandleRowDoubleClick"
                    AllowSelection="true"
                    SelectedItems="@selectedCardItems"
                    SelectedItemsChanged="@HandleCardSelectionChanged">
        <CustomCardContent Context="package">
            <div class="package-card">
                <div class="package-card-header">
                    <h4>@package.PackageName</h4>
                    <span class="badge">@package.PackageNumber</span>
                </div>
                <p class="package-card-description">@(package.Description ?? "No description")</p>
                <div class="package-card-footer">
                    <span class="package-status">@(package.Status ?? "Active")</span>
                    <span class="package-cost">$@package.EstimatedCost.ToString("N2")</span>
                </div>
            </div>
        </CustomCardContent>
    </GenericCardView>
}
```

**List View with Custom Content**:
```razor
@if (currentView == GenericViewSwitcher<Package>.ViewType.List)
{
    <GenericListView TItem="Package"
                    Items="@filteredPackages"
                    OnItemClick="@HandleRowClick"
                    OnItemDoubleClick="@HandleRowDoubleClick"
                    AllowSelection="true"
                    SelectedItems="@selectedListItems"
                    SelectedItemsChanged="@HandleListSelectionChanged">
        <CustomListItemContent Context="package">
            <div class="package-list-item">
                <div class="package-header">
                    <h3 class="package-title">@package.PackageName</h3>
                    <span class="package-number">@package.PackageNumber</span>
                </div>
                <p class="package-description">@(package.Description ?? "No description available")</p>
                <div class="package-meta">
                    <span class="package-status">@(package.Status ?? "Active")</span>
                    <span class="package-hours">@package.EstimatedHours.ToString("N2") hours</span>
                    <span class="package-cost">$@package.EstimatedCost.ToString("N2")</span>
                </div>
            </div>
        </CustomListItemContent>
    </GenericListView>
}
```

## Data Flow

### Unified Toolbar Integration Flow
```
1. Page loads with StandardToolbar containing RenderFragment sections
2. ViewSwitcher renders GenericViewSwitcher component
3. ColumnManager renders ColumnManagerDropdown component
4. FilterButton renders FilterDialog component
5. ViewSaving renders ViewSavingDropdown component
6. All components communicate through async EventCallbacks
```

### Enhanced Selection Flow
```
1. User clicks checkbox/selects item in any view (table/card/list)
2. View component updates: SelectedItems collection
3. Async callback: SelectedItemsChanged.InvokeAsync(SelectedItems)
4. Page handles selection across all view types consistently
5. Selection state maintained during view switching
```

### Advanced Frozen Column State Flow
```
1. User toggles freeze in ColumnManagerDropdown
2. Working columns updated: ColumnDefinition.IsFrozen = true, FreezePosition = Left
3. Async callback: OnColumnsChanged.InvokeAsync(columns) triggered
4. Page.OnColumnsChanged updates columnDefinitions
5. UpdateTableColumns() creates TableColumn instances from ColumnDefinitions
6. ApplyFrozenColumns() called with JSRuntime interop
7. JavaScript measures widths and applies dynamic positioning
8. hasUnsavedChanges = true triggers ViewSavingDropdown indicator
```

### Integrated Filter and Search Flow
```
1. User enters search term in StandardToolbar search
2. OnSearchChanged callback updates searchTerm
3. FilterPackages() applies combined search and filter logic
4. User adds filter rules in FilterDialog
5. OnFiltersChanged callback updates activeFilters
6. FilterPackages() re-applies with new filter criteria
7. All views (table/card/list) show filtered results
8. hasUnsavedChanges = true for view state persistence
```

## State Management Patterns

### Async State Management Pattern
Comprehensive async patterns with error handling for JavaScript interop:
```csharp
private async Task OnColumnsChanged(List<ColumnDefinition>? columns)
{
    if (columns != null)
    {
        columnDefinitions = columns;
        await UpdateTableColumns();
        hasUnsavedChanges = true;
    }
    StateHasChanged();
}

private async Task UpdateTableColumns()
{
    tableColumns = columnDefinitions
        .Where(c => c.IsVisible)
        .OrderBy(c => c.Order)
        .Select(c => CreateTableColumn(c))
        .ToList();

    await ApplyFrozenColumns();
}

private async Task ApplyFrozenColumns()
{
    try
    {
        if (JSRuntime != null)
        {
            var frozenColumns = columnDefinitions
                .Where(c => c.IsVisible && c.IsFrozen)
                .OrderBy(c => c.Order)
                .Select(c => new
                {
                    PropertyName = c.PropertyName,
                    FreezePosition = c.FreezePosition.ToString()
                })
                .ToArray();

            await JSRuntime.InvokeVoidAsync("applyFrozenColumns", frozenColumns);
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

### Enhanced View State Loading Pattern
Handles complete state restoration with async operations:
```csharp
private async Task OnViewLoaded(ViewState? state)
{
    if (state == null)
    {
        // Reset to defaults
        InitializeDefaultColumns();
        activeFilters.Clear();
        FilterPackages();
    }
    else
    {
        currentViewState = state;
        if (state.Columns.Any())
        {
            columnDefinitions = state.Columns;
            await UpdateTableColumns();
        }
        if (state.Filters.Any())
        {
            activeFilters = state.Filters;
            FilterPackages();
        }
        if (state.CurrentView != null)
        {
            currentView = state.CurrentView.Value;
        }
    }

    hasUnsavedChanges = false;
    StateHasChanged();
}
```

### Error-Safe Reflection Pattern
Robust property access with comprehensive error handling:
```csharp
private bool MatchesFilter(object? value, FilterRule filter)
{
    try
    {
        var stringValue = value?.ToString() ?? "";
        var filterValue = filter.Value?.ToString() ?? "";

        return filter.Operator switch
        {
            FilterOperator.Equals => stringValue.Equals(filterValue, StringComparison.OrdinalIgnoreCase),
            FilterOperator.NotEquals => !stringValue.Equals(filterValue, StringComparison.OrdinalIgnoreCase),
            FilterOperator.Contains => stringValue.Contains(filterValue, StringComparison.OrdinalIgnoreCase),
            FilterOperator.StartsWith => stringValue.StartsWith(filterValue, StringComparison.OrdinalIgnoreCase),
            FilterOperator.EndsWith => stringValue.EndsWith(filterValue, StringComparison.OrdinalIgnoreCase),
            FilterOperator.GreaterThan => CompareNumeric(value, filter.Value, (a, b) => a > b),
            FilterOperator.LessThan => CompareNumeric(value, filter.Value, (a, b) => a < b),
            FilterOperator.Between => CompareBetween(value, filter.Value, filter.SecondValue),
            _ => true
        };
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Error applying filter: {ex.Message}");
        return true; // If filter fails, include the item
    }
}
```

## Styling Architecture

### CSS Organization
Comprehensive styling system with unified Fab.OS theme:
- `/wwwroot/css/view-controls.css` - Unified styling for all view control components
- `/wwwroot/css/frozen-columns.css` - Frozen column positioning and visual effects
- `GenericViewSwitcher.razor.css` - View switcher controls
- `GenericTableView.razor.css` - Table-specific styles with frozen column support
- `GenericCardView.razor.css` - Responsive grid layout styles
- `GenericListView.razor.css` - Compact list item styles

### Fab.OS Visual Identity
All components follow the consistent Fab.OS design system:
- **Primary Blue**: #3144CD (var(--fabos-primary) - active states, selections)
- **Secondary Blue**: #4F6AF7 (var(--fabos-secondary) - buttons, accents)
- **Deep Blue**: #0D1A80 (gradients, hover states)
- **Success Green**: #10B981 (var(--fabos-success) - indicators, badges)
- **Neutral Gray**: #777777 (text, borders)
- **Light Gray**: #B1B1B1 (inactive elements)

### Enhanced Responsive Design
- **Cards**: 1-4 columns based on viewport with gap consistency
- **Lists**: Horizontal layout with mobile stacking
- **Tables**: Horizontal scroll with frozen column positioning
- **Toolbar**: Responsive layout with collapsing sections
- **Dropdowns**: Position-aware with proper z-indexing

### Frozen Column Visual Treatment
```css
.frozen-column {
    position: sticky !important;
    left: var(--frozen-left-position) !important;
    z-index: 10;
    background: white;
    border-right: 2px solid var(--fabos-secondary);
    box-shadow: 2px 0 4px rgba(49, 68, 205, 0.1);
}

.frozen-column.frozen-right {
    left: auto !important;
    right: var(--frozen-right-position) !important;
    border-right: none;
    border-left: 2px solid var(--fabos-secondary);
    box-shadow: -2px 0 4px rgba(49, 68, 205, 0.1);
}
```

## Benefits

### Enhanced Consistency
- Uniform appearance across all list pages with Fab.OS design system
- Standardized interaction patterns with unified toolbar integration
- Predictable user experience with consistent view switching
- Cohesive styling with blue theme throughout all components

### Advanced Maintainability
- Single source of truth for view rendering with generic components
- Centralized styling through unified CSS architecture
- Reduced code duplication with reusable components
- Async patterns ensure robust state management
- Error handling prevents component crashes

### Superior Extensibility
- Easy integration of new pages through StandardToolbar pattern
- Custom content templates for flexible data presentation
- Pluggable filtering with reflection-based field detection
- Modular component architecture supports feature additions
- Frozen column system adapts to any entity type

### Optimized Performance
- Efficient JavaScript interop with error handling
- Async state management prevents UI blocking
- Frozen column positioning with dynamic width calculations
- Optimized re-rendering with StateHasChanged() controls
- Memory-efficient reflection patterns

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

The enhanced Generic View System provides a comprehensive, enterprise-grade solution for displaying data collections across Fab.OS. Through the integration of StandardToolbar, advanced frozen columns, unified filtering, and persistent view preferences, it delivers a powerful and consistent user experience.

Key achievements include:
- **Unified Architecture**: All list pages follow the same patterns with StandardToolbar integration
- **Advanced Features**: Frozen columns, filtering, and view preferences work seamlessly together
- **Robust Implementation**: Async patterns and error handling ensure reliable operation
- **Fab.OS Consistency**: Blue theme and design patterns maintained throughout
- **Future-Ready**: Modular architecture supports easy extension and maintenance

This system serves as the foundation for all data collection interfaces in Fab.OS, providing users with powerful, consistent tools for managing and viewing their data while maintaining the application's visual identity and performance standards.