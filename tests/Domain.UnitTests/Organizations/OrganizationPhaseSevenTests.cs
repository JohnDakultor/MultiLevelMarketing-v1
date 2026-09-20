using modular_mlm.Domain.Events;
using modular_mlm.Domain.Exceptions;
using modular_mlm.Domain.Organizations;
using NUnit.Framework;
using Shouldly;

namespace modular_mlm.Domain.UnitTests.Organizations;

public sealed class OrganizationPhaseSevenTests
{
    private static readonly DateTimeOffset OccurredAt = new(2026, 9, 12, 8, 0, 0, TimeSpan.Zero);

    [Test]
    public void CreateInitializesCommerceAndRaisesProvisionedEvent()
    {
        var organization = Organization.Create(
            "Green Zero",
            "greenzero",
            "PHP",
            provisionedAt: OccurredAt
        );

        organization.Commerce.ShouldNotBeNull();
        organization.BrandingRevision.ShouldBe(1);
        organization.PublishedBrandingRevision.ShouldBe(0);
        organization
            .DomainEvents.Any(@event =>
                @event
                    is OrganizationProvisionedEvent
                    {
                        Slug: "greenzero",
                        OccurredAt: var occurredAt,
                    }
                && occurredAt == OccurredAt
            )
            .ShouldBeTrue();
    }

    [Test]
    public void PublishBrandingRaisesOneEventForTheCurrentRevision()
    {
        var organization = CreatePublishableOrganization();

        organization.PublishBranding(OccurredAt).ShouldBeTrue();
        organization.PublishBranding(OccurredAt.AddMinutes(1)).ShouldBeFalse();

        organization.PublishedBrandingRevision.ShouldBe(organization.BrandingRevision);
        organization.BrandingPublishedAt.ShouldBe(OccurredAt);
        organization.DomainEvents.OfType<OrganizationBrandingPublishedEvent>().Count().ShouldBe(1);
    }

    [Test]
    public void BrandingChangeCreatesANewPublishableRevision()
    {
        var organization = CreatePublishableOrganization();
        organization.PublishBranding(OccurredAt);
        var publishedLogo = organization.PublishedBranding!.LogoUrl;
        organization.UpdateBranding(
            BrandingSettings.Create(
                "Green Zero Store",
                "support@greenzero.example",
                logoUrl: "https://cdn.example.com/new-logo.png"
            )
        );

        organization.PublishedBranding!.LogoUrl.ShouldBe(publishedLogo);

        organization.PublishBranding(OccurredAt.AddMinutes(1)).ShouldBeTrue();

        organization.PublishedBrandingRevision.ShouldBe(3);
        organization.PublishedBranding!.LogoUrl.ShouldBe("https://cdn.example.com/new-logo.png");
        organization.DomainEvents.OfType<OrganizationBrandingPublishedEvent>().Count().ShouldBe(2);
    }

    [Test]
    public void PublishRejectsIncompleteBranding()
    {
        var organization = Organization.Create(
            "Green Zero",
            "greenzero",
            "PHP",
            provisionedAt: OccurredAt
        );

        Should.Throw<DomainInvariantException>(() => organization.PublishBranding(OccurredAt));
    }

    [Test]
    public void ConfigureDomainMakesTheFirstDomainPrimaryAndPreventsDuplicates()
    {
        var organization = CreatePublishableOrganization();

        var configured = organization.ConfigureDomain(
            " Shop.GreenZero.Example ",
            false,
            OccurredAt
        );
        var repeated = organization.ConfigureDomain(
            "shop.greenzero.example",
            false,
            OccurredAt.AddMinutes(1)
        );

        configured.IsPrimary.ShouldBeTrue();
        repeated.ShouldBeSameAs(configured);
        organization.Domains.Count.ShouldBe(1);
    }

    [Test]
    public void MakingAnotherDomainPrimaryDemotesThePreviousPrimary()
    {
        var organization = CreatePublishableOrganization();
        var first = organization.ConfigureDomain("shop.greenzero.example", false, OccurredAt);
        var second = organization.ConfigureDomain(
            "members.greenzero.example",
            true,
            OccurredAt.AddMinutes(1)
        );

        first.IsPrimary.ShouldBeFalse();
        second.IsPrimary.ShouldBeTrue();
        organization.Domains.Count(domain => domain.IsPrimary).ShouldBe(1);
    }

    [Test]
    public void RemoveDomainRejectsTheCurrentPrimary()
    {
        var organization = CreatePublishableOrganization();
        var primary = organization.ConfigureDomain("shop.greenzero.example", false, OccurredAt);

        var exception = Should.Throw<DomainInvariantException>(() =>
            organization.RemoveDomain(primary.Id)
        );

        exception.Message.ShouldContain("primary domain");
    }

    private static Organization CreatePublishableOrganization()
    {
        var organization = Organization.Create(
            "Green Zero",
            "greenzero",
            "PHP",
            provisionedAt: OccurredAt
        );
        organization.UpdateBranding(
            BrandingSettings.Create(
                "Green Zero Store",
                "support@greenzero.example",
                logoUrl: "https://cdn.example.com/logo.png",
                faviconUrl: "https://cdn.example.com/favicon.ico"
            )
        );
        return organization;
    }
}
