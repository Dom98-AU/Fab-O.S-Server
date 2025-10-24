/**
 * SignalR Hub Client for Real-Time Measurement Updates
 *
 * This module handles the SignalR connection to the MeasurementHub for cross-tab/cross-user synchronization.
 * Unlike Blazor's automatic circuit, this is a separate, reliable connection with automatic reconnection.
 *
 * Usage:
 * - Blazor components call subscribeToDrawing(drawingId, dotNetRef) to start receiving updates
 * - When measurement is deleted on ANY tab/user, ALL subscribers receive the event
 * - Automatic reconnection with exponential backoff ensures reliability
 */

let connection = null;
let subscribedDrawings = new Map(); // Map of drawingId -> Set of dotNetRefs

/**
 * Initialize the SignalR connection (only called once)
 */
async function initializeConnection() {
    if (connection) {
        console.log('[MeasurementHub] Connection already initialized');
        return connection;
    }

    console.log('[MeasurementHub] Initializing SignalR connection...');

    connection = new signalR.HubConnectionBuilder()
        .withUrl("/measurementHub")
        .withAutomaticReconnect([0, 1000, 5000, 10000, 30000]) // Exponential backoff: 0ms, 1s, 5s, 10s, 30s, then 30s repeatedly
        .configureLogging(signalR.LogLevel.Information)
        .build();

    // Set up event listeners for broadcasts from server
    connection.on("MeasurementCreated", (data) => {
        console.log('[MeasurementHub] ✓ Received MeasurementCreated:', data);
        // Handle both PascalCase (C#) and camelCase (JSON) property names
        const packageDrawingId = data.PackageDrawingId || data.packageDrawingId;
        notifyBlazorComponents(packageDrawingId, 'OnMeasurementCreated', data);
    });

    connection.on("MeasurementDeleted", (data) => {
        console.log('[MeasurementHub] ✓ Received MeasurementDeleted:', data);
        // Handle both PascalCase (C#) and camelCase (JSON) property names
        const packageDrawingId = data.PackageDrawingId || data.packageDrawingId;
        notifyBlazorComponents(packageDrawingId, 'OnMeasurementDeleted', data);
    });

    connection.on("MeasurementUpdated", (data) => {
        console.log('[MeasurementHub] ✓ Received MeasurementUpdated:', data);
        // Handle both PascalCase (C#) and camelCase (JSON) property names
        const packageDrawingId = data.PackageDrawingId || data.packageDrawingId;
        notifyBlazorComponents(packageDrawingId, 'OnMeasurementUpdated', data);
    });

    // Connection lifecycle events
    connection.onreconnecting((error) => {
        console.warn('[MeasurementHub] ⚠️ Connection lost, reconnecting...', error);
    });

    connection.onreconnected((connectionId) => {
        console.log('[MeasurementHub] ✅ Reconnected! Connection ID:', connectionId);
        // Re-subscribe to all drawings
        resubscribeAll();
    });

    connection.onclose((error) => {
        console.error('[MeasurementHub] ❌ Connection closed:', error);
        // Connection will automatically try to reconnect due to withAutomaticReconnect()
    });

    // Start the connection
    try {
        await connection.start();
        console.log('[MeasurementHub] ✅ Connected successfully! Connection ID:', connection.connectionId);
    } catch (error) {
        console.error('[MeasurementHub] ❌ Failed to connect:', error);
        // withAutomaticReconnect() will retry automatically
        throw error;
    }

    return connection;
}

/**
 * Subscribe a Blazor component to measurement updates for a specific drawing
 * @param {number} packageDrawingId - The drawing ID to subscribe to
 * @param {object} dotNetRef - The Blazor component reference for callbacks
 */
async function subscribeToDrawing(packageDrawingId, dotNetRef) {
    console.log('[MeasurementHub] Subscribing to drawing:', packageDrawingId);

    try {
        // Initialize connection if not already done
        await initializeConnection();

        // Track this subscription
        if (!subscribedDrawings.has(packageDrawingId)) {
            subscribedDrawings.set(packageDrawingId, new Set());

            // Tell the server we want to join this drawing's group
            await connection.invoke("SubscribeToDrawing", packageDrawingId);
            console.log('[MeasurementHub] ✓ Subscribed to Drawing_' + packageDrawingId);
        }

        // Add this component to the list of subscribers
        subscribedDrawings.get(packageDrawingId).add(dotNetRef);
        console.log('[MeasurementHub] ✓ Component registered for drawing ' + packageDrawingId);

    } catch (error) {
        console.error('[MeasurementHub] ❌ Error subscribing to drawing:', error);
        throw error;
    }
}

/**
 * Unsubscribe a Blazor component from measurement updates
 * @param {number} packageDrawingId - The drawing ID to unsubscribe from
 * @param {object} dotNetRef - The Blazor component reference to remove
 */
async function unsubscribeFromDrawing(packageDrawingId, dotNetRef) {
    console.log('[MeasurementHub] Unsubscribing from drawing:', packageDrawingId);

    if (!subscribedDrawings.has(packageDrawingId)) {
        console.warn('[MeasurementHub] Not subscribed to drawing:', packageDrawingId);
        return;
    }

    // Remove this component from subscribers
    const subscribers = subscribedDrawings.get(packageDrawingId);
    subscribers.delete(dotNetRef);

    // If no more subscribers for this drawing, unsubscribe from server
    if (subscribers.size === 0) {
        subscribedDrawings.delete(packageDrawingId);

        try {
            if (connection) {
                await connection.invoke("UnsubscribeFromDrawing", packageDrawingId);
                console.log('[MeasurementHub] ✓ Unsubscribed from Drawing_' + packageDrawingId);
            }
        } catch (error) {
            console.error('[MeasurementHub] ❌ Error unsubscribing from drawing:', error);
        }
    }
}

/**
 * Notify all Blazor components subscribed to a drawing
 * @param {number} packageDrawingId - The drawing ID
 * @param {string} methodName - The Blazor method to invoke
 * @param {object} data - The data to pass to the method
 */
function notifyBlazorComponents(packageDrawingId, methodName, data) {
    const subscribers = subscribedDrawings.get(packageDrawingId);
    if (!subscribers || subscribers.size === 0) {
        console.log('[MeasurementHub] No subscribers for drawing:', packageDrawingId);
        return;
    }

    console.log(`[MeasurementHub] Notifying ${subscribers.size} component(s) for drawing ${packageDrawingId}`);
    console.log(`[MeasurementHub] Method: ${methodName}, Data:`, data);

    subscribers.forEach(dotNetRef => {
        console.log('[MeasurementHub] Calling dotNetRef.invokeMethodAsync:', methodName);
        try {
            dotNetRef.invokeMethodAsync(methodName, data)
                .then(() => {
                    console.log('[MeasurementHub] ✓ Successfully invoked', methodName);
                })
                .catch(error => {
                    console.error('[MeasurementHub] ❌ Error invoking Blazor method:', methodName, error);
                    console.error('[MeasurementHub] Error details:', error.message, error.stack);
                });
        } catch (error) {
            console.error('[MeasurementHub] ❌ Synchronous error notifying component:', error);
            console.error('[MeasurementHub] Error details:', error.message, error.stack);
        }
    });
}

/**
 * Re-subscribe to all drawings after reconnection
 */
async function resubscribeAll() {
    console.log('[MeasurementHub] Re-subscribing to all drawings after reconnection...');

    for (const [packageDrawingId, subscribers] of subscribedDrawings.entries()) {
        if (subscribers.size > 0) {
            try {
                await connection.invoke("SubscribeToDrawing", packageDrawingId);
                console.log('[MeasurementHub] ✓ Re-subscribed to Drawing_' + packageDrawingId);

                // Trigger refresh for all components to catch any missed updates
                notifyBlazorComponents(packageDrawingId, 'OnReconnected', { PackageDrawingId: packageDrawingId });
            } catch (error) {
                console.error('[MeasurementHub] ❌ Error re-subscribing to drawing:', packageDrawingId, error);
            }
        }
    }
}

/**
 * Get connection state for debugging
 */
function getConnectionState() {
    if (!connection) {
        return 'Not initialized';
    }

    const stateNames = ['Disconnected', 'Connecting', 'Connected', 'Disconnecting', 'Reconnecting'];
    return stateNames[connection.state] || 'Unknown';
}

// Export functions for Blazor JS Interop
window.MeasurementHubClient = {
    subscribeToDrawing,
    unsubscribeFromDrawing,
    getConnectionState
};

console.log('[MeasurementHub] Client module loaded');
