// PHASE 10 PSEUDOCODE ONLY
// DEFINE DatabaseConcurrencyConflictException for an expected-version or EF concurrency failure.
// ACCEPT a stable resource type and optional public resource identifier.
// EXPOSE no provider SQL, table names, or internal exception details.
// MAP to HTTP 409 with error code "concurrency_conflict" in the Web exception handler.

namespace modular_mlm.Application.Common.Exceptions;

public sealed class DatabaseConcurrencyConflictException : Exception
{
    public string ResourceType { get; }

    public string? ResourceId { get; }

    public DatabaseConcurrencyConflictException(string resourceType, string? resourceId = null)
        : base(BuildMessage(resourceType, resourceId))
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(resourceType);

        ResourceType = resourceType;
        ResourceId = resourceId;
    }

    private static string BuildMessage(string resourceType, string? resourceId)
    {
        return string.IsNullOrWhiteSpace(resourceId)
            ? $"The {resourceType} was modified by another operation."
            : $"The {resourceType} '{resourceId}' was modified by another operation.";
    }
}
