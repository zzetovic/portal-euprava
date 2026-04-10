using MediatR;
using Microsoft.EntityFrameworkCore;
using Portal.Application.Commands.Auth.Register;
using Portal.Application.DTOs.Auth;
using Portal.Application.Interfaces;
using Portal.Domain.Entities;

namespace Portal.Application.Commands.Auth.Login;

public record LoginCommand(
    string Email,
    string Password,
    Guid TenantId,
    string? UserAgent,
    string? Ip) : IRequest<LoginResult>;

public record LoginResult(AuthResponse Response, string RawRefreshToken);

public class LoginCommandHandler(
    IPortalDbContext db,
    IPasswordHasher passwordHasher,
    IJwtTokenGenerator jwtTokenGenerator) : IRequestHandler<LoginCommand, LoginResult>
{
    public async Task<LoginResult> Handle(LoginCommand request, CancellationToken ct)
    {
        var user = await db.Users.FirstOrDefaultAsync(
            u => u.TenantId == request.TenantId && u.Email == request.Email && u.IsActive, ct);

        if (user is null || user.PasswordHash is null || !passwordHasher.Verify(request.Password, user.PasswordHash))
            throw new InvalidOperationException("INVALID_CREDENTIALS");

        user.LastLoginAt = DateTime.UtcNow;

        var rawRefreshToken = jwtTokenGenerator.GenerateRefreshToken();
        var refreshToken = new RefreshToken
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            TokenHash = RegisterCommandHandler.HashToken(rawRefreshToken),
            ExpiresAt = DateTime.UtcNow.AddDays(jwtTokenGenerator.RefreshTokenExpirationDays),
            UserAgent = request.UserAgent,
            Ip = request.Ip
        };

        db.RefreshTokens.Add(refreshToken);
        await db.SaveChangesAsync(ct);

        var accessToken = jwtTokenGenerator.GenerateAccessToken(user);

        var response = new AuthResponse(
            AccessToken: accessToken,
            TokenType: "Bearer",
            ExpiresIn: 900,
            User: new UserProfile(
                Id: user.Id,
                Email: user.Email,
                FirstName: user.FirstName,
                LastName: user.LastName,
                Oib: user.Oib,
                Phone: user.Phone,
                UserType: user.UserType.ToString(),
                PreferredLanguage: user.PreferredLanguage,
                MustChangePassword: user.MustChangePassword,
                EmailVerified: user.EmailVerifiedAt.HasValue));

        return new LoginResult(response, rawRefreshToken);
    }
}
