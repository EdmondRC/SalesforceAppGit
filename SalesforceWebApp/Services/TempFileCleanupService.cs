using Microsoft.Extensions.Hosting;

namespace SalesforceWebApp.Services
{
    /// <summary>
    /// Periodically deletes orphaned temp files left behind in the app's dedicated upload temp
    /// folder - e.g. from abandoned sessions, browser crashes, or unhandled errors - that were
    /// never cleaned up by the normal request-driven cleanup in SalesforceAccountMatchingModel.
    /// </summary>
    public class TempFileCleanupService : BackgroundService
    {
        private static readonly string UploadFolder =
            Path.Combine(Path.GetTempPath(), "SalesforceAccountMatching");

        private static readonly TimeSpan SweepInterval = TimeSpan.FromHours(1);
        private static readonly TimeSpan MaxFileAge = TimeSpan.FromHours(4);

        private readonly ILogger<TempFileCleanupService> _logger;

        public TempFileCleanupService(ILogger<TempFileCleanupService> logger)
        {
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                CleanupOldFiles();

                try
                {
                    await Task.Delay(SweepInterval, stoppingToken);
                }
                catch (TaskCanceledException)
                {
                    // Expected when the host is shutting down.
                }
            }
        }

        private void CleanupOldFiles()
        {
            if (!Directory.Exists(UploadFolder))
            {
                return;
            }

            foreach (var filePath in Directory.EnumerateFiles(UploadFolder))
            {
                try
                {
                    var lastWriteTime = File.GetLastWriteTimeUtc(filePath);
                    if (DateTime.UtcNow - lastWriteTime > MaxFileAge)
                    {
                        File.Delete(filePath);
                        _logger.LogInformation("Deleted orphaned temp file: {FilePath}", filePath);
                    }
                }
                catch (Exception ex)
                {
                    // Best-effort cleanup - a locked or already-deleted file should not crash the sweep.
                    _logger.LogWarning(ex, "Failed to delete temp file during cleanup: {FilePath}", filePath);
                }
            }
        }
    }
}