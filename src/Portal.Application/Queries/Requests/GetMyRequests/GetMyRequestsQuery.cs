using MediatR;
using Microsoft.EntityFrameworkCore;
using Portal.Application.DTOs.Requests;
using Portal.Application.Interfaces;
using Portal.Domain.Enums;

namespace Portal.Application.Queries.Requests.GetMyRequests;

public record GetMyRequestsQuery(
    Guid TenantId,
    Guid CitizenId,
    string? StatusFilter,
    int Page,
    int PageSize) : IRequest<PaginatedResult<CitizenRequestListItemDto>>;

public class GetMyRequestsQueryHandler(IPortalDbContext db)
    : IRequestHandler<GetMyRequestsQuery, PaginatedResult<CitizenRequestListItemDto>>
{
    public async Task<PaginatedResult<CitizenRequestListItemDto>> Handle(GetMyRequestsQuery request, CancellationToken ct)
    {
        var query = db.Requests
            .Where(r => r.TenantId == request.TenantId && r.CitizenId == request.CitizenId);

        if (!string.IsNullOrEmpty(request.StatusFilter) && request.StatusFilter != "all")
        {
            query = request.StatusFilter switch
            {
                "draft" => query.Where(r => r.Status == RequestStatus.Draft),
                "submitted" => query.Where(r => r.Status == RequestStatus.Submitted || r.Status == RequestStatus.ProcessingRegistry),
                "received" => query.Where(r => r.Status == RequestStatus.ReceivedInRegistry),
                "rejected" => query.Where(r => r.Status == RequestStatus.RejectedByOfficer),
                _ => query
            };
        }

        var totalCount = await query.CountAsync(ct);

        var items = await query
            .OrderByDescending(r => r.CreatedAt)
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(r => new CitizenRequestListItemDto(
                r.Id,
                r.ReferenceNumber,
                MapCitizenStatus(r.Status),
                r.RequestType.NameI18n,
                r.RequestType.Code,
                r.CreatedAt,
                r.SubmittedAt,
                r.AktMapping != null ? r.AktMapping.AktId : null))
            .ToListAsync(ct);

        return new PaginatedResult<CitizenRequestListItemDto>(items, totalCount, request.Page, request.PageSize);
    }

    private static string MapCitizenStatus(RequestStatus status) => status switch
    {
        RequestStatus.Draft => "draft",
        RequestStatus.Submitted => "submitted",
        RequestStatus.ProcessingRegistry => "submitted", // Hide internal state from citizen
        RequestStatus.ReceivedInRegistry => "received_in_registry",
        RequestStatus.RejectedByOfficer => "rejected_by_officer",
        _ => status.ToString()
    };
}
