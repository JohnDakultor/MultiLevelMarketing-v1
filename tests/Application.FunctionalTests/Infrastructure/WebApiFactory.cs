using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using modular_mlm.Application.Common.Interfaces;
using modular_mlm.Domain.Constants;
using modular_mlm.Infrastructure.BackgroundJobs;
using modular_mlm.Infrastructure.Identity;

namespace modular_mlm.Application.FunctionalTests.Infrastructure;

public class WebApiFactory(string connectionString) : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseContentRoot(ResolveWebContentRoot());
        builder.UseSetting("Cors:AllowedOrigins:0", "https://frontend.test");
        // CI runs the release suite with the Staging environment so the deployed-runtime
        // URL validators are active. Supply inert, non-loopback HTTPS URLs for the test
        // host instead of allowing it to inherit the localhost development defaults.
        builder.UseSetting(
            "IdentitySecurity:PasswordResetBaseUrl",
            "https://storefront.example.test/reset-password"
        );
        builder.UseSetting(
            "AdministratorInvitations:AcceptanceBaseUrl",
            "https://admin.example.test/invitations/accept"
        );
        builder.UseSetting(
            "PayMongo:PayoutCallbackUrl",
            "https://api.example.test/api/webhooks/paymongo/transfers"
        );
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
            foreach (
                var registration in services
                    .Where(service =>
                        service.ServiceType == typeof(IHostedService)
                        && service.ImplementationType == typeof(WalletSettingsStartupValidator)
                    )
                    .ToArray()
            )
                services.Remove(registration);

            services.RemoveAll<IAdministratorInvitationDelivery>();
            services.RemoveAll<IInvitationDeliveryOutbox>();
            services.RemoveAll<IPaymentGateway>();
            services.RemoveAll<IPayoutProvider>();
            services.RemoveAll<IPayoutAccountProtector>();
            services.RemoveAll<IObjectStorage>();
            services.RemoveAll<IDnsTxtRecordResolver>();
            services.RemoveAll<IEmailSender<ApplicationUser>>();
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
            services.AddSingleton<TestDnsTxtRecordResolver>();
            services.AddSingleton<IDnsTxtRecordResolver>(provider =>
                provider.GetRequiredService<TestDnsTxtRecordResolver>()
            );
            services.AddSingleton<TestIdentityEmailSender>();
            services.AddSingleton<IEmailSender<ApplicationUser>>(provider =>
                provider.GetRequiredService<TestIdentityEmailSender>()
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
