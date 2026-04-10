using Portal.Domain.Common;

namespace Portal.Domain.Entities;

public class Role : TenantEntity
{
    public string Code { get; set; } = default!;
    public string Name { get; set; } = default!;

    public ICollection<UserRole> UserRoles { get; set; } = new List<UserRole>();
}
