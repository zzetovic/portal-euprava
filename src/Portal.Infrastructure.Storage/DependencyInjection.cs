using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Portal.Application.Interfaces;

namespace Portal.Infrastructure.Storage;

public static class DependencyInjection
{
    public static IServiceCollection AddStorage(this IServiceCollection services, IConfiguration configuration)
    {
        var attachmentPath = configuration["Storage:AttachmentBasePath"] ?? "/var/portal/attachments";
        var archiveIntakePath = configuration["Storage:ArchiveIntakePath"] ?? "/var/portal/archive-staging";

        services.AddSingleton<IAttachmentStorage>(new LocalFileSystemAttachmentStorage(attachmentPath));
        services.AddSingleton<IArchiveFileCopier>(new LocalArchiveFileCopier(archiveIntakePath));

        return services;
    }
}
