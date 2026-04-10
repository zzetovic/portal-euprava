using MediatR;
using Microsoft.EntityFrameworkCore;
using Portal.Application.Interfaces;
using Portal.Domain.Entities;

namespace Portal.Application.Commands.Admin.DeactivateRequestType;

public record DeactivateRequestTypeCommand(Guid TenantId, Guid UserId, Guid Id) : IRequest;

public class DeactivateRequestTypeCommandHandler(IPortalDbContext db) : IRequestHandler<DeactivateRequestTypeCommand>
{
    public async Task Handle(DeactivateRequestTypeCommand request, CancellationToken ct)
    {
        var rt = await db.RequestTypes
            .FirstOrDefaultAsync(r => r.Id == request.Id && r.TenantId == request.TenantId, ct)
            ?? throw new InvalidOperationException("REQUEST_TYPE_NOT_FOUND");

        rt.IsActive = false;

        db.AuditLogs.Add(new AuditLog
        {
            Id = Guid.NewGuid(),
            TenantId = request.TenantId,
            UserId = request.UserId,
            Action = "request_type.deactivated",
            EntityType = "request_type",
            EntityId = rt.Id
        });

        await db.SaveChangesAsync(ct);
    }
}
