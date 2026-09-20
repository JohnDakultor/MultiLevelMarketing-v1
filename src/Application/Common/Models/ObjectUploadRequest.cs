namespace modular_mlm.Application.Common.Models;

/// <summary>
/// Describes content to store under a server-generated key.
/// The caller owns <see cref="Content"/> and disposes it after the storage operation completes.
/// </summary>
public sealed record ObjectUploadRequest
{
    public const int MaximumObjectKeyLength = 1_024;

    public ObjectUploadRequest(
        string objectKey,
        string contentType,
        long contentLength,
        Stream content
    )
    {
        ObjectKey = ValidateObjectKey(objectKey);
        ContentType = ValidateContentType(contentType);
        if (contentLength <= 0)
            throw new ArgumentOutOfRangeException(
                nameof(contentLength),
                "Content length must be positive."
            );

        ArgumentNullException.ThrowIfNull(content);
        if (!content.CanRead)
            throw new ArgumentException("Content stream must be readable.", nameof(content));

        ContentLength = contentLength;
        Content = content;
    }

    public string ObjectKey { get; }
    public string ContentType { get; }
    public long ContentLength { get; }
    public Stream Content { get; }

    internal static string ValidateObjectKey(string objectKey)
    {
        if (string.IsNullOrWhiteSpace(objectKey))
            throw new ArgumentException("Object key is required.", nameof(objectKey));

        var normalized = objectKey.Trim();
        if (normalized.Length > MaximumObjectKeyLength)
            throw new ArgumentException("Object key is too long.", nameof(objectKey));
        if (
            normalized.StartsWith('/')
            || normalized.EndsWith('/')
            || normalized.Contains('\\')
            || normalized.Any(char.IsControl)
            || normalized.Split('/').Any(segment => segment is "" or "." or "..")
        )
        {
            throw new ArgumentException("Object key contains an unsafe path.", nameof(objectKey));
        }

        return normalized;
    }

    internal static string ValidateContentType(string contentType)
    {
        if (string.IsNullOrWhiteSpace(contentType))
            throw new ArgumentException("Content type is required.", nameof(contentType));

        var normalized = contentType.Trim().ToLowerInvariant();
        var separatorIndex = normalized.IndexOf('/');
        if (
            separatorIndex <= 0
            || separatorIndex == normalized.Length - 1
            || normalized.IndexOf('/', separatorIndex + 1) >= 0
            || normalized.Any(character =>
                char.IsWhiteSpace(character) || char.IsControl(character) || character == ';'
            )
        )
        {
            throw new ArgumentException("Content type is invalid.", nameof(contentType));
        }

        return normalized;
    }
}
