using Microsoft.Extensions.Options;
using modular_mlm.Application.Common.Interfaces;

namespace modular_mlm.Infrastructure.Identity;

public sealed class AdministratorInvitationDelivery(
    IEmailSender emailSender,
    IOptions<AdministratorInvitationOptions> options
) : IAdministratorInvitationDelivery
{
    private readonly AdministratorInvitationOptions _options = options.Value;

    public Task SendAsync(
        Guid invitationId,
        string recipient,
        string rawToken,
        CancellationToken cancellationToken
    )
    {
        var separator = string.IsNullOrEmpty(_options.AcceptanceBaseUrl.Query) ? "?" : "&";
        var url =
            $"{_options.AcceptanceBaseUrl}{separator}invitationId={invitationId:D}&token={Uri.EscapeDataString(rawToken)}";
        return emailSender.SendAsync(
            recipient,
            "You have been invited as an administrator",
            $"Accept your administrator invitation using this single-use link: {url}",
            cancellationToken
        );
    }
}
