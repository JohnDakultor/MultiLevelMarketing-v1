namespace modular_mlm.Application.Customers.Commands.RemoveCustomerAddress;

public sealed class RemoveCustomerAddressCommandHandler(IApplicationDbContext db, IUser currentUser)
    : IRequestHandler<RemoveCustomerAddressCommand>
{
    public async Task Handle(
        RemoveCustomerAddressCommand request,
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

        address.Archive();
        if (profile.DefaultAddressId == address.Id)
        {
            var replacementAddressId = await db
                .CustomerAddresses.AsNoTracking()
                .Where(candidate =>
                    candidate.OrganizationId == request.OrganizationId
                    && candidate.CustomerProfileId == profile.Id
                    && candidate.Id != address.Id
                    && candidate.IsActive
                )
                .OrderBy(candidate => candidate.Created)
                .ThenBy(candidate => candidate.Id)
                .Select(candidate => (Guid?)candidate.Id)
                .FirstOrDefaultAsync(cancellationToken);

            if (replacementAddressId.HasValue)
                profile.SetDefaultAddress(replacementAddressId.Value);
            else
                profile.ClearDefaultAddress();
        }

        await db.SaveChangesAsync(cancellationToken);
    }
}
