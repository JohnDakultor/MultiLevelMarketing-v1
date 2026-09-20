using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using modular_mlm.Application.Common.Auditing;
using modular_mlm.Application.Common.Interfaces;
using modular_mlm.Domain.AuditLogs;
using modular_mlm.Domain.Organizations;
using modular_mlm.Infrastructure.Data;

namespace modular_mlm.Application.FunctionalTests.Auditing;

using static Infrastructure.TestApp;

public sealed class AuditContextAndRedactionTests : Infrastructure.TestBase
{
    [Test]
    public async Task WriterUsesTrustedActorAndRecursivelyRedactsSensitivePayloads()
    {
        var organization = Organization.Create(
            "Audit Integrity",
            $"audit-integrity-{Guid.NewGuid():N}",
            "PHP"
        );
        await AddAsync(organization);
        var actorId = Guid.Parse(await RunAsAdministratorAsync(organization.Id));
        var entityId = Guid.NewGuid();

        await ExecuteInScopeAsync(async services =>
        {
            var writer = services.GetRequiredService<IAuditWriter>();
            var db = services.GetRequiredService<ApplicationDbContext>();
            writer.Write(
                organization.Id,
                AuditCoverageMap.PayoutAccountVerified.Action,
                AuditCoverageMap.PayoutAccountVerified.EntityType,
                entityId,
                "{\"status\":\"Pending\",\"nested\":{\"secretKey\":\"never-store-me\"}}",
                "{\"status\":\"Verified\",\"accounts\":[{\"accountNumber\":\"09171234567\"}]}",
                "Ownership documents verified"
            );
            await db.SaveChangesAsync();
            return 0;
        });

        var audit = await SingleAsync<AuditLog>(entry => entry.EntityId == entityId);
        audit.OrganizationId.ShouldBe(organization.Id);
        audit.ActorUserId.ShouldBe(actorId);
        audit.Action.ShouldBe(AuditCoverageMap.PayoutAccountVerified.Action);
        audit.EntityType.ShouldBe(AuditCoverageMap.PayoutAccountVerified.EntityType);
        audit.Reason.ShouldBe("Ownership documents verified");
        audit.IpAddress.ShouldBe("0.0.0.0");
        audit.UserAgent.ShouldBe("Unknown");
        var beforePayload =
            audit.BeforeJson ?? throw new AssertionException("Missing before state.");
        var afterPayload = audit.AfterJson ?? throw new AssertionException("Missing after state.");
        beforePayload.ShouldContain("[REDACTED_BY_AUDIT_POLICY]");
        afterPayload.ShouldContain("[REDACTED_BY_AUDIT_POLICY]");
        beforePayload.ShouldNotContain("never-store-me");
        afterPayload.ShouldNotContain("09171234567");
    }

    [TestCase(EntityState.Modified)]
    [TestCase(EntityState.Deleted)]
    public async Task SavedAuditRecordCannotBeChangedOrDeleted(EntityState attemptedState)
    {
        var organization = Organization.Create(
            "Immutable Audit",
            $"immutable-audit-{Guid.NewGuid():N}",
            "PHP"
        );
        await AddAsync(organization);
        var actorId = Guid.Parse(await RunAsAdministratorAsync(organization.Id));
        var audit = AuditLog.Record(
            organization.Id,
            actorId,
            "TEST_ACTION",
            "TestEntity",
            Guid.NewGuid(),
            "{\"state\":\"before\"}",
            "{\"state\":\"after\"}",
            null,
            "127.0.0.1",
            "Tests",
            "trace-id"
        );
        await AddAsync(audit);

        var exception = await Should.ThrowAsync<InvalidOperationException>(() =>
            ExecuteInScopeAsync(async services =>
            {
                var db = services.GetRequiredService<ApplicationDbContext>();
                var persisted = await db.AuditLogs.SingleAsync(entry => entry.Id == audit.Id);
                if (attemptedState == EntityState.Modified)
                    db.Entry(persisted).Property(entry => entry.Reason).CurrentValue = "tampered";
                else
                    db.AuditLogs.Remove(persisted);
                await db.SaveChangesAsync();
                return 0;
            })
        );

        exception.Message.ShouldContain("immutable");
        (await CountAsync<AuditLog>(entry => entry.Id == audit.Id)).ShouldBe(1);
    }
}
