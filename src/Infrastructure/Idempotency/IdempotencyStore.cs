using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using modular_mlm.Application.Common.Exceptions;
using modular_mlm.Application.Common.Interfaces;
using modular_mlm.Application.Common.Models;
using modular_mlm.Domain.Idempotency;
using modular_mlm.Infrastructure.Data;

namespace modular_mlm.Infrastructure.Idempotency;

public sealed class IdempotencyStore : IIdempotencyStore
{
    private readonly ApplicationDbContext _db;
    private readonly IDataProtector _protector;
    private readonly IdempotencyOptions _options;
    private readonly TimeProvider _clock;

    public IdempotencyStore(
        ApplicationDbContext db,
        IDataProtectionProvider protectionProvider,
        IOptions<IdempotencyOptions> options,
        TimeProvider clock
    )
    {
        _db = db;
        _protector = protectionProvider.CreateProtector(
            "modular_mlm.Infrastructure.Idempotency.Outcome.v1"
        );
        _options = options.Value;
        _clock = clock;
    }

    public async Task<IdempotencyDecision> TryBeginAsync(
        Guid? organizationId,
        string scope,
        string key,
        string requestHash,
        DateTimeOffset expiresAt,
        CancellationToken cancellationToken
    )
    {
        var identity = Normalize(scope, key, requestHash);
        var now = _clock.GetUtcNow();
        EnsureFutureExpiration(expiresAt, now);
        var recordId = Guid.NewGuid();
        var processing = IdempotencyStatus.Processing.ToString();

        var inserted = await _db.Database.ExecuteSqlInterpolatedAsync(
            $"""
            INSERT INTO "IdempotencyRecords"
                ("Id", "OrganizationId", "Scope", "KeyHash", "RequestHash", "Status",
                 "StartedAt", "ExpiresAt", "CompletedAt", "StatusCode", "ProtectedOutcome", "Version")
            VALUES
                ({recordId}, {organizationId}, {identity.Scope}, {identity.KeyHash},
                 {identity.RequestHash}, {processing}, {now}, {expiresAt}, NULL, NULL, NULL, 0)
            ON CONFLICT DO NOTHING
            """,
            cancellationToken
        );
        if (inserted == 1)
            return IdempotencyDecision.Started(expiresAt);

        var existing =
            await FindAsync(organizationId, identity.Scope, identity.KeyHash, cancellationToken)
            ?? throw new InvalidOperationException(
                "The idempotency record could not be read after a concurrent acquisition."
            );

        if (existing.ExpiresAt <= now)
        {
            var restarted = await _db
                .IdempotencyRecords.Where(record =>
                    record.Id == existing.Id
                    && record.Version == existing.Version
                    && record.ExpiresAt <= now
                )
                .ExecuteUpdateAsync(
                    setters =>
                        setters
                            .SetProperty(record => record.RequestHash, identity.RequestHash)
                            .SetProperty(record => record.Status, IdempotencyStatus.Processing)
                            .SetProperty(record => record.StartedAt, now)
                            .SetProperty(record => record.ExpiresAt, expiresAt)
                            .SetProperty(record => record.CompletedAt, (DateTimeOffset?)null)
                            .SetProperty(record => record.StatusCode, (int?)null)
                            .SetProperty(record => record.ProtectedOutcome, (string?)null)
                            .SetProperty(record => record.Version, record => record.Version + 1),
                    cancellationToken
                );
            if (restarted == 1)
                return IdempotencyDecision.Started(expiresAt);

            existing =
                await FindAsync(organizationId, identity.Scope, identity.KeyHash, cancellationToken)
                ?? throw new InvalidOperationException(
                    "The idempotency record disappeared during acquisition."
                );
        }

        return ToDecision(existing, identity.RequestHash);
    }

    public async Task CompleteAsync(
        Guid? organizationId,
        string scope,
        string key,
        string requestHash,
        int statusCode,
        string? outcome,
        DateTimeOffset completedAt,
        CancellationToken cancellationToken
    )
    {
        var identity = Normalize(scope, key, requestHash);
        ValidateOutcome(outcome);
        var record = await FindTrackedAsync(
            organizationId,
            identity.Scope,
            identity.KeyHash,
            cancellationToken
        );
        EnsureMatchingRecord(record, identity.RequestHash);
        if (record!.Status == IdempotencyStatus.Completed)
            return;
        if (record.Status != IdempotencyStatus.Processing)
            throw new IdempotencyConflictException(
                "The idempotency operation is no longer processing."
            );

        record.Complete(
            identity.RequestHash,
            statusCode,
            outcome is null ? null : _protector.Protect(outcome),
            completedAt,
            completedAt.AddDays(_options.RetentionDays)
        );
        await SaveWithConcurrencyHandlingAsync(record, cancellationToken);
    }

    public async Task FailAsync(
        Guid? organizationId,
        string scope,
        string key,
        string requestHash,
        DateTimeOffset failedAt,
        DateTimeOffset expiresAt,
        CancellationToken cancellationToken
    )
    {
        var identity = Normalize(scope, key, requestHash);
        EnsureFutureExpiration(expiresAt, failedAt);
        var record = await FindTrackedAsync(
            organizationId,
            identity.Scope,
            identity.KeyHash,
            cancellationToken
        );
        EnsureMatchingRecord(record, identity.RequestHash);
        if (record!.Status == IdempotencyStatus.Completed)
            return;

        record.MarkFailedForRetry(identity.RequestHash, failedAt, expiresAt);
        await SaveWithConcurrencyHandlingAsync(record, cancellationToken);
    }

    public async Task<IdempotencyDecision?> GetAsync(
        Guid? organizationId,
        string scope,
        string key,
        string requestHash,
        CancellationToken cancellationToken
    )
    {
        var identity = Normalize(scope, key, requestHash);
        var record = await FindAsync(
            organizationId,
            identity.Scope,
            identity.KeyHash,
            cancellationToken
        );
        return record is null || record.ExpiresAt <= _clock.GetUtcNow()
            ? null
            : ToDecision(record, identity.RequestHash);
    }

    private async Task SaveWithConcurrencyHandlingAsync(
        IdempotencyRecord record,
        CancellationToken cancellationToken
    )
    {
        try
        {
            await _db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            _db.Entry(record).State = EntityState.Detached;
            throw new IdempotencyConflictException(
                "The idempotency operation changed concurrently."
            );
        }
    }

    private IdempotencyDecision ToDecision(IdempotencyRecord record, string requestHash)
    {
        if (!string.Equals(record.RequestHash, requestHash, StringComparison.Ordinal))
            return IdempotencyDecision.Conflict(record.ExpiresAt);

        return record.Status switch
        {
            IdempotencyStatus.Completed => IdempotencyDecision.Completed(
                record.StatusCode
                    ?? throw new InvalidOperationException(
                        "Completed idempotency record has no status code."
                    ),
                record.ProtectedOutcome is null
                    ? null
                    : _protector.Unprotect(record.ProtectedOutcome),
                record.ExpiresAt
            ),
            IdempotencyStatus.Processing or IdempotencyStatus.Failed =>
                IdempotencyDecision.InProgress(record.ExpiresAt),
            _ => throw new InvalidOperationException("Unknown idempotency status."),
        };
    }

    private Task<IdempotencyRecord?> FindAsync(
        Guid? organizationId,
        string scope,
        string keyHash,
        CancellationToken cancellationToken
    ) =>
        _db
            .IdempotencyRecords.AsNoTracking()
            .SingleOrDefaultAsync(
                record =>
                    record.OrganizationId == organizationId
                    && record.Scope == scope
                    && record.KeyHash == keyHash,
                cancellationToken
            );

    private Task<IdempotencyRecord?> FindTrackedAsync(
        Guid? organizationId,
        string scope,
        string keyHash,
        CancellationToken cancellationToken
    ) =>
        _db.IdempotencyRecords.SingleOrDefaultAsync(
            record =>
                record.OrganizationId == organizationId
                && record.Scope == scope
                && record.KeyHash == keyHash,
            cancellationToken
        );

    private static void EnsureMatchingRecord(IdempotencyRecord? record, string requestHash)
    {
        if (record is null)
            throw new KeyNotFoundException("Idempotency record was not found.");
        if (!string.Equals(record.RequestHash, requestHash, StringComparison.Ordinal))
            throw new IdempotencyConflictException(
                "The idempotency key belongs to a different request payload."
            );
    }

    private (string Scope, string KeyHash, string RequestHash) Normalize(
        string scope,
        string key,
        string requestHash
    )
    {
        var normalizedScope = Required(scope, nameof(scope)).ToLowerInvariant();
        var normalizedKey = Required(key, nameof(key));
        var normalizedRequestHash = Required(requestHash, nameof(requestHash));
        if (normalizedScope.Length > 200)
            throw new ArgumentException("Idempotency scope is too long.", nameof(scope));
        if (normalizedKey.Length > _options.MaximumKeyLength)
            throw new ArgumentException("Idempotency key is too long.", nameof(key));
        if (normalizedRequestHash.Length > 128)
            throw new ArgumentException("Request hash is too long.", nameof(requestHash));

        return (normalizedScope, Sha256(normalizedKey), Sha256(normalizedRequestHash));
    }

    private void ValidateOutcome(string? outcome)
    {
        if (
            outcome is not null
            && Encoding.UTF8.GetByteCount(outcome) > _options.MaximumOutcomeBytes
        )
            throw new ArgumentException("Idempotency outcome is too large.", nameof(outcome));
    }

    private static void EnsureFutureExpiration(
        DateTimeOffset expiresAt,
        DateTimeOffset referenceTime
    )
    {
        if (expiresAt <= referenceTime)
            throw new ArgumentOutOfRangeException(
                nameof(expiresAt),
                "Idempotency expiration must be in the future."
            );
    }

    private static string Required(string value, string parameterName) =>
        string.IsNullOrWhiteSpace(value)
            ? throw new ArgumentException("Value is required.", parameterName)
            : value.Trim().Normalize(NormalizationForm.FormKC);

    private static string Sha256(string value) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(value)));
}
