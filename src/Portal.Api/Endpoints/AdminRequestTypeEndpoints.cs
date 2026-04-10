using MediatR;
using Portal.Application.Commands.Admin.ActivateRequestType;
using Portal.Application.Commands.Admin.CreateRequestType;
using Portal.Application.Commands.Admin.DeactivateRequestType;
using Portal.Application.Commands.Admin.DeleteRequestType;
using Portal.Application.Commands.Admin.DuplicateRequestType;
using Portal.Application.Commands.Admin.UpdateRequestType;
using Portal.Application.DTOs.Admin;
using Portal.Application.Interfaces;
using Portal.Application.Queries.Admin.GetRequestTypeDetail;
using Portal.Application.Queries.Admin.GetRequestTypes;
using Portal.Application.Queries.Admin.GetRequestTypeUsage;
using Portal.Domain.Enums;

namespace Portal.Api.Endpoints;

public static class AdminRequestTypeEndpoints
{
    public static void MapAdminRequestTypeEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/v1/admin/request-types")
            .WithTags("Admin - Request Types")
            .RequireAuthorization();

        group.MapGet("/", async (
            string? filter,
            string? search,
            ICurrentUserService currentUser,
            ITenantProvider tenantProvider,
            IMediator mediator) =>
        {
            if (!IsAdmin(currentUser)) return Results.Forbid();
            var tenantId = tenantProvider.GetCurrentTenantId();

            var result = await mediator.Send(new GetRequestTypesQuery(tenantId, filter, search));
            return Results.Ok(result);
        });

        group.MapPost("/", async (
            CreateRequestTypeRequest request,
            ICurrentUserService currentUser,
            ITenantProvider tenantProvider,
            IMediator mediator) =>
        {
            if (!IsAdmin(currentUser)) return Results.Forbid();
            var tenantId = tenantProvider.GetCurrentTenantId();

            try
            {
                var result = await mediator.Send(new CreateRequestTypeCommand(
                    tenantId, currentUser.UserId!.Value,
                    request.Code, request.NameI18n, request.DescriptionI18n,
                    request.IsActive, request.SortOrder, request.EstimatedProcessingDays,
                    request.Fields, request.Attachments));
                return Results.Created($"/api/v1/admin/request-types/{result.Id}", result);
            }
            catch (InvalidOperationException ex) when (ex.Message == "REQUEST_TYPE_CODE_EXISTS")
            {
                return Results.Conflict(new { detail = "Vrsta zahtjeva s ovim kodom već postoji." });
            }
        });

        group.MapGet("/{id:guid}", async (
            Guid id,
            ICurrentUserService currentUser,
            ITenantProvider tenantProvider,
            IMediator mediator) =>
        {
            if (!IsAdmin(currentUser)) return Results.Forbid();
            var tenantId = tenantProvider.GetCurrentTenantId();

            try
            {
                var result = await mediator.Send(new GetRequestTypeDetailQuery(tenantId, id));
                return Results.Ok(result);
            }
            catch (InvalidOperationException ex) when (ex.Message == "REQUEST_TYPE_NOT_FOUND")
            {
                return Results.NotFound();
            }
        });

        group.MapPut("/{id:guid}", async (
            Guid id,
            UpdateRequestTypeRequest request,
            ICurrentUserService currentUser,
            ITenantProvider tenantProvider,
            IMediator mediator) =>
        {
            if (!IsAdmin(currentUser)) return Results.Forbid();
            var tenantId = tenantProvider.GetCurrentTenantId();

            try
            {
                var result = await mediator.Send(new UpdateRequestTypeCommand(
                    tenantId, currentUser.UserId!.Value, id,
                    request.Code, request.NameI18n, request.DescriptionI18n,
                    request.IsActive, request.SortOrder, request.EstimatedProcessingDays,
                    request.Fields, request.Attachments));
                return Results.Ok(result);
            }
            catch (InvalidOperationException ex) when (ex.Message == "REQUEST_TYPE_NOT_FOUND")
            {
                return Results.NotFound();
            }
            catch (InvalidOperationException ex) when (ex.Message == "REQUEST_TYPE_ARCHIVED")
            {
                return Results.BadRequest(new { detail = "Arhivirana vrsta zahtjeva ne može se uređivati." });
            }
        });

        group.MapDelete("/{id:guid}", async (
            Guid id,
            ICurrentUserService currentUser,
            ITenantProvider tenantProvider,
            IMediator mediator) =>
        {
            if (!IsAdmin(currentUser)) return Results.Forbid();
            var tenantId = tenantProvider.GetCurrentTenantId();

            try
            {
                await mediator.Send(new DeleteRequestTypeCommand(tenantId, currentUser.UserId!.Value, id));
                return Results.NoContent();
            }
            catch (InvalidOperationException ex) when (ex.Message == "REQUEST_TYPE_NOT_FOUND")
            {
                return Results.NotFound();
            }
            catch (InvalidOperationException ex) when (ex.Message == "REQUEST_TYPE_HAS_ACTIVE_REQUESTS")
            {
                return Results.BadRequest(new { detail = "Vrsta zahtjeva ima aktivne zahtjeve. Deaktivirajte umjesto brisanja." });
            }
        });

        group.MapPost("/{id:guid}/activate", async (
            Guid id,
            ICurrentUserService currentUser,
            ITenantProvider tenantProvider,
            IMediator mediator) =>
        {
            if (!IsAdmin(currentUser)) return Results.Forbid();
            var tenantId = tenantProvider.GetCurrentTenantId();

            try
            {
                await mediator.Send(new ActivateRequestTypeCommand(tenantId, currentUser.UserId!.Value, id));
                return Results.NoContent();
            }
            catch (InvalidOperationException ex) when (ex.Message == "REQUEST_TYPE_NOT_FOUND")
            {
                return Results.NotFound();
            }
        });

        group.MapPost("/{id:guid}/deactivate", async (
            Guid id,
            ICurrentUserService currentUser,
            ITenantProvider tenantProvider,
            IMediator mediator) =>
        {
            if (!IsAdmin(currentUser)) return Results.Forbid();
            var tenantId = tenantProvider.GetCurrentTenantId();

            try
            {
                await mediator.Send(new DeactivateRequestTypeCommand(tenantId, currentUser.UserId!.Value, id));
                return Results.NoContent();
            }
            catch (InvalidOperationException ex) when (ex.Message == "REQUEST_TYPE_NOT_FOUND")
            {
                return Results.NotFound();
            }
        });

        group.MapPost("/{id:guid}/duplicate", async (
            Guid id,
            ICurrentUserService currentUser,
            ITenantProvider tenantProvider,
            IMediator mediator) =>
        {
            if (!IsAdmin(currentUser)) return Results.Forbid();
            var tenantId = tenantProvider.GetCurrentTenantId();

            try
            {
                var result = await mediator.Send(new DuplicateRequestTypeCommand(tenantId, currentUser.UserId!.Value, id));
                return Results.Created($"/api/v1/admin/request-types/{result.Id}", result);
            }
            catch (InvalidOperationException ex) when (ex.Message == "REQUEST_TYPE_NOT_FOUND")
            {
                return Results.NotFound();
            }
        });

        group.MapGet("/{id:guid}/usage", async (
            Guid id,
            ICurrentUserService currentUser,
            ITenantProvider tenantProvider,
            IMediator mediator) =>
        {
            if (!IsAdmin(currentUser)) return Results.Forbid();
            var tenantId = tenantProvider.GetCurrentTenantId();

            try
            {
                var result = await mediator.Send(new GetRequestTypeUsageQuery(tenantId, id));
                return Results.Ok(result);
            }
            catch (InvalidOperationException ex) when (ex.Message == "REQUEST_TYPE_NOT_FOUND")
            {
                return Results.NotFound();
            }
        });
    }

    private static bool IsAdmin(ICurrentUserService currentUser) =>
        currentUser.IsAuthenticated && currentUser.UserType == UserType.JlsAdmin;
}
