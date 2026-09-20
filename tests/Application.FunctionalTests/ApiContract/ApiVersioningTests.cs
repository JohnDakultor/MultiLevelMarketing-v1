using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using modular_mlm.Web.Infrastructure.Versioning;

namespace modular_mlm.Application.FunctionalTests.ApiContract;

public sealed class ApiVersioningTests : TestBase
{
    [Test]
    public async Task TenantFirstApiReportsItsDocumentedVersion()
    {
        using var client = FunctionalTestSetup.CreateClient();
        using var response = await client.GetAsync($"/api/organizations/{Guid.NewGuid()}/products");
        response
            .Headers.GetValues(ApiRouteVersioningPolicy.VersionHeader)
            .Single()
            .ShouldBe(ApiRouteVersioningPolicy.CurrentVersion);
        response
            .Headers.GetValues(ApiRouteVersioningPolicy.SupportedVersionsHeader)
            .Single()
            .ShouldBe(ApiRouteVersioningPolicy.CurrentVersion);
    }

    [Test]
    public async Task EndpointNamesAreGloballyUnique()
    {
        var names = await TestApp.ExecuteInScopeAsync(services =>
            Task.FromResult(
                services
                    .GetRequiredService<EndpointDataSource>()
                    .Endpoints.Select(endpoint =>
                        endpoint.Metadata.GetMetadata<IEndpointNameMetadata>()?.EndpointName
                    )
                    .Where(name => name is not null)
                    .ToArray()
            )
        );
        names.Length.ShouldBe(names.Distinct(StringComparer.Ordinal).Count());
    }
}
