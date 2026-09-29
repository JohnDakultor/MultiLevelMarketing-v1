using modular_mlm.Infrastructure.Identity;
using modular_mlm.Infrastructure.Payments;
using Shouldly;

namespace modular_mlm.Infrastructure.IntegrationTests.Configuration;

public sealed class RuntimeUrlOptionsTests
{
    [TestCase("http://localhost/reset-password")]
    [TestCase("https://localhost/reset-password")]
    [TestCase("https://127.0.0.1/reset-password")]
    public void DeploymentIdentityUrlsRejectLoopback(string value)
    {
        IdentitySecurityOptions.IsValidForDeployment(
            new IdentitySecurityOptions { PasswordResetBaseUrl = new Uri(value) }
        ).ShouldBeFalse();
    }

    [Test]
    public void DeploymentIdentityAndInvitationUrlsAcceptPublicHttps()
    {
        IdentitySecurityOptions.IsValidForDeployment(
            new IdentitySecurityOptions
            {
                PasswordResetBaseUrl = new Uri("https://shop.example.test/reset-password"),
            }
        ).ShouldBeTrue();
        AdministratorInvitationOptions.IsValidForDeployment(
            new AdministratorInvitationOptions
            {
                AcceptanceBaseUrl = new Uri("https://admin.example.test/invitations/accept"),
            }
        ).ShouldBeTrue();
    }

    [Test]
    public void PayMongoCallbackRejectsLoopbackOutsideDevelopment()
    {
        PayMongoOptions.HasValidCallback(
            new PayMongoOptions
            {
                PayoutCallbackUrl = new Uri("https://localhost/api/webhooks/paymongo/transfers"),
            },
            allowLoopback: false
        ).ShouldBeFalse();
        PayMongoOptions.HasValidCallback(
            new PayMongoOptions
            {
                PayoutCallbackUrl = new Uri(
                    "https://api.example.test/api/webhooks/paymongo/transfers"
                ),
            },
            allowLoopback: false
        ).ShouldBeTrue();
    }
}
