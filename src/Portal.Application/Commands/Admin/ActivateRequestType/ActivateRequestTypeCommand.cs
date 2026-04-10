using MediatR;
using Microsoft.EntityFrameworkCore;
using Portal.Application.Interfaces;
using Portal.Domain.Entities;

namespace Portal.Application.Commands.Admin.ActivateRequestType;

public record ActivateRequestTypeCommand(Guid TenantId, Guid UserId, Guid Id) : IRequest;

public class ActivateRequestTypeCommandHandler(IPortalDbContext db) : IRequestHandler<ActivateRequestTypeCommand>
{
    public async Task Handle(ActivateRequestTypeCommand request, CancellationToken ct)
    {
        var rt = await db.RequestTypes
            .FirstOrDefaultAsync(r => r.Id == request.Id && r.TenantId == request.TenantId, ct)
            ?? throw new InvalidOperationException("REQUEST_TYPE_NOT_FOUND");

        if (rt.IsArchived)
            throw new InvalidOperationException("REQUEST_TYPE_ARCHIVED");

        rt.IsActive = true;

        db.AuditLogs.Add(new AuditLog
        {
            Id = Guid.NewGuid(),
            TenantId = request.TenantId,
            UserId = request.UserId,
            Action = "request_type.activated",
            EntityType = "request_type",
            EntityId = rt.Id
        });

        await db.SaveChangesAsync(ct);
    }
}
