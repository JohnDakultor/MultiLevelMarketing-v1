using modular_mlm.Application.Common.Models;
using modular_mlm.Domain.Commerce;
using modular_mlm.Domain.Constants;
using modular_mlm.Domain.Identity;
using modular_mlm.Domain.Organizations;

namespace modular_mlm.Application.Common.Services;

public sealed class CustomerContextProvisioner(
    IApplicationDbContext db,
    ICartSessionAccessor cartSessions,
    IIdentityService identities,
    IUser currentUser
)
{
    public async Task EnsureProvisionedAndMergeCartAsync(
        Guid organizationId,
        CancellationToken cancellationToken
    )
    {
        if (organizationId == Guid.Empty || string.IsNullOrWhiteSpace(currentUser.Id))
            return;

        var organizationIsActive = await db
            .Organizations.AsNoTracking()
            .AnyAsync(
                organization =>
                    organization.Id == organizationId
                    && organization.Status == OrganizationStatus.Active,
                cancellationToken
            );
        if (!organizationIsActive)
            throw new KeyNotFoundException("An active organization was not found.");

        var userId = currentUser.Id;
        var customer = await db.CustomerProfiles.SingleOrDefaultAsync(
            profile => profile.OrganizationId == organizationId && profile.UserId == userId,
            cancellationToken
        );

        var customerWasCreated = customer is null;
        if (customer is null)
        {
            var userName =
                await identities.GetUserNameAsync(userId)
                ?? throw new UnauthorizedAccessException(
                    "The authenticated identity no longer exists."
                );
            await EnsureCustomerRoleAsync(userId);

            customer = CustomerProfile.Create(organizationId, userId, CreateDisplayName(userName));
            db.CustomerProfiles.Add(customer);
        }

        var sessionId = cartSessions.GetSessionId();
        if (sessionId is null)
        {
            if (customerWasCreated)
                await db.SaveChangesAsync(cancellationToken);
            return;
        }

        var anonymousCart = await db
            .Carts.Include(cart => cart.Items)
            .SingleOrDefaultAsync(
                cart =>
                    cart.OrganizationId == organizationId
                    && cart.CustomerId == null
                    && cart.SessionId == sessionId,
                cancellationToken
            );
        if (anonymousCart is null)
        {
            if (customerWasCreated)
                await db.SaveChangesAsync(cancellationToken);
            cartSessions.ClearSession();
            return;
        }

        var customerCart = await db
            .Carts.Include(cart => cart.Items)
            .SingleOrDefaultAsync(
                cart => cart.OrganizationId == organizationId && cart.CustomerId == customer.Id,
                cancellationToken
            );

        if (customerCart is null)
            anonymousCart.AssignToCustomer(customer.Id);
        else
        {
            customerCart.MergeAnonymousCart(anonymousCart);
            db.Carts.Remove(anonymousCart);
        }

        await db.SaveChangesAsync(cancellationToken);
        cartSessions.ClearSession();
    }

    private async Task EnsureCustomerRoleAsync(string userId)
    {
        Result result = await identities.AddToRoleAsync(userId, Roles.Customer);
        if (!result.Succeeded)
            throw new InvalidOperationException(
                $"The customer role could not be assigned: {string.Join("; ", result.Errors)}"
            );
    }

    private static string CreateDisplayName(string userName)
    {
        var separator = userName.IndexOf('@');
        var candidate = separator > 0 ? userName[..separator] : userName;
        candidate = candidate.Trim();

        return string.IsNullOrWhiteSpace(candidate)
            ? "Customer"
            : candidate[..Math.Min(candidate.Length, CustomerProfile.MaximumDisplayNameLength)];
    }
}
