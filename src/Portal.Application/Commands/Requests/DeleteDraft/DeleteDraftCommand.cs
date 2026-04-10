using MediatR;
using Microsoft.EntityFrameworkCore;
using Portal.Application.Interfaces;
using Portal.Domain.Enums;

namespace Portal.Application.Commands.Requests.DeleteDraft;

public record DeleteDraftCommand(Guid TenantId, Guid CitizenId, Guid RequestId) : IRequest;

public class DeleteDraftCommandHandler(
    IPortalDbContext db,
    IAttachmentStorage attachmentStorage) : IRequestHandler<DeleteDraftCommand>
{
    public async Task Handle(DeleteDraftCommand request, CancellationToken ct)
    {
        var r = await db.Requests
            .Include(r => r.Attachments)
            .Include(r => r.StatusHistory)
            .FirstOrDefaultAsync(r => r.Id == request.RequestId
                && r.TenantId == request.TenantId
                && r.CitizenId == request.CitizenId, ct)
            ?? throw new InvalidOperationException("REQUEST_NOT_FOUND");

        if (r.Status != RequestStatus.Draft)
            throw new InvalidOperationException("REQUEST_NOT_DRAFT");

        // Collect storage keys for background deletion
        var storageKeys = r.Attachments.Select(a => a.StorageKey).ToList();

        // Remove from DB
        db.RequestStatusHistories.RemoveRange(r.StatusHistory);
        db.RequestAttachments.RemoveRange(r.Attachments);
        db.Requests.Remove(r);
        await db.SaveChangesAsync(ct);

        // Background delete files from storage
        _ = Task.Run(async () =>
        {
            foreach (var key in storageKeys)
            {
                try { await attachmentStorage.DeleteAsync(key, CancellationToken.None); }
                catch { /* log warning in production */ }
            }
        });
    }
}
