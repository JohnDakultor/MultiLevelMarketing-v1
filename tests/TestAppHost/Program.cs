using modular_mlm.Shared;

namespace modular_mlm.TestAppHost;

public class Program
{
    public static void Main(string[] args)
    {
        var builder = DistributedApplication.CreateBuilder(args);

        builder
            .AddPostgres(Services.DatabaseServer)
            .AddDatabase(Services.DatabaseResource, Services.Database);

        builder.Build().Run();
    }
}
