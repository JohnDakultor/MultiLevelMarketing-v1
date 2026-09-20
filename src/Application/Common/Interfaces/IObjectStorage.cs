using modular_mlm.Application.Common.Models;

namespace modular_mlm.Application.Common.Interfaces;

/// <summary>
/// Provides provider-neutral durable object storage operations.
/// </summary>
public interface IObjectStorage
{
    /// <summary>
    /// Stores the supplied content under its server-generated object key.
    /// The caller retains ownership of <see cref="ObjectUploadRequest.Content"/> and must dispose it
    /// after this operation completes. Implementations must not dispose the stream.
    /// </summary>
    Task<StoredObject> PutAsync(ObjectUploadRequest request, CancellationToken cancellationToken);

    /// <summary>
    /// Deletes an object as an idempotent compensation operation. A missing object is treated as
    /// already deleted.
    /// </summary>
    Task DeleteAsync(string objectKey, CancellationToken cancellationToken);
}
