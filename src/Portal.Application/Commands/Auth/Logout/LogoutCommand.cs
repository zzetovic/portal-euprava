using MediatR;
using Microsoft.EntityFrameworkCore;
using Portal.Application.Commands.Auth.Register;
using Portal.Application.Interfaces;

namespace Portal.Application.Commands.Auth.Logout;

public record LogoutCommand(string RefreshToken) : IRequest;

public class LogoutCommandHandler(IPortalDbContext db) : IRequestHandler<LogoutCommand>
{
    public async Task Handle(LogoutCommand request, CancellationToken ct)
    {
        var tokenHash = RegisterCommandHandler.HashToken(request.RefreshToken);

        var token = await db.RefreshTokens
            .FirstOrDefaultAsync(t => t.TokenHash == tokenHash && t.RevokedAt == null, ct);

        if (token is not null)
        {
            token.RevokedAt = DateTime.UtcNow;
            await db.SaveChangesAsync(ct);
        }
    }
}
