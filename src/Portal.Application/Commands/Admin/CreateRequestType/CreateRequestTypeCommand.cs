using MediatR;
using Microsoft.EntityFrameworkCore;
using Portal.Application.DTOs.Admin;
using Portal.Application.Interfaces;
using Portal.Domain.Entities;
using Portal.Domain.Enums;

namespace Portal.Application.Commands.Admin.CreateRequestType;

public record CreateRequestTypeCommand(
    Guid TenantId,
    Guid UserId,
    string Code,
    string? NameI18n,
    string? DescriptionI18n,
    bool IsActive,
    int SortOrder,
    int? EstimatedProcessingDays,
    List<RequestTypeFieldDto>? Fields,
    List<RequestTypeAttachmentDto>? Attachments) : IRequest<RequestTypeDetailDto>;

public class CreateRequestTypeCommandHandler(IPortalDbContext db)
    : IRequestHandler<CreateRequestTypeCommand, RequestTypeDetailDto>
{
    public async Task<RequestTypeDetailDto> Handle(CreateRequestTypeCommand request, CancellationToken ct)
    {
        var codeExists = await db.RequestTypes.AnyAsync(
            rt => rt.TenantId == request.TenantId && rt.Code == request.Code, ct);
        if (codeExists)
            throw new InvalidOperationException("REQUEST_TYPE_CODE_EXISTS");

        var rt = new RequestType
        {
            Id = Guid.NewGuid(),
            TenantId = request.TenantId,
            Code = request.Code,
            NameI18n = request.NameI18n,
            DescriptionI18n = request.DescriptionI18n,
            IsActive = request.IsActive,
            SortOrder = request.SortOrder,
            Version = 1,
            EstimatedProcessingDays = request.EstimatedProcessingDays
        };

        if (request.Fields is not null)
        {
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

        if (request.Attachments is not null)
        {
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

        db.RequestTypes.Add(rt);

        db.AuditLogs.Add(new AuditLog
        {
            Id = Guid.NewGuid(),
            TenantId = request.TenantId,
            UserId = request.UserId,
            Action = "request_type.created",
            EntityType = "request_type",
            EntityId = rt.Id,
            After = System.Text.Json.JsonSerializer.Serialize(new { rt.Code, rt.NameI18n, rt.Version })
        });

        await db.SaveChangesAsync(ct);

        return new RequestTypeDetailDto(
            rt.Id, rt.Code, rt.NameI18n, rt.DescriptionI18n,
            rt.IsActive, rt.IsArchived, rt.SortOrder, rt.Version,
            rt.EstimatedProcessingDays, rt.CreatedAt, rt.UpdatedAt,
            rt.Fields.Select(f => new RequestTypeFieldDto(
                f.Id, f.FieldKey, f.LabelI18n, f.HelpTextI18n,
                f.FieldType.ToString(), f.IsRequired, f.ValidationRules, f.Options, f.SortOrder)).ToList(),
            rt.Attachments.Select(a => new RequestTypeAttachmentDto(
                a.Id, a.AttachmentKey, a.LabelI18n, a.DescriptionI18n,
                a.IsRequired, a.MaxSizeBytes, a.AllowedMimeTypes, a.SortOrder)).ToList());
    }
}
