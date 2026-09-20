namespace modular_mlm.Application.Customers.Commands.UpdateCustomerAddress;

public sealed class UpdateCustomerAddressCommandHandler(IApplicationDbContext db, IUser currentUser)
    : IRequestHandler<UpdateCustomerAddressCommand>
{
    public async Task Handle(
        UpdateCustomerAddressCommand request,
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

        var address = await db.CustomerAddresses.SingleOrDefaultAsync(
            candidate =>
                candidate.Id == request.AddressId
                && candidate.OrganizationId == request.OrganizationId
                && candidate.CustomerProfileId == profile.Id
                && candidate.IsActive,
            cancellationToken
        );
        if (address is null)
            throw new KeyNotFoundException("Customer address was not found.");

        address.UpdateDeliveryDetails(
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
        if (request.MakeDefault)
            profile.SetDefaultAddress(address.Id);

        await db.SaveChangesAsync(cancellationToken);
    }
}
