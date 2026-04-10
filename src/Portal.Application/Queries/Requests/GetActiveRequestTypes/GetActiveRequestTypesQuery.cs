using MediatR;
using Microsoft.EntityFrameworkCore;
using Portal.Application.DTOs.Requests;
using Portal.Application.Interfaces;

namespace Portal.Application.Queries.Requests.GetActiveRequestTypes;

public record GetActiveRequestTypesQuery(Guid TenantId) : IRequest<List<RequestTypeListItemForCitizenDto>>;

public class GetActiveRequestTypesQueryHandler(IPortalDbContext db)
    : IRequestHandler<GetActiveRequestTypesQuery, List<RequestTypeListItemForCitizenDto>>
{
    public async Task<List<RequestTypeListItemForCitizenDto>> Handle(GetActiveRequestTypesQuery request, CancellationToken ct)
    {
        return await db.RequestTypes
            .Where(rt => rt.TenantId == request.TenantId && rt.IsActive && !rt.IsArchived)
            .OrderBy(rt => rt.SortOrder)
            .Select(rt => new RequestTypeListItemForCitizenDto(
                rt.Id, rt.Code, rt.NameI18n, rt.DescriptionI18n, rt.SortOrder))
            .ToListAsync(ct);
    }
}
