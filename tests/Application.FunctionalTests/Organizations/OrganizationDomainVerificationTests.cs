using modular_mlm.Application.Common.Exceptions;
using modular_mlm.Application.Organizations.Commands.ConfigureOrganizationDomain;
using modular_mlm.Application.Organizations.Commands.VerifyOrganizationDomain;
using modular_mlm.Domain.Organizations;

namespace modular_mlm.Application.FunctionalTests.Organizations;

using static Infrastructure.TestApp;

[NonParallelizable]
public sealed class OrganizationDomainVerificationTests : TestBase
{
    [Test]
    public async Task CorrectDnsTokenVerifiesDomainAndWrongOrMissingTokenDoesNot()
    {
        var organization = Organization.Create("Domains", $"domains-{Guid.NewGuid():N}", "PHP");
        await AddAsync(organization);
        await RunAsAdministratorAsync(organization.Id);
        var domainId = await SendAsync(
            new ConfigureOrganizationDomainCommand(organization.Id, "shop.example.test", true)
        );
        var domain = await FindAsync<OrganizationDomain>(domainId);
        domain.ShouldNotBeNull();

        await Should.ThrowAsync<ConflictException>(() =>
            SendAsync(new VerifyOrganizationDomainCommand(organization.Id, domainId))
        );
        GetRequiredService<Infrastructure.TestDnsTxtRecordResolver>()
            .Add(domain.VerificationRecordName, "wrong-token");
        await Should.ThrowAsync<ConflictException>(() =>
            SendAsync(new VerifyOrganizationDomainCommand(organization.Id, domainId))
        );

        GetRequiredService<Infrastructure.TestDnsTxtRecordResolver>()
            .Add(domain.VerificationRecordName, domain.VerificationToken);
        await SendAsync(new VerifyOrganizationDomainCommand(organization.Id, domainId));
        (await FindAsync<OrganizationDomain>(domainId))!.IsVerified.ShouldBeTrue();

        // Verification is idempotent once ownership has been proven.
        await SendAsync(new VerifyOrganizationDomainCommand(organization.Id, domainId));
    }

    [Test]
    public async Task HostnameCannotBeClaimedByAnotherTenant()
    {
        var owner = Organization.Create("Owner", $"owner-{Guid.NewGuid():N}", "PHP");
        var other = Organization.Create("Other", $"other-{Guid.NewGuid():N}", "PHP");
        await AddAsync(owner);
        await AddAsync(other);
        await RunAsAdministratorAsync(owner.Id);
        await SendAsync(
            new ConfigureOrganizationDomainCommand(owner.Id, "owned.example.test", true)
        );
        await SetCurrentOrganizationAsync(other.Id);
        await Should.ThrowAsync<ConflictException>(() =>
            SendAsync(
                new ConfigureOrganizationDomainCommand(other.Id, "OWNED.EXAMPLE.TEST.", true)
            )
        );
    }
}
