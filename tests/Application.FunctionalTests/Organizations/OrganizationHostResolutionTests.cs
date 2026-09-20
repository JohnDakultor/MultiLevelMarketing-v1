using System.Net;
using modular_mlm.Domain.Organizations;

namespace modular_mlm.Application.FunctionalTests.Organizations;

using static Infrastructure.TestApp;

public sealed class OrganizationHostResolutionTests : TestBase
{
    [Test]
    public async Task AspireLocalhostSubdomainBypassesTenantResolution()
    {
        using var client = FunctionalTestSetup.CreateClient();
        using var request = new HttpRequestMessage(HttpMethod.Get, "/");
        request.Headers.Host = "webapi-modular_mlm.dev.localhost";

        using var response = await client.SendAsync(request);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    [Test]
    public async Task VerifiedHostResolvesItsPublishedOrganization()
    {
        var organization = CreatePublishedOrganization("tenant.phase-seven.example");
        await AddAsync(organization);
        using var client = FunctionalTestSetup.CreateClient();
        using var request = new HttpRequestMessage(
            HttpMethod.Get,
            "/api/organizations/public-config"
        );
        request.Headers.Host = "tenant.phase-seven.example";

        using var response = await client.SendAsync(request);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        (await response.Content.ReadAsStringAsync()).ShouldContain(organization.Slug);
    }

    [Test]
    public async Task UnknownAndUnverifiedHostsDoNotResolve()
    {
        var organization = Organization.Create(
            "Unverified",
            $"unverified-{Guid.NewGuid():N}",
            "PHP"
        );
        organization.UpdateBranding(
            BrandingSettings.Create("Unverified", "support@unverified.example")
        );
        organization.PublishBranding(DateTimeOffset.UtcNow);
        organization.ConfigureDomain("unverified.phase-seven.example", true, DateTimeOffset.UtcNow);
        await AddAsync(organization);
        using var client = FunctionalTestSetup.CreateClient();

        foreach (
            var host in new[] { "unknown.phase-seven.example", "unverified.phase-seven.example" }
        )
        {
            using var request = new HttpRequestMessage(
                HttpMethod.Get,
                "/api/organizations/public-config"
            );
            request.Headers.Host = host;
            using var response = await client.SendAsync(request);
            response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
        }
    }

    [Test]
    public async Task SuspendedOrganizationDoesNotResolvePublicly()
    {
        var organization = CreatePublishedOrganization("suspended.phase-seven.example");
        organization.Suspend();
        await AddAsync(organization);
        using var client = FunctionalTestSetup.CreateClient();
        using var request = new HttpRequestMessage(
            HttpMethod.Get,
            "/api/organizations/public-config"
        );
        request.Headers.Host = "suspended.phase-seven.example";

        using var response = await client.SendAsync(request);

        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    private static Organization CreatePublishedOrganization(string hostName)
    {
        var now = DateTimeOffset.UtcNow;
        var organization = Organization.Create(
            "Resolved Store",
            $"resolved-{Guid.NewGuid():N}",
            "PHP"
        );
        organization.UpdateBranding(
            BrandingSettings.Create("Resolved Store", "support@resolved.example")
        );
        organization.PublishBranding(now);
        organization.ConfigureDomain(hostName, true, now).Verify(now);
        return organization;
    }
}
