using MediatR;
using Microsoft.EntityFrameworkCore;
using Portal.Application.Interfaces;
using Portal.Domain.Enums;

namespace Portal.Application.Queries.Office.GetUnreadCount;

public record GetUnreadCountQuery(Guid TenantId) : IRequest<int>;

public class GetUnreadCountQueryHandler(IPortalDbContext db) : IRequestHandler<GetUnreadCountQuery, int>
{
    public async Task<int> Handle(GetUnreadCountQuery request, CancellationToken ct)
    {
        return await db.Requests.CountAsync(r =>
            r.TenantId == request.TenantId
            && (r.Status == RequestStatus.Submitted || r.Status == RequestStatus.ProcessingRegistry)
            && r.ViewedFirstAt == null, ct);
    }
}
