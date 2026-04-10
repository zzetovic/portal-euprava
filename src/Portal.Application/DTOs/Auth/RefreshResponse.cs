namespace Portal.Application.DTOs.Auth;

public record RefreshResponse(
    string AccessToken,
    string TokenType,
    int ExpiresIn);
