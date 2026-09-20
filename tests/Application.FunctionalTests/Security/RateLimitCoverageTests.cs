using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;

namespace modular_mlm.Application.FunctionalTests.Security;

public sealed class RateLimitCoverageTests : TestBase
{
    [Test]
    public async Task SensitiveMutationRoutesDeclareRateLimitingMetadata()
    {
        var patterns = new[]
        {
            "/checkout",
            "/payment-session",
            "/refund",
            "/payout",
            "/webhooks/paymongo",
        };
        var endpoints = await TestApp.ExecuteInScopeAsync(services =>
            Task.FromResult(
                services
                    .GetRequiredService<EndpointDataSource>()
                    .Endpoints.OfType<RouteEndpoint>()
                    .ToArray()
            )
        );
        foreach (var pattern in patterns)
        {
            var matches = endpoints
                .Where(endpoint =>
                    endpoint.RoutePattern.RawText?.Contains(
                        pattern,
                        StringComparison.OrdinalIgnoreCase
                    ) == true
                    && endpoint
                        .Metadata.GetMetadata<HttpMethodMetadata>()
                        ?.HttpMethods.Any(method =>
                            method is "POST" or "PUT" or "PATCH" or "DELETE"
                        ) == true
                )
                .ToArray();
            matches.ShouldNotBeEmpty($"No mutation route matched {pattern}.");
            matches.ShouldAllBe(endpoint =>
                endpoint.Metadata.Any(metadata =>
                    metadata.GetType().Name == "EnableRateLimitingAttribute"
                )
            );
        }
    }
}
