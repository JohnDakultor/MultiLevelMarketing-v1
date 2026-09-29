namespace modular_mlm.Infrastructure.Identity;

internal static class ExternalApplicationUri
{
    public static bool IsHttp(Uri uri) =>
        uri.IsAbsoluteUri
        && (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps)
        && string.IsNullOrEmpty(uri.UserInfo);

    public static bool IsPublicHttps(Uri uri) =>
        IsHttp(uri)
        && uri.Scheme == Uri.UriSchemeHttps
        && !uri.IsLoopback
        && !uri.Host.Equals("localhost", StringComparison.OrdinalIgnoreCase);
}
