namespace Portal.Application.DTOs.Notifications;

public record NotificationDto(
    Guid Id,
    string Type,
    string? TitleI18n,
    string? BodyI18n,
    Guid? RelatedRequestId,
    bool IsRead,
    DateTime? ReadAt,
    DateTime CreatedAt);
