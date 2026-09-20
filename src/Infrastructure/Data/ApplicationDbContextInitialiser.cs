using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System.Net.Mail;
using modular_mlm.Domain.Catalog;
using modular_mlm.Domain.Constants;
using modular_mlm.Domain.Organizations;
using modular_mlm.Infrastructure.Identity;

namespace modular_mlm.Infrastructure.Data;

public static class InitialiserExtensions
{
    public static async Task InitialiseDatabaseAsync(this WebApplication app)
    {
        await using var scope = app.Services.CreateAsyncScope();
        await scope
            .ServiceProvider.GetRequiredService<ApplicationDbContextInitialiser>()
            .InitialiseAsync();
    }
}

public sealed class ApplicationDbContextInitialiser(
    ApplicationDbContext db,
    UserManager<ApplicationUser> users,
    RoleManager<IdentityRole> roles,
    IConfiguration configuration,
    IHostEnvironment environment,
    TimeProvider timeProvider
)
{
    public async Task InitialiseAsync()
    {
        await db.Database.MigrateAsync();
        var developmentSupportEmail = ResolveDevelopmentSupportEmail();

        foreach (
            var roleName in new[]
            {
                Roles.PlatformAdministrator,
                Roles.Administrator,
                Roles.Agent,
                Roles.Customer,
            }
        )
            if (!await roles.RoleExistsAsync(roleName))
                await roles.CreateAsync(new IdentityRole(roleName));

        const string email = "administrator@localhost";
        var administratorPassword = configuration["Seed:AdministratorPassword"];
        var admin = await users.FindByEmailAsync(email);
        if (admin is null && !string.IsNullOrWhiteSpace(administratorPassword))
        {
            admin = new ApplicationUser
            {
                UserName = email,
                Email = email,
                EmailConfirmed = true,
                DisplayName = "Platform Administrator",
            };
            var result = await users.CreateAsync(admin, administratorPassword);
            if (!result.Succeeded)
                throw new InvalidOperationException(
                    string.Join("; ", result.Errors.Select(x => x.Description))
                );
            await users.AddToRoleAsync(admin, Roles.Administrator);
        }

        // Keep the local seed credential deterministic. Previously the configured
        // password was used only when the account was first created, so an existing
        // development database silently kept an obsolete password and every login
        // attempt with the current Seed:AdministratorPassword returned 401.
        if (
            environment.IsDevelopment()
            && admin is not null
            && !string.IsNullOrWhiteSpace(administratorPassword)
            && !await users.CheckPasswordAsync(admin, administratorPassword)
        )
        {
            var resetToken = await users.GeneratePasswordResetTokenAsync(admin);
            var resetResult = await users.ResetPasswordAsync(
                admin,
                resetToken,
                administratorPassword
            );
            if (!resetResult.Succeeded)
                throw new InvalidOperationException(
                    string.Join("; ", resetResult.Errors.Select(error => error.Description))
                );
        }

        if (admin is not null && !await users.IsInRoleAsync(admin, Roles.PlatformAdministrator))
            await users.AddToRoleAsync(admin, Roles.PlatformAdministrator);

        if (!await db.Organizations.AnyAsync())
        {
            var organization = Organization.Create("Modular Marketplace", "default", "USD");
            organization.UpdateBranding(
                BrandingSettings.Create("Modular Marketplace", developmentSupportEmail)
            );
            organization.PublishBranding(timeProvider.GetUtcNow());
            db.Organizations.Add(organization);
            db.Categories.Add(Category.Create(organization.Id, "General", "general"));
            await db.SaveChangesAsync();
        }

        // Branding publication became mandatory for public tenant resolution after the
        // original development seed was introduced. Repair only local development data;
        // production organizations must still be published through the audited command.
        if (environment.IsDevelopment())
        {
            var unpublishedOrganizations = await db
                .Organizations.Where(organization =>
                    organization.Status == OrganizationStatus.Active
                    && organization.PublishedBranding == null
                )
                .ToListAsync();

            foreach (var organization in unpublishedOrganizations)
            {
                if (!IsValidEmail(organization.Branding.SupportEmail))
                {
                    var branding = organization.Branding;
                    organization.UpdateBranding(
                        BrandingSettings.Create(
                            branding.StoreTitle,
                            developmentSupportEmail,
                            branding.PrimaryColor,
                            branding.SecondaryColor,
                            branding.AccentColor,
                            branding.LogoUrl,
                            branding.FaviconUrl,
                            branding.SupportPhone,
                            branding.FooterText
                        )
                    );
                }

                organization.PublishBranding(timeProvider.GetUtcNow());
            }

            if (unpublishedOrganizations.Count > 0)
                await db.SaveChangesAsync();
        }

        if (admin is not null && admin.OrganizationId is null)
        {
            var defaultOrganizationId = await db
                .Organizations.Where(organization => organization.Slug == "default")
                .Select(organization => organization.Id)
                .SingleOrDefaultAsync();

            if (defaultOrganizationId != Guid.Empty)
            {
                admin.OrganizationId = defaultOrganizationId;
                var result = await users.UpdateAsync(admin);
                if (!result.Succeeded)
                    throw new InvalidOperationException(
                        string.Join("; ", result.Errors.Select(error => error.Description))
                    );
            }
        }
    }

    private string ResolveDevelopmentSupportEmail()
    {
        var configuredEmail =
            configuration["Seed:SupportEmail"] ?? configuration["Email:Smtp:FromAddress"];

        return IsValidEmail(configuredEmail) ? configuredEmail!.Trim() : "support@localhost.test";
    }

    private static bool IsValidEmail(string? value) =>
        !string.IsNullOrWhiteSpace(value) && MailAddress.TryCreate(value.Trim(), out _);
}
