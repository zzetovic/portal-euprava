using System.Text.Json;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Portal.Application.DTOs.Requests;
using Portal.Application.Interfaces;

namespace Portal.Application.Queries.Requests.GetRequestTypePreflight;

public record GetRequestTypePreflightQuery(Guid TenantId, string Code) : IRequest<RequestTypePreflightDto>;

public class GetRequestTypePreflightQueryHandler(IPortalDbContext db)
    : IRequestHandler<GetRequestTypePreflightQuery, RequestTypePreflightDto>
{
    public async Task<RequestTypePreflightDto> Handle(GetRequestTypePreflightQuery request, CancellationToken ct)
    {
        var rt = await db.RequestTypes
            .Include(r => r.Fields.OrderBy(f => f.SortOrder))
            .Include(r => r.Attachments.OrderBy(a => a.SortOrder))
            .Include(r => r.Tenant)
            .FirstOrDefaultAsync(r => r.Code == request.Code
                && r.TenantId == request.TenantId
                && r.IsActive && !r.IsArchived, ct)
            ?? throw new InvalidOperationException("REQUEST_TYPE_NOT_FOUND");

        var defaultDays = 5;
        if (rt.Tenant?.Settings is not null)
        {
            try
            {
                var settings = JsonSerializer.Deserialize<JsonElement>(rt.Tenant.Settings);
                if (settings.TryGetProperty("default_processing_days", out var dpd) && dpd.TryGetInt32(out var val))
                    defaultDays = val;
            }
            catch { }
        }

        var estimatedDays = rt.EstimatedProcessingDays ?? defaultDays;

        return new RequestTypePreflightDto(
            rt.Id, rt.Code, rt.NameI18n, rt.DescriptionI18n,
            estimatedDays,
            rt.Fields.Where(f => f.IsRequired).Select(f => new PreflightFieldDto(f.LabelI18n)).ToList(),
            rt.Fields.Where(f => !f.IsRequired).Select(f => new PreflightFieldDto(f.LabelI18n)).ToList(),
            rt.Attachments.Where(a => a.IsRequired).Select(a =>
                new PreflightAttachmentDto(a.LabelI18n, a.DescriptionI18n, a.AllowedMimeTypes, a.MaxSizeBytes)).ToList(),
            rt.Attachments.Where(a => !a.IsRequired).Select(a =>
                new PreflightAttachmentDto(a.LabelI18n, a.DescriptionI18n, a.AllowedMimeTypes, a.MaxSizeBytes)).ToList());
    }
}
