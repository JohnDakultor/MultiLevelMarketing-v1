using modular_mlm.Application.Common.Exceptions;
using modular_mlm.Application.Organizations.Commands.CreateOrganization;
using modular_mlm.Application.Organizations.Commands.UpdateCommerceSettings;
using modular_mlm.Application.Organizations.Commands.UpdateOrganizationProfile;
using modular_mlm.Application.Organizations.Queries.GetAdminOrganizationSettings;
using modular_mlm.Domain.AuditLogs;
using modular_mlm.Domain.Constants;
using modular_mlm.Domain.Organizations;

namespace modular_mlm.Application.FunctionalTests.Organizations;

using static Infrastructure.TestApp;

public sealed class OrganizationAdministrationTests : TestBase
{
    [Test]
    public async Task PlatformAdministratorCanProvisionAndOrganizationAdministratorCanManageSettings()
    {
        await RunAsUserAsync(
            "platform-admin@local",
            "Platform1234!",
            [Roles.PlatformAdministrator]
        );
        var slug = $"phase-seven-{Guid.NewGuid():N}";

        var organizationId = await SendAsync(
            new CreateOrganizationCommand("Phase Seven", slug, "PHP", "UTC", "en-PH")
        );
        await RunAsAdministratorAsync(organizationId);
        await SendAsync(
            new UpdateOrganizationProfileCommand(
                organizationId,
                "Phase Seven Store",
                "PHP",
                "Asia/Manila",
                "en-PH"
            )
        );
        await SendAsync(new UpdateCommerceSettingsCommand(organizationId, true, true, false, 45));

        var settings = await SendAsync(new GetAdminOrganizationSettingsQuery(organizationId));

        settings.ShouldNotBeNull();
        settings.Profile.Name.ShouldBe("Phase Seven Store");
        settings.Profile.Slug.ShouldBe(slug);
        settings.Commerce.InventoryReservationMinutes.ShouldBe(45);
        settings.Commerce.RequireBillingAddress.ShouldBeFalse();
        settings.Network.MaxQueryDepth.ShouldBe(10);
        settings.Network.AutoPlacementEnabled.ShouldBeTrue();
        (await CountAsync<AuditLog>(entry => entry.OrganizationId == organizationId)).ShouldBe(3);
    }

    [Test]
    public async Task OrganizationAdministratorCannotMutateAnotherOrganization()
    {
        var owned = Organization.Create("Owned", $"owned-{Guid.NewGuid():N}", "PHP");
        var foreign = Organization.Create("Foreign", $"foreign-{Guid.NewGuid():N}", "PHP");
        await AddAsync(owned);
        await AddAsync(foreign);
        await RunAsAdministratorAsync(owned.Id);

        await Should.ThrowAsync<ForbiddenAccessException>(() =>
            SendAsync(
                new UpdateOrganizationProfileCommand(foreign.Id, "Compromised", "PHP", "UTC", "en")
            )
        );

        (await FindAsync<Organization>(foreign.Id))!.Name.ShouldBe("Foreign");
    }

    [Test]
    public async Task ProvisioningRejectsReservedAndDuplicateSlugs()
    {
        await RunAsUserAsync(
            "platform-admin@local",
            "Platform1234!",
            [Roles.PlatformAdministrator]
        );
        await Should.ThrowAsync<modular_mlm.Application.Common.Exceptions.ValidationException>(() =>
            SendAsync(new CreateOrganizationCommand("Reserved", "admin", "PHP"))
        );

        var slug = $"duplicate-{Guid.NewGuid():N}";
        await SendAsync(new CreateOrganizationCommand("First", slug, "PHP"));
        await Should.ThrowAsync<ConflictException>(() =>
            SendAsync(new CreateOrganizationCommand("Second", slug, "PHP"))
        );
    }
}
