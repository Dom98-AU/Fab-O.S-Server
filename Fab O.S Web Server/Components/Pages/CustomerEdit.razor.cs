using Microsoft.AspNetCore.Components;
using Microsoft.EntityFrameworkCore;
using Microsoft.JSInterop;
using FabOS.WebServer.Components.Shared.Interfaces;
using FabOS.WebServer.Data.Contexts;
using FabOS.WebServer.Models.Entities;
using FabOS.WebServer.Services.Interfaces;
using System.ComponentModel.DataAnnotations;

namespace FabOS.WebServer.Components.Pages;

public partial class CustomerEdit : ComponentBase, IToolbarActionProvider
{
    [Parameter] public int Id { get; set; }

    [Inject] private ApplicationDbContext DbContext { get; set; } = default!;
    [Inject] private NavigationManager Navigation { get; set; } = default!;
    [Inject] private IJSRuntime JSRuntime { get; set; } = default!;
    [Inject] private IConfiguration Configuration { get; set; } = default!;
    [Inject] private IAbnLookupService AbnLookupService { get; set; } = default!;
    [Inject] private IGooglePlacesService GooglePlacesService { get; set; } = default!;

    private CustomerModel customerModel = new();
    private AddressModel addressModel = new();
    private ContactModel contactModel = new();
    private List<CustomerContact> customerContacts = new();
    private int editingContactId = 0;

    private bool isProcessing = false;
    private bool showLegacy = false;
    private bool showSuggestions = false;
    private bool hasLoadedGoogleMaps = false;
    private string googleMapsApiKey = "";
    private bool isLoadingAbn = false;
    private bool showAbnResults = false;
    private string abnSearchTerm = "";
    private string abnValidationMessage = "";
    private string abnValidationClass = "";
    private bool showAbnSearchDialog = false;
    private string abnSearchMode = "abn"; // "abn" or "name"
    private string addressSearchText = "";
    private string sessionToken = Guid.NewGuid().ToString();

    private List<PlaceAutocompletePrediction> addressSuggestions = new();
    private List<AbnSearchResult> abnSearchResults = new();

    protected override async Task OnInitializedAsync()
    {
        googleMapsApiKey = Configuration["GoogleMaps:ApiKey"] ?? "";

        if (Id > 0)
        {
            await LoadCustomer();
        }
        else
        {
            // Initialize with an empty list for new customers
            customerContacts = new List<CustomerContact>();
        }
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender && !string.IsNullOrEmpty(googleMapsApiKey))
        {
            hasLoadedGoogleMaps = true;
        }
    }

    private async Task LoadCustomer()
    {
        var customer = await DbContext.Customers
            .Include(c => c.Contacts)
            .Include(c => c.Addresses)
            .FirstOrDefaultAsync(c => c.Id == Id);

        if (customer != null)
        {
            customerModel = new CustomerModel
            {
                Code = customer.Code ?? "",
                Name = customer.Name ?? "",
                ABN = customer.ABN,
                Website = customer.Website,
                Industry = customer.Industry,
                Notes = customer.Notes,
                IsActive = customer.IsActive,
                // Legacy fields
                ContactPerson = customer.ContactPerson,
                Email = customer.Email,
                PhoneNumber = customer.PhoneNumber
            };

            // Load primary address
            var primaryAddress = customer.Addresses?.FirstOrDefault(a => a.IsPrimary)
                                ?? customer.Addresses?.FirstOrDefault();
            if (primaryAddress != null)
            {
                addressModel = new AddressModel
                {
                    AddressLine1 = primaryAddress.AddressLine1,
                    AddressLine2 = primaryAddress.AddressLine2,
                    City = primaryAddress.City,
                    State = primaryAddress.State,
                    PostalCode = primaryAddress.PostalCode,
                    GooglePlaceId = primaryAddress.GooglePlaceId,
                    FormattedAddress = primaryAddress.FormattedAddress
                };
            }

            // Load all contacts
            customerContacts = customer.Contacts?.ToList() ?? new List<CustomerContact>();

            // Keep the legacy contactModel for backward compatibility
            var primaryContact = customerContacts.FirstOrDefault(c => c.IsPrimary)
                                ?? customerContacts.FirstOrDefault();
            if (primaryContact != null)
            {
                contactModel = new ContactModel
                {
                    FirstName = primaryContact.FirstName,
                    LastName = primaryContact.LastName,
                    Title = primaryContact.Title,
                    Department = primaryContact.Department,
                    Email = primaryContact.Email,
                    PhoneNumber = primaryContact.PhoneNumber,
                    MobileNumber = primaryContact.MobileNumber
                };
            }
        }
    }

    private async Task GenerateCustomerCode()
    {
        if (string.IsNullOrEmpty(customerModel.Code))
        {
            var timestamp = DateTime.UtcNow.ToString("yyyyMMddHHmmss");
            customerModel.Code = $"CUST-{timestamp}";
            StateHasChanged();
        }
    }

    private async Task OnAbnChanged(ChangeEventArgs e)
    {
        var abn = e.Value?.ToString() ?? "";
        customerModel.ABN = abn;
        abnValidationMessage = "";
        abnValidationClass = "";

        if (!string.IsNullOrEmpty(abn))
        {
            var cleanAbn = abn.Replace(" ", "").Replace("-", "");
            if (cleanAbn.Length == 11)
            {
                if (AbnLookupService.ValidateAbn(cleanAbn))
                {
                    abnValidationMessage = "Valid ABN format";
                    abnValidationClass = "valid";
                    await SearchByAbn(cleanAbn);
                }
                else
                {
                    abnValidationMessage = "Invalid ABN checksum";
                    abnValidationClass = "invalid";
                }
            }
            else if (cleanAbn.Length > 0)
            {
                abnValidationMessage = "ABN must be 11 digits";
                abnValidationClass = "invalid";
            }
        }
        StateHasChanged();
    }

    private async Task SearchByAbn(string abn)
    {
        isLoadingAbn = true;
        StateHasChanged();

        try
        {
            var result = await AbnLookupService.SearchByAbnAsync(abn);
            if (result != null)
            {
                abnSearchResults = new List<AbnSearchResult> { result };
                showAbnResults = true;
            }
            else
            {
                abnValidationMessage = "ABN not found in register";
                abnValidationClass = "not-found";
            }
        }
        catch (Exception ex)
        {
            abnValidationMessage = "Error looking up ABN";
            abnValidationClass = "error";
            Console.WriteLine($"ABN lookup error: {ex.Message}");
        }
        finally
        {
            isLoadingAbn = false;
            StateHasChanged();
        }
    }

    private async Task SearchByBusinessName()
    {
        if (string.IsNullOrWhiteSpace(abnSearchTerm))
            return;

        isLoadingAbn = true;
        showAbnResults = false;
        StateHasChanged();

        try
        {
            var results = await AbnLookupService.SearchByNameAsync(abnSearchTerm);
            abnSearchResults = results;
            showAbnResults = results.Any();

            if (!results.Any())
            {
                abnValidationMessage = "No businesses found with that name";
                abnValidationClass = "not-found";
            }
        }
        catch (Exception ex)
        {
            abnValidationMessage = "Error searching for business";
            abnValidationClass = "error";
            Console.WriteLine($"Business name search error: {ex.Message}");
        }
        finally
        {
            isLoadingAbn = false;
            StateHasChanged();
        }
    }

    private void SelectAbnResult(AbnSearchResult result)
    {
        // Auto-populate customer fields from ABN result
        customerModel.ABN = result.Abn;
        customerModel.Name = result.DisplayName;

        // Update address if provided
        if (!string.IsNullOrEmpty(result.FormattedAddress))
        {
            addressModel.AddressLine1 = result.AddressLine1;
            addressModel.AddressLine2 = result.AddressLine2;
            addressModel.City = result.Suburb;
            addressModel.State = result.State;
            addressModel.PostalCode = result.Postcode;
            addressModel.FormattedAddress = result.FormattedAddress;
        }

        showAbnResults = false;
        abnValidationMessage = "Business details populated from ABN register";
        abnValidationClass = "populated";
        StateHasChanged();
    }

    private void CloseAbnResults()
    {
        showAbnResults = false;
        StateHasChanged();
    }

    private async Task SearchAddress(ChangeEventArgs e)
    {
        addressSearchText = e.Value?.ToString() ?? "";

        if (string.IsNullOrWhiteSpace(addressSearchText) || addressSearchText.Length < 3)
        {
            showSuggestions = false;
            addressSuggestions.Clear();
            return;
        }

        addressSuggestions = await GooglePlacesService.AutocompleteAsync(addressSearchText, sessionToken);
        showSuggestions = addressSuggestions.Any();
        StateHasChanged();
    }

    private async Task SelectAddress(PlaceAutocompletePrediction suggestion)
    {
        var details = await GooglePlacesService.GetPlaceDetailsAsync(suggestion.PlaceId, sessionToken);

        addressModel.FormattedAddress = details.FormattedAddress;
        addressModel.GooglePlaceId = suggestion.PlaceId;

        // Construct address line 1 from street number and route
        var addressLine1Parts = new List<string>();
        if (!string.IsNullOrEmpty(details.StreetNumber))
            addressLine1Parts.Add(details.StreetNumber);
        if (!string.IsNullOrEmpty(details.Route))
            addressLine1Parts.Add(details.Route);

        addressModel.AddressLine1 = string.Join(" ", addressLine1Parts);
        addressModel.City = details.Locality;
        addressModel.State = details.State;
        addressModel.PostalCode = details.PostalCode;

        showSuggestions = false;
        addressSearchText = suggestion.Description;
        sessionToken = Guid.NewGuid().ToString(); // Generate new session token after selection
        StateHasChanged();
    }

    private void ToggleLegacySection()
    {
        showLegacy = !showLegacy;
    }

    private async Task HandleValidSubmit()
    {
        isProcessing = true;
        StateHasChanged();

        try
        {
            Customer customer;

            if (Id > 0)
            {
                customer = await DbContext.Customers
                    .Include(c => c.Contacts)
                    .Include(c => c.Addresses)
                    .FirstOrDefaultAsync(c => c.Id == Id) ?? new Customer();
            }
            else
            {
                customer = new Customer();
                DbContext.Customers.Add(customer);
            }

            // Update customer fields
            customer.Code = customerModel.Code;
            customer.Name = customerModel.Name;
            customer.ABN = customerModel.ABN;
            customer.Website = customerModel.Website;
            customer.Industry = customerModel.Industry;
            customer.Notes = customerModel.Notes;
            customer.IsActive = customerModel.IsActive;
            customer.LastModified = DateTime.UtcNow;

            // Update legacy fields if provided
            customer.ContactPerson = customerModel.ContactPerson;
            customer.Email = customerModel.Email;
            customer.PhoneNumber = customerModel.PhoneNumber;

            // Handle primary address
            if (!string.IsNullOrWhiteSpace(addressModel.AddressLine1))
            {
                var address = customer.Addresses?.FirstOrDefault(a => a.IsPrimary)
                             ?? new CustomerAddress { CustomerId = customer.Id, IsPrimary = true };

                address.AddressLine1 = addressModel.AddressLine1;
                address.AddressLine2 = addressModel.AddressLine2;
                address.City = addressModel.City;
                address.State = addressModel.State;
                address.PostalCode = addressModel.PostalCode;
                address.Country = "Australia";
                address.GooglePlaceId = addressModel.GooglePlaceId;
                address.FormattedAddress = addressModel.FormattedAddress;
                address.LastModified = DateTime.UtcNow;

                if (address.Id == 0)
                {
                    address.AddressType = "Main";
                    customer.Addresses.Add(address);
                }
            }

            // Clear existing contacts and add the modified ones
            if (Id > 0)
            {
                // For existing customers, update contacts
                var existingContacts = customer.Contacts.ToList();
                foreach (var existingContact in existingContacts)
                {
                    if (!customerContacts.Any(c => c.Id == existingContact.Id))
                    {
                        // Contact was deleted
                        DbContext.Remove(existingContact);
                    }
                }
            }
            else
            {
                // For new customers, add all contacts
                customer.Contacts = new List<CustomerContact>();
            }

            // Add or update contacts
            foreach (var contact in customerContacts)
            {
                if (contact.Id == 0)
                {
                    // New contact
                    contact.CustomerId = customer.Id;
                    contact.LastModified = DateTime.UtcNow;
                    customer.Contacts.Add(contact);
                }
                else
                {
                    // Existing contact - update
                    var existingContact = customer.Contacts.FirstOrDefault(c => c.Id == contact.Id);
                    if (existingContact != null)
                    {
                        existingContact.FirstName = contact.FirstName;
                        existingContact.LastName = contact.LastName;
                        existingContact.Title = contact.Title;
                        existingContact.Department = contact.Department;
                        existingContact.Email = contact.Email;
                        existingContact.PhoneNumber = contact.PhoneNumber;
                        existingContact.MobileNumber = contact.MobileNumber;
                        existingContact.IsPrimary = contact.IsPrimary;
                        existingContact.LastModified = DateTime.UtcNow;
                    }
                }
            }

            await DbContext.SaveChangesAsync();
            Navigation.NavigateTo("/customers");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error saving customer: {ex.Message}");
            isProcessing = false;
            StateHasChanged();
        }
    }

    private void Cancel()
    {
        Navigation.NavigateTo("/customers");
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
                Text = "Save Customer",
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
                Label = "ABN Lookup",
                Text = "ABN Lookup",
                Icon = "fas fa-search",
                Action = EventCallback.Factory.Create(this, OpenAbnSearchDialog),
                IsDisabled = isProcessing
            },
            new ToolbarAction
            {
                Label = "Generate Code",
                Text = "Generate Customer Code",
                Icon = "fas fa-key",
                Action = EventCallback.Factory.Create(this, GenerateCustomerCode),
                IsDisabled = !string.IsNullOrEmpty(customerModel.Code)
            },
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

    private void OpenAbnSearchDialog()
    {
        showAbnSearchDialog = true;
        abnSearchMode = "abn";
        abnSearchTerm = customerModel.ABN ?? "";
        abnSearchResults.Clear();
        showAbnResults = false;
        StateHasChanged();
    }

    private void CloseAbnSearchDialog()
    {
        showAbnSearchDialog = false;
        StateHasChanged();
    }

    private async Task PerformAbnSearch()
    {
        if (abnSearchMode == "abn")
        {
            if (!string.IsNullOrWhiteSpace(abnSearchTerm))
            {
                var cleanAbn = abnSearchTerm.Replace(" ", "").Replace("-", "");
                await SearchByAbn(cleanAbn);
            }
        }
        else
        {
            await SearchByBusinessName();
        }
    }

    private void SetSearchMode(string mode)
    {
        abnSearchMode = mode;
        StateHasChanged();
    }

    // Contact Management Methods
    private void AddNewContact()
    {
        var newContact = new CustomerContact
        {
            Id = 0, // Temporary ID for tracking in UI
            FirstName = "",
            LastName = "",
            Email = "",
            IsPrimary = !customerContacts.Any() // First contact is primary by default
        };
        customerContacts.Add(newContact);
        editingContactId = newContact.Id;
        StateHasChanged();
    }

    private async Task SaveContact(CustomerContact contact)
    {
        // If this is the only contact, make it primary
        if (customerContacts.Count == 1)
        {
            contact.IsPrimary = true;
        }

        editingContactId = 0;
        StateHasChanged();
    }

    private async Task DeleteContact(CustomerContact contact)
    {
        customerContacts.Remove(contact);

        // If we deleted the primary contact and there are others, make the first one primary
        if (contact.IsPrimary && customerContacts.Any())
        {
            customerContacts.First().IsPrimary = true;
        }

        StateHasChanged();
    }

    private async Task SetContactAsPrimary(CustomerContact contact)
    {
        // Remove primary flag from all other contacts
        foreach (var c in customerContacts)
        {
            c.IsPrimary = false;
        }

        // Set this contact as primary
        contact.IsPrimary = true;
        StateHasChanged();
    }

    private async Task CancelContactEdit()
    {
        // If it's a new contact (Id == 0) and empty, remove it
        var editingContact = customerContacts.FirstOrDefault(c => c.Id == editingContactId);
        if (editingContact != null && editingContact.Id == 0 &&
            string.IsNullOrWhiteSpace(editingContact.FirstName) &&
            string.IsNullOrWhiteSpace(editingContact.LastName) &&
            string.IsNullOrWhiteSpace(editingContact.Email))
        {
            customerContacts.Remove(editingContact);
        }

        editingContactId = 0;
        StateHasChanged();
    }

    // View Models
    private class CustomerModel
    {
        [Required(ErrorMessage = "Customer code is required")]
        public string Code { get; set; } = "";

        [Required(ErrorMessage = "Customer name is required")]
        public string Name { get; set; } = "";

        public string? ABN { get; set; }
        public string? Website { get; set; }
        public string? Industry { get; set; }
        public string? Notes { get; set; }
        public bool IsActive { get; set; } = true;

        // Legacy fields
        public string? ContactPerson { get; set; }
        public string? Email { get; set; }
        public string? PhoneNumber { get; set; }
    }

    private class AddressModel
    {
        public string? AddressLine1 { get; set; }
        public string? AddressLine2 { get; set; }
        public string? City { get; set; }
        public string? State { get; set; }
        public string? PostalCode { get; set; }
        public string? GooglePlaceId { get; set; }
        public string? FormattedAddress { get; set; }
    }

    private class ContactModel
    {
        public string? FirstName { get; set; }
        public string? LastName { get; set; }
        public string? Title { get; set; }
        public string? Department { get; set; }

        [EmailAddress(ErrorMessage = "Invalid email address")]
        public string? Email { get; set; }

        public string? PhoneNumber { get; set; }
        public string? MobileNumber { get; set; }
    }

}