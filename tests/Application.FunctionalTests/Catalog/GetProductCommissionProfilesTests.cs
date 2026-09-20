using modular_mlm.Application.Catalog.Queries.GetProductCommissionProfiles;
using modular_mlm.Domain.Catalog;
using modular_mlm.Domain.Organizations;

namespace modular_mlm.Application.FunctionalTests.Catalog;

using static Infrastructure.TestApp;

public sealed class GetProductCommissionProfilesTests : TestBase
{
    [Test]
    public async Task ReturnsOnlyProfilesOwnedByTheAdministratorsOrganization()
    {
        var owned = Organization.Create("Owned", $"owned-{Guid.NewGuid():N}", "PHP");
        var foreign = Organization.Create("Foreign", $"foreign-{Guid.NewGuid():N}", "PHP");
        var ownedProfile = ProductCommissionProfile.Create(
            owned.Id,
            "Standard",
            DateTimeOffset.UtcNow
        );
        var foreignProfile = ProductCommissionProfile.Create(
            foreign.Id,
            "Foreign",
            DateTimeOffset.UtcNow
        );
        await AddAsync(owned);
        await AddAsync(foreign);
        await AddAsync(ownedProfile);
        await AddAsync(foreignProfile);
        await RunAsAdministratorAsync(owned.Id);

        var profiles = await SendAsync(new GetProductCommissionProfilesQuery(owned.Id));

        profiles.Count.ShouldBe(1);
        profiles[0].Id.ShouldBe(ownedProfile.Id);
        profiles[0].Name.ShouldBe("Standard");
    }
}
