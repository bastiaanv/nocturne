using Nocturne.Infrastructure.Data.Abstractions;

namespace Nocturne.API.Services.BackgroundServices;

public class NotificationCleanupService(
    IServiceProvider serviceProvider,
    ILogger<NotificationCleanupService> logger) : BackgroundService
{
    private static readonly TimeSpan Interval = TimeSpan.FromHours(24);
    private static readonly TimeSpan RetentionPeriod = TimeSpan.FromDays(30);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = serviceProvider.CreateScope();
                var repository = scope.ServiceProvider
                    .GetRequiredService<IInAppNotificationRepository>();

                var cutoff = DateTime.UtcNow - RetentionPeriod;
                var deleted = await repository.DeleteArchivedBeforeAsync(cutoff, stoppingToken);

                if (deleted > 0)
                {
                    logger.LogInformation(
                        "Cleaned up {Count} archived notifications older than {Cutoff}",
                        deleted, cutoff);
                }
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                logger.LogError(ex, "Error during notification cleanup");
            }

            await Task.Delay(Interval, stoppingToken);
        }
    }
}
