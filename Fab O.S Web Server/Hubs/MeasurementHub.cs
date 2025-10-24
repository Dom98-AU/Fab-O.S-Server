using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace FabOS.WebServer.Hubs
{
    /// <summary>
    /// SignalR Hub for broadcasting real-time measurement updates across all connected clients
    /// This enables multi-tab and multi-user synchronization when measurements are created/deleted
    ///
    /// KEY DIFFERENCE from Blazor Circuit:
    /// - Separate dedicated connection (not tied to Blazor UI lifecycle)
    /// - Automatic reconnection built-in
    /// - One-way broadcasts (Server → Client) - no timeout issues
    /// - YOU control connection behavior
    ///
    /// IMPORTANT: AllowAnonymous is required because the C# HubConnection client runs server-side
    /// and doesn't have access to the browser's authentication cookie
    /// </summary>
    [AllowAnonymous]
    public class MeasurementHub : Hub
    {
        private readonly ILogger<MeasurementHub> _logger;

        public MeasurementHub(ILogger<MeasurementHub> logger)
        {
            _logger = logger;
        }

        /// <summary>
        /// Called when a client connects to the hub
        /// </summary>
        public override async Task OnConnectedAsync()
        {
            _logger.LogInformation("[MeasurementHub] ✓ Client connected: {ConnectionId}", Context.ConnectionId);
            await base.OnConnectedAsync();
        }

        /// <summary>
        /// Called when a client disconnects from the hub
        /// </summary>
        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            if (exception != null)
            {
                _logger.LogWarning(exception, "[MeasurementHub] Client disconnected with error: {ConnectionId}", Context.ConnectionId);
            }
            else
            {
                _logger.LogInformation("[MeasurementHub] Client disconnected: {ConnectionId}", Context.ConnectionId);
            }
            await base.OnDisconnectedAsync(exception);
        }

        /// <summary>
        /// Client subscribes to updates for a specific drawing
        /// This allows filtering so clients only receive updates relevant to their current drawing
        /// Called from client: await connection.invoke("SubscribeToDrawing", drawingId);
        /// </summary>
        public async Task SubscribeToDrawing(int packageDrawingId)
        {
            var groupName = $"Drawing_{packageDrawingId}";
            await Groups.AddToGroupAsync(Context.ConnectionId, groupName);
            _logger.LogInformation("[MeasurementHub] ✓ Client {ConnectionId} subscribed to {GroupName}",
                Context.ConnectionId, groupName);
        }

        /// <summary>
        /// Client unsubscribes from a specific drawing
        /// Called when user navigates away or closes the drawing
        /// </summary>
        public async Task UnsubscribeFromDrawing(int packageDrawingId)
        {
            var groupName = $"Drawing_{packageDrawingId}";
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, groupName);
            _logger.LogInformation("[MeasurementHub] Client {ConnectionId} unsubscribed from {GroupName}",
                Context.ConnectionId, groupName);
        }
    }

    /// <summary>
    /// Service for broadcasting measurement events to all connected clients
    /// This replaces the static event system for better multi-user/multi-tab support
    /// </summary>
    public interface IMeasurementHubService
    {
        Task NotifyMeasurementCreatedAsync(int packageDrawingId, int measurementId);
        Task NotifyMeasurementDeletedAsync(int packageDrawingId, int measurementId, string? annotationId = null);
        Task NotifyMeasurementUpdatedAsync(int packageDrawingId, int measurementId);
        Task NotifyInstantJsonUpdatedAsync(int packageDrawingId);

        // PDF Lock notifications
        Task NotifyLockAcquiredAsync(int packageDrawingId, string userName, string sessionId);
        Task NotifyLockReleasedAsync(int packageDrawingId, string sessionId);
        Task NotifyLockStolenAsync(int packageDrawingId, string newUserName, string oldSessionId);

        // Click-to-highlight feature
        Task NotifyAnnotationSelectedAsync(int packageDrawingId, string annotationId);
    }

    /// <summary>
    /// Implementation of IMeasurementHubService
    /// Injects IHubContext to send messages to all connected clients
    /// </summary>
    public class MeasurementHubService : IMeasurementHubService
    {
        private readonly IHubContext<MeasurementHub> _hubContext;
        private readonly ILogger<MeasurementHubService> _logger;

        public MeasurementHubService(
            IHubContext<MeasurementHub> hubContext,
            ILogger<MeasurementHubService> logger)
        {
            _hubContext = hubContext;
            _logger = logger;
        }

        public async Task NotifyMeasurementCreatedAsync(int packageDrawingId, int measurementId)
        {
            var groupName = $"Drawing_{packageDrawingId}";
            _logger.LogInformation("[MeasurementHubService] 📢 Broadcasting MeasurementCreated to {GroupName}: MeasurementId={MeasurementId}",
                groupName, measurementId);

            await _hubContext.Clients.Group(groupName).SendAsync("MeasurementCreated", new
            {
                PackageDrawingId = packageDrawingId,
                MeasurementId = measurementId
            });
        }

        public async Task NotifyMeasurementDeletedAsync(int packageDrawingId, int measurementId, string? annotationId = null)
        {
            var groupName = $"Drawing_{packageDrawingId}";
            _logger.LogInformation("[MeasurementHubService] 📢 Broadcasting MeasurementDeleted to {GroupName}: MeasurementId={MeasurementId}, AnnotationId={AnnotationId}",
                groupName, measurementId, annotationId ?? "null");

            await _hubContext.Clients.Group(groupName).SendAsync("MeasurementDeleted", new
            {
                PackageDrawingId = packageDrawingId,
                MeasurementId = measurementId,
                AnnotationId = annotationId
            });
        }

        public async Task NotifyMeasurementUpdatedAsync(int packageDrawingId, int measurementId)
        {
            var groupName = $"Drawing_{packageDrawingId}";
            _logger.LogInformation("[MeasurementHubService] 📢 Broadcasting MeasurementUpdated to {GroupName}: MeasurementId={MeasurementId}",
                groupName, measurementId);

            await _hubContext.Clients.Group(groupName).SendAsync("MeasurementUpdated", new
            {
                PackageDrawingId = packageDrawingId,
                MeasurementId = measurementId
            });
        }

        public async Task NotifyInstantJsonUpdatedAsync(int packageDrawingId)
        {
            var groupName = $"Drawing_{packageDrawingId}";
            _logger.LogInformation("[MeasurementHubService] 📢 Broadcasting InstantJsonUpdated to {GroupName}", groupName);

            await _hubContext.Clients.Group(groupName).SendAsync("InstantJsonUpdated", new
            {
                PackageDrawingId = packageDrawingId
            });
        }

        public async Task NotifyLockAcquiredAsync(int packageDrawingId, string userName, string sessionId)
        {
            var groupName = $"Drawing_{packageDrawingId}";
            _logger.LogInformation("[MeasurementHubService] 🔒 Broadcasting LockAcquired to {GroupName}: User={UserName}, SessionId={SessionId}",
                groupName, userName, sessionId);

            await _hubContext.Clients.Group(groupName).SendAsync("LockAcquired", new
            {
                PackageDrawingId = packageDrawingId,
                UserName = userName,
                SessionId = sessionId
            });
        }

        public async Task NotifyLockReleasedAsync(int packageDrawingId, string sessionId)
        {
            var groupName = $"Drawing_{packageDrawingId}";
            _logger.LogInformation("[MeasurementHubService] 🔓 Broadcasting LockReleased to {GroupName}: SessionId={SessionId}",
                groupName, sessionId);

            await _hubContext.Clients.Group(groupName).SendAsync("LockReleased", new
            {
                PackageDrawingId = packageDrawingId,
                SessionId = sessionId
            });
        }

        public async Task NotifyLockStolenAsync(int packageDrawingId, string newUserName, string oldSessionId)
        {
            var groupName = $"Drawing_{packageDrawingId}";
            _logger.LogWarning("[MeasurementHubService] ⚠️ Broadcasting LockStolen to {GroupName}: NewUser={NewUserName}, OldSessionId={OldSessionId}",
                groupName, newUserName, oldSessionId);

            await _hubContext.Clients.Group(groupName).SendAsync("LockStolen", new
            {
                PackageDrawingId = packageDrawingId,
                NewUserName = newUserName,
                OldSessionId = oldSessionId
            });
        }

        public async Task NotifyAnnotationSelectedAsync(int packageDrawingId, string annotationId)
        {
            var groupName = $"Drawing_{packageDrawingId}";
            _logger.LogInformation("[MeasurementHubService] 🖱️ Broadcasting AnnotationSelected to {GroupName}: AnnotationId={AnnotationId}",
                groupName, annotationId);

            await _hubContext.Clients.Group(groupName).SendAsync("AnnotationSelected", new
            {
                PackageDrawingId = packageDrawingId,
                AnnotationId = annotationId
            });
        }
    }
}
