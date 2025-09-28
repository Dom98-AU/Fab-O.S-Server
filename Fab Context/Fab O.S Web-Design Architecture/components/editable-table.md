# EditableTable Component

## Overview
The **EditableTable** component is a powerful, Excel-like data grid that provides inline editing capabilities with support for multiple cell types, including images with clipboard paste functionality. It's designed to work seamlessly with the existing shared components ecosystem while providing advanced worksheet features.

## Key Features

### Cell Type Support
- **Text**: Standard text input with validation
- **Number**: Numeric input with min/max, decimal places
- **Date/DateTime**: Date and time pickers
- **Select/MultiSelect**: Dropdown selection
- **Checkbox**: Boolean values
- **Image**: Image upload with clipboard paste support
- **Computed**: Calculated/readonly values
- **Action**: Custom buttons/actions
- **Custom**: Fully customizable cells

### Image Handling
- Display image thumbnails in cells (40x40px default)
- Click to preview full-size images
- Paste images from clipboard (Ctrl+V)
- Upload multiple images per cell
- Delete images with confirmation
- Support for base64, URLs, and ImageUpload entities

### Excel-like Features
- Keyboard navigation (Arrow keys, Tab, Enter, F2)
- Auto-save with configurable delay
- Row selection with checkboxes
- Row numbers display
- Add/delete rows dynamically
- Drag-and-drop row reordering
- CSV import/export
- Validation with error messages

### Integration Features
- Works with GenericViewSwitcher
- Compatible with ColumnReorderManager
- Supports ColumnResizer
- Integrates with ViewStateProvider
- Template-driven column configuration

## Component Structure

```
EditableTable/
├── EditableTable.razor          # Main table component
├── EditableTable.razor.css      # Isolated styles
├── EditableCell.razor          # Individual cell component
├── CellTemplate.cs             # Column/cell configuration model
├── editable-table.js           # JavaScript support
└── clipboard-paste.js          # Enhanced clipboard handling
```

## Usage Example

### Basic Setup

```razor
@page "/worksheet"
@using SteelEstimation.Core.Models
@using SteelEstimation.Core.Entities

<EditableTable TItem="WorksheetItem"
               Items="@items"
               Config="@tableConfig"
               OnRowSaved="@HandleRowSaved"
               OnRowDeleted="@HandleRowDeleted" />

@code {
    private List<WorksheetItem> items = new();
    private EditableTableConfig<WorksheetItem> tableConfig = null!;
    
    protected override void OnInitialized()
    {
        tableConfig = new EditableTableConfig<WorksheetItem>
        {
            ShowRowNumbers = true,
            ShowSelectionColumn = true,
            AllowAddRows = true,
            AllowDeleteRows = true,
            AutoSave = true,
            AutoSaveDelayMs = 1000,
            EnableKeyboardNavigation = true,
            EnableClipboardPaste = true,
            Columns = CreateColumns()
        };
    }
}
```

### Column Configuration

```csharp
private List<CellTemplate<WorksheetItem>> CreateColumns()
{
    return new List<CellTemplate<WorksheetItem>>
    {
        // Text column
        new()
        {
            Key = "Name",
            DisplayName = "Product Name",
            Type = CellType.Text,
            IsEditable = true,
            Width = "250px",
            GetValue = i => i.Name,
            SetValue = (i, v) => i.Name = v?.ToString() ?? "",
            Validate = (i, v) => string.IsNullOrEmpty(v?.ToString()) 
                ? "Name is required" : null
        },
        
        // Number column with validation
        new()
        {
            Key = "Price",
            DisplayName = "Price",
            Type = CellType.Number,
            IsEditable = true,
            DecimalPlaces = 2,
            MinValue = 0,
            MaxValue = 999999,
            GetValue = i => i.Price,
            SetValue = (i, v) => i.Price = Convert.ToDecimal(v),
            Validate = (i, v) =>
            {
                var price = Convert.ToDecimal(v);
                if (price < 0) return "Price cannot be negative";
                if (price > 999999) return "Price exceeds maximum";
                return null;
            }
        },
        
        // Dropdown selection
        new()
        {
            Key = "Category",
            DisplayName = "Category",
            Type = CellType.Select,
            Options = new List<SelectOption>
            {
                new() { Value = "steel", Display = "Steel" },
                new() { Value = "aluminum", Display = "Aluminum" },
                new() { Value = "copper", Display = "Copper" }
            },
            GetValue = i => i.Category,
            SetValue = (i, v) => i.Category = v?.ToString() ?? ""
        },
        
        // Image column with clipboard paste
        new()
        {
            Key = "Images",
            DisplayName = "Product Images",
            Type = CellType.Image,
            IsEditable = true,
            AllowMultipleImages = true,
            MaxImages = 5,
            AcceptedImageTypes = new[] { "image/png", "image/jpeg" },
            MaxImageSize = 5 * 1024 * 1024, // 5MB
            GetValue = i => i.Images,
            SetValue = (i, v) => i.Images = v as List<ImageUpload>
        },
        
        // Computed column
        new()
        {
            Key = "Total",
            DisplayName = "Total",
            Type = CellType.Computed,
            ComputeDisplay = i => (i.Price * i.Quantity).ToString("C")
        }
    };
}
```

## Clipboard Image Paste

### How It Works
1. User clicks on image cell or presses Ctrl+V
2. Cell becomes "paste-ready" (visual indicator)
3. User pastes image from clipboard
4. Image is converted to base64
5. `HandlePastedImage` is called on the cell
6. Image is added to the item's image collection
7. Auto-save triggers if enabled

### JavaScript Integration
```javascript
// Automatic initialization when component renders
EditableTable.initializeClipboardPaste(dotNetRef);

// Handles paste events globally
document.addEventListener('paste', async function(e) {
    // Check for image data
    // Convert to base64
    // Invoke Blazor component method
});
```

## Template System Integration

The EditableTable is designed to work with template-driven systems like RoutingTemplate:

```csharp
// Generate columns from a template
private List<CellTemplate<T>> GenerateFromTemplate(RoutingTemplate template)
{
    return template.Operations.Select(op => new CellTemplate<T>
    {
        Key = op.OperationCode,
        DisplayName = op.OperationName,
        Type = DetermineType(op.OperationType),
        // Map template properties to cell configuration
    }).ToList();
}
```

## Multi-View Support

The EditableTable works seamlessly with all three view modes:

### Table View
- Traditional spreadsheet layout
- All editing features available
- Keyboard navigation enabled
- Column reordering/resizing supported

### Card View
- Each card contains editable fields
- Images displayed as thumbnails
- Touch-friendly for mobile

### List View
- Inline editing in list items
- Compact display
- Quick edit mode

## Performance Optimization

- **Virtualization**: Only renders visible rows for large datasets
- **Debounced auto-save**: Prevents excessive server calls
- **Lazy loading**: Images loaded on demand
- **Efficient change tracking**: Only modified items are saved

## Accessibility

- Full keyboard navigation
- ARIA labels and roles
- Screen reader compatible
- Focus management
- High contrast mode support

## Browser Compatibility

- Chrome/Edge: Full support
- Firefox: Full support
- Safari: Full support (no clipboard paste)
- Mobile browsers: Touch-optimized

## Configuration Options

| Option | Type | Default | Description |
|--------|------|---------|-------------|
| ShowRowNumbers | bool | false | Display row numbers |
| ShowSelectionColumn | bool | false | Enable row selection |
| AllowAddRows | bool | false | Allow adding new rows |
| AllowDeleteRows | bool | false | Allow deleting rows |
| AllowReorderRows | bool | false | Enable drag-drop reordering |
| AutoSave | bool | true | Enable auto-save |
| AutoSaveDelayMs | int | 500 | Auto-save delay in ms |
| EnableKeyboardNavigation | bool | true | Enable keyboard shortcuts |
| EnableClipboardPaste | bool | true | Enable clipboard paste |
| EmptyMessage | string | null | Message when no data |
| CssClass | string | null | Additional CSS classes |

## Events

| Event | Parameters | Description |
|-------|------------|-------------|
| OnRowSave | TItem | Fired when row is saved |
| OnRowDelete | TItem | Fired before row deletion |
| OnRowAdd | - | Fired when adding new row |
| OnRowsReorder | List<TItem> | Fired after reordering |
| OnValueChanged | TItem, column, value | Fired on cell change |
| OnImageUpload | TItem, ImageUpload | Fired on image upload |
| OnImageDelete | TItem, image | Fired on image deletion |

## Best Practices

1. **Define columns once**: Create column definitions in OnInitialized
2. **Use validation**: Add validators to prevent invalid data
3. **Handle save errors**: Implement proper error handling in OnRowSave
4. **Optimize images**: Set reasonable size limits
5. **Consider mobile**: Test on various screen sizes
6. **Use computed columns**: For calculated values instead of manual updates
7. **Enable auto-save**: Better user experience with automatic persistence

## Advanced Scenarios

### Custom Cell Rendering
```csharp
new CellTemplate<Item>
{
    Key = "Actions",
    Type = CellType.Custom,
    CustomRender = (item) => @<div>
        <button @onclick="() => Edit(item)">Edit</button>
        <button @onclick="() => Delete(item)">Delete</button>
    </div>
}
```

### Dynamic Column Generation
```csharp
var columns = dbContext.TableMetadata
    .Select(meta => new CellTemplate<dynamic>
    {
        Key = meta.ColumnName,
        DisplayName = meta.DisplayName,
        Type = MapSqlTypeToCellType(meta.DataType),
        // Dynamic configuration
    })
    .ToList();
```

### Bulk Operations
```csharp
// Apply to all selected rows
foreach (var item in selectedItems)
{
    ApplyBulkUpdate(item, bulkValue);
}
await SaveAllChanges(selectedItems);
```

## Testing Checklist

- [ ] Text editing works correctly
- [ ] Number validation enforces min/max
- [ ] Dropdown selections save properly
- [ ] Image upload via button works
- [ ] Clipboard paste for images works
- [ ] Keyboard navigation functions
- [ ] Auto-save triggers after delay
- [ ] Row add/delete operations work
- [ ] Drag-drop reordering functions
- [ ] CSV export/import works
- [ ] Mobile touch interactions work
- [ ] Accessibility features function

## Related Components

- [GenericViewSwitcher](./generic-view-switcher.md) - Multi-view container
- [DataTable](./data-table.md) - Basic table component
- [ColumnReorderManager](./column-reorder-manager.md) - Column management
- [ColumnResizer](./column-resizer.md) - Column width adjustment
- [SaveViewPreferences](./save-view-preferences.md) - View state persistence