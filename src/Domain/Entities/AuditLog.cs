using System;
using System.Text.Json;
using modular_mlm.Domain.Exceptions;

namespace modular_mlm.Domain.AuditLogs;

public sealed class AuditLog : BaseEntity
{
    private AuditLog() { }

    public Guid OrganizationId { get; private set; }
    public Guid ActorUserId { get; private set; }
    public string Action { get; private set; } = null!;
    public string EntityType { get; private set; } = null!;
    public Guid EntityId { get; private set; }
    public string? BeforeJson { get; private set; }
    public string? AfterJson { get; private set; }
    public string? Reason { get; private set; }
    public string? IpAddress { get; private set; }
    public string? UserAgent { get; private set; }
    public string? TraceId { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }

    public static AuditLog Record(
        Guid organizationId,
        Guid actorUserId,
        string action,
        string entityType,
        Guid entityId,
        string? beforeJson,
        string? afterJson,
        string? reason,
        string? ipAddress,
        string? userAgent,
        string? traceId
    )
    {
        if (organizationId == Guid.Empty)
            throw new DomainInvariantException("OrganizationId is required for audit logging.");
        if (actorUserId == Guid.Empty)
            throw new DomainInvariantException("ActorUserId is required.");
        if (entityId == Guid.Empty)
            throw new DomainInvariantException("EntityId is required.");
        if (string.IsNullOrWhiteSpace(action))
            throw new DomainInvariantException("Action must be provided.");
        if (string.IsNullOrWhiteSpace(entityType))
            throw new DomainInvariantException("EntityType must be provided.");

        var normalizedAction = action.Trim().ToUpperInvariant();
        var normalizedEntityType = entityType.Trim();
        var cleanReason = string.IsNullOrWhiteSpace(reason) ? null : reason.Trim();
        var cleanIp = string.IsNullOrWhiteSpace(ipAddress) ? "0.0.0.0" : ipAddress.Trim();
        var cleanAgent = string.IsNullOrWhiteSpace(userAgent) ? "Unknown" : userAgent.Trim();

        bool hasStateChange = !string.Equals(beforeJson, afterJson, StringComparison.Ordinal);
        if (!hasStateChange && string.IsNullOrWhiteSpace(cleanReason))
        {
            throw new DomainInvariantException(
                "Audit log rejected: Changes require an explicit reason if state fields remain identical."
            );
        }

        var sanitizedBefore = SanitizePayload(beforeJson);
        var sanitizedAfter = SanitizePayload(afterJson);

        return new AuditLog
        {
            OrganizationId = organizationId,
            ActorUserId = actorUserId,
            Action = normalizedAction,
            EntityType = normalizedEntityType,
            EntityId = entityId,
            BeforeJson = sanitizedBefore,
            AfterJson = sanitizedAfter,
            Reason = cleanReason,
            IpAddress = cleanIp,
            UserAgent = cleanAgent,
            TraceId = string.IsNullOrWhiteSpace(traceId) ? null : traceId.Trim(),
            CreatedAt = DateTimeOffset.UtcNow, // Timestamp generated at time of logging assertion
        };
    }

    /// <summary>
    /// Scrub structural data fields to avoid storing high-risk PII or infrastructure credentials.
    /// </summary>
    private static string? SanitizePayload(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
            return null;

        // Fast preliminary check before allocating JSON document memory overhead
        if (
            !json.Contains("password", StringComparison.OrdinalIgnoreCase)
            && !json.Contains("token", StringComparison.OrdinalIgnoreCase)
            && !json.Contains("secret", StringComparison.OrdinalIgnoreCase)
            && !json.Contains("payout", StringComparison.OrdinalIgnoreCase)
        )
        {
            return json;
        }

        try
        {
            // Simple generic dictionary parser safely stripping keys out
            using var document = JsonDocument.Parse(json);
            var root = document.RootElement;

            if (root.ValueKind != JsonValueKind.Object)
                return json;

            var options = new JsonWriterOptions { Indented = false };
            using var stream = new System.IO.MemoryStream();
            using (var writer = new Utf8JsonWriter(stream, options))
            {
                writer.WriteStartObject();
                foreach (var property in root.EnumerateObject())
                {
                    string name = property.Name.ToLowerInvariant();
                    if (
                        name.Contains("password")
                        || name.Contains("token")
                        || name.Contains("secret")
                        || name.Contains("payout")
                    )
                    {
                        writer.WriteString(property.Name, "[REDACTED_BY_AUDIT_POLICY]");
                    }
                    else
                    {
                        property.WriteTo(writer);
                    }
                }
                writer.WriteEndObject();
            }
            return System.Text.Encoding.UTF8.GetString(stream.ToArray());
        }
        catch
        {
            // Fallback safety barrier if JSON is malformed raw text
            return "[UNPARSABLE_DATA_REDACTED_FOR_SECURITY]";
        }
    }
}
