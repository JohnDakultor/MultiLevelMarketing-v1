using Microsoft.Extensions.Options;
using modular_mlm.Application.Common.Interfaces;
using modular_mlm.Infrastructure.Identity;
using Moq;

namespace modular_mlm.Infrastructure.IntegrationTests.Identity;

public sealed class IdentityEmailSenderTests
{
    [Test]
    public async Task ConfirmationLinkUsesConfiguredApplicationEmailTransport()
    {
        var transport = new Mock<IEmailSender>();
        var sender = CreateSender(transport);

        await sender.SendConfirmationLinkAsync(
            new ApplicationUser(),
            "member@example.test",
            "https://example.test/confirm?code=one&amp;userId=two"
        );

        transport.Verify(
            item =>
                item.SendAsync(
                    "member@example.test",
                    "Confirm your email address",
                    It.Is<string>(body =>
                        body.Contains("https://example.test/confirm?code=one&userId=two")
                    ),
                    CancellationToken.None
                ),
            Times.Once
        );
    }

    [Test]
    public async Task PasswordResetCodeUsesConfiguredApplicationEmailTransport()
    {
        var transport = new Mock<IEmailSender>();
        var sender = CreateSender(transport);

        await sender.SendPasswordResetCodeAsync(
            new ApplicationUser(),
            "member@example.test",
            "reset-code"
        );

        transport.Verify(
            item =>
                item.SendAsync(
                    "member@example.test",
                    "Reset your password",
                    It.Is<string>(body =>
                        body.Contains(
                            "https://storefront.example.test/reset-password?email=member%40example.test&code=reset-code"
                        )
                    ),
                    CancellationToken.None
                ),
            Times.Once
        );
    }

    private static IdentityEmailSender CreateSender(Mock<IEmailSender> transport) =>
        new(
            transport.Object,
            Options.Create(
                new IdentitySecurityOptions
                {
                    PasswordResetBaseUrl = new Uri(
                        "https://storefront.example.test/reset-password"
                    ),
                }
            )
        );
}
