namespace modular_mlm.Application.Common.Exceptions;

public sealed class ProviderUnavailableException : Exception
{
    public ProviderUnavailableException(string capability, TimeSpan? retryAfter = null)
        : this(capability, retryAfter, null) { }

    public ProviderUnavailableException(
        string capability,
        TimeSpan? retryAfter,
        Exception? innerException
    )
        : base(CreateMessage(capability), innerException)
    {
        if (retryAfter < TimeSpan.Zero)
            throw new ArgumentOutOfRangeException(
                nameof(retryAfter),
                "Retry-after duration cannot be negative."
            );

        Capability = NormalizeCapability(capability);
        RetryAfter = retryAfter;
    }

    public string Capability { get; }
    public TimeSpan? RetryAfter { get; }

    private static string CreateMessage(string capability) =>
        $"The {NormalizeCapability(capability)} provider is temporarily unavailable.";

    private static string NormalizeCapability(string capability)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(capability);
        return capability.Trim().ToLowerInvariant();
    }
}
