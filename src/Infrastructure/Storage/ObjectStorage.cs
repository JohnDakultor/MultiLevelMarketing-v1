using modular_mlm.Application.Common.Interfaces;
using modular_mlm.Application.Common.Models;

namespace modular_mlm.Infrastructure.Storage;

public sealed class ObjectStorage : IObjectStorage
{
    public Task<StoredObject> PutAsync(
        ObjectUploadRequest request,
        CancellationToken cancellationToken
    ) =>
        throw new InvalidOperationException(
            "Configure S3-compatible object storage before uploading marketplace assets."
        );

    public Task DeleteAsync(string objectKey, CancellationToken cancellationToken) =>
        throw new InvalidOperationException(
            "Configure S3-compatible object storage before deleting marketplace assets."
        );
}
