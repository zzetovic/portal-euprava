using MediatR;
using Microsoft.EntityFrameworkCore;
using Portal.Application.DTOs.Office;
using Portal.Application.DTOs.Requests;
using Portal.Application.Interfaces;
using Portal.Domain.Entities;
using Portal.Domain.Enums;

namespace Portal.Application.Queries.Office.GetOfficerRequestDetail;

public record GetOfficerRequestDetailQuery(
    Guid TenantId,
    Guid OfficerId,
    Guid RequestId) : IRequest<OfficerRequestDetailDto>;

public class GetOfficerRequestDetailQueryHandler(IPortalDbContext db)
    : IRequestHandler<GetOfficerRequestDetailQuery, OfficerRequestDetailDto>
{
    public async Task<OfficerRequestDetailDto> Handle(GetOfficerRequestDetailQuery request, CancellationToken ct)
    {
        var r = await db.Requests
            .Include(r => r.RequestType)
            .Include(r => r.Citizen)
            .Include(r => r.Attachments.OrderBy(a => a.UploadedAt))
            .Include(r => r.StatusHistory.OrderBy(h => h.ChangedAt))
            .Include(r => r.AktMapping)
            .FirstOrDefaultAsync(r => r.Id == request.RequestId && r.TenantId == request.TenantId, ct)
            ?? throw new InvalidOperationException("REQUEST_NOT_FOUND");

        // Auto-mark viewed on first access
        if (r.ViewedFirstAt is null)
        {
            r.ViewedFirstAt = DateTime.UtcNow;
            r.ViewedFirstByUserId = request.OfficerId;

            db.AuditLogs.Add(new AuditLog
            {
                Id = Guid.NewGuid(),
                TenantId = request.TenantId,
                UserId = request.OfficerId,
                Action = "request.viewed",
                EntityType = "request",
                EntityId = r.Id
            });

            await db.SaveChangesAsync(ct);
        }

        return new OfficerRequestDetailDto(
            r.Id, r.ReferenceNumber, r.Status.ToString(),
            r.FormData, r.FormSchemaSnapshot,
            r.RequestType?.NameI18n, r.RequestType?.Code, r.RequestTypeVersion,
            r.CreatedAt, r.SubmittedAt, r.ViewedFirstAt, r.ReviewedAt,
            r.RejectionReasonCode, r.RejectionInternalNote,
            r.AktMapping?.AktId,
            new CitizenInfoDto(r.Citizen.FirstName, r.Citizen.LastName, r.Citizen.Email, r.Citizen.Oib, r.Citizen.Phone),
            r.Attachments.Select(a => new RequestAttachmentDto(
                a.Id, a.AttachmentKey, a.OriginalFilename, a.MimeType, a.SizeBytes, a.UploadedAt)).ToList(),
            r.StatusHistory.Select(h => new RequestStatusHistoryDto(
                h.FromStatus?.ToString(), h.ToStatus.ToString(),
                h.ChangedBySource.ToString(), h.Comment, h.ChangedAt)).ToList());
    }
}
