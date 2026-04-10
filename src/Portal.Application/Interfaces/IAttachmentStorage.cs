namespace Portal.Application.Interfaces;

public interface IAttachmentStorage
{
    Task<string> SaveAsync(Stream content, string suggestedName, CancellationToken ct);
    Task<Stream> OpenReadAsync(string storageKey, CancellationToken ct);
    Task DeleteAsync(string storageKey, CancellationToken ct);
}
