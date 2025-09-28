# Standalone Components Architecture

## Executive Summary

The Standalone Components System provides a collection of 100% independent, reusable UI components that can work without any dependencies on the existing column infrastructure. These components are designed to be used anywhere - within the current Steel Estimation platform, in other Blazor projects, or even packaged as NuGet packages.

## Philosophy & Design Principles

### Core Principles

1. **Zero Dependencies**: Each component works independently without requiring other components
2. **Interface-Based**: All components work through generic interfaces, not concrete implementations
3. **Progressive Enhancement**: Can be used standalone or integrated with existing systems via adapters
4. **Universal Compatibility**: Works with any data type that implements the required interface
5. **No Breaking Changes**: Existing integrated components continue to work unchanged

### Architecture Overview

```
Standalone Components
│
├── Core Layer (Interfaces & Models)
│   ├── IResizable
│   ├── IReorderable<T>
│   ├── IViewSaveable
│   ├── IViewSwitchable
│   └── IFreezable
│
├── Component Layer (Implementations)
│   ├── GenericResizer
│   ├── GenericReorderer
│   ├── GenericViewSaver
│   ├── GenericViewSwitcher
│   └── GenericFreezer
│
└── Integration Layer (Adapters)
    ├── ColumnResizerAdapter
    ├── ColumnReordererAdapter
    ├── ViewStateAdapter
    ├── ViewSwitcherAdapter
    └── ColumnFreezerAdapter
```

## Component Catalog

### 1. GenericFreezer

**Purpose**: Freeze/pin any elements to any side of a container with multi-freeze support.

**Interface**: `IFreezable`
```csharp
public interface IFreezable {
    string Id { get; }
    bool IsFrozen { get; set; }
    FreezePosition Position { get; set; }
    int FreezeOrder { get; set; }
    bool CanFreeze { get; }
    string? FreezeGroup { get; }
}
```

**Features**:
- Multi-position freezing (Left, Right, Top, Bottom)
- Single or multi-freeze modes
- Group freezing support
- Touch gesture support
- Auto-size calculation
- Nested freezing capability
- Visual indicators and animations
- Freeze rules engine for advanced scenarios

**Usage Examples**:

```razor
@* Basic Usage - Freeze any list of items *@
<GenericFreezer Items="@myItems" 
                Mode="Multi"
                DefaultPosition="FreezePosition.Left"
                MaxFrozen="3">
    <ItemTemplate>
        <div>@context.Data</div>
    </ItemTemplate>
</GenericFreezer>

@* Advanced Usage - With gestures and rules *@
<GenericFreezer Items="@columns"
                EnableGestures="true"
                Configuration="@freezeConfig">
    <FrozenTemplate>
        @foreach(var item in context) {
            <FrozenColumn Data="@item" />
        }
    </FrozenTemplate>
    <ScrollableTemplate>
        @foreach(var item in context) {
            <ScrollableColumn Data="@item" />
        }
    </ScrollableTemplate>
</GenericFreezer>

@code {
    private FreezeConfiguration freezeConfig = new() {
        Mode = FreezeMode.Advanced,
        FreezeGesture = FreezeGesture.SwipeRight,
        UnfreezeGesture = FreezeGesture.SwipeLeft,
        MaxFrozen = 5,
        AnimateFreeze = true
    };
}
```

### 2. GenericResizer

**Purpose**: Add resizing capability to any element or data object.

**Interface**: `IResizable`
```csharp
public interface IResizable {
    string Id { get; }
    double Size { get; set; }
    double MinSize { get; }
    double MaxSize { get; }
    ResizeDirection Direction { get; }
    bool CanResize { get; }
}
```

**Features**:
- Horizontal, vertical, or bidirectional resizing
- Mouse and touch support
- Visual resize guide
- Grid snapping
- Aspect ratio preservation
- Keyboard support (arrow keys)
- Real-time or end-only updates
- Throttled updates for performance

**Usage Examples**:

```razor
@* Resize a data object *@
<GenericResizer Target="@myResizableItem"
                Direction="ResizeDirection.Horizontal"
                ShowSizeIndicator="true"
                OnResizeEnd="@HandleResize" />

@* Resize a DOM element directly *@
<div id="my-panel">
    <GenericResizer ElementId="my-panel"
                    MinSize="200"
                    MaxSize="800"
                    Direction="ResizeDirection.Both" />
    Content here...
</div>

@* With configuration *@
<GenericResizer Target="@column"
                Configuration="@resizeConfig">
    <HandleContent>
        <i class="fas fa-grip-lines-vertical"></i>
    </HandleContent>
</GenericResizer>

@code {
    private ResizeConfiguration resizeConfig = new() {
        ShowGuide = true,
        SnapToGrid = true,
        GridSize = 10,
        RealTimeResize = false,
        ThrottleMs = 50
    };
}
```

### 3. GenericReorderer

**Purpose**: Enable drag-and-drop reordering for any collection.

**Interface**: `IReorderable<T>`
```csharp
public interface IReorderable<T> {
    string Id { get; }
    int Order { get; set; }
    T Data { get; }
    bool CanReorder { get; }
    string? GroupId { get; }
}
```

**Features**:
- Drag and drop with visual feedback
- Touch gesture support
- Group-based reordering
- Auto-scroll during drag
- Custom drag handles
- Placeholder display
- Animation support
- Keyboard navigation

**Usage Examples**:

```razor
@* Basic reordering *@
<GenericReorderer Items="@taskList"
                  OnReordered="@HandleReorder">
    <ItemTemplate>
        <div class="task-item">
            <span class="drag-handle">≡</span>
            @context.Data.Title
        </div>
    </ItemTemplate>
</GenericReorderer>

@* Advanced with groups *@
<GenericReorderer Items="@groupedItems"
                  Configuration="@reorderConfig"
                  OnBatchReorder="@HandleBatchReorder">
    <GroupTemplate>
        <h3>@context.GroupId</h3>
    </GroupTemplate>
    <ItemTemplate>
        <Card Data="@context.Data" />
    </ItemTemplate>
</GenericReorderer>
```

### 4. GenericViewSaver

**Purpose**: Save and restore any component's view state.

**Interface**: `IViewSaveable`
```csharp
public interface IViewSaveable {
    string GetState();
    void SetState(string state);
    string GetStateId();
    bool HasChanges();
}
```

**Features**:
- Multiple storage backends (LocalStorage, SessionStorage, Server)
- Auto-save capability
- Named views management
- Import/Export functionality
- Default views
- Shared views (with server storage)
- State compression
- Change detection

**Usage Examples**:

```razor
@* Basic state saving *@
<GenericViewSaver StateProvider="@this"
                  StorageKey="my-dashboard"
                  AutoSave="true" />

@* Advanced with custom UI *@
<GenericViewSaver StateProvider="@stateManager"
                  Configuration="@saveConfig"
                  OnViewSaved="@HandleSave"
                  OnViewLoaded="@HandleLoad">
    <SaveButton>
        <button class="btn-custom">💾 Save Layout</button>
    </SaveButton>
    <LoadButton>
        <button class="btn-custom">📂 Load Layout</button>
    </LoadButton>
    <ManageButton>
        <button class="btn-custom">⚙️ Manage Views</button>
    </ManageButton>
</GenericViewSaver>

@code {
    private ViewSaveConfiguration saveConfig = new() {
        StorageType = StorageType.Server,
        AutoSave = true,
        AutoSaveDelayMs = 2000,
        AllowExport = true,
        AllowImport = true,
        MaxSavedViews = 20
    };
}
```

### 5. GenericViewSwitcher

**Purpose**: Switch between multiple view modes with various UI patterns.

**Interface**: `IViewSwitchable`
```csharp
public interface IViewSwitchable {
    string CurrentView { get; set; }
    Dictionary<string, ViewDefinition> Views { get; }
    ViewSwitchMode SwitchMode { get; }
    bool CanSwitch { get; }
}
```

**Features**:
- Multiple switch modes (Tabs, Dropdown, Buttons, Sidebar, Carousel)
- Transition animations
- Icon support
- Keyboard shortcuts
- Lazy loading
- View count badges
- Mobile responsive
- Remember last view

**Usage Examples**:

```razor
@* Basic view switching *@
<GenericViewSwitcher @bind-CurrentView="currentView"
                     SwitchMode="ViewSwitchMode.Tabs">
    <Views>
        <View Key="grid" Icon="fa-th" Label="Grid">
            <GridView Items="@items" />
        </View>
        <View Key="list" Icon="fa-list" Label="List">
            <ListView Items="@items" />
        </View>
        <View Key="chart" Icon="fa-chart" Label="Analytics">
            <ChartView Data="@items" />
        </View>
    </Views>
</GenericViewSwitcher>

@* Advanced with configuration *@
<GenericViewSwitcher Configuration="@switchConfig"
                     OnViewSwitch="@HandleViewChange">
    @* Views defined programmatically *@
</GenericViewSwitcher>

@code {
    private ViewSwitchConfiguration switchConfig = new() {
        Mode = ViewSwitchMode.Carousel,
        Transition = TransitionType.Slide,
        TransitionDurationMs = 300,
        EnableKeyboardShortcuts = true,
        LazyLoadViews = true,
        RememberLastView = true
    };
}
```

## Integration with Existing System

### Adapter Pattern

The adapter pattern allows standalone components to work seamlessly with the existing column-based system:

```csharp
// Example: ColumnFreezerAdapter
public class ColumnFreezerAdapter : IFreezable {
    private ColumnDefinition<T> _column;
    
    public ColumnFreezerAdapter(ColumnDefinition<T> column) {
        _column = column;
    }
    
    public string Id => _column.Key;
    public bool IsFrozen { 
        get => _column.IsFrozen;
        set => _column.IsFrozen = value;
    }
    // ... other mappings
}
```

### Using Adapters

```razor
@* Use standalone component with existing column *@
<GenericFreezer Items="@columns.Select(c => new ColumnFreezerAdapter(c))"
                OnItemFrozen="@HandleColumnFreeze" />

@* Use standalone resizer with column *@
<GenericResizer Target="@(new ColumnResizerAdapter(column))"
                OnResizeEnd="@UpdateColumnWidth" />
```

## Migration Guide

### From Integrated to Standalone

1. **Identify Dependencies**: Check which integrated components you're using
2. **Choose Migration Strategy**:
   - Full migration: Replace all integrated components
   - Hybrid: Use adapters to mix both systems
   - Progressive: Migrate component by component
3. **Implement Interfaces**: Ensure your data models implement required interfaces
4. **Update Templates**: Adjust your Razor templates for new component syntax
5. **Test Thoroughly**: Verify all functionality works as expected

### Example Migration

**Before (Integrated)**:
```razor
<DataTable Columns="@columns" Items="@items">
    <ColumnResizer />
</DataTable>
```

**After (Standalone)**:
```razor
<table>
    <thead>
        @foreach(var col in columns) {
            <th>
                @col.Header
                <GenericResizer Target="@(new ColumnAdapter(col))" />
            </th>
        }
    </thead>
    <tbody>
        <!-- content -->
    </tbody>
</table>
```

## Performance Considerations

### Optimization Tips

1. **Use Virtualization**: For large lists in reorderer/freezer
2. **Throttle Updates**: Configure throttling in resizer
3. **Lazy Load Views**: Enable in view switcher for better initial load
4. **Compress State**: Enable compression for large view states
5. **Batch Operations**: Use batch events instead of individual updates

### Memory Management

- Components implement `IAsyncDisposable`
- JavaScript modules are properly cleaned up
- Event handlers are detached on disposal
- DOM references are released

## Styling & Customization

### CSS Variables

Each component exposes CSS variables for customization:

```css
:root {
    --resizer-handle-color: #3144CD;
    --freezer-frozen-bg: rgba(241, 245, 255, 0.98);
    --reorderer-placeholder-bg: #f0f0f0;
    --view-switcher-tab-active: #3144CD;
}
```

### Custom Templates

All components support custom templates:

```razor
<GenericFreezer>
    <FrozenTemplate>
        <!-- Custom frozen item rendering -->
    </FrozenTemplate>
    <ScrollableTemplate>
        <!-- Custom scrollable item rendering -->
    </ScrollableTemplate>
</GenericFreezer>
```

## Testing

### Unit Testing

```csharp
[Test]
public void Resizer_RespectsMinMaxConstraints() {
    var item = new ResizableItem { 
        MinSize = 100, 
        MaxSize = 500 
    };
    
    var resizer = new GenericResizer<ResizableItem>();
    resizer.Target = item;
    
    // Test constraints
    await resizer.OnJsResize(50);  // Should clamp to 100
    Assert.AreEqual(100, item.Size);
    
    await resizer.OnJsResize(600); // Should clamp to 500
    Assert.AreEqual(500, item.Size);
}
```

### Integration Testing

```csharp
[Test]
public async Task FreezerAndResizer_WorkTogether() {
    var items = CreateTestItems();
    
    // Freeze items
    var freezer = new GenericFreezer<TestItem>();
    await freezer.FreezeItem(items[0]);
    
    // Resize frozen item
    var resizer = new GenericResizer<TestItem>();
    resizer.Target = items[0];
    await resizer.OnJsResize(300);
    
    // Verify both states maintained
    Assert.IsTrue(items[0].IsFrozen);
    Assert.AreEqual(300, items[0].Size);
}
```

## Troubleshooting

### Common Issues

1. **Component not rendering**:
   - Verify interface implementation
   - Check required parameters
   - Ensure CSS is loaded

2. **Events not firing**:
   - Check event callback binding
   - Verify JavaScript interop initialization
   - Check browser console for errors

3. **Gestures not working**:
   - Enable gestures explicitly
   - Check touch event support
   - Verify gesture configuration

4. **State not persisting**:
   - Check storage permissions
   - Verify state provider implementation
   - Check browser storage limits

## Package Distribution

### Creating NuGet Package

```xml
<Project Sdk="Microsoft.NET.Sdk.Razor">
  <PropertyGroup>
    <TargetFramework>net8.0</TargetFramework>
    <PackageId>FabOS.StandaloneComponents</PackageId>
    <Version>1.0.0</Version>
    <Authors>FabOS Team</Authors>
    <Description>Standalone Blazor components for UI interactions</Description>
  </PropertyGroup>
</Project>
```

### Usage in Other Projects

```bash
dotnet add package FabOS.StandaloneComponents
```

```razor
@using FabOS.StandaloneComponents

<GenericFreezer Items="@myItems" />
<GenericResizer Target="@myResizable" />
```

## Future Enhancements

### Planned Features

1. **GenericSelector**: Multi-select with gestures
2. **GenericZoomer**: Zoom and pan functionality
3. **GenericTimeline**: Timeline view switching
4. **GenericThemer**: Dynamic theme switching
5. **GenericExporter**: Universal export functionality

### Community Contributions

- Open for pull requests
- Component request process
- Documentation improvements
- Localization support

## Conclusion

The Standalone Components System provides a robust, flexible, and truly independent set of UI components that can be used in any Blazor application. By following interface-based design and providing adapters for integration, these components offer the best of both worlds: complete independence when needed, and seamless integration when desired.