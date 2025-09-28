using Microsoft.AspNetCore.Components;
using Microsoft.EntityFrameworkCore;
using FabOS.WebServer.Components.Shared.Interfaces;
using FabOS.WebServer.Data.Contexts;
using FabOS.WebServer.Models.Entities;
using System.ComponentModel.DataAnnotations;

namespace FabOS.WebServer.Components.Pages;

public partial class ContactEdit : ComponentBase, IToolbarActionProvider
{
    [Parameter] public int Id { get; set; }

    [Inject] private ApplicationDbContext DbContext { get; set; } = default!;
    [Inject] private NavigationManager Navigation { get; set; } = default!;

    private ContactModel contactModel = new();
    private List<Customer> customers = new();
    private Customer? selectedCustomer;
    private bool isProcessing = false;

    protected override async Task OnInitializedAsync()
    {
        await LoadCustomers();

        if (Id > 0)
        {
            await LoadContact();
        }
        else
        {
            // Set defaults for new contact
            contactModel.IsActive = true;
        }
    }

    private async Task LoadCustomers()
    {
        try
        {
            customers = await DbContext.Customers
                .Where(c => c.IsActive)
                .OrderBy(c => c.Name)
                .ToListAsync();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error loading customers: {ex.Message}");
        }
    }

    private async Task LoadContact()
    {
        var contact = await DbContext.CustomerContacts
            .Include(c => c.Customer)
            .FirstOrDefaultAsync(c => c.Id == Id);

        if (contact != null)
        {
            contactModel = new ContactModel
            {
                FirstName = contact.FirstName,
                LastName = contact.LastName,
                Title = contact.Title,
                Department = contact.Department,
                Email = contact.Email,
                PhoneNumber = contact.PhoneNumber,
                MobileNumber = contact.MobileNumber,
                CustomerId = contact.CustomerId,
                IsPrimary = contact.IsPrimary,
                IsActive = contact.IsActive,
                Notes = contact.Notes
            };

            selectedCustomer = contact.Customer;
        }
    }

    private async Task OnCustomerChanged(ChangeEventArgs e)
    {
        if (int.TryParse(e.Value?.ToString(), out var customerId) && customerId > 0)
        {
            selectedCustomer = await DbContext.Customers
                .FirstOrDefaultAsync(c => c.Id == customerId);
        }
        else
        {
            selectedCustomer = null;
        }

        StateHasChanged();
    }

    private void NavigateToNewCustomer()
    {
        Navigation.NavigateTo("/customers/new");
    }

    private async Task HandleValidSubmit()
    {
        isProcessing = true;
        StateHasChanged();

        try
        {
            CustomerContact contact;

            if (Id > 0)
            {
                contact = await DbContext.CustomerContacts
                    .FirstOrDefaultAsync(c => c.Id == Id) ?? new CustomerContact();
            }
            else
            {
                contact = new CustomerContact();
                DbContext.CustomerContacts.Add(contact);
            }

            // If setting as primary, unset other primary contacts for this customer
            if (contactModel.IsPrimary && contactModel.CustomerId.HasValue)
            {
                var existingPrimary = await DbContext.CustomerContacts
                    .Where(c => c.CustomerId == contactModel.CustomerId && c.IsPrimary && c.Id != Id)
                    .ToListAsync();

                foreach (var existing in existingPrimary)
                {
                    existing.IsPrimary = false;
                }
            }

            // Update contact fields
            contact.FirstName = contactModel.FirstName;
            contact.LastName = contactModel.LastName;
            contact.Title = contactModel.Title;
            contact.Department = contactModel.Department;
            contact.Email = contactModel.Email;
            contact.PhoneNumber = contactModel.PhoneNumber;
            contact.MobileNumber = contactModel.MobileNumber;
            contact.CustomerId = contactModel.CustomerId ?? 0;
            contact.IsPrimary = contactModel.IsPrimary;
            contact.IsActive = contactModel.IsActive;
            contact.Notes = contactModel.Notes;
            contact.LastModified = DateTime.UtcNow;

            if (contact.Id == 0)
            {
                contact.CreatedDate = DateTime.UtcNow;
            }

            await DbContext.SaveChangesAsync();

            // Navigate to the contact detail page
            Navigation.NavigateTo($"/contacts/{contact.Id}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error saving contact: {ex.Message}");
            isProcessing = false;
            StateHasChanged();
        }
    }

    private void Cancel()
    {
        if (Id > 0)
        {
            Navigation.NavigateTo($"/contacts/{Id}");
        }
        else
        {
            Navigation.NavigateTo("/contacts");
        }
    }

    // IToolbarActionProvider implementation
    public ToolbarActionGroup GetActions()
    {
        var group = new ToolbarActionGroup();
        group.PrimaryActions = new List<ToolbarAction>
        {
            new ToolbarAction
            {
                Label = "Save",
                Text = "Save Contact",
                Icon = "fas fa-save",
                Action = EventCallback.Factory.Create(this, async () => await HandleValidSubmit()),
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
                Action = EventCallback.Factory.Create(this, Cancel),
                IsDisabled = false
            }
        };
        return group;
    }

    // View Model
    private class ContactModel
    {
        [Required(ErrorMessage = "First name is required")]
        [StringLength(100, ErrorMessage = "First name must be less than 100 characters")]
        public string FirstName { get; set; } = "";

        [Required(ErrorMessage = "Last name is required")]
        [StringLength(100, ErrorMessage = "Last name must be less than 100 characters")]
        public string LastName { get; set; } = "";

        [StringLength(50, ErrorMessage = "Title must be less than 50 characters")]
        public string? Title { get; set; }

        [StringLength(100, ErrorMessage = "Department must be less than 100 characters")]
        public string? Department { get; set; }

        [Required(ErrorMessage = "Email is required")]
        [EmailAddress(ErrorMessage = "Invalid email address")]
        [StringLength(200, ErrorMessage = "Email must be less than 200 characters")]
        public string Email { get; set; } = "";

        [Phone(ErrorMessage = "Invalid phone number")]
        [StringLength(20, ErrorMessage = "Phone number must be less than 20 characters")]
        public string? PhoneNumber { get; set; }

        [Phone(ErrorMessage = "Invalid mobile number")]
        [StringLength(20, ErrorMessage = "Mobile number must be less than 20 characters")]
        public string? MobileNumber { get; set; }

        [Required(ErrorMessage = "Customer is required")]
        public int? CustomerId { get; set; }

        public bool IsPrimary { get; set; }
        public bool IsActive { get; set; } = true;

        [StringLength(500, ErrorMessage = "Notes must be less than 500 characters")]
        public string? Notes { get; set; }
    }
}