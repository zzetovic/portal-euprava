using Microsoft.Extensions.Logging;
using Portal.Application.Interfaces;

namespace Portal.Infrastructure.Email;

public class NullEmailSender(ILogger<NullEmailSender> logger) : IEmailSender
{
    public Task SendAsync(EmailMessage message, CancellationToken ct)
    {
        logger.LogInformation("Email suppressed (NullEmailSender). To: {To}, Subject: {Subject}", message.To, message.Subject);
        return Task.CompletedTask;
    }
}
