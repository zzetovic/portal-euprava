using System.Text.Json;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Portal.Application.Commands.Office.AcceptRequest;
using Portal.Application.Commands.Office.RejectRequest;
using Portal.Application.Commands.Office.RetryAccept;
using Portal.Application.DTOs.Office;
using Portal.Application.Interfaces;
using Portal.Application.Queries.Office.GetInbox;
using Portal.Application.Queries.Office.GetOfficerRequestDetail;
using Portal.Application.Queries.Office.GetUnreadCount;
using Portal.Domain.Enums;

namespace Portal.Api.Endpoints;

public static class OfficerEndpoints
{
    public static void MapOfficerEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/v1/office")
            .WithTags("Officer - Back Office")
            .RequireAuthorization();

        group.MapGet("/inbox", async (
            string? tab,
            string? requestTypeId,
            string? search,
            DateTime? dateFrom,
            DateTime? dateTo,
            string? sort,
            int? page,
            int? size,
            ICurrentUserService currentUser,
            ITenantProvider tenantProvider,
            IMediator mediator) =>
        {
            if (!IsOfficer(currentUser)) return Results.Forbid();
            var p = page is null or < 1 ? 1 : page.Value;
            var s = size is null or < 1 or > 100 ? 25 : size.Value;

            var result = await mediator.Send(new GetInboxQuery(
                tenantProvider.GetCurrentTenantId(),
                tab, requestTypeId, search, dateFrom, dateTo, sort, p, s));
            return Results.Ok(result);
        });

        group.MapGet("/inbox/unread-count", async (
            ICurrentUserService currentUser,
            ITenantProvider tenantProvider,
            IMediator mediator) =>
        {
            if (!IsOfficer(currentUser)) return Results.Forbid();
            var count = await mediator.Send(new GetUnreadCountQuery(tenantProvider.GetCurrentTenantId()));
            return Results.Ok(new { count });
        });

        group.MapGet("/requests/{id:guid}", async (
            Guid id,
            ICurrentUserService currentUser,
            ITenantProvider tenantProvider,
            IMediator mediator) =>
        {
            if (!IsOfficer(currentUser)) return Results.Forbid();
            try
            {
                var result = await mediator.Send(new GetOfficerRequestDetailQuery(
                    tenantProvider.GetCurrentTenantId(), currentUser.UserId!.Value, id));
                return Results.Ok(result);
            }
            catch (InvalidOperationException ex) when (ex.Message == "REQUEST_NOT_FOUND")
            {
                return Results.NotFound();
            }
        });

        group.MapGet("/requests/{id:guid}/attachments/{attId:guid}/preview", async (
            Guid id,
            Guid attId,
            ICurrentUserService currentUser,
            ITenantProvider tenantProvider,
            IPortalDbContext db,
            IAttachmentStorage attachmentStorage) =>
        {
            if (!IsOfficer(currentUser)) return Results.Forbid();

            var attachment = await db.RequestAttachments
                .FirstOrDefaultAsync(a => a.Id == attId && a.RequestId == id
                    && a.Request.TenantId == tenantProvider.GetCurrentTenantId());

            if (attachment is null)
                return Results.NotFound();

            var stream = await attachmentStorage.OpenReadAsync(attachment.StorageKey, CancellationToken.None);
            return Results.File(stream, attachment.MimeType);
        });

        group.MapGet("/requests/{id:guid}/attachments/{attId:guid}/download", async (
            Guid id,
            Guid attId,
            ICurrentUserService currentUser,
            ITenantProvider tenantProvider,
            IPortalDbContext db,
            IAttachmentStorage attachmentStorage) =>
        {
            if (!IsOfficer(currentUser)) return Results.Forbid();

            var attachment = await db.RequestAttachments
                .FirstOrDefaultAsync(a => a.Id == attId && a.RequestId == id
                    && a.Request.TenantId == tenantProvider.GetCurrentTenantId());

            if (attachment is null)
                return Results.NotFound();

            // Audit log without original_filename (PII protection per CLAUDE.md sec 17)
            db.AuditLogs.Add(new Domain.Entities.AuditLog
            {
                Id = Guid.NewGuid(),
                TenantId = tenantProvider.GetCurrentTenantId(),
                UserId = currentUser.UserId,
                Action = "attachment.downloaded",
                EntityType = "request_attachment",
                EntityId = attachment.Id
            });
            await db.SaveChangesAsync();

            var stream = await attachmentStorage.OpenReadAsync(attachment.StorageKey, CancellationToken.None);
            return Results.File(stream, attachment.MimeType, attachment.OriginalFilename);
        });

        group.MapPost("/requests/{id:guid}/accept", async (
            Guid id,
            ICurrentUserService currentUser,
            ITenantProvider tenantProvider,
            IMediator mediator,
            IPortalDbContext db) =>
        {
            if (!IsOfficer(currentUser)) return Results.Forbid();
            try
            {
                var result = await mediator.Send(new AcceptRequestCommand(
                    tenantProvider.GetCurrentTenantId(), currentUser.UserId!.Value, id));

                // Sync wait with 12s timeout (CLAUDE.md sec 9.2 step 2)
                var outboxEntry = await db.IntegrationOutbox.FindAsync(result.OutboxId);
                if (outboxEntry is not null)
                {
                    var deadline = DateTime.UtcNow.AddSeconds(12);
                    while (DateTime.UtcNow < deadline)
                    {
                        await Task.Delay(500);
                        // Reload from DB
                        await ((Microsoft.EntityFrameworkCore.DbContext)(object)db).Entry(outboxEntry).ReloadAsync();
                        if (outboxEntry.Status == OutboxStatus.Done)
                        {
                            var mapping = await db.SeupAktMappings
                                .FirstOrDefaultAsync(m => m.RequestId == id);
                            return Results.Ok(new { aktId = mapping?.AktId, status = "received_in_registry" });
                        }
                        if (outboxEntry.Status == OutboxStatus.DeadLetter || outboxEntry.Status == OutboxStatus.Failed)
                            break;
                    }
                }

                // Timeout or failure - return 202 Accepted
                return Results.Accepted(value: new { status = "processing", outboxId = result.OutboxId });
            }
            catch (InvalidOperationException ex) when (ex.Message == "REQUEST_NOT_FOUND")
            {
                return Results.NotFound();
            }
            catch (InvalidOperationException ex) when (ex.Message == "REQUEST_NOT_SUBMITTED")
            {
                return Results.Conflict(new { detail = "Zahtjev nije u statusu za zaprimanje." });
            }
            catch (InvalidOperationException ex) when (ex.Message == "ALREADY_PROCESSING")
            {
                return Results.Conflict(new { detail = "Zahtjev se već obrađuje." });
            }
        });

        group.MapPost("/requests/{id:guid}/retry-accept", async (
            Guid id,
            ICurrentUserService currentUser,
            ITenantProvider tenantProvider,
            IMediator mediator) =>
        {
            if (!IsOfficer(currentUser)) return Results.Forbid();
            try
            {
                await mediator.Send(new RetryAcceptCommand(
                    tenantProvider.GetCurrentTenantId(), currentUser.UserId!.Value, id));
                return Results.Ok(new { status = "pending" });
            }
            catch (InvalidOperationException ex) when (ex.Message == "OUTBOX_NOT_FOUND")
            {
                return Results.NotFound(new { detail = "Nema dead-letter zapisa za ovaj zahtjev." });
            }
        });

        group.MapPost("/requests/{id:guid}/reject", async (
            Guid id,
            RejectRequestBody body,
            ICurrentUserService currentUser,
            ITenantProvider tenantProvider,
            IMediator mediator) =>
        {
            if (!IsOfficer(currentUser)) return Results.Forbid();
            try
            {
                await mediator.Send(new RejectRequestCommand(
                    tenantProvider.GetCurrentTenantId(), currentUser.UserId!.Value, id,
                    body.RejectionReasonCode, body.InternalNote));
                return Results.NoContent();
            }
            catch (InvalidOperationException ex) when (ex.Message == "REQUEST_NOT_FOUND")
            {
                return Results.NotFound();
            }
            catch (InvalidOperationException ex) when (ex.Message == "REQUEST_NOT_SUBMITTED")
            {
                return Results.Conflict(new { detail = "Zahtjev nije u statusu za odbijanje." });
            }
        });

        group.MapGet("/rejection-reasons", (ICurrentUserService currentUser) =>
        {
            if (!IsOfficer(currentUser)) return Results.Forbid();

            var reasons = new List<RejectionReasonDto>
            {
                new("inappropriate_content", JsonSerializer.Serialize(new Dictionary<string, string>
                {
                    ["hr"] = "Neprimjereni sadržaj", ["en"] = "Inappropriate content"
                })),
                new("out_of_jurisdiction", JsonSerializer.Serialize(new Dictionary<string, string>
                {
                    ["hr"] = "Nije u nadležnosti ove JLS", ["en"] = "Out of jurisdiction"
                })),
                new("duplicate", JsonSerializer.Serialize(new Dictionary<string, string>
                {
                    ["hr"] = "Ponavljajući zahtjev", ["en"] = "Duplicate request"
                })),
                new("not_serious", JsonSerializer.Serialize(new Dictionary<string, string>
                {
                    ["hr"] = "Očito neozbiljan zahtjev", ["en"] = "Obviously frivolous request"
                })),
                new("other", JsonSerializer.Serialize(new Dictionary<string, string>
                {
                    ["hr"] = "Ostalo", ["en"] = "Other"
                }))
            };

            return Results.Ok(reasons);
        });
    }

    private static bool IsOfficer(ICurrentUserService currentUser) =>
        currentUser.IsAuthenticated &&
        (currentUser.UserType == UserType.JlsOfficer || currentUser.UserType == UserType.JlsAdmin);
}
