using Portal.Domain.Common;
using Portal.Domain.Enums;

namespace Portal.Domain.Entities;

public class User : TenantEntity
{
    public string Email { get; set; } = default!;
    public string? PasswordHash { get; set; }
    public string? Oib { get; set; }
    public string FirstName { get; set; } = default!;
    public string LastName { get; set; } = default!;
    public string? Phone { get; set; }
    public UserType UserType { get; set; }
    public DateTime? EmailVerifiedAt { get; set; }
    public DateTime? LastLoginAt { get; set; }
    public bool IsActive { get; set; } = true;
    public bool MustChangePassword { get; set; }
    public string PreferredLanguage { get; set; } = "hr";
    public DateTime? DeletedAt { get; set; }

    public Tenant Tenant { get; set; } = default!;
    public ICollection<UserRole> UserRoles { get; set; } = new List<UserRole>();
    public ICollection<RefreshToken> RefreshTokens { get; set; } = new List<RefreshToken>();
}
