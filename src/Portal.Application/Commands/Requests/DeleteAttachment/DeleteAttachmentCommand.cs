using MediatR;
using Microsoft.EntityFrameworkCore;
using Portal.Application.Interfaces;
using Portal.Domain.Enums;

namespace Portal.Application.Commands.Requests.DeleteAttachment;

public record DeleteAttachmentCommand(
    Guid TenantId,
    Guid CitizenId,
    Guid RequestId,
    Guid AttachmentId) : IRequest;

public class DeleteAttachmentCommandHandler(
    IPortalDbContext db,
    IAttachmentStorage attachmentStorage) : IRequestHandler<DeleteAttachmentCommand>
{
    public async Task Handle(DeleteAttachmentCommand request, CancellationToken ct)
    {
        var r = await db.Requests
            .FirstOrDefaultAsync(r => r.Id == request.RequestId
                && r.TenantId == request.TenantId
                && r.CitizenId == request.CitizenId, ct)
            ?? throw new InvalidOperationException("REQUEST_NOT_FOUND");

        if (r.Status != RequestStatus.Draft)
            throw new InvalidOperationException("REQUEST_NOT_DRAFT");

        var attachment = await db.RequestAttachments
            .FirstOrDefaultAsync(a => a.Id == request.AttachmentId && a.RequestId == request.RequestId, ct)
            ?? throw new InvalidOperationException("ATTACHMENT_NOT_FOUND");

        var storageKey = attachment.StorageKey;
        db.RequestAttachments.Remove(attachment);
        await db.SaveChangesAsync(ct);

        try { await attachmentStorage.DeleteAsync(storageKey, ct); }
        catch { /* log warning */ }
    }
}
