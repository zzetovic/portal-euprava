using MediatR;
using Microsoft.EntityFrameworkCore;
using Portal.Application.Commands.Auth.Register;
using Portal.Application.DTOs.Auth;
using Portal.Application.Interfaces;
using Portal.Domain.Entities;

namespace Portal.Application.Commands.Auth.Refresh;

public record RefreshCommand(
    string RefreshToken,
    string? UserAgent,
    string? Ip) : IRequest<RefreshResult>;

public record RefreshResult(RefreshResponse Response, string RawRefreshToken);

public class RefreshCommandHandler(
    IPortalDbContext db,
    IJwtTokenGenerator jwtTokenGenerator) : IRequestHandler<RefreshCommand, RefreshResult>
{
    public async Task<RefreshResult> Handle(RefreshCommand request, CancellationToken ct)
    {
        var tokenHash = RegisterCommandHandler.HashToken(request.RefreshToken);

        var existing = await db.RefreshTokens
            .Include(t => t.User)
            .FirstOrDefaultAsync(t => t.TokenHash == tokenHash && t.RevokedAt == null, ct);

        if (existing is null || existing.ExpiresAt < DateTime.UtcNow)
            throw new InvalidOperationException("INVALID_REFRESH_TOKEN");

        if (!existing.User.IsActive)
            throw new InvalidOperationException("USER_INACTIVE");

        // Revoke old token
        existing.RevokedAt = DateTime.UtcNow;

        // Issue new refresh token (rotation)
        var rawRefreshToken = jwtTokenGenerator.GenerateRefreshToken();
        var newRefreshToken = new RefreshToken
        {
            Id = Guid.NewGuid(),
            UserId = existing.UserId,
            TokenHash = RegisterCommandHandler.HashToken(rawRefreshToken),
            ExpiresAt = DateTime.UtcNow.AddDays(jwtTokenGenerator.RefreshTokenExpirationDays),
            UserAgent = request.UserAgent,
            Ip = request.Ip
        };

        db.RefreshTokens.Add(newRefreshToken);
        await db.SaveChangesAsync(ct);

        var accessToken = jwtTokenGenerator.GenerateAccessToken(existing.User);

        var response = new RefreshResponse(
            AccessToken: accessToken,
            TokenType: "Bearer",
            ExpiresIn: 900);

        return new RefreshResult(response, rawRefreshToken);
    }
}
