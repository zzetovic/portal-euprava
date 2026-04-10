namespace Portal.Infrastructure.Identity;

public class JwtSettings
{
    public string Secret { get; set; } = default!;
    public string Issuer { get; set; } = "portal-euprava";
    public string Audience { get; set; } = "portal-euprava";
    public int AccessTokenExpirationMinutes { get; set; } = 15;
    public int RefreshTokenExpirationDays { get; set; } = 14;
}
