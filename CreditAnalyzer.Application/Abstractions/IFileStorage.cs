namespace CreditAnalyzer.Application.Abstractions;

public interface IFileStorage
{
    Task<string> UploadAsync(Stream content, string objectName, string contentType, CancellationToken ct = default);
    Task<Stream> GetAsync(string objectName, CancellationToken ct = default);
}
