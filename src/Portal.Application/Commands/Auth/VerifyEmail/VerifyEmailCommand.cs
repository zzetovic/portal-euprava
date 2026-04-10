using MediatR;
using Microsoft.EntityFrameworkCore;
using Portal.Application.Commands.Auth.Register;
using Portal.Application.Interfaces;

namespace Portal.Application.Commands.Auth.VerifyEmail;

public record VerifyEmailCommand(string Token) : IRequest;

public class VerifyEmailCommandHandler(IPortalDbContext db) : IRequestHandler<VerifyEmailCommand>
{
    public async Task Handle(VerifyEmailCommand request, CancellationToken ct)
    {
        var tokenHash = RegisterCommandHandler.HashToken(request.Token);

        var token = await db.EmailVerificationTokens
            .Include(t => t.User)
            .FirstOrDefaultAsync(t => t.TokenHash == tokenHash && t.UsedAt == null, ct);

        if (token is null)
            throw new InvalidOperationException("INVALID_TOKEN");

        if (token.ExpiresAt < DateTime.UtcNow)
            throw new InvalidOperationException("TOKEN_EXPIRED");

        token.UsedAt = DateTime.UtcNow;
        token.User.EmailVerifiedAt = DateTime.UtcNow;

        await db.SaveChangesAsync(ct);
    }
}
