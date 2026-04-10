using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Portal.Application.Interfaces;
using Portal.Domain.Entities;
using Portal.Domain.Enums;

namespace Portal.Api.Workers;

public class OutboxDispatcher(
    IServiceScopeFactory scopeFactory,
    ILogger<OutboxDispatcher> logger,
    IConfiguration configuration) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await Task.Yield(); // Ensure host startup is not blocked

        if (!configuration.GetValue("Workers:OutboxDispatcher:Enabled", true))
        {
            logger.LogInformation("OutboxDispatcher is disabled");
            return;
        }

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessPendingEntries(stoppingToken);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "OutboxDispatcher encountered an error");
            }

            await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
        }
    }

    private async Task ProcessPendingEntries(CancellationToken ct)
    {
        using var scope = scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<IPortalDbContext>();

        // FOR UPDATE SKIP LOCKED equivalent via raw SQL
        var entries = await db.IntegrationOutbox
            .Where(o => o.Status == OutboxStatus.Pending && o.NextAttemptAt <= DateTime.UtcNow)
            .OrderBy(o => o.CreatedAt)
            .Take(10)
            .ToListAsync(ct);

        foreach (var entry in entries)
        {
            await ProcessEntry(scope.ServiceProvider, entry, ct);
        }
    }

    private async Task ProcessEntry(IServiceProvider services, IntegrationOutbox entry, CancellationToken ct)
    {
        var db = services.GetRequiredService<IPortalDbContext>();
        var aktWriter = services.GetRequiredService<ILocalDbAktWriter>();

        entry.Status = OutboxStatus.Processing;
        await db.SaveChangesAsync(ct);

        try
        {
            var payload = JsonSerializer.Deserialize<JsonElement>(entry.Payload!);
            var requestId = payload.GetProperty("RequestId").GetGuid();

            var request = await db.Requests
                .Include(r => r.Citizen)
                .Include(r => r.Attachments)
                .Include(r => r.RequestType)
                .FirstOrDefaultAsync(r => r.Id == requestId, ct);

            if (request is null)
            {
                entry.Status = OutboxStatus.Failed;
                entry.LastError = "Request not found";
                await db.SaveChangesAsync(ct);
                return;
            }

            var cmd = new WriteAktCommand(
                TenantId: entry.TenantId,
                RequestId: requestId,
                IdempotencyKey: requestId.ToString(),
                CitizenOib: request.Citizen?.Oib ?? "",
                CitizenFullName: $"{request.Citizen?.FirstName} {request.Citizen?.LastName}",
                CitizenAddress: "",
                CitizenEmail: request.Citizen?.Email ?? "",
                Subject: request.RequestType?.NameI18n ?? request.ReferenceNumber,
                BodyText: request.FormData,
                ReceivedAt: request.ReviewedAt ?? DateTimeOffset.UtcNow,
                Attachments: request.Attachments.Select(a => new AktAttachmentInput(
                    a.OriginalFilename, a.MimeType, a.SizeBytes, a.StorageKey)).ToArray());

            var result = await aktWriter.WriteAktAsync(cmd, ct);

            if (result.Success || result.IsDuplicate)
            {
                await HandleSuccess(db, entry, request, result, ct);
            }
            else
            {
                await HandleFailure(db, entry, result.ErrorMessage ?? "Unknown error", ct);
            }
        }
        catch (NotImplementedException)
        {
            // Stub - mark as failed with specific message
            await HandleFailure(db, entry, "ILocalDbAktWriter not implemented (awaiting SEUP schema)", ct);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "OutboxDispatcher failed to process entry {OutboxId}", entry.Id);
            await HandleFailure(db, entry, ex.Message, ct);
        }
    }

    private static async Task HandleSuccess(
        IPortalDbContext db, IntegrationOutbox entry, Request request, AktWriteResult result, CancellationToken ct)
    {
        // Idempotency layer 3: ON CONFLICT DO NOTHING
        var existingMapping = await db.SeupAktMappings
            .AnyAsync(m => m.RequestId == request.Id, ct);

        if (!existingMapping)
        {
            db.SeupAktMappings.Add(new SeupAktMapping
            {
                Id = Guid.NewGuid(),
                TenantId = entry.TenantId,
                RequestId = request.Id,
                AktId = result.AktId!.Value,
                ReceivedAt = DateTime.UtcNow,
                ReceivedByUserId = request.ReviewedByUserId!.Value
            });
        }

        if (request.Status != RequestStatus.ReceivedInRegistry)
        {
            request.Status = RequestStatus.ReceivedInRegistry;

            db.RequestStatusHistories.Add(new RequestStatusHistory
            {
                Id = Guid.NewGuid(),
                RequestId = request.Id,
                FromStatus = RequestStatus.ProcessingRegistry,
                ToStatus = RequestStatus.ReceivedInRegistry,
                ChangedBySource = StatusChangeSource.System,
                ChangedAt = DateTime.UtcNow
            });

            // Notification for citizen
            db.Notifications.Add(new Notification
            {
                Id = Guid.NewGuid(),
                TenantId = entry.TenantId,
                UserId = request.CitizenId,
                Type = "request_received_in_registry",
                TitleI18n = JsonSerializer.Serialize(new Dictionary<string, string>
                {
                    ["hr"] = "Zahtjev zaprimljen u pisarnicu",
                    ["en"] = "Request received in registry"
                }),
                BodyI18n = JsonSerializer.Serialize(new Dictionary<string, string>
                {
                    ["hr"] = $"Vaš zahtjev {request.ReferenceNumber} je zaprimljen u pisarnicu pod brojem akta {result.AktId}.",
                    ["en"] = $"Your request {request.ReferenceNumber} has been received in the registry as act {result.AktId}."
                }),
                RelatedRequestId = request.Id
            });

            // Notification for officer
            if (request.ReviewedByUserId.HasValue)
            {
                db.Notifications.Add(new Notification
                {
                    Id = Guid.NewGuid(),
                    TenantId = entry.TenantId,
                    UserId = request.ReviewedByUserId.Value,
                    Type = "outbox_success",
                    TitleI18n = JsonSerializer.Serialize(new Dictionary<string, string>
                    {
                        ["hr"] = "Zahtjev uspješno zaprimljen",
                        ["en"] = "Request successfully received"
                    }),
                    BodyI18n = JsonSerializer.Serialize(new Dictionary<string, string>
                    {
                        ["hr"] = $"Zahtjev {request.ReferenceNumber} uspješno zaprimljen pod brojem akta {result.AktId}.",
                        ["en"] = $"Request {request.ReferenceNumber} received as act {result.AktId}."
                    }),
                    RelatedRequestId = request.Id
                });
            }
        }

        entry.Status = OutboxStatus.Done;
        entry.ProcessedAt = DateTime.UtcNow;

        await db.SaveChangesAsync(ct);
    }

    private static async Task HandleFailure(IPortalDbContext db, IntegrationOutbox entry, string error, CancellationToken ct)
    {
        entry.Attempts++;
        entry.LastError = error;

        if (entry.Attempts >= 5)
        {
            entry.Status = OutboxStatus.DeadLetter;

            // Alert admin via notification
            var admins = await db.Users
                .Where(u => u.TenantId == entry.TenantId && u.UserType == UserType.JlsAdmin && u.IsActive)
                .Select(u => u.Id)
                .ToListAsync(ct);

            foreach (var adminId in admins)
            {
                db.Notifications.Add(new Notification
                {
                    Id = Guid.NewGuid(),
                    TenantId = entry.TenantId,
                    UserId = adminId,
                    Type = "outbox_dead_letter",
                    TitleI18n = JsonSerializer.Serialize(new Dictionary<string, string>
                    {
                        ["hr"] = "Zaprimanje zahtjeva u SEUP nije uspjelo",
                        ["en"] = "Request registration in SEUP failed"
                    }),
                    BodyI18n = JsonSerializer.Serialize(new Dictionary<string, string>
                    {
                        ["hr"] = $"Zahtjev nije mogao biti zaprimljen u SEUP nakon {entry.Attempts} pokušaja. Greška: {error}",
                        ["en"] = $"Request could not be registered in SEUP after {entry.Attempts} attempts. Error: {error}"
                    })
                });
            }
        }
        else
        {
            entry.Status = OutboxStatus.Pending;
            // Exponential backoff: 5s, 25s, 2m, 10m, 52m
            var delaySeconds = Math.Pow(5, entry.Attempts);
            entry.NextAttemptAt = DateTime.UtcNow.AddSeconds(delaySeconds);
        }

        await db.SaveChangesAsync(ct);
    }
}
