namespace modular_mlm.Infrastructure.Storage;

public sealed class S3ObjectStorageOptions
{
    public const string SectionName = "ObjectStorage:S3";

    public bool Enabled { get; init; }
    public string? ServiceUrl { get; init; }
    public string Region { get; init; } = "ap-southeast-1";
    public string BucketName { get; init; } = string.Empty;
    public string PublicBaseUrl { get; init; } = string.Empty;
    public string AccessKey { get; init; } = string.Empty;
    public string SecretKey { get; init; } = string.Empty;
    public bool ForcePathStyle { get; init; }
}
