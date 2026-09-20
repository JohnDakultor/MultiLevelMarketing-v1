using modular_mlm.Application.FunctionalTests.Infrastructure;
using modular_mlm.Application.Identity.Commands.AcceptAdministratorInvitation;
using modular_mlm.Application.Identity.Commands.InviteAdministrator;
using modular_mlm.Domain.Organizations;
using modular_mlm.Infrastructure.Identity;

namespace modular_mlm.Application.FunctionalTests.Identity;

using static Infrastructure.TestApp;

public sealed class InviteAdministratorTests : TestBase
{
    [Test]
    public async Task ShouldPersistDeliverAndAcceptAnInvitation()
    {
        var organization = Organization.Create(
            "Invitation Test",
            $"invitation-{Guid.NewGuid():N}",
            "PHP"
        );
        await AddAsync(organization);
        await RunAsAdministratorAsync(organization.Id);

        var invitationId = await SendAsync(
            new InviteAdministratorCommand(organization.Id, "invited@example.test")
        );

        var invitation = await FindAsync<AdministratorInvitation>(invitationId);
        invitation.ShouldNotBeNull();
        invitation.Status.ShouldBe(AdministratorInvitationStatus.Pending);
        var delivery = GetRequiredService<TestAdministratorInvitationDelivery>();
        delivery.InvitationId.ShouldBe(invitationId);
        delivery.Recipient.ShouldBe("invited@example.test");
        delivery.RawToken.ShouldNotBeNullOrWhiteSpace();
        var firstToken = delivery.RawToken;

        var retriedInvitationId = await SendAsync(
            new InviteAdministratorCommand(organization.Id, "invited@example.test")
        );
        retriedInvitationId.ShouldBe(invitationId);
        delivery.RawToken.ShouldNotBe(firstToken);

        var administratorId = await SendAsync(
            new AcceptAdministratorInvitationCommand(
                delivery.RawToken!,
                "Invited Administrator",
                "Invitation1234!",
                "Invitation1234!"
            )
        );

        var accepted = await FindAsync<AdministratorInvitation>(invitationId);
        accepted!.Status.ShouldBe(AdministratorInvitationStatus.Accepted);
        accepted.AcceptedByUserId.ShouldBe(administratorId);
    }
}
