using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using modular_mlm.Infrastructure.Identity;

namespace modular_mlm.Application.FunctionalTests.Identity;

[NonParallelizable]
public sealed class RegistrationAtomicityTests : TestBase
{
    [Test]
    public async Task SuccessfulConfirmationDeliveryCommitsIdentityUser()
    {
        var email = $"registration-{Guid.NewGuid():N}@example.test";
        using var client = FunctionalTestSetup.CreateClient();
        client.DefaultRequestHeaders.Add("Api-Version", "1.0");

        using var response = await client.PostAsJsonAsync(
            "/api/Users/register",
            new { email, password = "Registration1234!" }
        );

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var persistedUser = await TestApp.ExecuteInScopeAsync(services =>
            services.GetRequiredService<UserManager<ApplicationUser>>().FindByEmailAsync(email)
        );
        persistedUser.ShouldNotBeNull();
    }

    [Test]
    public async Task ConfirmationDeliveryFailureRollsBackIdentityUser()
    {
        var email = $"registration-{Guid.NewGuid():N}@example.test";
        var sender = TestApp.GetRequiredService<TestIdentityEmailSender>();
        sender.FailConfirmationDelivery = true;
        using var client = FunctionalTestSetup.CreateClient();
        client.DefaultRequestHeaders.Add("Api-Version", "1.0");

        using var response = await client.PostAsJsonAsync(
            "/api/Users/register",
            new { email, password = "Registration1234!" }
        );

        response.StatusCode.ShouldBe(HttpStatusCode.InternalServerError);
        var persistedUser = await TestApp.ExecuteInScopeAsync(services =>
            services.GetRequiredService<UserManager<ApplicationUser>>().FindByEmailAsync(email)
        );
        persistedUser.ShouldBeNull();
    }
}
