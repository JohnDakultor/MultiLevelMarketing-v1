using Microsoft.AspNetCore.Http.HttpResults;
using modular_mlm.Application.Identity.Commands.InviteAdministrator;
using modular_mlm.Application.Identity.Commands.RevokeAdministratorInvitation;
using modular_mlm.Application.Identity.Commands.RevokeAdministratorRole;
using modular_mlm.Application.Identity.Queries.GetAdministratorInvitations;
using modular_mlm.Application.Identity.Queries.GetAdministratorInvitations.Models;
using modular_mlm.Application.Identity.Queries.GetAdministrators;
using modular_mlm.Application.Identity.Queries.GetAdministrators.Models;
using modular_mlm.Domain.Constants;
using modular_mlm.Domain.Organizations;
using modular_mlm.Web.Infrastructure.Security;

namespace modular_mlm.Web.Endpoints;

public sealed class Administrators : IEndpointGroup
{
    public static string RoutePrefix => "/api/organizations/{organizationId:guid}/administrators";

    public static void Map(RouteGroupBuilder group)
    {
        group.RequireAuthorization(policy => policy.RequireRole(Roles.Administrator));

        group.MapGet(GetAdministrators);
        group
            .MapPost(InviteAdministrator, "invitations")
            .RequireRateLimiting(RateLimitPolicyNames.Invitation);
        group.MapGet(GetAdministratorInvitations, "invitations");
        group
            .MapPost(RevokeAdministratorInvitation, "invitations/{invitationId:guid}/revoke")
            .RequireRateLimiting(RateLimitPolicyNames.Invitation);
        group.MapPost(RevokeAdministratorRole, "{administratorUserId:guid}/revoke-role");
    }

    public static async Task<Ok<List<AdministratorDto>>> GetAdministrators(
        ISender sender,
        Guid organizationId,
        int page = 1,
        int pageSize = 20
    ) =>
        TypedResults.Ok(
            await sender.Send(new GetAdministratorsQuery(organizationId, page, pageSize))
        );

    public static async Task<Ok<Guid>> InviteAdministrator(
        ISender sender,
        Guid organizationId,
        InviteAdministratorRequest request
    ) =>
        TypedResults.Ok(
            await sender.Send(new InviteAdministratorCommand(organizationId, request.Email))
        );

    public static async Task<Ok<List<AdministratorInvitationDto>>> GetAdministratorInvitations(
        ISender sender,
        Guid organizationId,
        AdministratorInvitationStatus status = AdministratorInvitationStatus.Pending,
        int page = 1,
        int pageSize = 20
    ) =>
        TypedResults.Ok(
            await sender.Send(
                new GetAdministratorInvitationsQuery(organizationId, status, page, pageSize)
            )
        );

    public static async Task<NoContent> RevokeAdministratorInvitation(
        ISender sender,
        Guid organizationId,
        Guid invitationId,
        RevokeAdministratorInvitationRequest request
    )
    {
        await sender.Send(
            new RevokeAdministratorInvitationCommand(organizationId, invitationId, request.Reason)
        );
        return TypedResults.NoContent();
    }

    public static async Task<NoContent> RevokeAdministratorRole(
        ISender sender,
        Guid organizationId,
        Guid administratorUserId,
        RevokeAdministratorRoleRequest request
    )
    {
        await sender.Send(
            new RevokeAdministratorRoleCommand(organizationId, administratorUserId, request.Reason)
        );
        return TypedResults.NoContent();
    }
}

public sealed record InviteAdministratorRequest(string Email);

public sealed record RevokeAdministratorInvitationRequest(string Reason);

public sealed record RevokeAdministratorRoleRequest(string Reason);
