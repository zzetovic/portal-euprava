using MediatR;
using Microsoft.EntityFrameworkCore;
using Portal.Application.DTOs.Requests;
using Portal.Application.Interfaces;
using Portal.Domain.Enums;

namespace Portal.Application.Queries.Requests.GetRequestDetail;

public record GetRequestDetailQuery(Guid TenantId, Guid CitizenId, Guid RequestId) : IRequest<CitizenRequestDetailDto>;

public class GetRequestDetailQueryHandler(IPortalDbContext db)
    : IRequestHandler<GetRequestDetailQuery, CitizenRequestDetailDto>
{
    public async Task<CitizenRequestDetailDto> Handle(GetRequestDetailQuery request, CancellationToken ct)
    {
        var r = await db.Requests
            .Include(r => r.RequestType)
            .Include(r => r.Attachments.OrderBy(a => a.UploadedAt))
            .Include(r => r.StatusHistory.OrderBy(h => h.ChangedAt))
            .Include(r => r.AktMapping)
            .FirstOrDefaultAsync(r => r.Id == request.RequestId
                && r.TenantId == request.TenantId
                && r.CitizenId == request.CitizenId, ct)
            ?? throw new InvalidOperationException("REQUEST_NOT_FOUND");

        var citizenStatus = r.Status switch
        {
            RequestStatus.ProcessingRegistry => "submitted",
            RequestStatus.Draft => "draft",
            RequestStatus.Submitted => "submitted",
            RequestStatus.ReceivedInRegistry => "received_in_registry",
            RequestStatus.RejectedByOfficer => "rejected_by_officer",
            _ => r.Status.ToString()
        };

        return new CitizenRequestDetailDto(
            r.Id, r.ReferenceNumber, citizenStatus,
            r.FormData, r.FormSchemaSnapshot,
            r.RequestType?.NameI18n, r.RequestType?.Code, r.RequestTypeVersion,
            r.CreatedAt, r.SubmittedAt, r.ExpiresAt,
            r.IsLockedToOldVersion, r.Etag,
            r.RejectionReasonCode,
            r.Status == RequestStatus.ReceivedInRegistry ? r.AktMapping?.AktId : null,
            r.Attachments.Select(a => new RequestAttachmentDto(
                a.Id, a.AttachmentKey, a.OriginalFilename, a.MimeType, a.SizeBytes, a.UploadedAt)).ToList(),
            r.StatusHistory.Select(h => new RequestStatusHistoryDto(
                h.FromStatus?.ToString(), h.ToStatus.ToString(),
                h.ChangedBySource.ToString(), h.Comment, h.ChangedAt)).ToList());
    }
}
