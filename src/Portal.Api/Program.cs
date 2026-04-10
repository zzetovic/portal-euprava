using Portal.Api.Endpoints;
using Portal.Api.Middleware;
using Portal.Api.Services;
using Portal.Application;
using Portal.Application.Interfaces;
using Portal.Infrastructure.Email;
using Portal.Infrastructure.Identity;
using Portal.Infrastructure.LocalDb;
using Portal.Infrastructure.Persistence;
using Portal.Infrastructure.Storage;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

// Serilog
builder.Host.UseSerilog((context, loggerConfig) =>
{
    loggerConfig
        .ReadFrom.Configuration(context.Configuration)
        .Enrich.FromLogContext()
        .WriteTo.Console();
});

// Application layer
builder.Services.AddApplication();

// Infrastructure layers
builder.Services.AddPersistence(builder.Configuration);
builder.Services.AddLocalDb();
builder.Services.AddStorage(builder.Configuration);
builder.Services.AddEmail(builder.Configuration);
builder.Services.AddIdentityInfrastructure(builder.Configuration);

// HTTP context & services
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ICurrentUserService, CurrentUserService>();
builder.Services.AddScoped<ITenantProvider, HttpTenantProvider>();

// CORS
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        var origins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>()
            ?? ["http://localhost:5173"];
        policy.WithOrigins(origins)
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials();
    });
});

var app = builder.Build();

// Middleware pipeline
app.UseMiddleware<ExceptionHandlingMiddleware>();
app.UseMiddleware<TenantMiddleware>();

app.UseCors();
app.UseAuthentication();
app.UseMiddleware<TenantJwtCrossCheckMiddleware>();
app.UseMiddleware<MustChangePasswordMiddleware>();
app.UseAuthorization();

app.UseSerilogRequestLogging();

// Endpoints
app.MapHealthEndpoints();
app.MapAuthEndpoints();

app.Run();
