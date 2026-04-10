using Portal.Domain.Common;

namespace Portal.Domain.Entities;

public class DeploymentBackup : BaseEntity
{
    public DateTime TakenAt { get; set; }
    public long SizeBytes { get; set; }
    public string Location { get; set; } = default!;
    public DateTime? VerifiedAt { get; set; }
}
