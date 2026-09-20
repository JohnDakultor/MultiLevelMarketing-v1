using modular_mlm.Application.Common.Interfaces;
using modular_mlm.Application.Common.Models;
using modular_mlm.Infrastructure.Notifications;
using Moq;
using Shouldly;

namespace modular_mlm.Infrastructure.IntegrationTests.Notifications;

public sealed class EmailNotificationSenderTests
{
    [Test]
    public async Task AllowListedTemplateIsRenderedAndSent()
    {
        var transport = new Mock<IEmailSender>();
        var sender = new EmailNotificationSender(
            transport.Object,
            new NotificationTemplateRenderer()
        );
        var request = Request(
            "commission-released",
            new Dictionary<string, string> { ["amount"] = "100.00", ["currency"] = "PHP" }
        );

        var result = await sender.SendAsync(request, CancellationToken.None);

        result.Succeeded.ShouldBeTrue();
        transport.Verify(
            item =>
                item.SendAsync(
                    "member@example.test",
                    "Commission available",
                    It.Is<string>(body => body.Contains("PHP 100.00")),
                    It.IsAny<CancellationToken>()
                ),
            Times.Once
        );
    }

    [Test]
    public async Task UnknownTemplateIsPermanentFailure()
    {
        var sender = new EmailNotificationSender(
            Mock.Of<IEmailSender>(),
            new NotificationTemplateRenderer()
        );
        var result = await sender.SendAsync(
            Request("unknown", new Dictionary<string, string>()),
            CancellationToken.None
        );
        result.Succeeded.ShouldBeFalse();
        result.IsTransient.ShouldBeFalse();
        result.FailureCode.ShouldBe("INVALID_TEMPLATE");
    }

    private static NotificationDeliveryRequest Request(
        string template,
        IReadOnlyDictionary<string, string> variables
    ) =>
        new(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid().ToString(),
            "member@example.test",
            NotificationChannel.Email,
            template,
            "en-PH",
            variables
        );
}
