using modular_mlm.Domain.Events;
using modular_mlm.Domain.Exceptions;
using modular_mlm.Domain.Organizations;
using NUnit.Framework;
using Shouldly;

namespace modular_mlm.Domain.UnitTests.Organizations;

public sealed class OrganizationDomainTests
{
    private static readonly DateTimeOffset CreatedAt = new(2026, 9, 12, 8, 0, 0, TimeSpan.Zero);

    [TestCase(" Shop.Example.COM. ", "shop.example.com")]
    [TestCase("münich.example", "xn--mnich-kva.example")]
    public void CreateNormalizesDnsHostNames(string input, string expected)
    {
        var domain = OrganizationDomain.Create(Guid.NewGuid(), input, false, CreatedAt);

        domain.HostName.ShouldBe(expected);
        domain.IsVerified.ShouldBeFalse();
        domain
            .DomainEvents.Any(@event =>
                @event is OrganizationDomainConfiguredEvent configured
                && configured.HostName == expected
            )
            .ShouldBeTrue();
    }

    [TestCase("")]
    [TestCase("https://shop.example.com")]
    [TestCase("shop.example.com:443")]
    [TestCase("shop.example.com/path")]
    [TestCase("*.example.com")]
    [TestCase("127.0.0.1")]
    [TestCase("localhost")]
    [TestCase("shop_local.example.com")]
    [TestCase("-shop.example.com")]
    public void CreateRejectsUnsafeOrInvalidHostNames(string hostName)
    {
        Should.Throw<DomainInvariantException>(() =>
            OrganizationDomain.Create(Guid.NewGuid(), hostName, false, CreatedAt)
        );
    }

    [Test]
    public void VerifyIsIdempotentAndPreservesTheFirstVerificationTime()
    {
        var domain = OrganizationDomain.Create(
            Guid.NewGuid(),
            "shop.example.com",
            false,
            CreatedAt
        );
        var verifiedAt = CreatedAt.AddMinutes(10);

        domain.Verify(verifiedAt).ShouldBeTrue();
        domain.Verify(verifiedAt.AddMinutes(10)).ShouldBeFalse();

        domain.IsVerified.ShouldBeTrue();
        domain.VerifiedAt.ShouldBe(verifiedAt);
    }

    [Test]
    public void PrimaryTransitionsAreIdempotent()
    {
        var domain = OrganizationDomain.Create(
            Guid.NewGuid(),
            "shop.example.com",
            false,
            CreatedAt
        );

        domain.MakePrimary(CreatedAt.AddMinutes(1)).ShouldBeTrue();
        domain.MakePrimary(CreatedAt.AddMinutes(2)).ShouldBeFalse();
        domain.IsPrimary.ShouldBeTrue();

        domain.Demote(CreatedAt.AddMinutes(3)).ShouldBeTrue();
        domain.Demote(CreatedAt.AddMinutes(4)).ShouldBeFalse();
        domain.IsPrimary.ShouldBeFalse();
    }

    [Test]
    public void ChangesCannotPredateCreation()
    {
        var domain = OrganizationDomain.Create(
            Guid.NewGuid(),
            "shop.example.com",
            false,
            CreatedAt
        );

        Should.Throw<DomainInvariantException>(() => domain.Verify(CreatedAt.AddTicks(-1)));
        Should.Throw<DomainInvariantException>(() => domain.MakePrimary(CreatedAt.AddTicks(-1)));
    }
}
