using MediatR;
using Portal.Application.Commands.Requests.CreateDraft;
using Portal.Application.Commands.Requests.DeleteAttachment;
using Portal.Application.Commands.Requests.DeleteDraft;
using Portal.Application.Commands.Requests.SubmitRequest;
using Portal.Application.Commands.Requests.UpdateDraft;
using Portal.Application.Commands.Requests.UploadAttachment;
using Portal.Application.Interfaces;
using Portal.Application.Queries.Requests.GetActiveRequestTypes;
using Portal.Application.Queries.Requests.GetMyRequests;
using Portal.Application.Queries.Requests.GetRequestDetail;
using Portal.Application.Queries.Requests.GetRequestHistory;
using Portal.Application.Queries.Requests.GetRequestTypePreflight;
using Portal.Application.Queries.Requests.GetRequestTypeSchema;
using Portal.Domain.Enums;

namespace Portal.Api.Endpoints;

public static class CitizenRequestEndpoints
{
    public static void MapCitizenRequestEndpoints(this WebApplication app)
    {
        // Request types (citizen view)
        var rtGroup = app.MapGroup("/api/v1/request-types")
            .WithTags("Request Types")
            .RequireAuthorization();

        rtGroup.MapGet("/", async (
            ICurrentUserService currentUser,
            ITenantProvider tenantProvider,
            IMediator mediator) =>
        {
            if (!IsCitizen(currentUser)) return Results.Forbid();
            var result = await mediator.Send(new GetActiveRequestTypesQuery(tenantProvider.GetCurrentTenantId()));
            return Results.Ok(result);
        });

        rtGroup.MapGet("/{code}", async (
            string code,
            ICurrentUserService currentUser,
            ITenantProvider tenantProvider,
            IMediator mediator) =>
        {
            if (!IsCitizen(currentUser)) return Results.Forbid();
            try
            {
                var result = await mediator.Send(new GetRequestTypePreflightQuery(tenantProvider.GetCurrentTenantId(), code));
                return Results.Ok(result);
            }
            catch (InvalidOperationException ex) when (ex.Message == "REQUEST_TYPE_NOT_FOUND")
            {
                return Results.NotFound();
            }
        });

        rtGroup.MapGet("/{id:guid}/schema", async (
            Guid id,
            ICurrentUserService currentUser,
            ITenantProvider tenantProvider,
            IMediator mediator) =>
        {
            if (!IsCitizen(currentUser)) return Results.Forbid();
            try
            {
                var result = await mediator.Send(new GetRequestTypeSchemaQuery(tenantProvider.GetCurrentTenantId(), id));
                return Results.Ok(result);
            }
            catch (InvalidOperationException ex) when (ex.Message == "REQUEST_TYPE_NOT_FOUND")
            {
                return Results.NotFound();
            }
        });

        // Requests (citizen CRUD)
        var reqGroup = app.MapGroup("/api/v1/requests")
            .WithTags("Citizen Requests")
            .RequireAuthorization();

        reqGroup.MapGet("/", async (
            string? status,
            int page,
            int pageSize,
            ICurrentUserService currentUser,
            ITenantProvider tenantProvider,
            IMediator mediator) =>
        {
            if (!IsCitizen(currentUser)) return Results.Forbid();
            var p = page < 1 ? 1 : page;
            var ps = pageSize is < 1 or > 100 ? 20 : pageSize;
            var result = await mediator.Send(new GetMyRequestsQuery(
                tenantProvider.GetCurrentTenantId(), currentUser.UserId!.Value, status, p, ps));
            return Results.Ok(result);
        });

        reqGroup.MapPost("/", async (
            CreateDraftRequest body,
            ICurrentUserService currentUser,
            ITenantProvider tenantProvider,
            IMediator mediator) =>
        {
            if (!IsCitizen(currentUser)) return Results.Forbid();
            try
            {
                var result = await mediator.Send(new CreateDraftCommand(
                    tenantProvider.GetCurrentTenantId(), currentUser.UserId!.Value, body.RequestTypeId));
                return Results.Created($"/api/v1/requests/{result.Id}", result);
            }
            catch (InvalidOperationException ex) when (ex.Message == "REQUEST_TYPE_NOT_FOUND")
            {
                return Results.BadRequest(new { detail = "Vrsta zahtjeva nije pronađena ili nije aktivna." });
            }
        });

        reqGroup.MapGet("/{id:guid}", async (
            Guid id,
            ICurrentUserService currentUser,
            ITenantProvider tenantProvider,
            IMediator mediator) =>
        {
            if (!IsCitizen(currentUser)) return Results.Forbid();
            try
            {
                var result = await mediator.Send(new GetRequestDetailQuery(
                    tenantProvider.GetCurrentTenantId(), currentUser.UserId!.Value, id));
                return Results.Ok(result);
            }
            catch (InvalidOperationException ex) when (ex.Message == "REQUEST_NOT_FOUND")
            {
                return Results.NotFound();
            }
        });

        reqGroup.MapPatch("/{id:guid}", async (
            Guid id,
            PatchDraftRequest body,
            HttpContext httpContext,
            ICurrentUserService currentUser,
            ITenantProvider tenantProvider,
            IMediator mediator) =>
        {
            if (!IsCitizen(currentUser)) return Results.Forbid();

            var etag = httpContext.Request.Headers.IfMatch.FirstOrDefault()?.Trim('"');
            if (string.IsNullOrEmpty(etag))
                return Results.BadRequest(new { detail = "If-Match header is required." });

            try
            {
                var result = await mediator.Send(new UpdateDraftCommand(
                    tenantProvider.GetCurrentTenantId(), currentUser.UserId!.Value, id, body.FormData, etag));
                httpContext.Response.Headers.ETag = $"\"{result.NewEtag}\"";
                return Results.Ok(new { etag = result.NewEtag });
            }
            catch (InvalidOperationException ex) when (ex.Message == "REQUEST_NOT_FOUND")
            {
                return Results.NotFound();
            }
            catch (InvalidOperationException ex) when (ex.Message == "REQUEST_NOT_DRAFT")
            {
                return Results.BadRequest(new { detail = "Samo nacrti se mogu uređivati." });
            }
            catch (InvalidOperationException ex) when (ex.Message == "ETAG_MISMATCH")
            {
                return Results.Conflict(new { detail = "Zahtjev je uređen u drugom tabu." });
            }
        });

        reqGroup.MapDelete("/{id:guid}", async (
            Guid id,
            ICurrentUserService currentUser,
            ITenantProvider tenantProvider,
            IMediator mediator) =>
        {
            if (!IsCitizen(currentUser)) return Results.Forbid();
            try
            {
                await mediator.Send(new DeleteDraftCommand(
                    tenantProvider.GetCurrentTenantId(), currentUser.UserId!.Value, id));
                return Results.NoContent();
            }
            catch (InvalidOperationException ex) when (ex.Message == "REQUEST_NOT_FOUND")
            {
                return Results.NotFound();
            }
            catch (InvalidOperationException ex) when (ex.Message == "REQUEST_NOT_DRAFT")
            {
                return Results.BadRequest(new { detail = "Samo nacrti se mogu obrisati." });
            }
        });

        reqGroup.MapPost("/{id:guid}/attachments", async (
            Guid id,
            HttpRequest httpRequest,
            ICurrentUserService currentUser,
            ITenantProvider tenantProvider,
            IMediator mediator) =>
        {
            if (!IsCitizen(currentUser)) return Results.Forbid();

            if (!httpRequest.HasFormContentType || httpRequest.Form.Files.Count == 0)
                return Results.BadRequest(new { detail = "File is required." });

            var file = httpRequest.Form.Files[0];
            var attachmentKey = httpRequest.Form["attachmentKey"].FirstOrDefault() ?? file.FileName;

            try
            {
                using var stream = file.OpenReadStream();
                var result = await mediator.Send(new UploadAttachmentCommand(
                    tenantProvider.GetCurrentTenantId(), currentUser.UserId!.Value, id,
                    attachmentKey, file.FileName, file.ContentType, file.Length, stream));
                return Results.Created($"/api/v1/requests/{id}/attachments/{result.Id}", result);
            }
            catch (InvalidOperationException ex) when (ex.Message == "REQUEST_NOT_FOUND_OR_NOT_DRAFT")
            {
                return Results.BadRequest(new { detail = "Zahtjev nije pronađen ili nije u statusu nacrta." });
            }
        }).DisableAntiforgery();

        reqGroup.MapDelete("/{id:guid}/attachments/{attId:guid}", async (
            Guid id,
            Guid attId,
            ICurrentUserService currentUser,
            ITenantProvider tenantProvider,
            IMediator mediator) =>
        {
            if (!IsCitizen(currentUser)) return Results.Forbid();
            try
            {
                await mediator.Send(new DeleteAttachmentCommand(
                    tenantProvider.GetCurrentTenantId(), currentUser.UserId!.Value, id, attId));
                return Results.NoContent();
            }
            catch (InvalidOperationException ex) when (ex.Message is "REQUEST_NOT_FOUND" or "ATTACHMENT_NOT_FOUND")
            {
                return Results.NotFound();
            }
            catch (InvalidOperationException ex) when (ex.Message == "REQUEST_NOT_DRAFT")
            {
                return Results.BadRequest(new { detail = "Privitci se mogu brisati samo za nacrte." });
            }
        });

        reqGroup.MapPost("/{id:guid}/submit", async (
            Guid id,
            ICurrentUserService currentUser,
            ITenantProvider tenantProvider,
            IMediator mediator) =>
        {
            if (!IsCitizen(currentUser)) return Results.Forbid();
            try
            {
                var result = await mediator.Send(new SubmitRequestCommand(
                    tenantProvider.GetCurrentTenantId(), currentUser.UserId!.Value, id));
                return Results.Ok(result);
            }
            catch (InvalidOperationException ex) when (ex.Message == "REQUEST_NOT_FOUND")
            {
                return Results.NotFound();
            }
            catch (InvalidOperationException ex) when (ex.Message == "REQUEST_NOT_DRAFT")
            {
                return Results.BadRequest(new { detail = "Samo nacrti se mogu podnijeti." });
            }
            catch (InvalidOperationException ex) when (ex.Message == "EMAIL_NOT_VERIFIED")
            {
                return Results.BadRequest(new { detail = "Za podnošenje zahtjeva morate verificirati email adresu.", code = "EMAIL_NOT_VERIFIED" });
            }
            catch (InvalidOperationException ex) when (ex.Message.StartsWith("REQUIRED_FIELD_MISSING:"))
            {
                var fieldKey = ex.Message.Split(':')[1];
                return Results.UnprocessableEntity(new { detail = $"Obavezno polje '{fieldKey}' nije popunjeno." });
            }
            catch (InvalidOperationException ex) when (ex.Message.StartsWith("REQUIRED_ATTACHMENT_MISSING:"))
            {
                var attKey = ex.Message.Split(':')[1];
                return Results.UnprocessableEntity(new { detail = $"Obavezni privitak '{attKey}' nije učitan." });
            }
        });

        reqGroup.MapGet("/{id:guid}/attachments/{attId:guid}/download", async (
            Guid id,
            Guid attId,
            ICurrentUserService currentUser,
            ITenantProvider tenantProvider,
            IPortalDbContext db,
            IAttachmentStorage attachmentStorage) =>
        {
            if (!IsCitizen(currentUser)) return Results.Forbid();

            var attachment = await Microsoft.EntityFrameworkCore.EntityFrameworkQueryableExtensions.FirstOrDefaultAsync(
                db.RequestAttachments.Where(a =>
                    a.Id == attId && a.RequestId == id
                    && a.Request.TenantId == tenantProvider.GetCurrentTenantId()
                    && a.Request.CitizenId == currentUser.UserId!.Value));

            if (attachment is null)
                return Results.NotFound();

            var stream = await attachmentStorage.OpenReadAsync(attachment.StorageKey, CancellationToken.None);
            return Results.File(stream, attachment.MimeType, attachment.OriginalFilename);
        });

        reqGroup.MapGet("/{id:guid}/history", async (
            Guid id,
            ICurrentUserService currentUser,
            ITenantProvider tenantProvider,
            IMediator mediator) =>
        {
            if (!IsCitizen(currentUser)) return Results.Forbid();
            try
            {
                var result = await mediator.Send(new GetRequestHistoryQuery(
                    tenantProvider.GetCurrentTenantId(), currentUser.UserId!.Value, id));
                return Results.Ok(result);
            }
            catch (InvalidOperationException ex) when (ex.Message == "REQUEST_NOT_FOUND")
            {
                return Results.NotFound();
            }
        });
    }

    private static bool IsCitizen(ICurrentUserService currentUser) =>
        currentUser.IsAuthenticated && currentUser.UserType == UserType.Citizen;
}

public record CreateDraftRequest(Guid RequestTypeId);
public record PatchDraftRequest(string FormData);
