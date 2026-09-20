using modular_mlm.Domain.Events;
using modular_mlm.Domain.Exceptions;
using modular_mlm.Domain.Notifications;
using modular_mlm.Domain.ValueObjects;
using NUnit.Framework;
using Shouldly;

namespace modular_mlm.Domain.UnitTests.Notifications;

public sealed class NotificationTests
{
    [Test]
    public void CreateRaisesEventAndPreservesTrustedContent()
    {
        var organizationId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var createdAt = DateTimeOffset.UtcNow;
        var notification = Notification.Create(
            organizationId,
            userId,
            NotificationKind.CommissionReleased,
            new NotificationContent("Commission available", "PHP 100.00", "/agent/wallet"),
            "commission-released",
            "en-PH",
            "commission:1",
            createdAt
        );

        notification.OrganizationId.ShouldBe(organizationId);
        notification.RecipientUserId.ShouldBe(userId);
        notification.DomainEvents.OfType<NotificationCreatedEvent>().Count().ShouldBe(1);
    }

    [Test]
    public void DeliveredAndReadTransitionsAreIdempotent()
    {
        var createdAt = DateTimeOffset.UtcNow;
        var notification = Create(createdAt);
        var deliveredAt = createdAt.AddMinutes(1);
        var readAt = createdAt.AddMinutes(2);

        notification.MarkDelivered(deliveredAt).ShouldBeTrue();
        notification.MarkDelivered(deliveredAt.AddMinutes(1)).ShouldBeFalse();
        notification.DeliveredAt.ShouldBe(deliveredAt);
        notification.MarkRead(readAt).ShouldBeTrue();
        notification.MarkRead(readAt.AddMinutes(1)).ShouldBeFalse();
        notification.ReadAt.ShouldBe(readAt);
        notification.DomainEvents.OfType<NotificationReadEvent>().Count().ShouldBe(1);
    }

    [Test]
    public void UnsafeActionPathsAreRejected()
    {
        Should.Throw<DomainInvariantException>(() =>
            new NotificationContent("Title", "Body", "https://evil.example")
        );
        Should.Throw<DomainInvariantException>(() =>
            new NotificationContent("Title", "Body", "/path<script>")
        );
    }

    private static Notification Create(DateTimeOffset createdAt) =>
        Notification.Create(
            Guid.NewGuid(),
            Guid.NewGuid(),
            NotificationKind.OrderPaid,
            new NotificationContent("Paid", "Order paid"),
            "order-paid",
            "en-PH",
            Guid.NewGuid().ToString("N"),
            createdAt
        );
}
