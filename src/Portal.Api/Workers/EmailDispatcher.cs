using Microsoft.EntityFrameworkCore;
using Portal.Application.Interfaces;
using Portal.Domain.Enums;

namespace Portal.Api.Workers;

public class EmailDispatcher(
    IServiceScopeFactory scopeFactory,
    ILogger<EmailDispatcher> logger,
    IConfiguration configuration) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await Task.Yield(); // Ensure host startup is not blocked

        if (!configuration.GetValue("Workers:EmailDispatcher:Enabled", true))
        {
            logger.LogInformation("EmailDispatcher is disabled");
            return;
        }

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessPendingDeliveries(stoppingToken);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "EmailDispatcher encountered an error");
            }

            await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
        }
    }

    private async Task ProcessPendingDeliveries(CancellationToken ct)
    {
        using var scope = scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<IPortalDbContext>();
        var emailSender = scope.ServiceProvider.GetRequiredService<IEmailSender>();

        var deliveries = await db.NotificationDeliveries
            .Include(d => d.Notification)
                .ThenInclude(n => n.User)
            .Where(d => d.Channel == NotificationChannel.Email
                && d.Status == DeliveryStatus.Pending)
            .OrderBy(d => d.CreatedAt)
            .Take(20)
            .ToListAsync(ct);

        foreach (var delivery in deliveries)
        {
            try
            {
                var user = delivery.Notification.User;
                var title = delivery.Notification.TitleI18n ?? "Notifikacija";

                await emailSender.SendAsync(new EmailMessage(
                    To: user.Email,
                    Subject: title,
                    HtmlBody: $"<p>{delivery.Notification.BodyI18n}</p>"), ct);

                delivery.Status = DeliveryStatus.Sent;
                delivery.SentAt = DateTime.UtcNow;
            }
            catch (Exception ex)
            {
                delivery.Attempts++;
                delivery.LastError = ex.Message;

                if (delivery.Attempts >= 3)
                    delivery.Status = DeliveryStatus.Failed;
            }
        }

        if (deliveries.Count > 0)
            await db.SaveChangesAsync(ct);
    }
}
