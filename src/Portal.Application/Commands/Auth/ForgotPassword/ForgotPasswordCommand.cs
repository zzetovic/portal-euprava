using System.Security.Cryptography;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Portal.Application.Commands.Auth.Register;
using Portal.Application.Interfaces;
using Portal.Domain.Entities;

namespace Portal.Application.Commands.Auth.ForgotPassword;

public record ForgotPasswordCommand(string Email, Guid TenantId) : IRequest;

public class ForgotPasswordCommandHandler(
    IPortalDbContext db,
    IEmailSender emailSender) : IRequestHandler<ForgotPasswordCommand>
{
    public async Task Handle(ForgotPasswordCommand request, CancellationToken ct)
    {
        var user = await db.Users.FirstOrDefaultAsync(
            u => u.TenantId == request.TenantId && u.Email == request.Email && u.IsActive, ct);

        // Always return success to prevent email enumeration
        if (user is null)
            return;

        var rawToken = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32))
            .Replace("+", "-").Replace("/", "_").TrimEnd('=');

        var resetToken = new PasswordResetToken
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            TokenHash = RegisterCommandHandler.HashToken(rawToken),
            ExpiresAt = DateTime.UtcNow.AddHours(1)
        };

        db.PasswordResetTokens.Add(resetToken);
        await db.SaveChangesAsync(ct);

        await emailSender.SendAsync(new EmailMessage(
            To: user.Email,
            Subject: "Zahtjev za promjenu lozinke",
            HtmlBody: $"<p>Kliknite na poveznicu za promjenu lozinke:</p><p><a href=\"{{BASE_URL}}/reset-password?token={rawToken}\">Promijeni lozinku</a></p><p>Poveznica vrijedi 1 sat.</p>",
            PlainTextBody: $"Token za promjenu lozinke: {rawToken} (vrijedi 1 sat)"),
            ct);
    }
}
