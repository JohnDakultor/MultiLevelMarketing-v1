using Aspire.Hosting.ApplicationModel;
using modular_mlm.Shared;

var builder = DistributedApplication.CreateBuilder(args);
var organizationSlug = builder.Configuration["ORGANIZATION_SLUG"]?.Trim().ToLowerInvariant();
if (string.IsNullOrWhiteSpace(organizationSlug))
    organizationSlug = "default";

var administratorEmail = builder.Configuration["SEED_ADMINISTRATOR_EMAIL"]?.Trim();
if (string.IsNullOrWhiteSpace(administratorEmail))
    administratorEmail = "administrator@localhost";

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
}

var storefront = AddFrontend(Services.Storefront, "apps/storefront/Dockerfile");
AddFrontend(Services.AgentPortal, "apps/agent-portal/Dockerfile")
    .WithEnvironment("STOREFRONT_URL", storefront.GetEndpoint("http"))
    .WaitFor(storefront);
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
