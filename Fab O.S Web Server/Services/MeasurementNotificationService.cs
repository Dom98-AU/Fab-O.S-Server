namespace FabOS.WebServer.Services
{
    /// <summary>
    /// Service for notifying UI components when measurements are updated/deleted
    /// Uses static events to communicate across different request contexts
    /// </summary>
    public class MeasurementNotificationService
    {
        /// <summary>
        /// Event triggered when a measurement is deleted
        /// Parameters: (int packageDrawingId, int? measurementId)
        /// </summary>
        public static event Action<int, int?>? MeasurementDeleted;

        /// <summary>
        /// Notify all subscribers that a measurement was deleted
        /// </summary>
        public static void NotifyMeasurementDeleted(int packageDrawingId, int? measurementId = null)
        {
            MeasurementDeleted?.Invoke(packageDrawingId, measurementId);
        }
    }
}
