using MediatR;
using Microsoft.EntityFrameworkCore;
using Portal.Application.DTOs.Office;
using Portal.Application.Interfaces;
using Portal.Domain.Enums;

namespace Portal.Application.Queries.Office.GetInbox;

public record GetInboxQuery(
    Guid TenantId,
    string? Tab,
    string? RequestTypeId,
    string? Search,
    DateTime? DateFrom,
    DateTime? DateTo,
    string? Sort,
    int Page,
    int PageSize) : IRequest<InboxResult>;

public class GetInboxQueryHandler(IPortalDbContext db) : IRequestHandler<GetInboxQuery, InboxResult>
{
    public async Task<InboxResult> Handle(GetInboxQuery request, CancellationToken ct)
    {
        var query = db.Requests
            .Include(r => r.Citizen)
            .Include(r => r.RequestType)
            .Include(r => r.AktMapping)
            .Where(r => r.TenantId == request.TenantId && r.Status != RequestStatus.Draft);

        query = request.Tab switch
        {
            "pending" => query.Where(r => r.Status == RequestStatus.Submitted || r.Status == RequestStatus.ProcessingRegistry),
            "received" => query.Where(r => r.Status == RequestStatus.ReceivedInRegistry),
            "rejected" => query.Where(r => r.Status == RequestStatus.RejectedByOfficer),
            _ => query // "all"
        };

        if (!string.IsNullOrEmpty(request.RequestTypeId) && Guid.TryParse(request.RequestTypeId, out var rtId))
            query = query.Where(r => r.RequestTypeId == rtId);

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var search = request.Search.ToLower();
            query = query.Where(r =>
                r.ReferenceNumber.ToLower().Contains(search) ||
                r.Citizen.FirstName.ToLower().Contains(search) ||
                r.Citizen.LastName.ToLower().Contains(search) ||
                (r.Citizen.Oib != null && r.Citizen.Oib.Contains(search)));
        }

        if (request.DateFrom.HasValue)
            query = query.Where(r => r.SubmittedAt >= request.DateFrom.Value);

        if (request.DateTo.HasValue)
            query = query.Where(r => r.SubmittedAt <= request.DateTo.Value);

        query = request.Sort switch
        {
            "newest" => query.OrderByDescending(r => r.SubmittedAt),
            _ => query.OrderBy(r => r.SubmittedAt) // oldest first (FIFO default)
        };

        var totalCount = await query.CountAsync(ct);

        var items = await query
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(r => new InboxItemDto(
                r.Id,
                r.ReferenceNumber,
                r.Status.ToString(),
                r.RequestType.NameI18n,
                r.RequestType.Code,
                r.Citizen.FirstName + " " + r.Citizen.LastName,
                r.Citizen.Oib,
                r.Attachments.Count,
                r.SubmittedAt ?? r.CreatedAt,
                r.ViewedFirstAt,
                r.AktMapping != null ? r.AktMapping.AktId : null))
            .ToListAsync(ct);

        return new InboxResult(items, totalCount, request.Page, request.PageSize);
    }
}
