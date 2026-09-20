using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using modular_mlm.Application.Common.Interfaces;
using modular_mlm.Application.Common.Models;
using modular_mlm.Web.Endpoints;
using modular_mlm.Web.Infrastructure;
using modular_mlm.Web.Infrastructure.Security;

namespace modular_mlm.Application.FunctionalTests.Security;

public sealed class CookieAntiforgeryTests : TestBase
{
    [Test]
    public async Task DevelopmentHttpProxyCanIssueAntiforgeryToken()
    {
        var email = $"cookie-http-{Guid.NewGuid():N}@local";
        const string password = "Testing1234!";
        await TestApp.RunAsUserAsync(email, password, []);
        using var client = FunctionalTestSetup.CreateClient();
        client.BaseAddress = new Uri("https://localhost");
        using var loginResponse = await client.PostAsJsonAsync(
            "/api/Users/login?useCookies=true&useSessionCookies=true",
            new { email, password }
        );
        loginResponse.EnsureSuccessStatusCode();
        var authenticationCookie = loginResponse
            .Headers.GetValues("Set-Cookie")
            .Single(value => value.StartsWith("__Host-modular_mlm.auth=", StringComparison.Ordinal))
            .Split(';', 2)[0];

        using var tokenRequest = new HttpRequestMessage(
            HttpMethod.Get,
            "http://localhost/api/security/antiforgery-token"
        );
        tokenRequest.Headers.Add("Cookie", authenticationCookie);
        using var tokenResponse = await client.SendAsync(tokenRequest);
        tokenResponse.EnsureSuccessStatusCode();

        var token =
            await tokenResponse.Content.ReadFromJsonAsync<AntiforgeryTokenResponse>()
            ?? throw new InvalidOperationException("Antiforgery token response was empty.");

        token.RequestToken.ShouldNotBeNullOrWhiteSpace();
        token.HeaderName.ShouldBe(MarketplaceAntiforgeryPolicy.HeaderName);
    }

    [Test]
    public async Task CookieMutationRequiresMatchingAntiforgeryToken()
    {
        var email = $"cookie-{Guid.NewGuid():N}@local";
        const string password = "Testing1234!";
        await TestApp.RunAsUserAsync(email, password, []);
        using var client = CreateHttpsClient();
        await LoginWithCookieAsync(client, email, password);

        using var missingTokenResponse = await client.PostAsJsonAsync("/api/Users/logout", new { });
        await AssertAntiforgeryFailureAsync(missingTokenResponse);

        var token = await GetAntiforgeryTokenAsync(client);
        using var mismatchedRequest = new HttpRequestMessage(HttpMethod.Post, "/api/Users/logout")
        {
            Content = JsonContent.Create(new { }),
        };
        mismatchedRequest.Headers.Add(MarketplaceAntiforgeryPolicy.HeaderName, "invalid-token");
        using var mismatchedResponse = await client.SendAsync(mismatchedRequest);
        await AssertAntiforgeryFailureAsync(mismatchedResponse);

        using var validRequest = new HttpRequestMessage(HttpMethod.Post, "/api/Users/logout")
        {
            Content = JsonContent.Create(new { }),
        };
        validRequest.Headers.Add(token.HeaderName, token.RequestToken);
        using var validResponse = await client.SendAsync(validRequest);
        validResponse.StatusCode.ShouldBe(HttpStatusCode.OK);
        var sessionDeletion = validResponse
            .Headers.GetValues("Set-Cookie")
            .Single(value =>
                value.StartsWith("__Host-modular_mlm.session=", StringComparison.Ordinal)
            );
        sessionDeletion.ShouldContain("secure", Case.Insensitive);
        sessionDeletion.ShouldContain("path=/", Case.Insensitive);
    }

    [Test]
    public async Task CookieLoginCreatesARevocableCurrentSession()
    {
        var email = $"session-{Guid.NewGuid():N}@local";
        const string password = "Testing1234!";
        await TestApp.RunAsUserAsync(email, password, []);
        using var client = CreateHttpsClient();
        await LoginWithCookieAsync(client, email, password);

        var sessions =
            await client.GetFromJsonAsync<List<AuthenticationSessionInfo>>("/api/me/sessions")
            ?? throw new InvalidOperationException("Session response was empty.");
        var current = sessions.Single(session => session.IsCurrent);
        var token = await GetAntiforgeryTokenAsync(client);
        using var request = new HttpRequestMessage(HttpMethod.Post, "/api/me/session/revoke")
        {
            Content = JsonContent.Create(new { sessionId = current.Id }),
        };
        request.Headers.Add(token.HeaderName, token.RequestToken);

        using var response = await client.SendAsync(request);
        response.StatusCode.ShouldBe(HttpStatusCode.NoContent);
        using var currentUserResponse = await client.GetAsync("/api/me");
        currentUserResponse.StatusCode.ShouldBe(HttpStatusCode.NoContent);
    }

    [Test]
    public async Task RevokedCookieFallsBackToAnonymousAndCanSignInAgain()
    {
        var email = $"revoked-cookie-{Guid.NewGuid():N}@local";
        const string password = "Testing1234!";
        var userId = await TestApp.RunAsUserAsync(email, password, []);
        using var client = CreateHttpsClient();
        await LoginWithCookieAsync(client, email, password);

        var sessions =
            await client.GetFromJsonAsync<List<AuthenticationSessionInfo>>("/api/me/sessions")
            ?? throw new InvalidOperationException("Session response was empty.");
        var current = sessions.Single(session => session.IsCurrent);
        await TestApp.ExecuteInScopeAsync(async services =>
        {
            var sessionService = services.GetRequiredService<IAuthenticationSessionService>();
            return await sessionService.RevokeSessionAsync(
                userId,
                current.Id,
                DateTimeOffset.UtcNow,
                "functional-test",
                CancellationToken.None
            );
        });

        using var anonymousResponse = await client.GetAsync("/api/me");
        anonymousResponse.StatusCode.ShouldBe(HttpStatusCode.NoContent);

        await LoginWithCookieAsync(client, email, password);
        using var authenticatedResponse = await client.GetAsync("/api/me");
        authenticatedResponse.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    [Test]
    public async Task BearerMutationDoesNotRequireAntiforgeryToken()
    {
        var email = $"bearer-{Guid.NewGuid():N}@local";
        const string password = "Testing1234!";
        await TestApp.RunAsUserAsync(email, password, []);
        using var client = CreateHttpsClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
            "Bearer",
            await LoginWithBearerAsync(client, email, password)
        );

        using var response = await client.PostAsJsonAsync("/api/Users/logout", new { });

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    [Test]
    public async Task ProviderWebhookIsNotRejectedByAntiforgeryPolicy()
    {
        var email = $"webhook-cookie-{Guid.NewGuid():N}@local";
        const string password = "Testing1234!";
        await TestApp.RunAsUserAsync(email, password, []);
        using var client = CreateHttpsClient();
        await LoginWithCookieAsync(client, email, password);

        using var response = await client.PostAsJsonAsync("/api/webhooks/paymongo", new { });
        var body = await response.Content.ReadAsStringAsync();

        body.ShouldNotContain(ApiErrorCodes.AntiforgeryValidationFailed);
    }

    private static HttpClient CreateHttpsClient()
    {
        var client = FunctionalTestSetup.CreateClient();
        client.BaseAddress = new Uri("https://localhost");
        return client;
    }

    private static async Task LoginWithCookieAsync(HttpClient client, string email, string password)
    {
        using var response = await client.PostAsJsonAsync(
            "/api/Users/login?useCookies=true&useSessionCookies=true",
            new { email, password }
        );
        if (!response.IsSuccessStatusCode)
            throw new HttpRequestException(await response.Content.ReadAsStringAsync());
    }

    private static async Task<string> LoginWithBearerAsync(
        HttpClient client,
        string email,
        string password
    )
    {
        using var response = await client.PostAsJsonAsync(
            "/api/Users/login?useCookies=false&useSessionCookies=false",
            new { email, password }
        );
        response.EnsureSuccessStatusCode();
        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        return document.RootElement.GetProperty("accessToken").GetString()
            ?? throw new InvalidOperationException("Login did not return an access token.");
    }

    private static async Task<AntiforgeryTokenResponse> GetAntiforgeryTokenAsync(HttpClient client)
    {
        using var response = await client.GetAsync("/api/security/antiforgery-token");
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<AntiforgeryTokenResponse>()
            ?? throw new InvalidOperationException("Antiforgery token response was empty.");
    }

    private static async Task AssertAntiforgeryFailureAsync(HttpResponseMessage response)
    {
        response.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        document
            .RootElement.GetProperty("errorCode")
            .GetString()
            .ShouldBe(ApiErrorCodes.AntiforgeryValidationFailed);
    }
}
