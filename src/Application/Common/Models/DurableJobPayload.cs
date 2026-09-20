namespace modular_mlm.Application.Common.Models;

public sealed record DurableJobPayload
{
    public DurableJobPayload(
        Guid jobId,
        Guid? organizationId,
        string jobName,
        string payloadJson,
        string idempotencyKey,
        Guid correlationId,
        DateTimeOffset createdAt,
        DateTimeOffset? notBefore
    )
    {
        if (jobId == Guid.Empty)
            throw new ArgumentException("A durable job ID is required.", nameof(jobId));
        if (organizationId == Guid.Empty)
            throw new ArgumentException(
                "Organization ID must be null or non-empty.",
                nameof(organizationId)
            );
        JobName = DurableJobRegistry.Normalize(jobName);
        ArgumentException.ThrowIfNullOrWhiteSpace(payloadJson);
        ArgumentException.ThrowIfNullOrWhiteSpace(idempotencyKey);
        if (correlationId == Guid.Empty)
            throw new ArgumentException("A correlation ID is required.", nameof(correlationId));
        if (notBefore < createdAt)
            throw new ArgumentException("NotBefore cannot precede CreatedAt.", nameof(notBefore));

        JobId = jobId;
        OrganizationId = organizationId;
        PayloadJson = payloadJson;
        IdempotencyKey = idempotencyKey.Trim();
        CorrelationId = correlationId;
        CreatedAt = createdAt;
        NotBefore = notBefore;
    }

    public Guid JobId { get; }
    public Guid? OrganizationId { get; }
    public string JobName { get; }
    public string PayloadJson { get; }
    public string IdempotencyKey { get; }
    public Guid CorrelationId { get; }
    public DateTimeOffset CreatedAt { get; }
    public DateTimeOffset? NotBefore { get; }
}

public static class DurableJobRegistry
{
    public const string ProcessCommissionPayout = "process-payout";
    public const string ReconcileRefundVolume = "reconcile-refund-volume";
    public const string SendInvitationEmail = "send-invitation-email";

    private static readonly HashSet<string> ValidJobs = new(StringComparer.OrdinalIgnoreCase)
    {
        ProcessCommissionPayout,
        ReconcileRefundVolume,
        SendInvitationEmail,
    };

    public static bool IsValidJobName(string? jobName) =>
        !string.IsNullOrWhiteSpace(jobName) && ValidJobs.Contains(jobName.Trim());

    public static string Normalize(string jobName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(jobName);
        var normalized = jobName.Trim().ToLowerInvariant();
        if (!ValidJobs.Contains(normalized))
            throw new ArgumentOutOfRangeException(nameof(jobName), jobName, "Unknown job name.");
        return normalized;
    }
}
