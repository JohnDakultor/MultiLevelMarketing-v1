using System.Globalization;
using System.Net;
using modular_mlm.Domain.Events;
using modular_mlm.Domain.Exceptions;

namespace modular_mlm.Domain.Organizations;

public sealed class OrganizationDomain : OrganizationEntity
{
    public const int MaximumHostNameLength = 253;

    private OrganizationDomain() { }

    public string HostName { get; private set; } = string.Empty;
    public bool IsPrimary { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset? VerifiedAt { get; private set; }
    public bool IsVerified => VerifiedAt.HasValue;

    public static OrganizationDomain Create(
        Guid organizationId,
        string hostName,
        bool isPrimary,
        DateTimeOffset createdAt
    )
    {
        if (organizationId == Guid.Empty)
            throw new DomainInvariantException("Organization is required.");
        if (createdAt == default)
            throw new DomainInvariantException("Domain creation time is required.");

        var domain = new OrganizationDomain
        {
            OrganizationId = organizationId,
            HostName = NormalizeHostName(hostName),
            IsPrimary = isPrimary,
            CreatedAt = createdAt,
        };

        domain.RaiseConfiguredEvent(createdAt);
        return domain;
    }

    public bool Verify(DateTimeOffset verifiedAt)
    {
        if (verifiedAt < CreatedAt)
            throw new DomainInvariantException("Verification cannot precede domain creation.");
        if (VerifiedAt.HasValue)
            return false;

        VerifiedAt = verifiedAt;
        RaiseConfiguredEvent(verifiedAt);
        return true;
    }

    public bool MakePrimary(DateTimeOffset occurredAt)
    {
        EnsureEventTime(occurredAt);
        if (IsPrimary)
            return false;

        IsPrimary = true;
        RaiseConfiguredEvent(occurredAt);
        return true;
    }

    public bool Demote(DateTimeOffset occurredAt)
    {
        EnsureEventTime(occurredAt);
        if (!IsPrimary)
            return false;

        IsPrimary = false;
        RaiseConfiguredEvent(occurredAt);
        return true;
    }

    public static string NormalizeHostName(string hostName)
    {
        if (string.IsNullOrWhiteSpace(hostName))
            throw new DomainInvariantException("Hostname is required.");

        var candidate = hostName.Trim().TrimEnd('.');
        if (
            candidate.Contains("://", StringComparison.Ordinal)
            || candidate.IndexOfAny(['/', '\\', '?', '#', '@', ':', '*']) >= 0
        )
        {
            throw new DomainInvariantException("A hostname must not contain URL components.");
        }

        string normalized;
        try
        {
            normalized = new IdnMapping().GetAscii(candidate).ToLowerInvariant();
        }
        catch (ArgumentException)
        {
            throw new DomainInvariantException("Hostname is invalid.");
        }

        if (normalized.Length is 0 or > MaximumHostNameLength)
            throw new DomainInvariantException("Hostname length is invalid.");
        if (
            normalized.Equals("localhost", StringComparison.Ordinal)
            || normalized.EndsWith(".localhost", StringComparison.Ordinal)
            || IPAddress.TryParse(normalized, out _)
            || Uri.CheckHostName(normalized) != UriHostNameType.Dns
        )
        {
            throw new DomainInvariantException("A valid public DNS hostname is required.");
        }

        foreach (var label in normalized.Split('.'))
        {
            if (
                label.Length is 0 or > 63
                || label[0] == '-'
                || label[^1] == '-'
                || label.Any(character => !char.IsAsciiLetterOrDigit(character) && character != '-')
            )
            {
                throw new DomainInvariantException("Hostname contains an invalid DNS label.");
            }
        }

        return normalized;
    }

    private void EnsureEventTime(DateTimeOffset occurredAt)
    {
        if (occurredAt < CreatedAt)
            throw new DomainInvariantException("Domain change cannot precede domain creation.");
    }

    private void RaiseConfiguredEvent(DateTimeOffset occurredAt) =>
        AddDomainEvent(
            new OrganizationDomainConfiguredEvent(
                OrganizationId,
                Id,
                HostName,
                IsPrimary,
                IsVerified,
                occurredAt
            )
        );
}
