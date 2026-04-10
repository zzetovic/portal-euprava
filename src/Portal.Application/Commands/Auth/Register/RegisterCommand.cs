using System.Security.Cryptography;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Portal.Application.DTOs.Auth;
using Portal.Application.Interfaces;
using Portal.Domain.Entities;
using Portal.Domain.Enums;

namespace Portal.Application.Commands.Auth.Register;

public record RegisterCommand(
    string Email,
    string Password,
    string FirstName,
    string LastName,
    string? Oib,
    string? Phone,
    Guid TenantId) : IRequest<AuthResponse>;

public class RegisterCommandHandler(
    IPortalDbContext db,
    IPasswordHasher passwordHasher,
    IJwtTokenGenerator jwtTokenGenerator,
    IEmailSender emailSender) : IRequestHandler<RegisterCommand, AuthResponse>
{
    public async Task<AuthResponse> Handle(RegisterCommand request, CancellationToken ct)
    {
        var exists = await db.Users.AnyAsync(
            u => u.TenantId == request.TenantId && u.Email == request.Email, ct);

        if (exists)
            throw new InvalidOperationException("USER_ALREADY_EXISTS");

        var user = new User
        {
            Id = Guid.NewGuid(),
            TenantId = request.TenantId,
            Email = request.Email,
            PasswordHash = passwordHasher.Hash(request.Password),
            FirstName = request.FirstName,
            LastName = request.LastName,
            Oib = request.Oib,
            Phone = request.Phone,
            UserType = UserType.Citizen,
            IsActive = true,
            PreferredLanguage = "hr"
        };

        db.Users.Add(user);

        var rawToken = GenerateSecureToken();
        var verificationToken = new EmailVerificationToken
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            TokenHash = HashToken(rawToken),
            ExpiresAt = DateTime.UtcNow.AddHours(24)
        };

        db.EmailVerificationTokens.Add(verificationToken);
        await db.SaveChangesAsync(ct);

        _ = Task.Run(async () =>
        {
            try
            {
                await emailSender.SendAsync(new EmailMessage(
                    To: user.Email,
                    Subject: "Verificirajte svoju email adresu",
                    HtmlBody: $"<p>Dobrodošli! Kliknite na poveznicu za verificiranje email adrese:</p><p><a href=\"{{BASE_URL}}/verify-email?token={rawToken}\">Verificiraj email</a></p>",
                    PlainTextBody: $"Dobrodošli! Token za verificiranje: {rawToken}"),
                CancellationToken.None);
            }
            catch
            {
                // Email delivery failure is non-blocking; user can request resend
            }
        });

        var accessToken = jwtTokenGenerator.GenerateAccessToken(user);

        return new AuthResponse(
            AccessToken: accessToken,
            TokenType: "Bearer",
            ExpiresIn: 900,
            User: MapUserProfile(user));
    }

    private static string GenerateSecureToken()
    {
        var bytes = RandomNumberGenerator.GetBytes(32);
        return Convert.ToBase64String(bytes).Replace("+", "-").Replace("/", "_").TrimEnd('=');
    }

    internal static string HashToken(string token)
    {
        var bytes = System.Text.Encoding.UTF8.GetBytes(token);
        var hash = SHA256.HashData(bytes);
        return Convert.ToHexStringLower(hash);
    }

    private static UserProfile MapUserProfile(User user) => new(
        Id: user.Id,
        Email: user.Email,
        FirstName: user.FirstName,
        LastName: user.LastName,
        Oib: user.Oib,
        Phone: user.Phone,
        UserType: user.UserType.ToString(),
        PreferredLanguage: user.PreferredLanguage,
        MustChangePassword: user.MustChangePassword,
        EmailVerified: user.EmailVerifiedAt.HasValue);
}
