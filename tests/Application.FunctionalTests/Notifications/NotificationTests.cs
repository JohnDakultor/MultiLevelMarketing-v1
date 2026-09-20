using modular_mlm.Application.Notifications.Commands.MarkNotificationRead;
using modular_mlm.Application.Notifications.Queries.GetMyNotifications;
using modular_mlm.Domain.Notifications;
using modular_mlm.Domain.Organizations;
using modular_mlm.Domain.ValueObjects;

namespace modular_mlm.Application.FunctionalTests.Notifications;

using static Infrastructure.TestApp;

public sealed class NotificationTests : TestBase
{
    [Test]
    public async Task CurrentRecipientCanListAndReadOwnNotification()
    {
        var organization = Organization.Create(
            "Notifications",
            $"notifications-{Guid.NewGuid():N}",
            "PHP"
        );
        await AddAsync(organization);
        var userId = Guid.Parse(await RunAsAdministratorAsync(organization.Id));
        var notification = Notification.Create(
            organization.Id,
            userId,
            NotificationKind.OrderPaid,
            new NotificationContent("Payment received", "Order was paid."),
            "order-paid",
            "en-PH",
            Guid.NewGuid().ToString("N"),
            DateTimeOffset.UtcNow
        );
        await AddAsync(notification);

        var page = await SendAsync(new GetMyNotificationsQuery(organization.Id));
        page.Items.Single().Id.ShouldBe(notification.Id);
        await SendAsync(new MarkNotificationReadCommand(organization.Id, notification.Id));
        (await FindAsync<Notification>(notification.Id))!.ReadAt.ShouldNotBeNull();
    }
}
