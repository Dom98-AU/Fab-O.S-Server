using Microsoft.AspNetCore.Components;
using Microsoft.EntityFrameworkCore;
using FabOS.WebServer.Components.Shared.Interfaces;
using FabOS.WebServer.Components.Shared;
using FabOS.WebServer.Data.Contexts;
using FabOS.WebServer.Models.Entities;
using FabOS.WebServer.Models;
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


    // Table columns configuration
    private List<GenericTableView<TraceDrawing>.TableColumn<TraceDrawing>> tableColumns = new()
    {
        new() { Header = "Name", ValueSelector = item => item.DrawingNumber ?? item.FileName, IsSortable = true },
        new() { Header = "Type", ValueSelector = item => item.FileType, IsSortable = true },
        new() { Header = "Status", ValueSelector = item => item.ProcessingStatus, IsSortable = true },
        new() { Header = "Scale", ValueSelector = item => item.Scale?.ToString() ?? "N/A", IsSortable = false },
        new() { Header = "Upload Date", ValueSelector = item => item.UploadDate.ToString("MMM dd, yyyy"), IsSortable = true }
    };


    protected override async Task OnInitializedAsync()
    {
        await LoadTakeoffs();
    }


    private async Task LoadTakeoffs()
    {
        try
        {
            isLoading = true;
            allTakeoffs = await DbContext.TraceDrawings
                .OrderByDescending(t => t.UploadDate)
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
                Label = "New Takeoff",
                Text = "New Takeoff",
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