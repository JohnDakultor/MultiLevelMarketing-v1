using Microsoft.AspNetCore.Http.HttpResults;
using modular_mlm.Application.Identity.Commands.AcceptAdministratorInvitation;

namespace modular_mlm.Web.Endpoints;

public sealed class AdministratorInvitations : IEndpointGroup
{
    public static string RoutePrefix => "/api/administrator-invitations";

    public static void Map(RouteGroupBuilder group)
    {
        group.RequireRateLimiting(Infrastructure.Security.RateLimitPolicyNames.Invitation);
        group.MapPost(AcceptInvitation, "accept").AllowAnonymous();
    }

    public static async Task<Ok<Guid>> AcceptInvitation(
        ISender sender,
        AcceptAdministratorInvitationRequest request
    ) =>
        TypedResults.Ok(
            await sender.Send(
                new AcceptAdministratorInvitationCommand(
                    request.RawToken,
                    request.DisplayName,
                    request.Password,
                    request.ConfirmPassword
                )
            )
        );
}

public sealed record AcceptAdministratorInvitationRequest(
    string RawToken,
    string DisplayName,
    string Password,
    string ConfirmPassword
);
