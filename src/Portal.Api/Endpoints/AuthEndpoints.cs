using MediatR;
using Portal.Application.Commands.Auth.ForgotPassword;
using Portal.Application.Commands.Auth.Login;
using Portal.Application.Commands.Auth.Logout;
using Portal.Application.Commands.Auth.Refresh;
using Portal.Application.Commands.Auth.Register;
using Portal.Application.Commands.Auth.ResetPassword;
using Portal.Application.Commands.Auth.VerifyEmail;
using Portal.Application.DTOs.Auth;
using Portal.Application.Interfaces;
using Portal.Application.Queries.Auth.GetCurrentUser;

namespace Portal.Api.Endpoints;

public static class AuthEndpoints
{
    private const string RefreshTokenCookie = "refresh_token";

    public static void MapAuthEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/v1/auth").WithTags("Auth");

        group.MapPost("/register", async (
            RegisterRequest request,
            ITenantProvider tenantProvider,
            IMediator mediator) =>
        {
            var tenantId = tenantProvider.GetCurrentTenantId();
            if (tenantId == Guid.Empty)
                return Results.BadRequest(ProblemDetailsFor("Invalid tenant.", 400));

            var command = new RegisterCommand(
                request.Email, request.Password,
                request.FirstName, request.LastName,
                request.Oib, request.Phone, tenantId);

            try
            {
                var result = await mediator.Send(command);
                return Results.Created($"/api/v1/auth/me", result);
            }
            catch (InvalidOperationException ex) when (ex.Message == "USER_ALREADY_EXISTS")
            {
                return Results.Conflict(ProblemDetailsFor("Korisnik s ovom email adresom već postoji.", 409));
            }
        }).AllowAnonymous();

        group.MapPost("/verify-email", async (VerifyEmailRequest request, IMediator mediator) =>
        {
            try
            {
                await mediator.Send(new VerifyEmailCommand(request.Token));
                return Results.Ok(new { message = "Email uspješno verificiran." });
            }
            catch (InvalidOperationException ex) when (ex.Message is "INVALID_TOKEN" or "TOKEN_EXPIRED")
            {
                return Results.BadRequest(ProblemDetailsFor(
                    ex.Message == "TOKEN_EXPIRED"
                        ? "Token je istekao. Zatražite novi."
                        : "Nevažeći token.", 400));
            }
        }).AllowAnonymous();

        group.MapPost("/login", async (
            LoginRequest request,
            ITenantProvider tenantProvider,
            IMediator mediator,
            HttpContext httpContext) =>
        {
            var tenantId = tenantProvider.GetCurrentTenantId();
            if (tenantId == Guid.Empty)
                return Results.BadRequest(ProblemDetailsFor("Invalid tenant.", 400));

            var command = new LoginCommand(
                request.Email, request.Password, tenantId,
                httpContext.Request.Headers.UserAgent,
                httpContext.Connection.RemoteIpAddress?.ToString());

            try
            {
                var result = await mediator.Send(command);
                SetRefreshTokenCookie(httpContext, result.RawRefreshToken);
                return Results.Ok(result.Response);
            }
            catch (InvalidOperationException ex) when (ex.Message == "INVALID_CREDENTIALS")
            {
                return Results.Unauthorized();
            }
        }).AllowAnonymous();

        group.MapPost("/refresh", async (
            IMediator mediator,
            HttpContext httpContext) =>
        {
            var refreshToken = httpContext.Request.Cookies[RefreshTokenCookie];
            if (string.IsNullOrEmpty(refreshToken))
                return Results.Unauthorized();

            try
            {
                var result = await mediator.Send(new RefreshCommand(
                    refreshToken,
                    httpContext.Request.Headers.UserAgent,
                    httpContext.Connection.RemoteIpAddress?.ToString()));

                SetRefreshTokenCookie(httpContext, result.RawRefreshToken);
                return Results.Ok(result.Response);
            }
            catch (InvalidOperationException)
            {
                ClearRefreshTokenCookie(httpContext);
                return Results.Unauthorized();
            }
        }).AllowAnonymous();

        group.MapPost("/logout", async (IMediator mediator, HttpContext httpContext) =>
        {
            var refreshToken = httpContext.Request.Cookies[RefreshTokenCookie];
            if (!string.IsNullOrEmpty(refreshToken))
            {
                await mediator.Send(new LogoutCommand(refreshToken));
            }

            ClearRefreshTokenCookie(httpContext);
            return Results.NoContent();
        }).RequireAuthorization();

        group.MapPost("/password/forgot", async (
            ForgotPasswordRequest request,
            ITenantProvider tenantProvider,
            IMediator mediator) =>
        {
            var tenantId = tenantProvider.GetCurrentTenantId();
            if (tenantId != Guid.Empty)
            {
                await mediator.Send(new ForgotPasswordCommand(request.Email, tenantId));
            }

            // Always return 200 to prevent email enumeration
            return Results.Ok(new { message = "Ako postoji korisnički račun s navedenom email adresom, poslali smo upute za promjenu lozinke." });
        }).AllowAnonymous();

        group.MapPost("/password/reset", async (ResetPasswordRequest request, IMediator mediator) =>
        {
            try
            {
                await mediator.Send(new ResetPasswordCommand(request.Token, request.NewPassword));
                return Results.Ok(new { message = "Lozinka je uspješno promijenjena." });
            }
            catch (InvalidOperationException ex) when (ex.Message is "INVALID_TOKEN" or "TOKEN_EXPIRED")
            {
                return Results.BadRequest(ProblemDetailsFor(
                    ex.Message == "TOKEN_EXPIRED"
                        ? "Token je istekao. Zatražite novi."
                        : "Nevažeći token.", 400));
            }
        }).AllowAnonymous();

        group.MapGet("/me", async (ICurrentUserService currentUser, IMediator mediator) =>
        {
            if (currentUser.UserId is null)
                return Results.Unauthorized();

            try
            {
                var profile = await mediator.Send(new GetCurrentUserQuery(currentUser.UserId.Value));
                return Results.Ok(profile);
            }
            catch (InvalidOperationException)
            {
                return Results.Unauthorized();
            }
        }).RequireAuthorization();
    }

    private static void SetRefreshTokenCookie(HttpContext context, string token)
    {
        context.Response.Cookies.Append(RefreshTokenCookie, token, new CookieOptions
        {
            HttpOnly = true,
            Secure = true,
            SameSite = SameSiteMode.Strict,
            MaxAge = TimeSpan.FromDays(14),
            Path = "/api/v1/auth"
        });
    }

    private static void ClearRefreshTokenCookie(HttpContext context)
    {
        context.Response.Cookies.Delete(RefreshTokenCookie, new CookieOptions
        {
            HttpOnly = true,
            Secure = true,
            SameSite = SameSiteMode.Strict,
            Path = "/api/v1/auth"
        });
    }

    private static object ProblemDetailsFor(string detail, int status) => new
    {
        type = "https://tools.ietf.org/html/rfc7807",
        title = status switch
        {
            400 => "Bad Request",
            401 => "Unauthorized",
            409 => "Conflict",
            _ => "Error"
        },
        status,
        detail
    };
}
