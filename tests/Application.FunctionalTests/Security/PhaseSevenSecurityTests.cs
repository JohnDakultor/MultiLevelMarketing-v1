using System.Net;
using System.Net.Http.Headers;
using System.Text;
using modular_mlm.Application.FunctionalTests.Infrastructure;

namespace modular_mlm.Application.FunctionalTests.Security;

public sealed class PhaseSevenSecurityTests : TestBase
{
    [Test]
    public async Task ShouldApplySecurityHeaders()
    {
        using var client = FunctionalTestSetup.CreateClient();

        using var response = await client.GetAsync("/");

        response.Headers.GetValues("X-Content-Type-Options").Single().ShouldBe("nosniff");
        response.Headers.GetValues("X-Frame-Options").Single().ShouldBe("DENY");
        response.Headers.GetValues("Referrer-Policy").Single().ShouldBe("no-referrer");
        response
            .Headers.GetValues("Content-Security-Policy")
            .Single()
            .ShouldContain("frame-ancestors 'none'");
    }

    [Test]
    public async Task ShouldAllowScalarToLoadItsOwnScripts()
    {
        using var client = FunctionalTestSetup.CreateClient();

        using var response = await client.GetAsync("/scalar/");

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var policy = response.Headers.GetValues("Content-Security-Policy").Single();
        policy.ShouldContain("script-src 'self' 'unsafe-inline'");
        policy.ShouldContain("style-src 'self' 'unsafe-inline'");
        (await response.Content.ReadAsStringAsync()).ShouldContain("scalar.aspnetcore.js");
    }

    [Test]
    public async Task ShouldAllowOnlyConfiguredCredentialedCorsOrigin()
    {
        using var client = FunctionalTestSetup.CreateClient();
        using var allowed = CreatePreflight("https://frontend.test");
        using var rejected = CreatePreflight("https://untrusted.test");

        using var allowedResponse = await client.SendAsync(allowed);
        using var rejectedResponse = await client.SendAsync(rejected);

        allowedResponse
            .Headers.GetValues("Access-Control-Allow-Origin")
            .Single()
            .ShouldBe("https://frontend.test");
        allowedResponse
            .Headers.GetValues("Access-Control-Allow-Credentials")
            .Single()
            .ShouldBe("true");
        rejectedResponse.Headers.Contains("Access-Control-Allow-Origin").ShouldBeFalse();
    }

    [Test]
    public async Task ShouldRejectNonJsonWebhookBeforeProcessing()
    {
        using var client = FunctionalTestSetup.CreateClient();
        using var content = new StringContent("not-json", Encoding.UTF8, "text/plain");

        using var response = await client.PostAsync("/api/webhooks/paymongo", content);

        response.StatusCode.ShouldBe(HttpStatusCode.UnsupportedMediaType);
    }

    private static HttpRequestMessage CreatePreflight(string origin)
    {
        var request = new HttpRequestMessage(HttpMethod.Options, "/api/webhooks/paymongo");
        request.Headers.Add("Origin", origin);
        request.Headers.Add("Access-Control-Request-Method", "POST");
        request.Headers.Add("Access-Control-Request-Headers", "content-type");
        return request;
    }
}
