using Portal.Domain.Common;

namespace Portal.Domain.Entities;

public class Tenant : BaseEntity
{
    public string Code { get; set; } = default!;
    public string Name { get; set; } = default!;
    public string? Oib { get; set; }
    public string? Settings { get; set; } // jsonb
    public bool IsActive { get; set; } = true;

    public ICollection<User> Users { get; set; } = new List<User>();
    public ICollection<RequestType> RequestTypes { get; set; } = new List<RequestType>();
}
