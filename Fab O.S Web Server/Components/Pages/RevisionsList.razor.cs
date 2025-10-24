using Microsoft.AspNetCore.Components;
using Microsoft.EntityFrameworkCore;
using FabOS.WebServer.Data.Contexts;
using FabOS.WebServer.Models.Entities;
using FabOS.WebServer.Components.Shared.Interfaces;
using FabOS.WebServer.Components.Shared;
using FabOS.WebServer.Services.Interfaces;
using FabOS.WebServer.Models.Columns;
using FabOS.WebServer.Models.ViewState;
using ModelToolbarAction = FabOS.WebServer.Models.ToolbarAction;

namespace FabOS.WebServer.Components.Pages;

public partial class RevisionsList : ComponentBase, IToolbarActionProvider
{
    [Parameter] public string? TenantSlug { get; set; }
    [Parameter] public int TakeoffId { get; set; }

    [Inject] private ApplicationDbContext DbContext { get; set; } = default!;
    [Inject] private NavigationManager Navigation { get; set; } = default!;
    [Inject] private ITakeoffRevisionService RevisionService { get; set; } = default!;

    private List<TakeoffRevision> allRevisions = new();
    private List<TakeoffRevision> filteredRevisions = new();
    private TraceDrawing? takeoff;
    private bool isLoading = true;
    private string? errorMessage;
    private string searchTerm = "";

    // View management
    private GenericViewSwitcher<TakeoffRevision>.ViewType currentView = GenericViewSwitcher<TakeoffRevision>.ViewType.Table;
    private List<TakeoffRevision> selectedTableItems = new();
    private List<TakeoffRevision> selectedListItems = new();
    private List<TakeoffRevision> selectedCardItems = new();

    // View state management
    private ViewState? currentViewState;
    private bool hasUnsavedChanges = false;
    private bool hasCustomColumnConfig = false;
    private List<ColumnDefinition> managedColumns = new();

    // Table columns configuration
    private List<GenericTableView<TakeoffRevision>.TableColumn<TakeoffRevision>> tableColumns = new()
    {
        new() { Header = "Revision Code", ValueSelector = item => item.RevisionCode, IsSortable = true },
        new() { Header = "Description", ValueSelector = item => item.Description ?? "N/A", IsSortable = true },
        new() { Header = "Status", ValueSelector = item => item.IsActive ? "Active" : "Inactive", IsSortable = true },
        new() { Header = "Packages", ValueSelector = item => (item.Packages?.Count ?? 0).ToString(), IsSortable = true },
        new() { Header = "Created Date", ValueSelector = item => item.CreatedDate.ToString("MMM dd, yyyy"), IsSortable = true },
        new() { Header = "Created By", ValueSelector = item => item.CreatedByUser?.Username ?? "Unknown", IsSortable = false }
    };

    protected override async Task OnInitializedAsync()
    {
        InitializeColumns();
        await LoadRevisions();
    }

    protected override async Task OnParametersSetAsync()
    {
        if (TakeoffId != takeoff?.Id)
        {
            await LoadRevisions();
        }
    }

    private void InitializeColumns()
    {
        managedColumns = new List<ColumnDefinition>
        {
            new ColumnDefinition
            {
                PropertyName = "RevisionCode",
                DisplayName = "Revision Code",
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
                Width = 300,
                Order = 1
            },
            new ColumnDefinition
            {
                PropertyName = "IsActive",
                DisplayName = "Status",
                Type = ColumnType.Status,
                IsVisible = true,
                IsSortable = true,
                IsResizable = true,
                IsReorderable = true,
                Width = 120,
                Order = 2
            },
            new ColumnDefinition
            {
                PropertyName = "PackagesCount",
                DisplayName = "Packages",
                Type = ColumnType.Number,
                IsVisible = true,
                IsSortable = true,
                IsResizable = true,
                IsReorderable = true,
                Width = 100,
                Order = 3
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
                Order = 4,
                Format = "MMM dd, yyyy"
            },
            new ColumnDefinition
            {
                PropertyName = "CreatedBy",
                DisplayName = "Created By",
                Type = ColumnType.Text,
                IsVisible = true,
                IsSortable = false,
                IsResizable = true,
                IsReorderable = true,
                Width = 150,
                Order = 5
            }
        };
    }

    private async Task LoadRevisions()
    {
        isLoading = true;
        errorMessage = null;

        try
        {
            // Load takeoff
            takeoff = await DbContext.TraceDrawings.FindAsync(TakeoffId);
            if (takeoff == null)
            {
                errorMessage = "Takeoff not found";
                return;
            }

            // Load revisions with related data
            allRevisions = await RevisionService.GetRevisionsByTakeoffAsync(TakeoffId);
            FilterRevisions();
        }
        catch (Exception ex)
        {
            errorMessage = $"Error loading revisions: {ex.Message}";
        }
        finally
        {
            isLoading = false;
        }
    }

    private void FilterRevisions()
    {
        if (string.IsNullOrWhiteSpace(searchTerm))
        {
            filteredRevisions = allRevisions;
        }
        else
        {
            var search = searchTerm.ToLower();
            filteredRevisions = allRevisions.Where(r =>
                (r.RevisionCode?.ToLower().Contains(search) ?? false) ||
                (r.Description?.ToLower().Contains(search) ?? false) ||
                (r.CreatedByUser?.Username?.ToLower().Contains(search) ?? false)
            ).ToList();
        }
    }

    private void OnSearchChanged(string newSearchTerm)
    {
        searchTerm = newSearchTerm;
        FilterRevisions();
        StateHasChanged();
    }

    private void OnViewChanged(GenericViewSwitcher<TakeoffRevision>.ViewType newView)
    {
        currentView = newView;
        StateHasChanged();
    }

    private void HandleViewLoaded(ViewState viewState)
    {
        currentViewState = viewState;
        StateHasChanged();
    }

    private void HandleColumnsChanged(List<ColumnDefinition> columns)
    {
        managedColumns = columns;
        hasCustomColumnConfig = true;
        hasUnsavedChanges = true;
        StateHasChanged();
    }

    private void HandleTableSelectionChanged(List<TakeoffRevision> selectedItems)
    {
        selectedTableItems = selectedItems;
        StateHasChanged();
    }

    private void HandleListSelectionChanged(List<TakeoffRevision> selectedItems)
    {
        selectedListItems = selectedItems;
        StateHasChanged();
    }

    private void HandleCardSelectionChanged(List<TakeoffRevision> selectedItems)
    {
        selectedCardItems = selectedItems;
        StateHasChanged();
    }

    private void HandleRowClick(TakeoffRevision revision)
    {
        // Single click - select the item
        StateHasChanged();
    }

    private void HandleRowDoubleClick(TakeoffRevision revision)
    {
        // Double click - navigate to revision
        NavigateToRevision(revision.Id);
    }

    private void OpenRevision(int revisionId)
    {
        NavigateToRevision(revisionId);
    }

    private void NavigateToRevision(int revisionId)
    {
        Navigation.NavigateTo($"/{TenantSlug}/trace/takeoffs/{TakeoffId}/revisions/{revisionId}");
    }

    private void CreateRevision()
    {
        Navigation.NavigateTo($"/{TenantSlug}/trace/takeoffs/{TakeoffId}/revisions/new");
    }

    private async Task DeleteSelectedRevisions()
    {
        // This would be implemented if we add multi-select capability
        await Task.CompletedTask;
    }

    // IToolbarActionProvider implementation
    public ToolbarActionGroup GetActions()
    {
        Console.WriteLine("[RevisionsList] GetActions called");
        var actionGroup = new ToolbarActionGroup();

        // Primary Actions
        var newAction = new ToolbarAction
        {
            Text = "New",
            Label = "New",
            Icon = "fas fa-plus",
            ActionFunc = () => { CreateRevision(); return Task.CompletedTask; },
            Style = ToolbarActionStyle.Primary,
            Tooltip = "Create a new revision",
            IsDisabled = false
        };
        Console.WriteLine($"[RevisionsList] Creating New action - Label='{newAction.Label}', IsDisabled={newAction.IsDisabled}");
        actionGroup.PrimaryActions.Add(newAction);

        // Related Actions
        actionGroup.RelatedActions.Add(new ToolbarAction
        {
            Text = "View Takeoff",
            Icon = "fas fa-file-alt",
            ActionFunc = () => { Navigation.NavigateTo($"/{TenantSlug}/trace/takeoffs/{TakeoffId}"); return Task.CompletedTask; }
        });

        if (allRevisions.Any())
        {
            var activeRevision = allRevisions.FirstOrDefault(r => r.IsActive);
            if (activeRevision != null)
            {
                actionGroup.RelatedActions.Add(new ToolbarAction
                {
                    Text = "Active Revision Packages",
                    Icon = "fas fa-box",
                    ActionFunc = () => { Navigation.NavigateTo($"/{TenantSlug}/trace/takeoffs/{TakeoffId}/packages"); return Task.CompletedTask; },
                    Tooltip = "View packages in the active revision"
                });
            }
        }

        return actionGroup;
    }
}
