using System.Reflection;
using NetArchTest.Rules;
using FluentAssertions;

namespace Portal.Architecture.Tests;

public class ArchitectureTests
{
    private static readonly Assembly DomainAssembly = typeof(Domain.Common.BaseEntity).Assembly;
    private static readonly Assembly ApplicationAssembly = typeof(Application.DependencyInjection).Assembly;
    private static readonly Assembly PersistenceAssembly = typeof(Infrastructure.Persistence.PortalDbContext).Assembly;
    private static readonly Assembly LocalDbAssembly = typeof(Infrastructure.LocalDb.LocalDbAktWriter).Assembly;
    private static readonly Assembly StorageAssembly = typeof(Infrastructure.Storage.LocalFileSystemAttachmentStorage).Assembly;
    private static readonly Assembly EmailAssembly = typeof(Infrastructure.Email.SmtpEmailSender).Assembly;
    private static readonly Assembly IdentityAssembly = typeof(Infrastructure.Identity.JwtSettings).Assembly;

    [Fact]
    public void Domain_Should_Not_Reference_Application()
    {
        var result = Types.InAssembly(DomainAssembly)
            .ShouldNot()
            .HaveDependencyOn("Portal.Application")
            .GetResult();

        result.IsSuccessful.Should().BeTrue(
            "Domain must not depend on Application layer");
    }

    [Fact]
    public void Domain_Should_Not_Reference_Infrastructure()
    {
        var result = Types.InAssembly(DomainAssembly)
            .ShouldNot()
            .HaveDependencyOnAny(
                "Portal.Infrastructure.Persistence",
                "Portal.Infrastructure.LocalDb",
                "Portal.Infrastructure.Storage",
                "Portal.Infrastructure.Email",
                "Portal.Infrastructure.Identity")
            .GetResult();

        result.IsSuccessful.Should().BeTrue(
            "Domain must not depend on any Infrastructure layer");
    }

    [Fact]
    public void Domain_Should_Not_Reference_Api()
    {
        var result = Types.InAssembly(DomainAssembly)
            .ShouldNot()
            .HaveDependencyOn("Portal.Api")
            .GetResult();

        result.IsSuccessful.Should().BeTrue(
            "Domain must not depend on Api layer");
    }

    [Fact]
    public void Application_Should_Not_Reference_Infrastructure()
    {
        var result = Types.InAssembly(ApplicationAssembly)
            .ShouldNot()
            .HaveDependencyOnAny(
                "Portal.Infrastructure.Persistence",
                "Portal.Infrastructure.LocalDb",
                "Portal.Infrastructure.Storage",
                "Portal.Infrastructure.Email",
                "Portal.Infrastructure.Identity")
            .GetResult();

        result.IsSuccessful.Should().BeTrue(
            "Application must not depend on any Infrastructure layer");
    }

    [Fact]
    public void Application_Should_Not_Reference_Api()
    {
        var result = Types.InAssembly(ApplicationAssembly)
            .ShouldNot()
            .HaveDependencyOn("Portal.Api")
            .GetResult();

        result.IsSuccessful.Should().BeTrue(
            "Application must not depend on Api layer");
    }

    [Fact]
    public void SqlClient_Should_Only_Exist_In_LocalDb()
    {
        var nonLocalDbAssemblies = new[]
        {
            DomainAssembly,
            ApplicationAssembly,
            PersistenceAssembly,
            StorageAssembly,
            EmailAssembly,
            IdentityAssembly,
        };

        foreach (var assembly in nonLocalDbAssemblies)
        {
            var result = Types.InAssembly(assembly)
                .ShouldNot()
                .HaveDependencyOn("Microsoft.Data.SqlClient")
                .GetResult();

            result.IsSuccessful.Should().BeTrue(
                $"Microsoft.Data.SqlClient must only exist in Portal.Infrastructure.LocalDb, but was found in {assembly.GetName().Name}");
        }
    }

    [Fact]
    public void LocalDb_May_Reference_SqlClient()
    {
        // This test documents the allowed dependency — LocalDb IS the SQL Server adapter
        var referencedAssemblies = LocalDbAssembly.GetReferencedAssemblies()
            .Select(a => a.Name)
            .ToList();

        referencedAssemblies.Should().Contain("Microsoft.Data.SqlClient",
            "Portal.Infrastructure.LocalDb is the designated SQL Server adapter");
    }
}
