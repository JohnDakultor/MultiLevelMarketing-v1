using Aspire.Hosting.ApplicationModel;
using Azure.Provisioning.Storage;
using modular_mlm.Shared;

var builder = DistributedApplication.CreateBuilder(args);
var organizationSlug = builder.Configuration["ORGANIZATION_SLUG"]?.Trim().ToLowerInvariant();
if (string.IsNullOrWhiteSpace(organizationSlug))
    organizationSlug = "default";

var administratorEmail = builder.Configuration["SEED_ADMINISTRATOR_EMAIL"]?.Trim();
if (string.IsNullOrWhiteSpace(administratorEmail))
    administratorEmail = "administrator@localhost";

var publicApiBaseUrl = builder.Configuration["PUBLIC_API_BASE_URL"]?.Trim().TrimEnd('/');
var publicAdminBaseUrl = builder.Configuration["PUBLIC_ADMIN_BASE_URL"]?.Trim().TrimEnd('/');
var publicStorefrontBaseUrl = builder.Configuration["PUBLIC_STOREFRONT_BASE_URL"]?.Trim().TrimEnd('/');

builder.AddAzureContainerAppEnvironment("aca-env");

var databaseServer = builder
    .AddAzurePostgresFlexibleServer(Services.DatabaseServer)
    .WithPasswordAuthentication()
    .RunAsContainer(container => container.WithLifetime(ContainerLifetime.Persistent))
    .AddDatabase(Services.DatabaseResource, Services.Database);

var storage = builder
    .AddAzureStorage(Services.ObjectStorageAccount)
    .RunAsEmulator(emulator => emulator.WithLifetime(ContainerLifetime.Persistent));

var objectStorage = storage.AddBlobContainer(
        Services.ObjectStorageContainer,
        blobContainerName: Services.ObjectStorageContainer
    );
var dataProtectionStorage = storage.AddBlobContainer(
    Services.DataProtectionContainer,
    blobContainerName: Services.DataProtectionContainer
);
storage.ConfigureInfrastructure(infrastructure =>
{
    var resources = infrastructure.GetProvisionableResources();
    var account = resources.OfType<StorageAccount>().Single();
    account.AllowBlobPublicAccess = true;

    var assetContainer = resources
        .OfType<BlobContainer>()
        .Single(container =>
            container.Name.Value?.EndsWith(
                Services.ObjectStorageContainer,
                StringComparison.Ordinal
            ) == true
        );
    assetContainer.PublicAccess = StoragePublicAccessType.Blob;
});

var databaseMigrator = builder
    .AddProject<Projects.DatabaseMigrator>(Services.DatabaseMigrator)
    .WithReference(databaseServer, Services.Database)
    .WaitFor(databaseServer)
    .WithReference(dataProtectionStorage)
    .WaitFor(dataProtectionStorage)
    .WithEnvironment("Seed__OrganizationSlug", organizationSlug)
    .WithEnvironment("Seed__AdministratorEmail", administratorEmail)
    .PublishAsAzureContainerAppJob();

if (
    !string.IsNullOrWhiteSpace(
        builder.Configuration["Parameters:seed-administrator-password"]
    )
)
{
    var administratorPassword = builder.AddParameter(
        "seed-administrator-password",
        secret: true
    );
    databaseMigrator.WithEnvironment("Seed__AdministratorPassword", administratorPassword);
}

var web = builder
    .AddProject<Projects.Web>(Services.WebApi)
    .WithReference(databaseServer, Services.Database)
    .WaitFor(databaseServer)
    .WithReference(objectStorage)
    .WaitFor(objectStorage)
    .WithReference(dataProtectionStorage)
    .WaitFor(dataProtectionStorage)
    .WaitForCompletion(databaseMigrator)
    .WithEnvironment("ObjectStorage__AzureBlob__Enabled", "true")
    .WithEnvironment("DataProtection__AzureBlob__Enabled", "true")
    .WithEnvironment("OrganizationResolution__FallbackOrganizationSlug", organizationSlug)
    .WithExternalHttpEndpoints()
    .WithAspNetCoreEnvironment()
    .WithUrlForEndpoint(
        "http",
        url =>
        {
            url.DisplayText = "Scalar API Reference";
            url.Url = "/scalar";
        }
    );

if (builder.ExecutionContext.IsPublishMode)
{
    var keyVault = builder.AddAzureKeyVault(Services.KeyVault);
    web.WithReference(keyVault).WaitFor(keyVault);

    if (
        string.IsNullOrWhiteSpace(publicApiBaseUrl)
        || string.IsNullOrWhiteSpace(publicAdminBaseUrl)
        || string.IsNullOrWhiteSpace(publicStorefrontBaseUrl)
    )
        throw new InvalidOperationException(
            "PUBLIC_API_BASE_URL, PUBLIC_ADMIN_BASE_URL, and PUBLIC_STOREFRONT_BASE_URL are required when publishing."
        );

    web.WithEnvironment(
            "PayMongo__PayoutCallbackUrl",
            $"{publicApiBaseUrl}/api/webhooks/paymongo/transfers"
        )
        .WithEnvironment(
            "AdministratorInvitations__AcceptanceBaseUrl",
            $"{publicAdminBaseUrl}/invitations/accept"
        )
        .WithEnvironment(
            "IdentitySecurity__PasswordResetBaseUrl",
            $"{publicStorefrontBaseUrl}/reset-password"
        );
}

var storefront = AddFrontend(Services.Storefront, "apps/storefront/Dockerfile");
AddFrontend(Services.AgentPortal, "apps/agent-portal/Dockerfile")
    .WithEnvironment("STOREFRONT_URL", storefront.GetEndpoint("http"));
AddFrontend(Services.AdminPortal, "apps/admin-portal/Dockerfile")
    .WithEnvironment("STOREFRONT_URL", storefront.GetEndpoint("http"))
    .WaitFor(storefront);

IResourceBuilder<ContainerResource> AddFrontend(string name, string dockerfilePath)
{
    return builder
        .AddDockerfile(name, "../../web", dockerfilePath)
        .WithHttpEndpoint(targetPort: 8080, name: "http")
        .WithEnvironment("PORT", "8080")
        .WithEnvironment("HOSTNAME", "0.0.0.0")
        .WithEnvironment("BACKEND_API_BASE_URL", web.GetEndpoint("http"))
        .WithEnvironment("ORGANIZATION_SLUG", organizationSlug)
        .WithReference(web)
        .WaitFor(web)
        .WithExternalHttpEndpoints();
}

builder.Build().Run();
