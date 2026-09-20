using Microsoft.AspNetCore.Http.HttpResults;
using modular_mlm.Application.Commerce.Commands.AddCartItem;
using modular_mlm.Application.Commerce.Commands.ApplyReferralCode;
using modular_mlm.Application.Commerce.Commands.RemoveCartItem;
using modular_mlm.Application.Commerce.Commands.UpdateCartItem;
using modular_mlm.Application.Commerce.Queries.GetCart;

namespace modular_mlm.Web.Endpoints;

public sealed class Carts : IEndpointGroup
{
    public static string RoutePrefix => "/api/organizations/{organizationId:guid}/cart";

    public static void Map(RouteGroupBuilder group)
    {
        group.MapGet(GetCart).AllowAnonymous();
        group.MapPost(AddCartItem, "items").AllowAnonymous();
        group.MapPut(UpdateCartItem, "items/{cartItemId:guid}").AllowAnonymous();
        group.MapDelete(RemoveCartItem, "items/{cartItemId:guid}").AllowAnonymous();
        group.MapPut(ApplyReferralCode, "referral").AllowAnonymous();
    }

    public static async Task<Ok<CartDto>> GetCart(ISender sender, Guid organizationId) =>
        TypedResults.Ok(await sender.Send(new GetCartQuery(organizationId)));

    public static async Task<Created<Guid>> AddCartItem(
        ISender sender,
        Guid organizationId,
        AddCartItemRequest request
    )
    {
        var cartId = await sender.Send(
            new AddCartItemCommand(organizationId, request.ProductVariantId, request.Quantity)
        );
        return TypedResults.Created($"/api/organizations/{organizationId}/cart", cartId);
    }

    public static async Task<Ok<CartDto>> UpdateCartItem(
        ISender sender,
        Guid organizationId,
        Guid cartItemId,
        UpdateCartItemRequest request
    ) =>
        TypedResults.Ok(
            await sender.Send(
                new UpdateCartItemCommand(organizationId, cartItemId, request.Quantity)
            )
        );

    public static async Task<Ok<CartDto>> RemoveCartItem(
        ISender sender,
        Guid organizationId,
        Guid cartItemId
    ) => TypedResults.Ok(await sender.Send(new RemoveCartItemCommand(organizationId, cartItemId)));

    public static async Task<Ok<CartDto>> ApplyReferralCode(
        ISender sender,
        Guid organizationId,
        ApplyReferralCodeRequest request
    ) =>
        TypedResults.Ok(
            await sender.Send(new ApplyReferralCodeCommand(organizationId, request.ReferralCode))
        );
}

public sealed record AddCartItemRequest(Guid ProductVariantId, int Quantity);

public sealed record UpdateCartItemRequest(int Quantity);

public sealed record ApplyReferralCodeRequest(string ReferralCode);
