namespace Portal.Application.DTOs.Requests;

public record RequestTypeListItemForCitizenDto(
    Guid Id,
    string Code,
    string? NameI18n,
    string? DescriptionI18n,
    int SortOrder);

public record RequestTypePreflightDto(
    Guid Id,
    string Code,
    string? NameI18n,
    string? DescriptionI18n,
    int EstimatedProcessingDays,
    List<PreflightFieldDto> RequiredFields,
    List<PreflightFieldDto> OptionalFields,
    List<PreflightAttachmentDto> RequiredAttachments,
    List<PreflightAttachmentDto> OptionalAttachments);

public record PreflightFieldDto(string? LabelI18n);
public record PreflightAttachmentDto(string? LabelI18n, string? DescriptionI18n, string[] AllowedMimeTypes, long MaxSizeBytes);

public record RequestTypeSchemaDto(
    Guid Id,
    string Code,
    int Version,
    List<Admin.RequestTypeFieldDto> Fields,
    List<Admin.RequestTypeAttachmentDto> Attachments);

public record CitizenRequestListItemDto(
    Guid Id,
    string ReferenceNumber,
    string Status,
    string? RequestTypeName,
    string? RequestTypeCode,
    DateTime CreatedAt,
    DateTime? SubmittedAt,
    long? AktId);

public record CitizenRequestDetailDto(
    Guid Id,
    string ReferenceNumber,
    string Status,
    string FormData,
    string FormSchemaSnapshot,
    string? RequestTypeName,
    string? RequestTypeCode,
    int RequestTypeVersion,
    DateTime CreatedAt,
    DateTime? SubmittedAt,
    DateTime? ExpiresAt,
    bool IsLockedToOldVersion,
    string? Etag,
    string? RejectionReasonCode,
    long? AktId,
    List<RequestAttachmentDto> Attachments,
    List<RequestStatusHistoryDto> StatusHistory);

public record RequestAttachmentDto(
    Guid Id,
    string AttachmentKey,
    string OriginalFilename,
    string MimeType,
    long SizeBytes,
    DateTime UploadedAt);

public record RequestStatusHistoryDto(
    string? FromStatus,
    string ToStatus,
    string ChangedBySource,
    string? Comment,
    DateTime ChangedAt);

public record PaginatedResult<T>(
    List<T> Items,
    int TotalCount,
    int Page,
    int PageSize);
