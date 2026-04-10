namespace Portal.Application.DTOs.Auth;

public record AuthResponse(
    string AccessToken,
    string TokenType,
    int ExpiresIn,
    UserProfile User);

public record UserProfile(
    Guid Id,
    string Email,
    string FirstName,
    string LastName,
    string? Oib,
    string? Phone,
    string UserType,
    string PreferredLanguage,
    bool MustChangePassword,
    bool EmailVerified);
