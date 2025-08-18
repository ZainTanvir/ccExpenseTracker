using CreditAnalyzer.Application.Abstractions;
using Minio;
using Minio.DataModel.Args;

namespace CreditAnalyzer.Infrastructure.FileStorage;

public class MinioFileStorage(IMinioClient client, string bucket) : IFileStorage
{
    public async Task<string> UploadAsync(Stream content, string objectName, string contentType, CancellationToken ct = default)
    {
        var exists = await client.BucketExistsAsync(new BucketExistsArgs().WithBucket(bucket), ct);
        if (!exists) await client.MakeBucketAsync(new MakeBucketArgs().WithBucket(bucket), ct);

        if (!content.CanSeek) { var ms = new MemoryStream(); await content.CopyToAsync(ms, ct); ms.Position = 0; content = ms; }

        await client.PutObjectAsync(new PutObjectArgs()
            .WithBucket(bucket)
            .WithObject(objectName)
            .WithStreamData(content)
            .WithObjectSize(content.Length)
            .WithContentType(contentType), ct);

        return objectName;
    }

    public async Task<Stream> GetAsync(string objectName, CancellationToken ct = default)
    {
        var ms = new MemoryStream();
        await client.GetObjectAsync(new GetObjectArgs()
            .WithBucket(bucket)
            .WithObject(objectName)
            .WithCallbackStream(s => s.CopyTo(ms)), ct);
        ms.Position = 0;
        return ms;
    }
}