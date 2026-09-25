using modular_mlm.Application.Common.Auditing;
using modular_mlm.Application.Common.Exceptions;
using modular_mlm.Application.Compensation.Commands.PublishCommissionPlan;
using modular_mlm.Application.Compensation.Commands.RetireCommissionPlan;
using modular_mlm.Application.Compensation.Commands.UpdateCommissionPlan;
using modular_mlm.Domain.AuditLogs;
using modular_mlm.Domain.Compensation;
using modular_mlm.Domain.Exceptions;
using modular_mlm.Domain.Organizations;

namespace modular_mlm.Application.FunctionalTests.Compensation;

using static Infrastructure.TestApp;

public sealed class UpdateCommissionPlanTests : Infrastructure.TestBase
{
    [Test]
    public async Task UpdatesPercentagePairingAndWritesBeforeAfterAudit()
    {
        var data = await CreateDraftAsync();
        var actorId = Guid.Parse(await RunAsAdministratorAsync(data.Organization.Id));

        await SendAsync(
            CreateCommand(
                data,
                PairingCalculationType.PercentageMatchedVolume,
                binaryPairingRate: 0.12m,
                frequency: ProcessingFrequency.Weekly
            )
        );

        var plan = (await FindAsync<CommissionPlan>(data.Plan.Id))!;
        plan.ConfigurationVersion.ShouldBe(1);
        plan.DirectSalesRate.ShouldBe(0.25m);
        plan.BinaryPairing.Enabled.ShouldBeTrue();
        plan.BinaryPairing.CalculationType.ShouldBe(
            PairingCalculationType.PercentageMatchedVolume
        );
        plan.BinaryPairing.PairingRate.ShouldBe(0.12m);
        plan.BinaryPairing.ProcessingFrequency.ShouldBe(ProcessingFrequency.Weekly);

        var audit = await SingleAsync<AuditLog>(entry =>
            entry.OrganizationId == data.Organization.Id
            && entry.EntityId == data.Plan.Id
            && entry.Action == AuditCoverageMap.CommissionPlanUpdated.Action
        );
        audit.ActorUserId.ShouldBe(actorId);
        audit.EntityType.ShouldBe(AuditEntityNames.CommissionPlan);
        audit.BeforeJson.ShouldNotBeNullOrWhiteSpace();
        audit.AfterJson.ShouldNotBeNullOrWhiteSpace();
        audit.BeforeJson.ShouldNotBe(audit.AfterJson);
    }

    [Test]
    public async Task UpdatesFixedPerPairConfiguration()
    {
        var data = await CreateDraftAsync();
        await RunAsAdministratorAsync(data.Organization.Id);

        await SendAsync(
            CreateCommand(
                data,
                PairingCalculationType.FixedPerPair,
                pairUnitBv: 100m,
                fixedPairAmount: 35m,
                frequency: ProcessingFrequency.Daily
            )
        );

        var plan = (await FindAsync<CommissionPlan>(data.Plan.Id))!;
        plan.BinaryPairing.Enabled.ShouldBeTrue();
        plan.BinaryPairing.PairUnitBv.ShouldBe(100m);
        plan.BinaryPairing.FixedPairAmount.ShouldBe(35m);
        plan.BinaryPairing.PairingRate.ShouldBeNull();
    }

    [Test]
    public async Task DisablesPairingAndDirectSales()
    {
        var data = await CreateDraftAsync(configured: true);
        await RunAsAdministratorAsync(data.Organization.Id);

        await SendAsync(
            CreateCommand(
                data,
                PairingCalculationType.PercentageMatchedVolume,
                directSalesEnabled: false,
                directSalesRate: 0m,
                binaryPairingEnabled: false
            )
        );

        var plan = (await FindAsync<CommissionPlan>(data.Plan.Id))!;
        plan.DirectSalesRate.ShouldBe(0m);
        plan.BinaryPairing.Enabled.ShouldBeFalse();
        plan.BinaryPairing.PairingRate.ShouldBeNull();
    }

    [TestCase(CommissionPlanStatus.Active)]
    [TestCase(CommissionPlanStatus.Retired)]
    public async Task RejectsChangesToImmutablePlans(CommissionPlanStatus status)
    {
        var data = await CreateDraftAsync();
        await RunAsAdministratorAsync(data.Organization.Id);
        await SendAsync(new PublishCommissionPlanCommand(data.Organization.Id, data.Plan.Id));
        if (status == CommissionPlanStatus.Retired)
            await SendAsync(
                new RetireCommissionPlanCommand(
                    data.Organization.Id,
                    data.Plan.Id,
                    data.Plan.EffectiveFrom.AddDays(30),
                    "Lifecycle test"
                )
            );

        var currentVersion = (await FindAsync<CommissionPlan>(data.Plan.Id))!
            .ConfigurationVersion;
        var command = CreateCommand(
            data,
            PairingCalculationType.PercentageMatchedVolume,
            0.1m
        ) with
        {
            ExpectedConfigurationVersion = currentVersion,
        };

        await Should.ThrowAsync<DomainInvariantException>(() =>
            SendAsync(command)
        );

        (await CountAsync<AuditLog>(entry =>
            entry.OrganizationId == data.Organization.Id
            && entry.Action == AuditCoverageMap.CommissionPlanUpdated.Action
        )).ShouldBe(0);
    }

    [Test]
    public async Task DoesNotLoadAPlanFromAnotherOrganization()
    {
        var owned = await CreateDraftAsync();
        var foreign = await CreateDraftAsync();
        await RunAsAdministratorAsync(owned.Organization.Id);
        var command = CreateCommand(
            owned with { Plan = foreign.Plan },
            PairingCalculationType.PercentageMatchedVolume,
            0.1m
        ) with
        {
            OrganizationId = owned.Organization.Id,
        };

        await Should.ThrowAsync<KeyNotFoundException>(() => SendAsync(command));
    }

    [Test]
    public async Task RejectsStaleConfigurationVersion()
    {
        var data = await CreateDraftAsync();
        await RunAsAdministratorAsync(data.Organization.Id);
        var first = CreateCommand(data, PairingCalculationType.PercentageMatchedVolume, 0.1m);
        await SendAsync(first);

        await Should.ThrowAsync<DatabaseConcurrencyConflictException>(() => SendAsync(first));

        (await FindAsync<CommissionPlan>(data.Plan.Id))!.ConfigurationVersion.ShouldBe(1);
        (await CountAsync<AuditLog>(entry =>
            entry.EntityId == data.Plan.Id
            && entry.Action == AuditCoverageMap.CommissionPlanUpdated.Action
        )).ShouldBe(1);
    }

    [Test]
    public async Task ConcurrentUpdatesPersistOnlyOneConfigurationAndAudit()
    {
        var data = await CreateDraftAsync();
        await RunAsAdministratorAsync(data.Organization.Id);
        var percentage = CreateCommand(
            data,
            PairingCalculationType.PercentageMatchedVolume,
            binaryPairingRate: 0.1m
        );
        var fixedPerPair = CreateCommand(
            data,
            PairingCalculationType.FixedPerPair,
            pairUnitBv: 100m,
            fixedPairAmount: 25m
        );

        async Task<Exception?> Attempt(UpdateCommissionPlanCommand command)
        {
            try
            {
                await SendAsync(command);
                return null;
            }
            catch (Exception exception)
            {
                return exception;
            }
        }

        var outcomes = await Task.WhenAll(Attempt(percentage), Attempt(fixedPerPair));

        outcomes.Count(outcome => outcome is null).ShouldBe(1);
        outcomes.Count(outcome => outcome is DatabaseConcurrencyConflictException).ShouldBe(1);
        (await FindAsync<CommissionPlan>(data.Plan.Id))!.ConfigurationVersion.ShouldBe(1);
        (await CountAsync<AuditLog>(entry =>
            entry.EntityId == data.Plan.Id
            && entry.Action == AuditCoverageMap.CommissionPlanUpdated.Action
        )).ShouldBe(1);
    }

    [Test]
    public async Task RejectsInvalidPairingAndRuleJsonInputs()
    {
        var data = await CreateDraftAsync();
        await RunAsAdministratorAsync(data.Organization.Id);

        var invalidPairing = CreateCommand(
            data,
            PairingCalculationType.FixedPerPair,
            pairUnitBv: 0m,
            fixedPairAmount: -1m
        );
        await Should.ThrowAsync<ValidationException>(() => SendAsync(invalidPairing));

        var invalidJson = CreateCommand(
            data,
            PairingCalculationType.PercentageMatchedVolume,
            binaryPairingRate: 0.1m
        ) with
        {
            QualificationRulesJson = "not-json",
        };
        await Should.ThrowAsync<ValidationException>(() => SendAsync(invalidJson));
    }

    private static UpdateCommissionPlanCommand CreateCommand(
        TestData data,
        PairingCalculationType calculationType,
        decimal? binaryPairingRate = null,
        decimal? pairUnitBv = null,
        decimal? fixedPairAmount = null,
        ProcessingFrequency frequency = ProcessingFrequency.Monthly,
        bool directSalesEnabled = true,
        decimal directSalesRate = 0.25m,
        bool binaryPairingEnabled = true
    ) =>
        new(
            data.Organization.Id,
            data.Plan.Id,
            directSalesEnabled,
            directSalesRate,
            binaryPairingEnabled,
            calculationType,
            binaryPairingRate,
            pairUnitBv,
            fixedPairAmount,
            frequency,
            CarryForwardEnabled: true,
            QualificationRulesJson: "[]",
            CapRulesJson: "[]",
            ExpectedConfigurationVersion: data.Plan.ConfigurationVersion
        );

    private static async Task<TestData> CreateDraftAsync(bool configured = false)
    {
        var organization = Organization.Create(
            "Commission Update Tests",
            $"commission-update-{Guid.NewGuid():N}",
            "PHP"
        );
        await AddAsync(organization);
        var plan = CommissionPlan.Draft(
            organization.Id,
            "Standard",
            1,
            DateTimeOffset.UtcNow.AddDays(1)
        );
        if (configured)
        {
            plan.ConfigureDirectSales(0.10m);
            plan.ConfigureBinaryPairing(
                BinaryPairingRule.Percentage(0.05m, ProcessingFrequency.Monthly)
            );
        }
        await AddAsync(plan);
        return new TestData(organization, plan);
    }

    private sealed record TestData(Organization Organization, CommissionPlan Plan);
}
