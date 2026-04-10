namespace Portal.Application.Interfaces;

public interface ILocalDbAktWriter
{
    Task<AktWriteResult> WriteAktAsync(
        WriteAktCommand cmd,
        CancellationToken ct);
}

public record WriteAktCommand(
    Guid TenantId,
    Guid RequestId,
    string IdempotencyKey,
    string CitizenOib,
    string CitizenFullName,
    string CitizenAddress,
    string CitizenEmail,
    string Subject,
    string BodyText,
    DateTimeOffset ReceivedAt,
    IReadOnlyCollection<AktAttachmentInput> Attachments);

public record AktAttachmentInput(
    string OriginalFilename,
    string MimeType,
    long SizeBytes,
    string PortalStorageKey);

public record AktWriteResult(
    bool Success,
    long? AktId,
    string? ErrorCode,
    string? ErrorMessage,
    bool IsDuplicate);
