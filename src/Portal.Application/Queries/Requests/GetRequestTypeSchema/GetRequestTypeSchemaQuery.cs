using MediatR;
using Microsoft.EntityFrameworkCore;
using Portal.Application.DTOs.Admin;
using Portal.Application.DTOs.Requests;
using Portal.Application.Interfaces;

namespace Portal.Application.Queries.Requests.GetRequestTypeSchema;

public record GetRequestTypeSchemaQuery(Guid TenantId, Guid Id) : IRequest<RequestTypeSchemaDto>;

public class GetRequestTypeSchemaQueryHandler(IPortalDbContext db)
    : IRequestHandler<GetRequestTypeSchemaQuery, RequestTypeSchemaDto>
{
    public async Task<RequestTypeSchemaDto> Handle(GetRequestTypeSchemaQuery request, CancellationToken ct)
    {
        var rt = await db.RequestTypes
            .Include(r => r.Fields.OrderBy(f => f.SortOrder))
            .Include(r => r.Attachments.OrderBy(a => a.SortOrder))
            .FirstOrDefaultAsync(r => r.Id == request.Id
                && r.TenantId == request.TenantId
                && r.IsActive && !r.IsArchived, ct)
            ?? throw new InvalidOperationException("REQUEST_TYPE_NOT_FOUND");

        return new RequestTypeSchemaDto(
            rt.Id, rt.Code, rt.Version,
            rt.Fields.Select(f => new RequestTypeFieldDto(
                f.Id, f.FieldKey, f.LabelI18n, f.HelpTextI18n,
                f.FieldType.ToString(), f.IsRequired, f.ValidationRules, f.Options, f.SortOrder)).ToList(),
            rt.Attachments.Select(a => new RequestTypeAttachmentDto(
                a.Id, a.AttachmentKey, a.LabelI18n, a.DescriptionI18n,
                a.IsRequired, a.MaxSizeBytes, a.AllowedMimeTypes, a.SortOrder)).ToList());
    }
}
