using modular_mlm.Application.Customers.Queries.GetCurrentCustomerProfile.Models;

namespace modular_mlm.Application.Customers.Commands.UpdateCurrentCustomerProfile;

public sealed class UpdateCurrentCustomerProfileCommandHandler(
    IApplicationDbContext db,
    IUser currentUser
) : IRequestHandler<UpdateCurrentCustomerProfileCommand, CustomerProfileDto>
{
    public async Task<CustomerProfileDto> Handle(
        UpdateCurrentCustomerProfileCommand request,
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

        profile.Rename(request.DisplayName);
        await db.SaveChangesAsync(cancellationToken);

        return new CustomerProfileDto(
            profile.Id,
            profile.OrganizationId,
            profile.DisplayName,
            profile.DefaultAddressId
        );
    }
}
