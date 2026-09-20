using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using modular_mlm.Application.Common.Interfaces;
using modular_mlm.Application.Common.Models;
using modular_mlm.Domain.Constants;

namespace modular_mlm.Infrastructure.Identity;

public class IdentityService : IIdentityService
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly RoleManager<IdentityRole> _roleManager;
    private readonly IUserClaimsPrincipalFactory<ApplicationUser> _userClaimsPrincipalFactory;
    private readonly IAuthorizationService _authorizationService;

    public IdentityService(
        UserManager<ApplicationUser> userManager,
        RoleManager<IdentityRole> roleManager,
        IUserClaimsPrincipalFactory<ApplicationUser> userClaimsPrincipalFactory,
        IAuthorizationService authorizationService
    )
    {
        _userManager = userManager;
        _roleManager = roleManager;
        _userClaimsPrincipalFactory = userClaimsPrincipalFactory;
        _authorizationService = authorizationService;
    }

    public async Task<string?> GetUserNameAsync(string userId)
    {
        var user = await _userManager.FindByIdAsync(userId);

        return user?.UserName;
    }

    public async Task<(Result Result, string UserId)> CreateUserAsync(
        string userName,
        string password
    )
    {
        var user = new ApplicationUser { UserName = userName, Email = userName };

        var result = await _userManager.CreateAsync(user, password);

        return (result.ToApplicationResult(), user.Id);
    }

    public async Task<bool> IsInRoleAsync(string userId, string role)
    {
        var user = await _userManager.FindByIdAsync(userId);

        return user != null && await _userManager.IsInRoleAsync(user, role);
    }

    public async Task<Result> AddToRoleAsync(string userId, string role)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user is null)
            return Result.Failure(["Identity user was not found."]);

        if (!await _roleManager.RoleExistsAsync(role))
        {
            var createRole = await _roleManager.CreateAsync(new IdentityRole(role));
            if (!createRole.Succeeded && !await _roleManager.RoleExistsAsync(role))
                return createRole.ToApplicationResult();
        }

        if (await _userManager.IsInRoleAsync(user, role))
            return Result.Success();

        return (await _userManager.AddToRoleAsync(user, role)).ToApplicationResult();
    }

    public async Task<Result> GrantAgentAccessAsync(
        string userId,
        Guid organizationId,
        CancellationToken cancellationToken
    )
    {
        var user = await _userManager.Users.SingleOrDefaultAsync(
            candidate => candidate.Id == userId,
            cancellationToken
        );
        if (user is null)
            return Result.Failure(["The Agent's identity account was not found."]);

        if (
            user.OrganizationId is { } assignedOrganizationId
            && assignedOrganizationId != organizationId
        )
            return Result.Failure([
                "The identity account is already assigned to another Organization.",
            ]);

        if (!await _roleManager.RoleExistsAsync(Roles.Agent))
        {
            var roleResult = await _roleManager.CreateAsync(new IdentityRole(Roles.Agent));
            if (!roleResult.Succeeded)
                return roleResult.ToApplicationResult();
        }

        if (user.OrganizationId != organizationId)
        {
            user.OrganizationId = organizationId;
            var updateResult = await _userManager.UpdateAsync(user);
            if (!updateResult.Succeeded)
                return updateResult.ToApplicationResult();
        }

        if (await _userManager.IsInRoleAsync(user, Roles.Agent))
            return Result.Success();

        return (await _userManager.AddToRoleAsync(user, Roles.Agent)).ToApplicationResult();
    }

    public async Task<bool> AuthorizeAsync(string userId, string policyName)
    {
        var user = await _userManager.FindByIdAsync(userId);

        if (user == null)
        {
            return false;
        }

        var principal = await _userClaimsPrincipalFactory.CreateAsync(user);

        var result = await _authorizationService.AuthorizeAsync(principal, policyName);

        return result.Succeeded;
    }

    public async Task<Result> DeleteUserAsync(string userId)
    {
        var user = await _userManager.FindByIdAsync(userId);

        return user != null ? await DeleteUserAsync(user) : Result.Success();
    }

    public async Task<Result> DeleteUserAsync(ApplicationUser user)
    {
        var result = await _userManager.DeleteAsync(user);

        return result.ToApplicationResult();
    }
}
