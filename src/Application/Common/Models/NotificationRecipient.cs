using System.Collections.ObjectModel;

namespace modular_mlm.Application.Common.Models;

public sealed record NotificationRecipient
{
    public NotificationRecipient(
        string userId,
        string email,
        string displayName,
        string culture,
        bool emailConfirmed,
        IReadOnlyCollection<string>? enabledNotificationKinds = null
    )
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(userId);
        ArgumentException.ThrowIfNullOrWhiteSpace(email);
        ArgumentException.ThrowIfNullOrWhiteSpace(displayName);
        ArgumentException.ThrowIfNullOrWhiteSpace(culture);

        UserId = userId.Trim();
        Email = email.Trim();
        DisplayName = displayName.Trim();
        Culture = culture.Trim();
        EmailConfirmed = emailConfirmed;
        EnabledNotificationKinds = new ReadOnlyCollection<string>(
            (enabledNotificationKinds ?? [])
                .Where(kind => !string.IsNullOrWhiteSpace(kind))
                .Select(kind => kind.Trim())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToArray()
        );
    }

    public string UserId { get; }
    public string Email { get; }
    public string DisplayName { get; }
    public string Culture { get; }
    public bool EmailConfirmed { get; }
    public IReadOnlyCollection<string> EnabledNotificationKinds { get; }

    public bool Allows(string notificationKind) =>
        EnabledNotificationKinds.Count == 0
        || EnabledNotificationKinds.Contains(notificationKind, StringComparer.OrdinalIgnoreCase);
}
