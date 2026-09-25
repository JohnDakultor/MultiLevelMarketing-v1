using Azure;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Microsoft.Extensions.Options;
using modular_mlm.Application.Common.Exceptions;
using modular_mlm.Application.Common.Interfaces;
using modular_mlm.Application.Common.Models;

namespace modular_mlm.Infrastructure.Storage;

public sealed class AzureBlobObjectStorage(
    BlobContainerClient container,
    IOptions<AzureBlobObjectStorageOptions> options
) : IObjectStorage
{
    private readonly AzureBlobObjectStorageOptions _options = options.Value;

    public async Task<StoredObject> PutAsync(
        ObjectUploadRequest request,
        CancellationToken cancellationToken
    )
    {
        try
        {
            var blob = container.GetBlobClient(request.ObjectKey);
            var response = await blob.UploadAsync(
                request.Content,
                new BlobUploadOptions
                {
                    HttpHeaders = new BlobHttpHeaders
                    {
                        ContentType = request.ContentType,
                        CacheControl = "public,max-age=31536000,immutable",
                    },
                },
                cancellationToken
            );

            return new StoredObject(
                request.ObjectKey,
                BuildPublicUri(request.ObjectKey),
                request.ContentType,
                request.ContentLength,
                response.Value.ETag.ToString(),
                response.Value.VersionId
            );
        }
        catch (RequestFailedException exception)
        {
            throw new ObjectStorageException("The object could not be stored.", exception);
        }
    }

    public async Task DeleteAsync(string objectKey, CancellationToken cancellationToken)
    {
        var validatedKey = ValidateKey(objectKey);
        try
        {
            await container
                .GetBlobClient(validatedKey)
                .DeleteIfExistsAsync(
                    DeleteSnapshotsOption.IncludeSnapshots,
                    cancellationToken: cancellationToken
                );
        }
        catch (RequestFailedException exception)
        {
            throw new ObjectStorageException("The object could not be deleted.", exception);
        }
    }

    private Uri BuildPublicUri(string objectKey)
    {
        var publicBaseUrl = string.IsNullOrWhiteSpace(_options.PublicBaseUrl)
            ? container.Uri.AbsoluteUri
            : _options.PublicBaseUrl;
        var baseUri = new Uri(publicBaseUrl.TrimEnd('/') + "/", UriKind.Absolute);
        var escapedKey = string.Join('/', objectKey.Split('/').Select(Uri.EscapeDataString));
        return new Uri(baseUri, escapedKey);
    }

    private static string ValidateKey(string objectKey)
    {
        using var emptyContent = new MemoryStream([0]);
        return new ObjectUploadRequest(
            objectKey,
            "application/octet-stream",
            1,
            emptyContent
        ).ObjectKey;
    }
}
