# Save View Preferences Architecture

## Executive Summary
The Save View Preferences system provides users with the ability to save, load, and manage custom view configurations across List and Worksheet pages. It integrates seamlessly with the Generic View System to persist view states, filters, column settings, and other user preferences.

## Architecture Overview

### System Components
```
┌─────────────────────────────────────────────────────────┐
│                     Page Level                           │
│           (List/Worksheet Pages)                         │
│                                                          │
│  • Implements ISaveableViewState                        │
│  • Provides GetCurrentViewState()                       │
│  • Handles ApplyViewState()                             │
└────────────────────┬────────────────────────────────────┘
                     │
┌────────────────────▼────────────────────────────────────┐
│              SaveViewPreferences                         │
│                                                          │
│  • User interface for view management                   │
│  • Save/Load/Delete operations                          │
│  • Unsaved changes detection                            │
│  • Default view management                              │
└────────────────────┬────────────────────────────────────┘
                     │
┌────────────────────▼────────────────────────────────────┐
│             ITableViewService                            │
│                                                          │
│  • Database operations                                  │
│  • View state serialization                            │
│  • User/Company scoping                                 │
│  • Default view logic                                   │
└────────────────────┬────────────────────────────────────┘
                     │
┌────────────────────▼────────────────────────────────────┐
│             SavedTableView                               │
│            (Database Entity)                             │
│                                                          │
│  • View metadata storage                                │
│  • JSON view state persistence                         │
│  • User/Company relationships                           │
└─────────────────────────────────────────────────────────┘
```

## Core Concepts

### 1. ISaveableViewState Interface
Defines the contract for pages that support view state persistence:
```csharp
public interface ISaveableViewState
{
    string GetPageIdentifier();
    string GetCurrentViewState();
    Task ApplyViewState(string viewState);
    event EventHandler? ViewStateChanged;
    bool HasUnsavedChanges { get; }
}
```

### 2. View State Serialization
View states are stored as JSON strings containing:
- Current view mode (table/card/list)
- Filter configurations
- Sort settings
- Column configurations
  - Column order (display sequence)
  - Column visibility (shown/hidden)
  - **Column freeze positions** (Left/None/Right)
  - Column widths (pixel values)
- Selection states
- Page-specific settings

### 3. User Scoping
Views are scoped by:
- **User ID**: Personal views per user
- **Company ID**: Company-wide view sharing
- **Page Identifier**: Unique per page/component

## Component Details

### SaveViewPreferences Component
**Purpose**: Provides UI for managing saved views
**Location**: `/SteelEstimation.Web/Shared/Components/SaveViewPreferences.razor`

**Key Features**:
- Dropdown interface for view selection
- Save current view dialog
- Update existing views
- Delete view management
- Default view settings
- Unsaved changes indicators
- Reset to original functionality

**Parameters**:
```csharp
[Parameter, EditorRequired] public ISaveableViewState? ViewStateProvider { get; set; }
[Parameter] public EventCallback<SavedTableView> OnViewLoaded { get; set; }
[Parameter] public string? Theme { get; set; } = "default";
```

### SaveViewDialog Component
**Purpose**: Modal dialog for saving new views
**Key Features**:
- View name input
- Set as default option
- Share with team option
- Validation and error handling

### ITableViewService Interface
**Purpose**: Service layer for view persistence
**Key Operations**:
```csharp
Task<List<SavedTableView>> GetUserViewsAsync(int userId, int companyId, string pageId);
Task<SaveViewResult> SaveViewAsync(int userId, int companyId, SaveViewStateRequest request);
Task<SaveViewResult> UpdateViewAsync(int viewId, SaveViewStateRequest request);
Task<bool> SetDefaultViewAsync(int viewId, int userId, int companyId);
Task<bool> DeleteViewAsync(int viewId, int userId);
Task UpdateLastUsedAsync(int viewId);
Task<SavedTableView?> GetDefaultViewAsync(int userId, int companyId, string pageId);
```

## Data Model

### SavedTableView Entity
```csharp
public class SavedTableView
{
    public int Id { get; set; }
    public string ViewName { get; set; } = "";
    public string PageIdentifier { get; set; } = "";
    public string ViewState { get; set; } = "";
    public int UserId { get; set; }
    public int CompanyId { get; set; }
    public bool IsDefault { get; set; }
    public bool IsShared { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime LastUsedAt { get; set; }
    
    // Navigation properties
    public User User { get; set; } = null!;
    public Company Company { get; set; } = null!;
}
```

### SaveViewStateRequest
```csharp
public class SaveViewStateRequest
{
    public string ViewName { get; set; } = "";
    public string PageIdentifier { get; set; } = "";
    public string ViewState { get; set; } = "";
    public bool SetAsDefault { get; set; }
    public bool ShareWithTeam { get; set; }
}
```

### SaveViewResult
```csharp
public class SaveViewResult
{
    public bool Success { get; set; }
    public string Message { get; set; } = "";
    public SavedTableView? SavedView { get; set; }
}
```

## Integration Pattern

### 1. Page Implementation
Pages implement ISaveableViewState interface:

```csharp
@page "/example-list"
@implements ISaveableViewState
@inject ITableViewService ViewService

public class ExampleListPage : ISaveableViewState
{
    // State variables
    private string viewMode = "table";
    private Dictionary<string, object> activeFilters = new();
    private string lastSavedState = "";
    
    // ISaveableViewState implementation
    public string GetPageIdentifier() => "example-list";
    
    public string GetCurrentViewState()
    {
        return JsonSerializer.Serialize(new
        {
            ViewMode = viewMode,
            Filters = activeFilters,
            // ... other state
        });
    }
    
    public async Task ApplyViewState(string viewState)
    {
        try
        {
            var state = JsonSerializer.Deserialize<ViewState>(viewState);
            viewMode = state.ViewMode;
            activeFilters = state.Filters;
            
            // Reset frozen column states before applying saved configuration
            foreach (var column in columnDefinitions)
            {
                column.IsFrozen = false;
                column.FreezePosition = FreezePosition.None;
            }
            
            // Apply column configuration including frozen states
            state.ApplyColumnConfiguration(columnDefinitions);
            
            // Refresh frozen column visual styles if needed
            if (viewSwitcher != null)
            {
                await viewSwitcher.RefreshFrozenColumns();
            }
            
            StateHasChanged();
        }
        catch (Exception ex)
        {
            // Handle deserialization errors
        }
    }
    
    public bool HasUnsavedChanges => 
        GetCurrentViewState() != lastSavedState;
        
    public event EventHandler? ViewStateChanged;
}
```

### 2. Component Usage
Add SaveViewPreferences to page layout:

```razor
<SaveViewPreferences 
    ViewStateProvider="this"
    OnViewLoaded="@HandleViewLoaded"
    Theme="default" />
```

### 3. State Change Detection
Trigger change events when user modifies view:

```csharp
private void OnFilterChanged()
{
    ViewStateChanged?.Invoke(this, EventArgs.Empty);
    StateHasChanged();
}

private void OnViewModeChanged(string newMode)
{
    viewMode = newMode;
    ViewStateChanged?.Invoke(this, EventArgs.Empty);
    StateHasChanged();
}
```

## User Experience Flow

### Saving a View
```
1. User customizes page (filters, view mode, etc.)
2. Unsaved indicator appears in SaveViewPreferences dropdown
3. User clicks "Save Current View"
4. SaveViewDialog appears with options:
   - View name input
   - Set as default checkbox
   - Share with team checkbox
5. User confirms save
6. View is persisted to database
7. Dropdown updates with new view
8. Success notification shows
```

### Loading a View
```
1. User opens SaveViewPreferences dropdown
2. Available views are listed with metadata:
   - View name
   - Default indicator
   - Shared indicator
3. User clicks on desired view
4. View state is applied to page
5. Page updates with saved configuration
6. Success notification shows
7. View is marked as "last used"
```

### Default View Behavior
```
1. User can set any saved view as default
2. Default view is automatically loaded on page initialization
3. Only one default view per page per user
4. Default view indicator shows in dropdown
5. "Load Default View" option available when not current
```

## Database Schema

### SavedTableViews Table
```sql
CREATE TABLE [dbo].[SavedTableViews] (
    [Id] int IDENTITY(1,1) PRIMARY KEY,
    [ViewName] nvarchar(100) NOT NULL,
    [PageIdentifier] nvarchar(100) NOT NULL,
    [ViewState] nvarchar(max) NOT NULL,
    [UserId] int NOT NULL,
    [CompanyId] int NOT NULL,
    [IsDefault] bit NOT NULL DEFAULT 0,
    [IsShared] bit NOT NULL DEFAULT 0,
    [CreatedAt] datetime2 NOT NULL DEFAULT GETUTCDATE(),
    [LastUsedAt] datetime2 NOT NULL DEFAULT GETUTCDATE(),
    
    CONSTRAINT [FK_SavedTableViews_Users] 
        FOREIGN KEY ([UserId]) REFERENCES [Users]([Id]),
    CONSTRAINT [FK_SavedTableViews_Companies] 
        FOREIGN KEY ([CompanyId]) REFERENCES [Companies]([Id])
);

-- Indexes
CREATE INDEX [IX_SavedTableViews_User_Page] 
    ON [SavedTableViews] ([UserId], [CompanyId], [PageIdentifier]);
    
CREATE INDEX [IX_SavedTableViews_LastUsed] 
    ON [SavedTableViews] ([LastUsedAt] DESC);
```

## Security Considerations

### Access Control
- Users can only access their own views
- Shared views require same company membership
- Admin users can manage company-wide views
- View state validation prevents code injection

### Data Protection
- View states are validated before application
- JSON deserialization is protected with try-catch
- Database queries use parameterized statements
- User input is sanitized for view names

## Performance Optimizations

### Caching Strategy
- User views cached in memory per session
- Default view cached separately
- Cache invalidation on view modifications
- Lazy loading of view states

### Database Optimization
- Indexed queries by user/company/page
- JSON column optimization for view state
- Pagination for large view collections
- Cleanup of unused/old views

## Error Handling

### View State Application Errors
```csharp
try
{
    await ViewStateProvider.ApplyViewState(view.ViewState);
    // Success notification
}
catch (JsonException ex)
{
    await JSRuntime.InvokeVoidAsync("showToast", 
        "Invalid view format", "error");
}
catch (Exception ex)
{
    await JSRuntime.InvokeVoidAsync("showToast", 
        $"Failed to load view: {ex.Message}", "error");
}
```

### Save Operation Errors
- Duplicate view name validation
- View state size limits
- Database connection failures
- Permission validation

## Visual Design

### SaveViewPreferences Dropdown
- **Bookmark icon**: Indicates saved views functionality
- **Unsaved indicator**: Orange dot when changes detected  
- **Current view name**: Shows active view in parentheses
- **View actions**: Star (default), Delete icons
- **Shared indicator**: Users icon for team views
- **Default badge**: Blue badge for default views

### Dropdown Menu Structure
```
┌─ Saved Views Header ─────────────┐
├─ View 1 (Default) ⭐️            │
├─ View 2 (Shared) 👥              │
├─ View 3                          │
├─ ─────────────────────────────── │
├─ 💾 Save Current View           │
├─ 🔄 Update "Current View"       │
├─ ─────────────────────────────── │
├─ ↩️ Reset to Original           │
└─ 🏠 Load Default View           │
```

## Integration with Generic View System

### Coordinated State Management
- SaveViewPreferences tracks Generic View System state
- View mode changes trigger unsaved indicators
- Filter changes are captured in view state
- Column configurations are persisted

### Template Integration
```razor
<!-- Standard pattern for List/Worksheet pages -->
<div class="page-header">
    <StandardToolbar ActionProvider="this" />
    <div class="toolbar-row">
        <FilterSystem @bind-Filters="activeFilters" 
                     FilterProvider="this"
                     OnFiltersChanged="@OnFilterChanged" />
        <SaveViewPreferences ViewStateProvider="this" 
                           OnViewLoaded="@HandleViewLoaded" />
    </div>
</div>

<GenericViewSwitcher @bind-CurrentView="viewMode"
                    OnCurrentViewChanged="@OnViewModeChanged"
                    ItemCount="@filteredItems.Count()"
                    TableTemplate="@tableTemplate"
                    CardTemplate="@cardTemplate"
                    ListTemplate="@listTemplate" />
```

## Future Enhancements

### Planned Features
- **Import/Export Views**: Share views between users
- **View Templates**: Pre-built views for common scenarios  
- **View History**: Track and restore previous versions
- **Advanced Permissions**: Role-based view sharing
- **View Analytics**: Track most used views and patterns

### Extension Points
- **Custom View Metadata**: Additional view properties
- **View Validation**: Custom validation rules
- **View Transformation**: Migration between view formats
- **External Storage**: Cloud-based view persistence
- **View Collaboration**: Real-time shared view editing

## Troubleshooting

### Common Issues

#### Views Not Loading
**Symptoms**: Saved views appear but don't apply
**Solutions**:
1. Check ISaveableViewState implementation
2. Verify JSON serialization format
3. Check browser console for deserialization errors
4. Validate ViewStateProvider parameter

#### Unsaved Changes Not Detected  
**Symptoms**: Orange indicator doesn't appear
**Solutions**:
1. Ensure ViewStateChanged events are triggered
2. Check HasUnsavedChanges implementation
3. Verify state comparison logic

#### Default View Not Loading
**Symptoms**: Default view doesn't load on page init
**Solutions**:
1. Check OnInitializedAsync implementation
2. Verify default view exists in database
3. Check user permissions and company scoping

#### Frozen Columns Not Persisting in Saved Views
**Symptoms**: Frozen column state lost when loading saved views
**Solutions**:
1. Implement deep copy pattern in OnColumnsChanged handler
2. Add explicit frozen state reset before applying saved state
3. Ensure ColumnDefinitions are used as single source of truth
4. Call RefreshFrozenColumns() after loading view state

**Code Example**:
```csharp
// Deep copy to prevent reference pollution
private async Task OnColumnsChanged(List<ColumnDefinition<T>> columns)
{
    columnDefinitions = columns.Select(c => new ColumnDefinition<T>
    {
        Key = c.Key,
        IsFrozen = c.IsFrozen,
        FreezePosition = c.FreezePosition,
        // ... copy all properties
    }).ToList();
}
```

#### Reference Pollution Between View Switches
**Symptoms**: Previous view's frozen state appears in newly loaded view
**Solutions**:
1. Never assign column lists by reference
2. Always create new instances when updating columnDefinitions
3. Reset state explicitly before applying new configuration

### Best Practices
1. **Implement proper state comparison** for unsaved changes detection
2. **Handle JSON serialization errors** gracefully
3. **Trigger ViewStateChanged events** on all state modifications
4. **Use meaningful view names** for better user experience
5. **Clean up old/unused views** periodically

## Conclusion

The Save View Preferences system provides a comprehensive solution for persisting user customizations across the SteelEstimation application. By integrating seamlessly with the Generic View System and following established patterns, it enables users to maintain personalized workflows while supporting team collaboration through shared views.