using MailKit.Net.Smtp;
using MimeKit;
using Portal.Application.Interfaces;

namespace Portal.Infrastructure.Email;

public class SmtpEmailSender(SmtpSettings settings) : IEmailSender
{
    public async Task SendAsync(EmailMessage message, CancellationToken ct)
    {
        var email = new MimeMessage();
        email.From.Add(MailboxAddress.Parse(message.From ?? settings.DefaultFrom));
        email.To.Add(MailboxAddress.Parse(message.To));
        email.Subject = message.Subject;

        var bodyBuilder = new BodyBuilder
        {
            HtmlBody = message.HtmlBody,
            TextBody = message.PlainTextBody
        };
        email.Body = bodyBuilder.ToMessageBody();

        using var smtp = new SmtpClient();
        await smtp.ConnectAsync(settings.Host, settings.Port, settings.UseSsl, ct);

        if (!string.IsNullOrEmpty(settings.Username))
            await smtp.AuthenticateAsync(settings.Username, settings.Password, ct);

        await smtp.SendAsync(email, ct);
        await smtp.DisconnectAsync(true, ct);
    }
}

public class SmtpSettings
{
    public string Host { get; set; } = "localhost";
    public int Port { get; set; } = 1025;
    public bool UseSsl { get; set; }
    public string? Username { get; set; }
    public string? Password { get; set; }
    public string DefaultFrom { get; set; } = "noreply@portal.local";
}
