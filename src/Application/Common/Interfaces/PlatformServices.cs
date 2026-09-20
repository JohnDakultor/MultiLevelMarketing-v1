namespace modular_mlm.Application.Common.Interfaces;

public interface ICurrentOrganization
{
    Guid? Id { get; }
}

public interface IClock
{
    DateTimeOffset UtcNow { get; }
}

public sealed record PaymentCheckoutLineItem(
    string Name,
    long AmountInMinorUnits,
    string Currency,
    int Quantity
);

public sealed record CreatePaymentCheckoutRequest(
    Guid OrderId,
    string OrderNumber,
    IReadOnlyList<PaymentCheckoutLineItem> LineItems,
    IReadOnlyList<string> PaymentMethodTypes,
    Uri SuccessUrl,
    Uri CancelUrl,
    string IdempotencyKey
);

public sealed record PaymentCheckoutSession(string Id, Uri CheckoutUrl);

public sealed record PaymentWebhookNotification(
    string EventId,
    string EventType,
    string CheckoutSessionId,
    string ReferenceNumber,
    string? PaymentId,
    DateTimeOffset OccurredAt,
    string PayloadHash
);

public sealed record PaymentProviderState(
    string ProviderPaymentId,
    string Status,
    long AmountInMinorUnits,
    string Currency,
    DateTimeOffset? PaidAt,
    long RefundedAmountInMinorUnits
);

public sealed record CreatePaymentRefundRequest(
    string ProviderPaymentId,
    long AmountInMinorUnits,
    string Reason,
    string IdempotencyKey
);

public sealed record PaymentRefundResult(string ProviderRefundId, string Status);

public interface IPaymentGateway
{
    Task<PaymentCheckoutSession> CreateCheckoutAsync(
        CreatePaymentCheckoutRequest request,
        CancellationToken cancellationToken
    );

    PaymentWebhookNotification? VerifyAndParseWebhook(string payload, string signatureHeader);

    bool VerifyWebhookSignature(string payload, string signatureHeader);

    Task<PaymentProviderState> GetPaymentAsync(
        string providerPaymentId,
        CancellationToken cancellationToken
    );

    Task<PaymentRefundResult> RefundAsync(
        CreatePaymentRefundRequest request,
        CancellationToken cancellationToken
    );
}

public interface IEmailSender
{
    Task SendAsync(
        string recipient,
        string subject,
        string body,
        CancellationToken cancellationToken
    );
}

public interface IAdministratorInvitationDelivery
{
    Task SendAsync(
        Guid invitationId,
        string recipient,
        string rawToken,
        CancellationToken cancellationToken
    );
}

public interface IAdministratorInvitationPolicy
{
    TimeSpan Lifetime { get; }
}

public interface IBackgroundJobScheduler
{
    Task EnqueueAsync(string jobName, object payload, CancellationToken cancellationToken);
}

public interface IOutboxPublisher
{
    Task PublishAsync(object message, CancellationToken cancellationToken);
}

public interface ICache
{
    Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken);
    Task SetAsync<T>(string key, T value, TimeSpan lifetime, CancellationToken cancellationToken);
    Task RemoveByPrefixAsync(string prefix, CancellationToken cancellationToken);
}

public sealed record PayoutDestination(
    string AccountName,
    string AccountNumber,
    string BankCode,
    string Rail
);

public sealed record CreateProviderPayoutRequest(
    Guid PayoutRequestId,
    long AmountInMinorUnits,
    string Currency,
    PayoutDestination Destination,
    string IdempotencyKey
);

public sealed record ProviderPayoutResult(
    string BatchId,
    string TransferId,
    string Status,
    string? ProviderReference,
    string? FailureCode,
    string? FailureMessage
);

public interface IPayoutProvider
{
    Task<ProviderPayoutResult> CreateAsync(
        CreateProviderPayoutRequest request,
        CancellationToken cancellationToken
    );

    Task<ProviderPayoutResult> GetStatusAsync(
        string transferId,
        CancellationToken cancellationToken
    );
}

public interface IPayoutAccountProtector
{
    string Protect(string value);
    string Unprotect(string protectedValue);
}
