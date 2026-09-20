using modular_mlm.Application.Customers.Queries.GetCurrentCustomerProfile.Models;

namespace modular_mlm.Application.Customers.Queries.GetCurrentCustomerProfile;

public sealed class GetCurrentCustomerProfileQueryHandler(
    IApplicationDbContext db,
    IUser currentUser
) : IRequestHandler<GetCurrentCustomerProfileQuery, CustomerProfileDto>
{
    public async Task<CustomerProfileDto> Handle(
        GetCurrentCustomerProfileQuery request,
        CancellationToken cancellationToken
    )
    {
        var userId = currentUser.Id ?? throw new UnauthorizedAccessException();
        return await db
                .CustomerProfiles.AsNoTracking()
                .Where(profile =>
                    profile.OrganizationId == request.OrganizationId && profile.UserId == userId
                )
                .Select(profile => new CustomerProfileDto(
                    profile.Id,
                    profile.OrganizationId,
                    profile.DisplayName,
                    profile.DefaultAddressId
                ))
                .SingleOrDefaultAsync(cancellationToken)
            ?? throw new KeyNotFoundException(
                "A customer profile was not found for the current user in this organization."
            );
    }
}
