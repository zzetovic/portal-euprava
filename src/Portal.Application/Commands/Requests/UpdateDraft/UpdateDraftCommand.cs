using MediatR;
using Microsoft.EntityFrameworkCore;
using Portal.Application.Interfaces;
using Portal.Domain.Enums;

namespace Portal.Application.Commands.Requests.UpdateDraft;

public record UpdateDraftCommand(
    Guid TenantId,
    Guid CitizenId,
    Guid RequestId,
    string FormData,
    string Etag) : IRequest<UpdateDraftResult>;

public record UpdateDraftResult(string NewEtag);

public class UpdateDraftCommandHandler(IPortalDbContext db)
    : IRequestHandler<UpdateDraftCommand, UpdateDraftResult>
{
    public async Task<UpdateDraftResult> Handle(UpdateDraftCommand request, CancellationToken ct)
    {
        var r = await db.Requests
            .FirstOrDefaultAsync(r => r.Id == request.RequestId
                && r.TenantId == request.TenantId
                && r.CitizenId == request.CitizenId, ct)
            ?? throw new InvalidOperationException("REQUEST_NOT_FOUND");

        if (r.Status != RequestStatus.Draft)
            throw new InvalidOperationException("REQUEST_NOT_DRAFT");

        if (r.Etag != request.Etag)
            throw new InvalidOperationException("ETAG_MISMATCH");

        r.FormData = request.FormData;
        r.Etag = Guid.NewGuid().ToString("N");

        // Sliding expiry - reset to 90 days unless locked to old version
        if (!r.IsLockedToOldVersion)
            r.ExpiresAt = DateTime.UtcNow.AddDays(90);

        await db.SaveChangesAsync(ct);

        return new UpdateDraftResult(r.Etag);
    }
}
