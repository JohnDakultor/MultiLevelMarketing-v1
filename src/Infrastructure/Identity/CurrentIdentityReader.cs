using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using modular_mlm.Application.Common.Interfaces;
using modular_mlm.Application.Common.Models;
using modular_mlm.Infrastructure.Data;

namespace modular_mlm.Infrastructure.Identity;

public sealed class CurrentIdentityReader(
    ApplicationDbContext db,
    UserManager<ApplicationUser> userManager
) : ICurrentIdentityReader
{
    public async Task<CurrentIdentitySummary?> GetByUserIdAsync(
        string userId,
        CancellationToken cancellationToken
    )
    {
        var user = await db
            .Users.AsNoTracking()
            .SingleOrDefaultAsync(candidate => candidate.Id == userId, cancellationToken);
        if (user is null)
            return null;
        var roles = await userManager.GetRolesAsync(user);
        return new CurrentIdentitySummary(
            user.Id,
            user.Email ?? string.Empty,
            string.IsNullOrWhiteSpace(user.DisplayName)
                ? user.Email ?? string.Empty
                : user.DisplayName,
            roles.Order(StringComparer.Ordinal).ToArray(),
            user.OrganizationId,
            user.EmailConfirmed,
            user.TwoFactorEnabled
        );
    }
}
