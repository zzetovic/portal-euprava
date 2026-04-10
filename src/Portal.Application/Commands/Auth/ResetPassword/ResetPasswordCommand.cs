using MediatR;
using Microsoft.EntityFrameworkCore;
using Portal.Application.Commands.Auth.Register;
using Portal.Application.Interfaces;

namespace Portal.Application.Commands.Auth.ResetPassword;

public record ResetPasswordCommand(string Token, string NewPassword) : IRequest;

public class ResetPasswordCommandHandler(
    IPortalDbContext db,
    IPasswordHasher passwordHasher) : IRequestHandler<ResetPasswordCommand>
{
    public async Task Handle(ResetPasswordCommand request, CancellationToken ct)
    {
        var tokenHash = RegisterCommandHandler.HashToken(request.Token);

        var token = await db.PasswordResetTokens
            .Include(t => t.User)
            .FirstOrDefaultAsync(t => t.TokenHash == tokenHash && t.UsedAt == null, ct);

        if (token is null)
            throw new InvalidOperationException("INVALID_TOKEN");

        if (token.ExpiresAt < DateTime.UtcNow)
            throw new InvalidOperationException("TOKEN_EXPIRED");

        token.UsedAt = DateTime.UtcNow;
        token.User.PasswordHash = passwordHasher.Hash(request.NewPassword);
        token.User.MustChangePassword = false;

        await db.SaveChangesAsync(ct);
    }
}
