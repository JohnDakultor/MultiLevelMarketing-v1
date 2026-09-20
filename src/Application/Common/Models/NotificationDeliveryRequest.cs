using System.Collections.ObjectModel;

namespace modular_mlm.Application.Common.Models;

public enum NotificationChannel
{
    Email = 1,
}

public sealed record NotificationDeliveryRequest
{
    private const int MaximumVariableCount = 32;
    private const int MaximumVariableLength = 4_096;

    public NotificationDeliveryRequest(
        Guid notificationId,
        Guid organizationId,
        string recipientUserId,
        string recipientAddress,
        NotificationChannel channel,
        string templateKey,
        string culture,
        IReadOnlyDictionary<string, string>? variables = null
    )
    {
        if (notificationId == Guid.Empty)
            throw new ArgumentException("A notification ID is required.", nameof(notificationId));
        if (organizationId == Guid.Empty)
            throw new ArgumentException("An organization ID is required.", nameof(organizationId));
        ArgumentException.ThrowIfNullOrWhiteSpace(recipientUserId);
        ArgumentException.ThrowIfNullOrWhiteSpace(recipientAddress);
        ArgumentException.ThrowIfNullOrWhiteSpace(templateKey);
        ArgumentException.ThrowIfNullOrWhiteSpace(culture);
        if (!Enum.IsDefined(channel))
            throw new ArgumentOutOfRangeException(nameof(channel));

        var copiedVariables = variables is null
            ? new Dictionary<string, string>(StringComparer.Ordinal)
            : new Dictionary<string, string>(variables, StringComparer.Ordinal);
        if (copiedVariables.Count > MaximumVariableCount)
            throw new ArgumentException(
                $"A notification cannot contain more than {MaximumVariableCount} variables.",
                nameof(variables)
            );
        if (
            copiedVariables.Any(variable =>
                string.IsNullOrWhiteSpace(variable.Key)
                || variable.Key.Length > 100
                || variable.Value is null
                || variable.Value.Length > MaximumVariableLength
            )
        )
            throw new ArgumentException(
                "Notification variables are invalid or too large.",
                nameof(variables)
            );

        NotificationId = notificationId;
        OrganizationId = organizationId;
        RecipientUserId = recipientUserId.Trim();
        RecipientAddress = recipientAddress.Trim();
        Channel = channel;
        TemplateKey = templateKey.Trim();
        Culture = culture.Trim();
        Variables = new ReadOnlyDictionary<string, string>(copiedVariables);
    }

    public Guid NotificationId { get; }
    public Guid OrganizationId { get; }
    public string RecipientUserId { get; }
    public string RecipientAddress { get; }
    public NotificationChannel Channel { get; }
    public string TemplateKey { get; }
    public string Culture { get; }
    public IReadOnlyDictionary<string, string> Variables { get; }
}
