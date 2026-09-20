using modular_mlm.Domain.Identity;

namespace modular_mlm.Application.Customers.Commands.AddCustomerAddress;

public sealed class AddCustomerAddressCommandHandler(IApplicationDbContext db, IUser currentUser)
    : IRequestHandler<AddCustomerAddressCommand, Guid>
{
    public async Task<Guid> Handle(
        AddCustomerAddressCommand request,
        CancellationToken cancellationToken
    )
    {
        var userId = currentUser.Id ?? throw new UnauthorizedAccessException();
        var profile = await db.CustomerProfiles.SingleOrDefaultAsync(
            candidate =>
                candidate.OrganizationId == request.OrganizationId && candidate.UserId == userId,
            cancellationToken
        );
        if (profile is null)
            throw new KeyNotFoundException(
                "A customer profile was not found for the current user in this organization."
            );

        var hasActiveAddress = await db.CustomerAddresses.AnyAsync(
            address =>
                address.OrganizationId == request.OrganizationId
                && address.CustomerProfileId == profile.Id
                && address.IsActive,
            cancellationToken
        );
        var address = CustomerAddress.Create(
            request.OrganizationId,
            profile.Id,
            request.Label,
            request.RecipientName,
            request.PhoneNumber,
            request.AddressLine1,
            request.AddressLine2,
            request.Barangay,
            request.CityOrMunicipality,
            request.Province,
            request.PostalCode,
            request.CountryCode
        );
        db.CustomerAddresses.Add(address);

        if (request.MakeDefault || !hasActiveAddress)
            profile.SetDefaultAddress(address.Id);

        await db.SaveChangesAsync(cancellationToken);
        return address.Id;
    }
}
