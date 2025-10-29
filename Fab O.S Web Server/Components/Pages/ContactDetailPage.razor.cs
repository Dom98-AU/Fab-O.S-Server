using Microsoft.AspNetCore.Components;
using Microsoft.EntityFrameworkCore;
using FabOS.WebServer.Components.Shared.Interfaces;
using FabOS.WebServer.Data.Contexts;
using FabOS.WebServer.Models.Entities;
using FabOS.WebServer.Services;
using FabOS.WebServer.Services.Interfaces;
using Microsoft.AspNetCore.WebUtilities;

namespace FabOS.WebServer.Components.Pages;

public partial class ContactDetailPage : ComponentBase, IToolbarActionProvider
{
    [Parameter] public string TenantSlug { get; set; } = string.Empty;
    [Parameter] public int Id { get; set; }

    [Inject] private IDbContextFactory<ApplicationDbContext> DbContextFactory { get; set; } = default!;
    [Inject] private NavigationManager Navigation { get; set; } = default!;
    [Inject] private BreadcrumbService BreadcrumbService { get; set; } = default!;
    [Inject] private NumberSeriesService NumberSeriesService { get; set; } = default!;

    private CustomerContact? contact;
    private Customer? customer;
    private CustomerAddress? primaryCustomerAddress;
    private List<Customer> customers = new();
    private Customer? selectedCustomer;
    private bool isLoading = false;
    private bool isEditMode = false;
    private bool contactNumberGenerated = false;
    private bool isProcessing = false;
    private string? returnUrl;

    // Section collapse management
    private Dictionary<string, bool> sectionStates = new Dictionary<string, bool>
    {
        { "general", true }      // Contact Information - expanded by default
    };

    protected override async Task OnInitializedAsync()
    {
        // Get the return URL from query parameters
        var uri = Navigation.ToAbsoluteUri(Navigation.Uri);
        if (QueryHelpers.ParseQuery(uri.Query).TryGetValue("returnUrl", out var returnUrlValue))
        {
            returnUrl = System.Net.WebUtility.UrlDecode(returnUrlValue.ToString());
        }

        await LoadCustomers();

        if (Id == 0)
        {
            // Create new contact
            contact = new CustomerContact
            {
                Id = 0,
                IsActive = true,
                InheritCustomerAddress = true,
                CreatedDate = DateTime.UtcNow,
                LastModified = DateTime.UtcNow
            };
            isEditMode = true;
            await GenerateContactNumber();
        }
        else
        {
            // Load existing contact
            await LoadContactDetails();
            isEditMode = false;
        }

        await UpdateBreadcrumbAsync();
    }

    private async Task UpdateBreadcrumbAsync()
    {
        if (Id == 0)
        {
            // For new contacts, use custom label
            await BreadcrumbService.BuildAndSetSimpleBreadcrumbAsync(
                "Contacts",
                $"/{TenantSlug}/trace/contacts",
                "Contact",
                null,
                "New Contact"
            );
        }
        else
        {
            // For existing contacts, use the breadcrumb builder to load the actual contact name
            await BreadcrumbService.BuildAndSetBreadcrumbsAsync(
                ("Contacts", null, $"/{TenantSlug}/trace/contacts", null),
                ("Contact", Id, null, null)
            );
        }
    }

    private async Task GenerateContactNumber()
    {
        if (!contactNumberGenerated && contact != null)
        {
            try
            {
                contact.ContactNumber = await NumberSeriesService.GetNextNumberAsync("Contact", 1);
                contactNumberGenerated = true;
                StateHasChanged();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to generate contact number: {ex.Message}");
            }
        }
    }

    private async Task LoadCustomers()
    {
        try
        {
            await using var dbContext = await DbContextFactory.CreateDbContextAsync();
            customers = await dbContext.Customers
                .Where(c => c.IsActive)
                .OrderBy(c => c.Name)
                .ToListAsync();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error loading customers: {ex.Message}");
        }
    }

    private async Task LoadContactDetails()
    {
        try
        {
            isLoading = true;

            await using var dbContext = await DbContextFactory.CreateDbContextAsync();

            // Load contact with related data
            contact = await dbContext.CustomerContacts
                .Include(c => c.Customer)
                .FirstOrDefaultAsync(c => c.Id == Id);

            if (contact != null)
            {
                // Get the associated customer
                customer = contact.Customer;
                selectedCustomer = customer;

                // If contact inherits address from customer, get the primary customer address
                if (contact.InheritCustomerAddress && customer != null)
                {
                    primaryCustomerAddress = await dbContext.CustomerAddresses
                        .FirstOrDefaultAsync(a => a.CustomerId == customer.Id && a.IsPrimary);
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error loading contact details: {ex.Message}");
        }
        finally
        {
            isLoading = false;
        }
    }

    private async Task OnCustomerChanged(ChangeEventArgs e)
    {
        if (int.TryParse(e.Value?.ToString(), out var customerId) && customerId > 0)
        {
            await using var dbContext = await DbContextFactory.CreateDbContextAsync();
            selectedCustomer = await dbContext.Customers
                .FirstOrDefaultAsync(c => c.Id == customerId);

            customer = selectedCustomer;
            if (contact != null)
            {
                contact.CustomerId = customerId;
            }
        }
        else
        {
            selectedCustomer = null;
            customer = null;
            if (contact != null)
            {
                contact.CustomerId = 0;
            }
        }

        StateHasChanged();
    }

    private void NavigateToNewCustomer()
    {
        Navigation.NavigateTo($"/{TenantSlug}/trace/customers/new");
    }

    private void ToggleEditMode()
    {
        isEditMode = !isEditMode;
        StateHasChanged();
    }

    private async Task SaveContact()
    {
        if (contact == null) return;

        // Basic validation
        if (string.IsNullOrWhiteSpace(contact.FirstName))
        {
            Console.WriteLine("First name is required");
            return;
        }

        if (string.IsNullOrWhiteSpace(contact.LastName))
        {
            Console.WriteLine("Last name is required");
        }

        if (string.IsNullOrWhiteSpace(contact.Email))
        {
            Console.WriteLine("Email is required");
            return;
        }

        if (contact.CustomerId == 0)
        {
            Console.WriteLine("Customer is required");
            return;
        }

        isProcessing = true;
        StateHasChanged();

        try
        {
            await using var dbContext = await DbContextFactory.CreateDbContextAsync();

            CustomerContact contactToSave;

            if (Id > 0)
            {
                // Update existing contact
                contactToSave = await dbContext.CustomerContacts
                    .FirstOrDefaultAsync(c => c.Id == Id) ?? contact;

                // Copy values from our contact object
                contactToSave.ContactNumber = contact.ContactNumber;
                contactToSave.FirstName = contact.FirstName;
                contactToSave.LastName = contact.LastName;
                contactToSave.Title = contact.Title;
                contactToSave.Department = contact.Department;
                contactToSave.Email = contact.Email;
                contactToSave.PhoneNumber = contact.PhoneNumber;
                contactToSave.MobileNumber = contact.MobileNumber;
                contactToSave.AddressLine1 = contact.AddressLine1;
                contactToSave.AddressLine2 = contact.AddressLine2;
                contactToSave.City = contact.City;
                contactToSave.State = contact.State;
                contactToSave.PostalCode = contact.PostalCode;
                contactToSave.Country = contact.Country;
                contactToSave.InheritCustomerAddress = contact.InheritCustomerAddress;
                contactToSave.CustomerId = contact.CustomerId;
                contactToSave.IsPrimary = contact.IsPrimary;
                contactToSave.IsActive = contact.IsActive;
                contactToSave.Notes = contact.Notes;
                contactToSave.LastModified = DateTime.UtcNow;
            }
            else
            {
                // Create new contact
                contactToSave = contact;
                contactToSave.CreatedDate = DateTime.UtcNow;
                contactToSave.LastModified = DateTime.UtcNow;
                dbContext.CustomerContacts.Add(contactToSave);
            }

            // If setting as primary, unset other primary contacts for this customer
            if (contactToSave.IsPrimary && contactToSave.CustomerId > 0)
            {
                var existingPrimary = await dbContext.CustomerContacts
                    .Where(c => c.CustomerId == contactToSave.CustomerId && c.IsPrimary && c.Id != contactToSave.Id)
                    .ToListAsync();

                foreach (var existing in existingPrimary)
                {
                    existing.IsPrimary = false;
                }
            }

            await dbContext.SaveChangesAsync();

            // After save, switch to view mode or navigate
            if (Id == 0)
            {
                // Navigate to the new contact's detail page
                Navigation.NavigateTo($"/{TenantSlug}/trace/contacts/{contactToSave.Id}");
            }
            else
            {
                // Switch to view mode
                isEditMode = false;
                await LoadContactDetails();
                await UpdateBreadcrumbAsync();
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error saving contact: {ex.Message}");
        }
        finally
        {
            isProcessing = false;
            StateHasChanged();
        }
    }

    private void CancelEdit()
    {
        if (Id == 0)
        {
            // Cancel creating new contact - go back to list
            Navigation.NavigateTo($"/{TenantSlug}/trace/contacts");
        }
        else
        {
            // Cancel editing - reload and switch to view mode
            isEditMode = false;
            _ = LoadContactDetails();
        }
    }

    private async Task DeleteContact()
    {
        if (contact != null)
        {
            try
            {
                await using var dbContext = await DbContextFactory.CreateDbContextAsync();
                var contactToDelete = await dbContext.CustomerContacts.FindAsync(contact.Id);
                if (contactToDelete != null)
                {
                    dbContext.CustomerContacts.Remove(contactToDelete);
                    await dbContext.SaveChangesAsync();
                }
                Navigation.NavigateTo($"/{TenantSlug}/trace/contacts");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error deleting contact: {ex.Message}");
            }
        }
    }

    private void NavigateBack()
    {
        if (!string.IsNullOrEmpty(returnUrl))
        {
            Navigation.NavigateTo(returnUrl);
        }
        else
        {
            Navigation.NavigateTo($"/{TenantSlug}/trace/contacts");
        }
    }

    private void NavigateToCustomer()
    {
        if (customer != null)
        {
            Navigation.NavigateTo($"/{TenantSlug}/trace/customers/{customer.Id}");
        }
    }

    private void ToggleSection(string sectionName)
    {
        if (sectionStates.ContainsKey(sectionName))
        {
            sectionStates[sectionName] = !sectionStates[sectionName];
            StateHasChanged();
        }
    }

    private bool IsSectionExpanded(string sectionName)
    {
        return sectionStates.ContainsKey(sectionName) && sectionStates[sectionName];
    }

    // IToolbarActionProvider implementation
    public ToolbarActionGroup GetActions()
    {
        var group = new ToolbarActionGroup();

        if (isEditMode)
        {
            // Edit mode actions
            group.PrimaryActions = new List<ToolbarAction>
            {
                new ToolbarAction
                {
                    Label = "Save",
                    Text = "Save Contact",
                    Icon = "fas fa-save",
                    Action = EventCallback.Factory.Create(this, async () => await SaveContact()),
                    IsDisabled = isProcessing,
                    Style = ToolbarActionStyle.Primary
                }
            };
            group.MenuActions = new List<ToolbarAction>
            {
                new ToolbarAction
                {
                    Label = "Cancel",
                    Text = "Cancel",
                    Icon = "fas fa-times",
                    Action = EventCallback.Factory.Create(this, CancelEdit),
                    IsDisabled = false
                }
            };
        }
        else
        {
            // View mode actions
            group.PrimaryActions = new List<ToolbarAction>
            {
                new ToolbarAction
                {
                    Label = "Edit",
                    Text = "Edit Contact",
                    Icon = "fas fa-edit",
                    Action = EventCallback.Factory.Create(this, ToggleEditMode),
                    IsDisabled = contact == null,
                    Style = ToolbarActionStyle.Primary
                }
            };

            // Menu Actions
            group.MenuActions = new List<ToolbarAction>
            {
                new ToolbarAction
                {
                    Label = "Delete",
                    Text = "Delete Contact",
                    Icon = "fas fa-trash",
                    Action = EventCallback.Factory.Create(this, async () => await DeleteContact()),
                    IsDisabled = contact == null,
                    Style = ToolbarActionStyle.Danger
                },
                new ToolbarAction
                {
                    Label = "Back",
                    Text = "Back to List",
                    Icon = "fas fa-arrow-left",
                    Action = EventCallback.Factory.Create(this, NavigateBack),
                    IsDisabled = false
                }
            };

            // Related Actions
            if (customer != null)
            {
                group.RelatedActions = new List<ToolbarAction>
                {
                    new ToolbarAction
                    {
                        Label = "View Customer",
                        Text = "View Customer",
                        Icon = "fas fa-building",
                        Action = EventCallback.Factory.Create(this, NavigateToCustomer),
                        IsDisabled = false
                    }
                };
            }
        }

        return group;
    }
}
