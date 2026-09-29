using modular_mlm.Application.Wallets.Queries.GetAdminWallets;
using NUnit.Framework;
using Shouldly;

namespace modular_mlm.Application.UnitTests.Wallets;

public sealed class GetAdminWalletsQueryValidatorTests
{
    private readonly GetAdminWalletsQueryValidator _validator = new();

    [TestCase(null)]
    [TestCase("")]
    [TestCase("AGENT")]
    public async Task OptionalSearchValuesAreAccepted(string? search)
    {
        var result = await _validator.ValidateAsync(
            new GetAdminWalletsQuery(Guid.NewGuid(), 1, 20, search, null, false)
        );

        result.IsValid.ShouldBeTrue();
    }

    [Test]
    public async Task WhitespaceOnlySearchIsRejected()
    {
        var result = await _validator.ValidateAsync(
            new GetAdminWalletsQuery(Guid.NewGuid(), 1, 20, "   ", null, false)
        );

        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(error => error.PropertyName == "Search");
    }
}
