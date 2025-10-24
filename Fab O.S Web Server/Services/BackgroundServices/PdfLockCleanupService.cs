using FabOS.WebServer.Services.Interfaces;

namespace FabOS.WebServer.Services.BackgroundServices
{
    /// <summary>
    /// Background service that periodically cleans up stale PDF edit locks.
    /// Runs every 30 seconds to release locks from disconnected sessions or inactive users.
    /// </summary>
    public class PdfLockCleanupService : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<PdfLockCleanupService> _logger;
        private const int CLEANUP_INTERVAL_SECONDS = 30;

        public PdfLockCleanupService(
            IServiceProvider serviceProvider,
            ILogger<PdfLockCleanupService> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("[PdfLockCleanupService] ✓ Started - will run cleanup every {Interval} seconds",
                CLEANUP_INTERVAL_SECONDS);

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    // Create a scope to get scoped services (DbContext is scoped)
                    using (var scope = _serviceProvider.CreateScope())
                    {
                        var lockService = scope.ServiceProvider.GetRequiredService<IPdfLockService>();

                        _logger.LogDebug("[PdfLockCleanupService] Running stale lock cleanup...");
                        await lockService.ReleaseStaleLocksAsync();
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "[PdfLockCleanupService] Error during cleanup cycle");
                }

                // Wait for next cleanup cycle
                await Task.Delay(TimeSpan.FromSeconds(CLEANUP_INTERVAL_SECONDS), stoppingToken);
            }

            _logger.LogInformation("[PdfLockCleanupService] Stopped");
        }
    }
}
