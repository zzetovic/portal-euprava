using Microsoft.EntityFrameworkCore;
using Portal.Application.Interfaces;

namespace Portal.Api.Workers;

public class RefreshTokenCleanup(
    IServiceScopeFactory scopeFactory,
    ILogger<RefreshTokenCleanup> logger,
    IConfiguration configuration) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await Task.Yield(); // Ensure host startup is not blocked

        if (!configuration.GetValue("Workers:RefreshTokenCleanup:Enabled", true))
        {
            logger.LogInformation("RefreshTokenCleanup is disabled");
            return;
        }

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = scopeFactory.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<IPortalDbContext>();

                var expired = await db.RefreshTokens
                    .Where(t => t.ExpiresAt <= DateTime.UtcNow || t.RevokedAt != null)
                    .Where(t => t.ExpiresAt <= DateTime.UtcNow.AddDays(-7)) // Keep revoked for 7 days for audit
                    .ToListAsync(stoppingToken);

                if (expired.Count > 0)
                {
                    db.RefreshTokens.RemoveRange(expired);
                    await db.SaveChangesAsync(stoppingToken);
                    logger.LogInformation("Cleaned up {Count} expired refresh tokens", expired.Count);
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "RefreshTokenCleanup encountered an error");
            }

            await Task.Delay(TimeSpan.FromHours(24), stoppingToken);
        }
    }
}
