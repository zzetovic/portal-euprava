using MediatR;
using Microsoft.EntityFrameworkCore;
using Portal.Application.DTOs.Admin;
using Portal.Application.Interfaces;

namespace Portal.Application.Queries.Admin.GetRequestTypes;

public record GetRequestTypesQuery(
    Guid TenantId,
    string? Filter,
    string? Search) : IRequest<List<RequestTypeListItemDto>>;

public class GetRequestTypesQueryHandler(IPortalDbContext db)
    : IRequestHandler<GetRequestTypesQuery, List<RequestTypeListItemDto>>
{
    public async Task<List<RequestTypeListItemDto>> Handle(GetRequestTypesQuery request, CancellationToken ct)
    {
        var query = db.RequestTypes
            .Where(rt => rt.TenantId == request.TenantId);

        query = request.Filter switch
        {
            "active" => query.Where(rt => rt.IsActive && !rt.IsArchived),
            "archived" => query.Where(rt => rt.IsArchived),
            _ => query.Where(rt => !rt.IsArchived) // "all" non-archived by default
        };

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var search = request.Search.ToLower();
            query = query.Where(rt =>
                (rt.NameI18n != null && rt.NameI18n.ToLower().Contains(search)) ||
                rt.Code.ToLower().Contains(search));
        }

        return await query
            .OrderBy(rt => rt.SortOrder)
            .ThenBy(rt => rt.CreatedAt)
            .Select(rt => new RequestTypeListItemDto(
                rt.Id,
                rt.Code,
                rt.NameI18n,
                rt.DescriptionI18n,
                rt.IsActive,
                rt.IsArchived,
                rt.SortOrder,
                rt.Version,
                rt.EstimatedProcessingDays,
                rt.Fields.Count,
                rt.Attachments.Count,
                rt.UpdatedAt))
            .ToListAsync(ct);
    }
}
