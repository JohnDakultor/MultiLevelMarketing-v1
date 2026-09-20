using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using modular_mlm.Domain.Organizations;

namespace modular_mlm.Application.FunctionalTests.Authorization;

using static Infrastructure.TestApp;

public sealed class RouteAuthorizationTests : TestBase
{
    [Test]
    public async Task ProtectedRouteReturnsUnauthorizedWithoutAuthentication()
    {
        using var client = FunctionalTestSetup.CreateClient();

        using var response = await client.GetAsync(
            $"/api/organizations/{Guid.NewGuid()}/admin/audit-trail"
        );

        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Test]
    public async Task AdministratorRouteReturnsForbiddenForAuthenticatedNonAdministrator()
    {
        const string email = "route-user@local";
        const string password = "Testing1234!";
        await RunAsUserAsync(email, password, []);
        using var client = FunctionalTestSetup.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
            "Bearer",
            await LoginAsync(client, email, password)
        );

        using var response = await client.GetAsync(
            $"/api/organizations/{Guid.NewGuid()}/admin/audit-trail"
        );

        response.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
    }

    [Test]
    public async Task ApplicationScopeReturnsForbiddenForForeignOrganizationAdministrator()
    {
        var ownedOrganization = Organization.Create(
            "Route Owned",
            $"route-owned-{Guid.NewGuid():N}",
            "PHP"
        );
        var foreignOrganization = Organization.Create(
            "Route Foreign",
            $"route-foreign-{Guid.NewGuid():N}",
            "PHP"
        );
        await AddAsync(ownedOrganization);
        await AddAsync(foreignOrganization);
        await RunAsAdministratorAsync(ownedOrganization.Id);
        using var client = FunctionalTestSetup.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
            "Bearer",
            await LoginAsync(client, "administrator@local", "Administrator1234!")
        );

        using var response = await client.GetAsync(
            $"/api/organizations/{foreignOrganization.Id}/admin/audit-trail"
        );

        response.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
    }

    [Test]
    public async Task AuthorizedScopeReturnsNotFoundForMissingResource()
    {
        var organization = Organization.Create(
            "Route Not Found",
            $"route-not-found-{Guid.NewGuid():N}",
            "PHP"
        );
        await AddAsync(organization);
        await RunAsAdministratorAsync(organization.Id);
        using var client = FunctionalTestSetup.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
            "Bearer",
            await LoginAsync(client, "administrator@local", "Administrator1234!")
        );

        using var response = await client.GetAsync(
            $"/api/organizations/{organization.Id}/admin/payouts/{Guid.NewGuid()}"
        );

        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    private static async Task<string> LoginAsync(HttpClient client, string email, string password)
    {
        using var response = await client.PostAsJsonAsync(
            "/api/Users/login?useCookies=false&useSessionCookies=false",
            new { email, password }
        );
        response.EnsureSuccessStatusCode();
        await using var stream = await response.Content.ReadAsStreamAsync();
        using var document = await JsonDocument.ParseAsync(stream);
        return document.RootElement.GetProperty("accessToken").GetString()
            ?? throw new InvalidOperationException("Login did not return an access token.");
    }
}
