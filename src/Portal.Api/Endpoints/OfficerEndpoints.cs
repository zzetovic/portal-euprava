using System.Text.Json;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Portal.Application.Commands.Office.RejectRequest;
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
            int page,
            int size,
            ICurrentUserService currentUser,
            ITenantProvider tenantProvider,
            IMediator mediator) =>
        {
            if (!IsOfficer(currentUser)) return Results.Forbid();
            var p = page < 1 ? 1 : page;
            var s = size is < 1 or > 100 ? 25 : size;

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
