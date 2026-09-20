using modular_mlm.Application.Network.Commands.AutoPlaceAgent;
using modular_mlm.Domain.Network;
using NUnit.Framework;
using Shouldly;

namespace modular_mlm.Application.UnitTests.Network.Commands;

public sealed class AutoPlaceAgentCommandValidatorTests
{
    private readonly AutoPlaceAgentCommandValidator _validator = new();

    [Test]
    public void AcceptsValidBreadthFirstCommand()
    {
        var command = new AutoPlaceAgentCommand(Guid.NewGuid(), Guid.NewGuid());

        _validator.Validate(command).IsValid.ShouldBeTrue();
    }

    [Test]
    public void RequiresPreferredSideForPreferredLegStrategy()
    {
        var command = new AutoPlaceAgentCommand(
            Guid.NewGuid(),
            Guid.NewGuid(),
            PlacementStrategyType.PreferredLeg
        );

        var result = _validator.Validate(command);

        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(error => error.PropertyName == nameof(command.PreferredSide));
    }

    [Test]
    public void RejectsEmptyOrganizationAndAgentIdentifiers()
    {
        var result = _validator.Validate(new AutoPlaceAgentCommand(Guid.Empty, Guid.Empty));

        result.Errors.ShouldContain(error => error.PropertyName == "OrganizationId");
        result.Errors.ShouldContain(error => error.PropertyName == "AgentId");
    }
}
