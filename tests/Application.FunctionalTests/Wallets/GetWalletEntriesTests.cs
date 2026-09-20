using modular_mlm.Application.Common.Exceptions;
using modular_mlm.Application.Wallets.Queries.GetWalletEntries;
using modular_mlm.Domain.Network;
using modular_mlm.Domain.Organizations;
using modular_mlm.Domain.Wallets;

namespace modular_mlm.Application.FunctionalTests.Wallets;

using static Infrastructure.TestApp;

public sealed class GetWalletEntriesTests : TestBase
{
    [Test]
    public async Task AgentCanReadOwnEntriesAndAdministratorCanReadSameOrganizationEntries()
    {
        var data = await CreateWalletAsync();
        await AddEntryAsync(data.WalletId, WalletEntryType.AvailableCredit, 200m);

        var agentResult = await SendAsync(CreateQuery(data));

        agentResult.TotalCount.ShouldBe(1);
        agentResult.Items.Single().Amount.ShouldBe(200m);

        await RunAsAdministratorAsync(data.OrganizationId);
        var administratorResult = await SendAsync(CreateQuery(data));
        administratorResult.TotalCount.ShouldBe(1);
    }

    [Test]
    public async Task AgentCannotReadAnotherAgentsEntries()
    {
        var organization = Organization.Create(
            "Wallet Authorization",
            $"wallet-auth-{Guid.NewGuid():N}",
            "PHP"
        );
        await AddAsync(organization);
        var second = await CreateAgentWalletAsync(organization.Id, "second");
        var first = await CreateAgentWalletAsync(organization.Id, "first");

        await Should.ThrowAsync<ForbiddenAccessException>(() => SendAsync(CreateQuery(second)));
    }

    [Test]
    public async Task AdministratorCannotReadAnotherOrganizationsEntries()
    {
        var first = await CreateWalletAsync("first-organization");
        var second = await CreateWalletAsync("second-organization");
        await RunAsAdministratorAsync(first.OrganizationId);

        await Should.ThrowAsync<ForbiddenAccessException>(() => SendAsync(CreateQuery(second)));
    }

    [Test]
    public async Task PaginationUsesStableNewestFirstOrderingAndCorrectMetadata()
    {
        var data = await CreateWalletAsync();
        await AddEntryAsync(data.WalletId, WalletEntryType.AvailableCredit, 100m);
        await AddEntryAsync(data.WalletId, WalletEntryType.Hold, -20m);
        await AddEntryAsync(data.WalletId, WalletEntryType.Payout, -10m);
        var firstPage = await SendAsync(CreateQuery(data, page: 1, pageSize: 2));
        var secondPage = await SendAsync(CreateQuery(data, page: 2, pageSize: 2));

        firstPage.TotalCount.ShouldBe(3);
        firstPage.TotalPages.ShouldBe(2);
        firstPage.HasPreviousPage.ShouldBeFalse();
        firstPage.HasNextPage.ShouldBeTrue();
        secondPage.Items.Count.ShouldBe(1);
        secondPage.HasPreviousPage.ShouldBeTrue();
        secondPage.HasNextPage.ShouldBeFalse();

        var ordered = firstPage.Items.Concat(secondPage.Items).ToList();
        ordered.ShouldBe(
            ordered
                .OrderByDescending(entry => entry.CreatedAt)
                .ThenByDescending(entry => entry.Id)
                .ToList()
        );
        ordered.Select(entry => entry.Id).Distinct().Count().ShouldBe(3);
    }

    [Test]
    public async Task EntryTypeAndUtcDateRangeFiltersAreApplied()
    {
        var data = await CreateWalletAsync();
        var available = await AddEntryAsync(data.WalletId, WalletEntryType.AvailableCredit, 100m);
        var hold = await AddEntryAsync(data.WalletId, WalletEntryType.Hold, -25m);
        var payout = await AddEntryAsync(data.WalletId, WalletEntryType.Payout, -10m);
        var typeResult = await SendAsync(CreateQuery(data, entryType: WalletEntryType.Hold));
        var rangeResult = await SendAsync(
            CreateQuery(data, from: available.Created, to: payout.Created)
        );

        typeResult.Items.ShouldHaveSingleItem().Id.ShouldBe(hold.Id);
        rangeResult.Items.Select(entry => entry.Id).ShouldBe([hold.Id, available.Id]);
    }

    [Test]
    public async Task DtoIncludesLedgerLinkage()
    {
        var data = await CreateWalletAsync();
        var pending = await AddEntryAsync(data.WalletId, WalletEntryType.PendingCredit, 80m);
        var released = pending.Release(DateTimeOffset.UtcNow);
        await AddAsync(released);
        var result = await SendAsync(CreateQuery(data));
        var releasedDto = result.Items.Single(entry => entry.Id == released.Id);

        releasedDto.SourceType.ShouldBe(pending.SourceType);
        releasedDto.SourceId.ShouldBe(pending.SourceId);
        releasedDto.ReleasedFromEntryId.ShouldBe(pending.Id);
        releasedDto.ReversalOfEntryId.ShouldBeNull();
    }

    [Test]
    public async Task InvalidPaginationEnumAndDateRangeFailValidation()
    {
        var data = await CreateWalletAsync();
        var now = DateTimeOffset.UtcNow;

        await Should.ThrowAsync<ValidationException>(() => SendAsync(CreateQuery(data, page: 0)));
        await Should.ThrowAsync<ValidationException>(() =>
            SendAsync(CreateQuery(data, pageSize: 101))
        );
        await Should.ThrowAsync<ValidationException>(() =>
            SendAsync(CreateQuery(data, entryType: (WalletEntryType)999))
        );
        await Should.ThrowAsync<ValidationException>(() =>
            SendAsync(CreateQuery(data, from: now, to: now))
        );
        await Should.ThrowAsync<ValidationException>(() =>
            SendAsync(CreateQuery(data, from: now.ToOffset(TimeSpan.FromHours(8))))
        );
    }

    private static GetWalletEntriesQuery CreateQuery(
        WalletTestData data,
        int page = 1,
        int pageSize = 20,
        WalletEntryType? entryType = null,
        DateTimeOffset? from = null,
        DateTimeOffset? to = null
    ) => new(data.OrganizationId, data.AgentId, page, pageSize, entryType, from, to);

    private static async Task<WalletTestData> CreateWalletAsync(string label = "wallet-query")
    {
        var organization = Organization.Create(
            "Wallet Query Test",
            $"{label}-{Guid.NewGuid():N}",
            "PHP"
        );
        await AddAsync(organization);
        return await CreateAgentWalletAsync(organization.Id, label);
    }

    private static async Task<WalletTestData> CreateAgentWalletAsync(
        Guid organizationId,
        string label
    )
    {
        var userName = $"{label}-{Guid.NewGuid():N}@local";
        var userId = await RunAsUserAsync(userName, "Testing1234!", ["Agent"]);
        var now = DateTimeOffset.UtcNow;
        var agent = Agent.Apply(
            organizationId,
            userId,
            $"AG{Guid.NewGuid():N}"[..12],
            $"RF{Guid.NewGuid():N}"[..12],
            now
        );
        agent.Activate(now);
        var wallet = AgentWallet.Open(organizationId, agent.Id, "PHP");
        await AddAsync(agent);
        await AddAsync(wallet);
        return new WalletTestData(organizationId, agent.Id, wallet.Id, userName);
    }

    private static async Task<WalletEntry> AddEntryAsync(
        Guid walletId,
        WalletEntryType type,
        decimal amount
    )
    {
        var entry = WalletEntry.Create(walletId, type, amount, "WalletQueryTest", Guid.NewGuid());
        await AddAsync(entry);
        return entry;
    }

    private sealed record WalletTestData(
        Guid OrganizationId,
        Guid AgentId,
        Guid WalletId,
        string UserName
    );
}
