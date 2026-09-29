using modular_mlm.Application.Catalog.Queries.GetProducts;
using modular_mlm.Application.Common.Exceptions;
using modular_mlm.Application.Organizations.Commands.UpdateFeatureSettings;
using modular_mlm.Application.Wallets.Queries.GetAdminWallets;
using modular_mlm.Domain.Organizations;

namespace modular_mlm.Application.FunctionalTests.Organizations;

using static Infrastructure.TestApp;

[NonParallelizable]
public sealed class FeatureGateTests : TestBase
{
    [Test]
    public async Task DisabledFeatureIsRejectedAndSettingsRemainTenantSpecific()
    {
        var disabled = Organization.Create("Disabled", $"disabled-{Guid.NewGuid():N}", "PHP");
        var enabled = Organization.Create("Enabled", $"enabled-{Guid.NewGuid():N}", "PHP");
        await AddAsync(disabled);
        await AddAsync(enabled);
        await RunAsAdministratorAsync(disabled.Id);
        await SendAsync(
            new UpdateFeatureSettingsCommand(disabled.Id, false, true, true, true, false, false)
        );

        await Should.ThrowAsync<ConflictException>(() =>
            SendAsync(new GetProductsQuery(disabled.Id))
        );
        await Should.ThrowAsync<ConflictException>(() =>
            SendAsync(new GetAdminWalletsQuery(disabled.Id, 1, 20, null, null, false))
        );

        var result = await SendAsync(new GetProductsQuery(enabled.Id));
        result.TotalCount.ShouldBe(0);
    }
}
