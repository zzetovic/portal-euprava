using MediatR;
using Microsoft.EntityFrameworkCore;
using Portal.Application.DTOs.Admin;
using Portal.Application.Interfaces;
using Portal.Domain.Enums;

namespace Portal.Application.Queries.Admin.GetRequestTypeUsage;

public record GetRequestTypeUsageQuery(Guid TenantId, Guid Id) : IRequest<RequestTypeUsageDto>;

public class GetRequestTypeUsageQueryHandler(IPortalDbContext db)
    : IRequestHandler<GetRequestTypeUsageQuery, RequestTypeUsageDto>
{
    public async Task<RequestTypeUsageDto> Handle(GetRequestTypeUsageQuery request, CancellationToken ct)
    {
        var exists = await db.RequestTypes.AnyAsync(
            rt => rt.Id == request.Id && rt.TenantId == request.TenantId, ct);
        if (!exists)
            throw new InvalidOperationException("REQUEST_TYPE_NOT_FOUND");

        var counts = await db.Requests
            .Where(r => r.RequestTypeId == request.Id && r.TenantId == request.TenantId)
            .GroupBy(r => r.Status)
            .Select(g => new { Status = g.Key, Count = g.Count() })
            .ToListAsync(ct);

        return new RequestTypeUsageDto(
            DraftCount: counts.FirstOrDefault(c => c.Status == RequestStatus.Draft)?.Count ?? 0,
            SubmittedCount: counts.Where(c => c.Status is RequestStatus.Submitted or RequestStatus.ProcessingRegistry).Sum(c => c.Count),
            ReceivedCount: counts.FirstOrDefault(c => c.Status == RequestStatus.ReceivedInRegistry)?.Count ?? 0,
            RejectedCount: counts.FirstOrDefault(c => c.Status == RequestStatus.RejectedByOfficer)?.Count ?? 0,
            TotalCount: counts.Sum(c => c.Count));
    }
}
