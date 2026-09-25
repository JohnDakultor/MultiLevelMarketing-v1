using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using modular_mlm.Application.Common.Interfaces;
using modular_mlm.Domain.Constants;

namespace modular_mlm.Application.FunctionalTests.Infrastructure;

public class WebApiFactory(string connectionString) : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseContentRoot(ResolveWebContentRoot());
        builder.UseSetting("Cors:AllowedOrigins:0", "https://frontend.test");
        builder.UseSetting("BackgroundJobs:CommissionRelease:Enabled", "false");
        builder.UseSetting("BackgroundJobs:CommissionRelease:OrganizationBatchSize", "1");
        builder.UseSetting("BackgroundJobs:CommissionRelease:CommissionBatchSize", "1");
        builder.UseSetting("BackgroundJobs:AdministratorInvitationExpiry:Enabled", "false");
        builder.UseSetting("BackgroundJobs:Outbox:Enabled", "false");
        builder.UseSetting("BackgroundJobs:Durable:Enabled", "false");
        builder.UseSetting("Notifications:Delivery:Enabled", "false");
        builder.UseSetting("RateLimiting:Authentication:PermitLimit", "10000");
        builder.UseSetting(
            $"ConnectionStrings:{modular_mlm.Shared.Services.Database}",
            connectionString
        );

        builder.ConfigureTestServices(services =>
        {
            services.RemoveAll<IAdministratorInvitationDelivery>();
            services.RemoveAll<IInvitationDeliveryOutbox>();
            services.RemoveAll<IPaymentGateway>();
            services.RemoveAll<IPayoutProvider>();
            services.RemoveAll<IPayoutAccountProtector>();
            services.RemoveAll<IObjectStorage>();
            services.AddSingleton<TestAdministratorInvitationDelivery>();
            services.AddSingleton<IAdministratorInvitationDelivery>(provider =>
                provider.GetRequiredService<TestAdministratorInvitationDelivery>()
            );
            services.AddSingleton<IInvitationDeliveryOutbox>(provider =>
                provider.GetRequiredService<TestAdministratorInvitationDelivery>()
            );
            services.AddSingleton<TestPaymentGateway>();
            services.AddSingleton<IPaymentGateway>(provider =>
                provider.GetRequiredService<TestPaymentGateway>()
            );
            services.AddSingleton<TestPayoutProvider>();
            services.AddSingleton<IPayoutProvider>(provider =>
                provider.GetRequiredService<TestPayoutProvider>()
            );
            services.AddSingleton<IPayoutAccountProtector, TestPayoutAccountProtector>();
            services.AddSingleton<TestObjectStorage>();
            services.AddSingleton<IObjectStorage>(provider =>
                provider.GetRequiredService<TestObjectStorage>()
            );
            services
                .RemoveAll<IUser>()
                .AddTransient(provider =>
                {
                    var mock = new Mock<IUser>();
                    mock.SetupGet(x => x.Roles).Returns(TestApp.GetRoles());
                    mock.SetupGet(x => x.Id).Returns(TestApp.GetUserId());
                    mock.SetupGet(x => x.UserId)
                        .Returns(() =>
                            Guid.TryParse(TestApp.GetUserId(), out var userId) ? userId : Guid.Empty
                        );
                    mock.SetupGet(x => x.OrganizationId).Returns(TestApp.GetOrganizationId());
                    mock.SetupGet(x => x.IsAdmin)
                        .Returns(() => TestApp.GetRoles()?.Contains(Roles.Administrator) ?? false);
                    mock.SetupGet(x => x.IsAgent)
                        .Returns(() => TestApp.GetRoles()?.Contains(Roles.Agent) ?? false);
                    return mock.Object;
                });
        });
    }

    private static string ResolveWebContentRoot()
    {
        foreach (
            var startingPath in new[] { Directory.GetCurrentDirectory(), AppContext.BaseDirectory }
        )
        {
            for (
                var directory = new DirectoryInfo(startingPath);
                directory is not null;
                directory = directory.Parent
            )
            {
                var candidate = Path.Combine(directory.FullName, "src", "Web");
                if (File.Exists(Path.Combine(candidate, "Web.csproj")))
                    return candidate;
            }
        }

        throw new DirectoryNotFoundException(
            "Could not locate the Web project content root from the working directory or test output."
        );
    }
}
