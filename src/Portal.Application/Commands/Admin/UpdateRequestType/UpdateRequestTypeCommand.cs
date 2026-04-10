using System.Text.Json;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Portal.Application.DTOs.Admin;
using Portal.Application.Interfaces;
using Portal.Domain.Entities;
using Portal.Domain.Enums;

namespace Portal.Application.Commands.Admin.UpdateRequestType;

public record UpdateRequestTypeCommand(
    Guid TenantId,
    Guid UserId,
    Guid Id,
    string Code,
    string? NameI18n,
    string? DescriptionI18n,
    bool IsActive,
    int SortOrder,
    int? EstimatedProcessingDays,
    List<RequestTypeFieldDto>? Fields,
    List<RequestTypeAttachmentDto>? Attachments) : IRequest<RequestTypeDetailDto>;

public class UpdateRequestTypeCommandHandler(IPortalDbContext db)
    : IRequestHandler<UpdateRequestTypeCommand, RequestTypeDetailDto>
{
    public async Task<RequestTypeDetailDto> Handle(UpdateRequestTypeCommand request, CancellationToken ct)
    {
        var rt = await db.RequestTypes
            .Include(r => r.Fields)
            .Include(r => r.Attachments)
            .FirstOrDefaultAsync(r => r.Id == request.Id && r.TenantId == request.TenantId, ct)
            ?? throw new InvalidOperationException("REQUEST_TYPE_NOT_FOUND");

        if (rt.IsArchived)
            throw new InvalidOperationException("REQUEST_TYPE_ARCHIVED");

        var beforeJson = JsonSerializer.Serialize(new { rt.Code, rt.NameI18n, rt.Version, rt.IsActive });

        var needsVersionBump = false;
        var hasSubmittedRequests = await db.Requests.AnyAsync(
            r => r.RequestTypeId == request.Id && r.TenantId == request.TenantId
                && r.Status != RequestStatus.Draft, ct);

        if (hasSubmittedRequests && request.Fields is not null)
            needsVersionBump = DetectStructuralChange(rt, request.Fields, request.Attachments);

        // Update basic properties
        rt.Code = request.Code;
        rt.NameI18n = request.NameI18n;
        rt.DescriptionI18n = request.DescriptionI18n;
        rt.IsActive = request.IsActive;
        rt.SortOrder = request.SortOrder;
        rt.EstimatedProcessingDays = request.EstimatedProcessingDays;

        if (needsVersionBump)
        {
            rt.Version++;

            // Lock existing drafts to old version with 30-day expiry
            var drafts = await db.Requests
                .Where(r => r.RequestTypeId == rt.Id && r.Status == RequestStatus.Draft
                    && !r.IsLockedToOldVersion)
                .ToListAsync(ct);

            foreach (var draft in drafts)
            {
                draft.IsLockedToOldVersion = true;
                draft.ExpiresAt = DateTime.UtcNow.AddDays(30);
            }

            db.AuditLogs.Add(new AuditLog
            {
                Id = Guid.NewGuid(),
                TenantId = request.TenantId,
                UserId = request.UserId,
                Action = "request_type.version_bumped",
                EntityType = "request_type",
                EntityId = rt.Id,
                After = JsonSerializer.Serialize(new { rt.Version, LockedDrafts = drafts.Count })
            });
        }

        // Replace fields
        if (request.Fields is not null)
        {
            foreach (var existing in rt.Fields.ToList())
                db.RequestTypeFields.Remove(existing);

            foreach (var f in request.Fields)
            {
                rt.Fields.Add(new RequestTypeField
                {
                    Id = Guid.NewGuid(),
                    RequestTypeId = rt.Id,
                    FieldKey = f.FieldKey,
                    LabelI18n = f.LabelI18n,
                    HelpTextI18n = f.HelpTextI18n,
                    FieldType = Enum.Parse<FieldType>(f.FieldType),
                    IsRequired = f.IsRequired,
                    ValidationRules = f.ValidationRules,
                    Options = f.Options,
                    SortOrder = f.SortOrder
                });
            }
        }

        // Replace attachments
        if (request.Attachments is not null)
        {
            foreach (var existing in rt.Attachments.ToList())
                db.RequestTypeAttachments.Remove(existing);

            foreach (var a in request.Attachments)
            {
                rt.Attachments.Add(new RequestTypeAttachment
                {
                    Id = Guid.NewGuid(),
                    RequestTypeId = rt.Id,
                    AttachmentKey = a.AttachmentKey,
                    LabelI18n = a.LabelI18n,
                    DescriptionI18n = a.DescriptionI18n,
                    IsRequired = a.IsRequired,
                    MaxSizeBytes = a.MaxSizeBytes,
                    AllowedMimeTypes = a.AllowedMimeTypes,
                    SortOrder = a.SortOrder
                });
            }
        }

        db.AuditLogs.Add(new AuditLog
        {
            Id = Guid.NewGuid(),
            TenantId = request.TenantId,
            UserId = request.UserId,
            Action = "request_type.updated",
            EntityType = "request_type",
            EntityId = rt.Id,
            Before = beforeJson,
            After = JsonSerializer.Serialize(new { rt.Code, rt.NameI18n, rt.Version, rt.IsActive })
        });

        await db.SaveChangesAsync(ct);

        return new RequestTypeDetailDto(
            rt.Id, rt.Code, rt.NameI18n, rt.DescriptionI18n,
            rt.IsActive, rt.IsArchived, rt.SortOrder, rt.Version,
            rt.EstimatedProcessingDays, rt.CreatedAt, rt.UpdatedAt,
            rt.Fields.OrderBy(f => f.SortOrder).Select(f => new RequestTypeFieldDto(
                f.Id, f.FieldKey, f.LabelI18n, f.HelpTextI18n,
                f.FieldType.ToString(), f.IsRequired, f.ValidationRules, f.Options, f.SortOrder)).ToList(),
            rt.Attachments.OrderBy(a => a.SortOrder).Select(a => new RequestTypeAttachmentDto(
                a.Id, a.AttachmentKey, a.LabelI18n, a.DescriptionI18n,
                a.IsRequired, a.MaxSizeBytes, a.AllowedMimeTypes, a.SortOrder)).ToList());
    }

    private static bool DetectStructuralChange(
        RequestType existing,
        List<RequestTypeFieldDto> newFields,
        List<RequestTypeAttachmentDto>? newAttachments)
    {
        var existingFieldKeys = existing.Fields.ToDictionary(f => f.FieldKey);
        var newFieldKeys = newFields.ToDictionary(f => f.FieldKey);

        // Added or removed fields
        if (existingFieldKeys.Keys.ToHashSet().SetEquals(newFieldKeys.Keys) == false)
            return true;

        // Check each field for structural changes
        foreach (var (key, existingField) in existingFieldKeys)
        {
            if (!newFieldKeys.TryGetValue(key, out var newField))
                return true; // removed

            if (existingField.FieldType.ToString() != newField.FieldType)
                return true;

            if (!existingField.IsRequired && newField.IsRequired)
                return true; // false→true is structural
        }

        // Attachment changes
        if (newAttachments is not null)
        {
            var existingAttKeys = existing.Attachments.ToDictionary(a => a.AttachmentKey);
            var newAttKeys = newAttachments.ToDictionary(a => a.AttachmentKey);

            if (!existingAttKeys.Keys.ToHashSet().SetEquals(newAttKeys.Keys))
                return true;

            foreach (var (key, existingAtt) in existingAttKeys)
            {
                if (!newAttKeys.TryGetValue(key, out var newAtt))
                    return true;

                // Shrunk max size
                if (newAtt.MaxSizeBytes < existingAtt.MaxSizeBytes)
                    return true;

                // Narrowed MIME types
                var existingMimes = existingAtt.AllowedMimeTypes.ToHashSet();
                var newMimes = newAtt.AllowedMimeTypes.ToHashSet();
                if (!newMimes.IsSupersetOf(existingMimes))
                    return true;
            }
        }

        return false;
    }
}
