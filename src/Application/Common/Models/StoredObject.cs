namespace modular_mlm.Application.Common.Models;

/// <summary>
/// Public metadata describing a successfully persisted object.
/// </summary>
public sealed record StoredObject
{
    public StoredObject(
        string objectKey,
        Uri url,
        string contentType,
        long contentLength,
        string? eTag = null,
        string? versionId = null
    )
    {
        ObjectKey = ObjectUploadRequest.ValidateObjectKey(objectKey);
        ArgumentNullException.ThrowIfNull(url);
        if (
            !url.IsAbsoluteUri
            || (url.Scheme != Uri.UriSchemeHttps && url.Scheme != Uri.UriSchemeHttp)
            || !string.IsNullOrEmpty(url.UserInfo)
        )
        {
            throw new ArgumentException(
                "Stored object URL must be an absolute HTTP URL without embedded credentials.",
                nameof(url)
            );
        }

        ContentType = ObjectUploadRequest.ValidateContentType(contentType);
        if (contentLength <= 0)
            throw new ArgumentOutOfRangeException(
                nameof(contentLength),
                "Content length must be positive."
            );

        Url = url;
        ContentLength = contentLength;
        ETag = NormalizeOptionalMetadata(eTag);
        VersionId = NormalizeOptionalMetadata(versionId);
    }

    public string ObjectKey { get; }
    public Uri Url { get; }
    public string ContentType { get; }
    public long ContentLength { get; }
    public string? ETag { get; }
    public string? VersionId { get; }

    private static string? NormalizeOptionalMetadata(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
