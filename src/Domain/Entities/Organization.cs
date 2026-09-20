using System.Net.Mail;
using System.Text.RegularExpressions;
using modular_mlm.Domain.Events;
using modular_mlm.Domain.Exceptions;

namespace modular_mlm.Domain.Organizations;

public sealed class Organization : BaseAuditableEntity
{
    private static readonly Regex HexColorPattern = new(
        "^#[0-9A-Fa-f]{6}$",
        RegexOptions.Compiled | RegexOptions.CultureInvariant
    );
    private static readonly Regex SlugPattern = new(
        "^[a-z0-9]+(?:-[a-z0-9]+)*$",
        RegexOptions.Compiled | RegexOptions.CultureInvariant
    );

    private Organization() { }

    private readonly List<OrganizationDomain> _domains = [];

    public string Name { get; private set; } = string.Empty;
    public string Slug { get; private set; } = string.Empty;
    public OrganizationStatus Status { get; private set; }
    public string CurrencyCode { get; private set; } = string.Empty;
    public string TimeZone { get; private set; } = string.Empty;
    public string Locale { get; private set; } = string.Empty;
    public BrandingSettings Branding { get; private set; } = null!;
    public BrandingSettings? PublishedBranding { get; private set; }
    public FeatureSettings Features { get; private set; } = null!;
    public CommerceSettings Commerce { get; private set; } = null!;
    public NetworkSettings Network { get; private set; } = null!;

    public WalletSettings Wallet { get; private set; } = null!;
    public ReferralSettings Referrals { get; private set; } = null!;
    public int BrandingRevision { get; private set; }
    public int PublishedBrandingRevision { get; private set; }
    public DateTimeOffset? BrandingPublishedAt { get; private set; }
    public IReadOnlyCollection<OrganizationDomain> Domains => _domains.AsReadOnly();

    public static Organization Create(
        string name,
        string slug,
        string currencyCode,
        string timeZone = "UTC",
        string locale = "en",
        DateTimeOffset? provisionedAt = null
    )
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new DomainInvariantException("Organization name is required.");
        if (string.IsNullOrWhiteSpace(slug))
            throw new DomainInvariantException("Organization slug is required.");

        var normalizedSlug = slug.Trim().ToLowerInvariant();
        if (!SlugPattern.IsMatch(normalizedSlug))
            throw new DomainInvariantException("Organization slug is invalid.");

        var organization = new Organization
        {
            Slug = normalizedSlug,
            Status = OrganizationStatus.Active,
            Branding = BrandingSettings.Create(name, string.Empty),
            Features = FeatureSettings.Default(),
            Commerce = CommerceSettings.Default(),
            Network = NetworkSettings.Default(),
            Referrals = ReferralSettings.Default(),
            Wallet = WalletSettings.Default(),
            BrandingRevision = 1,
        };
        organization.UpdateProfile(name, currencyCode, timeZone, locale);

        organization.AddDomainEvent(
            new OrganizationProvisionedEvent(
                organization.Id,
                organization.Slug,
                provisionedAt ?? DateTimeOffset.UtcNow
            )
        );
        return organization;
    }

    public void Rename(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new DomainInvariantException("Organization name is required.");
        Name = name.Trim();
    }

    public void UpdateProfile(string name, string currencyCode, string timeZone, string locale)
    {
        Rename(name);
        if (
            string.IsNullOrWhiteSpace(currencyCode)
            || currencyCode.Trim().Length != 3
            || !currencyCode.Trim().All(char.IsAsciiLetter)
        )
        {
            throw new DomainInvariantException("Currency must be a three-letter ISO code.");
        }
        if (string.IsNullOrWhiteSpace(timeZone))
            throw new DomainInvariantException("Time zone is required.");
        if (string.IsNullOrWhiteSpace(locale))
            throw new DomainInvariantException("Locale is required.");

        CurrencyCode = currencyCode.Trim().ToUpperInvariant();
        TimeZone = timeZone.Trim();
        Locale = locale.Trim();
    }

    public void UpdateBranding(BrandingSettings branding)
    {
        Branding = branding ?? throw new ArgumentNullException(nameof(branding));
        BrandingRevision++;
    }

    public void UpdateFeatures(FeatureSettings features) =>
        Features = features ?? throw new ArgumentNullException(nameof(features));

    public void UpdateCommerceSettings(CommerceSettings settings) =>
        Commerce = settings ?? throw new ArgumentNullException(nameof(settings));

    public void UpdateNetworkSettings(NetworkSettings settings) =>
        Network = settings ?? throw new ArgumentNullException(nameof(settings));

    public void Suspend() => Status = OrganizationStatus.Suspended;

    public void Reactivate()
    {
        if (Status == OrganizationStatus.Closed)
            throw new DomainInvariantException("A closed organization cannot be reactivated.");
        Status = OrganizationStatus.Active;
    }

    public void UpdateReferralSettings(ReferralSettings settings)
    {
        Referrals = settings ?? throw new ArgumentNullException(nameof(settings));
    }

    public void UpdateWalletSettings(WalletSettings settings)
    {
        Wallet = settings ?? throw new ArgumentNullException(nameof(settings));
    }

    public bool PublishBranding(DateTimeOffset publishedAt)
    {
        if (publishedAt == default)
            throw new DomainInvariantException("Branding publication time is required.");
        if (PublishedBrandingRevision == BrandingRevision)
            return false;

        ValidateBrandingForPublication();
        PublishedBranding = BrandingSettings.Create(
            Branding.StoreTitle,
            Branding.SupportEmail,
            Branding.PrimaryColor,
            Branding.SecondaryColor,
            Branding.AccentColor,
            Branding.LogoUrl,
            Branding.FaviconUrl,
            Branding.SupportPhone,
            Branding.FooterText
        );
        PublishedBrandingRevision = BrandingRevision;
        BrandingPublishedAt = publishedAt;
        AddDomainEvent(
            new OrganizationBrandingPublishedEvent(Id, PublishedBrandingRevision, publishedAt)
        );
        return true;
    }

    public OrganizationDomain ConfigureDomain(
        string hostName,
        bool makePrimary,
        DateTimeOffset occurredAt
    )
    {
        var normalizedHostName = OrganizationDomain.NormalizeHostName(hostName);
        var existing = _domains.SingleOrDefault(domain => domain.HostName == normalizedHostName);
        if (existing is not null)
        {
            if (makePrimary)
                MakePrimaryDomain(existing.Id, occurredAt);
            return existing;
        }

        var shouldBePrimary = makePrimary || _domains.Count == 0;
        if (shouldBePrimary)
            DemoteCurrentPrimary(occurredAt);

        var domain = OrganizationDomain.Create(Id, normalizedHostName, shouldBePrimary, occurredAt);
        _domains.Add(domain);
        return domain;
    }

    public bool MakePrimaryDomain(Guid organizationDomainId, DateTimeOffset occurredAt)
    {
        if (organizationDomainId == Guid.Empty)
            throw new DomainInvariantException("Organization domain is required.");

        var domain = _domains.SingleOrDefault(candidate => candidate.Id == organizationDomainId);
        if (domain is null)
            throw new DomainInvariantException("Organization domain was not found.");
        if (domain.IsPrimary)
            return false;

        DemoteCurrentPrimary(occurredAt);
        domain.MakePrimary(occurredAt);
        return true;
    }

    public void RemoveDomain(Guid organizationDomainId)
    {
        var domain = _domains.SingleOrDefault(candidate => candidate.Id == organizationDomainId);
        if (domain is null)
            throw new DomainInvariantException("Organization domain was not found.");
        if (domain.IsPrimary)
        {
            throw new DomainInvariantException(
                "The primary domain cannot be removed until another domain is made primary."
            );
        }

        _domains.Remove(domain);
    }

    private void DemoteCurrentPrimary(DateTimeOffset occurredAt)
    {
        var primaryDomain = _domains.SingleOrDefault(domain => domain.IsPrimary);
        primaryDomain?.Demote(occurredAt);
    }

    private void ValidateBrandingForPublication()
    {
        if (string.IsNullOrWhiteSpace(Branding.StoreTitle))
            throw new DomainInvariantException("Store title is required before publication.");
        if (!IsValidEmail(Branding.SupportEmail))
            throw new DomainInvariantException(
                "A valid support email is required before publication."
            );
        if (
            !HexColorPattern.IsMatch(Branding.PrimaryColor)
            || !HexColorPattern.IsMatch(Branding.SecondaryColor)
            || !HexColorPattern.IsMatch(Branding.AccentColor)
        )
        {
            throw new DomainInvariantException(
                "Brand colors must use six-digit hexadecimal values."
            );
        }

        ValidateAssetUrl(Branding.LogoUrl, "Logo");
        ValidateAssetUrl(Branding.FaviconUrl, "Favicon");
    }

    private static bool IsValidEmail(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return false;

        try
        {
            return new MailAddress(value).Address.Equals(
                value.Trim(),
                StringComparison.OrdinalIgnoreCase
            );
        }
        catch (FormatException)
        {
            return false;
        }
    }

    private static void ValidateAssetUrl(string? value, string label)
    {
        if (string.IsNullOrWhiteSpace(value))
            return;
        if (
            !Uri.TryCreate(value, UriKind.Absolute, out var uri)
            || (uri.Scheme != Uri.UriSchemeHttps && uri.Scheme != Uri.UriSchemeHttp)
        )
        {
            throw new DomainInvariantException($"{label} URL must be an absolute HTTP URL.");
        }
    }

    public void Close() => Status = OrganizationStatus.Closed;
}
