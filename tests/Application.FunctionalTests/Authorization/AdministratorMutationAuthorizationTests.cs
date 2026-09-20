using MediatR;
using modular_mlm.Application.Catalog.Commands.CreateProduct;
using modular_mlm.Application.Catalog.Commands.CreateProductCommissionProfile;
using modular_mlm.Application.Commerce.Commands.ReconcilePayment;
using modular_mlm.Application.Commerce.Commands.RequestPaymentRefund;
using modular_mlm.Application.Common.Exceptions;
using modular_mlm.Application.Compensation.Commands.CreateCommissionPlan;
using modular_mlm.Application.Network.Commands.ApproveAgent;
using modular_mlm.Application.Organizations.Commands.UpdateNetworkSettings;
using modular_mlm.Application.Organizations.Commands.UpdateReferralSettings;
using modular_mlm.Domain.Catalog;
using modular_mlm.Domain.Compensation;
using modular_mlm.Domain.Network;
using modular_mlm.Domain.Organizations;

namespace modular_mlm.Application.FunctionalTests.Authorization;

using static Infrastructure.TestApp;

public sealed class AdministratorMutationAuthorizationTests : TestBase
{
    [Test]
    public async Task AdministratorCannotMutateAnotherOrganization()
    {
        var ownedOrganization = Organization.Create(
            "Owned Administration Scope",
            $"owned-administration-{Guid.NewGuid():N}",
            "PHP"
        );
        var foreignOrganization = Organization.Create(
            "Foreign Administration Scope",
            $"foreign-administration-{Guid.NewGuid():N}",
            "PHP"
        );
        await AddAsync(ownedOrganization);
        await AddAsync(foreignOrganization);
        await RunAsAdministratorAsync(ownedOrganization.Id);
        var now = DateTimeOffset.UtcNow;
        IBaseRequest[] foreignMutations =
        [
            new CreateProductCommand(
                foreignOrganization.Id,
                Guid.NewGuid(),
                "Foreign Product",
                $"foreign-product-{Guid.NewGuid():N}",
                "Must not be created.",
                $"SKU-{Guid.NewGuid():N}",
                100m,
                10m,
                1
            ),
            new CreateProductCommissionProfileCommand(
                foreignOrganization.Id,
                "Foreign Profile",
                true,
                null,
                true,
                null,
                now
            ),
            new CreateCommissionPlanCommand(
                foreignOrganization.Id,
                "Foreign Plan",
                1,
                now,
                0.10m,
                false,
                null,
                ProcessingFrequency.Weekly
            ),
            new ApproveAgentCommand(foreignOrganization.Id, Guid.NewGuid()),
            new UpdateNetworkSettingsCommand(
                foreignOrganization.Id,
                PlacementStrategyType.BreadthFirst,
                true,
                10,
                true,
                true
            ),
            new UpdateReferralSettingsCommand(foreignOrganization.Id, 60, true, false),
            new RequestPaymentRefundCommand(
                foreignOrganization.Id,
                Guid.NewGuid(),
                100m,
                "Must not be refunded."
            ),
            new ReconcilePaymentForOrganizationCommand(foreignOrganization.Id, Guid.NewGuid()),
        ];

        foreach (var command in foreignMutations)
            await Should.ThrowAsync<ForbiddenAccessException>(() => SendAsync(command));

        (await CountAsync<Product>()).ShouldBe(0);
        (await CountAsync<ProductCommissionProfile>()).ShouldBe(0);
        (await CountAsync<Agent>()).ShouldBe(0);
        var unchanged = await FindAsync<Organization>(foreignOrganization.Id);
        unchanged.ShouldNotBeNull();
        unchanged.Referrals.AttributionWindowDays.ShouldBe(30);
        unchanged.Referrals.AllowReferralOverride.ShouldBeFalse();
        unchanged.Referrals.ReferralLockAfterFirstPurchase.ShouldBeTrue();
    }
}
