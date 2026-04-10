FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Copy solution and project files first for layer caching
COPY PortalEuprava.sln .
COPY src/Portal.Domain/Portal.Domain.csproj src/Portal.Domain/
COPY src/Portal.Application/Portal.Application.csproj src/Portal.Application/
COPY src/Portal.Infrastructure.Persistence/Portal.Infrastructure.Persistence.csproj src/Portal.Infrastructure.Persistence/
COPY src/Portal.Infrastructure.LocalDb/Portal.Infrastructure.LocalDb.csproj src/Portal.Infrastructure.LocalDb/
COPY src/Portal.Infrastructure.Storage/Portal.Infrastructure.Storage.csproj src/Portal.Infrastructure.Storage/
COPY src/Portal.Infrastructure.Email/Portal.Infrastructure.Email.csproj src/Portal.Infrastructure.Email/
COPY src/Portal.Infrastructure.Identity/Portal.Infrastructure.Identity.csproj src/Portal.Infrastructure.Identity/
COPY src/Portal.Api/Portal.Api.csproj src/Portal.Api/
COPY tests/Portal.Domain.Tests/Portal.Domain.Tests.csproj tests/Portal.Domain.Tests/
COPY tests/Portal.Application.Tests/Portal.Application.Tests.csproj tests/Portal.Application.Tests/
COPY tests/Portal.Architecture.Tests/Portal.Architecture.Tests.csproj tests/Portal.Architecture.Tests/

RUN dotnet restore

# Copy everything and build
COPY . .
RUN dotnet publish src/Portal.Api/Portal.Api.csproj -c Release -o /app/publish --no-restore

# Runtime image
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS runtime
WORKDIR /app

RUN mkdir -p /var/portal/attachments /var/portal/archive-staging

COPY --from=build /app/publish .

ENV ASPNETCORE_URLS=http://+:5000
EXPOSE 5000

ENTRYPOINT ["dotnet", "Portal.Api.dll"]
