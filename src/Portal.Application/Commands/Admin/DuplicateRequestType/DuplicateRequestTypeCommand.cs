using System.Text.Json;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Portal.Application.DTOs.Admin;
using Portal.Application.Interfaces;
using Portal.Domain.Entities;

namespace Portal.Application.Commands.Admin.DuplicateRequestType;

public record DuplicateRequestTypeCommand(Guid TenantId, Guid UserId, Guid Id) : IRequest<RequestTypeDetailDto>;

public class DuplicateRequestTypeCommandHandler(IPortalDbContext db)
    : IRequestHandler<DuplicateRequestTypeCommand, RequestTypeDetailDto>
{
    public async Task<RequestTypeDetailDto> Handle(DuplicateRequestTypeCommand request, CancellationToken ct)
    {
        var source = await db.RequestTypes
            .Include(r => r.Fields)
            .Include(r => r.Attachments)
            .FirstOrDefaultAsync(r => r.Id == request.Id && r.TenantId == request.TenantId, ct)
            ?? throw new InvalidOperationException("REQUEST_TYPE_NOT_FOUND");

        // Generate unique code
        var baseCode = $"kopija-{source.Code}";
        var code = baseCode;
        var suffix = 1;
        while (await db.RequestTypes.AnyAsync(rt => rt.TenantId == request.TenantId && rt.Code == code, ct))
        {
            code = $"{baseCode}-{suffix++}";
        }

        // Parse name i18n to prepend "Kopija od"
        string? nameI18n = null;
        if (source.NameI18n is not null)
        {
            try
            {
                var nameDict = JsonSerializer.Deserialize<Dictionary<string, string>>(source.NameI18n);
                if (nameDict is not null)
                {
                    if (nameDict.ContainsKey("hr"))
                        nameDict["hr"] = $"Kopija od {nameDict["hr"]}";
                    if (nameDict.ContainsKey("en"))
                        nameDict["en"] = $"Copy of {nameDict["en"]}";
                    nameI18n = JsonSerializer.Serialize(nameDict);
                }
            }
            catch
            {
                nameI18n = source.NameI18n;
            }
        }

        var copy = new RequestType
        {
            Id = Guid.NewGuid(),
            TenantId = request.TenantId,
            Code = code,
            NameI18n = nameI18n,
            DescriptionI18n = source.DescriptionI18n,
            IsActive = false,
            SortOrder = source.SortOrder,
            Version = 1,
            EstimatedProcessingDays = source.EstimatedProcessingDays
        };

        foreach (var f in source.Fields)
        {
            copy.Fields.Add(new RequestTypeField
            {
                Id = Guid.NewGuid(),
                RequestTypeId = copy.Id,
                FieldKey = f.FieldKey,
                LabelI18n = f.LabelI18n,
                HelpTextI18n = f.HelpTextI18n,
                FieldType = f.FieldType,
                IsRequired = f.IsRequired,
                ValidationRules = f.ValidationRules,
                Options = f.Options,
                SortOrder = f.SortOrder
            });
        }

        foreach (var a in source.Attachments)
        {
            copy.Attachments.Add(new RequestTypeAttachment
            {
                Id = Guid.NewGuid(),
                RequestTypeId = copy.Id,
                AttachmentKey = a.AttachmentKey,
                LabelI18n = a.LabelI18n,
                DescriptionI18n = a.DescriptionI18n,
                IsRequired = a.IsRequired,
                MaxSizeBytes = a.MaxSizeBytes,
                AllowedMimeTypes = a.AllowedMimeTypes,
                SortOrder = a.SortOrder
            });
        }

        db.RequestTypes.Add(copy);

        db.AuditLogs.Add(new AuditLog
        {
            Id = Guid.NewGuid(),
            TenantId = request.TenantId,
            UserId = request.UserId,
            Action = "request_type.duplicated",
            EntityType = "request_type",
            EntityId = copy.Id,
            Before = JsonSerializer.Serialize(new { SourceId = source.Id, source.Code }),
            After = JsonSerializer.Serialize(new { copy.Code })
        });

        await db.SaveChangesAsync(ct);

        return new RequestTypeDetailDto(
            copy.Id, copy.Code, copy.NameI18n, copy.DescriptionI18n,
            copy.IsActive, copy.IsArchived, copy.SortOrder, copy.Version,
            copy.EstimatedProcessingDays, copy.CreatedAt, copy.UpdatedAt,
            copy.Fields.OrderBy(f => f.SortOrder).Select(f => new RequestTypeFieldDto(
                f.Id, f.FieldKey, f.LabelI18n, f.HelpTextI18n,
                f.FieldType.ToString(), f.IsRequired, f.ValidationRules, f.Options, f.SortOrder)).ToList(),
            copy.Attachments.OrderBy(a => a.SortOrder).Select(a => new RequestTypeAttachmentDto(
                a.Id, a.AttachmentKey, a.LabelI18n, a.DescriptionI18n,
                a.IsRequired, a.MaxSizeBytes, a.AllowedMimeTypes, a.SortOrder)).ToList());
    }
}
