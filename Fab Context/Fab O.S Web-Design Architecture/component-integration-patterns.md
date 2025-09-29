# Component Integration Patterns - Fab.OS

## Executive Summary
The Component Integration Patterns document defines the comprehensive integration strategies, communication patterns, and architectural guidelines for connecting all view management components in Fab.OS. This includes StandardToolbar coordination, async EventCallback patterns, state synchronization, error handling boundaries, and cross-component communication protocols that ensure seamless operation of the unified view management system.

## Architecture Overview

### Integration Layers
```
┌─────────────────────────────────────────────────────────────────────────┐
│                         Page Orchestration Layer                         │
│                      (PackagesList.razor, etc.)                          │
│                                                                           │
│  • StandardToolbar coordination and RenderFragment management            │
│  • Unified state management with ViewState model                         │
│  • Cross-component event handling and async coordination                 │
│  • Error boundary implementation and graceful degradation                │
└───────────────────────────┬─────────────────────────────────────────────┘
                            │
┌───────────────────────────▼─────────────────────────────────────────────┐
│                    Component Communication Layer                         │
│               (EventCallback and State Propagation)                      │
│                                                                           │
│  • Async EventCallback<T> patterns for type-safe communication          │
│  • State change propagation with debouncing and error handling           │
│  • Component lifecycle coordination (OnAfterRender, OnParametersSet)     │
│  • JavaScript interop coordination and safety patterns                   │
└───────────────────────────┬─────────────────────────────────────────────┘
                            │
┌───────────────────────────▼─────────────────────────────────────────────┐
│                     Component Integration Layer                          │
│           (Individual component async patterns and interfaces)           │
│                                                                           │
│  ┌─────────────────┐ ┌─────────────────┐ ┌─────────────────┐ ┌─────────┐ │
│  │ GenericView     │ │ ColumnManager   │ │ FilterDialog    │ │ View    │ │
│  │ Switcher        │ │ Dropdown        │ │                 │ │ Saving  │ │
│  │                 │ │                 │ │                 │ │ Dropdown│ │
│  │ • Type-safe     │ │ • Async column  │ │ • Reflection-   │ │ • State │ │
│  │   view events   │ │   operations    │ │   based fields  │ │   mgmt  │ │
│  │ • State sync    │ │ • JS integration│ │ • Type-safe     │ │ • Async │ │
│  └─────────────────┘ └─────────────────┘ │   filtering     │ │   ops   │ │
│                                          └─────────────────┘ └─────────┘ │
└─────────────────────────────────────────────────────────────────────────┘
```

## Core Integration Patterns

### 1. StandardToolbar Integration Pattern
The foundation pattern for all list page implementations:

**Recent Fixes Applied:**
- Fixed namespace imports for `TableColumn<T>` in GenericTableView
- Corrected type bindings in FilterDialog (changed from `object?` to `string`)
- Added missing enum definitions (FilterOperator, LogicalOperator)
- Verified all component dependencies exist

```razor
<!-- Universal StandardToolbar Integration Pattern -->
@page "/[entity-name]"
@rendermode InteractiveServer
@using FabOS.WebServer.Components.Shared
@using FabOS.WebServer.Models.Entities
@using FabOS.WebServer.Models.Columns
@using FabOS.WebServer.Models.Filtering
@using FabOS.WebServer.Models.ViewState

<PageTitle>[Entity Name] - Fab.OS</PageTitle>

<!-- Standardized Toolbar Integration -->
<StandardToolbar ActionProvider="@this"
                OnSearch="@OnSearchChanged"
                SearchPlaceholder="Search [entities]..."
                PageType="PageType.List"
                PageTitle="[Entity Name]"
                PageIcon="[entity-icon-class]">

    <!-- View Switcher Section -->
    <ViewSwitcher>
        <GenericViewSwitcher TItem="[EntityType]"
                           CurrentView="@currentView"
                           CurrentViewChanged="@OnViewChanged"
                           ShowViewPreferences="false" />
    </ViewSwitcher>

    <!-- Column Manager Section -->
    <ColumnManager>
        <ColumnManagerDropdown Columns="@columnDefinitions"
                             OnColumnsChanged="@(async (columns) => await OnColumnsChanged(columns))" />
    </ColumnManager>

    <!-- Filter Section -->
    <FilterButton>
        <FilterDialog TItem="[EntityType]"
                    OnFiltersChanged="@OnFiltersChanged" />
    </FilterButton>

    <!-- View Saving Section -->
    <ViewSaving>
        <ViewSavingDropdown EntityType="[EntityTypeName]"
                          CurrentState="@currentViewState"
                          OnViewLoaded="@(async (state) => await OnViewLoaded(state))"
                          HasUnsavedChanges="@hasUnsavedChanges" />
    </ViewSaving>

</StandardToolbar>

<!-- Content Implementation -->
<div class="[entity]-container">
    @if (isLoading)
    {
        <div class="loading-container">
            <div class="loading-spinner"></div>
            <p>Loading [entities]...</p>
        </div>
    }
    else
    {
        <!-- View Implementations using standard patterns... -->
    }
</div>
```

### 2. Async EventCallback Communication Pattern
Type-safe, async communication between components:

```csharp
// Page-level EventCallback coordination
@code {
    // State management properties
    private ViewState currentViewState = new();
    private ViewState? lastSavedViewState;
    private bool hasUnsavedChanges = false;
    private bool isLoading = false;

    // Component state properties
    private GenericViewSwitcher<[EntityType]>.ViewType currentView = GenericViewSwitcher<[EntityType]>.ViewType.Table;
    private List<ColumnDefinition> columnDefinitions = new();
    private List<FilterRule> activeFilters = new();
    private string searchTerm = "";

    // Dependency injection
    [Inject] private IJSRuntime JSRuntime { get; set; } = default!;

    // === ASYNC EVENT HANDLERS ===

    // Column management async handler
    private async Task OnColumnsChanged(List<ColumnDefinition>? columns)
    {
        try
        {
            if (columns != null)
            {
                columnDefinitions = columns;
                await UpdateTableColumns();
                TriggerViewStateChanged();
            }
        }
        catch (Exception ex)
        {
            await HandleError("column management", ex);
        }
        finally
        {
            StateHasChanged();
        }
    }

    // Filter management handler
    private void OnFiltersChanged(List<FilterRule> filters)
    {
        try
        {
            activeFilters = filters ?? new List<FilterRule>();
            FilterEntities();
            TriggerViewStateChanged();
        }
        catch (Exception ex)
        {
            // Non-async error handling for synchronous operations
            LogError("filter management", ex);
        }
        finally
        {
            StateHasChanged();
        }
    }

    // View switching handler
    private void OnViewChanged(GenericViewSwitcher<[EntityType]>.ViewType newView)
    {
        try
        {
            currentView = newView;
            TriggerViewStateChanged();
        }
        catch (Exception ex)
        {
            LogError("view switching", ex);
        }
        finally
        {
            StateHasChanged();
        }
    }

    // Search handling
    private void OnSearchChanged(string newSearchTerm)
    {
        try
        {
            searchTerm = newSearchTerm ?? "";
            FilterEntities();
            TriggerViewStateChanged();
        }
        catch (Exception ex)
        {
            LogError("search", ex);
        }
        finally
        {
            StateHasChanged();
        }
    }

    // View state loading async handler
    private async Task OnViewLoaded(ViewState? state)
    {
        try
        {
            isLoading = true;
            StateHasChanged();

            if (state == null)
            {
                await ResetToDefaults();
            }
            else
            {
                await ApplyViewState(state);
            }

            lastSavedViewState = state?.Clone();
            hasUnsavedChanges = false;
        }
        catch (Exception ex)
        {
            await HandleError("view loading", ex);
            await ResetToDefaults();
        }
        finally
        {
            isLoading = false;
            StateHasChanged();
        }
    }

    // === ERROR HANDLING PATTERNS ===

    private async Task HandleError(string operation, Exception ex)
    {
        Console.WriteLine($"Error in {operation}: {ex.Message}");

        // Optional: Show user notification
        if (JSRuntime != null)
        {
            try
            {
                await JSRuntime.InvokeVoidAsync("showErrorNotification",
                    $"Error in {operation}", ex.Message);
            }
            catch
            {
                // Ignore notification errors
            }
        }
    }

    private void LogError(string operation, Exception ex)
    {
        Console.WriteLine($"Error in {operation}: {ex.Message}");
    }
}
```

### 3. Component State Synchronization Pattern
Comprehensive state management with change detection:

**Type Safety Improvements:**
- FilterDialog now uses proper string types for input binding
- All FilterRule objects use string values consistently
- Added FilterFieldDefinition class for type-safe field definitions

```csharp
// State management implementation pattern
@code {
    // === STATE COLLECTION ===

    private ViewState GetCurrentViewState()
    {
        try
        {
            return new ViewState
            {
                CurrentView = currentView,
                Columns = columnDefinitions.Select(c => new ColumnDefinition
                {
                    Id = c.Id,
                    PropertyName = c.PropertyName,
                    DisplayName = c.DisplayName,
                    IsVisible = c.IsVisible,
                    IsFrozen = c.IsFrozen,
                    FreezePosition = c.FreezePosition,
                    Order = c.Order,
                    Width = c.Width,
                    IsRequired = c.IsRequired,
                    DataType = c.DataType
                }).ToList(),
                Filters = activeFilters.Select(f => new FilterRule
                {
                    Id = f.Id,
                    Field = f.Field,
                    FieldName = f.FieldName,
                    Operator = f.Operator,
                    Value = f.Value,
                    SecondValue = f.SecondValue
                }).ToList(),
                SearchTerm = searchTerm ?? "",
                AdditionalState = CollectAdditionalState()
            };
        }
        catch (Exception ex)
        {
            LogError("state collection", ex);
            return new ViewState();
        }
    }

    // === STATE APPLICATION ===

    private async Task ApplyViewState(ViewState state)
    {
        // Apply current view
        if (state.CurrentView.HasValue)
        {
            currentView = state.CurrentView.Value;
        }

        // Apply column configurations with async frozen column handling
        if (state.Columns.Any())
        {
            columnDefinitions = state.Columns.Select(c => new ColumnDefinition
            {
                Id = c.Id,
                PropertyName = c.PropertyName,
                DisplayName = c.DisplayName,
                IsVisible = c.IsVisible,
                IsFrozen = c.IsFrozen,
                FreezePosition = c.FreezePosition,
                Order = c.Order,
                Width = c.Width,
                IsRequired = c.IsRequired,
                DataType = c.DataType
            }).ToList();

            await UpdateTableColumns();
        }

        // Apply filter configurations
        if (state.Filters.Any())
        {
            activeFilters = state.Filters.Select(f => new FilterRule
            {
                Id = f.Id,
                Field = f.Field,
                FieldName = f.FieldName,
                Operator = f.Operator,
                Value = f.Value,
                SecondValue = f.SecondValue
            }).ToList();

            FilterEntities();
        }

        // Apply search term
        searchTerm = state.SearchTerm ?? "";
        if (!string.IsNullOrEmpty(searchTerm))
        {
            FilterEntities();
        }

        // Apply additional state
        await ApplyAdditionalState(state.AdditionalState);
    }

    // === CHANGE DETECTION ===

    private Timer? changeDetectionTimer;

    private void TriggerViewStateChanged()
    {
        // Debounce change detection to prevent excessive operations
        changeDetectionTimer?.Dispose();
        changeDetectionTimer = new Timer(async (_) =>
        {
            try
            {
                await InvokeAsync(() =>
                {
                    var currentState = GetCurrentViewState();
                    var lastSavedJson = lastSavedViewState?.ToJson() ?? "";
                    var currentJson = currentState.ToJson();

                    hasUnsavedChanges = !currentJson.Equals(lastSavedJson, StringComparison.Ordinal);
                    StateHasChanged();
                });
            }
            catch (Exception ex)
            {
                LogError("change detection", ex);
            }
        }, null, 250, Timeout.Infinite);
    }

    // === COMPONENT LIFECYCLE ===

    protected override async Task OnInitializedAsync()
    {
        try
        {
            InitializeDefaultColumns();
            FilterEntities();
        }
        catch (Exception ex)
        {
            await HandleError("initialization", ex);
        }
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            try
            {
                // Initialize JavaScript features if needed
                if (JSRuntime != null)
                {
                    await JSRuntime.InvokeVoidAsync("initializePageFeatures");
                }
            }
            catch (Exception ex)
            {
                // JavaScript errors shouldn't break the page
                Console.WriteLine($"JavaScript initialization error: {ex.Message}");
            }
        }
    }

    public void Dispose()
    {
        changeDetectionTimer?.Dispose();
    }
}
```

### 4. JavaScript Interop Integration Pattern
Safe JavaScript coordination across components:

**Namespace and Import Fixes:**
- Added `using FabOS.WebServer.Models;` for TableColumn access
- Verified all model namespaces are correctly imported
- Fixed component references to use proper namespace paths

```csharp
// JavaScript interop coordination pattern
@code {
    // === FROZEN COLUMNS INTEGRATION ===

    private async Task UpdateTableColumns()
    {
        try
        {
            tableColumns = columnDefinitions
                .Where(c => c.IsVisible)
                .OrderBy(c => c.Order)
                .Select(c => CreateTableColumn(c))
                .ToList();

            // Apply frozen columns after table update
            await ApplyFrozenColumns();
        }
        catch (Exception ex)
        {
            await HandleError("table column update", ex);
        }
    }

    private async Task ApplyFrozenColumns()
    {
        try
        {
            if (JSRuntime != null)
            {
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
        }
        catch (InvalidOperationException)
        {
            // JSRuntime not available during prerendering - ignore safely
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error applying frozen columns: {ex.Message}");
            // Continue gracefully without frozen columns
        }
    }

    // === COMPONENT DISPOSAL INTEGRATION ===

    public void Dispose()
    {
        try
        {
            changeDetectionTimer?.Dispose();

            // Clean up JavaScript resources
            if (JSRuntime != null)
            {
                _ = JSRuntime.InvokeVoidAsync("disposeFrozenColumns");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error during disposal: {ex.Message}");
        }
    }
}
```

## Advanced Integration Patterns

### 1. Cross-Component Error Boundaries
Comprehensive error handling that prevents cascade failures:

```csharp
// Error boundary implementation pattern
public class ComponentErrorBoundary
{
    private readonly Dictionary<string, int> _errorCounts = new();
    private readonly TimeSpan _errorResetWindow = TimeSpan.FromMinutes(5);
    private readonly Dictionary<string, DateTime> _lastErrorTimes = new();

    public async Task<T> ExecuteWithBoundary<T>(
        string componentName,
        Func<Task<T>> operation,
        T fallbackValue,
        Func<string, Exception, Task>? errorHandler = null)
    {
        try
        {
            return await operation();
        }
        catch (Exception ex)
        {
            await HandleComponentError(componentName, ex, errorHandler);
            return fallbackValue;
        }
    }

    public void ExecuteWithBoundary(
        string componentName,
        Action operation,
        Action<string, Exception>? errorHandler = null)
    {
        try
        {
            operation();
        }
        catch (Exception ex)
        {
            HandleComponentErrorSync(componentName, ex, errorHandler);
        }
    }

    private async Task HandleComponentError(
        string componentName,
        Exception ex,
        Func<string, Exception, Task>? errorHandler)
    {
        // Track error frequency
        var now = DateTime.UtcNow;

        if (_lastErrorTimes.ContainsKey(componentName) &&
            now - _lastErrorTimes[componentName] > _errorResetWindow)
        {
            _errorCounts[componentName] = 0;
        }

        _errorCounts[componentName] = _errorCounts.GetValueOrDefault(componentName, 0) + 1;
        _lastErrorTimes[componentName] = now;

        // Log error with context
        Console.WriteLine($"Component error in {componentName} (count: {_errorCounts[componentName]}): {ex.Message}");

        // Execute custom error handler
        if (errorHandler != null)
        {
            try
            {
                await errorHandler(componentName, ex);
            }
            catch (Exception handlerEx)
            {
                Console.WriteLine($"Error in error handler for {componentName}: {handlerEx.Message}");
            }
        }

        // Escalate if too many errors
        if (_errorCounts[componentName] > 5)
        {
            Console.WriteLine($"Component {componentName} has exceeded error threshold, consider disabling");
        }
    }

    private void HandleComponentErrorSync(
        string componentName,
        Exception ex,
        Action<string, Exception>? errorHandler)
    {
        Console.WriteLine($"Sync component error in {componentName}: {ex.Message}");

        try
        {
            errorHandler?.Invoke(componentName, ex);
        }
        catch (Exception handlerEx)
        {
            Console.WriteLine($"Error in sync error handler for {componentName}: {handlerEx.Message}");
        }
    }
}

// Usage in page components
@code {
    private readonly ComponentErrorBoundary _errorBoundary = new();

    private async Task OnColumnsChanged(List<ColumnDefinition>? columns)
    {
        await _errorBoundary.ExecuteWithBoundary(
            "ColumnManager",
            async () =>
            {
                if (columns != null)
                {
                    columnDefinitions = columns;
                    await UpdateTableColumns();
                    TriggerViewStateChanged();
                }
            },
            async (component, ex) => await ShowErrorToUser($"Column management error: {ex.Message}")
        );

        StateHasChanged();
    }
}
```

### 2. Component Communication Hub Pattern
Centralized communication for complex interactions:

```csharp
// Communication hub for complex component interactions
public class ViewManagementHub : IDisposable
{
    private readonly Dictionary<string, List<Func<object, Task>>> _eventHandlers = new();
    private readonly SemaphoreSlim _semaphore = new(1, 1);

    // Event registration
    public void Subscribe<T>(string eventName, Func<T, Task> handler)
    {
        if (!_eventHandlers.ContainsKey(eventName))
        {
            _eventHandlers[eventName] = new List<Func<object, Task>>();
        }

        _eventHandlers[eventName].Add(async (data) =>
        {
            if (data is T typedData)
            {
                await handler(typedData);
            }
        });
    }

    // Event publication
    public async Task PublishAsync<T>(string eventName, T data)
    {
        await _semaphore.WaitAsync();
        try
        {
            if (_eventHandlers.ContainsKey(eventName))
            {
                var tasks = _eventHandlers[eventName].Select(handler => handler(data!));
                await Task.WhenAll(tasks);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error publishing event {eventName}: {ex.Message}");
        }
        finally
        {
            _semaphore.Release();
        }
    }

    // Synchronous event publication
    public void Publish<T>(string eventName, T data)
    {
        _ = Task.Run(() => PublishAsync(eventName, data));
    }

    public void Dispose()
    {
        _semaphore?.Dispose();
        _eventHandlers.Clear();
    }
}

// Hub integration in page components
@code {
    private readonly ViewManagementHub _hub = new();

    protected override void OnInitialized()
    {
        // Register for cross-component events
        _hub.Subscribe<ColumnDefinition>("ColumnFrozen", OnColumnFrozen);
        _hub.Subscribe<FilterRule>("FilterApplied", OnFilterApplied);
        _hub.Subscribe<string>("SearchPerformed", OnSearchPerformed);
        _hub.Subscribe<ViewState>("ViewStateRestored", OnViewStateRestored);
    }

    private async Task OnColumnFrozen(ColumnDefinition column)
    {
        // React to freeze events from other components
        await ApplyFrozenColumns();
        TriggerViewStateChanged();
    }

    private async Task OnColumnsChanged(List<ColumnDefinition>? columns)
    {
        if (columns != null)
        {
            columnDefinitions = columns;
            await UpdateTableColumns();

            // Notify other components of column changes
            foreach (var column in columns.Where(c => c.IsFrozen))
            {
                await _hub.PublishAsync("ColumnFrozen", column);
            }

            TriggerViewStateChanged();
        }
        StateHasChanged();
    }

    public void Dispose()
    {
        _hub?.Dispose();
    }
}
```

### 3. Performance Optimization Patterns
Efficient coordination that minimizes performance impact:

```csharp
// Performance optimization patterns
public class PerformanceOptimizedIntegration
{
    private readonly ConcurrentDictionary<string, object> _cache = new();
    private readonly Timer _cacheCleanupTimer;

    public PerformanceOptimizedIntegration()
    {
        _cacheCleanupTimer = new Timer(CleanupCache, null, TimeSpan.FromMinutes(5), TimeSpan.FromMinutes(5));
    }

    // Batched state updates
    private readonly List<Action> _pendingUpdates = new();
    private Timer? _batchTimer;

    public void BatchUpdate(Action update)
    {
        lock (_pendingUpdates)
        {
            _pendingUpdates.Add(update);
        }

        _batchTimer?.Dispose();
        _batchTimer = new Timer(_ =>
        {
            ExecuteBatchedUpdates();
        }, null, 100, Timeout.Infinite);
    }

    private void ExecuteBatchedUpdates()
    {
        List<Action> updates;
        lock (_pendingUpdates)
        {
            updates = new List<Action>(_pendingUpdates);
            _pendingUpdates.Clear();
        }

        foreach (var update in updates)
        {
            try
            {
                update();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in batched update: {ex.Message}");
            }
        }
    }

    // Memoized operations
    public T GetOrCompute<T>(string key, Func<T> computation)
    {
        return (T)_cache.GetOrAdd(key, _ => computation()!);
    }

    // Cache cleanup
    private void CleanupCache(object? state)
    {
        _cache.Clear();
    }

    public void Dispose()
    {
        _cacheCleanupTimer?.Dispose();
        _batchTimer?.Dispose();
    }
}

// Usage in component integration
@code {
    private readonly PerformanceOptimizedIntegration _performance = new();

    private void TriggerViewStateChanged()
    {
        _performance.BatchUpdate(() =>
        {
            var currentState = _performance.GetOrCompute(
                "current-view-state",
                () => GetCurrentViewState()
            );

            var lastSavedJson = lastSavedViewState?.ToJson() ?? "";
            var currentJson = currentState.ToJson();

            hasUnsavedChanges = !currentJson.Equals(lastSavedJson, StringComparison.Ordinal);
            StateHasChanged();
        });
    }
}
```

## Component-Specific Integration Guides

### 1. GenericViewSwitcher Integration
Type-safe view switching with proper event handling:

```csharp
// GenericViewSwitcher integration pattern
@code {
    // Type-safe view enumeration
    private GenericViewSwitcher<[EntityType]>.ViewType currentView =
        GenericViewSwitcher<[EntityType]>.ViewType.Table;

    // Event handler with error boundary
    private void OnViewChanged(GenericViewSwitcher<[EntityType]>.ViewType newView)
    {
        _errorBoundary.ExecuteWithBoundary(
            "ViewSwitcher",
            () =>
            {
                currentView = newView;
                TriggerViewStateChanged();

                // Optional: Trigger view-specific operations
                switch (newView)
                {
                    case GenericViewSwitcher<[EntityType]>.ViewType.Table:
                        PrepareTableView();
                        break;
                    case GenericViewSwitcher<[EntityType]>.ViewType.Card:
                        PrepareCardView();
                        break;
                    case GenericViewSwitcher<[EntityType]>.ViewType.List:
                        PrepareListView();
                        break;
                }
            },
            (component, ex) => Console.WriteLine($"View switching error: {ex.Message}")
        );

        StateHasChanged();
    }

    // View-specific preparation methods
    private void PrepareTableView()
    {
        // Ensure table columns are up to date
        _ = Task.Run(async () => await UpdateTableColumns());
    }

    private void PrepareCardView()
    {
        // Prepare card-specific data if needed
    }

    private void PrepareListView()
    {
        // Prepare list-specific data if needed
    }
}
```

### 2. ColumnManagerDropdown Integration
Comprehensive column management with async operations:

```csharp
// ColumnManagerDropdown integration pattern
@code {
    // Column definitions management
    private List<ColumnDefinition> columnDefinitions = new();
    private List<TableColumn<[EntityType]>> tableColumns = new();

    // Async column change handler
    private async Task OnColumnsChanged(List<ColumnDefinition>? columns)
    {
        await _errorBoundary.ExecuteWithBoundary(
            "ColumnManager",
            async () =>
            {
                if (columns != null)
                {
                    // Update column definitions
                    columnDefinitions = columns;

                    // Update table columns
                    await UpdateTableColumns();

                    // Apply frozen columns if any
                    var frozenColumns = columns.Where(c => c.IsFrozen).ToList();
                    if (frozenColumns.Any())
                    {
                        await ApplyFrozenColumns();
                    }

                    // Trigger state change
                    TriggerViewStateChanged();

                    // Optionally notify other components
                    await _hub.PublishAsync("ColumnsChanged", columns);
                }
            },
            new List<ColumnDefinition>(),
            async (component, ex) => await ShowErrorToUser($"Column management error: {ex.Message}")
        );

        StateHasChanged();
    }

    private async Task UpdateTableColumns()
    {
        try
        {
            tableColumns = columnDefinitions
                .Where(c => c.IsVisible)
                .OrderBy(c => c.Order)
                .Select(c => CreateTableColumn(c))
                .ToList();

            await InvokeAsync(StateHasChanged);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error updating table columns: {ex.Message}");
        }
    }

    private TableColumn<[EntityType]> CreateTableColumn(ColumnDefinition columnDef)
    {
        return new TableColumn<[EntityType]>
        {
            Header = columnDef.DisplayName,
            PropertyName = columnDef.PropertyName,
            IsSortable = true,
            CssClass = columnDef.IsFrozen ? "frozen-column" : "",
            ValueSelector = entity => GetPropertyValue(entity, columnDef.PropertyName)
        };
    }

    private object? GetPropertyValue(object entity, string propertyName)
    {
        try
        {
            var property = typeof([EntityType]).GetProperty(propertyName);
            return property?.GetValue(entity);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error getting property value for {propertyName}: {ex.Message}");
            return null;
        }
    }
}
```

### 3. FilterDialog Integration
Type-safe filtering with reflection-based field detection:

```csharp
// FilterDialog integration pattern
@code {
    // Filter state management
    private List<FilterRule> activeFilters = new();

    // Filter change handler
    private void OnFiltersChanged(List<FilterRule> filters)
    {
        _errorBoundary.ExecuteWithBoundary(
            "FilterDialog",
            () =>
            {
                activeFilters = filters ?? new List<FilterRule>();

                // Apply filters to data
                FilterEntities();

                // Trigger state change
                TriggerViewStateChanged();

                // Optional: Notify other components
                _hub.Publish("FiltersChanged", filters);
            },
            (component, ex) => Console.WriteLine($"Filter error: {ex.Message}")
        );

        StateHasChanged();
    }

    private void FilterEntities()
    {
        try
        {
            var result = [allEntities].AsEnumerable();

            // Apply search filter
            if (!string.IsNullOrEmpty(searchTerm))
            {
                result = ApplySearchFilter(result, searchTerm);
            }

            // Apply active filters
            if (activeFilters.Any())
            {
                result = ApplyActiveFilters(result, activeFilters);
            }

            [filteredEntities] = result.ToList();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error filtering entities: {ex.Message}");
            [filteredEntities] = [allEntities]; // Fallback to unfiltered
        }
    }

    private IEnumerable<[EntityType]> ApplySearchFilter(IEnumerable<[EntityType]> entities, string term)
    {
        var searchLower = term.ToLower();
        return entities.Where(entity =>
        {
            try
            {
                // Apply search to relevant properties
                return SearchEntityProperties(entity, searchLower);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in search filter: {ex.Message}");
                return true; // Include item if search fails
            }
        });
    }

    private IEnumerable<[EntityType]> ApplyActiveFilters(IEnumerable<[EntityType]> entities, List<FilterRule> filters)
    {
        return entities.Where(entity =>
        {
            try
            {
                foreach (var filter in filters)
                {
                    var fieldName = filter.Field ?? filter.FieldName;
                    if (string.IsNullOrEmpty(fieldName))
                        continue;

                    var propertyInfo = typeof([EntityType]).GetProperty(fieldName);
                    if (propertyInfo?.CanRead == true)
                    {
                        var value = propertyInfo.GetValue(entity);
                        if (!MatchesFilter(value, filter))
                            return false;
                    }
                }
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error applying filters to entity: {ex.Message}");
                return true; // Include item if filtering fails
            }
        });
    }

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
            Console.WriteLine($"Error matching filter: {ex.Message}");
            return true; // Include item if filter matching fails
        }
    }
}
```

### 4. ViewSavingDropdown Integration
Comprehensive view state persistence with error handling:

```csharp
// ViewSavingDropdown integration pattern
@code {
    // View state properties
    private ViewState currentViewState = new();
    private ViewState? lastSavedViewState;
    private bool hasUnsavedChanges = false;

    // View loading handler
    private async Task OnViewLoaded(ViewState? state)
    {
        await _errorBoundary.ExecuteWithBoundary(
            "ViewSaving",
            async () =>
            {
                isLoading = true;
                StateHasChanged();

                if (state == null)
                {
                    await ResetToDefaults();
                }
                else
                {
                    // Validate state before applying
                    if (ValidateViewState(state))
                    {
                        await ApplyViewState(state);
                    }
                    else
                    {
                        Console.WriteLine("Invalid view state detected, resetting to defaults");
                        await ResetToDefaults();
                        return;
                    }
                }

                // Update saved state reference
                lastSavedViewState = state?.Clone();
                hasUnsavedChanges = false;

                // Notify other components
                await _hub.PublishAsync("ViewStateLoaded", state);
            },
            async (component, ex) =>
            {
                await ShowErrorToUser($"Error loading view: {ex.Message}");
                await ResetToDefaults();
            }
        );

        isLoading = false;
        StateHasChanged();
    }

    private bool ValidateViewState(ViewState state)
    {
        try
        {
            // Basic validation
            if (state.Columns.Any(c => string.IsNullOrEmpty(c.PropertyName)))
                return false;

            if (state.Filters.Any(f => string.IsNullOrEmpty(f.Field) && string.IsNullOrEmpty(f.FieldName)))
                return false;

            return true;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error validating view state: {ex.Message}");
            return false;
        }
    }

    private async Task ResetToDefaults()
    {
        try
        {
            currentView = GenericViewSwitcher<[EntityType]>.ViewType.Table;
            InitializeDefaultColumns();
            activeFilters.Clear();
            searchTerm = "";
            FilterEntities();
            await UpdateTableColumns();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error resetting to defaults: {ex.Message}");
        }
    }
}
```

## Testing Integration Patterns

### 1. Component Integration Testing
Patterns for testing component interactions:

```csharp
// Integration test patterns for component coordination
[Test]
public async Task Should_Coordinate_Column_Changes_With_View_State()
{
    // Arrange
    var page = new TestableListPage();
    var columnChanges = new List<ColumnDefinition>
    {
        new() { PropertyName = "Name", IsVisible = true, IsFrozen = true, FreezePosition = FreezePosition.Left }
    };

    // Act
    await page.SimulateColumnChanges(columnChanges);

    // Assert
    Assert.That(page.HasUnsavedChanges, Is.True);
    Assert.That(page.GetCurrentViewState().Columns.First().IsFrozen, Is.True);
}

[Test]
public async Task Should_Handle_Component_Errors_Gracefully()
{
    // Arrange
    var page = new TestableListPage();
    var invalidFilters = new List<FilterRule>
    {
        new() { Field = null, FieldName = null } // Invalid filter
    };

    // Act & Assert
    Assert.DoesNotThrowAsync(async () => await page.SimulateFilterChanges(invalidFilters));
    Assert.That(page.IsInErrorState, Is.False);
}

// Testable page implementation
public class TestableListPage
{
    public bool HasUnsavedChanges { get; private set; }
    public bool IsInErrorState { get; private set; }

    public async Task SimulateColumnChanges(List<ColumnDefinition> columns)
    {
        // Simulate the OnColumnsChanged event
        await OnColumnsChanged(columns);
    }

    public async Task SimulateFilterChanges(List<FilterRule> filters)
    {
        // Simulate the OnFiltersChanged event
        OnFiltersChanged(filters);
    }

    public ViewState GetCurrentViewState()
    {
        // Return current state for testing
        return new ViewState();
    }

    // Implementation of actual page methods...
}
```

### 2. Error Handling Testing
Comprehensive error scenario testing:

```csharp
// Error handling test patterns
[Test]
public async Task Should_Handle_JavaScript_Interop_Errors()
{
    // Arrange
    var page = new TestableListPage();
    page.SimulateJSRuntimeUnavailable();

    var frozenColumns = new List<ColumnDefinition>
    {
        new() { PropertyName = "Name", IsFrozen = true }
    };

    // Act & Assert
    Assert.DoesNotThrowAsync(async () => await page.SimulateColumnChanges(frozenColumns));
    Assert.That(page.IsInErrorState, Is.False);
}

[Test]
public void Should_Maintain_State_During_Component_Failures()
{
    // Arrange
    var page = new TestableListPage();
    var originalState = page.GetCurrentViewState();

    // Act - Simulate component failure
    page.SimulateComponentError("FilterDialog");

    // Assert - State should be preserved
    var currentState = page.GetCurrentViewState();
    Assert.That(currentState.IsEquivalentTo(originalState), Is.True);
}
```

## Performance Monitoring

### 1. Integration Performance Metrics
Monitoring component interaction performance:

```csharp
// Performance monitoring for component integration
public class IntegrationPerformanceMonitor
{
    private readonly Dictionary<string, List<TimeSpan>> _operationTimes = new();
    private readonly object _lock = new();

    public void RecordOperation(string operationName, TimeSpan duration)
    {
        lock (_lock)
        {
            if (!_operationTimes.ContainsKey(operationName))
            {
                _operationTimes[operationName] = new List<TimeSpan>();
            }

            _operationTimes[operationName].Add(duration);

            // Keep only recent measurements
            if (_operationTimes[operationName].Count > 100)
            {
                _operationTimes[operationName].RemoveAt(0);
            }
        }
    }

    public PerformanceMetrics GetMetrics(string operationName)
    {
        lock (_lock)
        {
            if (!_operationTimes.ContainsKey(operationName) || !_operationTimes[operationName].Any())
            {
                return new PerformanceMetrics();
            }

            var times = _operationTimes[operationName];
            return new PerformanceMetrics
            {
                AverageTime = TimeSpan.FromTicks((long)times.Average(t => t.Ticks)),
                MinTime = times.Min(),
                MaxTime = times.Max(),
                OperationCount = times.Count
            };
        }
    }
}

public class PerformanceMetrics
{
    public TimeSpan AverageTime { get; set; }
    public TimeSpan MinTime { get; set; }
    public TimeSpan MaxTime { get; set; }
    public int OperationCount { get; set; }
}

// Usage in component integration
@code {
    private readonly IntegrationPerformanceMonitor _performanceMonitor = new();

    private async Task OnColumnsChanged(List<ColumnDefinition>? columns)
    {
        var stopwatch = Stopwatch.StartNew();

        try
        {
            if (columns != null)
            {
                columnDefinitions = columns;
                await UpdateTableColumns();
                TriggerViewStateChanged();
            }
        }
        finally
        {
            stopwatch.Stop();
            _performanceMonitor.RecordOperation("ColumnChanges", stopwatch.Elapsed);

            var metrics = _performanceMonitor.GetMetrics("ColumnChanges");
            if (metrics.AverageTime > TimeSpan.FromMilliseconds(500))
            {
                Console.WriteLine($"Column changes taking longer than expected: {metrics.AverageTime.TotalMilliseconds}ms average");
            }
        }
    }
}
```

## Conclusion

The Component Integration Patterns provide a comprehensive framework for building robust, performant, and maintainable view management systems in Fab.OS. Through standardized integration patterns, comprehensive error handling, and efficient communication protocols, this architecture ensures:

**Key Achievements:**
- **Unified Integration**: Standardized patterns across all list page implementations
- **Robust Error Handling**: Comprehensive error boundaries prevent cascade failures
- **Efficient Communication**: Type-safe async patterns with optimal performance
- **Maintainable Architecture**: Clear separation of concerns and consistent patterns
- **Extensible Design**: Easy integration of new components and functionality

**Developer Benefits:**
- **Reduced Implementation Time**: Standardized patterns accelerate development
- **Consistent Quality**: Error handling and performance patterns ensure reliability
- **Easy Maintenance**: Clear integration guidelines reduce maintenance overhead
- **Future-Proof Design**: Extensible patterns support new requirements

**System Benefits:**
- **Reliable Operation**: Comprehensive error handling prevents system failures
- **Optimal Performance**: Efficient patterns minimize performance impact
- **Scalable Architecture**: Patterns support growing complexity and feature sets
- **Quality Assurance**: Built-in monitoring and testing patterns ensure reliability

This architecture establishes component integration as a core competency of Fab.OS, providing developers with enterprise-grade patterns while maintaining system performance and user experience standards. The patterns serve as the foundation for all future view management development, ensuring consistency, reliability, and maintainability across the entire application.