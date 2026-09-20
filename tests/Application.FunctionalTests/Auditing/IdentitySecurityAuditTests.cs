using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using modular_mlm.Application.Common.Auditing;
using modular_mlm.Application.Common.Interfaces;
using modular_mlm.Application.Identity.Commands.AcceptAdministratorInvitation;
using modular_mlm.Application.Identity.Commands.ExpireAdministratorInvitations;
using modular_mlm.Application.Identity.Commands.InviteAdministrator;
using modular_mlm.Application.Identity.Commands.RevokeAdministratorInvitation;
using modular_mlm.Application.Identity.Commands.RevokeAdministratorRole;
using modular_mlm.Domain.AuditLogs;
using modular_mlm.Domain.Organizations;
using modular_mlm.Infrastructure.Data;

namespace modular_mlm.Application.FunctionalTests.Auditing;

using static Infrastructure.TestApp;

public sealed class IdentitySecurityAuditTests : Infrastructure.TestBase
{
    [Test]
    public async Task AdministratorLoginSuccessAndFailureCaptureHttpSecurityContext()
    {
        var organization = await CreateOrganizationAsync();
        var actorId = Guid.Parse(await RunAsAdministratorAsync(organization.Id));
        using var client = FunctionalTestSetup.CreateClient();
        client.DefaultRequestHeaders.UserAgent.ParseAdd("AuditSecurityTests/1.0");

        using var successful = await client.PostAsJsonAsync(
            "/api/Users/login?useCookies=false&useSessionCookies=false",
            new { email = "administrator@local", password = "Administrator1234!" }
        );
        using var failed = await client.PostAsJsonAsync(
            "/api/Users/login?useCookies=false&useSessionCookies=false",
            new { email = "administrator@local", password = "incorrect-password" }
        );

        successful.IsSuccessStatusCode.ShouldBeTrue();
        failed.IsSuccessStatusCode.ShouldBeFalse();
        foreach (
            var definition in new[]
            {
                AuditCoverageMap.AdministratorLoginSucceeded,
                AuditCoverageMap.AdministratorLoginFailed,
            }
        )
        {
            var audit = await SingleAsync<AuditLog>(entry =>
                entry.OrganizationId == organization.Id
                && entry.EntityId == actorId
                && entry.Action == definition.Action
            );
            audit.ActorUserId.ShouldBe(actorId);
            audit.UserAgent!.ShouldContain("AuditSecurityTests/1.0");
            audit.IpAddress.ShouldNotBeNullOrWhiteSpace();
            audit.TraceId.ShouldNotBeNullOrWhiteSpace();
            $"{audit.BeforeJson}{audit.AfterJson}".ShouldNotContain("Administrator1234!");
            $"{audit.BeforeJson}{audit.AfterJson}".ShouldNotContain("incorrect-password");
        }
    }

    [Test]
    public async Task InvitationCreateAndAcceptAuditInvitationAndRoleGrantWithoutSecrets()
    {
        var organization = await CreateOrganizationAsync();
        await RunAsAdministratorAsync(organization.Id);
        const string invitedEmail = "audit-invite@example.test";
        const string password = "DoNotAuditThis1234!";

        var invitationId = await SendAsync(
            new InviteAdministratorCommand(organization.Id, invitedEmail)
        );
        var delivery = GetRequiredService<Infrastructure.TestAdministratorInvitationDelivery>();
        var rawToken = delivery.RawToken!;
        var administratorId = await SendAsync(
            new AcceptAdministratorInvitationCommand(
                rawToken,
                "Audit Administrator",
                password,
                password
            )
        );

        foreach (
            var definition in new[]
            {
                AuditCoverageMap.AdministratorInvitationCreated,
                AuditCoverageMap.AdministratorInvitationAccepted,
                AuditCoverageMap.AdministratorRoleGranted,
            }
        )
        {
            var entityId =
                definition == AuditCoverageMap.AdministratorRoleGranted
                    ? administratorId
                    : invitationId;
            var audit = await SingleAsync<AuditLog>(entry =>
                entry.OrganizationId == organization.Id
                && entry.EntityId == entityId
                && entry.Action == definition.Action
            );
            audit.EntityType.ShouldBe(definition.EntityType);
            var payload = $"{audit.BeforeJson}{audit.AfterJson}{audit.Reason}";
            payload.ShouldNotContain(rawToken);
            payload.ShouldNotContain(password);
            payload.ShouldNotContain(invitedEmail);
        }
    }

    [Test]
    public async Task RevokeAndExpireProduceReasonedTenantAuditEvents()
    {
        var organization = await CreateOrganizationAsync();
        var actorId = Guid.Parse(await RunAsAdministratorAsync(organization.Id));
        var revokeId = await SendAsync(
            new InviteAdministratorCommand(organization.Id, "revoke@example.test")
        );
        const string reason = "Invitation sent to the wrong recipient";

        await SendAsync(
            new RevokeAdministratorInvitationCommand(organization.Id, revokeId, reason)
        );
        var expired = AdministratorInvitation.Create(
            organization.Id,
            "expired@example.test",
            "EXPIRED@EXAMPLE.TEST",
            $"hash-{Guid.NewGuid():N}",
            actorId,
            DateTimeOffset.UtcNow.AddDays(-3),
            DateTimeOffset.UtcNow.AddDays(-2)
        );
        await AddAsync(expired);
        var expiryResult = await SendAsync(
            new ExpireAdministratorInvitationsCommand(organization.Id, 10, DateTimeOffset.UtcNow)
        );

        expiryResult.ExpiredCount.ShouldBe(1);
        var revokedAudit = await SingleAsync<AuditLog>(entry =>
            entry.EntityId == revokeId
            && entry.Action == AuditCoverageMap.AdministratorInvitationRevoked.Action
        );
        revokedAudit.ActorUserId.ShouldBe(actorId);
        revokedAudit.Reason.ShouldBe(reason);
        var expiredAudit = await SingleAsync<AuditLog>(entry =>
            entry.EntityId == expired.Id
            && entry.Action == AuditCoverageMap.AdministratorInvitationExpired.Action
        );
        expiredAudit.OrganizationId.ShouldBe(organization.Id);
        expiredAudit.EntityType.ShouldBe(AuditEntityNames.AdministratorInvitation);
    }

    [Test]
    public async Task AdministratorRoleRevocationIsAtomicReasonedAndAudited()
    {
        var organization = await CreateOrganizationAsync();
        var actorId = Guid.Parse(await RunAsAdministratorAsync(organization.Id));
        const string email = "role-revocation@example.test";
        var invitationId = await SendAsync(new InviteAdministratorCommand(organization.Id, email));
        var delivery = GetRequiredService<Infrastructure.TestAdministratorInvitationDelivery>();
        var targetId = await SendAsync(
            new AcceptAdministratorInvitationCommand(
                delivery.RawToken!,
                "Role Revocation Target",
                "Temporary1234!",
                "Temporary1234!"
            )
        );
        const string reason = "Administrator access is no longer required";

        await SendAsync(new RevokeAdministratorRoleCommand(organization.Id, targetId, reason));

        var administratorStillExists = await ExecuteInScopeAsync(services =>
            services
                .GetRequiredService<IAdministratorAccountService>()
                .AdministratorExistsAsync(organization.Id, email, CancellationToken.None)
        );
        administratorStillExists.ShouldBeFalse();
        var audit = await SingleAsync<AuditLog>(entry =>
            entry.OrganizationId == organization.Id
            && entry.EntityId == targetId
            && entry.Action == AuditCoverageMap.AdministratorRoleRevoked.Action
        );
        audit.ActorUserId.ShouldBe(actorId);
        audit.Reason.ShouldBe(reason);
        audit.EntityType.ShouldBe(AuditEntityNames.AdministratorAccount);
        (await FindAsync<AdministratorInvitation>(invitationId))!.Status.ShouldBe(
            AdministratorInvitationStatus.Accepted
        );
    }

    [Test]
    public async Task AdministratorPasswordAndTwoFactorChangesAuditInIdentitySave()
    {
        var organization = await CreateOrganizationAsync();
        var actorId = Guid.Parse(await RunAsAdministratorAsync(organization.Id));

        await ExecuteInScopeAsync(async services =>
        {
            var db = services.GetRequiredService<ApplicationDbContext>();
            var administrator = await db.Users.SingleAsync(user => user.Id == actorId.ToString());
            administrator.PasswordHash = "replacement-password-hash";
            administrator.TwoFactorEnabled = true;
            await db.SaveChangesAsync();
            return 0;
        });

        foreach (
            var definition in new[]
            {
                AuditCoverageMap.AdministratorPasswordChanged,
                AuditCoverageMap.AdministratorTwoFactorChanged,
            }
        )
        {
            var audit = await SingleAsync<AuditLog>(entry =>
                entry.OrganizationId == organization.Id
                && entry.EntityId == actorId
                && entry.Action == definition.Action
            );
            audit.ActorUserId.ShouldBe(actorId);
            audit.EntityType.ShouldBe(AuditEntityNames.AdministratorAccount);
            $"{audit.BeforeJson}{audit.AfterJson}{audit.Reason}".ShouldNotContain(
                "replacement-password-hash"
            );
        }
    }

    private static async Task<Organization> CreateOrganizationAsync()
    {
        var organization = Organization.Create(
            "Identity Audit",
            $"identity-audit-{Guid.NewGuid():N}",
            "PHP"
        );
        await AddAsync(organization);
        return organization;
    }
}
