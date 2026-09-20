namespace modular_mlm.Infrastructure.Messaging;

public sealed class ProcessedMessage
{
    private ProcessedMessage() { }

    public Guid MessageId { get; private set; }
    public string ConsumerName { get; private set; } = string.Empty;
    public DateTimeOffset ProcessedAt { get; private set; }

    public static ProcessedMessage Create(
        Guid messageId,
        string consumerName,
        DateTimeOffset processedAt
    ) =>
        messageId == Guid.Empty || string.IsNullOrWhiteSpace(consumerName)
            ? throw new ArgumentException("Processed-message identity is required.")
            : new ProcessedMessage
            {
                MessageId = messageId,
                ConsumerName = consumerName.Trim(),
                ProcessedAt = processedAt,
            };
}
