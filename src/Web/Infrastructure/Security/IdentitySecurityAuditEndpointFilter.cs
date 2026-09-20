using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Identity;
using modular_mlm.Application.Common.Auditing;
using modular_mlm.Application.Common.Interfaces;
using modular_mlm.Application.Common.Models;
using modular_mlm.Infrastructure.Identity;

namespace modular_mlm.Web.Infrastructure.Security;

public sealed class IdentitySecurityAuditEndpointFilter(
    UserManager<ApplicationUser> userManager,
    IIdentitySecurityAuditWriter securityAuditWriter,
    TimeProvider clock
) : IEndpointFilter
{
    public async ValueTask<object?> InvokeAsync(
        EndpointFilterInvocationContext context,
        EndpointFilterDelegate next
    )
    {
        var path = context.HttpContext.Request.Path.Value ?? string.Empty;
        var loginEmail = path.EndsWith("/login", StringComparison.OrdinalIgnoreCase)
            ? ReadStringProperty(context.Arguments, "Email")
            : null;
        var result = await next(context);

        var user =
            !string.IsNullOrWhiteSpace(loginEmail) ? await userManager.FindByEmailAsync(loginEmail)
            : context.HttpContext.User.Identity?.IsAuthenticated == true
                ? await userManager.GetUserAsync(context.HttpContext.User)
            : null;
        if (
            user?.OrganizationId is not { } organizationId
            || !Guid.TryParse(user.Id, out var userId)
            || !await userManager.IsInRoleAsync(user, Domain.Constants.Roles.Administrator)
        )
            return result;

        var statusCode = ResolveStatusCode(result);
        var succeeded = statusCode is >= 200 and < 300;
        var action = ResolveAction(path, context.HttpContext.Request.Method, succeeded);
        if (
            !succeeded
            && path.EndsWith("/login", StringComparison.OrdinalIgnoreCase)
            && await userManager.IsLockedOutAsync(user)
        )
            action = AuditActionNames.AdministratorLockedOut;
        if (action is not null)
            await securityAuditWriter.WriteAsync(
                new IdentitySecurityAuditEvent(
                    organizationId,
                    userId,
                    action,
                    succeeded,
                    succeeded ? null : "identity_request_rejected",
                    clock.GetUtcNow()
                ),
                context.HttpContext.RequestAborted
            );
        return result;
    }

    private static string? ResolveAction(string path, string method, bool succeeded)
    {
        if (path.EndsWith("/login", StringComparison.OrdinalIgnoreCase))
            return succeeded
                ? AuditActionNames.AdministratorLoginSucceeded
                : AuditActionNames.AdministratorLoginFailed;
        return null;
    }

    private static int ResolveStatusCode(object? result)
    {
        // Typed Results<T1,...> wrap the concrete result. Unwrap them before reading status.
        var current = result;
        for (var depth = 0; current is not null && depth < 4; depth++)
        {
            if (current is IStatusCodeHttpResult statusResult)
                return statusResult.StatusCode ?? StatusCodes.Status200OK;
            var nested = current.GetType().GetProperty("Result")?.GetValue(current);
            if (nested is null || ReferenceEquals(nested, current))
                break;
            current = nested;
        }
        return StatusCodes.Status200OK;
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
