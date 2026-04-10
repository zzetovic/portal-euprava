using MediatR;
using Portal.Application.Commands.Notifications.MarkAllRead;
using Portal.Application.Commands.Notifications.MarkRead;
using Portal.Application.Interfaces;
using Portal.Application.Queries.Notifications.GetNotifications;

namespace Portal.Api.Endpoints;

public static class NotificationEndpoints
{
    public static void MapNotificationEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/v1/notifications")
            .WithTags("Notifications")
            .RequireAuthorization();

        group.MapGet("/", async (
            int? page,
            int? pageSize,
            ICurrentUserService currentUser,
            ITenantProvider tenantProvider,
            IMediator mediator) =>
        {
            if (currentUser.UserId is null) return Results.Unauthorized();
            var p = page is null or < 1 ? 1 : page.Value;
            var ps = pageSize is null or < 1 or > 100 ? 20 : pageSize.Value;

            var result = await mediator.Send(new GetNotificationsQuery(
                tenantProvider.GetCurrentTenantId(), currentUser.UserId.Value, p, ps));
            return Results.Ok(result);
        });

        group.MapPost("/{id:guid}/read", async (
            Guid id,
            ICurrentUserService currentUser,
            ITenantProvider tenantProvider,
            IMediator mediator) =>
        {
            if (currentUser.UserId is null) return Results.Unauthorized();
            try
            {
                await mediator.Send(new MarkNotificationReadCommand(
                    tenantProvider.GetCurrentTenantId(), currentUser.UserId.Value, id));
                return Results.NoContent();
            }
            catch (InvalidOperationException)
            {
                return Results.NotFound();
            }
        });

        group.MapPost("/read-all", async (
            ICurrentUserService currentUser,
            ITenantProvider tenantProvider,
            IMediator mediator) =>
        {
            if (currentUser.UserId is null) return Results.Unauthorized();
            var count = await mediator.Send(new MarkAllNotificationsReadCommand(
                tenantProvider.GetCurrentTenantId(), currentUser.UserId.Value));
            return Results.Ok(new { markedRead = count });
        });
    }
}
