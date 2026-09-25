extern alias AzureIdentity;

using Azure.Storage.Blobs;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Mvc;
using modular_mlm.Application.Common.Interfaces;
using modular_mlm.Application.Common.Models;
using modular_mlm.Infrastructure.Data;
using modular_mlm.Infrastructure.Storage;
using modular_mlm.Shared;
using modular_mlm.Web.Infrastructure.Security;
using modular_mlm.Web.Infrastructure.Tenancy;
using modular_mlm.Web.Services;

namespace Microsoft.Extensions.DependencyInjection;

public static class DependencyInjection
{
    public static void AddWebServices(this IHostApplicationBuilder builder)
    {
        builder.Services.AddDatabaseDeveloperPageExceptionFilter();

        builder.Services.AddScoped<IUser, CurrentUser>();

        builder.Services.AddScoped<ProtectedCookieService>();
        builder.Services.AddScoped<ICartSessionAccessor, HttpCartSessionAccessor>();
        builder.Services.AddScoped<
            ICurrentAuthenticationSession,
            HttpCurrentAuthenticationSession
        >();
        var dataProtectionSection = builder.Configuration.GetSection(
            DataProtectionStorageOptions.SectionName
        );
        builder
            .Services.AddOptions<DataProtectionStorageOptions>()
            .Bind(dataProtectionSection)
            .Validate(
                options => !string.IsNullOrWhiteSpace(options.ApplicationName),
                "Data Protection application name is required."
            )
            .Validate(
                options => !string.IsNullOrWhiteSpace(options.BlobName),
                "Data Protection key blob name is required."
            )
            .ValidateOnStart();

        var dataProtectionOptions = dataProtectionSection.Get<DataProtectionStorageOptions>()
            ?? new DataProtectionStorageOptions();
        var dataProtection = builder
            .Services.AddDataProtection()
            .SetApplicationName(dataProtectionOptions.ApplicationName);
        if (dataProtectionOptions.Enabled)
        {
            dataProtection.PersistKeysToAzureBlobStorage(provider =>
                provider
                    .GetRequiredKeyedService<BlobContainerClient>(
                        Services.DataProtectionContainer
                    )
                    .GetBlobClient(dataProtectionOptions.BlobName)
            );
        }

        builder.Services.AddHttpContextAccessor();
        builder.Services.AddScoped<IAuditContextAccessor, HttpAuditContextAccessor>();
        builder.Services.AddScoped<IdentitySecurityAuditEndpointFilter>();

        builder.Services.AddProblemDetails();
        builder.Services.AddSingleton<ApiProblemDetailsFactory>();
        builder.Services.AddExceptionHandler<ProblemDetailsExceptionHandler>();

        // Customise default API behaviour
        builder.Services.Configure<ApiBehaviorOptions>(options =>
            options.SuppressModelStateInvalidFilter = true
        );

        builder.Services.AddEndpointsApiExplorer();

        builder.Services.AddOpenApi(options =>
        {
            options.AddOperationTransformer<ApiExceptionOperationTransformer>();
            options.AddOperationTransformer<IdentityApiOperationTransformer>();
            options.AddDocumentTransformer<BearerSecuritySchemeTransformer>();
        });

        builder.Services.AddMarketplaceCors(builder.Configuration);
        builder.Services.AddMarketplaceRateLimiting(builder.Configuration);
        builder.Services.AddMarketplaceAntiforgery(builder.Environment);
        builder.Services.AddRequestTimeouts();
        builder.Services.AddOrganizationResolution(builder.Configuration);
        builder.Services.Configure<ForwardedHeadersOptions>(options =>
        {
            options.ForwardedHeaders =
                ForwardedHeaders.XForwardedFor
                | ForwardedHeaders.XForwardedProto
                | ForwardedHeaders.XForwardedHost;
        });

        builder.Services.AddMediatR(config =>
        {
            config.RegisterServicesFromAssembly(typeof(IMediatrMarker).Assembly);

            config.LicenseKey = builder.Configuration["MediatR:LicenseKey"];
        });
    }

    public static void AddKeyVaultIfConfigured(this IHostApplicationBuilder builder)
    {
        var keyVaultUri =
            builder.Configuration["AZURE_KEY_VAULT_ENDPOINT"]
            ?? builder.Configuration.GetConnectionString(Services.KeyVault);
        if (!string.IsNullOrWhiteSpace(keyVaultUri))
        {
            builder.Configuration.AddAzureKeyVault(
                new Uri(keyVaultUri),
                new AzureIdentity::Azure.Identity.DefaultAzureCredential()
            );
        }
    }
}
