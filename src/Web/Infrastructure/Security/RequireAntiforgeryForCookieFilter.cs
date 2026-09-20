using Microsoft.AspNetCore.Antiforgery;
using modular_mlm.Web.Infrastructure;

namespace modular_mlm.Web.Infrastructure.Security;

public sealed class RequireAntiforgeryForCookieFilter(
    IAntiforgery antiforgery,
    ApiProblemDetailsFactory problemFactory
) : IEndpointFilter
{
    public async ValueTask<object?> InvokeAsync(
        EndpointFilterInvocationContext context,
        EndpointFilterDelegate next
    )
    {
        if (!AuthenticationTransportPolicy.RequiresAntiforgery(context.HttpContext))
            return await next(context);
        try
        {
            await antiforgery.ValidateRequestAsync(context.HttpContext);
            return await next(context);
        }
        catch (AntiforgeryValidationException)
        {
            var problem = problemFactory.Create(
                context.HttpContext,
                StatusCodes.Status403Forbidden,
                ApiErrorCodes.AntiforgeryValidationFailed,
                "Invalid antiforgery token",
                "A valid antiforgery token is required for this cookie-authenticated request."
            );
            return Results.Json(
                problem,
                statusCode: problem.Status,
                contentType: "application/problem+json"
            );
        }
    }
}
