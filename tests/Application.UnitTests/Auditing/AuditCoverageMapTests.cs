using modular_mlm.Application.Common.Auditing;
using NUnit.Framework;
using Shouldly;

namespace modular_mlm.Application.UnitTests.Auditing;

public sealed class AuditCoverageMapTests
{
    [Test]
    public void EveryRegisteredActionIsUniqueAndMachineReadable()
    {
        var definitions = AuditCoverageMap.All;

        definitions.Count.ShouldBeGreaterThan(0);
        definitions
            .Select(definition => definition.Action)
            .Distinct()
            .Count()
            .ShouldBe(definitions.Count);
        definitions.ShouldAllBe(definition =>
            !string.IsNullOrWhiteSpace(definition.EntityType)
            && definition.Action.All(character =>
                char.IsUpper(character) || char.IsDigit(character) || character == '_'
            )
        );
    }

    [Test]
    public void FinancialAndAdministratorActionsResolveToExpectedEntities()
    {
        AuditCoverageMap.WalletAdjusted.EntityType.ShouldBe(AuditEntityNames.WalletEntry);
        AuditCoverageMap.WalletAdjusted.RequiresReason.ShouldBeTrue();
        AuditCoverageMap.OrderItemRefundReversed.EntityType.ShouldBe(
            AuditEntityNames.OrderItemRefund
        );
        AuditCoverageMap.AdministratorInvitationRevoked.EntityType.ShouldBe(
            AuditEntityNames.AdministratorInvitation
        );
    }

    [Test]
    public void GetByActionReturnsRegisteredDefinition()
    {
        var definition = AuditCoverageMap.GetByAction(AuditActionNames.AgentSuspended);

        definition.ShouldBeSameAs(AuditCoverageMap.AgentSuspended);
    }

    [Test]
    public void GetByActionRejectsUnknownOrBlankActions()
    {
        Should.Throw<ArgumentException>(() => AuditCoverageMap.GetByAction(" "));
        Should.Throw<KeyNotFoundException>(() =>
            AuditCoverageMap.GetByAction("UNREGISTERED_ACTION")
        );
    }
}
