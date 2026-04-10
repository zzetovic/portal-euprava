using Portal.Domain.Enums;

namespace Portal.Application.Interfaces;

public interface ICurrentUserService
{
    Guid? UserId { get; }
    Guid? TenantId { get; }
    string? Email { get; }
    UserType? UserType { get; }
    bool IsAuthenticated { get; }
}
