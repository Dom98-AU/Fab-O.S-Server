# Page Types Architecture Plan

## Overview
Define standard page types for the SteelEstimation application following Business Central patterns to ensure consistent user experience and development standards.

## Page Type Definitions

### 1. List Pages
**Purpose**: Display collections of records for browsing, filtering, and selection
**Characteristics**:
- Multiple records displayed in a table/grid/list format
- Include Generic View System (table/grid/list switching)
- Support filtering, searching, sorting
- Support multi-select operations
- **Mandatory StandardToolbar** with action providers following Fab.OS Toolbar Specification

**Examples**:
- Customer List (`/customers`)
- Sales Order List (`/orders`)
- Product List (`/products`)
- Invoice List (`/invoices`)
- Supplier List (`/suppliers`)
- Project List (`/projects`)

**StandardToolbar Implementation**:
All List Pages must implement the StandardToolbar component with proper IToolbarActionProvider integration. The toolbar must follow the **Fab.OS Toolbar Specification** detailed in the Fab.OS Visual Identity Guidelines document (`C:\Fab.OS Platform\Fab O.S Web-Design Architecture\fabos_visual_identity.md`), including:

- Fab.OS color palette implementation
- Proper gradient backgrounds and hover states
- Accessibility features and focus states
- Responsive behavior requirements
- Button styling and visual hierarchy

See the "Toolbar Specification" section of the Fab.OS Visual Identity Guidelines for complete implementation details.

**Layout Structure**:
```
┌─ StandardToolbar (Fab.OS styled) ─┐
├─ Breadcrumb Navigation          ─┤
├─ FilterSystem Component         ─┤
├─ GenericViewSwitcher            ─┤
│  ├─ Table View                  │
│  ├─ Grid View (cards)           │
│  └─ List View (compact)         │
└─ Pagination                     ─┘
```

### 2. Card Pages
**Purpose**: Display and edit a single record with detailed form layout
**Characteristics**:
- Single record focus
- Form-based layout with tabs/sections
- No view switching options
- StandardToolbar with record-specific actions (Save, Delete, etc.)
- Field validation and data entry

**Examples**:
- Customer Details (`/customers/{id}`)
- Sales Order Details (`/orders/{id}`)
- Product Details (`/products/{id}`)
- User Profile (`/profile`)
- Company Details (`/company/{id}`)

**Layout Structure**:
```
┌─ StandardToolbar (Save/Delete/etc) ─┐
├─ Breadcrumb Navigation          ─┤
├─ Record Header/Title              ─┤
├─ Form Sections/Tabs               ─┤
│  ├─ General Information           │
│  ├─ Contact Details               │
│  ├─ Address Information           │
│  └─ Additional Details            │
└─ Action Buttons                   ─┘
```

### 3. Document Pages
**Purpose**: Handle complex business transactions with multiple related records
**Characteristics**:
- Master-detail relationship (header + lines)
- Transaction workflow support
- Complex business logic and validation
- Document state management (Draft, Posted, etc.)
- Print/email capabilities

**Examples**:
- Sales Quote Creation (`/quotes/new`)
- Purchase Order Processing (`/purchase-orders/{id}`)
- Invoice Generation (`/invoices/new`)
- Estimation Document (`/estimations/{id}`)
- Contract Management (`/contracts/{id}`)

**Layout Structure**:
```
┌─ StandardToolbar (Process/Post/Print) ─┐
├─ Breadcrumb Navigation              ─┤
├─ Document Header                     ─┤
│  ├─ Document Number                  │
│  ├─ Customer/Supplier                │
│  └─ Dates/Status                     │
├─ Document Lines (Subgrid)            ─┤
│  ├─ Line Items Table                 │
│  ├─ Add/Remove Lines                 │
│  └─ Line Totals                      │
├─ Document Totals                     ─┤
└─ Workflow Actions                    ─┘
```

### 4. Worksheet Pages
**Purpose**: Data processing and batch operations on multiple records
**Characteristics**:
- Spreadsheet-like interface
- Batch data entry and processing
- Temporary data storage before posting
- Import/export capabilities
- Advanced filtering and sorting

**Examples**:
- Material Import Worksheet (`/worksheets/materials`)
- Price Update Worksheet (`/worksheets/prices`)
- Inventory Adjustment (`/worksheets/inventory`)
- Bulk Customer Updates (`/worksheets/customers`)
- Data Import Processing (`/worksheets/import`)

**Layout Structure**:
```
┌─ StandardToolbar (Process/Import/Export) ─┐
├─ Breadcrumb Navigation                  ─┤
├─ FilterSystem Component                 ─┤
├─ Worksheet Controls                     ─┤
│  ├─ Template Selection                  │
│  ├─ Batch Operations                    │
│  └─ Bulk Actions                        │
├─ Data Grid (Editable)                   ─┤
│  ├─ Spreadsheet Interface               │
│  ├─ Validation Indicators               │
│  └─ Calculation Columns                 │
└─ Processing Actions                     ─┘
```

## Implementation Guidelines

### Common Components Across All Page Types

#### Mandatory Components
All page types MUST include:

1. **StandardToolbar** with IToolbarActionProvider implementation
   - Provides consistent action structure across all pages
   - Handles save, delete, new, and other contextual actions

2. **Breadcrumb Navigation** using the Breadcrumb component
   - Positioned immediately after StandardToolbar
   - Shows hierarchical navigation path
   - Follows Fab.OS visual identity guidelines
   - See [Breadcrumb Navigation Guide](./breadcrumb-navigation-guide.md)

3. **FilterSystem** component (for List and Worksheet pages)
   - Provides consistent filtering interface across pages
   - Implements IFilterProvider<T> interface
   - Includes search, dropdown filters, and advanced filter builder
   - See [Filter System Implementation](./filter-system-implementation.md)

#### Required Interface Implementations

**List Pages** must implement:
```csharp
@implements IToolbarActionProvider         // Toolbar actions
@implements IFilterProvider<TEntity>       // Filtering functionality  
@implements ISaveableViewState             // View state persistence
```

**Worksheet Pages** must implement:
```csharp
@implements IToolbarActionProvider         // Toolbar actions
@implements IFilterProvider<TEntity>       // Filtering functionality
@implements ISaveableViewState             // View state persistence
```

#### StandardToolbar Integration
All page types use StandardToolbar with IToolbarActionProvider:

**List Pages** - Actual Working Implementation:
```csharp
public ToolbarActionGroup GetActions() => new()
{
    PrimaryActions = new List<ToolbarAction>
    {
        new() { Text = "Add Product", Icon = "fas fa-plus", Action = AddProduct, Style = ToolbarActionStyle.Primary },
        new() { Text = "Import", Icon = "fas fa-upload", Action = ImportProducts }
    },
    MenuActions = new List<ToolbarAction>
    {
        new() { Text = "Export", Icon = "fas fa-download", Action = ExportProducts },
        new() { Text = "Bulk Edit", Icon = "fas fa-edit", Action = BulkEdit, RequiresSelection = true }
    }
};
```

**Worksheet Pages** - Actual Working Implementation:
```csharp
public ToolbarActionGroup GetActions() => new()
{
    PrimaryActions = new List<ToolbarAction>
    {
        new() { Text = "Process", Icon = "fas fa-play", Action = ProcessWorksheet, Style = ToolbarActionStyle.Primary },
        new() { Text = "Import", Icon = "fas fa-upload", Action = ImportData }
    },
    MenuActions = new List<ToolbarAction>
    {
        new() { Text = "Export Template", Icon = "fas fa-file-download", Action = ExportTemplate },
        new() { Text = "Clear All", Icon = "fas fa-trash", Action = ClearWorksheet },
        new() { Text = "Validate", Icon = "fas fa-check-circle", Action = ValidateData }
    }
};
```

**Card Pages**:
```csharp
public ToolbarActionGroup GetActions() => new()
{
    PrimaryActions = new() { /* Save, Save & Close */ },
    MenuActions = new() { /* Delete, Duplicate, Archive */ },
    RelatedActions = new() { /* Related records */ }
};
```

**Document Pages**:
```csharp
public ToolbarActionGroup GetActions() => new()
{
    PrimaryActions = new() { /* Save Draft, Post, Print */ },
    MenuActions = new() { /* Copy Document, Email, Approvals */ },
    ReportsActions = new() { /* Preview, PDF Export */ }
};
```

### Column Management Features

#### Column Resizing
The **ColumnResizer** component provides drag-to-resize functionality for table columns:

**Integration with DataTable**:
```razor
@if (ColumnDefinitions != null)
{
    <ColumnResizer TItem="TItem" 
                   @ref="columnResizer"
                   Columns="@ColumnDefinitions" 
                   TableElement="@tableElement"
                   OnColumnResized="@HandleColumnResized" />
}
```

**Key Features**:
- Visual resize guide during drag operation
- Min/max width constraints
- Touch and mouse support
- Automatic persistence through saved views
- Integration with frozen columns

**Column Definition Configuration**:
```csharp
var columnDef = new ColumnDefinition<Product>
{
    Key = "ProductName",
    Header = "Product Name",
    IsResizable = true,        // Enable resizing
    Width = 200,               // Initial width
    MinWidth = 100,           // Minimum width constraint
    MaxWidth = 400,           // Maximum width constraint
    IsFrozen = false,
    FreezePosition = FreezePosition.None
};
```

#### Column Freezing
Columns can be frozen to the left or right side of the table:
- Frozen columns remain visible during horizontal scrolling
- Visual indicators (green border) for frozen columns
- Automatic position calculations for multiple frozen columns

#### Column Reordering
Using **ColumnReorderManager** for drag-and-drop column reordering:
- Visual feedback during drag operations
- Constraints on required columns
- Persistence through saved views

### Page-Specific Features

#### List Pages Only
- **MUST include FilterSystem component** with IFilterProvider implementation
- **MUST include GenericViewSwitcher component** for table/card/list views
  - See [Generic View System Architecture](./generic-view-system-architecture.md) for implementation details
- **MUST include SaveViewPreferences component** for view state persistence
  - See [Save View Preferences Architecture](./save-view-preferences-architecture.md) for implementation details
- Support GenericDisplayConfig for consistent card/list rendering
- Multi-select capabilities with bulk actions
- Dynamic filter value detection from actual page data

**Working List Page Structure**:
```razor
<div class="container-fluid px-0">
    <!-- StandardToolbar with action provider -->
    <StandardToolbar ActionProvider="@this" />
    
    <!-- FilterSystem for data filtering -->
    <div class="px-3 mt-3">
        <FilterSystem TItem="Product"
                     FilterProvider="@this"
                     Items="@products"
                     OnFilteredItemsChanged="@HandleFilteredItemsChanged"
                     ShowSearch="true"
                     SearchPlaceholder="Search products..."
                     @bind-ActiveFilters="activeFilters" />
    </div>
    
    <!-- Main content with GenericViewSwitcher -->
    <div class="card mx-3">
        <div class="card-body">
            <GenericViewSwitcher 
                @bind-CurrentView="viewMode"
                ItemCount="@filteredProducts.Count()"
                TableTemplate="@tableTemplate"
                CardTemplate="@cardTemplate"
                ListTemplate="@listTemplate" />
        </div>
    </div>
    
    <!-- SaveViewPreferences (when implemented) -->
    <SaveViewPreferences ViewStateProvider="this" 
                        OnViewLoaded="@HandleViewLoaded" />
</div>
```

#### Card Pages Only  
- Form validation and field-level error display
- Tab/section navigation
- Dirty state tracking (unsaved changes warning)
- Field dependencies and conditional visibility

#### Document Pages Only
- Master-detail editing with line item management
- Document workflow state management
- Complex calculation engines
- Approval processes and electronic signatures

#### Worksheet Pages Only
- **MUST include FilterSystem component** with IFilterProvider implementation  
- **MUST include GenericViewSwitcher component** for different view modes
  - See [Generic View System Architecture](./generic-view-system-architecture.md) for implementation details
- **MUST include SaveViewPreferences component** for view state persistence
  - See [Save View Preferences Architecture](./save-view-preferences-architecture.md) for implementation details
- Excel-like editing capabilities (maintained in all views)
- Template-based data entry
- Batch validation and error reporting
- Preview before processing functionality
- Dynamic filter value detection from actual worksheet data

**Working Worksheet Page Structure**:
```razor
<div class="container-fluid px-0">
    <!-- StandardToolbar for worksheet operations -->
    <StandardToolbar ActionProvider="@this" ShowSearch="false" />
    
    <!-- Worksheet info bar with actions -->
    <div class="worksheet-info-bar bg-light px-3 py-2 border-bottom">
        <div class="row align-items-center">
            <div class="col-md-4">
                <span class="fw-bold">Bulk Price Update Worksheet</span>
                <span class="badge bg-warning ms-2">@unsavedChanges.Count Unsaved Changes</span>
            </div>
            <div class="col-md-4 text-center">
                <small class="text-muted">
                    @selectedRows.Count of @filteredItems.Count() items selected
                </small>
            </div>
            <div class="col-md-4 text-end">
                <button class="btn btn-sm btn-outline-secondary me-2" @onclick="CalculateChanges">
                    <i class="fas fa-calculator"></i> Calculate
                </button>
                <button class="btn btn-sm btn-primary" @onclick="ApplyChanges">
                    <i class="fas fa-check"></i> Apply Changes
                </button>
            </div>
        </div>
    </div>
    
    <!-- FilterSystem for worksheet -->
    <div class="px-3 py-2">
        <FilterSystem TItem="WorksheetItem"
                     FilterProvider="@this"
                     Items="@worksheetItems"
                     OnFilteredItemsChanged="@HandleFilteredItemsChanged"
                     ShowSearch="true"
                     SearchPlaceholder="Search items..."
                     @bind-ActiveFilters="activeFilters" />
    </div>
    
    <!-- Main content with GenericViewSwitcher -->
    <div class="card mx-3">
        <div class="card-body">
            <GenericViewSwitcher 
                @bind-CurrentView="viewMode"
                ItemCount="@filteredItems.Count()"
                TableTemplate="@tableTemplate"
                CardTemplate="@cardTemplate"
                ListTemplate="@listTemplate" />
        </div>
    </div>
    
    <!-- SaveViewPreferences (when implemented) -->
    <SaveViewPreferences ViewStateProvider="this" 
                        OnViewLoaded="@HandleViewLoaded" />
</div>
```

### Navigation Patterns

#### List → Card Flow
```
Customer List → Customer Details
Orders List → Order Details
Products List → Product Details
```

#### List → Document Flow
```
Customer List → New Sales Quote
Orders List → Order Processing
```

#### Card → Related Lists Flow
```
Customer Details → Customer Orders (List)
Product Details → Product Sales (List)
```

#### Worksheet Processing Flow
```
Data Import → Worksheet → Validation → Processing → Results
```

### URL Structure by Page Type

#### List Pages
```
/customers                    (Customer List)
/orders                      (Order List)  
/products                    (Product List)
/invoices                    (Invoice List)
```

#### Card Pages
```
/customers/{id}              (Customer Card)
/customers/new               (New Customer Card)
/orders/{id}                 (Order Card)
/products/{id}/edit          (Product Card)
```

#### Document Pages
```
/quotes/new                  (New Quote Document)
/quotes/{id}                 (Quote Document)
/purchase-orders/{id}        (Purchase Order Document)
/estimations/{id}            (Estimation Document)
```

#### Worksheet Pages
```
/worksheets/materials        (Material Import Worksheet)
/worksheets/prices          (Price Update Worksheet)
/worksheets/inventory       (Inventory Worksheet)
```

### Component Usage by Page Type

| Component | List Pages | Card Pages | Document Pages | Worksheet Pages |
|-----------|------------|------------|----------------|-----------------|
| FilterSystem | ✅ | ❌ | ❌ | ✅ |
| StandardToolbar | ✅ | ✅ | ✅ | ✅ |
| ColumnReorderManager | ✅ | ❌ | ❌ | ✅ |
| **ColumnResizer** | ✅ | ❌ | ✅ (lines) | ✅ |
| GenericViewSwitcher | ✅ | ❌ | ❌ | ✅ |
| GenericCard/Grid/List | ✅ | ❌ | ❌ | ✅ |
| SaveViewPreferences | ✅ | ❌ | ❌ | ✅ |
| Form Validation | ❌ | ✅ | ✅ | ✅ |
| DataTable | ✅ | ❌ | ✅ (lines) | ✅ |
| StandardListPage | ✅ | ❌ | ❌ | ❌ |
| Tab Navigation | ❌ | ✅ | ✅ | ❌ |
| Workflow Controls | ❌ | ❌ | ✅ | ✅ |

### State Management by Page Type

#### List Pages
- Current view mode (table/grid/list)
- Selected items
- Filter criteria
- Sort settings
- Pagination state
- **Column widths** (persisted in saved views)
- **Column freeze positions** (left/right/none)
- **Column order** (reorderable columns)

#### Card Pages  
- Form data
- Validation state
- Dirty tracking
- Tab navigation state

#### Document Pages
- Document header
- Document lines collection
- Workflow state
- Calculation results
- Approval status

#### Worksheet Pages
- Worksheet data collection
- Template selection
- Processing status
- Validation results
- **Column widths** (persisted in saved views)
- **Column freeze positions** (for spreadsheet-like interface)
- **Column order** (customizable per user)

## Migration Strategy

### Phase 1: Establish Page Types
1. Audit existing pages and categorize by type
2. Create base page components for each type
3. Define routing patterns
4. Create page templates

### Phase 2: Implement List Pages
1. Update existing list pages to use GenericViewSwitcher
2. Implement StandardToolbar with action providers
3. Create GenericDisplayConfig for each entity
4. Test all list page functionality

### Phase 3: Standardize Card Pages
1. Create standard card page layout
2. Implement form validation patterns
3. Add dirty state tracking
4. Standardize save/cancel workflows

### Phase 4: Document & Worksheet Pages
1. Create document page templates
2. Implement master-detail patterns
3. Create worksheet base functionality
4. Add workflow management

## FilterSystem Implementation Details

### Dynamic Value Detection
The FilterSystem component now includes intelligent dynamic value detection that automatically extracts unique values from the actual data on the page:

#### How It Works
1. **Define Filters Without Options**: Pages using IFilterProvider can define filters without hardcoded options
2. **Automatic Detection**: FilterSystem uses reflection to extract unique values from the Items collection
3. **Cached Results**: Detected values are cached for performance
4. **Dropdown Generation**: Automatically shows dropdowns with actual values when users select a filter field

#### Implementation Example
```csharp
// In your page implementing IFilterProvider<T>
public List<FilterDefinition> GetAvailableFilters()
{
    return new List<FilterDefinition>
    {
        new() 
        { 
            PropertyName = "Category", 
            DisplayName = "Category", 
            Type = FilterType.Select
            // No Options provided - FilterSystem will detect from data
        },
        new() 
        { 
            PropertyName = "CustomerName", 
            DisplayName = "Customer", 
            Type = FilterType.Text
            // Will show dropdown with actual customer names
        },
        new() 
        { 
            PropertyName = "Price", 
            DisplayName = "Price", 
            Type = FilterType.Number,
            Placeholder = "Enter price..." // Manual entry for numbers
        }
    };
}
```

### Filter Predicate Building
Filters now use a `field_operator` format for more precise filtering:

```csharp
public Func<T, bool> BuildFilterPredicate(Dictionary<string, object> filters)
{
    return item =>
    {
        foreach (var filter in filters)
        {
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
                    _ => true
                },
                // Additional fields...
                _ => true
            };
            
            if (!result) return false;
        }
        return true;
    };
}
```

### GenericViewSwitcher Integration
Both List and Worksheet pages now support multiple view modes through GenericViewSwitcher:

#### Configuration Example
```csharp
// Define display configuration
private static readonly GenericDisplayConfig ItemDisplayConfig = new()
{
    PrimaryProperty = nameof(Item.Name),
    SecondaryProperty = nameof(Item.Description),
    StatusProperty = nameof(Item.Status),
    DefaultIcon = "fas fa-cube",
    Stats = new List<StatMapping>
    {
        new() { PropertyName = nameof(Item.Price), Label = "Price", Format = "C" },
        new() { PropertyName = nameof(Item.Quantity), Label = "Qty" }
    }
};

// Use in page
<GenericViewSwitcher 
    @bind-CurrentView="viewMode"
    ItemCount="@filteredItems.Count()"
    TableTemplate="@tableTemplate"
    CardTemplate="@cardTemplate"
    ListTemplate="@listTemplate" />
```

### Key Benefits
- **No Hardcoded Options**: Filter values automatically adapt to actual data
- **Consistent Experience**: Same filtering interface across all applicable page types
- **Performance Optimized**: Caching prevents redundant value extraction
- **Type Safety**: Generic implementation maintains compile-time type checking
- **User-Friendly**: Shows only relevant filter values that exist in the current dataset

This architecture ensures consistent user experience while providing appropriate functionality for each page type's specific purpose.

## Implementation Examples from Working Samples

### List Page Sample (/samples/list)

**Complete Interface Implementation Pattern**:
```csharp
@page "/samples/list"
@using SteelEstimation.Core.Interfaces
@using SteelEstimation.Core.Models
@using SteelEstimation.Web.Shared.Components
@using System.Text.Json
@implements IToolbarActionProvider
@implements IFilterProvider<ListPageSample.Product>
@implements ISaveableViewState

public class ListPageSample : IToolbarActionProvider, IFilterProvider<Product>, ISaveableViewState
{
    // IToolbarActionProvider implementation
    public ToolbarActionGroup GetActions() => new()
    {
        PrimaryActions = new List<ToolbarAction>
        {
            new() { Text = "Add Product", Icon = "fas fa-plus", Action = AddProduct, Style = ToolbarActionStyle.Primary },
            new() { Text = "Import", Icon = "fas fa-upload", Action = ImportProducts }
        },
        MenuActions = new List<ToolbarAction>
        {
            new() { Text = "Export", Icon = "fas fa-download", Action = ExportProducts },
            new() { Text = "Bulk Edit", Icon = "fas fa-edit", Action = BulkEdit, RequiresSelection = true }
        }
    };

    // IFilterProvider implementation
    public List<FilterDefinition> GetAvailableFilters() => new()
    {
        new() { PropertyName = "Category", DisplayName = "Category", Type = FilterType.Select },
        new() { PropertyName = "Status", DisplayName = "Status", Type = FilterType.Select },
        new() { PropertyName = "Price", DisplayName = "Price", Type = FilterType.Number }
    };

    public Func<Product, bool> BuildFilterPredicate(Dictionary<string, object> filters)
    {
        return product => {
            // Filter implementation logic
            return true; // Simplified
        };
    }

    // ISaveableViewState implementation with column management
    public string GetPageIdentifier() => "list-page-sample";
    
    public string GetCurrentViewState()
    {
        var viewState = new ViewState
        {
            ViewMode = viewMode,
            ActiveFilters = activeFilters,
            SelectedItemIds = selectedRows.Select(p => p.Id.ToString()).ToList(),
            // Include column configuration for persistence
            ColumnConfig = columnDefinitions.ExtractConfiguration()
        };
        return viewState.SerializeViewState();
    }

    public async Task ApplyViewState(string viewState)
    {
        var state = ViewStateExtensions.DeserializeViewState(viewState);
        if (state != null)
        {
            viewMode = state.ViewMode;
            activeFilters = state.ActiveFilters;
            
            // Apply saved column configuration (widths, order, freeze)
            state.ApplyColumnConfiguration(columnDefinitions);
            
            StateHasChanged();
        }
    }

    public bool HasUnsavedChanges => GetCurrentViewState() != _lastSavedState;
    public event EventHandler? ViewStateChanged;
}
```

### Worksheet Page Sample (/samples/worksheet)

**Worksheet-Specific Implementation Pattern**:
```csharp
@page "/samples/worksheet"
@implements IToolbarActionProvider
@implements IFilterProvider<WorksheetPageSample.WorksheetItem>
@implements ISaveableViewState

public class WorksheetPageSample : IToolbarActionProvider, IFilterProvider<WorksheetItem>, ISaveableViewState
{
    // Worksheet-specific state
    private HashSet<WorksheetItem> unsavedChanges = new();
    private HashSet<WorksheetItem> selectedRows = new();
    
    // IToolbarActionProvider - Worksheet actions
    public ToolbarActionGroup GetActions() => new()
    {
        PrimaryActions = new List<ToolbarAction>
        {
            new() { Text = "Process", Icon = "fas fa-play", Action = ProcessWorksheet, Style = ToolbarActionStyle.Primary },
            new() { Text = "Import", Icon = "fas fa-upload", Action = ImportData }
        },
        MenuActions = new List<ToolbarAction>
        {
            new() { Text = "Export Template", Icon = "fas fa-file-download", Action = ExportTemplate },
            new() { Text = "Clear All", Icon = "fas fa-trash", Action = ClearWorksheet },
            new() { Text = "Validate", Icon = "fas fa-check-circle", Action = ValidateData }
        }
    };

    // Worksheet-specific methods
    private async Task ProcessWorksheet()
    {
        // Batch processing logic
    }

    private async Task ApplyChanges()
    {
        // Apply unsaved changes to database
        unsavedChanges.Clear();
        StateHasChanged();
    }
}
```

### GenericViewSwitcher Integration Pattern

**Standardized Template Definition with Column Management**:
```csharp
// Define resizable, freezable columns
private List<ColumnDefinition<Product>> columnDefinitions = new()
{
    new() { Key = "Code", Header = "Code", IsResizable = true, Width = 120, MinWidth = 80, MaxWidth = 200 },
    new() { Key = "Name", Header = "Name", IsResizable = true, Width = 250, IsFrozen = true, FreezePosition = FreezePosition.Left },
    new() { Key = "Category", Header = "Category", IsResizable = true, Width = 150 },
    new() { Key = "Price", Header = "Price", IsResizable = true, Width = 100, Type = ColumnType.Currency },
    new() { Key = "Stock", Header = "Stock", IsResizable = true, Width = 80, Type = ColumnType.Number }
};

// Handle column resize events
private async Task OnColumnResized(ColumnResizedEventArgs args)
{
    var column = columnDefinitions.FirstOrDefault(c => c.Key == args.ColumnKey);
    if (column != null)
    {
        column.Width = args.NewWidth;
        TriggerViewStateChanged(); // Save to view state
    }
}

private RenderFragment tableTemplate => @<DataTable TItem="Product" 
                                           Items="@filteredProducts"
                                           ColumnDefinitions="@columnDefinitions"
                                           ShowSelection="true"
                                           SelectedItems="@selectedRows"
                                           OnSelectionChanged="@HandleSelectionChanged"
                                           OnColumnResized="@OnColumnResized" />;

private RenderFragment cardTemplate => @<GenericCardView TItem="Product"
                                         Items="@filteredProducts"
                                         DisplayConfig="@ProductDisplayConfig"
                                         OnItemClick="@ViewProduct" />;

private RenderFragment listTemplate => @<GenericListView TItem="Product"
                                         Items="@filteredProducts"
                                         DisplayConfig="@ProductDisplayConfig"
                                         ShowSelection="true"
                                         SelectedItems="@selectedRows"
                                         OnSelectionChanged="@HandleSelectionChanged" />;
```

### GenericDisplayConfig Pattern

**Entity Display Configuration**:
```csharp
private static readonly GenericDisplayConfig ProductDisplayConfig = new()
{
    PrimaryProperty = nameof(Product.Name),
    SecondaryProperty = nameof(Product.Description),
    StatusProperty = nameof(Product.Status),
    DefaultIcon = "fas fa-cube",
    Stats = new List<StatMapping>
    {
        new() { PropertyName = nameof(Product.Price), Label = "Price", Format = "C" },
        new() { PropertyName = nameof(Product.StockLevel), Label = "Stock" },
        new() { PropertyName = nameof(Product.Category), Label = "Category" }
    }
};
```

## Testing and Validation

### Implementation Checklist

**For List Pages**:
- [ ] Implements IToolbarActionProvider with appropriate actions
- [ ] Implements IFilterProvider<T> with dynamic value detection  
- [ ] Implements ISaveableViewState with proper serialization
- [ ] Uses @bind-CurrentView pattern with GenericViewSwitcher
- [ ] Uses correct parameters for GenericTableView/CardView/ListView
- [ ] Includes FilterSystem with proper TItem binding
- [ ] Includes StandardToolbar with ActionProvider binding
- [ ] **Column Management**:
  - [ ] Define ColumnDefinition<T> list with resizable/freezable properties
  - [ ] Handle OnColumnResized event and update view state
  - [ ] Include column configuration in GetCurrentViewState()
  - [ ] Apply column configuration in ApplyViewState()
  - [ ] Test column resize with drag operation
  - [ ] Verify saved views restore column widths

**For Worksheet Pages**:
- [ ] All List Page requirements plus:
- [ ] Worksheet-specific info bar with status indicators
- [ ] Batch processing capabilities
- [ ] Unsaved changes tracking and display
- [ ] Process/Apply actions in toolbar
- [ ] **Column Management for Spreadsheet Interface**:
  - [ ] Enable column resizing for data entry columns
  - [ ] Support frozen columns for key identifiers
  - [ ] Maintain column state across worksheet operations

### Common Implementation Mistakes

1. **Invalid Component Parameters**: Using non-existent parameters like ShowActions
2. **Incorrect Binding Pattern**: Using separate CurrentView/CurrentViewChanged instead of @bind
3. **Missing Interface Implementation**: Not implementing required interfaces
4. **Parameter Validation Errors**: Component parameters don't match actual component definitions

### Debugging Steps

1. Check browser console for Blazor circuit errors
2. Verify all component parameters exist using API reference
3. Ensure @bind-CurrentView pattern is used consistently
4. Test all three view modes (table/card/list)
5. Rebuild Docker container for Razor changes

## JavaScript Interop Architecture

### Two-Layer JavaScript Pattern
The column resizer uses a two-layer JavaScript architecture for better maintainability:

#### Layer 1: Core Functionality (column-resizer.js)
```javascript
window.ColumnResizer = (function() {
    // Private state
    var resizeState = { /* ... */ };
    
    // Public API
    return {
        initialize: function(dotNetRef, tableElement) { /* ... */ },
        startResize: function(columnKey, startX, startWidth, isTouch) { /* ... */ },
        applyColumnWidths: function(columnWidths) { /* ... */ },
        cleanup: function() { /* ... */ }
    };
})();
```

#### Layer 2: Blazor Interop Bridge (column-resizer-interop.js)
```javascript
window.ColumnResizerInterop = {
    initialize: function(dotNetRef, tableElement) {
        if (window.ColumnResizer) {
            return window.ColumnResizer.initialize(dotNetRef, tableElement);
        }
        return false;
    },
    // Additional bridge methods...
};
```

### Docker Development Considerations

**IMPORTANT**: JavaScript changes require special handling in Docker:

| Change Type | Required Action | Reason |
|------------|-----------------|---------|
| JavaScript files | Full rebuild (`docker-compose down && docker-compose up -d --build`) | No volume mounts for wwwroot |
| Razor components | Full rebuild | C# code compiles to DLLs |
| CSS in .razor.css files | Full rebuild | CSS isolation requires compilation |
| appsettings.json | Container restart | Configuration loaded at startup |

**Quick Commands**:
```bash
# For JavaScript changes
docker-compose down && docker-compose up -d --build

# Check if rebuild is needed
./check-changes.ps1

# Full rebuild with cache clear
./rebuild.ps1
```

### Component Integration Pattern

The ColumnResizer is integrated through DataTable, not directly in pages:
```razor
<!-- In DataTable.razor -->
@if (ColumnDefinitions != null)
{
    <ColumnResizer TItem="TItem" 
                   @ref="columnResizer"
                   Columns="@ColumnDefinitions" 
                   TableElement="@tableElement"
                   OnColumnResized="@HandleColumnResized" />
}
```

This ensures:
- **Separation of Concerns**: Pages don't need to know about resize implementation
- **Reusability**: Any component using DataTable gets resizing automatically
- **Optional Feature**: Works only when ColumnDefinitions are provided

This comprehensive architecture provides a solid foundation for implementing consistent, maintainable page types across the SteelEstimation application.