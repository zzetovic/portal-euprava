using MediatR;
using Microsoft.EntityFrameworkCore;
using Portal.Application.DTOs.Admin;
using Portal.Application.Interfaces;

namespace Portal.Application.Queries.Admin.GetRequestTypeDetail;

public record GetRequestTypeDetailQuery(Guid TenantId, Guid Id) : IRequest<RequestTypeDetailDto>;

public class GetRequestTypeDetailQueryHandler(IPortalDbContext db)
    : IRequestHandler<GetRequestTypeDetailQuery, RequestTypeDetailDto>
{
    public async Task<RequestTypeDetailDto> Handle(GetRequestTypeDetailQuery request, CancellationToken ct)
    {
        var rt = await db.RequestTypes
            .Include(r => r.Fields.OrderBy(f => f.SortOrder))
            .Include(r => r.Attachments.OrderBy(a => a.SortOrder))
            .FirstOrDefaultAsync(r => r.Id == request.Id && r.TenantId == request.TenantId, ct)
            ?? throw new InvalidOperationException("REQUEST_TYPE_NOT_FOUND");

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
