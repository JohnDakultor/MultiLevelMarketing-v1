namespace modular_mlm.Infrastructure.Identity;

public sealed class AdministratorInvitationOptions
{
    public const string SectionName = "AdministratorInvitations";

    public Uri AcceptanceBaseUrl { get; init; } =
        new("https://localhost:4200/admin/invitations/accept");
    public int LifetimeHours { get; init; } = 72;
}

public sealed class AdministratorInvitationPolicy(
    Microsoft.Extensions.Options.IOptions<AdministratorInvitationOptions> options
) : modular_mlm.Application.Common.Interfaces.IAdministratorInvitationPolicy
{
    public TimeSpan Lifetime { get; } = TimeSpan.FromHours(options.Value.LifetimeHours);
}
