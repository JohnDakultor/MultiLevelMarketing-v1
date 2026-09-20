using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using modular_mlm.Application.Common.Interfaces;
using modular_mlm.Application.Common.Models;
using modular_mlm.Domain.Constants;
using modular_mlm.Infrastructure.Data;
using modular_mlm.Infrastructure.Identity;

namespace modular_mlm.Infrastructure.Notifications;

public sealed class IdentityNotificationRecipientResolver(
    ApplicationDbContext db,
    UserManager<ApplicationUser> userManager
) : INotificationRecipientResolver
{
    public async Task<NotificationRecipient?> FindAsync(
        Guid organizationId,
        string userId,
        CancellationToken cancellationToken
    )
    {
        if (organizationId == Guid.Empty || string.IsNullOrWhiteSpace(userId))
            return null;

        return await (
            from user in userManager.Users.AsNoTracking()
            join organization in db.Organizations.AsNoTracking()
                on user.OrganizationId equals organization.Id
            where
                user.Id == userId
                && user.OrganizationId == organizationId
                && user.Email != null
            select new NotificationRecipient(
                user.Id,
                user.Email ?? string.Empty,
                string.IsNullOrWhiteSpace(user.DisplayName) ? "Member" : user.DisplayName,
                organization.Locale,
                user.EmailConfirmed
            )
        ).SingleOrDefaultAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<NotificationRecipient>> GetOrganizationAdministratorsAsync(
        Guid organizationId,
        CancellationToken cancellationToken
    )
    {
        var administratorRoleId = await db
            .Roles.AsNoTracking()
            .Where(role => role.NormalizedName == Roles.Administrator.ToUpperInvariant())
            .Select(role => role.Id)
            .SingleOrDefaultAsync(cancellationToken);
        if (administratorRoleId is null)
            return [];

        return await (
            from user in userManager.Users.AsNoTracking()
            join userRole in db.UserRoles.AsNoTracking() on user.Id equals userRole.UserId
            join organization in db.Organizations.AsNoTracking()
                on user.OrganizationId equals organization.Id
            where
                user.OrganizationId == organizationId
                && userRole.RoleId == administratorRoleId
                && user.Email != null
            orderby user.Id
            select new NotificationRecipient(
                user.Id,
                user.Email!,
                string.IsNullOrWhiteSpace(user.DisplayName) ? "Administrator" : user.DisplayName,
                organization.Locale,
                user.EmailConfirmed
            )
        ).ToListAsync(cancellationToken);
    }
}
