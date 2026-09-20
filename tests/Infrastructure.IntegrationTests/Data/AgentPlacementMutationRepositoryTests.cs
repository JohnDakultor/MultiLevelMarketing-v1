using modular_mlm.Application.Common.Interfaces;
using modular_mlm.Infrastructure.Data.Repositories;
using Shouldly;

namespace modular_mlm.Infrastructure.IntegrationTests.Data;

public sealed class AgentPlacementMutationRepositoryTests
{
    [Test]
    public void RepositoryImplementsTheSharedPlacementMutationBoundary()
    {
        typeof(IAgentPlacementMutationRepository)
            .IsAssignableFrom(typeof(AgentPlacementMutationRepository))
            .ShouldBeTrue();

        var methods = typeof(IAgentPlacementMutationRepository)
            .GetMethods()
            .Select(method => method.Name)
            .ToArray();
        methods.ShouldContain(nameof(IAgentPlacementMutationRepository.TryPlaceAsync));
        methods.ShouldContain(nameof(IAgentPlacementMutationRepository.TryMoveUncommittedAsync));
    }
}
