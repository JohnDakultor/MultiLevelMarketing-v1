using modular_mlm.Application.Auditing.Queries.GetAuditTrail;
using modular_mlm.Application.Common.Exceptions;
using modular_mlm.Application.FunctionalTests.Infrastructure;
using modular_mlm.Domain.AuditLogs;
using modular_mlm.Domain.Organizations;

namespace modular_mlm.Application.FunctionalTests.Auditing;

using static Infrastructure.TestApp;

public sealed class AuditTrailTests : TestBase
{
    [Test]
    public async Task ShouldReturnOnlyTenantAuditRecordsAndRedactSecrets()
    {
        var organization = Organization.Create("Audit Test", $"audit-{Guid.NewGuid():N}", "PHP");
        await AddAsync(organization);
        var userId = Guid.Parse(await RunAsAdministratorAsync(organization.Id));
        var entityId = Guid.NewGuid();
        await AddAsync(
            AuditLog.Record(
                organization.Id,
                userId,
                "payout_account_updated",
                "PayoutAccount",
                entityId,
                "{\"status\":\"Pending\",\"secretKey\":\"old-secret\"}",
                "{\"status\":\"Verified\",\"payoutAccount\":\"123456\"}",
                "Administrator verified account ownership.",
                "127.0.0.1",
                "FunctionalTests/1.0",
                "trace-functional"
            )
        );

        var result = await SendAsync(
            new GetAuditTrailQuery(
                organization.Id,
                Action: "PAYOUT_ACCOUNT_UPDATED",
                EntityId: entityId
            )
        );

        result.Count.ShouldBe(1);
        var audit = result.Single();
        audit.ActorUserId.ShouldBe(userId);
        audit.IpAddress.ShouldBe("127.0.0.1");
        audit.UserAgent.ShouldBe("FunctionalTests/1.0");
        audit.TraceId.ShouldBe("trace-functional");
        audit.BeforeJson!.ShouldContain("[REDACTED_BY_AUDIT_POLICY]");
        audit.BeforeJson!.ShouldNotContain("old-secret");
        audit.AfterJson!.ShouldNotContain("123456");
    }

    [Test]
    public async Task ShouldForbidCrossOrganizationAuditQueries()
    {
        var owned = Organization.Create("Owned", $"owned-{Guid.NewGuid():N}", "PHP");
        var foreign = Organization.Create("Foreign", $"foreign-{Guid.NewGuid():N}", "PHP");
        await AddAsync(owned);
        await AddAsync(foreign);
        await RunAsAdministratorAsync(owned.Id);

        await Should.ThrowAsync<ForbiddenAccessException>(() =>
            SendAsync(new GetAuditTrailQuery(foreign.Id))
        );
    }
}
