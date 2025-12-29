using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.EntityFrameworkCore;
using FabOS.WebServer.Data.Contexts;
using FabOS.WebServer.Models.Entities;
using FabOS.WebServer.Components.Shared.Interfaces;
using FabOS.WebServer.Components.Shared;
using FabOS.WebServer.Models.Columns;
using FabOS.WebServer.Models.ViewState;
using System.Security.Claims;
using ToolbarAction = FabOS.WebServer.Components.Shared.Interfaces.ToolbarAction;

namespace FabOS.WebServer.Components.Pages.Estimate;

public partial class EstimationRevisionsList : ComponentBase, IToolbarActionProvider
{
    [Parameter] public string? TenantSlug { get; set; }
    [Parameter] public int EstimationId { get; set; }

    [Inject] private ApplicationDbContext DbContext { get; set; } = default!;
    [Inject] private NavigationManager Navigation { get; set; } = default!;
    [Inject] private AuthenticationStateProvider AuthenticationStateProvider { get; set; } = default!;
    [Inject] private ILogger<EstimationRevisionsList> Logger { get; set; } = default!;

    private List<EstimationRevision> allRevisions = new();
    private List<EstimationRevision> filteredRevisions = new();
    private Estimation? estimation;
    private bool isLoading = true;
    private string? errorMessage;
    private string searchTerm = "";
    private int currentCompanyId = 0;

    // View management
    private GenericViewSwitcher<EstimationRevision>.ViewType currentView = GenericViewSwitcher<EstimationRevision>.ViewType.Table;
    private List<EstimationRevision> selectedTableItems = new();
    private List<EstimationRevision> selectedListItems = new();
    private List<EstimationRevision> selectedCardItems = new();

    // View state management
    private ViewState? currentViewState;
    private bool hasUnsavedChanges = false;
    private bool hasCustomColumnConfig = false;
    private List<ColumnDefinition> managedColumns = new();

    // Table columns configuration
    private List<GenericTableView<EstimationRevision>.TableColumn<EstimationRevision>> tableColumns = new()
    {
        new() { Header = "Revision", ValueSelector = item => item.RevisionLetter, IsSortable = true },
        new() { Header = "Status", ValueSelector = item => item.Status ?? "Draft", IsSortable = true },
        new() { Header = "Packages", ValueSelector = item => (item.Packages?.Count(p => !p.IsDeleted) ?? 0).ToString(), IsSortable = true },
        new() { Header = "Total", ValueSelector = item => item.TotalAmount.ToString("C"), IsSortable = true },
        new() { Header = "Valid Until", ValueSelector = item => item.ValidUntilDate?.ToString("MMM dd, yyyy") ?? "-", IsSortable = true },
        new() { Header = "Created", ValueSelector = item => item.CreatedDate.ToString("MMM dd, yyyy"), IsSortable = true }
    };

    protected override async Task OnInitializedAsync()
    {
        var authState = await AuthenticationStateProvider.GetAuthenticationStateAsync();
        var user = authState.User;

        if (user.Identity?.IsAuthenticated == true)
        {
            var companyIdClaim = user.FindFirst("company_id") ?? user.FindFirst("CompanyId");
            if (companyIdClaim != null && int.TryParse(companyIdClaim.Value, out var companyId))
            {
                currentCompanyId = companyId;
            }
        }

        if (currentCompanyId == 0)
        {
            errorMessage = "User is not authenticated. Please log in and try again.";
            isLoading = false;
            return;
        }

        InitializeColumns();
        await LoadRevisions();
    }

    protected override async Task OnParametersSetAsync()
    {
        if (EstimationId != estimation?.Id)
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
                PropertyName = "RevisionLetter",
                DisplayName = "Revision",
                Type = ColumnType.Text,
                IsVisible = true,
                IsSortable = true,
                IsResizable = true,
                IsReorderable = true,
                Width = 120,
                Order = 0
            },
            new ColumnDefinition
            {
                PropertyName = "Status",
                DisplayName = "Status",
                Type = ColumnType.Status,
                IsVisible = true,
                IsSortable = true,
                IsResizable = true,
                IsReorderable = true,
                Width = 150,
                Order = 1
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
                Order = 2
            },
            new ColumnDefinition
            {
                PropertyName = "TotalAmount",
                DisplayName = "Total",
                Type = ColumnType.Currency,
                IsVisible = true,
                IsSortable = true,
                IsResizable = true,
                IsReorderable = true,
                Width = 120,
                Order = 3
            },
            new ColumnDefinition
            {
                PropertyName = "ValidUntilDate",
                DisplayName = "Valid Until",
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
                PropertyName = "CreatedDate",
                DisplayName = "Created",
                Type = ColumnType.Date,
                IsVisible = true,
                IsSortable = true,
                IsResizable = true,
                IsReorderable = true,
                Width = 150,
                Order = 5,
                Format = "MMM dd, yyyy"
            }
        };
    }

    private async Task LoadRevisions()
    {
        isLoading = true;
        errorMessage = null;

        try
        {
            // Load estimation
            estimation = await DbContext.Estimations
                .Include(e => e.Revisions.Where(r => !r.IsDeleted))
                    .ThenInclude(r => r.Packages.Where(p => !p.IsDeleted))
                .FirstOrDefaultAsync(e => e.Id == EstimationId && e.CompanyId == currentCompanyId && !e.IsDeleted);

            if (estimation == null)
            {
                errorMessage = "Estimation not found";
                return;
            }

            // Load revisions
            allRevisions = estimation.Revisions
                .Where(r => !r.IsDeleted)
                .OrderByDescending(r => r.RevisionLetter == estimation.CurrentRevisionLetter)
                .ThenByDescending(r => r.RevisionLetter)
                .ToList();

            FilterRevisions();
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error loading revisions for estimation {EstimationId}", EstimationId);
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
                (r.RevisionLetter?.ToLower().Contains(search) ?? false) ||
                (r.Status?.ToLower().Contains(search) ?? false)
            ).ToList();
        }
    }

    private void OnSearchChanged(string newSearchTerm)
    {
        searchTerm = newSearchTerm;
        FilterRevisions();
        StateHasChanged();
    }

    private void OnViewChanged(GenericViewSwitcher<EstimationRevision>.ViewType newView)
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

    private void HandleTableSelectionChanged(List<EstimationRevision> selectedItems)
    {
        selectedTableItems = selectedItems;
        StateHasChanged();
    }

    private void HandleListSelectionChanged(List<EstimationRevision> selectedItems)
    {
        selectedListItems = selectedItems;
        StateHasChanged();
    }

    private void HandleCardSelectionChanged(List<EstimationRevision> selectedItems)
    {
        selectedCardItems = selectedItems;
        StateHasChanged();
    }

    private void HandleRowClick(EstimationRevision revision)
    {
        // Single click - select the item
        StateHasChanged();
    }

    private void HandleRowDoubleClick(EstimationRevision revision)
    {
        // Double click - navigate to revision
        NavigateToRevision(revision.Id);
    }

    private void NavigateToRevision(int revisionId)
    {
        Navigation.NavigateTo($"/{TenantSlug}/estimate/estimations/{EstimationId}/revisions/{revisionId}");
    }

    private void CreateRevision()
    {
        Navigation.NavigateTo($"/{TenantSlug}/estimate/estimations/{EstimationId}/revisions/new");
    }

    private string GetStatusBadgeClass(string? status)
    {
        return status?.ToLower() switch
        {
            "draft" => "status-draft",
            "inprogress" => "status-inprogress",
            "submittedforreview" => "status-submittedforreview",
            "inreview" => "status-inreview",
            "approved" => "status-approved",
            "rejected" => "status-rejected",
            "sent" => "status-sent",
            "accepted" => "status-accepted",
            "customerrejected" => "status-customerrejected",
            _ => "status-default"
        };
    }

    // IToolbarActionProvider implementation
    private List<EstimationRevision> GetSelectedRevisions()
    {
        return currentView switch
        {
            GenericViewSwitcher<EstimationRevision>.ViewType.Table => selectedTableItems,
            GenericViewSwitcher<EstimationRevision>.ViewType.List => selectedListItems,
            GenericViewSwitcher<EstimationRevision>.ViewType.Card => selectedCardItems,
            _ => new List<EstimationRevision>()
        };
    }

    private async Task DeleteSelectedRevisions()
    {
        var selected = GetSelectedRevisions();
        if (!selected.Any()) return;

        try
        {
            foreach (var revision in selected)
            {
                revision.IsDeleted = true;
                revision.ModifiedDate = DateTime.UtcNow;
            }

            await DbContext.SaveChangesAsync();
            await LoadRevisions();
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error deleting revisions");
            errorMessage = "Error deleting revisions. Please try again.";
        }
    }

    public ToolbarActionGroup GetActions()
    {
        var selected = GetSelectedRevisions();
        var hasSelection = selected.Any();
        var actionGroup = new ToolbarActionGroup();

        // Primary Actions
        actionGroup.PrimaryActions.Add(new ToolbarAction
        {
            Text = "New",
            Label = "New",
            Icon = "fas fa-plus",
            ActionFunc = () => { CreateRevision(); return Task.CompletedTask; },
            Style = ToolbarActionStyle.Primary,
            Tooltip = "Create a new revision"
        });

        actionGroup.PrimaryActions.Add(new ToolbarAction
        {
            Text = "Delete",
            Label = "Delete",
            Icon = "fas fa-trash",
            ActionFunc = DeleteSelectedRevisions,
            IsDisabled = !hasSelection,
            Style = ToolbarActionStyle.Danger,
            Tooltip = hasSelection ? "Delete selected revisions" : "Select revisions to delete"
        });

        // Related Actions
        actionGroup.RelatedActions.Add(new ToolbarAction
        {
            Text = "Back to Estimation",
            Label = "Back to Estimation",
            Icon = "fas fa-arrow-left",
            ActionFunc = () => { Navigation.NavigateTo($"/{TenantSlug}/estimate/estimations/{EstimationId}"); return Task.CompletedTask; },
            Tooltip = "Go back to estimation"
        });

        return actionGroup;
    }
}
