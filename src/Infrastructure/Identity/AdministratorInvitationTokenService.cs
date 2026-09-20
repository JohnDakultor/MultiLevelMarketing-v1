using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.WebUtilities;
using modular_mlm.Application.Common.Interfaces;
using modular_mlm.Application.Common.Models;

namespace modular_mlm.Infrastructure.Identity;

public class AdministratorInvitationTokenService : IAdministratorInvitationTokenService
{
    private const int TOKEN_SIZE_IN_BYTES = 32;

    public InvitationTokenPair GenerateToken()
    {
        var randomBytes = RandomNumberGenerator.GetBytes(TOKEN_SIZE_IN_BYTES);

        var rawToken = WebEncoders.Base64UrlEncode(randomBytes);

        var tokenHashed = HashToken(rawToken).TokenHash;

        return new InvitationTokenPair(rawToken, tokenHashed);
    }

    public InvitationTokenPair HashToken(string rawToken)
    {
        if (string.IsNullOrEmpty(rawToken))
        {
            throw new ArgumentNullException(nameof(rawToken));
        }

        var tokenStringBytes = Encoding.UTF8.GetBytes(rawToken);

        var hashBytes = SHA256.HashData(tokenStringBytes);

        var stringHash = Convert.ToBase64String(hashBytes);

        return new InvitationTokenPair(rawToken, stringHash);
    }

    public bool VerifyToken(string rawToken, string storedTokenHash)
    {
        if (string.IsNullOrEmpty(rawToken))
        {
            throw new ArgumentNullException(nameof(rawToken));
        }

        if (string.IsNullOrEmpty(storedTokenHash))
        {
            throw new ArgumentNullException(nameof(storedTokenHash));
        }

        InvitationTokenPair calculatedPair = HashToken(rawToken);

        byte[] calculatedHashBytes = Convert.FromBase64String(calculatedPair.TokenHash);

        byte[] storedHashBytes = Convert.FromBase64String(storedTokenHash);

        if (calculatedHashBytes.Length != storedHashBytes.Length)
            return false;

        return CryptographicOperations.FixedTimeEquals(calculatedHashBytes, storedHashBytes);
    }
}
