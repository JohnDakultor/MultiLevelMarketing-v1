using System.Globalization;
using System.Net;

namespace modular_mlm.Infrastructure.Notifications;

public sealed record RenderedNotification(string Subject, string PlainTextBody, string HtmlBody);

public sealed class NotificationTemplateRenderer
{
    public const string AdministratorInvitation = "administrator-invitation";
    public const string OutboxDeadLetter = "outbox-dead-letter";

    private static readonly IReadOnlyDictionary<string, string[]> Variables = new Dictionary<
        string,
        string[]
    >(StringComparer.Ordinal)
    {
        ["commission-released"] = ["amount", "currency"],
        ["payout-requested"] = ["amount", "currency"],
        ["payout-approved"] = ["amount", "currency"],
        ["payout-completed"] = ["amount", "currency"],
        ["payout-failed"] = ["amount", "currency"],
        ["payout-failed-operations"] = ["payoutId", "failureCode"],
        ["order-paid"] = ["orderNumber"],
        ["refund-completed"] = ["amount", "currency"],
        [AdministratorInvitation] = ["invitationUrl"],
        [OutboxDeadLetter] = ["messageId", "messageType"],
    };

    public RenderedNotification Render(
        string templateKey,
        string culture,
        IReadOnlyDictionary<string, string> variables
    )
    {
        var key = templateKey.Trim().ToLowerInvariant();
        if (!Variables.TryGetValue(key, out var expected))
            throw new ArgumentOutOfRangeException(nameof(templateKey), "Unknown template.");
        if (
            variables.Count != expected.Length
            || expected.Any(name => !variables.ContainsKey(name))
            || variables.Keys.Any(name => !expected.Contains(name, StringComparer.Ordinal))
        )
            throw new ArgumentException("Template variables do not match the contract.");

        _ = ResolveCulture(culture);
        var safe = variables.ToDictionary(
            pair => pair.Key,
            pair => WebUtility.HtmlEncode(pair.Value),
            StringComparer.Ordinal
        );
        var plain = variables.ToDictionary(
            pair => pair.Key,
            pair => pair.Value.Replace("\r", string.Empty).Replace("\n", " "),
            StringComparer.Ordinal
        );

        return key switch
        {
            "commission-released" => Message(
                "Commission available",
                $"Your {Money(plain)} commission is now available.",
                $"Your <strong>{Money(safe)}</strong> commission is now available."
            ),
            "payout-requested" => Message(
                "Payout requires review",
                $"A payout request for {Money(plain)} is awaiting review.",
                $"A payout request for <strong>{Money(safe)}</strong> is awaiting review."
            ),
            "payout-approved" => Message(
                "Payout approved",
                $"Your payout request for {Money(plain)} was approved.",
                $"Your payout request for <strong>{Money(safe)}</strong> was approved."
            ),
            "payout-completed" => Message(
                "Payout completed",
                $"Your payout of {Money(plain)} was completed.",
                $"Your payout of <strong>{Money(safe)}</strong> was completed."
            ),
            "payout-failed" => Message(
                "Payout could not be completed",
                $"Your payout of {Money(plain)} could not be completed. Contact support.",
                $"Your payout of <strong>{Money(safe)}</strong> could not be completed. Contact support."
            ),
            "payout-failed-operations" => Message(
                "Payout processing alert",
                $"Payout {plain["payoutId"]} failed with category {plain["failureCode"]}.",
                $"Payout <strong>{safe["payoutId"]}</strong> failed with category {safe["failureCode"]}."
            ),
            "order-paid" => Message(
                "Payment received",
                $"Payment for order {plain["orderNumber"]} was received.",
                $"Payment for order <strong>{safe["orderNumber"]}</strong> was received."
            ),
            "refund-completed" => Message(
                "Refund completed",
                $"Your refund of {Money(plain)} was completed.",
                $"Your refund of <strong>{Money(safe)}</strong> was completed."
            ),
            AdministratorInvitation => Message(
                "You have been invited as an administrator",
                $"Accept your single-use invitation: {plain["invitationUrl"]}",
                $"<a href=\"{safe["invitationUrl"]}\">Accept your administrator invitation</a>"
            ),
            OutboxDeadLetter => Message(
                "Message processing requires attention",
                $"Message {plain["messageId"]} ({plain["messageType"]}) could not be processed.",
                $"Message <strong>{safe["messageId"]}</strong> ({safe["messageType"]}) could not be processed."
            ),
            _ => throw new ArgumentOutOfRangeException(nameof(templateKey)),
        };
    }

    private static RenderedNotification Message(string subject, string plain, string html) =>
        new(subject, plain, $"<p>{html}</p>");

    private static string Money(IReadOnlyDictionary<string, string> values) =>
        $"{values["currency"].ToUpperInvariant()} {values["amount"]}";

    private static CultureInfo ResolveCulture(string? culture)
    {
        try
        {
            return CultureInfo.GetCultureInfo(
                string.IsNullOrWhiteSpace(culture) ? "en-PH" : culture
            );
        }
        catch (CultureNotFoundException)
        {
            return CultureInfo.GetCultureInfo("en-PH");
        }
    }
}
