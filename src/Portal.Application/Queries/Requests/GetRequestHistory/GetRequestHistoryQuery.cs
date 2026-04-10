using MediatR;
using Microsoft.EntityFrameworkCore;
using Portal.Application.DTOs.Requests;
using Portal.Application.Interfaces;

namespace Portal.Application.Queries.Requests.GetRequestHistory;

public record GetRequestHistoryQuery(
    Guid TenantId,
    Guid CitizenId,
    Guid RequestId) : IRequest<List<RequestStatusHistoryDto>>;

public class GetRequestHistoryQueryHandler(IPortalDbContext db)
    : IRequestHandler<GetRequestHistoryQuery, List<RequestStatusHistoryDto>>
{
    public async Task<List<RequestStatusHistoryDto>> Handle(GetRequestHistoryQuery request, CancellationToken ct)
    {
        var exists = await db.Requests.AnyAsync(r =>
            r.Id == request.RequestId && r.TenantId == request.TenantId && r.CitizenId == request.CitizenId, ct);

        if (!exists)
            throw new InvalidOperationException("REQUEST_NOT_FOUND");

        return await db.RequestStatusHistories
            .Where(h => h.RequestId == request.RequestId)
            .OrderBy(h => h.ChangedAt)
            .Select(h => new RequestStatusHistoryDto(
                h.FromStatus != null ? h.FromStatus.ToString() : null,
                h.ToStatus.ToString(),
                h.ChangedBySource.ToString(),
                h.Comment,
                h.ChangedAt))
            .ToListAsync(ct);
    }
}
