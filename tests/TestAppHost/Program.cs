using modular_mlm.Shared;

namespace modular_mlm.TestAppHost;

public class Program
{
    public static void Main(string[] args)
    {
        var builder = DistributedApplication.CreateBuilder(args);

        // Create the physical database as part of PostgreSQL container startup.
        // Without POSTGRES_DB, the database health check can run before Aspire's
        // database lifecycle callback and leave both the server and child resource
        // unhealthy because the target database does not exist yet.
        builder
            .AddPostgres(Services.DatabaseServer)
            .WithEnvironment("POSTGRES_DB", Services.Database)
            .AddDatabase(Services.DatabaseResource, Services.Database);

        builder.Build().Run();
    }
}
