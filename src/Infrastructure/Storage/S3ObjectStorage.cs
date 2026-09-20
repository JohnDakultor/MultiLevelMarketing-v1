using Amazon.S3;
using Amazon.S3.Model;
using Microsoft.Extensions.Options;
using modular_mlm.Application.Common.Exceptions;
using modular_mlm.Application.Common.Interfaces;
using modular_mlm.Application.Common.Models;

namespace modular_mlm.Infrastructure.Storage;

public sealed class S3ObjectStorage(IAmazonS3 client, IOptions<S3ObjectStorageOptions> options)
    : IObjectStorage
{
    private readonly S3ObjectStorageOptions _options = options.Value;

    public async Task<StoredObject> PutAsync(
        ObjectUploadRequest request,
        CancellationToken cancellationToken
    )
    {
        try
        {
            var putRequest = new PutObjectRequest
            {
                BucketName = _options.BucketName,
                Key = request.ObjectKey,
                InputStream = request.Content,
                AutoCloseStream = false,
                ContentType = request.ContentType,
            };
            putRequest.Headers.CacheControl = "public,max-age=31536000,immutable";
            var response = await client.PutObjectAsync(putRequest, cancellationToken);

            return new StoredObject(
                request.ObjectKey,
                BuildPublicUri(request.ObjectKey),
                request.ContentType,
                request.ContentLength,
                response.ETag,
                response.VersionId
            );
        }
        catch (AmazonS3Exception exception)
        {
            throw new ObjectStorageException("The object could not be stored.", exception);
        }
    }

    public async Task DeleteAsync(string objectKey, CancellationToken cancellationToken)
    {
        var validatedKey = ValidateKey(objectKey);
        try
        {
            await client.DeleteObjectAsync(_options.BucketName, validatedKey, cancellationToken);
        }
        catch (AmazonS3Exception exception)
        {
            throw new ObjectStorageException("The object could not be deleted.", exception);
        }
    }

    private Uri BuildPublicUri(string objectKey)
    {
        var baseUri = new Uri(_options.PublicBaseUrl.TrimEnd('/') + "/", UriKind.Absolute);
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
