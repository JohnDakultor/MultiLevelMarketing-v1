namespace modular_mlm.Application.Common.Exceptions;

public sealed class ProviderTimeoutException : Exception
{
    public ProviderTimeoutException(string capability)
        : this(capability, null) { }

    public ProviderTimeoutException(string capability, Exception? innerException)
        : base(CreateMessage(capability), innerException)
    {
        Capability = NormalizeCapability(capability);
    }

    public string Capability { get; }

    private static string CreateMessage(string capability) =>
        $"The {NormalizeCapability(capability)} provider timed out.";

    private static string NormalizeCapability(string capability)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(capability);
        return capability.Trim().ToLowerInvariant();
    }
}
