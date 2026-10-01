using Microsoft.AspNetCore.Identity;
using modular_mlm.Infrastructure.Identity;

namespace modular_mlm.Web.Infrastructure.Security;

/// <summary>
/// Compensates for the framework-generated Identity registration sequence.
/// MapIdentityApi saves the user before invoking IEmailSender, so a delivery
/// exception would otherwise return HTTP 500 while leaving a new account behind.
/// </summary>
public sealed class CompensatingIdentityRegistrationEndpointFilter(
    UserManager<ApplicationUser> userManager,
    ILogger<CompensatingIdentityRegistrationEndpointFilter> logger
) : IEndpointFilter
{
    public async ValueTask<object?> InvokeAsync(
        EndpointFilterInvocationContext context,
        EndpointFilterDelegate next
    )
    {
        var request = context.HttpContext.Request;
        if (
            !HttpMethods.IsPost(request.Method)
            || !(request.Path.Value ?? string.Empty).EndsWith(
                "/register",
                StringComparison.OrdinalIgnoreCase
            )
        )
            return await next(context);

        var email = ReadStringProperty(context.Arguments, "Email")?.Trim();
        var existedBefore = !string.IsNullOrWhiteSpace(email)
            && await userManager.FindByEmailAsync(email) is not null;

        try
        {
            return await next(context);
        }
        catch
        {
            if (!existedBefore && !string.IsNullOrWhiteSpace(email))
                await DeletePartiallyRegisteredUserAsync(email);
            throw;
        }
    }

    private async Task DeletePartiallyRegisteredUserAsync(string email)
    {
        var createdUser = await userManager.FindByEmailAsync(email);
        if (createdUser is null)
            return;

        var deletion = await userManager.DeleteAsync(createdUser);
        if (!deletion.Succeeded)
        {
            logger.LogCritical(
                "Failed to compensate incomplete Identity registration for user {UserId}: {ErrorCodes}",
                createdUser.Id,
                string.Join(',', deletion.Errors.Select(error => error.Code))
            );
        }
    }

    private static string? ReadStringProperty(
        IEnumerable<object?> arguments,
        string propertyName
    ) =>
        arguments
            .Select(argument => argument?.GetType().GetProperty(propertyName)?.GetValue(argument))
            .OfType<string>()
            .FirstOrDefault();
}
