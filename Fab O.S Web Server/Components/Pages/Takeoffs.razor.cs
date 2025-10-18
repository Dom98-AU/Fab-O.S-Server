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
    [Inject] private ApplicationDbContext DbContext { get; set; } = default!;
    [Inject] private NavigationManager Navigation { get; set; } = default!;

    private bool isLoading = true;
    private string searchTerm = "";
    private GenericViewSwitcher<TraceDrawing>.ViewType currentView = GenericViewSwitcher<TraceDrawing>.ViewType.Table;
    private List<TraceDrawing> allTakeoffs = new();
    private List<TraceDrawing> filteredTakeoffs = new();
    private List<TraceDrawing> selectedTableItems = new();
    private List<TraceDrawing> selectedListItems = new();
    private List<TraceDrawing> selectedCardItems = new();

    // View state management
    private ViewState? currentViewState;
    private bool hasUnsavedChanges = false;
    private bool hasCustomColumnConfig = false;
    private List<ColumnDefinition> managedColumns = new();


    // Table columns configuration
    private List<GenericTableView<TraceDrawing>.TableColumn<TraceDrawing>> tableColumns = new()
    {
        new() { Header = "Takeoff Number", ValueSelector = item => item.TakeoffNumber ?? "N/A", IsSortable = true },
        new() { Header = "Description", ValueSelector = item => item.TraceName ?? "No description", IsSortable = true },
        new() { Header = "Project", ValueSelector = item => item.ProjectName ?? "N/A", IsSortable = true },
        new() { Header = "Customer", ValueSelector = item => item.Customer?.Name ?? "N/A", IsSortable = true },
        new() { Header = "Type", ValueSelector = item => item.FileType, IsSortable = true },
        new() { Header = "Status", ValueSelector = item => item.ProcessingStatus, IsSortable = true },
        new() { Header = "Created Date", ValueSelector = item => item.CreatedDate.ToString("MMM dd, yyyy"), IsSortable = true }
    };

    // Missing template properties for Takeoffs.razor
    private RenderFragment<TraceDrawing>? TableActionsTemplate => null;

    private RenderFragment<TraceDrawing> ListIconTemplate => item => builder =>
    {
        builder.OpenElement(0, "i");
        builder.AddAttribute(1, "class", "fas fa-drafting-compass");
        builder.CloseElement();
    };

    private RenderFragment<TraceDrawing> ListTitleTemplate => item => builder =>
    {
        builder.AddContent(0, item.TakeoffNumber ?? "N/A");
    };

    private RenderFragment<TraceDrawing> ListSubtitleTemplate => item => builder =>
    {
        builder.AddContent(0, item.TraceName ?? "No description");
    };

    private RenderFragment<TraceDrawing> ListStatusTemplate => item => builder =>
    {
        builder.OpenElement(0, "span");
        builder.AddAttribute(1, "class", $"badge bg-{(item.ProcessingStatus == "Completed" ? "success" : "secondary")}");
        builder.AddContent(2, item.ProcessingStatus);
        builder.CloseElement();
    };

    private RenderFragment<TraceDrawing> ListDetailsTemplate => item => builder =>
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
                PropertyName = "TraceName",
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
            filteredTakeoffs = new List<TraceDrawing>(allTakeoffs);
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

    private void OnViewChanged(GenericViewSwitcher<TraceDrawing>.ViewType newView)
    {
        currentView = newView;
        StateHasChanged();
    }

    private void HandleTableSelectionChanged(List<TraceDrawing> selected)
    {
        selectedTableItems = selected;
        StateHasChanged();
    }

    private void HandleListSelectionChanged(List<TraceDrawing> selected)
    {
        selectedListItems = selected;
        StateHasChanged();
    }

    private void HandleCardSelectionChanged(List<TraceDrawing> selected)
    {
        selectedCardItems = selected;
        StateHasChanged();
    }

    private void HandleRowClick(TraceDrawing item)
    {
        OpenTakeoff(item.Id);
    }

    private void HandleRowDoubleClick(TraceDrawing item)
    {
        OpenTakeoff(item.Id);
    }

    private void OnSelectionChanged((TraceDrawing item, bool selected) args)
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
        Navigation.NavigateTo($"/takeoffs/{takeoffId}");
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
        Navigation.NavigateTo("/takeoffs/0");
    }

    // IToolbarActionProvider implementation
    public ToolbarActionGroup GetActions()
    {
        var group = new ToolbarActionGroup();
        group.PrimaryActions = new List<FabOS.WebServer.Components.Shared.Interfaces.ToolbarAction>
        {
            new FabOS.WebServer.Components.Shared.Interfaces.ToolbarAction
            {
                Label = "New",
                Text = "New",
                Icon = "fas fa-plus",
                Action = EventCallback.Factory.Create(this, () => CreateNewTakeoff()),
                IsDisabled = false,
                Style = FabOS.WebServer.Components.Shared.Interfaces.ToolbarActionStyle.Primary
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
                IsDisabled = false
            }
        };
        return group;
    }
}