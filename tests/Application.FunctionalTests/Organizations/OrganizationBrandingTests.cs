using modular_mlm.Application.Common.Auditing;
using modular_mlm.Application.Organizations.Commands.PublishBranding;
using modular_mlm.Application.Organizations.Commands.UpdateBranding;
using modular_mlm.Application.Organizations.Queries.GetPublicOrganizationConfig;
using modular_mlm.Domain.AuditLogs;
using modular_mlm.Domain.Exceptions;
using modular_mlm.Domain.Organizations;

namespace modular_mlm.Application.FunctionalTests.Organizations;

using static Infrastructure.TestApp;

public sealed class OrganizationBrandingTests : TestBase
{
    [Test]
    public async Task PublicConfigChangesOnlyAfterBrandingIsPublished()
    {
        var organization = Organization.Create("Branding", $"branding-{Guid.NewGuid():N}", "PHP");
        await AddAsync(organization);
        await RunAsAdministratorAsync(organization.Id);

        await SendAsync(CreateBrandingCommand(organization.Id, "First Published Title"));
        (await SendAsync(new GetPublicOrganizationConfigQuery(organization.Slug))).ShouldBeNull();
        await SendAsync(new PublishBrandingCommand(organization.Id));
        var firstPublished = await SendAsync(
            new GetPublicOrganizationConfigQuery(organization.Slug)
        );

        await SendAsync(CreateBrandingCommand(organization.Id, "Unpublished Draft Title"));
        var whileDraftExists = await SendAsync(
            new GetPublicOrganizationConfigQuery(organization.Slug)
        );

        firstPublished.ShouldNotBeNull();
        firstPublished.StoreTitle.ShouldBe("First Published Title");
        whileDraftExists.ShouldNotBeNull();
        whileDraftExists.StoreTitle.ShouldBe("First Published Title");

        await SendAsync(new PublishBrandingCommand(organization.Id));
        var secondPublished = await SendAsync(
            new GetPublicOrganizationConfigQuery(organization.Slug)
        );
        secondPublished!.StoreTitle.ShouldBe("Unpublished Draft Title");
    }

    [Test]
    public async Task PublishIsIdempotentAndProducesOneAuditPerRevision()
    {
        var organization = Organization.Create(
            "Audit Branding",
            $"audit-branding-{Guid.NewGuid():N}",
            "PHP"
        );
        await AddAsync(organization);
        await RunAsAdministratorAsync(organization.Id);
        await SendAsync(CreateBrandingCommand(organization.Id, "Audit Store"));

        await SendAsync(new PublishBrandingCommand(organization.Id));
        await SendAsync(new PublishBrandingCommand(organization.Id));

        (
            await CountAsync<AuditLog>(entry =>
                entry.OrganizationId == organization.Id
                && entry.Action == AuditCoverageMap.BrandingPublished.Action
            )
        ).ShouldBe(1);
    }

    [Test]
    public async Task IncompleteBrandingCannotBePublished()
    {
        var organization = Organization.Create(
            "Incomplete",
            $"incomplete-{Guid.NewGuid():N}",
            "PHP"
        );
        await AddAsync(organization);
        await RunAsAdministratorAsync(organization.Id);

        await Should.ThrowAsync<DomainInvariantException>(() =>
            SendAsync(new PublishBrandingCommand(organization.Id))
        );
    }

    private static UpdateBrandingCommand CreateBrandingCommand(Guid organizationId, string title) =>
        new(
            organizationId,
            title,
            "support@phase-seven.example",
            "#114422",
            "#F5F7F5",
            "#FFAA00",
            "https://assets.example/logo.png",
            "https://assets.example/favicon.ico",
            "+63 2 8000 0000",
            "Phase Seven"
        );
}
