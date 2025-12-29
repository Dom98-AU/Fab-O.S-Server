using Microsoft.AspNetCore.Components;
using Microsoft.EntityFrameworkCore;
using FabOS.WebServer.Components.Shared;
using FabOS.WebServer.Components.Shared.Interfaces;
using FabOS.WebServer.Data.Contexts;
using FabOS.WebServer.Models.Entities;
using FabOS.WebServer.Models.Columns;
using FabOS.WebServer.Models.ViewState;
using FabOS.WebServer.Services;
using FabOS.WebServer.Services.Interfaces;

namespace FabOS.WebServer.Components.Pages;

public partial class Customers : ComponentBase, IToolbarActionProvider, IDisposable
{
    [Parameter]
    public string TenantSlug { get; set; } = string.Empty;

    [Inject] private ApplicationDbContext DbContext { get; set; } = default!;
    [Inject] private NavigationManager Navigation { get; set; } = default!;

    private List<Customer> customers = new();
    private List<Customer> allCustomers = new();
    private List<Customer> filteredCustomers = new();
    private string searchTerm = "";
    private bool isLoading = true;

    // View state management
    private GenericViewSwitcher<Customer>.ViewType currentView = GenericViewSwitcher<Customer>.ViewType.Table;
    private ViewState currentViewState = new();
    private bool hasUnsavedChanges = false;
    private bool hasCustomColumnConfig = false;

    // Column management
    private List<ColumnDefinition> managedColumns = new();
    private List<GenericTableView<Customer>.TableColumn<Customer>> tableColumns = new();

    // Selection tracking
    private List<Customer> selectedTableItems = new();
    private List<Customer> selectedListItems = new();
    private List<Customer> selectedCardItems = new();

    protected override async Task OnInitializedAsync()
    {
        InitializeColumns();
        await LoadCustomers();
    }


    private void InitializeColumns()
    {
        tableColumns = new List<GenericTableView<Customer>.TableColumn<Customer>>
        {
            new GenericTableView<Customer>.TableColumn<Customer>
            {
                Header = "Code",
                PropertyName = "Code",
                ValueSelector = c => c.Code ?? "-",
                IsSortable = true
            },
            new GenericTableView<Customer>.TableColumn<Customer>
            {
                Header = "Name",
                PropertyName = "Name",
                ValueSelector = c => c.Name ?? "",
                IsSortable = true
            },
            new GenericTableView<Customer>.TableColumn<Customer>
            {
                Header = "ABN",
                PropertyName = "ABN",
                ValueSelector = c => c.ABN ?? "-",
                IsSortable = true
            },
            new GenericTableView<Customer>.TableColumn<Customer>
            {
                Header = "Primary Contact",
                PropertyName = "ContactPerson",
                ValueSelector = c => c.ContactPerson ?? "-",
                IsSortable = true
            },
            new GenericTableView<Customer>.TableColumn<Customer>
            {
                Header = "Email",
                PropertyName = "Email",
                ValueSelector = c => c.Email ?? "-",
                IsSortable = true,
                Template = customer => builder =>
                {
                    if (!string.IsNullOrEmpty(customer.Email))
                    {
                        builder.OpenElement(0, "a");
                        builder.AddAttribute(1, "href", $"mailto:{customer.Email}");
                        builder.AddContent(2, customer.Email);
                        builder.CloseElement();
                    }
                    else
                    {
                        builder.AddContent(0, "-");
                    }
                }
            },
            new GenericTableView<Customer>.TableColumn<Customer>
            {
                Header = "Phone",
                PropertyName = "PhoneNumber",
                ValueSelector = c => c.PhoneNumber ?? "-",
                IsSortable = true
            },
            new GenericTableView<Customer>.TableColumn<Customer>
            {
                Header = "Industry",
                PropertyName = "Industry",
                ValueSelector = c => c.Industry ?? "-",
                IsSortable = true
            },
            new GenericTableView<Customer>.TableColumn<Customer>
            {
                Header = "Status",
                PropertyName = "IsActive",
                ValueSelector = c => c.IsActive ? "Active" : "Inactive",
                IsSortable = true,
                Template = customer => builder =>
                {
                    builder.OpenElement(0, "span");
                    builder.AddAttribute(1, "class", $"badge {(customer.IsActive ? "bg-success" : "bg-secondary")}");
                    builder.AddContent(2, customer.IsActive ? "Active" : "Inactive");
                    builder.CloseElement();
                }
            }
        };

        managedColumns = tableColumns.Select(c => new ColumnDefinition
        {
            PropertyName = c.PropertyName,
            DisplayName = c.Header,
            IsVisible = true,
            IsFrozen = false,
            IsRequired = false,
            Width = null
        }).ToList();
    }

    private async Task LoadCustomers()
    {
        try
        {
            isLoading = true;
            customers = await DbContext.Customers
                .OrderBy(c => c.Name)
                .ToListAsync();

            allCustomers = customers;
            ApplyFilters();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error loading customers: {ex.Message}");
        }
        finally
        {
            isLoading = false;
        }
    }

    private void ApplyFilters()
    {
        filteredCustomers = customers;

        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            var searchLower = searchTerm.ToLower();
            filteredCustomers = customers.Where(c =>
                (c.Name != null && c.Name.ToLower().Contains(searchLower)) ||
                (c.Code != null && c.Code.ToLower().Contains(searchLower)) ||
                (c.ABN != null && c.ABN.ToLower().Contains(searchLower)) ||
                (c.ContactPerson != null && c.ContactPerson.ToLower().Contains(searchLower)) ||
                (c.Email != null && c.Email.ToLower().Contains(searchLower)) ||
                (c.Industry != null && c.Industry.ToLower().Contains(searchLower))
            ).ToList();
        }
    }

    private void OnSearchChanged(string value)
    {
        searchTerm = value;
        ApplyFilters();
        StateHasChanged();
    }

    private void CreateNewCustomer()
    {
        Navigation.NavigateTo($"/{TenantSlug}/trace/customers/new");
    }

    private void ViewCustomer(int customerId)
    {
        Navigation.NavigateTo($"/{TenantSlug}/trace/customers/{customerId}");
    }

    private void EditCustomer(int customerId)
    {
        Navigation.NavigateTo($"/{TenantSlug}/trace/customers/{customerId}/edit");
    }

    private async Task DeleteCustomer(Customer customer)
    {
        // In a real application, you'd want to show a confirmation dialog
        try
        {
            DbContext.Customers.Remove(customer);
            await DbContext.SaveChangesAsync();
            await LoadCustomers();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error deleting customer: {ex.Message}");
            // In a real application, show error message to user
        }
    }

    // View management
    private void OnViewChanged(GenericViewSwitcher<Customer>.ViewType newView)
    {
        currentView = newView;
        StateHasChanged();
    }

    private async Task HandleViewLoaded(ViewState? state)
    {
        if (state == null)
        {
            InitializeColumns();
        }
        else
        {
            currentViewState = state;
            if (state.Columns.Any())
            {
                managedColumns = state.Columns;
            }
        }
        hasUnsavedChanges = false;
        StateHasChanged();
    }

    private async Task HandleColumnsChanged(List<ColumnDefinition>? columns)
    {
        if (columns == null)
        {
            InitializeColumns();
        }
        else
        {
            managedColumns = columns;
            hasCustomColumnConfig = true;
        }
        hasUnsavedChanges = true;
        StateHasChanged();
    }

    // Selection handling
    private async Task HandleTableSelectionChanged(List<Customer> items)
    {
        selectedTableItems = items;
        StateHasChanged();
    }

    private async Task HandleListSelectionChanged(List<Customer> items)
    {
        selectedListItems = items;
        StateHasChanged();
    }

    private async Task HandleCardSelectionChanged(List<Customer> items)
    {
        selectedCardItems = items;
        StateHasChanged();
    }

    // Item interaction
    private void HandleCustomerClick(Customer customer)
    {
        // Single click - could be used for selection or preview
    }

    private void HandleCustomerDoubleClick(Customer customer)
    {
        // Double click - navigate to customer details
        ViewCustomer(customer.Id);
    }

    private List<Customer> GetSelectedItems()
    {
        return currentView switch
        {
            GenericViewSwitcher<Customer>.ViewType.Table => selectedTableItems,
            GenericViewSwitcher<Customer>.ViewType.List => selectedListItems,
            GenericViewSwitcher<Customer>.ViewType.Card => selectedCardItems,
            _ => new List<Customer>()
        };
    }

    // IToolbarActionProvider implementation
    public ToolbarActionGroup GetActions()
    {
        var hasSelection = GetSelectedItems().Any();

        var group = new ToolbarActionGroup();
        group.PrimaryActions = new List<FabOS.WebServer.Components.Shared.Interfaces.ToolbarAction>
        {
            new FabOS.WebServer.Components.Shared.Interfaces.ToolbarAction
            {
                Label = "New",
                Text = "New",
                Icon = "fas fa-plus",
                Action = EventCallback.Factory.Create(this, CreateNewCustomer),
                IsDisabled = false,
                Style = FabOS.WebServer.Components.Shared.Interfaces.ToolbarActionStyle.Primary
            },
            new FabOS.WebServer.Components.Shared.Interfaces.ToolbarAction
            {
                Label = "Delete",
                Text = "Delete",
                Icon = "fas fa-trash",
                Action = EventCallback.Factory.Create(this, DeleteSelectedCustomers),
                IsDisabled = !hasSelection,
                Style = FabOS.WebServer.Components.Shared.Interfaces.ToolbarActionStyle.Danger
            }
        };

        group.MenuActions = new List<FabOS.WebServer.Components.Shared.Interfaces.ToolbarAction>
        {
            new FabOS.WebServer.Components.Shared.Interfaces.ToolbarAction
            {
                Label = "Refresh",
                Text = "Refresh",
                Icon = "fas fa-sync-alt",
                Action = EventCallback.Factory.Create(this, async () => await LoadCustomers()),
                IsDisabled = false
            }
        };

        group.RelatedActions = new List<FabOS.WebServer.Components.Shared.Interfaces.ToolbarAction>
        {
            new FabOS.WebServer.Components.Shared.Interfaces.ToolbarAction
            {
                Label = "Export to Excel",
                Text = "Export to Excel",
                Icon = "fas fa-file-excel",
                Action = EventCallback.Factory.Create(this, ExportToExcel),
                IsDisabled = false
            },
            new FabOS.WebServer.Components.Shared.Interfaces.ToolbarAction
            {
                Label = "Import from Excel",
                Text = "Import from Excel",
                Icon = "fas fa-file-import",
                Action = EventCallback.Factory.Create(this, ImportFromExcel),
                IsDisabled = false
            }
        };

        return group;
    }

    private async Task DeleteSelectedCustomers()
    {
        var selected = GetSelectedItems();
        if (!selected.Any()) return;

        try
        {
            DbContext.Customers.RemoveRange(selected);
            await DbContext.SaveChangesAsync();

            selectedTableItems.Clear();
            selectedListItems.Clear();
            selectedCardItems.Clear();

            await LoadCustomers();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error deleting customers: {ex.Message}");
        }
    }

    private void ExportToExcel()
    {
        Console.WriteLine("Export to Excel clicked");
    }

    private void ImportFromExcel()
    {
        Console.WriteLine("Import from Excel clicked");
    }

    public void Dispose()
    {
        // Cleanup if needed
    }
}