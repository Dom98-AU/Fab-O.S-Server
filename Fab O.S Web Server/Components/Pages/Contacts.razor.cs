using Microsoft.AspNetCore.Components;
using Microsoft.EntityFrameworkCore;
using FabOS.WebServer.Components.Shared.Interfaces;
using FabOS.WebServer.Data.Contexts;
using FabOS.WebServer.Models.Entities;
using Microsoft.AspNetCore.WebUtilities;

namespace FabOS.WebServer.Components.Pages;

public partial class Contacts : ComponentBase, IToolbarActionProvider
{
    [Inject] private ApplicationDbContext DbContext { get; set; } = default!;
    [Inject] private NavigationManager Navigation { get; set; } = default!;

    private List<CustomerContact> contacts = new();
    private List<CustomerContact> filteredContacts = new();
    private string searchTerm = "";
    private string viewMode = "grid";
    private bool isLoading = true;
    private int? filterByCustomerId = null;
    private Customer? filterCustomer = null;

    protected override async Task OnInitializedAsync()
    {
        // Check for customerId query parameter
        var uri = Navigation.ToAbsoluteUri(Navigation.Uri);
        if (QueryHelpers.ParseQuery(uri.Query).TryGetValue("customerId", out var customerIdValue))
        {
            if (int.TryParse(customerIdValue, out int customerId))
            {
                filterByCustomerId = customerId;
                // Load customer details for display
                filterCustomer = await DbContext.Customers.FindAsync(customerId);
            }
        }

        await LoadContacts();
    }

    private async Task LoadContacts()
    {
        try
        {
            isLoading = true;
            var query = DbContext.CustomerContacts
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

    private void SetViewMode(string mode)
    {
        viewMode = mode;
        StateHasChanged();
    }

    private string GetInitials(CustomerContact contact)
    {
        var firstInitial = !string.IsNullOrEmpty(contact.FirstName) ? contact.FirstName[0].ToString().ToUpper() : "";
        var lastInitial = !string.IsNullOrEmpty(contact.LastName) ? contact.LastName[0].ToString().ToUpper() : "";
        return $"{firstInitial}{lastInitial}";
    }

    private void CreateNewContact()
    {
        Navigation.NavigateTo("/contacts/new");
    }

    private void ViewContact(int contactId)
    {
        Navigation.NavigateTo($"/contacts/{contactId}");
    }

    private void EditContact(int contactId)
    {
        Navigation.NavigateTo($"/contacts/edit/{contactId}");
    }

    private async Task DeleteContact(CustomerContact contact)
    {
        // In a real application, you'd want to show a confirmation dialog
        try
        {
            DbContext.CustomerContacts.Remove(contact);
            await DbContext.SaveChangesAsync();
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
        var group = new ToolbarActionGroup();
        group.PrimaryActions = new List<FabOS.WebServer.Components.Shared.Interfaces.ToolbarAction>
        {
            new FabOS.WebServer.Components.Shared.Interfaces.ToolbarAction
            {
                Label = "Add Contact",
                Text = "Add Contact",
                Icon = "fas fa-plus",
                Action = EventCallback.Factory.Create(this, CreateNewContact),
                IsDisabled = false,
                Style = FabOS.WebServer.Components.Shared.Interfaces.ToolbarActionStyle.Primary
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
            },
            new FabOS.WebServer.Components.Shared.Interfaces.ToolbarAction
            {
                Label = "Export",
                Text = "Export to Excel",
                Icon = "fas fa-file-excel",
                Action = EventCallback.Factory.Create(this, () => Console.WriteLine("Export clicked")),
                IsDisabled = false
            },
            new FabOS.WebServer.Components.Shared.Interfaces.ToolbarAction
            {
                Label = "Grid View",
                Text = "Grid View",
                Icon = "fas fa-th",
                Action = EventCallback.Factory.Create(this, () => SetViewMode("grid")),
                IsDisabled = false
            },
            new FabOS.WebServer.Components.Shared.Interfaces.ToolbarAction
            {
                Label = "List View",
                Text = "List View",
                Icon = "fas fa-list",
                Action = EventCallback.Factory.Create(this, () => SetViewMode("list")),
                IsDisabled = false
            }
        };
        return group;
    }
}