using modular_mlm.Shared;

var builder = DistributedApplication.CreateBuilder(args);

builder.AddAzureContainerAppEnvironment("aca-env");

var databaseServer = builder
    .AddAzurePostgresFlexibleServer(Services.DatabaseServer)
    .WithPasswordAuthentication()
    .RunAsContainer(container => container.WithLifetime(ContainerLifetime.Persistent))
    .AddDatabase(Services.DatabaseResource, Services.Database);

var web = builder
    .AddProject<Projects.Web>(Services.WebApi)
    .WithReference(databaseServer, Services.Database)
    .WaitFor(databaseServer)
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

builder.Build().Run();
