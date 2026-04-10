using System.Security.Cryptography;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Portal.Application.DTOs.Requests;
using Portal.Application.Interfaces;
using Portal.Domain.Entities;
using Portal.Domain.Enums;

namespace Portal.Application.Commands.Requests.UploadAttachment;

public record UploadAttachmentCommand(
    Guid TenantId,
    Guid CitizenId,
    Guid RequestId,
    string AttachmentKey,
    string OriginalFilename,
    string MimeType,
    long SizeBytes,
    Stream Content) : IRequest<RequestAttachmentDto>;

public class UploadAttachmentCommandHandler(
    IPortalDbContext db,
    IAttachmentStorage attachmentStorage) : IRequestHandler<UploadAttachmentCommand, RequestAttachmentDto>
{
    public async Task<RequestAttachmentDto> Handle(UploadAttachmentCommand request, CancellationToken ct)
    {
        var r = await db.Requests
            .FirstOrDefaultAsync(r => r.Id == request.RequestId
                && r.TenantId == request.TenantId
                && r.CitizenId == request.CitizenId
                && r.Status == RequestStatus.Draft, ct)
            ?? throw new InvalidOperationException("REQUEST_NOT_FOUND_OR_NOT_DRAFT");

        // Save file to storage
        var storageKey = await attachmentStorage.SaveAsync(request.Content, request.OriginalFilename, ct);

        // Compute checksum
        request.Content.Position = 0;
        var hashBytes = await SHA256.HashDataAsync(request.Content, ct);
        var checksum = Convert.ToHexStringLower(hashBytes);

        var attachment = new RequestAttachment
        {
            Id = Guid.NewGuid(),
            RequestId = request.RequestId,
            AttachmentKey = request.AttachmentKey,
            OriginalFilename = request.OriginalFilename,
            MimeType = request.MimeType,
            SizeBytes = request.SizeBytes,
            StorageKey = storageKey,
            ChecksumSha256 = checksum,
            UploadedAt = DateTime.UtcNow,
            UploadedByUserId = request.CitizenId
        };

        db.RequestAttachments.Add(attachment);
        await db.SaveChangesAsync(ct);

        return new RequestAttachmentDto(
            attachment.Id, attachment.AttachmentKey, attachment.OriginalFilename,
            attachment.MimeType, attachment.SizeBytes, attachment.UploadedAt);
    }
}
