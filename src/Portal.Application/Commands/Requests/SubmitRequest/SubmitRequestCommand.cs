using System.Text.Json;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Portal.Application.Interfaces;
using Portal.Domain.Entities;
using Portal.Domain.Enums;

namespace Portal.Application.Commands.Requests.SubmitRequest;

public record SubmitRequestCommand(
    Guid TenantId,
    Guid CitizenId,
    Guid RequestId) : IRequest<SubmitRequestResult>;

public record SubmitRequestResult(string ReferenceNumber);

public class SubmitRequestCommandHandler(
    IPortalDbContext db,
    IEmailSender emailSender) : IRequestHandler<SubmitRequestCommand, SubmitRequestResult>
{
    public async Task<SubmitRequestResult> Handle(SubmitRequestCommand request, CancellationToken ct)
    {
        var r = await db.Requests
            .Include(r => r.Attachments)
            .Include(r => r.RequestType)
            .FirstOrDefaultAsync(r => r.Id == request.RequestId
                && r.TenantId == request.TenantId
                && r.CitizenId == request.CitizenId, ct)
            ?? throw new InvalidOperationException("REQUEST_NOT_FOUND");

        if (r.Status != RequestStatus.Draft)
            throw new InvalidOperationException("REQUEST_NOT_DRAFT");

        // Check email verified
        var citizen = await db.Users.FirstOrDefaultAsync(u => u.Id == request.CitizenId, ct)
            ?? throw new InvalidOperationException("USER_NOT_FOUND");

        if (citizen.EmailVerifiedAt is null)
            throw new InvalidOperationException("EMAIL_NOT_VERIFIED");

        // Validate required fields from schema snapshot
        ValidateRequiredFields(r);
        ValidateRequiredAttachments(r);

        r.Status = RequestStatus.Submitted;
        r.SubmittedAt = DateTime.UtcNow;

        db.RequestStatusHistories.Add(new RequestStatusHistory
        {
            Id = Guid.NewGuid(),
            RequestId = r.Id,
            FromStatus = RequestStatus.Draft,
            ToStatus = RequestStatus.Submitted,
            ChangedByUserId = request.CitizenId,
            ChangedBySource = StatusChangeSource.Citizen,
            ChangedAt = DateTime.UtcNow
        });

        // Create notification for citizen
        db.Notifications.Add(new Notification
        {
            Id = Guid.NewGuid(),
            TenantId = request.TenantId,
            UserId = request.CitizenId,
            Type = "request_submitted",
            TitleI18n = JsonSerializer.Serialize(new Dictionary<string, string>
            {
                ["hr"] = "Zahtjev podnesen",
                ["en"] = "Request submitted"
            }),
            BodyI18n = JsonSerializer.Serialize(new Dictionary<string, string>
            {
                ["hr"] = $"Vaš zahtjev {r.ReferenceNumber} je uspješno podnesen.",
                ["en"] = $"Your request {r.ReferenceNumber} has been submitted."
            }),
            RelatedRequestId = r.Id
        });

        // Create notification for officers
        var officers = await db.Users
            .Where(u => u.TenantId == request.TenantId
                && u.UserType == UserType.JlsOfficer && u.IsActive)
            .Select(u => u.Id)
            .ToListAsync(ct);

        foreach (var officerId in officers)
        {
            db.Notifications.Add(new Notification
            {
                Id = Guid.NewGuid(),
                TenantId = request.TenantId,
                UserId = officerId,
                Type = "new_request_submitted",
                TitleI18n = JsonSerializer.Serialize(new Dictionary<string, string>
                {
                    ["hr"] = "Novi zahtjev podnesen",
                    ["en"] = "New request submitted"
                }),
                BodyI18n = JsonSerializer.Serialize(new Dictionary<string, string>
                {
                    ["hr"] = $"Podnesen je novi zahtjev {r.ReferenceNumber}.",
                    ["en"] = $"New request {r.ReferenceNumber} has been submitted."
                }),
                RelatedRequestId = r.Id
            });
        }

        await db.SaveChangesAsync(ct);

        // Send confirmation email asynchronously
        _ = Task.Run(async () =>
        {
            try
            {
                await emailSender.SendAsync(new EmailMessage(
                    To: citizen.Email,
                    Subject: $"Zahtjev {r.ReferenceNumber} podnesen",
                    HtmlBody: $"<p>Vaš zahtjev <strong>{r.ReferenceNumber}</strong> je uspješno podnesen.</p><p>Službena osoba pregledat će vaš zahtjev u najkraćem mogućem roku.</p>"),
                    CancellationToken.None);
            }
            catch { }
        });

        return new SubmitRequestResult(r.ReferenceNumber);
    }

    private static void ValidateRequiredFields(Request r)
    {
        try
        {
            var schema = JsonSerializer.Deserialize<JsonElement>(r.FormSchemaSnapshot);
            var formData = JsonSerializer.Deserialize<JsonElement>(r.FormData);

            if (schema.TryGetProperty("fields", out var fields))
            {
                foreach (var field in fields.EnumerateArray())
                {
                    if (field.TryGetProperty("isRequired", out var req) && req.GetBoolean())
                    {
                        var key = field.GetProperty("fieldKey").GetString()!;
                        if (!formData.TryGetProperty(key, out var val)
                            || val.ValueKind == JsonValueKind.Null
                            || (val.ValueKind == JsonValueKind.String && string.IsNullOrWhiteSpace(val.GetString())))
                        {
                            throw new InvalidOperationException($"REQUIRED_FIELD_MISSING:{key}");
                        }
                    }
                }
            }
        }
        catch (InvalidOperationException) { throw; }
        catch { /* Schema parsing error - skip validation */ }
    }

    private static void ValidateRequiredAttachments(Request r)
    {
        try
        {
            var schema = JsonSerializer.Deserialize<JsonElement>(r.FormSchemaSnapshot);
            if (schema.TryGetProperty("attachments", out var attachments))
            {
                var uploadedKeys = r.Attachments.Select(a => a.AttachmentKey).ToHashSet();
                foreach (var att in attachments.EnumerateArray())
                {
                    if (att.TryGetProperty("isRequired", out var req) && req.GetBoolean())
                    {
                        var key = att.GetProperty("attachmentKey").GetString()!;
                        if (!uploadedKeys.Contains(key))
                            throw new InvalidOperationException($"REQUIRED_ATTACHMENT_MISSING:{key}");
                    }
                }
            }
        }
        catch (InvalidOperationException) { throw; }
        catch { }
    }
}
