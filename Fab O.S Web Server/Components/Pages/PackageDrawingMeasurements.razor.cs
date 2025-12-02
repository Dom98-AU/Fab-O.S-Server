using FabOS.WebServer.Components.Shared;
using FabOS.WebServer.Components.Shared.Interfaces;
using FabOS.WebServer.Data.Contexts;
using FabOS.WebServer.Models.Entities;
using FabOS.WebServer.Models.Columns;
using FabOS.WebServer.Models.Filtering;
using FabOS.WebServer.Models.ViewState;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.EntityFrameworkCore;

namespace FabOS.WebServer.Components.Pages
{
    public partial class PackageDrawingMeasurements : ComponentBase, IToolbarActionProvider, IAsyncDisposable
    {
        [Inject] private IDbContextFactory<ApplicationDbContext> DbContextFactory { get; set; } = default!;
        [Inject] private ILogger<PackageDrawingMeasurements> Logger { get; set; } = default!;
        [Inject] private NavigationManager NavigationManager { get; set; } = default!;

        [Parameter] public string? TenantSlug { get; set; }
        [Parameter] public int PackageId { get; set; }
        [Parameter] public int DrawingId { get; set; }

        // Data
        private List<TraceTakeoffMeasurement> allMeasurements = new();
        private List<TraceTakeoffMeasurement> filteredMeasurements = new();
        private bool isLoading = true;
        private string drawingName = string.Empty;
        private string drawingNumber = string.Empty;
        private string searchQuery = string.Empty;

        // Export modal reference
        private MeasurementExportModal? _exportModal;

        // View state
        private GenericViewSwitcher<TraceTakeoffMeasurement>.ViewType currentView = GenericViewSwitcher<TraceTakeoffMeasurement>.ViewType.List;
        private ViewState currentViewState = new();
        private bool hasUnsavedChanges = false;
        private bool hasCustomColumnConfig = false;

        // Table columns
        private List<GenericTableView<TraceTakeoffMeasurement>.TableColumn<TraceTakeoffMeasurement>> tableColumns = new();

        // Column management
        private List<ColumnDefinition> columnDefinitions = new();
        private List<ColumnDefinition> managedColumns = new();

        // Selection tracking
        private List<TraceTakeoffMeasurement> selectedTableItems = new();
        private List<TraceTakeoffMeasurement> selectedListItems = new();
        private List<TraceTakeoffMeasurement> selectedCardItems = new();

        // SignalR
        private HubConnection? hubConnection;

        // Click-to-highlight feature
        private string? selectedAnnotationId = null;
        private Dictionary<int, string> measurementToAnnotationMap = new(); // measurementId -> annotationId

        // IToolbarActionProvider implementation
        public ToolbarActionGroup GetActions()
        {
            var selected = GetSelectedMeasurements();
            var hasSelection = selected.Any();

            return new ToolbarActionGroup
            {
                PrimaryActions = new List<FabOS.WebServer.Components.Shared.Interfaces.ToolbarAction>
                {
                    new FabOS.WebServer.Components.Shared.Interfaces.ToolbarAction
                    {
                        Text = "Refresh",
                        Icon = "fas fa-sync-alt",
                        ActionFunc = () => RefreshMeasurements(),
                        IsDisabled = false,
                        Tooltip = "Refresh measurements"
                    },
                    new FabOS.WebServer.Components.Shared.Interfaces.ToolbarAction
                    {
                        Text = "Delete",
                        Icon = "fas fa-trash",
                        ActionFunc = () => DeleteSelected(),
                        IsDisabled = !hasSelection,
                        Tooltip = hasSelection ? "Delete selected measurements" : "Select measurements to delete"
                    }
                },
                MenuActions = new List<FabOS.WebServer.Components.Shared.Interfaces.ToolbarAction>
                {
                    new FabOS.WebServer.Components.Shared.Interfaces.ToolbarAction
                    {
                        Text = "Export to Excel",
                        Icon = "fas fa-file-excel",
                        ActionFunc = () => OpenExportModal(),
                        IsDisabled = !allMeasurements.Any(),
                        Tooltip = allMeasurements.Any() ? "Export measurements to Excel" : "No measurements to export"
                    }
                },
                RelatedActions = new List<FabOS.WebServer.Components.Shared.Interfaces.ToolbarAction>
                {
                    new FabOS.WebServer.Components.Shared.Interfaces.ToolbarAction
                    {
                        Text = "Back to Package",
                        Icon = "fas fa-arrow-left",
                        ActionFunc = () => { NavigateBack(); return Task.CompletedTask; },
                        IsDisabled = false,
                        Tooltip = "Return to package details"
                    }
                }
            };
        }

        private List<TraceTakeoffMeasurement> GetSelectedMeasurements()
        {
            return currentView switch
            {
                GenericViewSwitcher<TraceTakeoffMeasurement>.ViewType.Table => selectedTableItems,
                GenericViewSwitcher<TraceTakeoffMeasurement>.ViewType.List => selectedListItems,
                GenericViewSwitcher<TraceTakeoffMeasurement>.ViewType.Card => selectedCardItems,
                _ => new List<TraceTakeoffMeasurement>()
            };
        }

        protected override async Task OnInitializedAsync()
        {
            Logger.LogInformation("[PackageDrawingMeasurements] 🚀 OnInitializedAsync - PackageId={PackageId}, DrawingId={DrawingId}", PackageId, DrawingId);

            await LoadDrawingInfo();
            InitializeColumnDefinitions();
            await UpdateTableColumns();
            await LoadMeasurements();
            await InitializeSignalR();
        }

        private async Task LoadDrawingInfo()
        {
            try
            {
                await using var dbContext = await DbContextFactory.CreateDbContextAsync();
                var drawing = await dbContext.PackageDrawings
                    .Where(d => d.Id == DrawingId)
                    .Select(d => new { d.DrawingTitle, d.DrawingNumber })
                    .FirstOrDefaultAsync();

                drawingName = drawing?.DrawingTitle ?? "Unknown Drawing";
                drawingNumber = drawing?.DrawingNumber ?? string.Empty;
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "[PackageDrawingMeasurements] Error loading drawing info");
                drawingName = "Unknown Drawing";
            }
        }

        private void InitializeColumnDefinitions()
        {
            columnDefinitions = new List<ColumnDefinition>
            {
                new ColumnDefinition
                {
                    Id = "item-code",
                    PropertyName = "ItemCode",
                    DisplayName = "Item Code",
                    Order = 0,
                    IsVisible = true,
                    IsRequired = true
                },
                new ColumnDefinition
                {
                    Id = "description",
                    PropertyName = "Description",
                    DisplayName = "Description",
                    Order = 1,
                    IsVisible = true
                },
                new ColumnDefinition
                {
                    Id = "category",
                    PropertyName = "Category",
                    DisplayName = "Category",
                    Order = 2,
                    IsVisible = true
                },
                new ColumnDefinition
                {
                    Id = "type",
                    PropertyName = "MeasurementType",
                    DisplayName = "Type",
                    Order = 3,
                    IsVisible = true
                },
                new ColumnDefinition
                {
                    Id = "value",
                    PropertyName = "Value",
                    DisplayName = "Value",
                    Order = 4,
                    IsVisible = true
                },
                new ColumnDefinition
                {
                    Id = "weight",
                    PropertyName = "CalculatedWeight",
                    DisplayName = "Weight (kg)",
                    Order = 5,
                    IsVisible = true
                },
                new ColumnDefinition
                {
                    Id = "created-date",
                    PropertyName = "CreatedDate",
                    DisplayName = "Date Created",
                    Order = 6,
                    IsVisible = true
                }
            };

            managedColumns = new List<ColumnDefinition>(columnDefinitions);
        }

        private async Task UpdateTableColumns()
        {
            // Build table columns based on visible column definitions
            tableColumns = columnDefinitions
                .Where(c => c.IsVisible)
                .OrderBy(c => c.Order)
                .Select(c => CreateTableColumn(c))
                .Where(col => col != null)
                .ToList()!;

            await Task.CompletedTask;
        }

        private GenericTableView<TraceTakeoffMeasurement>.TableColumn<TraceTakeoffMeasurement>? CreateTableColumn(ColumnDefinition columnDef)
        {
            var baseColumn = columnDef.PropertyName switch
            {
                "ItemCode" => new GenericTableView<TraceTakeoffMeasurement>.TableColumn<TraceTakeoffMeasurement>
                {
                    Header = columnDef.DisplayName,
                    ValueSelector = m => m.CatalogueItem?.ItemCode ?? "",
                    CssClass = "text-start"
                },
                "Description" => new GenericTableView<TraceTakeoffMeasurement>.TableColumn<TraceTakeoffMeasurement>
                {
                    Header = columnDef.DisplayName,
                    ValueSelector = m => m.CatalogueItem?.Description ?? "",
                    CssClass = "text-start"
                },
                "Category" => new GenericTableView<TraceTakeoffMeasurement>.TableColumn<TraceTakeoffMeasurement>
                {
                    Header = columnDef.DisplayName,
                    ValueSelector = m => m.CatalogueItem?.Category ?? "Uncategorized",
                    CssClass = "text-start"
                },
                "MeasurementType" => new GenericTableView<TraceTakeoffMeasurement>.TableColumn<TraceTakeoffMeasurement>
                {
                    Header = columnDef.DisplayName,
                    ValueSelector = m => m.MeasurementType,
                    CssClass = "text-center"
                },
                "Value" => new GenericTableView<TraceTakeoffMeasurement>.TableColumn<TraceTakeoffMeasurement>
                {
                    Header = columnDef.DisplayName,
                    ValueSelector = m => $"{m.Value:F2} {m.Unit}",
                    CssClass = "text-end"
                },
                "CalculatedWeight" => new GenericTableView<TraceTakeoffMeasurement>.TableColumn<TraceTakeoffMeasurement>
                {
                    Header = columnDef.DisplayName,
                    ValueSelector = m => m.CalculatedWeight.HasValue ? $"{m.CalculatedWeight.Value:F2}" : "—",
                    CssClass = "text-end"
                },
                "CreatedDate" => new GenericTableView<TraceTakeoffMeasurement>.TableColumn<TraceTakeoffMeasurement>
                {
                    Header = columnDef.DisplayName,
                    ValueSelector = m => m.CreatedDate.ToLocalTime().ToString("g"),
                    CssClass = "text-center"
                },
                _ => null
            };

            if (baseColumn != null)
            {
                baseColumn.PropertyName = columnDef.PropertyName;
            }

            return baseColumn;
        }

        private async Task LoadMeasurements()
        {
            try
            {
                isLoading = true;
                Logger.LogInformation("[PackageDrawingMeasurements] 📊 Starting to load measurements for DrawingId={DrawingId}", DrawingId);

                await using var dbContext = await DbContextFactory.CreateDbContextAsync();
                allMeasurements = await dbContext.TraceTakeoffMeasurements
                    .Include(m => m.CatalogueItem)
                    .Where(m => m.PackageDrawingId == DrawingId)
                    .OrderByDescending(m => m.CreatedDate)
                    .ToListAsync();

                Logger.LogInformation("[PackageDrawingMeasurements] ✓ Loaded {Count} measurements from database", allMeasurements.Count);

                ApplySearch();

                Logger.LogInformation("[PackageDrawingMeasurements] ✓ After ApplySearch: filteredMeasurements count = {Count}", filteredMeasurements.Count);

                // Load annotation mappings for click-to-highlight
                await LoadAnnotationMappingAsync();
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "[PackageDrawingMeasurements] ✗ Error loading measurements");
            }
            finally
            {
                isLoading = false;
                Logger.LogInformation("[PackageDrawingMeasurements] Loading complete. isLoading={IsLoading}", isLoading);
            }
        }

        /// <summary>
        /// Loads the mapping between measurements and their PDF annotation IDs
        /// </summary>
        private async Task LoadAnnotationMappingAsync()
        {
            try
            {
                measurementToAnnotationMap.Clear();

                await using var dbContext = await DbContextFactory.CreateDbContextAsync();

                // Load PDF annotations for this drawing that have associated measurements
                var annotations = await dbContext.PdfAnnotations
                    .Where(a => a.PackageDrawingId == DrawingId && a.TraceTakeoffMeasurementId != null)
                    .Select(a => new { a.TraceTakeoffMeasurementId, a.AnnotationId })
                    .ToListAsync();

                foreach (var annotation in annotations)
                {
                    if (annotation.TraceTakeoffMeasurementId.HasValue)
                    {
                        measurementToAnnotationMap[annotation.TraceTakeoffMeasurementId.Value] = annotation.AnnotationId;
                    }
                }

                Logger.LogInformation("[PackageDrawingMeasurements] ✓ Loaded {Count} annotation mappings for click-to-highlight",
                    measurementToAnnotationMap.Count);
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "[PackageDrawingMeasurements] ❌ Error loading annotation mappings - click-to-highlight may not work");
                measurementToAnnotationMap.Clear();
            }
        }

        private void OnSearchChanged(string query)
        {
            searchQuery = query;
            ApplySearch();
        }

        private void ApplySearch()
        {
            Logger.LogInformation("[PackageDrawingMeasurements] 🔍 ApplySearch called - searchQuery='{SearchQuery}', allMeasurements.Count={Count}",
                searchQuery ?? "(null)", allMeasurements.Count);

            if (string.IsNullOrWhiteSpace(searchQuery))
            {
                filteredMeasurements = new List<TraceTakeoffMeasurement>(allMeasurements);
                Logger.LogInformation("[PackageDrawingMeasurements] No search query - showing all {Count} measurements", filteredMeasurements.Count);
            }
            else
            {
                var query = searchQuery.ToLower();
                filteredMeasurements = allMeasurements.Where(m =>
                    (m.CatalogueItem?.ItemCode?.ToLower().Contains(query) ?? false) ||
                    (m.CatalogueItem?.Description?.ToLower().Contains(query) ?? false) ||
                    (m.CatalogueItem?.Category?.ToLower().Contains(query) ?? false) ||
                    m.MeasurementType.ToLower().Contains(query)
                ).ToList();
                Logger.LogInformation("[PackageDrawingMeasurements] Search filtered to {Count} measurements", filteredMeasurements.Count);
            }

            Logger.LogInformation("[PackageDrawingMeasurements] 🎨 Triggering StateHasChanged - will render {Count} measurements", filteredMeasurements.Count);
            StateHasChanged();
        }

        private async Task RefreshMeasurements()
        {
            await LoadMeasurements();
        }

        private async Task DeleteSelected()
        {
            try
            {
                var itemsToDelete = currentView switch
                {
                    GenericViewSwitcher<TraceTakeoffMeasurement>.ViewType.Table => selectedTableItems,
                    GenericViewSwitcher<TraceTakeoffMeasurement>.ViewType.List => selectedListItems,
                    GenericViewSwitcher<TraceTakeoffMeasurement>.ViewType.Card => selectedCardItems,
                    _ => new List<TraceTakeoffMeasurement>()
                };

                if (!itemsToDelete.Any())
                {
                    return;
                }

                await using var dbContext = await DbContextFactory.CreateDbContextAsync();
                foreach (var measurement in itemsToDelete)
                {
                    dbContext.TraceTakeoffMeasurements.Remove(measurement);
                }

                await dbContext.SaveChangesAsync();

                // Clear selections
                selectedTableItems.Clear();
                selectedListItems.Clear();
                selectedCardItems.Clear();

                await LoadMeasurements();

                Logger.LogInformation("[PackageDrawingMeasurements] Deleted {Count} measurements", itemsToDelete.Count);
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "[PackageDrawingMeasurements] Error deleting measurements");
            }
        }

        // View management
        private void OnViewChanged(GenericViewSwitcher<TraceTakeoffMeasurement>.ViewType view)
        {
            currentView = view;
            Logger.LogInformation("[PackageDrawingMeasurements] View changed to: {View}", view);
        }

        private void HandleViewLoaded(ViewState viewState)
        {
            currentViewState = viewState;
            hasUnsavedChanges = false;
            StateHasChanged();
        }

        private void HandleColumnsChanged(List<ColumnDefinition> columns)
        {
            managedColumns = columns;
            hasCustomColumnConfig = true;
        }

        // Selection handlers
        private void HandleTableSelectionChanged(List<TraceTakeoffMeasurement> items)
        {
            selectedTableItems = items;
            StateHasChanged();
        }

        private void HandleListSelectionChanged(List<TraceTakeoffMeasurement> items)
        {
            selectedListItems = items;
            StateHasChanged();
        }

        private void HandleCardSelectionChanged(List<TraceTakeoffMeasurement> items)
        {
            selectedCardItems = items;
            StateHasChanged();
        }

        // Click handlers
        private void HandleRowClick(TraceTakeoffMeasurement measurement)
        {
            Logger.LogInformation("[PackageDrawingMeasurements] Measurement clicked: {Id}", measurement.Id);
        }

        private void HandleRowDoubleClick(TraceTakeoffMeasurement measurement)
        {
            Logger.LogInformation("[PackageDrawingMeasurements] Measurement double-clicked: {Id} - Navigating to detail page", measurement.Id);
            NavigationManager.NavigateTo($"/{TenantSlug}/trace/measurements/{measurement.Id}");
        }

        private async Task InitializeSignalR()
        {
            try
            {
                Logger.LogInformation("[PackageDrawingMeasurements] ============================================");
                Logger.LogInformation("[PackageDrawingMeasurements] 🔌 INITIALIZING SIGNALR HUB CONNECTION");
                Logger.LogInformation("[PackageDrawingMeasurements] PackageId: {PackageId}, DrawingId: {DrawingId}", PackageId, DrawingId);

                // Build the hub URL using NavigationManager to get base URI
                var hubUrl = NavigationManager.ToAbsoluteUri("/measurementHub").ToString();
                Logger.LogInformation("[PackageDrawingMeasurements] 🔌 Connecting to SignalR Hub at {HubUrl}", hubUrl);

                hubConnection = new HubConnectionBuilder()
                    .WithUrl(hubUrl)
                    .WithAutomaticReconnect(new[] { TimeSpan.Zero, TimeSpan.FromSeconds(2), TimeSpan.FromSeconds(5), TimeSpan.FromSeconds(10), TimeSpan.FromSeconds(30) })
                    .Build();

                // Register event handlers BEFORE connecting - use the same events as TakeoffMeasurementPanel
                hubConnection.On<MeasurementEventData>("MeasurementCreated", async (data) =>
                {
                    try
                    {
                        if (data.PackageDrawingId == DrawingId)
                        {
                            Logger.LogInformation("[PackageDrawingMeasurements] ✓ Received MeasurementCreated: MeasurementId={MeasurementId}",
                                data.MeasurementId);

                            await InvokeAsync(async () =>
                            {
                                await LoadMeasurements();
                                StateHasChanged();
                            });
                        }
                    }
                    catch (Exception ex)
                    {
                        Logger.LogError(ex, "[PackageDrawingMeasurements] ❌ Error handling MeasurementCreated event");
                    }
                });

                hubConnection.On<MeasurementEventData>("MeasurementDeleted", async (data) =>
                {
                    try
                    {
                        if (data.PackageDrawingId == DrawingId)
                        {
                            Logger.LogInformation("[PackageDrawingMeasurements] ✓ Received MeasurementDeleted: PackageDrawingId={PackageDrawingId}, MeasurementId={MeasurementId}",
                                data.PackageDrawingId, data.MeasurementId);

                            await InvokeAsync(async () =>
                            {
                                await LoadMeasurements();
                                StateHasChanged();
                            });
                        }
                    }
                    catch (Exception ex)
                    {
                        Logger.LogError(ex, "[PackageDrawingMeasurements] ❌ Error handling MeasurementDeleted event");
                    }
                });

                hubConnection.On<MeasurementEventData>("MeasurementUpdated", async (data) =>
                {
                    try
                    {
                        if (data.PackageDrawingId == DrawingId)
                        {
                            Logger.LogInformation("[PackageDrawingMeasurements] ✓ Received MeasurementUpdated: MeasurementId={MeasurementId}",
                                data.MeasurementId);

                            await InvokeAsync(async () =>
                            {
                                await LoadMeasurements();
                                StateHasChanged();
                            });
                        }
                    }
                    catch (Exception ex)
                    {
                        Logger.LogError(ex, "[PackageDrawingMeasurements] ❌ Error handling MeasurementUpdated event");
                    }
                });

                hubConnection.On<MeasurementEventData>("InstantJsonUpdated", async (data) =>
                {
                    try
                    {
                        if (data.PackageDrawingId == DrawingId)
                        {
                            Logger.LogInformation("[PackageDrawingMeasurements] ✓ Received InstantJsonUpdated for PackageDrawingId={PackageDrawingId} - reloading measurements",
                                data.PackageDrawingId);

                            await InvokeAsync(async () =>
                            {
                                await LoadMeasurements();
                                StateHasChanged();
                            });
                        }
                    }
                    catch (Exception ex)
                    {
                        Logger.LogError(ex, "[PackageDrawingMeasurements] ❌ Error handling InstantJsonUpdated event");
                    }
                });

                // Handle reconnection
                hubConnection.Reconnected += async (connectionId) =>
                {
                    Logger.LogInformation("[PackageDrawingMeasurements] ✓ SignalR Hub reconnected with ConnectionId={ConnectionId}", connectionId);

                    // Re-subscribe to drawing group after reconnection
                    if (DrawingId > 0)
                    {
                        await hubConnection.InvokeAsync("SubscribeToDrawing", DrawingId);
                        Logger.LogInformation("[PackageDrawingMeasurements] ✓ Re-subscribed to Drawing_{DrawingId}", DrawingId);
                    }

                    // Refresh to catch any missed updates
                    await InvokeAsync(async () =>
                    {
                        await LoadMeasurements();
                        StateHasChanged();
                    });
                };

                hubConnection.Reconnecting += (error) =>
                {
                    Logger.LogWarning("[PackageDrawingMeasurements] ⚠️ SignalR Hub reconnecting...");
                    return Task.CompletedTask;
                };

                hubConnection.Closed += async (error) =>
                {
                    if (error != null)
                    {
                        Logger.LogError(error, "[PackageDrawingMeasurements] ❌ SignalR Hub connection closed with error");
                    }
                    else
                    {
                        Logger.LogInformation("[PackageDrawingMeasurements] SignalR Hub connection closed");
                    }
                };

                // Start the connection
                await hubConnection.StartAsync();
                Logger.LogInformation("[PackageDrawingMeasurements] ✓ SignalR Hub connected with ConnectionId={ConnectionId}", hubConnection.ConnectionId);

                // Subscribe to the drawing-specific group
                if (DrawingId > 0)
                {
                    await hubConnection.InvokeAsync("SubscribeToDrawing", DrawingId);
                    Logger.LogInformation("[PackageDrawingMeasurements] ✓ Subscribed to Drawing_{DrawingId} group", DrawingId);
                }
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "[PackageDrawingMeasurements] ❌❌❌ FAILED TO INITIALIZE SIGNALR HUB CONNECTION ❌❌❌");
                Logger.LogError(ex, "[PackageDrawingMeasurements] Exception Type: {ExceptionType}", ex.GetType().Name);
                Logger.LogError(ex, "[PackageDrawingMeasurements] Exception Message: {Message}", ex.Message);
                Logger.LogError(ex, "[PackageDrawingMeasurements] Stack Trace: {StackTrace}", ex.StackTrace);
            }
        }

        private void NavigateBack()
        {
            NavigationManager.NavigateTo($"/{TenantSlug}/trace/packages/{PackageId}");
        }

        public async ValueTask DisposeAsync()
        {
            try
            {
                if (hubConnection != null)
                {
                    // Unsubscribe from drawing group before disconnecting
                    if (DrawingId > 0 && hubConnection.State == HubConnectionState.Connected)
                    {
                        await hubConnection.InvokeAsync("UnsubscribeFromDrawing", DrawingId);
                        Logger.LogInformation("[PackageDrawingMeasurements] ✓ Unsubscribed from Drawing_{DrawingId}", DrawingId);
                    }

                    // Stop and dispose the connection
                    await hubConnection.StopAsync();
                    await hubConnection.DisposeAsync();
                    Logger.LogInformation("[PackageDrawingMeasurements] ✓ SignalR Hub connection disposed");
                }
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "[PackageDrawingMeasurements] ❌ Error disposing SignalR Hub connection");
            }
        }

        /// <summary>
        /// DTO for SignalR event data - matches the anonymous object sent from MeasurementHubService
        /// </summary>
        private class MeasurementEventData
        {
            public int PackageDrawingId { get; set; }
            public int MeasurementId { get; set; }
            public string? AnnotationId { get; set; }
        }

        /// <summary>
        /// Helper method to check if a measurement should be highlighted based on annotation selection
        /// </summary>
        private bool IsMeasurementHighlighted(TraceTakeoffMeasurement measurement)
        {
            if (string.IsNullOrEmpty(selectedAnnotationId))
            {
                return false;
            }

            // Check if this measurement has an annotation that matches the selected one
            if (measurementToAnnotationMap.TryGetValue(measurement.Id, out string? annotationId))
            {
                return annotationId == selectedAnnotationId;
            }

            return false;
        }

        // ========================================
        // Export Functionality
        // ========================================

        /// <summary>
        /// Open the export modal
        /// </summary>
        private Task OpenExportModal()
        {
            Logger.LogInformation("[PackageDrawingMeasurements] Opening export modal");
            _exportModal?.Show();
            return Task.CompletedTask;
        }

        /// <summary>
        /// Handle export modal close
        /// </summary>
        private Task HandleExportModalClose()
        {
            Logger.LogInformation("[PackageDrawingMeasurements] Export modal closed");
            return Task.CompletedTask;
        }
    }
}
