using Portal.Application.Interfaces;

namespace Portal.Infrastructure.Storage;

public class LocalArchiveFileCopier(string intakePath) : IArchiveFileCopier
{
    public async Task<string> CopyToIntakeAsync(Stream source, string filename, CancellationToken ct)
    {
        Directory.CreateDirectory(intakePath);

        var destinationPath = Path.Combine(intakePath, $"{Guid.NewGuid()}_{filename}");
        await using var fileStream = new FileStream(destinationPath, FileMode.Create, FileAccess.Write);
        await source.CopyToAsync(fileStream, ct);

        return destinationPath;
    }
}
