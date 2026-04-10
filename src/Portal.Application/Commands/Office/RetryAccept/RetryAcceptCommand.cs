using MediatR;
using Microsoft.EntityFrameworkCore;
using Portal.Application.Interfaces;
using Portal.Domain.Enums;

namespace Portal.Application.Commands.Office.RetryAccept;

public record RetryAcceptCommand(Guid TenantId, Guid OfficerId, Guid RequestId) : IRequest;

public class RetryAcceptCommandHandler(IPortalDbContext db) : IRequestHandler<RetryAcceptCommand>
{
    public async Task Handle(RetryAcceptCommand request, CancellationToken ct)
    {
        var outbox = await db.IntegrationOutbox
            .FirstOrDefaultAsync(o =>
                o.IdempotencyKey == request.RequestId.ToString()
                && o.TenantId == request.TenantId
                && o.Status == OutboxStatus.DeadLetter, ct)
            ?? throw new InvalidOperationException("OUTBOX_NOT_FOUND");

        outbox.Status = OutboxStatus.Pending;
        outbox.Attempts = 0;
        outbox.NextAttemptAt = DateTime.UtcNow;
        outbox.LastError = null;

        await db.SaveChangesAsync(ct);
    }
}
