namespace modular_mlm.Infrastructure.Storage;

public sealed class AzureBlobObjectStorageOptions
{
    public const string SectionName = "ObjectStorage:AzureBlob";

    public bool Enabled { get; init; }
    public string PublicBaseUrl { get; init; } = string.Empty;
}
