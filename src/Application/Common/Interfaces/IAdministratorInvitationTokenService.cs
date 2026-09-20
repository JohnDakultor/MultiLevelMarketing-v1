using modular_mlm.Application.Common.Models;

namespace modular_mlm.Application.Common.Interfaces;

public interface IAdministratorInvitationTokenService
{
    InvitationTokenPair GenerateToken();

    InvitationTokenPair HashToken(string rawToken);

    bool VerifyToken(string rawToken, string stroredTokenHash);
}
