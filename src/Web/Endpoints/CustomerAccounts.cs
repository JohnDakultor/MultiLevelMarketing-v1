using Microsoft.AspNetCore.Http.HttpResults;
using modular_mlm.Application.Customers.Commands.AddCustomerAddress;
using modular_mlm.Application.Customers.Commands.RemoveCustomerAddress;
using modular_mlm.Application.Customers.Commands.UpdateCurrentCustomerProfile;
using modular_mlm.Application.Customers.Commands.UpdateCustomerAddress;
using modular_mlm.Application.Customers.Queries.GetCurrentCustomerProfile;
using modular_mlm.Application.Customers.Queries.GetCurrentCustomerProfile.Models;
using modular_mlm.Application.Customers.Queries.GetCustomerAddresses;
using modular_mlm.Application.Customers.Queries.GetCustomerAddresses.Models;

namespace modular_mlm.Web.Endpoints;

public sealed class CustomerAccounts : IEndpointGroup
{
    public static string RoutePrefix => "/api/organizations/{organizationId:guid}/me";

    public static void Map(RouteGroupBuilder group)
    {
        group.RequireAuthorization();
        group.MapGet(GetCurrentCustomerProfile, "profile");
        group.MapPut(UpdateCurrentCustomerProfile, "profile");
        group.MapGet(GetCustomerAddresses, "addresses");
        group.MapPost(AddCustomerAddress, "addresses");
        group.MapPut(UpdateCustomerAddress, "addresses/{addressId:guid}");
        group.MapDelete(RemoveCustomerAddress, "addresses/{addressId:guid}");
    }

    public static async Task<Ok<CustomerProfileDto>> GetCurrentCustomerProfile(
        ISender sender,
        Guid organizationId,
        CancellationToken cancellationToken
    ) =>
        TypedResults.Ok(
            await sender.Send(new GetCurrentCustomerProfileQuery(organizationId), cancellationToken)
        );

    public static async Task<Ok<CustomerProfileDto>> UpdateCurrentCustomerProfile(
        ISender sender,
        Guid organizationId,
        UpdateCustomerProfileRequest request,
        CancellationToken cancellationToken
    ) =>
        TypedResults.Ok(
            await sender.Send(
                new UpdateCurrentCustomerProfileCommand(organizationId, request.DisplayName),
                cancellationToken
            )
        );

    public static async Task<Ok<IReadOnlyList<CustomerAddressDto>>> GetCustomerAddresses(
        ISender sender,
        Guid organizationId,
        CancellationToken cancellationToken
    ) =>
        TypedResults.Ok(
            await sender.Send(new GetCustomerAddressesQuery(organizationId), cancellationToken)
        );

    public static async Task<Created<Guid>> AddCustomerAddress(
        ISender sender,
        Guid organizationId,
        CustomerAddressRequest request,
        CancellationToken cancellationToken
    )
    {
        var addressId = await sender.Send(
            new AddCustomerAddressCommand(
                organizationId,
                request.Label,
                request.RecipientName,
                request.PhoneNumber,
                request.AddressLine1,
                request.AddressLine2,
                request.Barangay,
                request.CityOrMunicipality,
                request.Province,
                request.PostalCode,
                request.CountryCode,
                request.MakeDefault
            ),
            cancellationToken
        );

        return TypedResults.Created(
            $"/api/organizations/{organizationId}/me/addresses/{addressId}",
            addressId
        );
    }

    public static async Task<NoContent> UpdateCustomerAddress(
        ISender sender,
        Guid organizationId,
        Guid addressId,
        CustomerAddressRequest request,
        CancellationToken cancellationToken
    )
    {
        await sender.Send(
            new UpdateCustomerAddressCommand(
                organizationId,
                addressId,
                request.Label,
                request.RecipientName,
                request.PhoneNumber,
                request.AddressLine1,
                request.AddressLine2,
                request.Barangay,
                request.CityOrMunicipality,
                request.Province,
                request.PostalCode,
                request.CountryCode,
                request.MakeDefault
            ),
            cancellationToken
        );
        return TypedResults.NoContent();
    }

    public static async Task<NoContent> RemoveCustomerAddress(
        ISender sender,
        Guid organizationId,
        Guid addressId,
        CancellationToken cancellationToken
    )
    {
        await sender.Send(
            new RemoveCustomerAddressCommand(organizationId, addressId),
            cancellationToken
        );
        return TypedResults.NoContent();
    }
}

public sealed record UpdateCustomerProfileRequest(string DisplayName);

public sealed record CustomerAddressRequest(
    string Label,
    string RecipientName,
    string PhoneNumber,
    string AddressLine1,
    string? AddressLine2,
    string? Barangay,
    string CityOrMunicipality,
    string Province,
    string PostalCode,
    string CountryCode,
    bool MakeDefault
);
