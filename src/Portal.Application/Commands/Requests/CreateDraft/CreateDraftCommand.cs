using System.Text.Json;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Portal.Application.DTOs.Requests;
using Portal.Application.Interfaces;
using Portal.Domain.Entities;
using Portal.Domain.Enums;

namespace Portal.Application.Commands.Requests.CreateDraft;

public record CreateDraftCommand(
    Guid TenantId,
    Guid CitizenId,
    Guid RequestTypeId) : IRequest<CitizenRequestDetailDto>;

public class CreateDraftCommandHandler(IPortalDbContext db) : IRequestHandler<CreateDraftCommand, CitizenRequestDetailDto>
{
    public async Task<CitizenRequestDetailDto> Handle(CreateDraftCommand request, CancellationToken ct)
    {
        var rt = await db.RequestTypes
            .Include(r => r.Fields.OrderBy(f => f.SortOrder))
            .Include(r => r.Attachments.OrderBy(a => a.SortOrder))
            .FirstOrDefaultAsync(r => r.Id == request.RequestTypeId
                && r.TenantId == request.TenantId
                && r.IsActive && !r.IsArchived, ct)
            ?? throw new InvalidOperationException("REQUEST_TYPE_NOT_FOUND");

        // Generate reference number
        var year = DateTime.UtcNow.Year;
        var count = await db.Requests
            .CountAsync(r => r.TenantId == request.TenantId
                && r.ReferenceNumber.StartsWith($"ZHT-{year}-"), ct);
        var referenceNumber = $"ZHT-{year}-{(count + 1):D6}";

        // Snapshot the schema
        var schemaSnapshot = JsonSerializer.Serialize(new
        {
            fields = rt.Fields.Select(f => new
            {
                f.FieldKey, f.LabelI18n, f.HelpTextI18n,
                FieldType = f.FieldType.ToString(),
                f.IsRequired, f.ValidationRules, f.Options, f.SortOrder
            }),
            attachments = rt.Attachments.Select(a => new
            {
                a.AttachmentKey, a.LabelI18n, a.DescriptionI18n,
                a.IsRequired, a.MaxSizeBytes, a.AllowedMimeTypes, a.SortOrder
            })
        });

        var req = new Request
        {
            Id = Guid.NewGuid(),
            TenantId = request.TenantId,
            CitizenId = request.CitizenId,
            RequestTypeId = rt.Id,
            RequestTypeVersion = rt.Version,
            ReferenceNumber = referenceNumber,
            Status = RequestStatus.Draft,
            FormData = "{}",
            FormSchemaSnapshot = schemaSnapshot,
            ExpiresAt = DateTime.UtcNow.AddDays(90),
            Etag = Guid.NewGuid().ToString("N")
        };

        db.Requests.Add(req);

        db.RequestStatusHistories.Add(new RequestStatusHistory
        {
            Id = Guid.NewGuid(),
            RequestId = req.Id,
            FromStatus = null,
            ToStatus = RequestStatus.Draft,
            ChangedByUserId = request.CitizenId,
            ChangedBySource = StatusChangeSource.Citizen,
            ChangedAt = DateTime.UtcNow
        });

        await db.SaveChangesAsync(ct);

        return new CitizenRequestDetailDto(
            req.Id, req.ReferenceNumber, "draft",
            req.FormData, req.FormSchemaSnapshot,
            rt.NameI18n, rt.Code, req.RequestTypeVersion,
            req.CreatedAt, req.SubmittedAt, req.ExpiresAt,
            req.IsLockedToOldVersion, req.Etag, null, null,
            [], []);
    }
}
