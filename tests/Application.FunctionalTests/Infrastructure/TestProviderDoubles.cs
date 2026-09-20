using modular_mlm.Application.Common.Interfaces;
using modular_mlm.Application.Common.Models;

namespace modular_mlm.Application.FunctionalTests.Infrastructure;

public sealed class TestAdministratorInvitationDelivery
    : IAdministratorInvitationDelivery,
        IInvitationDeliveryOutbox
{
    public Guid? InvitationId { get; private set; }
    public string? Recipient { get; private set; }
    public string? RawToken { get; private set; }

    public Task SendAsync(
        Guid invitationId,
        string recipient,
        string rawToken,
        CancellationToken cancellationToken
    )
    {
        InvitationId = invitationId;
        Recipient = recipient;
        RawToken = rawToken;
        return Task.CompletedTask;
    }

    public void Reset()
    {
        InvitationId = null;
        Recipient = null;
        RawToken = null;
    }

    public Task StageAsync(
        Guid invitationId,
        Guid organizationId,
        string recipient,
        string rawToken,
        string templateKey,
        DateTimeOffset expiresAt,
        CancellationToken cancellationToken
    ) => SendAsync(invitationId, recipient, rawToken, cancellationToken);

    public Task ActivateAsync(
        Guid organizationId,
        Guid invitationId,
        DateTimeOffset readyAt,
        CancellationToken cancellationToken
    ) => Task.CompletedTask;
}

public sealed class TestPaymentGateway : IPaymentGateway
{
    public PaymentProviderState? State { get; set; }
    public PaymentRefundResult RefundResult { get; set; } = new("refund_test", "pending");
    public bool IsWebhookSignatureValid { get; set; } = true;
    public CreatePaymentRefundRequest? LastRefundRequest { get; private set; }
    public int RefundCallCount { get; private set; }

    public Task<PaymentCheckoutSession> CreateCheckoutAsync(
        CreatePaymentCheckoutRequest request,
        CancellationToken cancellationToken
    ) =>
        Task.FromResult(
            new PaymentCheckoutSession(
                "checkout_test",
                new Uri("https://checkout.paymongo.com/checkout_test")
            )
        );

    public PaymentWebhookNotification? VerifyAndParseWebhook(
        string payload,
        string signatureHeader
    ) => null;

    public bool VerifyWebhookSignature(string payload, string signatureHeader) =>
        IsWebhookSignatureValid;

    public Task<PaymentProviderState> GetPaymentAsync(
        string providerPaymentId,
        CancellationToken cancellationToken
    ) =>
        Task.FromResult(
            State ?? throw new InvalidOperationException("Configure the test payment state first.")
        );

    public Task<PaymentRefundResult> RefundAsync(
        CreatePaymentRefundRequest request,
        CancellationToken cancellationToken
    )
    {
        LastRefundRequest = request;
        RefundCallCount++;
        return Task.FromResult(RefundResult);
    }

    public void Reset()
    {
        State = null;
        RefundResult = new PaymentRefundResult("refund_test", "pending");
        IsWebhookSignatureValid = true;
        LastRefundRequest = null;
        RefundCallCount = 0;
    }
}

public sealed class TestPayoutProvider : IPayoutProvider
{
    public ProviderPayoutResult CreateResult { get; set; } =
        new("batch_test", "transfer_test", "pending", null, null, null);
    public ProviderPayoutResult StatusResult { get; set; } =
        new("batch_test", "transfer_test", "pending", null, null, null);

    public Task<ProviderPayoutResult> CreateAsync(
        CreateProviderPayoutRequest request,
        CancellationToken cancellationToken
    ) => Task.FromResult(CreateResult);

    public Task<ProviderPayoutResult> GetStatusAsync(
        string transferId,
        CancellationToken cancellationToken
    ) => Task.FromResult(StatusResult);

    public void Reset()
    {
        CreateResult = new ProviderPayoutResult(
            "batch_test",
            "transfer_test",
            "pending",
            null,
            null,
            null
        );
        StatusResult = CreateResult;
    }
}

public sealed class TestPayoutAccountProtector : IPayoutAccountProtector
{
    public string Protect(string value) => value;

    public string Unprotect(string protectedValue) => protectedValue;
}

public sealed class TestObjectStorage : IObjectStorage
{
    public List<RecordedObjectUpload> Uploads { get; } = [];
    public List<string> DeletedObjectKeys { get; } = [];

    public async Task<StoredObject> PutAsync(
        ObjectUploadRequest request,
        CancellationToken cancellationToken
    )
    {
        using var buffer = new MemoryStream();
        await request.Content.CopyToAsync(buffer, cancellationToken);
        Uploads.Add(
            new RecordedObjectUpload(
                request.ObjectKey,
                request.ContentType,
                request.ContentLength,
                buffer.ToArray()
            )
        );
        return new StoredObject(
            request.ObjectKey,
            new Uri($"https://assets.test/{request.ObjectKey}"),
            request.ContentType,
            request.ContentLength,
            "test-etag"
        );
    }

    public Task DeleteAsync(string objectKey, CancellationToken cancellationToken)
    {
        DeletedObjectKeys.Add(objectKey);
        return Task.CompletedTask;
    }

    public void Reset()
    {
        Uploads.Clear();
        DeletedObjectKeys.Clear();
    }
}

public sealed record RecordedObjectUpload(
    string ObjectKey,
    string ContentType,
    long ContentLength,
    byte[] Content
);
