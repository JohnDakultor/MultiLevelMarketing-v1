using modular_mlm.Application.Common.Auditing;
using modular_mlm.Application.Common.Interfaces;
using modular_mlm.Application.Compensation.Commands.CreateCommissionPlan;
using modular_mlm.Application.Compensation.Commands.PublishCommissionPlan;
using modular_mlm.Application.Compensation.Commands.RetireCommissionPlan;
using modular_mlm.Application.Payouts.Commands.ProcessPayout;
using modular_mlm.Application.Payouts.Commands.RejectPayout;
using modular_mlm.Application.Payouts.Commands.RejectPayoutAccount;
using modular_mlm.Application.Payouts.Commands.VerifyPayoutAccount;
using modular_mlm.Domain.AuditLogs;
using modular_mlm.Domain.Compensation;
using modular_mlm.Domain.Network;
using modular_mlm.Domain.Organizations;
using modular_mlm.Domain.Payouts;
using modular_mlm.Domain.Wallets;

namespace modular_mlm.Application.FunctionalTests.Auditing;

using static Infrastructure.TestApp;

public sealed class FinancialOperationAuditTests : Infrastructure.TestBase
{
    [Test]
    public async Task CommissionPlanLifecycleProducesCreatePublishAndReasonedRetireAudits()
    {
        var organization = Organization.Create(
            "Financial Audit",
            $"financial-audit-{Guid.NewGuid():N}",
            "PHP"
        );
        await AddAsync(organization);
        var actorId = Guid.Parse(await RunAsAdministratorAsync(organization.Id));
        var effectiveFrom = DateTimeOffset.UtcNow.AddMinutes(-1);
        var planId = await SendAsync(
            new CreateCommissionPlanCommand(
                organization.Id,
                "Standard Plan",
                1,
                effectiveFrom,
                0.10m,
                false,
                null,
                ProcessingFrequency.Daily
            )
        );

        await SendAsync(new PublishCommissionPlanCommand(organization.Id, planId));
        const string reason = "Plan replaced after annual compensation review";
        await SendAsync(
            new RetireCommissionPlanCommand(
                organization.Id,
                planId,
                effectiveFrom.AddDays(30),
                reason
            )
        );

        foreach (
            var definition in new[]
            {
                AuditCoverageMap.CommissionPlanCreated,
                AuditCoverageMap.CommissionPlanPublished,
                AuditCoverageMap.CommissionPlanRetired,
            }
        )
        {
            var audit = await SingleAsync<AuditLog>(entry =>
                entry.OrganizationId == organization.Id
                && entry.EntityId == planId
                && entry.Action == definition.Action
            );
            audit.ActorUserId.ShouldBe(actorId);
            audit.EntityType.ShouldBe(AuditEntityNames.CommissionPlan);
            audit.AfterJson.ShouldNotBeNullOrWhiteSpace();
            if (definition.RequiresReason)
                audit.Reason.ShouldBe(reason);
        }
    }

    [Test]
    public async Task PayoutAccountDecisionsAreAuditedWithoutProtectedAccountData()
    {
        var organization = Organization.Create(
            "Payout Account Audit",
            $"payout-account-audit-{Guid.NewGuid():N}",
            "PHP"
        );
        await AddAsync(organization);
        var actorId = Guid.Parse(await RunAsAdministratorAsync(organization.Id));
        var verifiedAgent = CreateAgent(organization.Id);
        var rejectedAgent = CreateAgent(organization.Id);
        await AddAsync(verifiedAgent);
        await AddAsync(rejectedAgent);
        var verified = CreateAccount(organization.Id, verifiedAgent.Id);
        var rejected = CreateAccount(organization.Id, rejectedAgent.Id);
        await AddAsync(verified);
        await AddAsync(rejected);

        await SendAsync(new VerifyPayoutAccountCommand(organization.Id, verified.Id));
        await SendAsync(new RejectPayoutAccountCommand(organization.Id, rejected.Id));

        foreach (
            var pair in new[]
            {
                (Id: verified.Id, Definition: AuditCoverageMap.PayoutAccountVerified),
                (Id: rejected.Id, Definition: AuditCoverageMap.PayoutAccountRejected),
            }
        )
        {
            var audit = await SingleAsync<AuditLog>(entry =>
                entry.EntityId == pair.Id && entry.Action == pair.Definition.Action
            );
            audit.OrganizationId.ShouldBe(organization.Id);
            audit.ActorUserId.ShouldBe(actorId);
            audit.EntityType.ShouldBe(AuditEntityNames.PayoutAccount);
            var payload = $"{audit.BeforeJson}{audit.AfterJson}";
            payload.ShouldNotContain("09171234567");
            payload.ShouldNotContain("protected-value");
        }
    }

    [Test]
    public async Task PayoutRejectionAndWalletHoldReleaseCommitWithAudit()
    {
        var organization = Organization.Create(
            "Payout Rejection Audit",
            $"payout-rejection-audit-{Guid.NewGuid():N}",
            "PHP"
        );
        await AddAsync(organization);
        var agent = CreateAgent(organization.Id);
        await AddAsync(agent);
        var wallet = AgentWallet.Open(organization.Id, agent.Id, "PHP");
        var account = CreateAccount(organization.Id, agent.Id);
        account.Verify();
        await AddAsync(wallet);
        await AddAsync(account);
        var payout = PayoutRequest.Request(
            organization.Id,
            agent.Id,
            account.Id,
            500m,
            "PHP",
            DateTimeOffset.UtcNow
        );
        payout.StartReview();
        await AddAsync(payout);
        var hold = WalletEntry.Create(
            wallet.Id,
            WalletEntryType.Hold,
            -payout.Amount,
            "PayoutRequest",
            payout.Id
        );
        await AddAsync(hold);
        var actorId = Guid.Parse(await RunAsAdministratorAsync(organization.Id));

        await SendAsync(new RejectPayoutCommand(organization.Id, payout.Id));

        (await FindAsync<PayoutRequest>(payout.Id))!.Status.ShouldBe(PayoutStatus.Rejected);
        (await CountAsync<WalletEntry>(entry => entry.ReversalOfEntryId == hold.Id)).ShouldBe(1);
        var audit = await SingleAsync<AuditLog>(entry =>
            entry.EntityId == payout.Id && entry.Action == AuditCoverageMap.PayoutRejected.Action
        );
        audit.OrganizationId.ShouldBe(organization.Id);
        audit.ActorUserId.ShouldBe(actorId);
        audit.EntityType.ShouldBe(AuditEntityNames.PayoutRequest);
    }

    [Test]
    public async Task ProviderFailureAuditsProcessingAndFailureAndReleasesHold()
    {
        var organization = Organization.Create(
            "Payout Failure Audit",
            $"payout-failure-audit-{Guid.NewGuid():N}",
            "PHP"
        );
        await AddAsync(organization);
        var agent = CreateAgent(organization.Id);
        await AddAsync(agent);
        var wallet = AgentWallet.Open(organization.Id, agent.Id, "PHP");
        var account = CreateAccount(organization.Id, agent.Id);
        account.Verify();
        await AddAsync(wallet);
        await AddAsync(account);
        var payout = PayoutRequest.Request(
            organization.Id,
            agent.Id,
            account.Id,
            500m,
            "PHP",
            DateTimeOffset.UtcNow
        );
        payout.StartReview();
        payout.Approve(DateTimeOffset.UtcNow);
        await AddAsync(payout);
        var hold = WalletEntry.Create(
            wallet.Id,
            WalletEntryType.Hold,
            -payout.Amount,
            "PayoutRequest",
            payout.Id
        );
        await AddAsync(hold);
        await RunAsAdministratorAsync(organization.Id);
        GetRequiredService<Infrastructure.TestPayoutProvider>().CreateResult =
            new ProviderPayoutResult(
                "batch-failed",
                "transfer-failed",
                "failed",
                "provider-reference",
                "recipient_rejected",
                "Recipient account was rejected"
            );

        await SendAsync(new ProcessPayoutCommand(organization.Id, payout.Id));

        (await FindAsync<PayoutRequest>(payout.Id))!.Status.ShouldBe(PayoutStatus.Failed);
        (await CountAsync<WalletEntry>(entry => entry.ReversalOfEntryId == hold.Id)).ShouldBe(1);
        foreach (
            var definition in new[]
            {
                AuditCoverageMap.PayoutProcessingStarted,
                AuditCoverageMap.PayoutFailed,
            }
        )
            (
                await CountAsync<AuditLog>(entry =>
                    entry.EntityId == payout.Id && entry.Action == definition.Action
                )
            ).ShouldBe(1);
    }

    private static Agent CreateAgent(Guid organizationId) =>
        Agent.Apply(
            organizationId,
            Guid.NewGuid().ToString(),
            $"AG{Guid.NewGuid():N}"[..12],
            $"RF{Guid.NewGuid():N}"[..12],
            DateTimeOffset.UtcNow
        );

    private static PayoutAccount CreateAccount(Guid organizationId, Guid agentId) =>
        PayoutAccount.Register(
            organizationId,
            agentId,
            "gcash",
            "****4567",
            "Juan Dela Cruz",
            "protected-value",
            "GCASH",
            "instapay"
        );
}
