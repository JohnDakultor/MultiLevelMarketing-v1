using Domain.Enums;
using modular_mlm.Application.Common.Exceptions;
using modular_mlm.Application.Wallets.Commands.CreateWalletAdjustment;
using modular_mlm.Domain.AuditLogs;
using modular_mlm.Domain.Network;
using modular_mlm.Domain.Organizations;
using modular_mlm.Domain.Wallets;

namespace modular_mlm.Application.FunctionalTests.Wallets;

using static Infrastructure.TestApp;

public sealed class CreateWalletAdjustmentTests : TestBase
{
    [Test]
    public async Task CreditCreatesImmutableLedgerEntryAndAuditRecord()
    {
        var data = await CreateWalletAsync();
        await RunAsAdministratorAsync(data.OrganizationId);

        var entryId = await SendAsync(
            new CreateWalletAdjustmentCommand(
                data.OrganizationId,
                data.AgentId,
                WalletAdjustmentDirection.Credit,
                250.50m,
                "php",
                "Manual correction",
                "adjustment-credit-1"
            )
        );

        var entry = await FindAsync<WalletEntry>(entryId);
        entry.ShouldNotBeNull();
        entry.WalletId.ShouldBe(data.WalletId);
        entry.Type.ShouldBe(WalletEntryType.Adjustment);
        entry.Amount.ShouldBe(250.50m);
        entry.SourceType.ShouldBe("AdministratorAdjustment");
        entry.SourceId.ShouldBe(entry.Id);
        entry.IdempotencyKey.ShouldBe("adjustment-credit-1");
        entry.AvailableAt.ShouldNotBeNull();

        var audit = await SingleAsync<AuditLog>(candidate =>
            candidate.EntityId == entryId && candidate.Action == "WALLET_ADJUSTED"
        );
        audit.OrganizationId.ShouldBe(data.OrganizationId);
        audit.Reason.ShouldBe("Manual correction");
    }

    [Test]
    public async Task SameKeyAndPayloadReturnsOriginalEntryWithoutDuplicatingValue()
    {
        var data = await CreateWalletAsync();
        await RunAsAdministratorAsync(data.OrganizationId);
        var command = new CreateWalletAdjustmentCommand(
            data.OrganizationId,
            data.AgentId,
            WalletAdjustmentDirection.Credit,
            100m,
            "PHP",
            "Approved correction",
            "retry-safe-key"
        );

        var firstId = await SendAsync(command);
        var secondId = await SendAsync(command);

        secondId.ShouldBe(firstId);
        (
            await CountAsync<WalletEntry>(entry =>
                entry.WalletId == data.WalletId && entry.IdempotencyKey == "retry-safe-key"
            )
        ).ShouldBe(1);
        (
            await CountAsync<AuditLog>(audit =>
                audit.EntityId == firstId && audit.Action == "WALLET_ADJUSTED"
            )
        ).ShouldBe(1);
    }

    [Test]
    public async Task SameKeyWithDifferentPayloadIsRejected()
    {
        var data = await CreateWalletAsync();
        await RunAsAdministratorAsync(data.OrganizationId);

        await SendAsync(
            new CreateWalletAdjustmentCommand(
                data.OrganizationId,
                data.AgentId,
                WalletAdjustmentDirection.Credit,
                100m,
                "PHP",
                "First correction",
                "conflicting-key"
            )
        );

        await Should.ThrowAsync<IdempotencyConflictException>(() =>
            SendAsync(
                new CreateWalletAdjustmentCommand(
                    data.OrganizationId,
                    data.AgentId,
                    WalletAdjustmentDirection.Credit,
                    125m,
                    "PHP",
                    "First correction",
                    "conflicting-key"
                )
            )
        );
    }

    [Test]
    public async Task ConcurrentRetriesCreateExactlyOneEntry()
    {
        var data = await CreateWalletAsync();
        await RunAsAdministratorAsync(data.OrganizationId);
        var command = new CreateWalletAdjustmentCommand(
            data.OrganizationId,
            data.AgentId,
            WalletAdjustmentDirection.Credit,
            75m,
            "PHP",
            "Concurrent correction",
            "concurrent-key"
        );

        var ids = await Task.WhenAll(SendAsync(command), SendAsync(command));

        ids[1].ShouldBe(ids[0]);
        (
            await CountAsync<WalletEntry>(entry =>
                entry.WalletId == data.WalletId && entry.IdempotencyKey == "concurrent-key"
            )
        ).ShouldBe(1);
    }

    [Test]
    public async Task DebitCannotExceedConfiguredNegativeBalanceLimit()
    {
        var data = await CreateWalletAsync(maximumNegativeBalance: 100m);
        await RunAsAdministratorAsync(data.OrganizationId);

        await SendAsync(
            new CreateWalletAdjustmentCommand(
                data.OrganizationId,
                data.AgentId,
                WalletAdjustmentDirection.Debit,
                75m,
                "PHP",
                "First debit",
                "debit-1"
            )
        );

        await Should.ThrowAsync<InvalidOperationException>(() =>
            SendAsync(
                new CreateWalletAdjustmentCommand(
                    data.OrganizationId,
                    data.AgentId,
                    WalletAdjustmentDirection.Debit,
                    30m,
                    "PHP",
                    "Exceeds limit",
                    "debit-2"
                )
            )
        );
    }

    [Test]
    public async Task ReasonAndMonetaryPrecisionAreValidated()
    {
        var data = await CreateWalletAsync();
        await RunAsAdministratorAsync(data.OrganizationId);

        await Should.ThrowAsync<ValidationException>(() =>
            SendAsync(
                new CreateWalletAdjustmentCommand(
                    data.OrganizationId,
                    data.AgentId,
                    WalletAdjustmentDirection.Credit,
                    10m,
                    "PHP",
                    " ",
                    "missing-reason"
                )
            )
        );
        await Should.ThrowAsync<ValidationException>(() =>
            SendAsync(
                new CreateWalletAdjustmentCommand(
                    data.OrganizationId,
                    data.AgentId,
                    WalletAdjustmentDirection.Credit,
                    10.001m,
                    "PHP",
                    "Too precise",
                    "invalid-precision"
                )
            )
        );
        (await CountAsync<WalletEntry>()).ShouldBe(0);
    }

    [Test]
    public async Task CurrencyMustMatchWalletAndOrganization()
    {
        var data = await CreateWalletAsync();
        await RunAsAdministratorAsync(data.OrganizationId);

        var exception = await Should.ThrowAsync<InvalidOperationException>(() =>
            SendAsync(
                new CreateWalletAdjustmentCommand(
                    data.OrganizationId,
                    data.AgentId,
                    WalletAdjustmentDirection.Credit,
                    10m,
                    "USD",
                    "Wrong currency",
                    "wrong-currency"
                )
            )
        );

        exception.Message.ShouldContain("currency");
        (await CountAsync<WalletEntry>()).ShouldBe(0);
    }

    [Test]
    public async Task NonAdministratorCannotCreateAdjustment()
    {
        var data = await CreateWalletAsync();

        await Should.ThrowAsync<ForbiddenAccessException>(() =>
            SendAsync(
                new CreateWalletAdjustmentCommand(
                    data.OrganizationId,
                    data.AgentId,
                    WalletAdjustmentDirection.Credit,
                    10m,
                    "PHP",
                    "Unauthorized correction",
                    "not-admin"
                )
            )
        );
        (await CountAsync<WalletEntry>()).ShouldBe(0);
    }

    [Test]
    public async Task AdministratorCannotAdjustWalletInAnotherOrganization()
    {
        var first = await CreateWalletAsync();
        var second = await CreateWalletAsync();
        await RunAsAdministratorAsync(first.OrganizationId);

        await Should.ThrowAsync<ForbiddenAccessException>(() =>
            SendAsync(
                new CreateWalletAdjustmentCommand(
                    second.OrganizationId,
                    second.AgentId,
                    WalletAdjustmentDirection.Credit,
                    10m,
                    "PHP",
                    "Cross-organization correction",
                    "cross-organization"
                )
            )
        );
        (await CountAsync<WalletEntry>()).ShouldBe(0);
    }

    private static async Task<WalletTestData> CreateWalletAsync(decimal maximumNegativeBalance = 0m)
    {
        var organization = Organization.Create(
            "Wallet Adjustment Test",
            $"wallet-adjustment-{Guid.NewGuid():N}",
            "PHP"
        );
        organization.Wallet.Update(
            organization.Wallet.CommissionReleaseTrigger,
            0,
            0,
            10m,
            maximumNegativeBalance > 0m,
            maximumNegativeBalance
        );
        var now = DateTimeOffset.UtcNow;
        var agent = Agent.Apply(
            organization.Id,
            Guid.NewGuid().ToString(),
            $"AG{Guid.NewGuid():N}"[..12],
            $"RF{Guid.NewGuid():N}"[..12],
            now
        );
        agent.Activate(now);
        var wallet = AgentWallet.Open(organization.Id, agent.Id, "PHP");

        await AddAsync(organization);
        await AddAsync(agent);
        await AddAsync(wallet);

        return new WalletTestData(organization.Id, agent.Id, wallet.Id);
    }

    private sealed record WalletTestData(Guid OrganizationId, Guid AgentId, Guid WalletId);
}
