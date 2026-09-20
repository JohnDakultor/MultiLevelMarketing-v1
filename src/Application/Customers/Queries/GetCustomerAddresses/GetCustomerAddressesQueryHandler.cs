using modular_mlm.Application.Customers.Queries.GetCustomerAddresses.Models;

namespace modular_mlm.Application.Customers.Queries.GetCustomerAddresses;

public sealed class GetCustomerAddressesQueryHandler(IApplicationDbContext db, IUser currentUser)
    : IRequestHandler<GetCustomerAddressesQuery, IReadOnlyList<CustomerAddressDto>>
{
    public async Task<IReadOnlyList<CustomerAddressDto>> Handle(
        GetCustomerAddressesQuery request,
        CancellationToken cancellationToken
    )
    {
        var userId = currentUser.Id ?? throw new UnauthorizedAccessException();
        var profile = await db
            .CustomerProfiles.AsNoTracking()
            .Where(candidate =>
                candidate.OrganizationId == request.OrganizationId && candidate.UserId == userId
            )
            .Select(candidate => new { candidate.Id, candidate.DefaultAddressId })
            .SingleOrDefaultAsync(cancellationToken);
        if (profile is null)
            throw new KeyNotFoundException(
                "A customer profile was not found for the current user in this organization."
            );

        return await db
            .CustomerAddresses.AsNoTracking()
            .Where(address =>
                address.OrganizationId == request.OrganizationId
                && address.CustomerProfileId == profile.Id
                && address.IsActive
            )
            .OrderByDescending(address => address.Id == profile.DefaultAddressId)
            .ThenBy(address => address.Label)
            .ThenBy(address => address.RecipientName)
            .ThenBy(address => address.Id)
            .Select(address => new CustomerAddressDto(
                address.Id,
                address.Label,
                address.RecipientName,
                address.PhoneNumber,
                address.AddressLine1,
                address.AddressLine2,
                address.Barangay,
                address.CityOrMunicipality,
                address.Province,
                address.PostalCode,
                address.CountryCode,
                address.Id == profile.DefaultAddressId
            ))
            .ToListAsync(cancellationToken);
    }
}
