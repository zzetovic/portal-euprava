using MediatR;
using Microsoft.EntityFrameworkCore;
using Portal.Application.Interfaces;

namespace Portal.Application.Commands.Notifications.MarkAllRead;

public record MarkAllNotificationsReadCommand(Guid TenantId, Guid UserId) : IRequest<int>;

public class MarkAllNotificationsReadCommandHandler(IPortalDbContext db)
    : IRequestHandler<MarkAllNotificationsReadCommand, int>
{
    public async Task<int> Handle(MarkAllNotificationsReadCommand request, CancellationToken ct)
    {
        var unread = await db.Notifications
            .Where(n => n.TenantId == request.TenantId && n.UserId == request.UserId && !n.IsRead)
            .ToListAsync(ct);

        foreach (var n in unread)
        {
            n.IsRead = true;
            n.ReadAt = DateTime.UtcNow;
        }

        await db.SaveChangesAsync(ct);
        return unread.Count;
    }
}
