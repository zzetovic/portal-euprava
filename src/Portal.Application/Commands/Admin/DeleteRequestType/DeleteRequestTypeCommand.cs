using MediatR;
using Microsoft.EntityFrameworkCore;
using Portal.Application.Interfaces;
using Portal.Domain.Entities;
using Portal.Domain.Enums;

namespace Portal.Application.Commands.Admin.DeleteRequestType;

public record DeleteRequestTypeCommand(Guid TenantId, Guid UserId, Guid Id) : IRequest;

public class DeleteRequestTypeCommandHandler(IPortalDbContext db) : IRequestHandler<DeleteRequestTypeCommand>
{
    public async Task Handle(DeleteRequestTypeCommand request, CancellationToken ct)
    {
        var rt = await db.RequestTypes
            .FirstOrDefaultAsync(r => r.Id == request.Id && r.TenantId == request.TenantId, ct)
            ?? throw new InvalidOperationException("REQUEST_TYPE_NOT_FOUND");

        var hasActiveRequests = await db.Requests.AnyAsync(
            r => r.RequestTypeId == request.Id
                && r.TenantId == request.TenantId
                && (r.Status == RequestStatus.Draft || r.Status == RequestStatus.Submitted
                    || r.Status == RequestStatus.ProcessingRegistry), ct);

        if (hasActiveRequests)
            throw new InvalidOperationException("REQUEST_TYPE_HAS_ACTIVE_REQUESTS");

        rt.IsArchived = true;
        rt.IsActive = false;
        rt.DeletedAt = DateTime.UtcNow;

        db.AuditLogs.Add(new AuditLog
        {
            Id = Guid.NewGuid(),
            TenantId = request.TenantId,
            UserId = request.UserId,
            Action = "request_type.archived",
            EntityType = "request_type",
            EntityId = rt.Id
        });

        await db.SaveChangesAsync(ct);
    }
}
