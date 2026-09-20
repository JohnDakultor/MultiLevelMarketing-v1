using Domain.Enums;
using modular_mlm.Application.Catalog.Commands.ArchiveProduct;
using modular_mlm.Application.Catalog.Commands.AssignCommissionProfile;
using modular_mlm.Application.Catalog.Commands.CreateProduct;
using modular_mlm.Application.Catalog.Commands.CreateProductCommissionProfile;
using modular_mlm.Application.Catalog.Commands.PublishProduct;
using modular_mlm.Application.Catalog.Commands.UpdateProduct;
using modular_mlm.Application.Common.Auditing;
using modular_mlm.Application.Common.Exceptions;
using modular_mlm.Application.Organizations.Commands.UpdateBranding;
using modular_mlm.Application.Organizations.Commands.UpdateFeatureSettings;
using modular_mlm.Application.Organizations.Commands.UpdateNetworkSettings;
using modular_mlm.Application.Organizations.Commands.UpdateReferralSettings;
using modular_mlm.Application.Organizations.Commands.UpdateWalletSettings;
using modular_mlm.Domain.AuditLogs;
using modular_mlm.Domain.Catalog;
using modular_mlm.Domain.Network;
using modular_mlm.Domain.Organizations;

namespace modular_mlm.Application.FunctionalTests.Auditing;

using static Infrastructure.TestApp;

public sealed class CatalogOrganizationAuditTests : Infrastructure.TestBase
{
    [Test]
    public async Task ProductAndCommissionProfileLifecycleProducesCompleteAuditTrail()
    {
        var organization = await CreateOrganizationAsync();
        var category = Category.Create(organization.Id, "Supplements", "supplements");
        await AddAsync(category);
        var actorId = Guid.Parse(await RunAsAdministratorAsync(organization.Id));

        var productId = await SendAsync(
            new CreateProductCommand(
                organization.Id,
                category.Id,
                "Daily Greens",
                $"daily-greens-{Guid.NewGuid():N}",
                "Daily nutrition",
                $"SKU-{Guid.NewGuid():N}"[..16],
                1_000m,
                100m,
                20
            )
        );
        await SendAsync(
            new UpdateProductCommand(
                organization.Id,
                productId,
                category.Id,
                "Daily Greens Plus",
                "Updated nutrition"
            )
        );
        await SendAsync(new PublishProductCommand(organization.Id, productId));
        var profileId = await SendAsync(
            new CreateProductCommissionProfileCommand(
                organization.Id,
                "Standard",
                true,
                0.10m,
                true,
                100m,
                DateTimeOffset.UtcNow
            )
        );
        await SendAsync(new AssignCommissionProfileCommand(organization.Id, productId, profileId));
        await SendAsync(new ArchiveProductCommand(organization.Id, productId));

        var expected = new[]
        {
            AuditCoverageMap.ProductCreated,
            AuditCoverageMap.ProductUpdated,
            AuditCoverageMap.ProductPublished,
            AuditCoverageMap.ProductArchived,
            AuditCoverageMap.CommissionProfileCreated,
            AuditCoverageMap.CommissionProfileAssigned,
        };
        foreach (var definition in expected)
        {
            var entityId =
                definition == AuditCoverageMap.CommissionProfileCreated ? profileId : productId;
            var audit = await SingleAsync<AuditLog>(entry =>
                entry.OrganizationId == organization.Id
                && entry.EntityId == entityId
                && entry.Action == definition.Action
            );
            audit.ActorUserId.ShouldBe(actorId);
            audit.EntityType.ShouldBe(definition.EntityType);
            audit.AfterJson.ShouldNotBeNullOrWhiteSpace();
        }
    }

    [Test]
    public async Task BrandingAndFeatureChangesAuditOrganizationBeforeAndAfterState()
    {
        var organization = await CreateOrganizationAsync();
        var actorId = Guid.Parse(await RunAsAdministratorAsync(organization.Id));

        await SendAsync(
            new UpdateBrandingCommand(
                organization.Id,
                "GreenZero Store",
                "support@greenzero.test",
                "#114422",
                "#F5F7F5",
                "#FFAA00",
                null,
                null,
                null,
                "All rights reserved"
            )
        );
        await SendAsync(
            new UpdateFeatureSettingsCommand(organization.Id, false, true, true, false, true, false)
        );

        foreach (
            var definition in new[]
            {
                AuditCoverageMap.BrandingUpdated,
                AuditCoverageMap.FeaturesUpdated,
                AuditCoverageMap.CommerceSettingsUpdated,
            }
        )
        {
            var audit = await SingleAsync<AuditLog>(entry =>
                entry.OrganizationId == organization.Id
                && entry.EntityId == organization.Id
                && entry.Action == definition.Action
            );
            audit.ActorUserId.ShouldBe(actorId);
            audit.EntityType.ShouldBe(AuditEntityNames.Organization);
            audit.BeforeJson.ShouldNotBe(audit.AfterJson);
        }
    }

    [Test]
    public async Task NetworkReferralAndWalletSettingsChangesAreAudited()
    {
        var organization = await CreateOrganizationAsync();
        var actorId = Guid.Parse(await RunAsAdministratorAsync(organization.Id));

        await SendAsync(
            new UpdateNetworkSettingsCommand(
                organization.Id,
                PlacementStrategyType.BalancedLeg,
                false,
                25,
                true,
                true
            )
        );
        await SendAsync(new UpdateReferralSettingsCommand(organization.Id, 45, true, false));
        await SendAsync(
            new UpdateWalletSettingsCommand(
                organization.Id,
                CommissionReleaseTrigger.ReturnWindowElapsed,
                2,
                14,
                500m,
                true,
                1_000m
            )
        );

        foreach (
            var definition in new[]
            {
                AuditCoverageMap.NetworkSettingsUpdated,
                AuditCoverageMap.ReferralSettingsUpdated,
                AuditCoverageMap.WalletSettingsUpdated,
            }
        )
        {
            var audit = await SingleAsync<AuditLog>(entry =>
                entry.OrganizationId == organization.Id
                && entry.EntityId == organization.Id
                && entry.Action == definition.Action
            );
            audit.ActorUserId.ShouldBe(actorId);
            audit.EntityType.ShouldBe(AuditEntityNames.Organization);
            audit.BeforeJson.ShouldNotBe(audit.AfterJson);
        }
    }

    [Test]
    public async Task CrossOrganizationCatalogMutationIsForbiddenAndNotAudited()
    {
        var owned = await CreateOrganizationAsync();
        var foreign = await CreateOrganizationAsync();
        var category = Category.Create(foreign.Id, "Foreign", "foreign");
        await AddAsync(category);
        await RunAsAdministratorAsync(owned.Id);

        await Should.ThrowAsync<ForbiddenAccessException>(() =>
            SendAsync(
                new CreateProductCommand(
                    foreign.Id,
                    category.Id,
                    "Foreign Product",
                    $"foreign-{Guid.NewGuid():N}",
                    "Not owned",
                    $"SKU-{Guid.NewGuid():N}"[..16],
                    100m,
                    10m,
                    1
                )
            )
        );

        (await CountAsync<AuditLog>()).ShouldBe(0);
    }

    private static async Task<Organization> CreateOrganizationAsync()
    {
        var organization = Organization.Create(
            "Catalog Audit",
            $"catalog-audit-{Guid.NewGuid():N}",
            "PHP"
        );
        await AddAsync(organization);
        return organization;
    }
}
