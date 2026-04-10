using Portal.Domain.Entities;

namespace Portal.Application.Interfaces;

public interface IJwtTokenGenerator
{
    string GenerateAccessToken(User user);
    string GenerateRefreshToken();
    int RefreshTokenExpirationDays { get; }
}
