using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.EntityFrameworkCore;
using FabOS.WebServer.Components.Shared.Interfaces;
using FabOS.WebServer.Components.Shared;
using FabOS.WebServer.Data.Contexts;
using FabOS.WebServer.Models.Entities;
using FabOS.WebServer.Models;
using FabOS.WebServer.Models.Columns;
using FabOS.WebServer.Models.ViewState;
using System.Security.Claims;
// Resolve ambiguous ToolbarAction reference
using ToolbarAction = FabOS.WebServer.Components.Shared.Interfaces.ToolbarAction;

namespace FabOS.WebServer.Components.Pages.Estimate;

public partial class Estimations : ComponentBase, IToolbarActionProvider
{
    [Parameter] public string? TenantSlug { get; set; }

    [Inject] private ApplicationDbContext Context { get; set; } = default!;
    [Inject] private NavigationManager Navigation { get; set; } = default!;
    [Inject] private AuthenticationStateProvider AuthenticationStateProvider { get; set; } = default!;
    [Inject] private ILogger<Estimations> Logger { get; set; } = default!;

    private bool isLoading = true;
    private string? errorMessage;
    private string searchTerm = "";
    private GenericViewSwitcher<Estimation>.ViewType currentView = GenericViewSwitcher<Estimation>.ViewType.Table;
    private List<Estimation> allEstimations = new();
    private List<Estimation> filteredEstimations = new();
    private List<Estimation> selectedTableItems = new();
    private List<Estimation> selectedListItems = new();
    private List<Estimation> selectedCardItems = new();
    private List<Customer> customers = new();
    private List<Takeoff> takeoffs = new();

    // Authentication state
    private int currentUserId = 0;
    private int currentCompanyId = 0;

    // View state management
    private ViewState? currentViewState;
    private bool hasUnsavedChanges = false;
    private bool hasCustomColumnConfig = false;
    private List<ColumnDefinition> managedColumns = new();

    // Table columns configuration
    private List<GenericTableView<Estimation>.TableColumn<Estimation>> tableColumns = new()
    {
        new() { Header = "Estimation #", ValueSelector = item => item.EstimationNumber ?? "N/A", IsSortable = true },
        new() { Header = "Name", ValueSelector = item => item.Name ?? "N/A", IsSortable = true },
        new() { Header = "Customer", ValueSelector = item => item.Customer?.Name ?? "N/A", IsSortable = true },
        new() { Header = "Revision", ValueSelector = item => item.CurrentRevisionLetter ?? "A", IsSortable = false },
        new() { Header = "Status", ValueSelector = item => item.Status ?? "Draft", IsSortable = true },
        new() { Header = "Total", ValueSelector = item => item.CurrentTotal.ToString("C"), IsSortable = true },
        new() { Header = "Created", ValueSelector = item => item.CreatedDate.ToString("MMM dd, yyyy"), IsSortable = true }
    };

    // Missing template properties for Estimations.razor
    private RenderFragment<Estimation>? TableActionsTemplate => item => builder =>
    {
        builder.OpenElement(0, "div");
        builder.AddAttribute(1, "class", "btn-group btn-group-sm");
        builder.OpenElement(2, "button");
        builder.AddAttribute(3, "class", "btn btn-outline-primary btn-sm");
        builder.AddAttribute(4, "title", "View");
        builder.AddAttribute(5, "onclick", EventCallback.Factory.Create(this, () => OpenEstimation(item.Id)));
        builder.OpenElement(6, "i");
        builder.AddAttribute(7, "class", "fas fa-eye");
        builder.CloseElement();
        builder.CloseElement();
        builder.OpenElement(8, "button");
        builder.AddAttribute(9, "class", "btn btn-outline-secondary btn-sm");
        builder.AddAttribute(10, "title", "Duplicate");
        builder.AddAttribute(11, "onclick", EventCallback.Factory.Create(this, () => DuplicateEstimation(item.Id)));
        builder.OpenElement(12, "i");
        builder.AddAttribute(13, "class", "fas fa-copy");
        builder.CloseElement();
        builder.CloseElement();
        builder.CloseElement();
    };

    private RenderFragment<Estimation> ListIconTemplate => item => builder =>
    {
        builder.OpenElement(0, "i");
        builder.AddAttribute(1, "class", "fas fa-calculator");
        builder.CloseElement();
    };

    private RenderFragment<Estimation> ListTitleTemplate => item => builder =>
    {
        builder.AddContent(0, item.EstimationNumber ?? "N/A");
    };

    private RenderFragment<Estimation> ListSubtitleTemplate => item => builder =>
    {
        builder.AddContent(0, item.Name ?? "No description");
    };

    private RenderFragment<Estimation> ListStatusTemplate => item => builder =>
    {
        builder.OpenElement(0, "span");
        builder.AddAttribute(1, "class", $"badge {GetStatusBadgeClass(item.Status)}");
        builder.AddContent(2, item.Status);
        builder.CloseElement();
    };

    private RenderFragment<Estimation> ListDetailsTemplate => item => builder =>
    {
        builder.AddContent(0, $"Customer: {item.Customer?.Name ?? "N/A"} | Total: {item.CurrentTotal.ToString("C")}");
    };

    // Create dialog
    private bool showCreateDialog = false;
    private Estimation newEstimation = new();
    private decimal defaultOverhead = 15m;
    private decimal defaultMargin = 20m;

    protected override async Task OnInitializedAsync()
    {
        var authState = await AuthenticationStateProvider.GetAuthenticationStateAsync();
        var user = authState.User;

        if (user.Identity?.IsAuthenticated == true)
        {
            var userIdClaim = user.FindFirst("user_id") ?? user.FindFirst("UserId") ?? user.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim != null && int.TryParse(userIdClaim.Value, out var userId))
            {
                currentUserId = userId;
            }

            var companyIdClaim = user.FindFirst("company_id") ?? user.FindFirst("CompanyId");
            if (companyIdClaim != null && int.TryParse(companyIdClaim.Value, out var companyId))
            {
                currentCompanyId = companyId;
            }
        }

        if (currentUserId == 0 || currentCompanyId == 0)
        {
            Logger.LogWarning("User is not authenticated or missing required claims for Estimations page");
            errorMessage = "User is not authenticated. Please log in and try again.";
            isLoading = false;
            return;
        }

        InitializeColumns();
        await LoadEstimations();
        await LoadCustomersAndTakeoffs();
    }

    private void InitializeColumns()
    {
        managedColumns = new List<ColumnDefinition>
        {
            new ColumnDefinition
            {
                PropertyName = "EstimationNumber",
                DisplayName = "Estimation #",
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
                PropertyName = "Name",
                DisplayName = "Name",
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
                PropertyName = "Customer.Name",
                DisplayName = "Customer",
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
                PropertyName = "CurrentRevisionLetter",
                DisplayName = "Revision",
                Type = ColumnType.Text,
                IsVisible = true,
                IsSortable = false,
                IsResizable = true,
                IsReorderable = true,
                Width = 100,
                Order = 3
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
                Width = 120,
                Order = 4
            },
            new ColumnDefinition
            {
                PropertyName = "CurrentTotal",
                DisplayName = "Total",
                Type = ColumnType.Currency,
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
                DisplayName = "Created",
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

    private async Task LoadEstimations()
    {
        try
        {
            isLoading = true;

            allEstimations = await Context.Estimations
                .Include(e => e.Customer)
                .Include(e => e.Revisions.Where(r => !r.IsDeleted))
                .Where(e => e.CompanyId == currentCompanyId && !e.IsDeleted)
                .OrderByDescending(e => e.CreatedDate)
                .ToListAsync();

            FilterEstimations();
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error loading estimations");
            errorMessage = "Error loading estimations. Please try again.";
        }
        finally
        {
            isLoading = false;
        }
    }

    private async Task LoadCustomersAndTakeoffs()
    {
        try
        {
            customers = await Context.Customers
                .Where(c => c.CompanyId == currentCompanyId && c.IsActive)
                .OrderBy(c => c.Name)
                .ToListAsync();

            takeoffs = await Context.Takeoffs
                .Where(t => t.CompanyId == currentCompanyId && t.ProcessingStatus == "Completed")
                .OrderByDescending(t => t.CreatedDate)
                .Take(50)
                .ToListAsync();
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error loading customers and takeoffs");
        }
    }

    private void FilterEstimations()
    {
        if (string.IsNullOrWhiteSpace(searchTerm))
        {
            filteredEstimations = new List<Estimation>(allEstimations);
        }
        else
        {
            var term = searchTerm.ToLower();
            filteredEstimations = allEstimations.Where(e =>
                (e.EstimationNumber != null && e.EstimationNumber.ToLower().Contains(term)) ||
                (e.Name != null && e.Name.ToLower().Contains(term)) ||
                (e.Customer != null && e.Customer.Name != null && e.Customer.Name.ToLower().Contains(term)) ||
                (e.Description != null && e.Description.ToLower().Contains(term))
            ).ToList();
        }
    }

    private void OnSearchChanged(string newSearchTerm)
    {
        searchTerm = newSearchTerm;
        FilterEstimations();
        StateHasChanged();
    }

    private void OnViewChanged(GenericViewSwitcher<Estimation>.ViewType newView)
    {
        currentView = newView;
        StateHasChanged();
    }

    private void HandleTableSelectionChanged(List<Estimation> selected)
    {
        selectedTableItems = selected;
        StateHasChanged();
    }

    private void HandleListSelectionChanged(List<Estimation> selected)
    {
        selectedListItems = selected;
        StateHasChanged();
    }

    private void HandleCardSelectionChanged(List<Estimation> selected)
    {
        selectedCardItems = selected;
        StateHasChanged();
    }

    private void HandleRowClick(Estimation item)
    {
        OpenEstimation(item.Id);
    }

    private void HandleRowDoubleClick(Estimation item)
    {
        OpenEstimation(item.Id);
    }

    private void OnSelectionChanged((Estimation item, bool selected) args)
    {
        if (args.selected)
        {
            if (!selectedListItems.Contains(args.item))
                selectedListItems.Add(args.item);
        }
        else
        {
            selectedListItems.Remove(args.item);
        }
        StateHasChanged();
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

    private void OpenEstimation(int id)
    {
        Navigation.NavigateTo($"/{TenantSlug}/estimate/estimations/{id}");
    }

    private void CreateNew()
    {
        newEstimation = new Estimation();
        defaultOverhead = 15m;
        defaultMargin = 20m;
        showCreateDialog = true;
    }

    private void CloseCreateDialog()
    {
        showCreateDialog = false;
        newEstimation = new();
    }

    private async Task SaveNewEstimation()
    {
        if (string.IsNullOrWhiteSpace(newEstimation.Name))
            return;

        try
        {
            // Generate estimation number
            var year = DateTime.UtcNow.Year;
            var prefix = $"EST-{year}-";
            var lastNumber = await Context.Estimations
                .Where(e => e.CompanyId == currentCompanyId && e.EstimationNumber.StartsWith(prefix))
                .OrderByDescending(e => e.EstimationNumber)
                .Select(e => e.EstimationNumber)
                .FirstOrDefaultAsync();

            int nextSeq = 1;
            if (lastNumber != null)
            {
                var seqStr = lastNumber.Replace(prefix, "");
                if (int.TryParse(seqStr, out var seq))
                    nextSeq = seq + 1;
            }

            var estimation = new Estimation
            {
                CompanyId = currentCompanyId,
                EstimationNumber = $"{prefix}{nextSeq:D4}",
                Name = newEstimation.Name,
                Description = newEstimation.Description,
                CustomerId = newEstimation.CustomerId,
                SourceTakeoffId = newEstimation.SourceTakeoffId,
                Status = "Draft",
                CurrentRevisionLetter = "A",
                CreatedBy = currentUserId,
                CreatedDate = DateTime.UtcNow
            };

            Context.Estimations.Add(estimation);
            await Context.SaveChangesAsync();

            // Create initial revision A
            var revision = new EstimationRevision
            {
                CompanyId = currentCompanyId,
                EstimationId = estimation.Id,
                RevisionLetter = "A",
                Status = "Draft",
                ValidUntilDate = DateTime.UtcNow.AddDays(30),
                OverheadPercentage = defaultOverhead,
                MarginPercentage = defaultMargin,
                CreatedBy = currentUserId,
                CreatedDate = DateTime.UtcNow
            };

            Context.EstimationRevisions.Add(revision);
            await Context.SaveChangesAsync();

            Logger.LogInformation("Created estimation {EstimationNumber}", estimation.EstimationNumber);

            showCreateDialog = false;
            Navigation.NavigateTo($"/{TenantSlug}/estimate/estimations/{estimation.Id}");
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error creating estimation");
            errorMessage = "Error creating estimation. Please try again.";
        }
    }

    private List<Estimation> GetSelectedEstimations()
    {
        return currentView switch
        {
            GenericViewSwitcher<Estimation>.ViewType.Table => selectedTableItems,
            GenericViewSwitcher<Estimation>.ViewType.List => selectedListItems,
            GenericViewSwitcher<Estimation>.ViewType.Card => selectedCardItems,
            _ => new List<Estimation>()
        };
    }

    private async Task DeleteSelected()
    {
        var selected = GetSelectedEstimations();
        if (!selected.Any())
            return;

        try
        {
            foreach (var estimation in selected)
            {
                estimation.IsDeleted = true;
                estimation.ModifiedBy = currentUserId;
                estimation.ModifiedDate = DateTime.UtcNow;
            }

            await Context.SaveChangesAsync();
            Logger.LogInformation("Deleted {Count} estimations", selected.Count);

            selectedTableItems.Clear();
            selectedListItems.Clear();
            selectedCardItems.Clear();
            await LoadEstimations();
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error deleting estimations");
            errorMessage = "Error deleting estimations. Please try again.";
        }
    }

    private async Task DuplicateEstimation(int id)
    {
        try
        {
            var source = await Context.Estimations
                .FirstOrDefaultAsync(e => e.Id == id && e.CompanyId == currentCompanyId && !e.IsDeleted);

            if (source == null)
                return;

            // Generate new estimation number
            var year = DateTime.UtcNow.Year;
            var prefix = $"EST-{year}-";
            var lastNumber = await Context.Estimations
                .Where(e => e.CompanyId == currentCompanyId && e.EstimationNumber.StartsWith(prefix))
                .OrderByDescending(e => e.EstimationNumber)
                .Select(e => e.EstimationNumber)
                .FirstOrDefaultAsync();

            int nextSeq = 1;
            if (lastNumber != null)
            {
                var seqStr = lastNumber.Replace(prefix, "");
                if (int.TryParse(seqStr, out var seq))
                    nextSeq = seq + 1;
            }

            var newEstimation = new Estimation
            {
                CompanyId = currentCompanyId,
                EstimationNumber = $"{prefix}{nextSeq:D4}",
                Name = $"{source.Name} (Copy)",
                Description = source.Description,
                CustomerId = source.CustomerId,
                Status = "Draft",
                CurrentRevisionLetter = "A",
                CreatedBy = currentUserId,
                CreatedDate = DateTime.UtcNow
            };

            Context.Estimations.Add(newEstimation);
            await Context.SaveChangesAsync();

            // Create initial revision
            var revision = new EstimationRevision
            {
                CompanyId = currentCompanyId,
                EstimationId = newEstimation.Id,
                RevisionLetter = "A",
                Status = "Draft",
                ValidUntilDate = DateTime.UtcNow.AddDays(30),
                OverheadPercentage = 15m,
                MarginPercentage = 20m,
                CreatedBy = currentUserId,
                CreatedDate = DateTime.UtcNow
            };

            Context.EstimationRevisions.Add(revision);
            await Context.SaveChangesAsync();

            Logger.LogInformation("Duplicated estimation {SourceId} to {NewId}", id, newEstimation.Id);

            await LoadEstimations();
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error duplicating estimation {Id}", id);
            errorMessage = "Error duplicating estimation. Please try again.";
        }
    }

    private string GetStatusBadgeClass(string status)
    {
        return status switch
        {
            "Draft" => "bg-secondary",
            "InProgress" => "bg-primary",
            "SubmittedForReview" => "bg-warning text-dark",
            "Approved" => "bg-success",
            "Rejected" => "bg-danger",
            "Sent" => "bg-info",
            "Accepted" => "bg-success",
            "CustomerRejected" => "bg-danger",
            _ => "bg-secondary"
        };
    }

    #region IToolbarActionProvider Implementation

    public ToolbarActionGroup GetActions()
    {
        var selected = GetSelectedEstimations();
        var hasSelection = selected.Any();

        return new ToolbarActionGroup
        {
            PrimaryActions = new List<ToolbarAction>
            {
                new()
                {
                    Label = "New",
                    Text = "New",
                    Icon = "fas fa-plus",
                    ActionFunc = async () => { CreateNew(); await Task.CompletedTask; },
                    Style = ToolbarActionStyle.Primary
                },
                new()
                {
                    Label = "Delete",
                    Text = "Delete",
                    Icon = "fas fa-trash",
                    ActionFunc = DeleteSelected,
                    IsDisabled = !hasSelection,
                    Style = ToolbarActionStyle.Danger
                }
            },
            MenuActions = new List<ToolbarAction>
            {
                new()
                {
                    Label = "Export",
                    Text = "Export to Excel",
                    Icon = "fas fa-file-excel",
                    ActionFunc = async () => { await Task.CompletedTask; /* TODO: Export */ }
                }
            },
            RelatedActions = new List<ToolbarAction>
            {
                new()
                {
                    Label = "Templates",
                    Text = "Worksheet Templates",
                    Icon = "fas fa-table",
                    ActionFunc = async () => {
                        Navigation.NavigateTo($"/{TenantSlug}/estimate/templates");
                        await Task.CompletedTask;
                    }
                },
                new()
                {
                    Label = "Customers",
                    Text = "View Customers",
                    Icon = "fas fa-building",
                    ActionFunc = async () => {
                        Navigation.NavigateTo($"/{TenantSlug}/trace/customers");
                        await Task.CompletedTask;
                    }
                }
            }
        };
    }

    #endregion
}
