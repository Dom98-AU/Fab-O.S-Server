using FabOS.WebServer.Models.Entities;
using FabOS.WebServer.Services;
using FabOS.WebServer.Services.Interfaces;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.JSInterop;

namespace FabOS.WebServer.Components.Shared
{
    public partial class TakeoffMeasurementPanel : ComponentBase, IAsyncDisposable
    {
        [Inject] private ITakeoffCatalogueService CatalogueService { get; set; } = default!;
        [Inject] private ILogger<TakeoffMeasurementPanel> Logger { get; set; } = default!;
        [Inject] private IJSRuntime JSRuntime { get; set; } = default!;
        [Inject] private NavigationManager NavigationManager { get; set; } = default!;

        [Parameter] public int PackageDrawingId { get; set; }
        [Parameter] public string PdfContainerId { get; set; } = "pdf-container";
        [Parameter] public bool IsVisible { get; set; } = true;
        [Parameter] public EventCallback<bool> IsVisibleChanged { get; set; }
        [Parameter] public EventCallback OnMeasurementSaved { get; set; }

        // Current measurement (being created/edited)
        public MeasurementCalculationResult? CurrentMeasurement { get; set; }

        private List<TraceTakeoffMeasurement> measurements = new();
        private bool isLoadingMeasurements = false;
        private string? errorMessage = null;
        private string? expandedCategory = null;

        // Height management
        private FooterHeight currentHeight = FooterHeight.Default;
        private enum FooterHeight
        {
            Default,   // 200px
            Expanded,  // 350px
            Full       // 60vh
        }

        private const int companyId = 1; // TODO: Get from tenant context

        // SignalR Hub connection (C# client, NOT Blazor circuit)
        private HubConnection? _hubConnection;

        private decimal TotalWeight => measurements.Sum(m => m.CalculatedWeight ?? 0);

        private Dictionary<string, List<TraceTakeoffMeasurement>> GroupedMeasurements =>
            measurements
                .Where(m => m.CatalogueItem != null)
                .GroupBy(m => m.CatalogueItem!.Category)
                .ToDictionary(g => g.Key, g => g.ToList());

        protected override async Task OnInitializedAsync()
        {
            // Connect to SignalR Hub using C# client (NOT JavaScript interop)
            await InitializeSignalRConnectionAsync();
        }

        private async Task InitializeSignalRConnectionAsync()
        {
            try
            {
                Logger.LogInformation("[TakeoffMeasurementPanel] ============================================");
                Logger.LogInformation("[TakeoffMeasurementPanel] 🔌 INITIALIZING SIGNALR HUB CONNECTION");
                Logger.LogInformation("[TakeoffMeasurementPanel] PackageDrawingId: {PackageDrawingId}", PackageDrawingId);

                // Build the hub URL using NavigationManager to get base URI
                var hubUrl = NavigationManager.ToAbsoluteUri("/measurementHub").ToString();

                Logger.LogInformation("[TakeoffMeasurementPanel] 🔌 Connecting to SignalR Hub at {HubUrl}", hubUrl);

                _hubConnection = new HubConnectionBuilder()
                    .WithUrl(hubUrl)
                    .WithAutomaticReconnect(new[] { TimeSpan.Zero, TimeSpan.FromSeconds(2), TimeSpan.FromSeconds(5), TimeSpan.FromSeconds(10), TimeSpan.FromSeconds(30) })
                    .Build();

                // Register event handlers BEFORE connecting
                _hubConnection.On<MeasurementEventData>("MeasurementDeleted", async (data) =>
                {
                    try
                    {
                        if (data.PackageDrawingId == PackageDrawingId)
                        {
                            Logger.LogInformation("[TakeoffMeasurementPanel] ✓ Received MeasurementDeleted: PackageDrawingId={PackageDrawingId}, MeasurementId={MeasurementId}, AnnotationId={AnnotationId}",
                                data.PackageDrawingId, data.MeasurementId, data.AnnotationId ?? "null");

                            await InvokeAsync(async () =>
                            {
                                // Delete annotation from PDF viewer if annotation ID is provided
                                if (!string.IsNullOrEmpty(data.AnnotationId))
                                {
                                    try
                                    {
                                        await JSRuntime.InvokeVoidAsync("nutrientViewer.deleteAnnotationById", PdfContainerId, data.AnnotationId);
                                        Logger.LogInformation("[TakeoffMeasurementPanel] ✓ Deleted annotation {AnnotationId} from PDF viewer via SignalR", data.AnnotationId);
                                    }
                                    catch (Exception jsEx)
                                    {
                                        Logger.LogWarning(jsEx, "[TakeoffMeasurementPanel] Could not delete annotation {AnnotationId} from PDF viewer", data.AnnotationId);
                                    }
                                }

                                // Refresh measurements panel
                                await RefreshMeasurementsAsync();
                            });
                        }
                    }
                    catch (Exception ex)
                    {
                        Logger.LogError(ex, "[TakeoffMeasurementPanel] ❌ Error handling MeasurementDeleted event");
                    }
                });

                _hubConnection.On<MeasurementEventData>("MeasurementCreated", async (data) =>
                {
                    try
                    {
                        if (data.PackageDrawingId == PackageDrawingId)
                        {
                            Logger.LogInformation("[TakeoffMeasurementPanel] ✓ Received MeasurementCreated: MeasurementId={MeasurementId}",
                                data.MeasurementId);

                            await InvokeAsync(async () =>
                            {
                                await RefreshMeasurementsAsync();
                            });
                        }
                    }
                    catch (Exception ex)
                    {
                        Logger.LogError(ex, "[TakeoffMeasurementPanel] ❌ Error handling MeasurementCreated event");
                    }
                });

                _hubConnection.On<MeasurementEventData>("MeasurementUpdated", async (data) =>
                {
                    try
                    {
                        if (data.PackageDrawingId == PackageDrawingId)
                        {
                            Logger.LogInformation("[TakeoffMeasurementPanel] ✓ Received MeasurementUpdated: MeasurementId={MeasurementId}",
                                data.MeasurementId);

                            await InvokeAsync(async () =>
                            {
                                await RefreshMeasurementsAsync();
                            });
                        }
                    }
                    catch (Exception ex)
                    {
                        Logger.LogError(ex, "[TakeoffMeasurementPanel] ❌ Error handling MeasurementUpdated event");
                    }
                });

                _hubConnection.On<MeasurementEventData>("InstantJsonUpdated", async (data) =>
                {
                    try
                    {
                        if (data.PackageDrawingId == PackageDrawingId)
                        {
                            Logger.LogInformation("[TakeoffMeasurementPanel] ✓ Received InstantJsonUpdated for PackageDrawingId={PackageDrawingId} - reloading PDF annotations and measurements",
                                data.PackageDrawingId);

                            await InvokeAsync(async () =>
                            {
                                // Reload PDF annotations from database (autosave from another tab)
                                try
                                {
                                    await JSRuntime.InvokeVoidAsync("nutrientViewer.reloadInstantJson", PdfContainerId, PackageDrawingId);
                                    Logger.LogInformation("[TakeoffMeasurementPanel] ✓ Reloaded PDF annotations from database via SignalR");
                                }
                                catch (Exception jsEx)
                                {
                                    Logger.LogWarning(jsEx, "[TakeoffMeasurementPanel] Could not reload PDF annotations");
                                }

                                // Also refresh measurements panel
                                await RefreshMeasurementsAsync();
                            });
                        }
                    }
                    catch (Exception ex)
                    {
                        Logger.LogError(ex, "[TakeoffMeasurementPanel] ❌ Error handling InstantJsonUpdated event");
                    }
                });

                // Handle reconnection
                _hubConnection.Reconnected += async (connectionId) =>
                {
                    Logger.LogInformation("[TakeoffMeasurementPanel] ✓ SignalR Hub reconnected with ConnectionId={ConnectionId}", connectionId);

                    // Re-subscribe to drawing group after reconnection
                    if (PackageDrawingId > 0)
                    {
                        await _hubConnection.InvokeAsync("SubscribeToDrawing", PackageDrawingId);
                        Logger.LogInformation("[TakeoffMeasurementPanel] ✓ Re-subscribed to Drawing_{DrawingId}", PackageDrawingId);
                    }

                    // Refresh to catch any missed updates
                    await InvokeAsync(async () =>
                    {
                        await RefreshMeasurementsAsync();
                    });
                };

                _hubConnection.Reconnecting += (error) =>
                {
                    Logger.LogWarning("[TakeoffMeasurementPanel] ⚠️ SignalR Hub reconnecting...");
                    return Task.CompletedTask;
                };

                _hubConnection.Closed += async (error) =>
                {
                    if (error != null)
                    {
                        Logger.LogError(error, "[TakeoffMeasurementPanel] ❌ SignalR Hub connection closed with error");
                    }
                    else
                    {
                        Logger.LogInformation("[TakeoffMeasurementPanel] SignalR Hub connection closed");
                    }
                };

                // Start the connection
                await _hubConnection.StartAsync();
                Logger.LogInformation("[TakeoffMeasurementPanel] ✓ SignalR Hub connected with ConnectionId={ConnectionId}", _hubConnection.ConnectionId);

                // Subscribe to the drawing-specific group
                if (PackageDrawingId > 0)
                {
                    await _hubConnection.InvokeAsync("SubscribeToDrawing", PackageDrawingId);
                    Logger.LogInformation("[TakeoffMeasurementPanel] ✓ Subscribed to Drawing_{DrawingId} group", PackageDrawingId);
                }
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "[TakeoffMeasurementPanel] ❌❌❌ FAILED TO INITIALIZE SIGNALR HUB CONNECTION ❌❌❌");
                Logger.LogError(ex, "[TakeoffMeasurementPanel] Exception Type: {ExceptionType}", ex.GetType().Name);
                Logger.LogError(ex, "[TakeoffMeasurementPanel] Exception Message: {Message}", ex.Message);
                Logger.LogError(ex, "[TakeoffMeasurementPanel] Stack Trace: {StackTrace}", ex.StackTrace);
            }
        }

        protected override async Task OnParametersSetAsync()
        {
            if (PackageDrawingId > 0)
            {
                await LoadMeasurements();
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
        /// Called from JavaScript when a measurement is completed
        /// </summary>
        public async Task ShowMeasurementResult(MeasurementCalculationResult result)
        {
            CurrentMeasurement = result;
            Logger.LogInformation("[TakeoffMeasurementPanel] ============================================");
            Logger.LogInformation("[TakeoffMeasurementPanel] MEASUREMENT RESULT RECEIVED:");
            Logger.LogInformation("[TakeoffMeasurementPanel] ItemCode: {ItemCode}", result.ItemCode);
            Logger.LogInformation("[TakeoffMeasurementPanel] Description: {Description}", result.Description);
            Logger.LogInformation("[TakeoffMeasurementPanel] MeasurementValue: {Value} {Unit}", result.MeasurementValue, result.Unit);
            Logger.LogInformation("[TakeoffMeasurementPanel] Quantity: {Quantity} {QuantityUnit}", result.Quantity, result.QuantityUnit);
            Logger.LogInformation("[TakeoffMeasurementPanel] Weight: {Weight} {WeightUnit}", result.Weight, result.WeightUnit);
            Logger.LogInformation("[TakeoffMeasurementPanel] Panel Visible: {Visible}", IsVisible);
            Logger.LogInformation("[TakeoffMeasurementPanel] CurrentMeasurement set: {IsSet}", CurrentMeasurement != null);
            Logger.LogInformation("[TakeoffMeasurementPanel] ============================================");

            // Ensure panel is visible
            if (!IsVisible)
            {
                Logger.LogWarning("[TakeoffMeasurementPanel] Panel is hidden! Making it visible...");
                IsVisible = true;
                await IsVisibleChanged.InvokeAsync(IsVisible);
            }

            StateHasChanged();

            // Automatically save the measurement
            Logger.LogInformation("[TakeoffMeasurementPanel] Auto-saving measurement...");
            await SaveCurrentMeasurement();
        }

        private async Task LoadMeasurements()
        {
            try
            {
                isLoadingMeasurements = true;
                errorMessage = null;
                StateHasChanged();

                Logger.LogInformation("[TakeoffMeasurementPanel] Loading measurements for drawing {DrawingId}", PackageDrawingId);

                measurements = await CatalogueService.GetMeasurementsByDrawingAsync(PackageDrawingId, companyId);
                Logger.LogInformation("[TakeoffMeasurementPanel] Loaded {Count} measurements", measurements.Count);
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "[TakeoffMeasurementPanel] Error loading measurements");
                errorMessage = $"Error loading measurements: {ex.Message}";
            }
            finally
            {
                isLoadingMeasurements = false;
                StateHasChanged();
            }
        }

        private async Task SaveCurrentMeasurement()
        {
            if (CurrentMeasurement == null)
            {
                return;
            }

            try
            {
                Logger.LogInformation("[TakeoffMeasurementPanel] Saving measurement: {ItemCode}", CurrentMeasurement.ItemCode);

                // Get TraceTakeoffId by following the hierarchy:
                // PackageDrawing → Package → TakeoffRevision → TraceTakeoff
                var traceTakeoffId = await CatalogueService.GetTraceTakeoffIdFromPackageDrawingAsync(PackageDrawingId);

                if (traceTakeoffId == null)
                {
                    errorMessage = "Cannot save measurement: This package is not linked to a takeoff";
                    Logger.LogError("[TakeoffMeasurementPanel] PackageDrawing {DrawingId} is not linked to a takeoff", PackageDrawingId);
                    return;
                }

                Logger.LogInformation("[TakeoffMeasurementPanel] Resolved TraceTakeoffId: {TakeoffId}", traceTakeoffId);

                // Create measurement via service
                await CatalogueService.CreateMeasurementAsync(
                    traceTakeoffId: traceTakeoffId.Value,
                    packageDrawingId: PackageDrawingId,
                    catalogueItemId: CurrentMeasurement.CatalogueItemId,
                    measurementType: CurrentMeasurement.MeasurementType,
                    value: CurrentMeasurement.MeasurementValue,
                    unit: CurrentMeasurement.Unit,
                    coordinates: null, // TODO: Get coordinates from Nutrient annotation
                    companyId: companyId,
                    annotationId: CurrentMeasurement.AnnotationId // Link to PDF annotation
                );

                Logger.LogInformation("[TakeoffMeasurementPanel] Measurement saved successfully");

                // Clear current measurement
                CurrentMeasurement = null;

                // Reload measurements list
                await LoadMeasurements();

                // Notify parent component
                await OnMeasurementSaved.InvokeAsync();
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "[TakeoffMeasurementPanel] Error saving measurement");
                errorMessage = $"Error saving measurement: {ex.Message}";
            }
            finally
            {
                StateHasChanged();
            }
        }

        private void CancelCurrentMeasurement()
        {
            Logger.LogInformation("[TakeoffMeasurementPanel] Canceling current measurement");
            CurrentMeasurement = null;
            StateHasChanged();
        }

        private void ToggleCategory(string category)
        {
            expandedCategory = expandedCategory == category ? null : category;
            StateHasChanged();
        }

        private async Task DeleteMeasurement(int measurementId)
        {
            try
            {
                Logger.LogInformation("[TakeoffMeasurementPanel] Deleting measurement {Id}", measurementId);

                // Delete measurement and get list of deleted annotation IDs
                var deletedAnnotationIds = await CatalogueService.DeleteMeasurementAsync(measurementId, companyId);

                Logger.LogInformation("[TakeoffMeasurementPanel] Measurement deleted successfully, {Count} annotation(s) to remove from PDF",
                    deletedAnnotationIds?.Count ?? 0);

                // Remove annotations from PDF viewer
                if (deletedAnnotationIds != null && deletedAnnotationIds.Count > 0)
                {
                    foreach (var annotationId in deletedAnnotationIds)
                    {
                        try
                        {
                            await JSRuntime.InvokeVoidAsync("nutrientViewer.deleteAnnotationById", PdfContainerId, annotationId);
                            Logger.LogInformation("[TakeoffMeasurementPanel] Removed annotation {AnnotationId} from PDF viewer", annotationId);
                        }
                        catch (Exception jsEx)
                        {
                            Logger.LogWarning(jsEx, "[TakeoffMeasurementPanel] Could not remove annotation {AnnotationId} from PDF viewer", annotationId);
                        }
                    }
                }

                await LoadMeasurements();
                await OnMeasurementSaved.InvokeAsync();
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "[TakeoffMeasurementPanel] Error deleting measurement");
                errorMessage = $"Error deleting measurement: {ex.Message}";
            }
            finally
            {
                StateHasChanged();
            }
        }

        public async Task RefreshMeasurementsAsync()
        {
            Logger.LogInformation("[TakeoffMeasurementPanel] RefreshMeasurementsAsync called - reloading measurements");
            await LoadMeasurements();
            StateHasChanged();
            Logger.LogInformation("[TakeoffMeasurementPanel] ✓ UI refreshed after measurements reload");
        }

        private async Task RefreshMeasurements()
        {
            await RefreshMeasurementsAsync();
        }

        private async Task ClearAllMeasurements()
        {
            // TODO: Add confirmation dialog
            Logger.LogWarning("[TakeoffMeasurementPanel] Clear all measurements requested");

            // For now, just log - implement bulk delete if needed
            // Or delete one by one
            foreach (var measurement in measurements.ToList())
            {
                await DeleteMeasurement(measurement.Id);
            }
        }

        private async Task TogglePanel()
        {
            IsVisible = !IsVisible;
            await IsVisibleChanged.InvokeAsync(IsVisible);
        }

        private void CycleHeight()
        {
            currentHeight = currentHeight switch
            {
                FooterHeight.Default => FooterHeight.Expanded,
                FooterHeight.Expanded => FooterHeight.Full,
                FooterHeight.Full => FooterHeight.Default,
                _ => FooterHeight.Default
            };
            Logger.LogInformation("[TakeoffMeasurementPanel] Height changed to: {Height}", currentHeight);
            StateHasChanged();
        }

        private string GetHeightClass()
        {
            return currentHeight switch
            {
                FooterHeight.Expanded => "height-expanded",
                FooterHeight.Full => "height-full",
                _ => ""
            };
        }

        private string GetExpandIcon()
        {
            return currentHeight switch
            {
                FooterHeight.Default => "fa-expand-alt",
                FooterHeight.Expanded => "fa-expand-arrows-alt",
                FooterHeight.Full => "fa-compress-alt",
                _ => "fa-expand-alt"
            };
        }

        public async ValueTask DisposeAsync()
        {
            try
            {
                if (_hubConnection != null)
                {
                    // Unsubscribe from drawing group before disconnecting
                    if (PackageDrawingId > 0 && _hubConnection.State == HubConnectionState.Connected)
                    {
                        await _hubConnection.InvokeAsync("UnsubscribeFromDrawing", PackageDrawingId);
                        Logger.LogInformation("[TakeoffMeasurementPanel] ✓ Unsubscribed from Drawing_{DrawingId}", PackageDrawingId);
                    }

                    // Stop and dispose the connection
                    await _hubConnection.StopAsync();
                    await _hubConnection.DisposeAsync();
                    Logger.LogInformation("[TakeoffMeasurementPanel] ✓ SignalR Hub connection disposed");
                }
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "[TakeoffMeasurementPanel] ❌ Error disposing SignalR Hub connection");
            }
        }
    }
}
