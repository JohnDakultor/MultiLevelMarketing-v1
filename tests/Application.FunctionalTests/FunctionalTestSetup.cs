using Microsoft.Extensions.DependencyInjection;
using modular_mlm.Infrastructure.Data;

namespace modular_mlm.Application.FunctionalTests;

[SetUpFixture]
public class FunctionalTestSetup
{
    internal static IServiceScopeFactory ScopeFactory { get; private set; } = null!;
    internal static DatabaseResetter? DbResetter { get; private set; }

    private static WebApiFactory? _factory;
    private static DistributedApplication? _app;

    [OneTimeSetUp]
    public async Task OneTimeSetUp()
    {
        var directConnectionString = Environment.GetEnvironmentVariable(
            "MODULAR_MLM_TEST_CONNECTION_STRING"
        );
        if (!string.IsNullOrWhiteSpace(directConnectionString))
        {
            await ConfigureFactoryAsync(directConnectionString);
            return;
        }

        var cts = new CancellationTokenSource(TimeSpan.FromSeconds(60));
        var cancellationToken = cts.Token;

        var builder = await DistributedApplicationTestingBuilder.CreateAsync<Projects.TestAppHost>(
            args: [],
            configureBuilder: (options, _) =>
            {
                options.DisableDashboard = true;
            }
        );

        builder.Configuration["ASPIRE_ALLOW_UNSECURED_TRANSPORT"] = "true";

        _app = await builder.BuildAsync(cancellationToken).WaitAsync(cancellationToken);

        await _app.StartAsync(cancellationToken).WaitAsync(cancellationToken);

        await _app.ResourceNotifications.WaitForResourceHealthyAsync(
            Services.DatabaseResource,
            cancellationToken
        );

        var connectionString = (await _app.GetConnectionStringAsync(Services.DatabaseResource))!;

        await ConfigureFactoryAsync(connectionString);
    }

    private static async Task ConfigureFactoryAsync(string connectionString)
    {
        _factory = new WebApiFactory(connectionString);
        ScopeFactory = _factory.Services.GetRequiredService<IServiceScopeFactory>();

        await using (var scope = ScopeFactory.CreateAsyncScope())
        {
            await scope
                .ServiceProvider.GetRequiredService<ApplicationDbContextInitialiser>()
                .InitialiseAsync();
        }

        DbResetter = await DatabaseResetter.CreateAsync(connectionString);
    }

    internal static HttpClient CreateClient() =>
        (
            _factory ?? throw new InvalidOperationException("The test server is not initialized.")
        ).CreateClient(
            new Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactoryClientOptions
            {
                AllowAutoRedirect = false,
            }
        );

    [OneTimeTearDown]
    public async Task OneTimeTearDown()
    {
        if (DbResetter is not null)
            await DbResetter.DisposeAsync();
        if (_app is not null)
            await _app.DisposeAsync();
        if (_factory is not null)
            await _factory.DisposeAsync();
    }
}
