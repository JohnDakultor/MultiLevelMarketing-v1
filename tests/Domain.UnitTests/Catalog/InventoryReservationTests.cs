using modular_mlm.Domain.Catalog;
using modular_mlm.Domain.Exceptions;
using NUnit.Framework;
using Shouldly;

namespace modular_mlm.Domain.UnitTests.Catalog;

public sealed class InventoryReservationTests
{
    private static readonly DateTimeOffset Now = DateTimeOffset.Parse("2026-09-09T10:00:00Z");

    [Test]
    public void ReleaseIsIdempotent()
    {
        var reservation = CreateReservation();
        reservation.Release("Cancelled", Now.AddMinutes(1)).ShouldBeTrue();
        reservation.Release("Cancelled", Now.AddMinutes(2)).ShouldBeFalse();
        reservation.Status.ShouldBe(InventoryReservationStatus.Released);
        reservation.Version.ShouldBe(2);
    }

    [Test]
    public void FinalizeIsIdempotent()
    {
        var reservation = CreateReservation();
        reservation.Finalize(Now.AddMinutes(1)).ShouldBeTrue();
        reservation.Finalize(Now.AddMinutes(2)).ShouldBeFalse();
        reservation.Status.ShouldBe(InventoryReservationStatus.Finalized);
        reservation.Version.ShouldBe(2);
    }

    [Test]
    public void TerminalStatesCannotBeChanged()
    {
        var released = CreateReservation();
        released.Release("Cancelled", Now.AddMinutes(1));
        Should.Throw<DomainInvariantException>(() => released.Finalize(Now.AddMinutes(2)));

        var finalized = CreateReservation();
        finalized.Finalize(Now.AddMinutes(1));
        Should.Throw<DomainInvariantException>(() =>
            finalized.Release("Cancelled", Now.AddMinutes(2))
        );
    }

    [Test]
    public void ExpirationUsesSuppliedTime()
    {
        var reservation = CreateReservation();
        reservation.IsExpired(Now.AddMinutes(29)).ShouldBeFalse();
        reservation.IsExpired(Now.AddMinutes(30)).ShouldBeTrue();
    }

    private static InventoryReservation CreateReservation() =>
        InventoryReservation.Reserve(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            2,
            Now,
            Now.AddMinutes(30)
        );
}
