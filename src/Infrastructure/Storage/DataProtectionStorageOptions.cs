namespace modular_mlm.Infrastructure.Storage;

public sealed class DataProtectionStorageOptions
{
    public const string SectionName = "DataProtection:AzureBlob";

    public bool Enabled { get; init; }
    public string ApplicationName { get; init; } = "modular-mlm";
    public string BlobName { get; init; } = modular_mlm.Shared.Services.DataProtectionKeyBlob;
}
