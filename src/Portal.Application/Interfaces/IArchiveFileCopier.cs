namespace Portal.Application.Interfaces;

public interface IArchiveFileCopier
{
    Task<string> CopyToIntakeAsync(Stream source, string filename, CancellationToken ct);
}
