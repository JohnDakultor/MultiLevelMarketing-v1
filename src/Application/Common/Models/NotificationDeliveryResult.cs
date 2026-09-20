namespace modular_mlm.Application.Common.Models;

public sealed record NotificationDeliveryResult
{
    private NotificationDeliveryResult(
        bool succeeded,
        string? providerMessageId,
        string? failureCode,
        bool isTransient,
        TimeSpan? retryAfter
    )
    {
        if (succeeded && !string.IsNullOrWhiteSpace(failureCode))
            throw new ArgumentException("A successful delivery cannot have a failure code.");
        if (!succeeded && string.IsNullOrWhiteSpace(failureCode))
            throw new ArgumentException("A failed delivery requires a stable failure code.");
        if (!isTransient && retryAfter is not null)
            throw new ArgumentException("Only transient failures may specify RetryAfter.");
        if (retryAfter <= TimeSpan.Zero)
            throw new ArgumentOutOfRangeException(nameof(retryAfter));

        Succeeded = succeeded;
        ProviderMessageId = NullIfWhiteSpace(providerMessageId);
        FailureCode = NullIfWhiteSpace(failureCode);
        IsTransient = isTransient;
        RetryAfter = retryAfter;
    }

    public bool Succeeded { get; }
    public string? ProviderMessageId { get; }
    public string? FailureCode { get; }
    public bool IsTransient { get; }
    public TimeSpan? RetryAfter { get; }

    public static NotificationDeliveryResult Success(string? providerMessageId = null) =>
        new(true, providerMessageId, null, false, null);

    public static NotificationDeliveryResult TransientFailure(
        string failureCode,
        TimeSpan? retryAfter = null
    ) => new(false, null, failureCode, true, retryAfter);

    public static NotificationDeliveryResult PermanentFailure(string failureCode) =>
        new(false, null, failureCode, false, null);

    private static string? NullIfWhiteSpace(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
