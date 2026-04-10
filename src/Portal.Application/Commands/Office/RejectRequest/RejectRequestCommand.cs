using System.Text.Json;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Portal.Application.Interfaces;
using Portal.Domain.Entities;
using Portal.Domain.Enums;

namespace Portal.Application.Commands.Office.RejectRequest;

public record RejectRequestCommand(
    Guid TenantId,
    Guid OfficerId,
    Guid RequestId,
    string RejectionReasonCode,
    string? InternalNote) : IRequest;

public class RejectRequestCommandValidator : AbstractValidator<RejectRequestCommand>
{
    private static readonly string[] ValidReasons =
        ["inappropriate_content", "out_of_jurisdiction", "duplicate", "not_serious", "other"];

    public RejectRequestCommandValidator()
    {
        RuleFor(x => x.RejectionReasonCode)
            .NotEmpty().WithMessage("Razlog odbijanja je obavezan.")
            .Must(code => ValidReasons.Contains(code)).WithMessage("Nevažeći razlog odbijanja.");

        RuleFor(x => x.InternalNote)
            .NotEmpty()
            .When(x => x.RejectionReasonCode == "other")
            .WithMessage("Interna napomena je obavezna kada je razlog 'Ostalo'.");
    }
}

public class RejectRequestCommandHandler(
    IPortalDbContext db,
    IEmailSender emailSender) : IRequestHandler<RejectRequestCommand>
{
    public async Task Handle(RejectRequestCommand request, CancellationToken ct)
    {
        var r = await db.Requests
            .Include(r => r.Citizen)
            .FirstOrDefaultAsync(r => r.Id == request.RequestId && r.TenantId == request.TenantId, ct)
            ?? throw new InvalidOperationException("REQUEST_NOT_FOUND");

        if (r.Status != RequestStatus.Submitted)
            throw new InvalidOperationException("REQUEST_NOT_SUBMITTED");

        var beforeStatus = r.Status;
        r.Status = RequestStatus.RejectedByOfficer;
        r.RejectionReasonCode = request.RejectionReasonCode;
        r.RejectionInternalNote = request.InternalNote;
        r.ReviewedByUserId = request.OfficerId;
        r.ReviewedAt = DateTime.UtcNow;

        db.RequestStatusHistories.Add(new RequestStatusHistory
        {
            Id = Guid.NewGuid(),
            RequestId = r.Id,
            FromStatus = beforeStatus,
            ToStatus = RequestStatus.RejectedByOfficer,
            ChangedByUserId = request.OfficerId,
            ChangedBySource = StatusChangeSource.Officer,
            Comment = request.RejectionReasonCode,
            ChangedAt = DateTime.UtcNow
        });

        // Notification for citizen
        db.Notifications.Add(new Notification
        {
            Id = Guid.NewGuid(),
            TenantId = request.TenantId,
            UserId = r.CitizenId,
            Type = "request_rejected",
            TitleI18n = JsonSerializer.Serialize(new Dictionary<string, string>
            {
                ["hr"] = "Zahtjev odbijen",
                ["en"] = "Request rejected"
            }),
            BodyI18n = JsonSerializer.Serialize(new Dictionary<string, string>
            {
                ["hr"] = $"Vaš zahtjev {r.ReferenceNumber} je odbijen.",
                ["en"] = $"Your request {r.ReferenceNumber} has been rejected."
            }),
            RelatedRequestId = r.Id
        });

        db.AuditLogs.Add(new AuditLog
        {
            Id = Guid.NewGuid(),
            TenantId = request.TenantId,
            UserId = request.OfficerId,
            Action = "request.rejected",
            EntityType = "request",
            EntityId = r.Id,
            Before = JsonSerializer.Serialize(new { Status = beforeStatus.ToString() }),
            After = JsonSerializer.Serialize(new { Status = r.Status.ToString(), r.RejectionReasonCode })
        });

        await db.SaveChangesAsync(ct);

        // Send email to citizen
        _ = Task.Run(async () =>
        {
            try
            {
                var reasonLabel = GetReasonLabel(request.RejectionReasonCode);
                await emailSender.SendAsync(new EmailMessage(
                    To: r.Citizen.Email,
                    Subject: $"Zahtjev {r.ReferenceNumber} odbijen",
                    HtmlBody: $"<p>Vaš zahtjev <strong>{r.ReferenceNumber}</strong> je odbijen.</p><p>Razlog: {reasonLabel}</p>"),
                    CancellationToken.None);
            }
            catch { }
        });
    }

    private static string GetReasonLabel(string code) => code switch
    {
        "inappropriate_content" => "Neprimjereni sadržaj",
        "out_of_jurisdiction" => "Nije u nadležnosti ove JLS",
        "duplicate" => "Ponavljajući zahtjev",
        "not_serious" => "Očito neozbiljan zahtjev",
        "other" => "Ostalo",
        _ => code
    };
}
