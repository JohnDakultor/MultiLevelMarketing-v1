using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using modular_mlm.Application.Common.Auditing;
using modular_mlm.Application.Common.Interfaces;
using modular_mlm.Domain.AuditLogs;
using modular_mlm.Domain.Organizations;
using modular_mlm.Infrastructure.Data;

namespace modular_mlm.Application.FunctionalTests.Auditing;

using static Infrastructure.TestApp;

public sealed class AuditAtomicityTests : Infrastructure.TestBase
{
    [Test]
    public async Task AuditWriterOnlyTracksRecordAndNeverCommitsIndependently()
    {
        var organization = Organization.Create(
            "Atomic Audit",
            $"atomic-audit-{Guid.NewGuid():N}",
            "PHP"
        );
        await AddAsync(organization);
        await RunAsAdministratorAsync(organization.Id);

        await ExecuteInScopeAsync(services =>
        {
            services
                .GetRequiredService<IAuditWriter>()
                .Write(
                    organization.Id,
                    AuditCoverageMap.BrandingUpdated.Action,
                    AuditCoverageMap.BrandingUpdated.EntityType,
                    organization.Id,
                    "{\"title\":\"before\"}",
                    "{\"title\":\"after\"}",
                    null
                );
            return Task.FromResult(0);
        });

        (await CountAsync<AuditLog>()).ShouldBe(0);
    }

    [Test]
    public async Task AuditRecordCommitsWithTheOwningUnitOfWork()
    {
        var organization = Organization.Create(
            "Atomic Commit",
            $"atomic-commit-{Guid.NewGuid():N}",
            "PHP"
        );
        await AddAsync(organization);
        await RunAsAdministratorAsync(organization.Id);

        await ExecuteInScopeAsync(async services =>
        {
            var db = services.GetRequiredService<ApplicationDbContext>();
            services
                .GetRequiredService<IAuditWriter>()
                .Write(
                    organization.Id,
                    AuditCoverageMap.FeaturesUpdated.Action,
                    AuditCoverageMap.FeaturesUpdated.EntityType,
                    organization.Id,
                    "{\"enabled\":false}",
                    "{\"enabled\":true}",
                    null
                );
            await db.SaveChangesAsync();
            return 0;
        });

        (
            await CountAsync<AuditLog>(entry =>
                entry.OrganizationId == organization.Id
                && entry.Action == AuditCoverageMap.FeaturesUpdated.Action
            )
        ).ShouldBe(1);
    }

    [Test]
    public async Task TransactionRollbackRemovesBothMutationAndAudit()
    {
        var organization = Organization.Create(
            "Before Rollback",
            $"atomic-rollback-{Guid.NewGuid():N}",
            "PHP"
        );
        await AddAsync(organization);
        await RunAsAdministratorAsync(organization.Id);

        await ExecuteInScopeAsync(async services =>
        {
            var db = services.GetRequiredService<ApplicationDbContext>();
            var strategy = db.Database.CreateExecutionStrategy();
            await strategy.ExecuteAsync(async () =>
            {
                await using var transaction = await db.Database.BeginTransactionAsync();
                var tracked = await db.Organizations.SingleAsync(entry =>
                    entry.Id == organization.Id
                );
                tracked.Rename("Must Be Rolled Back");
                services
                    .GetRequiredService<IAuditWriter>()
                    .Write(
                        organization.Id,
                        AuditCoverageMap.BrandingUpdated.Action,
                        AuditCoverageMap.BrandingUpdated.EntityType,
                        organization.Id,
                        "{\"name\":\"Before Rollback\"}",
                        "{\"name\":\"Must Be Rolled Back\"}",
                        null
                    );
                await db.SaveChangesAsync();
                await transaction.RollbackAsync();
            });
            return 0;
        });

        (await FindAsync<Organization>(organization.Id))!.Name.ShouldBe("Before Rollback");
        (
            await CountAsync<AuditLog>(entry =>
                entry.OrganizationId == organization.Id
                && entry.Action == AuditCoverageMap.BrandingUpdated.Action
            )
        ).ShouldBe(0);
    }
}
