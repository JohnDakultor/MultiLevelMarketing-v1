using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using modular_mlm.Domain.Catalog;
using modular_mlm.Domain.Organizations;

namespace modular_mlm.Application.FunctionalTests.Contracts;

using static Infrastructure.TestApp;

public sealed class AdministrationHttpContractTests : TestBase
{
    [Test]
    public async Task ProtectedAdministrationEndpointRequiresAuthentication()
    {
        using var client = CreateClient();
        using var response = await client.GetAsync($"/api/organizations/{Guid.NewGuid()}/admin/categories");
        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Test]
    public async Task CookieMutationRequiresAntiforgeryToken()
    {
        var organization = await CreateOrganizationAsync();
        await RunAsAdministratorAsync(organization.Id);
        using var client = CreateClient();
        await LoginWithCookieAsync(client);

        using var response = await client.PostAsJsonAsync(
            $"/api/organizations/{organization.Id}/admin/categories",
            new { name = "Health", slug = "health" }
        );

        response.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
    }

    [Test]
    public async Task InvalidCategoryReturnsValidationProblem()
    {
        var organization = await CreateOrganizationAsync();
        using var client = await CreateAdministratorClientAsync(organization.Id);
        using var response = await client.PostAsJsonAsync(
            $"/api/organizations/{organization.Id}/admin/categories",
            new { name = "", slug = "INVALID SLUG" }
        );
        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        response.Content.Headers.ContentType?.MediaType.ShouldBe("application/problem+json");
    }

    [Test]
    public async Task CrossTenantCategoryLooksNotFound()
    {
        var owned = await CreateOrganizationAsync();
        var foreign = await CreateOrganizationAsync();
        var category = Category.Create(foreign.Id, "Foreign", "foreign");
        await AddAsync(category);
        using var client = await CreateAdministratorClientAsync(owned.Id);

        using var response = await client.PostAsync(
            $"/api/organizations/{owned.Id}/admin/categories/{category.Id}/archive",
            null
        );
        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    [Test]
    public async Task DuplicateCategoryReturnsConflict()
    {
        var organization = await CreateOrganizationAsync();
        await AddAsync(Category.Create(organization.Id, "Health", "health"));
        using var client = await CreateAdministratorClientAsync(organization.Id);
        using var response = await client.PostAsJsonAsync(
            $"/api/organizations/{organization.Id}/admin/categories",
            new { name = "Another", slug = "health" }
        );
        response.StatusCode.ShouldBe(HttpStatusCode.Conflict);
    }

    [TestCase("garbage", "api_version_malformed")]
    [TestCase("99.0", "api_version_unsupported")]
    public async Task InvalidApiVersionReturnsStableProblem(string version, string code)
    {
        using var client = FunctionalTestSetup.CreateClient();
        using var request = new HttpRequestMessage(HttpMethod.Get, "/api/organizations/resolve");
        request.Headers.Add("Api-Version", version);
        using var response = await client.SendAsync(request);
        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        document.RootElement.GetProperty("code").GetString().ShouldBe(code);
    }

    private static async Task<Organization> CreateOrganizationAsync()
    {
        var organization = Organization.Create(
            "Contract Tests",
            $"contract-{Guid.NewGuid():N}",
            "USD"
        );
        await AddAsync(organization);
        return organization;
    }

    private static HttpClient CreateClient()
    {
        var client = FunctionalTestSetup.CreateClient();
        client.BaseAddress = new Uri("https://localhost");
        client.DefaultRequestHeaders.Add("Api-Version", "1.0");
        return client;
    }

    private static async Task<HttpClient> CreateAdministratorClientAsync(Guid organizationId)
    {
        await RunAsAdministratorAsync(organizationId);
        var client = CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
            "Bearer",
            await LoginWithBearerAsync(client)
        );
        return client;
    }

    private static async Task LoginWithCookieAsync(HttpClient client)
    {
        using var response = await client.PostAsJsonAsync(
            "/api/Users/login?useCookies=true&useSessionCookies=true",
            new { email = "administrator@local", password = "Administrator1234!" }
        );
        response.EnsureSuccessStatusCode();
    }

    private static async Task<string> LoginWithBearerAsync(HttpClient client)
    {
        using var response = await client.PostAsJsonAsync(
            "/api/Users/login?useCookies=false&useSessionCookies=false",
            new { email = "administrator@local", password = "Administrator1234!" }
        );
        response.EnsureSuccessStatusCode();
        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        return document.RootElement.GetProperty("accessToken").GetString()!;
    }
}
