using Portal.Application.DTOs.Requests;

namespace Portal.Application.DTOs.Office;

public record InboxItemDto(
    Guid Id,
    string ReferenceNumber,
    string Status,
    string? RequestTypeName,
    string? RequestTypeCode,
    string CitizenFullName,
    string? CitizenOib,
    int AttachmentCount,
    DateTime SubmittedAt,
    DateTime? ViewedFirstAt,
    long? AktId);

public record InboxResult(
    List<InboxItemDto> Items,
    int TotalCount,
    int Page,
    int PageSize);

public record OfficerRequestDetailDto(
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
    DateTime? ViewedFirstAt,
    DateTime? ReviewedAt,
    string? RejectionReasonCode,
    string? RejectionInternalNote,
    long? AktId,
    CitizenInfoDto Citizen,
    List<RequestAttachmentDto> Attachments,
    List<RequestStatusHistoryDto> StatusHistory);

public record CitizenInfoDto(
    string FirstName,
    string LastName,
    string Email,
    string? Oib,
    string? Phone);

public record RejectRequestBody(
    string RejectionReasonCode,
    string? InternalNote);

public record RejectionReasonDto(
    string Code,
    string? LabelI18n);
