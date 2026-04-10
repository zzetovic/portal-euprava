namespace Portal.Application.DTOs.Admin;

public record RequestTypeListItemDto(
    Guid Id,
    string Code,
    string? NameI18n,
    string? DescriptionI18n,
    bool IsActive,
    bool IsArchived,
    int SortOrder,
    int Version,
    int? EstimatedProcessingDays,
    int FieldCount,
    int AttachmentCount,
    DateTime UpdatedAt);

public record RequestTypeDetailDto(
    Guid Id,
    string Code,
    string? NameI18n,
    string? DescriptionI18n,
    bool IsActive,
    bool IsArchived,
    int SortOrder,
    int Version,
    int? EstimatedProcessingDays,
    DateTime CreatedAt,
    DateTime UpdatedAt,
    List<RequestTypeFieldDto> Fields,
    List<RequestTypeAttachmentDto> Attachments);

public record RequestTypeFieldDto(
    Guid? Id,
    string FieldKey,
    string? LabelI18n,
    string? HelpTextI18n,
    string FieldType,
    bool IsRequired,
    string? ValidationRules,
    string? Options,
    int SortOrder);

public record RequestTypeAttachmentDto(
    Guid? Id,
    string AttachmentKey,
    string? LabelI18n,
    string? DescriptionI18n,
    bool IsRequired,
    long MaxSizeBytes,
    string[] AllowedMimeTypes,
    int SortOrder);

public record CreateRequestTypeRequest(
    string Code,
    string? NameI18n,
    string? DescriptionI18n,
    bool IsActive,
    int SortOrder,
    int? EstimatedProcessingDays,
    List<RequestTypeFieldDto>? Fields,
    List<RequestTypeAttachmentDto>? Attachments);

public record UpdateRequestTypeRequest(
    string Code,
    string? NameI18n,
    string? DescriptionI18n,
    bool IsActive,
    int SortOrder,
    int? EstimatedProcessingDays,
    List<RequestTypeFieldDto>? Fields,
    List<RequestTypeAttachmentDto>? Attachments);

public record RequestTypeUsageDto(
    int DraftCount,
    int SubmittedCount,
    int ReceivedCount,
    int RejectedCount,
    int TotalCount);
