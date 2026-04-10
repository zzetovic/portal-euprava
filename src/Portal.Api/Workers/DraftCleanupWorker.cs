using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Portal.Application.Interfaces;
using Portal.Domain.Entities;
using Portal.Domain.Enums;

namespace Portal.Api.Workers;

public class DraftCleanupWorker(
    IServiceScopeFactory scopeFactory,
    ILogger<DraftCleanupWorker> logger,
    IConfiguration configuration) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await Task.Yield(); // Ensure host startup is not blocked

        if (!configuration.GetValue("Workers:DraftCleanupWorker:Enabled", true))
        {
            logger.LogInformation("DraftCleanupWorker is disabled");
            return;
        }

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await CleanupExpiredDrafts(stoppingToken);
                await WarnExpiringDrafts(stoppingToken);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "DraftCleanupWorker encountered an error");
            }

            await Task.Delay(TimeSpan.FromHours(24), stoppingToken);
        }
    }

    private async Task CleanupExpiredDrafts(CancellationToken ct)
    {
        using var scope = scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<IPortalDbContext>();
        var attachmentStorage = scope.ServiceProvider.GetRequiredService<IAttachmentStorage>();

        var expiredDrafts = await db.Requests
            .Include(r => r.Attachments)
            .Include(r => r.StatusHistory)
            .Where(r => r.Status == RequestStatus.Draft && r.ExpiresAt != null && r.ExpiresAt <= DateTime.UtcNow)
            .ToListAsync(ct);

        foreach (var draft in expiredDrafts)
        {
            var storageKeys = draft.Attachments.Select(a => a.StorageKey).ToList();

            db.RequestStatusHistories.RemoveRange(draft.StatusHistory);
            db.RequestAttachments.RemoveRange(draft.Attachments);
            db.Requests.Remove(draft);

            // Background delete from storage
            foreach (var key in storageKeys)
            {
                try { await attachmentStorage.DeleteAsync(key, ct); }
                catch (Exception ex) { logger.LogWarning(ex, "Failed to delete attachment {Key}", key); }
            }
        }

        if (expiredDrafts.Count > 0)
        {
            await db.SaveChangesAsync(ct);
            logger.LogInformation("Deleted {Count} expired drafts", expiredDrafts.Count);
        }
    }

    private async Task WarnExpiringDrafts(CancellationToken ct)
    {
        using var scope = scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<IPortalDbContext>();

        var warningDate = DateTime.UtcNow.AddDays(7);
        var expiringDrafts = await db.Requests
            .Where(r => r.Status == RequestStatus.Draft
                && r.ExpiresAt != null
                && r.ExpiresAt <= warningDate
                && r.ExpiresAt > DateTime.UtcNow)
            .ToListAsync(ct);

        foreach (var draft in expiringDrafts)
        {
            // Check if we already sent a warning for this draft
            var alreadyWarned = await db.Notifications.AnyAsync(
                n => n.RelatedRequestId == draft.Id && n.Type == "draft_expiring_warning", ct);

            if (!alreadyWarned)
            {
                db.Notifications.Add(new Notification
                {
                    Id = Guid.NewGuid(),
                    TenantId = draft.TenantId,
                    UserId = draft.CitizenId,
                    Type = "draft_expiring_warning",
                    TitleI18n = JsonSerializer.Serialize(new Dictionary<string, string>
                    {
                        ["hr"] = "Nacrt zahtjeva uskoro istječe",
                        ["en"] = "Draft request expiring soon"
                    }),
                    BodyI18n = JsonSerializer.Serialize(new Dictionary<string, string>
                    {
                        ["hr"] = $"Vaš nacrt zahtjeva {draft.ReferenceNumber} istječe za 7 dana. Dovršite ga ili će biti automatski obrisan.",
                        ["en"] = $"Your draft request {draft.ReferenceNumber} expires in 7 days. Complete it or it will be automatically deleted."
                    }),
                    RelatedRequestId = draft.Id
                });
            }
        }

        if (expiringDrafts.Count > 0)
            await db.SaveChangesAsync(ct);
    }
}
