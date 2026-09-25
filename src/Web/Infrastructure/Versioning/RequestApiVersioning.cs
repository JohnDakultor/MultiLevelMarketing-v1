using System.Text.RegularExpressions;

namespace modular_mlm.Web.Infrastructure.Versioning;

public static partial class RequestApiVersioning
{
    public const string DeprecatedAliasSunset = "Wed, 31 Dec 2026 23:59:59 GMT";

    public static async Task<bool> ValidateAsync(HttpContext context)
    {
        if (!context.Request.Path.StartsWithSegments("/api"))
            return true;

        var supplied = context.Request.Headers[ApiRouteVersioningPolicy.VersionHeader].ToString().Trim();
        if (supplied.Length == 0)
        {
            // Compatibility alias for existing storefront/admin clients. New clients must send Api-Version.
            context.Response.Headers.TryAdd("Deprecation", "true");
            context.Response.Headers.TryAdd("Sunset", DeprecatedAliasSunset);
            context.Response.Headers.TryAdd(
                "Link",
                "</docs/api-versioning>; rel=\"deprecation\"; type=\"text/html\""
            );
            return true;
        }

        if (!VersionPattern().IsMatch(supplied))
        {
            await WriteProblemAsync(
                context,
                "Malformed API version",
                "api_version_malformed",
                $"'{supplied}' is not a valid API version. Use '1.0'."
            );
            return false;
        }

        if (supplied is not ("1" or ApiRouteVersioningPolicy.CurrentVersion))
        {
            await WriteProblemAsync(
                context,
                "Unsupported API version",
                "api_version_unsupported",
                $"API version '{supplied}' is not supported."
            );
            return false;
        }

        return true;
    }

    private static async Task WriteProblemAsync(
        HttpContext context,
        string title,
        string errorCode,
        string detail
    )
    {
        context.Response.StatusCode = StatusCodes.Status400BadRequest;
        await Results.Problem(
            statusCode: StatusCodes.Status400BadRequest,
            title: title,
            detail: detail,
            type: $"https://httpstatuses.com/400#{errorCode}",
            extensions: new Dictionary<string, object?>
            {
                ["code"] = errorCode,
                ["supportedVersions"] = new[] { ApiRouteVersioningPolicy.CurrentVersion },
            }
        ).ExecuteAsync(context);
    }

    [GeneratedRegex("^[0-9]+(?:\\.[0-9]+)?$", RegexOptions.CultureInvariant)]
    private static partial Regex VersionPattern();
}
