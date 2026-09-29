namespace modular_mlm.Infrastructure.Identity;

public sealed class IdentitySecurityOptions
{
    public const string SectionName = "IdentitySecurity";
    public const string AdministratorMfaPolicy = "AdministratorMfa";

    public bool RequireConfirmedEmail { get; init; } = true;
    public bool RequireAdministratorMfa { get; init; } = true;
    public int RefreshTokenLifetimeDays { get; init; } = 30;
    public int MaximumActiveSessions { get; init; } = 10;
    public bool RevokeTokenFamilyOnReuse { get; init; } = true;
    public Uri PasswordResetBaseUrl { get; init; } = new("http://localhost:3000/reset-password");

    public static bool IsValid(IdentitySecurityOptions options) =>
        options.RefreshTokenLifetimeDays is >= 1 and <= 90
        && options.MaximumActiveSessions is >= 1 and <= 100
        && ExternalApplicationUri.IsHttp(options.PasswordResetBaseUrl);

    public static bool IsValidForDeployment(IdentitySecurityOptions options) =>
        IsValid(options) && ExternalApplicationUri.IsPublicHttps(options.PasswordResetBaseUrl);
}
