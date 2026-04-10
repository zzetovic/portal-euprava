using MediatR;
using Microsoft.EntityFrameworkCore;
using Portal.Application.Interfaces;

namespace Portal.Application.Commands.Notifications.MarkRead;

public record MarkNotificationReadCommand(Guid TenantId, Guid UserId, Guid NotificationId) : IRequest;

public class MarkNotificationReadCommandHandler(IPortalDbContext db) : IRequestHandler<MarkNotificationReadCommand>
{
    public async Task Handle(MarkNotificationReadCommand request, CancellationToken ct)
    {
        var notification = await db.Notifications
            .FirstOrDefaultAsync(n => n.Id == request.NotificationId
                && n.TenantId == request.TenantId
                && n.UserId == request.UserId, ct)
            ?? throw new InvalidOperationException("NOTIFICATION_NOT_FOUND");

        if (!notification.IsRead)
        {
            notification.IsRead = true;
            notification.ReadAt = DateTime.UtcNow;
            await db.SaveChangesAsync(ct);
        }
    }
}
