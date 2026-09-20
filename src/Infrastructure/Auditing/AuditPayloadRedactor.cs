using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using modular_mlm.Application.Common.Interfaces;

namespace modular_mlm.Infrastructure.Auditing;

public sealed class AuditPayloadRedactor : IAuditPayloadRedactor
{
    public const string RedactedMarker = "[REDACTED_BY_AUDIT_POLICY]";
    public const string InvalidPayloadMarker = "[UNPARSABLE_DATA_REDACTED_FOR_SECURITY]";
    public const string OversizedPayloadMarker = "[OVERSIZED_DATA_REDACTED_FOR_SECURITY]";

    private const int MaximumPayloadLength = 64 * 1024;
    private const int MaximumDepth = 32;
    private const int MaximumTextLength = 4 * 1024;
    private static readonly Regex SensitiveTextPattern = new(
        @"(?ix)\b(password|token|secret|credential|authorization|account[-_ ]?(?:number|name)|protected[-_ ]?account|webhook[-_ ]?signature)\b\s*[:=]\s*(?:Bearer\s+)?\S+",
        RegexOptions.Compiled | RegexOptions.CultureInvariant,
        TimeSpan.FromMilliseconds(100)
    );
    private static readonly Regex BearerPattern = new(
        @"(?i)\bBearer\s+\S+",
        RegexOptions.Compiled | RegexOptions.CultureInvariant,
        TimeSpan.FromMilliseconds(100)
    );
    private static readonly string[] SensitiveFragments =
    [
        "password",
        "token",
        "secret",
        "credential",
        "authorization",
        "accountnumber",
        "accountname",
        "protectedaccount",
        "webhooksignature",
        "providerpayload",
        "rawtoken",
    ];

    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        MaxDepth = MaximumDepth,
    };

    public string? SerializeAndRedact(object? value)
    {
        if (value is null)
            return null;

        try
        {
            return RedactJson(JsonSerializer.Serialize(value, SerializerOptions));
        }
        catch (JsonException)
        {
            return InvalidPayloadMarker;
        }
        catch (NotSupportedException)
        {
            return InvalidPayloadMarker;
        }
    }

    public string? RedactJson(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
            return null;
        if (json.Length > MaximumPayloadLength)
            return JsonSerializer.Serialize(OversizedPayloadMarker);

        try
        {
            var node = JsonNode.Parse(
                json,
                documentOptions: new JsonDocumentOptions { MaxDepth = MaximumDepth }
            );
            Redact(node, depth: 0);
            return node?.ToJsonString(SerializerOptions);
        }
        catch (JsonException)
        {
            return JsonSerializer.Serialize(InvalidPayloadMarker);
        }
    }

    public string? RedactText(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return null;
        if (value.Length > MaximumTextLength)
            return OversizedPayloadMarker;

        try
        {
            var redacted = SensitiveTextPattern.Replace(
                value,
                match => $"{match.Groups[1].Value}={RedactedMarker}"
            );
            return BearerPattern.Replace(redacted, $"Bearer {RedactedMarker}").Trim();
        }
        catch (RegexMatchTimeoutException)
        {
            return InvalidPayloadMarker;
        }
    }

    private static void Redact(JsonNode? node, int depth)
    {
        if (node is null)
            return;
        if (depth > MaximumDepth)
            throw new JsonException("Audit payload exceeds the maximum depth.");

        if (node is JsonObject jsonObject)
        {
            foreach (var property in jsonObject.ToList())
            {
                if (IsSensitive(property.Key))
                    jsonObject[property.Key] = RedactedMarker;
                else
                    Redact(property.Value, depth + 1);
            }
        }
        else if (node is JsonArray jsonArray)
        {
            foreach (var item in jsonArray)
                Redact(item, depth + 1);
        }
    }

    private static bool IsSensitive(string propertyName)
    {
        var normalized = new string(
            propertyName.Where(char.IsLetterOrDigit).Select(char.ToLowerInvariant).ToArray()
        );
        return SensitiveFragments.Any(normalized.Contains);
    }
}
