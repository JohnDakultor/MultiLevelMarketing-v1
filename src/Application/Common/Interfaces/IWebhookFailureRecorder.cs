namespace modular_mlm.Application.Common.Interfaces;

public interface IWebhookFailureRecorder
{
    Task RecordAsync(
        string provider,
        string providerEventId,
        string eventType,
        string providerResourceId,
        string resourceKind,
        string payloadHash,
        string error,
        CancellationToken cancellationToken
    );
}
