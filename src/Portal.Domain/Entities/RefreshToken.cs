using Portal.Domain.Common;

namespace Portal.Domain.Entities;

public class RefreshToken : BaseEntity
{
    public Guid UserId { get; set; }
    public string TokenHash { get; set; } = default!;
    public DateTime ExpiresAt { get; set; }
    public DateTime? RevokedAt { get; set; }
    public string? UserAgent { get; set; }
    public string? Ip { get; set; }

    public User User { get; set; } = default!;
}
