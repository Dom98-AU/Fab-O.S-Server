using Microsoft.AspNetCore.Components;
using Microsoft.EntityFrameworkCore;
using FabOS.WebServer.Components.Shared.Interfaces;
using FabOS.WebServer.Components.Shared;
using FabOS.WebServer.Data.Contexts;
using FabOS.WebServer.Models.Entities;
using FabOS.WebServer.Models;
using FabOS.WebServer.Models.Columns;
using FabOS.WebServer.Models.ViewState;
using ModelToolbarAction = FabOS.WebServer.Models.ToolbarAction;

namespace FabOS.WebServer.Components.Pages;

public partial class Takeoffs : ComponentBase, IToolbarActionProvider
{
    [Parameter] public string? TenantSlug { get; set; }

    [Inject] private ApplicationDbContext DbContext { get; set; } = default!;
    [Inject] private NavigationManager Navigation { get; set; } = default!;

    private bool isLoading = true;
    private string searchTerm = "";
    private GenericViewSwitcher<Takeoff>.ViewType currentView = GenericViewSwitcher<Takeoff>.ViewType.Table;
    private List<Takeoff> allTakeoffs = new();
    private List<Takeoff> filteredTakeoffs = new();
    private List<Takeoff> selectedTableItems = new();
    private List<Takeoff> selectedListItems = new();
    private List<Takeoff> selectedCardItems = new();

    // View state management
    private ViewState? currentViewState;
    private bool hasUnsavedChanges = false;
    private bool hasCustomColumnConfig = false;
    private List<ColumnDefinition> managedColumns = new();


    // Table columns configuration
    private List<GenericTableView<Takeoff>.TableColumn<Takeoff>> tableColumns = new()
    {
        new() { Header = "Takeoff Number", ValueSelector = item => item.TakeoffNumber ?? "N/A", IsSortable = true },
        new() { Header = "Description", ValueSelector = item => item.Description ?? item.TraceName ?? "No description", IsSortable = true },
        new() { Header = "Project", ValueSelector = item => item.ProjectName ?? "N/A", IsSortable = true },
        new() { Header = "Customer", ValueSelector = item => item.Customer?.Name ?? "N/A", IsSortable = true },
        new() { Header = "Type", ValueSelector = item => item.FileType, IsSortable = true },
        new() { Header = "Status", ValueSelector = item => item.ProcessingStatus, IsSortable = true },
        new() { Header = "Created Date", ValueSelector = item => item.CreatedDate.ToString("MMM dd, yyyy"), IsSortable = true }
    };

    // Missing template properties for Takeoffs.razor
    private RenderFragment<Takeoff>? TableActionsTemplate => null;

    private RenderFragment<Takeoff> ListIconTemplate => item => builder =>
    {
        builder.OpenElement(0, "i");
        builder.AddAttribute(1, "class", "fas fa-drafting-compass");
        builder.CloseElement();
    };

    private RenderFragment<Takeoff> ListTitleTemplate => item => builder =>
    {
        builder.AddContent(0, item.TakeoffNumber ?? "N/A");
    };

    private RenderFragment<Takeoff> ListSubtitleTemplate => item => builder =>
    {
        builder.AddContent(0, item.Description ?? item.TraceName ?? "No description");
    };

    private RenderFragment<Takeoff> ListStatusTemplate => item => builder =>
    {
        builder.OpenElement(0, "span");
        builder.AddAttribute(1, "class", $"badge bg-{(item.ProcessingStatus == "Completed" ? "success" : "secondary")}");
        builder.AddContent(2, item.ProcessingStatus);
        builder.CloseElement();
    };

    private RenderFragment<Takeoff> ListDetailsTemplate => item => builder =>
    {
        builder.AddContent(0, $"Project: {item.ProjectName ?? "N/A"}");
    };

    protected override async Task OnInitializedAsync()
    {
        InitializeColumns();
        await LoadTakeoffs();
    }

    private void InitializeColumns()
    {
        managedColumns = new List<ColumnDefinition>
        {
            new ColumnDefinition
            {
                PropertyName = "TakeoffNumber",
                DisplayName = "Takeoff Number",
                Type = ColumnType.Text,
                IsVisible = true,
                IsSortable = true,
                IsResizable = true,
                IsReorderable = true,
                Width = 150,
                Order = 0
            },
            new ColumnDefinition
            {
                PropertyName = "Description",
                DisplayName = "Description",
                Type = ColumnType.Text,
                IsVisible = true,
                IsSortable = true,
                IsResizable = true,
                IsReorderable = true,
                Width = 250,
                Order = 1
            },
            new ColumnDefinition
            {
                PropertyName = "ProjectName",
                DisplayName = "Project",
                Type = ColumnType.Text,
                IsVisible = true,
                IsSortable = true,
                IsResizable = true,
                IsReorderable = true,
                Width = 200,
                Order = 2
            },
            new ColumnDefinition
            {
                PropertyName = "Customer.Name",
                DisplayName = "Customer",
                Type = ColumnType.Text,
                IsVisible = true,
                IsSortable = true,
                IsResizable = true,
                IsReorderable = true,
                Width = 180,
                Order = 3
            },
            new ColumnDefinition
            {
                PropertyName = "FileType",
                DisplayName = "Type",
                Type = ColumnType.Text,
                IsVisible = true,
                IsSortable = true,
                IsResizable = true,
                IsReorderable = true,
                Width = 100,
                Order = 4
            },
            new ColumnDefinition
            {
                PropertyName = "ProcessingStatus",
                DisplayName = "Status",
                Type = ColumnType.Status,
                IsVisible = true,
                IsSortable = true,
                IsResizable = true,
                IsReorderable = true,
                Width = 120,
                Order = 5
            },
            new ColumnDefinition
            {
                PropertyName = "CreatedDate",
                DisplayName = "Created Date",
                Type = ColumnType.Date,
                IsVisible = true,
                IsSortable = true,
                IsResizable = true,
                IsReorderable = true,
                Width = 150,
                Order = 6,
                Format = "MMM dd, yyyy"
            }
        };
    }


    private async Task LoadTakeoffs()
    {
        try
        {
            isLoading = true;
            allTakeoffs = await DbContext.TraceDrawings
                .Include(t => t.Customer)
                .Include(t => t.Project)
                .OrderByDescending(t => t.CreatedDate)
                .ToListAsync();

            FilterTakeoffs();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error loading takeoffs: {ex.Message}");
        }
        finally
        {
            isLoading = false;
        }
    }

    private void FilterTakeoffs()
    {
        if (string.IsNullOrWhiteSpace(searchTerm))
        {
            filteredTakeoffs = new List<Takeoff>(allTakeoffs);
        }
        else
        {
            var term = searchTerm.ToLower();
            filteredTakeoffs = allTakeoffs.Where(t =>
                t.DrawingNumber?.ToLower().Contains(term) == true ||
                t.FileName?.ToLower().Contains(term) == true ||
                t.FileType?.ToLower().Contains(term) == true ||
                t.ProcessingStatus?.ToLower().Contains(term) == true
            ).ToList();
        }
    }

    private void OnSearchChanged(string newSearchTerm)
    {
        searchTerm = newSearchTerm;
        FilterTakeoffs();
        StateHasChanged();
    }

    private void OnViewChanged(GenericViewSwitcher<Takeoff>.ViewType newView)
    {
        currentView = newView;
        StateHasChanged();
    }

    private void HandleTableSelectionChanged(List<Takeoff> selected)
    {
        selectedTableItems = selected;
        StateHasChanged();
    }

    private void HandleListSelectionChanged(List<Takeoff> selected)
    {
        selectedListItems = selected;
        StateHasChanged();
    }

    private void HandleCardSelectionChanged(List<Takeoff> selected)
    {
        selectedCardItems = selected;
        StateHasChanged();
    }

    private void HandleRowClick(Takeoff item)
    {
        OpenTakeoff(item.Id);
    }

    private void HandleRowDoubleClick(Takeoff item)
    {
        OpenTakeoff(item.Id);
    }

    private void OnSelectionChanged((Takeoff item, bool selected) args)
    {
        if (args.selected)
        {
            if (!selectedTableItems.Contains(args.item))
                selectedTableItems.Add(args.item);
        }
        else
        {
            selectedTableItems.Remove(args.item);
        }
        StateHasChanged();
    }

    private void OpenTakeoff(int takeoffId)
    {
        Navigation.NavigateTo($"/{TenantSlug}/trace/takeoffs/{takeoffId}");
    }

    private void HandleViewLoaded(ViewState viewState)
    {
        // Handle loading a saved view preference
        hasUnsavedChanges = false;
        StateHasChanged();
    }

    private void HandleColumnsChanged(List<ColumnDefinition> columns)
    {
        managedColumns = columns;
        hasUnsavedChanges = true;
        hasCustomColumnConfig = true;
        StateHasChanged();
    }

    private void CreateNewTakeoff()
    {
        Navigation.NavigateTo($"/{TenantSlug}/trace/takeoffs/0");
    }

    // IToolbarActionProvider implementation
    private List<Takeoff> GetSelectedTakeoffs()
    {
        return currentView switch
        {
            GenericViewSwitcher<Takeoff>.ViewType.Table => selectedTableItems,
            GenericViewSwitcher<Takeoff>.ViewType.List => selectedListItems,
            GenericViewSwitcher<Takeoff>.ViewType.Card => selectedCardItems,
            _ => new List<Takeoff>()
        };
    }

    private async Task DeleteSelectedTakeoffs()
    {
        var selected = GetSelectedTakeoffs();
        if (!selected.Any()) return;

        // TODO: Add confirmation dialog
        foreach (var takeoff in selected)
        {
            takeoff.IsDeleted = true;
            takeoff.LastModified = DateTime.UtcNow;
        }

        await DbContext.SaveChangesAsync();
        await LoadTakeoffs();
    }

    public ToolbarActionGroup GetActions()
    {
        var selected = GetSelectedTakeoffs();
        var hasSelection = selected.Any();

        var group = new ToolbarActionGroup();
        group.PrimaryActions = new List<FabOS.WebServer.Components.Shared.Interfaces.ToolbarAction>
        {
            new FabOS.WebServer.Components.Shared.Interfaces.ToolbarAction
            {
                Label = "New",
                Text = "New",
                Icon = "fas fa-plus",
                Action = EventCallback.Factory.Create(this, () => CreateNewTakeoff()),
                Style = FabOS.WebServer.Components.Shared.Interfaces.ToolbarActionStyle.Primary,
                Tooltip = "Create new takeoff"
            },
            new FabOS.WebServer.Components.Shared.Interfaces.ToolbarAction
            {
                Label = "Delete",
                Text = "Delete",
                Icon = "fas fa-trash",
                ActionFunc = DeleteSelectedTakeoffs,
                IsDisabled = !hasSelection,
                Style = FabOS.WebServer.Components.Shared.Interfaces.ToolbarActionStyle.Danger,
                Tooltip = hasSelection ? "Delete selected takeoffs" : "Select takeoffs to delete"
            }
        };
        group.MenuActions = new List<FabOS.WebServer.Components.Shared.Interfaces.ToolbarAction>
        {
            new FabOS.WebServer.Components.Shared.Interfaces.ToolbarAction
            {
                Label = "Import",
                Text = "Import",
                Icon = "fas fa-file-import",
                Action = EventCallback.Factory.Create(this, () => Console.WriteLine("Import clicked")),
                Tooltip = "Import takeoffs from file"
            },
            new FabOS.WebServer.Components.Shared.Interfaces.ToolbarAction
            {
                Label = "Export",
                Text = "Export",
                Icon = "fas fa-file-export",
                Action = EventCallback.Factory.Create(this, () => Console.WriteLine("Export clicked")),
                IsDisabled = !hasSelection,
                Tooltip = "Export selected takeoffs"
            }
        };
        return group;
    }
}