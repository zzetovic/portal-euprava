using System.Security.Claims;
using Portal.Application.Interfaces;
using Portal.Domain.Enums;

namespace Portal.Api.Services;

public class CurrentUserService(IHttpContextAccessor httpContextAccessor) : ICurrentUserService
{
    public Guid? UserId
    {
        get
        {
            var claim = httpContextAccessor.HttpContext?.User.FindFirstValue(ClaimTypes.NameIdentifier);
            return claim is not null ? Guid.Parse(claim) : null;
        }
    }

    public Guid? TenantId
    {
        get
        {
            var claim = httpContextAccessor.HttpContext?.User.FindFirstValue("tenant_id");
            return claim is not null ? Guid.Parse(claim) : null;
        }
    }

    public string? Email => httpContextAccessor.HttpContext?.User.FindFirstValue(ClaimTypes.Email);

    public UserType? UserType
    {
        get
        {
            var claim = httpContextAccessor.HttpContext?.User.FindFirstValue("user_type");
            return claim is not null ? Enum.Parse<UserType>(claim) : null;
        }
    }

    public bool IsAuthenticated => httpContextAccessor.HttpContext?.User.Identity?.IsAuthenticated ?? false;
}
