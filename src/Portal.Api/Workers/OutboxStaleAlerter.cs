using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Portal.Application.Interfaces;
using Portal.Domain.Entities;
using Portal.Domain.Enums;

namespace Portal.Api.Workers;

public class OutboxStaleAlerter(
    IServiceScopeFactory scopeFactory,
    ILogger<OutboxStaleAlerter> logger,
    IConfiguration configuration) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await Task.Yield(); // Ensure host startup is not blocked

        if (!configuration.GetValue("Workers:OutboxStaleAlerter:Enabled", true))
        {
            logger.LogInformation("OutboxStaleAlerter is disabled");
            return;
        }

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await CheckDeadLetters(stoppingToken);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "OutboxStaleAlerter encountered an error");
            }

            await Task.Delay(TimeSpan.FromHours(24), stoppingToken);
        }
    }

    private async Task CheckDeadLetters(CancellationToken ct)
    {
        using var scope = scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<IPortalDbContext>();

        var deadLetters = await db.IntegrationOutbox
            .Where(o => o.Status == OutboxStatus.DeadLetter)
            .GroupBy(o => o.TenantId)
            .Select(g => new { TenantId = g.Key, Count = g.Count() })
            .ToListAsync(ct);

        foreach (var tenant in deadLetters)
        {
            var admins = await db.Users
                .Where(u => u.TenantId == tenant.TenantId && u.UserType == UserType.JlsAdmin && u.IsActive)
                .Select(u => u.Id)
                .ToListAsync(ct);

            foreach (var adminId in admins)
            {
                db.Notifications.Add(new Notification
                {
                    Id = Guid.NewGuid(),
                    TenantId = tenant.TenantId,
                    UserId = adminId,
                    Type = "outbox_stale_alert",
                    TitleI18n = JsonSerializer.Serialize(new Dictionary<string, string>
                    {
                        ["hr"] = "Neriješeni zahtjevi u SEUP-u",
                        ["en"] = "Unresolved SEUP requests"
                    }),
                    BodyI18n = JsonSerializer.Serialize(new Dictionary<string, string>
                    {
                        ["hr"] = $"Imate {tenant.Count} zahtjeva koji nisu uspjeli biti zaprimljeni u SEUP. Provjerite ih.",
                        ["en"] = $"You have {tenant.Count} requests that failed to be registered in SEUP. Please review."
                    })
                });
            }

            logger.LogWarning("Tenant {TenantId} has {Count} dead-letter outbox entries", tenant.TenantId, tenant.Count);
        }

        if (deadLetters.Count > 0)
            await db.SaveChangesAsync(ct);
    }
}
