using Microsoft.EntityFrameworkCore;
using Portal.Application.Interfaces;

namespace Portal.Api.Workers;

public class FinanceCacheCleanup(
    IServiceScopeFactory scopeFactory,
    ILogger<FinanceCacheCleanup> logger,
    IConfiguration configuration) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!configuration.GetValue("Workers:FinanceCacheCleanup:Enabled", true))
        {
            logger.LogInformation("FinanceCacheCleanup is disabled");
            return;
        }

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = scopeFactory.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<IPortalDbContext>();

                var expired = await db.FinanceSnapshots
                    .Where(f => f.ExpiresAt <= DateTime.UtcNow)
                    .ToListAsync(stoppingToken);

                if (expired.Count > 0)
                {
                    db.FinanceSnapshots.RemoveRange(expired);
                    await db.SaveChangesAsync(stoppingToken);
                    logger.LogInformation("Cleaned up {Count} expired finance snapshots", expired.Count);
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "FinanceCacheCleanup encountered an error");
            }

            await Task.Delay(TimeSpan.FromHours(1), stoppingToken);
        }
    }
}
