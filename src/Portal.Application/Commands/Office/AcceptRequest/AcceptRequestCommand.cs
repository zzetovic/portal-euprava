using System.Text.Json;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Portal.Application.Interfaces;
using Portal.Domain.Entities;
using Portal.Domain.Enums;

namespace Portal.Application.Commands.Office.AcceptRequest;

public record AcceptRequestCommand(
    Guid TenantId,
    Guid OfficerId,
    Guid RequestId) : IRequest<AcceptRequestResult>;

public record AcceptRequestResult(
    string Status,
    long? AktId,
    Guid OutboxId);

public class AcceptRequestCommandHandler(IPortalDbContext db)
    : IRequestHandler<AcceptRequestCommand, AcceptRequestResult>
{
    public async Task<AcceptRequestResult> Handle(AcceptRequestCommand request, CancellationToken ct)
    {
        // Single PostgreSQL transaction as specified in CLAUDE.md sec 9.2
        var r = await db.Requests
            .Include(r => r.Citizen)
            .Include(r => r.Attachments)
            .Include(r => r.RequestType)
            .FirstOrDefaultAsync(r => r.Id == request.RequestId && r.TenantId == request.TenantId, ct)
            ?? throw new InvalidOperationException("REQUEST_NOT_FOUND");

        // Step 1: Status check (idempotency layer 2)
        if (r.Status != RequestStatus.Submitted)
            throw new InvalidOperationException("REQUEST_NOT_SUBMITTED");

        // Step 2: Check outbox doesn't already exist (idempotency layer 1)
        var existingOutbox = await db.IntegrationOutbox.AnyAsync(
            o => o.IdempotencyKey == request.RequestId.ToString()
                && (o.Status == OutboxStatus.Pending || o.Status == OutboxStatus.Processing || o.Status == OutboxStatus.Done), ct);

        if (existingOutbox)
            throw new InvalidOperationException("ALREADY_PROCESSING");

        // Step 3: Update status to processing_registry
        r.Status = RequestStatus.ProcessingRegistry;
        r.ReviewedByUserId = request.OfficerId;
        r.ReviewedAt = DateTime.UtcNow;

        // Step 4: Insert outbox
        var payload = JsonSerializer.Serialize(new
        {
            RequestId = r.Id,
            r.TenantId,
            CitizenOib = r.Citizen.Oib ?? "",
            CitizenFullName = $"{r.Citizen.FirstName} {r.Citizen.LastName}",
            CitizenAddress = "", // TODO: address field from form data
            CitizenEmail = r.Citizen.Email,
            Subject = r.RequestType?.NameI18n ?? r.ReferenceNumber,
            BodyText = r.FormData,
            ReceivedAt = r.ReviewedAt,
            OfficerId = request.OfficerId,
            Attachments = r.Attachments.Select(a => new
            {
                a.OriginalFilename, a.MimeType, a.SizeBytes, a.StorageKey
            })
        });

        var outboxEntry = new IntegrationOutbox
        {
            Id = Guid.NewGuid(),
            TenantId = request.TenantId,
            AggregateType = "request",
            AggregateId = r.Id,
            Operation = "write_akt_to_seup",
            IdempotencyKey = request.RequestId.ToString(),
            Payload = payload,
            Status = OutboxStatus.Pending,
            Attempts = 0,
            NextAttemptAt = DateTime.UtcNow
        };

        db.IntegrationOutbox.Add(outboxEntry);

        // Step 5: Status history
        db.RequestStatusHistories.Add(new RequestStatusHistory
        {
            Id = Guid.NewGuid(),
            RequestId = r.Id,
            FromStatus = RequestStatus.Submitted,
            ToStatus = RequestStatus.ProcessingRegistry,
            ChangedByUserId = request.OfficerId,
            ChangedBySource = StatusChangeSource.Officer,
            ChangedAt = DateTime.UtcNow
        });

        db.AuditLogs.Add(new AuditLog
        {
            Id = Guid.NewGuid(),
            TenantId = request.TenantId,
            UserId = request.OfficerId,
            Action = "request.accepted",
            EntityType = "request",
            EntityId = r.Id,
            Before = JsonSerializer.Serialize(new { Status = RequestStatus.Submitted.ToString() }),
            After = JsonSerializer.Serialize(new { Status = RequestStatus.ProcessingRegistry.ToString() })
        });

        await db.SaveChangesAsync(ct);

        return new AcceptRequestResult("processing", null, outboxEntry.Id);
    }
}
