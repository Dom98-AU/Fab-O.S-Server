using Microsoft.AspNetCore.Components;
using Microsoft.EntityFrameworkCore;
using FabOS.WebServer.Data.Contexts;
using FabOS.WebServer.Models.Entities;
using FabOS.WebServer.Components.Shared.Interfaces;
using FabOS.WebServer.Components.Shared;
using FabOS.WebServer.Services.Interfaces;
using FabOS.WebServer.Models.ViewState;
using FabOS.WebServer.Models.Columns;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System;

namespace FabOS.WebServer.Components.Pages
{
    public partial class Catalogues : ComponentBase, IToolbarActionProvider
    {
        [Parameter]
        public string TenantSlug { get; set; } = string.Empty;

        [Inject] private ICatalogueService CatalogueService { get; set; } = default!;
        [Inject] private NavigationManager Navigation { get; set; } = default!;
        [Inject] private ApplicationDbContext DbContext { get; set; } = default!;

        // Loading and data
        private bool isLoading = true;
        private List<Catalogue> allCatalogues = new();
        private List<Catalogue> filteredCatalogues = new();
        private string searchTerm = string.Empty;
        private Dictionary<int, int> itemCounts = new();
        private int companyId;
        private int userId;

        // View state management
        private GenericViewSwitcher<Catalogue>.ViewType currentView = GenericViewSwitcher<Catalogue>.ViewType.Card;
        private ViewState? currentViewState;
        private bool hasUnsavedChanges = false;
        private bool hasCustomColumnConfig = false;

        // Column management
        private List<ColumnDefinition> managedColumns = new();
        private List<GenericTableView<Catalogue>.TableColumn<Catalogue>> tableColumns = new();

        // Selection management
        private List<Catalogue> selectedTableItems = new();
        private List<Catalogue> selectedListItems = new();
        private List<Catalogue> selectedCardItems = new();

        protected override async Task OnInitializedAsync()
        {
            // Get companyId and userId from claims or database based on tenant slug
            var normalizedSlug = TenantSlug.Replace("-", " ");
            var company = await DbContext.Companies
                .FirstOrDefaultAsync(c => c.Code.ToLower() == normalizedSlug.ToLower() ||
                                         c.Code.ToLower().Replace(" ", "-") == TenantSlug.ToLower());

            if (company == null)
            {
                // Handle invalid tenant
                return;
            }

            companyId = company.Id;
            userId = 1; // TODO: Get from auth context

            // Initialize columns
            InitializeColumns();

            // Load data
            await LoadCatalogues();
        }

        private void InitializeColumns()
        {
            managedColumns = new List<ColumnDefinition>
            {
                new ColumnDefinition
                {
                    PropertyName = nameof(Catalogue.Id),
                    DisplayName = "ID",
                    IsVisible = true,
                    Width = 80
                },
                new ColumnDefinition
                {
                    PropertyName = nameof(Catalogue.Name),
                    DisplayName = "Name",
                    IsVisible = true,
                    Width = 250
                },
                new ColumnDefinition
                {
                    PropertyName = nameof(Catalogue.Description),
                    DisplayName = "Description",
                    IsVisible = true,
                    Width = 300
                },
                new ColumnDefinition
                {
                    PropertyName = "Type",
                    DisplayName = "Type",
                    IsVisible = true,
                    Width = 120
                },
                new ColumnDefinition
                {
                    PropertyName = "ItemCount",
                    DisplayName = "Items",
                    IsVisible = true,
                    Width = 100
                },
                new ColumnDefinition
                {
                    PropertyName = "ModifiedDate",
                    DisplayName = "Last Modified",
                    IsVisible = true,
                    Width = 150
                },
                new ColumnDefinition
                {
                    PropertyName = nameof(Catalogue.CreatedDate),
                    DisplayName = "Created",
                    IsVisible = false,
                    Width = 150
                }
            };

            // Build table columns from managed columns
            tableColumns = new List<GenericTableView<Catalogue>.TableColumn<Catalogue>>
            {
                new() { Header = "ID", ValueSelector = c => c.Id.ToString(), IsSortable = true },
                new() { Header = "Name", ValueSelector = c => c.Name, IsSortable = true },
                new() { Header = "Description", ValueSelector = c => c.Description ?? "", IsSortable = true },
                new() { Header = "Type", ValueSelector = c => c.IsSystemCatalogue ? "System" : "Custom", IsSortable = true },
                new() { Header = "Items", ValueSelector = c => GetItemCount(c.Id).ToString(), IsSortable = false },
                new() { Header = "Last Modified", ValueSelector = c => (c.ModifiedDate ?? c.CreatedDate).ToString("MMM dd, yyyy"), IsSortable = true }
            };
        }

        private async Task LoadCatalogues()
        {
            isLoading = true;

            try
            {
                allCatalogues = await CatalogueService.GetCataloguesAsync(companyId);

                // Load item counts for each catalogue
                foreach (var catalogue in allCatalogues)
                {
                    var items = await CatalogueService.GetItemsByCatalogueAsync(catalogue.Id, companyId);
                    itemCounts[catalogue.Id] = items.Count;
                }

                filteredCatalogues = allCatalogues;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading catalogues: {ex.Message}");
            }
            finally
            {
                isLoading = false;
            }
        }

        // Search and filtering
        private void OnSearchChanged(string searchTerm)
        {
            this.searchTerm = searchTerm?.ToLower() ?? string.Empty;
            ApplyFilters();
        }

        private void ApplyFilters()
        {
            if (string.IsNullOrWhiteSpace(searchTerm))
            {
                filteredCatalogues = allCatalogues;
            }
            else
            {
                filteredCatalogues = allCatalogues.Where(c =>
                    c.Name.ToLower().Contains(searchTerm) ||
                    (c.Description?.ToLower().Contains(searchTerm) ?? false)
                ).ToList();
            }

            StateHasChanged();
        }

        // View switching
        private void OnViewChanged(GenericViewSwitcher<Catalogue>.ViewType newView)
        {
            currentView = newView;
            StateHasChanged();
        }

        // View preferences
        private async Task HandleViewLoaded(ViewState viewState)
        {
            currentViewState = viewState;
            // View state loaded - could restore preferences here
            StateHasChanged();
            await Task.CompletedTask;
        }

        // Column management
        private void HandleColumnsChanged(List<ColumnDefinition> columns)
        {
            managedColumns = columns;
            // Rebuild table columns based on visibility
            tableColumns = new List<GenericTableView<Catalogue>.TableColumn<Catalogue>>();
            foreach (var col in columns.Where(c => c.IsVisible))
            {
                switch (col.PropertyName)
                {
                    case nameof(Catalogue.Id):
                        tableColumns.Add(new() { Header = "ID", ValueSelector = c => c.Id.ToString(), IsSortable = true });
                        break;
                    case nameof(Catalogue.Name):
                        tableColumns.Add(new() { Header = "Name", ValueSelector = c => c.Name, IsSortable = true });
                        break;
                    case nameof(Catalogue.Description):
                        tableColumns.Add(new() { Header = "Description", ValueSelector = c => c.Description ?? "", IsSortable = true });
                        break;
                    case "Type":
                        tableColumns.Add(new() { Header = "Type", ValueSelector = c => c.IsSystemCatalogue ? "System" : "Custom", IsSortable = true });
                        break;
                    case "ItemCount":
                        tableColumns.Add(new() { Header = "Items", ValueSelector = c => GetItemCount(c.Id).ToString(), IsSortable = false });
                        break;
                    case "ModifiedDate":
                        tableColumns.Add(new() { Header = "Last Modified", ValueSelector = c => (c.ModifiedDate ?? c.CreatedDate).ToString("MMM dd, yyyy"), IsSortable = true });
                        break;
                    case nameof(Catalogue.CreatedDate):
                        tableColumns.Add(new() { Header = "Created", ValueSelector = c => c.CreatedDate.ToString("MMM dd, yyyy"), IsSortable = true });
                        break;
                }
            }
            hasCustomColumnConfig = true;
            StateHasChanged();
        }

        // Selection management
        private void HandleTableSelectionChanged(List<Catalogue> selected)
        {
            selectedTableItems = selected;
            StateHasChanged();
        }

        private void OnSelectionChanged((Catalogue item, bool selected) args)
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

        private List<Catalogue> GetSelectedCatalogues()
        {
            return currentView switch
            {
                GenericViewSwitcher<Catalogue>.ViewType.Table => selectedTableItems,
                GenericViewSwitcher<Catalogue>.ViewType.List => selectedListItems,
                GenericViewSwitcher<Catalogue>.ViewType.Card => selectedCardItems,
                _ => new List<Catalogue>()
            };
        }

        // Row click handlers
        private void HandleRowClick(Catalogue catalogue)
        {
            // Single click - select
            if (selectedTableItems.Contains(catalogue))
                selectedTableItems.Remove(catalogue);
            else
                selectedTableItems.Add(catalogue);

            StateHasChanged();
        }

        private void HandleRowDoubleClick(Catalogue catalogue)
        {
            // Double click - open
            NavigateToCatalogue(catalogue.Id);
        }

        // Helper methods
        private int GetItemCount(int catalogueId)
        {
            return itemCounts.TryGetValue(catalogueId, out int count) ? count : 0;
        }

        // Navigation methods
        private void NavigateToCatalogue(int catalogueId)
        {
            Navigation.NavigateTo($"/{TenantSlug}/trace/catalogues/{catalogueId}");
        }

        private void CreateNewCatalogue()
        {
            Navigation.NavigateTo($"/{TenantSlug}/trace/catalogues/new");
        }

        private void EditCatalogue(int catalogueId)
        {
            Navigation.NavigateTo($"/{TenantSlug}/trace/catalogues/{catalogueId}/edit");
        }

        private async Task DeleteCatalogue(int catalogueId)
        {
            // TODO: Implement delete confirmation dialog
            var success = await CatalogueService.DeleteCatalogueAsync(catalogueId, companyId, userId);
            if (success)
            {
                await LoadCatalogues();
            }
        }

        private async Task DeleteSelectedCatalogues()
        {
            var selected = GetSelectedCatalogues();
            if (!selected.Any()) return;

            // TODO: Add confirmation dialog

            foreach (var catalogue in selected.Where(c => !c.IsSystemCatalogue))
            {
                await CatalogueService.DeleteCatalogueAsync(catalogue.Id, companyId, userId);
            }

            // Clear selections
            selectedTableItems.Clear();
            selectedListItems.Clear();
            selectedCardItems.Clear();

            await LoadCatalogues();
        }

        // IToolbarActionProvider implementation
        public ToolbarActionGroup GetActions()
        {
            var selected = GetSelectedCatalogues();
            var hasSelection = selected.Any();
            var hasNonSystemSelection = selected.Any(c => !c.IsSystemCatalogue);

            var group = new ToolbarActionGroup();

            // PRIMARY ACTIONS - [New] [Delete]
            group.PrimaryActions = new List<ToolbarAction>
            {
                new ToolbarAction
                {
                    Label = "New",
                    Text = "New",
                    Icon = "fas fa-plus",
                    ActionFunc = async () => { CreateNewCatalogue(); await Task.CompletedTask; },
                    Style = ToolbarActionStyle.Primary,
                    Tooltip = "Create new catalogue"
                },
                new ToolbarAction
                {
                    Label = "Delete",
                    Text = "Delete",
                    Icon = "fas fa-trash",
                    ActionFunc = DeleteSelectedCatalogues,
                    IsDisabled = !hasNonSystemSelection,
                    Style = ToolbarActionStyle.Danger,
                    Tooltip = hasSelection ? (hasNonSystemSelection ? $"Delete {selected.Count} catalogue(s)" : "Cannot delete system catalogues") : "Select catalogues to delete"
                }
            };

            // MENU ACTIONS - [Export]
            group.MenuActions = new List<ToolbarAction>
            {
                new ToolbarAction
                {
                    Label = "Export",
                    Text = "Export to Excel",
                    Icon = "fas fa-file-excel",
                    ActionFunc = async () => { /* TODO: Implement export */ await Task.CompletedTask; },
                    Tooltip = "Export catalogues to Excel"
                }
            };

            return group;
        }
    }
}
