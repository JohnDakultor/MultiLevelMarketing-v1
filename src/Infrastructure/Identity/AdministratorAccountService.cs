using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using modular_mlm.Application.Common.Interfaces;
using modular_mlm.Application.Common.Models;
using modular_mlm.Domain.Constants;
using modular_mlm.Infrastructure.Data;

namespace modular_mlm.Infrastructure.Identity;

public class AdministratorAccountService(
    ApplicationDbContext context,
    UserManager<ApplicationUser> userManager,
    RoleManager<IdentityRole> roleManager,
    ILogger<AdministratorAccountService> logger
) : IAdministratorAccountService
{
    public async Task<bool> CanManageOrganizationAsync(
        string userId,
        Guid organizationId,
        CancellationToken cancellationToken
    )
    {
        if (string.IsNullOrWhiteSpace(userId) || organizationId == Guid.Empty)
            return false;

        var user = await userManager.Users.SingleOrDefaultAsync(
            candidate => candidate.Id == userId,
            cancellationToken
        );

        return user is not null
            && user.OrganizationId == organizationId
            && await userManager.IsInRoleAsync(user, Roles.Administrator);
    }

    public async Task<IReadOnlyList<AdministratorAccountSummary>> GetAdministratorsAsync(
        Guid organizationId,
        int page,
        int pageSize,
        CancellationToken cancellationToken
    )
    {
        var administratorRoleId = await context
            .Roles.AsNoTracking()
            .Where(role => role.NormalizedName == Roles.Administrator.ToUpperInvariant())
            .Select(role => role.Id)
            .SingleOrDefaultAsync(cancellationToken);

        if (administratorRoleId is null)
            return [];

        return await (
            from user in context.Users.AsNoTracking()
            join userRole in context.UserRoles.AsNoTracking() on user.Id equals userRole.UserId
            where user.OrganizationId == organizationId && userRole.RoleId == administratorRoleId
            orderby user.DisplayName, user.Email
            select new AdministratorAccountSummary(
                user.Id,
                organizationId,
                user.DisplayName,
                user.Email ?? string.Empty,
                user.EmailConfirmed
            )
        )
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);
    }

    public async Task<bool> AdministratorExistsAsync(
        Guid organizationId,
        string email,
        CancellationToken cancellationToken
    )
    {
        if (organizationId == Guid.Empty)
        {
            throw new ArgumentException("Organization Id is required");
        }

        if (string.IsNullOrWhiteSpace(email))
        {
            throw new ArgumentException("Email is required");
        }

        var normalizedEmail = userManager.NormalizeEmail(email);

        var user = userManager
            .Users.Where(x =>
                x.OrganizationId == organizationId && x.NormalizedEmail == normalizedEmail
            )
            .FirstOrDefault();

        return user is not null && await userManager.IsInRoleAsync(user, Roles.Administrator);
    }

    public async Task<Guid> CreateAdministratorAsync(
        Guid organizationId,
        string email,
        string displayName,
        string password,
        CancellationToken cancellationToken
    )
    {
        if (organizationId == Guid.Empty)
        {
            throw new ArgumentException("Organization Id is required");
        }

        if (string.IsNullOrWhiteSpace(email))
        {
            throw new ArgumentException("Email is required");
        }

        if (string.IsNullOrWhiteSpace(displayName))
        {
            throw new ArgumentException("Display name is required");
        }

        if (string.IsNullOrWhiteSpace(password))
        {
            throw new ArgumentException("Password is required");
        }

        var normalizedEmail = userManager.NormalizeEmail(email);

        var existingUser = userManager
            .Users.Where(x =>
                x.OrganizationId == organizationId && x.NormalizedEmail == normalizedEmail
            )
            .FirstOrDefault();

        if (existingUser is not null)
        {
            throw new ArgumentException("User already exists");
        }

        var administratorExist = await roleManager.RoleExistsAsync(Roles.Administrator);

        if (administratorExist is false)
        {
            throw new ArgumentException("Administrator role is not configured");
        }

        var user = new ApplicationUser
        {
            OrganizationId = organizationId,
            UserName = email,
            NormalizedUserName = normalizedEmail,
            Email = email,
            NormalizedEmail = normalizedEmail,
            DisplayName = displayName,
        };

        user.EmailConfirmed = true;

        var createResult = await userManager.CreateAsync(user, password);

        if (!createResult.Succeeded)
        {
            throw new InvalidOperationException(
                string.Join("; ", createResult.Errors.Select(x => x.Description))
            );
        }

        var roleResult = await userManager.AddToRoleAsync(user, Roles.Administrator);

        if (!roleResult.Succeeded)
        {
            var deleteResult = await userManager.DeleteAsync(user);

            if (!deleteResult.Succeeded)
            {
                logger.LogError("Failed to delete user: {UserId}", user.Id);
            }

            throw new InvalidOperationException(
                string.Join("; ", roleResult.Errors.Select(x => x.Description))
            );
        }

        logger.LogInformation("Administrator created: {UserId}", user.Id);

        if (!Guid.TryParse(user.Id, out var userId))
        {
            await userManager.DeleteAsync(user);
            throw new InvalidOperationException(
                "The created administrator has an unsupported user identifier."
            );
        }

        return userId;
    }

    public string NormalizeEmail(string email)
    {
        if (string.IsNullOrWhiteSpace(email))
        {
            throw new ArgumentException("Email is required");
        }

        var normalizedEmail = userManager.NormalizeEmail(email);

        if (string.IsNullOrWhiteSpace(normalizedEmail))
        {
            throw new ArgumentException("Email could not be normalized");
        }

        return normalizedEmail;
    }

    public async Task StageAdministratorRoleRevocationAsync(
        Guid organizationId,
        Guid administratorUserId,
        CancellationToken cancellationToken
    )
    {
        var userId = administratorUserId.ToString();
        var administratorRoleId =
            await context
                .Roles.Where(role => role.NormalizedName == Roles.Administrator.ToUpperInvariant())
                .Select(role => role.Id)
                .SingleOrDefaultAsync(cancellationToken)
            ?? throw new InvalidOperationException("Administrator role is not configured.");
        var userBelongsToOrganization = await context.Users.AnyAsync(
            user => user.Id == userId && user.OrganizationId == organizationId,
            cancellationToken
        );
        if (!userBelongsToOrganization)
            throw new KeyNotFoundException("Administrator was not found.");

        var membership = await context.UserRoles.SingleOrDefaultAsync(
            role => role.UserId == userId && role.RoleId == administratorRoleId,
            cancellationToken
        );
        if (membership is null)
            throw new InvalidOperationException("The user is not an administrator.");
        var administratorCount = await context.UserRoles.CountAsync(
            role =>
                role.RoleId == administratorRoleId
                && context.Users.Any(user =>
                    user.Id == role.UserId && user.OrganizationId == organizationId
                ),
            cancellationToken
        );
        if (administratorCount <= 1)
            throw new InvalidOperationException(
                "The final Organization administrator cannot be revoked."
            );

        context.UserRoles.Remove(membership);
    }
}
