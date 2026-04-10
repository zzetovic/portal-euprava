using Portal.Domain.Common;

namespace Portal.Domain.Entities;

public class RequestAttachment : BaseEntity
{
    public Guid RequestId { get; set; }
    public string AttachmentKey { get; set; } = default!;
    public string OriginalFilename { get; set; } = default!;
    public string MimeType { get; set; } = default!;
    public long SizeBytes { get; set; }
    public string StorageKey { get; set; } = default!;
    public string ChecksumSha256 { get; set; } = default!;
    public DateTime UploadedAt { get; set; }
    public Guid UploadedByUserId { get; set; }

    public Request Request { get; set; } = default!;
    public User UploadedByUser { get; set; } = default!;
}
