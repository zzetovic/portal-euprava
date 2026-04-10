using MediatR;
using Microsoft.EntityFrameworkCore;
using Portal.Application.DTOs.Notifications;
using Portal.Application.DTOs.Requests;
using Portal.Application.Interfaces;

namespace Portal.Application.Queries.Notifications.GetNotifications;

public record GetNotificationsQuery(
    Guid TenantId,
    Guid UserId,
    int Page,
    int PageSize) : IRequest<PaginatedResult<NotificationDto>>;

public class GetNotificationsQueryHandler(IPortalDbContext db)
    : IRequestHandler<GetNotificationsQuery, PaginatedResult<NotificationDto>>
{
    public async Task<PaginatedResult<NotificationDto>> Handle(GetNotificationsQuery request, CancellationToken ct)
    {
        var query = db.Notifications
            .Where(n => n.TenantId == request.TenantId && n.UserId == request.UserId);

        var totalCount = await query.CountAsync(ct);

        var items = await query
            .OrderByDescending(n => n.CreatedAt)
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(n => new NotificationDto(
                n.Id, n.Type, n.TitleI18n, n.BodyI18n,
                n.RelatedRequestId, n.IsRead, n.ReadAt, n.CreatedAt))
            .ToListAsync(ct);

        return new PaginatedResult<NotificationDto>(items, totalCount, request.Page, request.PageSize);
    }
}
