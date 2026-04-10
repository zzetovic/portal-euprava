using Portal.Application.Interfaces;

namespace Portal.Infrastructure.Storage;

public class LocalFileSystemAttachmentStorage(string basePath) : IAttachmentStorage
{
    public async Task<string> SaveAsync(Stream content, string suggestedName, CancellationToken ct)
    {
        var now = DateTime.UtcNow;
        var relativePath = Path.Combine(now.Year.ToString(), now.Month.ToString("D2"), $"{Guid.NewGuid()}{Path.GetExtension(suggestedName)}");
        var fullPath = Path.Combine(basePath, relativePath);

        var directory = Path.GetDirectoryName(fullPath)!;
        Directory.CreateDirectory(directory);

        await using var fileStream = new FileStream(fullPath, FileMode.Create, FileAccess.Write);
        await content.CopyToAsync(fileStream, ct);

        return relativePath;
    }

    public Task<Stream> OpenReadAsync(string storageKey, CancellationToken ct)
    {
        var fullPath = Path.Combine(basePath, storageKey);
        Stream stream = new FileStream(fullPath, FileMode.Open, FileAccess.Read);
        return Task.FromResult(stream);
    }

    public Task DeleteAsync(string storageKey, CancellationToken ct)
    {
        var fullPath = Path.Combine(basePath, storageKey);
        if (File.Exists(fullPath))
            File.Delete(fullPath);
        return Task.CompletedTask;
    }
}
