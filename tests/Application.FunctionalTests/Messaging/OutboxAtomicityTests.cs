using modular_mlm.Domain.Organizations;
using modular_mlm.Infrastructure.Messaging;

namespace modular_mlm.Application.FunctionalTests.Messaging;

using static Infrastructure.TestApp;

public sealed class OutboxAtomicityTests : TestBase
{
    [Test]
    public async Task BusinessEntityAndItsDomainEventCommitTogether()
    {
        var organization = Organization.Create(
            "Atomic Outbox",
            $"atomic-{Guid.NewGuid():N}",
            "PHP"
        );
        await AddAsync(organization);

        (await FindAsync<Organization>(organization.Id)).ShouldNotBeNull();
        (
            await CountAsync<OutboxMessage>(message => message.OrganizationId == organization.Id)
        ).ShouldBeGreaterThan(0);
    }
}
