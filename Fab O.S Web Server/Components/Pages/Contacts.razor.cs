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
using Microsoft.AspNetCore.WebUtilities;

namespace FabOS.WebServer.Components.Pages;

public partial class Contacts : ComponentBase, IToolbarActionProvider, IDisposable
{
    [Parameter]
    public string TenantSlug { get; set; } = string.Empty;

    [Inject] private IDbContextFactory<ApplicationDbContext> DbContextFactory { get; set; } = default!;
    [Inject] private NavigationManager Navigation { get; set; } = default!;

    private List<CustomerContact> contacts = new();
    private List<CustomerContact> allContacts = new();
    private List<CustomerContact> filteredContacts = new();
    private string searchTerm = "";
    private bool isLoading = true;
    private int? filterByCustomerId = null;
    private Customer? filterCustomer = null;

    // View state management
    private GenericViewSwitcher<CustomerContact>.ViewType currentView = GenericViewSwitcher<CustomerContact>.ViewType.Table;
    private ViewState currentViewState = new();
    private bool hasUnsavedChanges = false;
    private bool hasCustomColumnConfig = false;

    // Column management
    private List<ColumnDefinition> managedColumns = new();
    private List<GenericTableView<CustomerContact>.TableColumn<CustomerContact>> tableColumns = new();

    // Selection tracking
    private List<CustomerContact> selectedTableItems = new();
    private List<CustomerContact> selectedListItems = new();
    private List<CustomerContact> selectedCardItems = new();

    protected override async Task OnInitializedAsync()
    {
        InitializeColumns();

        // Check for customerId query parameter
        var uri = Navigation.ToAbsoluteUri(Navigation.Uri);
        if (QueryHelpers.ParseQuery(uri.Query).TryGetValue("customerId", out var customerIdValue))
        {
            if (int.TryParse(customerIdValue, out int customerId))
            {
                filterByCustomerId = customerId;
                // Load customer details for display
                await using var dbContext = await DbContextFactory.CreateDbContextAsync();
                filterCustomer = await dbContext.Customers.FindAsync(customerId);
            }
        }

        await LoadContacts();
    }

    private void InitializeColumns()
    {
        tableColumns = new List<GenericTableView<CustomerContact>.TableColumn<CustomerContact>>
        {
            new GenericTableView<CustomerContact>.TableColumn<CustomerContact>
            {
                Header = "Contact Number",
                PropertyName = "ContactNumber",
                ValueSelector = c => c.ContactNumber ?? "-",
                IsSortable = true
            },
            new GenericTableView<CustomerContact>.TableColumn<CustomerContact>
            {
                Header = "Name",
                PropertyName = "FirstName",
                ValueSelector = c => $"{c.FirstName} {c.LastName}",
                IsSortable = true,
                Template = contact => builder =>
                {
                    builder.OpenElement(0, "div");
                    builder.OpenElement(1, "strong");
                    builder.AddContent(2, $"{contact.FirstName} {contact.LastName}");
                    builder.CloseElement();
                    if (contact.IsPrimary)
                    {
                        builder.OpenElement(3, "span");
                        builder.AddAttribute(4, "class", "ms-2 text-warning");
                        builder.AddAttribute(5, "title", "Primary Contact");
                        builder.AddContent(6, "★");
                        builder.CloseElement();
                    }
                    builder.CloseElement();
                }
            },
            new GenericTableView<CustomerContact>.TableColumn<CustomerContact>
            {
                Header = "Company",
                PropertyName = "CustomerId",
                ValueSelector = c => c.Customer?.Name ?? "-",
                IsSortable = true
            },
            new GenericTableView<CustomerContact>.TableColumn<CustomerContact>
            {
                Header = "Title",
                PropertyName = "Title",
                ValueSelector = c => c.Title ?? "-",
                IsSortable = true
            },
            new GenericTableView<CustomerContact>.TableColumn<CustomerContact>
            {
                Header = "Department",
                PropertyName = "Department",
                ValueSelector = c => c.Department ?? "-",
                IsSortable = true
            },
            new GenericTableView<CustomerContact>.TableColumn<CustomerContact>
            {
                Header = "Email",
                PropertyName = "Email",
                ValueSelector = c => c.Email ?? "-",
                IsSortable = true,
                Template = contact => builder =>
                {
                    if (!string.IsNullOrEmpty(contact.Email))
                    {
                        builder.OpenElement(0, "a");
                        builder.AddAttribute(1, "href", $"mailto:{contact.Email}");
                        builder.AddContent(2, contact.Email);
                        builder.CloseElement();
                    }
                    else
                    {
                        builder.AddContent(0, "-");
                    }
                }
            },
            new GenericTableView<CustomerContact>.TableColumn<CustomerContact>
            {
                Header = "Phone",
                PropertyName = "PhoneNumber",
                ValueSelector = c => c.PhoneNumber ?? "-",
                IsSortable = true
            },
            new GenericTableView<CustomerContact>.TableColumn<CustomerContact>
            {
                Header = "Mobile",
                PropertyName = "MobileNumber",
                ValueSelector = c => c.MobileNumber ?? "-",
                IsSortable = true
            },
            new GenericTableView<CustomerContact>.TableColumn<CustomerContact>
            {
                Header = "Status",
                PropertyName = "IsActive",
                ValueSelector = c => c.IsActive ? "Active" : "Inactive",
                IsSortable = true,
                Template = contact => builder =>
                {
                    builder.OpenElement(0, "span");
                    builder.AddAttribute(1, "class", $"badge {(contact.IsActive ? "bg-success" : "bg-secondary")}");
                    builder.AddContent(2, contact.IsActive ? "Active" : "Inactive");
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

    private async Task LoadContacts()
    {
        try
        {
            isLoading = true;
            await using var dbContext = await DbContextFactory.CreateDbContextAsync();
            var query = dbContext.CustomerContacts
                .Include(c => c.Customer)
                .AsQueryable();

            // Apply customer filter if specified
            if (filterByCustomerId.HasValue)
            {
                query = query.Where(c => c.CustomerId == filterByCustomerId.Value);
            }

            contacts = await query
                .OrderBy(c => c.FirstName)
                .ThenBy(c => c.LastName)
                .ToListAsync();

            allContacts = contacts;
            ApplyFilters();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error loading contacts: {ex.Message}");
        }
        finally
        {
            isLoading = false;
        }
    }

    private void ApplyFilters()
    {
        filteredContacts = contacts;

        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            var searchLower = searchTerm.ToLower();
            filteredContacts = contacts.Where(c =>
                (c.FirstName != null && c.FirstName.ToLower().Contains(searchLower)) ||
                (c.LastName != null && c.LastName.ToLower().Contains(searchLower)) ||
                (c.Email != null && c.Email.ToLower().Contains(searchLower)) ||
                (c.Title != null && c.Title.ToLower().Contains(searchLower)) ||
                (c.Department != null && c.Department.ToLower().Contains(searchLower)) ||
                (c.PhoneNumber != null && c.PhoneNumber.Contains(searchLower)) ||
                (c.MobileNumber != null && c.MobileNumber.Contains(searchLower)) ||
                (c.Customer?.Name != null && c.Customer.Name.ToLower().Contains(searchLower))
            ).ToList();
        }
    }

    private void OnSearchChanged(string value)
    {
        searchTerm = value;
        ApplyFilters();
        StateHasChanged();
    }

    // View management
    private void OnViewChanged(GenericViewSwitcher<CustomerContact>.ViewType newView)
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
    private async Task HandleTableSelectionChanged(List<CustomerContact> items)
    {
        selectedTableItems = items;
        StateHasChanged();
    }

    private async Task HandleListSelectionChanged(List<CustomerContact> items)
    {
        selectedListItems = items;
        StateHasChanged();
    }

    private async Task HandleCardSelectionChanged(List<CustomerContact> items)
    {
        selectedCardItems = items;
        StateHasChanged();
    }

    // Item interaction
    private void HandleContactClick(CustomerContact contact)
    {
        // Single click - could be used for selection or preview
    }

    private void HandleContactDoubleClick(CustomerContact contact)
    {
        // Double click - navigate to contact details
        ViewContact(contact.Id);
    }

    private List<CustomerContact> GetSelectedItems()
    {
        return currentView switch
        {
            GenericViewSwitcher<CustomerContact>.ViewType.Table => selectedTableItems,
            GenericViewSwitcher<CustomerContact>.ViewType.List => selectedListItems,
            GenericViewSwitcher<CustomerContact>.ViewType.Card => selectedCardItems,
            _ => new List<CustomerContact>()
        };
    }

    private void CreateNewContact()
    {
        Navigation.NavigateTo($"/{TenantSlug}/trace/contacts/new");
    }

    private void ViewContact(int contactId)
    {
        Navigation.NavigateTo($"/{TenantSlug}/trace/contacts/{contactId}");
    }

    private void EditContact(int contactId)
    {
        Navigation.NavigateTo($"/{TenantSlug}/trace/contacts/{contactId}");
    }

    private async Task DeleteContact(CustomerContact contact)
    {
        // In a real application, you'd want to show a confirmation dialog
        try
        {
            await using var dbContext = await DbContextFactory.CreateDbContextAsync();
            dbContext.CustomerContacts.Remove(contact);
            await dbContext.SaveChangesAsync();
            await LoadContacts();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error deleting contact: {ex.Message}");
            // In a real application, show error message to user
        }
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
                Action = EventCallback.Factory.Create(this, CreateNewContact),
                IsDisabled = false,
                Style = FabOS.WebServer.Components.Shared.Interfaces.ToolbarActionStyle.Primary
            },
            new FabOS.WebServer.Components.Shared.Interfaces.ToolbarAction
            {
                Label = "Delete",
                Text = "Delete",
                Icon = "fas fa-trash",
                Action = EventCallback.Factory.Create(this, DeleteSelectedContacts),
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
                Action = EventCallback.Factory.Create(this, async () => await LoadContacts()),
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

    private async Task DeleteSelectedContacts()
    {
        var selected = GetSelectedItems();
        if (!selected.Any()) return;

        try
        {
            await using var dbContext = await DbContextFactory.CreateDbContextAsync();
            dbContext.CustomerContacts.RemoveRange(selected);
            await dbContext.SaveChangesAsync();

            selectedTableItems.Clear();
            selectedListItems.Clear();
            selectedCardItems.Clear();

            await LoadContacts();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error deleting contacts: {ex.Message}");
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