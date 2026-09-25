using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using modular_mlm.Application.Common.Interfaces;
using modular_mlm.Application.Payouts.Commands.ProcessPayout;
using modular_mlm.Infrastructure.Auditing;
using modular_mlm.Infrastructure.BackgroundJobs;
using modular_mlm.Infrastructure.Caching;
using modular_mlm.Infrastructure.Data;
using modular_mlm.Infrastructure.Data.Interceptors;
using modular_mlm.Infrastructure.Data.Repositories;
using modular_mlm.Infrastructure.Email;
using modular_mlm.Infrastructure.Idempotency;
using modular_mlm.Infrastructure.Identity;
using modular_mlm.Infrastructure.Messaging;
using modular_mlm.Infrastructure.Monitoring;
using modular_mlm.Infrastructure.Notifications;
using modular_mlm.Infrastructure.Payments;
using modular_mlm.Infrastructure.Payouts;
using modular_mlm.Infrastructure.Services;
using modular_mlm.Infrastructure.Storage;

namespace Microsoft.Extensions.DependencyInjection;

public static class DependencyInjection
{
    public static void AddInfrastructureServices(this IHostApplicationBuilder builder)
    {
        var connectionString = builder.Configuration.GetConnectionString(Services.Database);
        Guard.Against.Null(
            connectionString,
            message: $"Connection string '{Services.Database}' not found."
        );

        builder.Services.AddScoped<ISaveChangesInterceptor, AuditableEntityInterceptor>();
        builder.Services.AddScoped<ISaveChangesInterceptor, DispatchDomainEventsInterceptor>();
        builder.Services.AddScoped<ISaveChangesInterceptor, AuditLogImmutabilityInterceptor>();
        builder.Services.AddScoped<
            ISaveChangesInterceptor,
            PublicReadCacheInvalidationInterceptor
        >();
        builder.Services.AddScoped<
            ISaveChangesInterceptor,
            AdministratorSecurityAuditInterceptor
        >();

        builder.Services.AddDbContext<ApplicationDbContext>(
            (sp, options) =>
            {
                options.AddInterceptors(sp.GetServices<ISaveChangesInterceptor>());
                options.UseNpgsql(connectionString);
            }
        );

        builder.EnrichNpgsqlDbContext<ApplicationDbContext>();

        builder.Services.AddScoped<IApplicationDbContext>(provider =>
            provider.GetRequiredService<ApplicationDbContext>()
        );

        builder.Services.AddSingleton<IDatabaseExceptionClassifier, PostgresDatabaseExceptionClassifier>();

        builder.Services.AddScoped<ApplicationDbContextInitialiser>();
        builder.Services.AddAgentOperationsInfrastructure();
        builder.Services.AddIdempotencyInfrastructure(builder.Configuration);
        builder.Services.AddNotificationInfrastructure(builder.Configuration);
        builder.Services.AddReliableOutboxInfrastructure(builder.Configuration);
        builder
            .Services.AddOptions<DurableBackgroundJobOptions>()
            .Bind(builder.Configuration.GetSection(DurableBackgroundJobOptions.SectionName))
            .Validate(DurableBackgroundJobOptions.IsValid, "Durable job settings are invalid.")
            .ValidateOnStart();
        builder.Services.AddScoped<IBackgroundJobScheduler, DurableBackgroundJobScheduler>();
        builder.Services.AddScoped<IBackgroundJobHandler, ProcessPayoutBackgroundJobHandler>();
        builder.Services.AddScoped<DurableBackgroundJobDispatcher>();
        builder.Services.AddScoped<ProcessPayoutCommandHandler>();
        builder.Services.AddHostedService<DurableBackgroundJobWorker>();
        builder.Services.AddScoped<IAuditWriter, AuditWriter>();
        builder.Services.AddSingleton<IAuditPayloadRedactor, AuditPayloadRedactor>();
        builder.Services.AddScoped<IIdentitySecurityAuditWriter, IdentitySecurityAuditWriter>();
        builder.Services.AddScoped<IOperationalHealthReader, OperationalHealthReader>();
        builder.Services.AddScoped<IWebhookFailureRecorder, WebhookFailureRecorder>();
        builder.Services.AddScoped<DatabaseReadinessHealthCheck>();
        builder.Services.AddScoped<PayMongoHealthCheck>();
        builder.Services.AddScoped<SmtpHealthCheck>();
        builder.Services.AddScoped<WorkerBacklogHealthCheck>();
        builder
            .Services.AddOptions<MonitoringOptions>()
            .Bind(builder.Configuration.GetSection(MonitoringOptions.SectionName))
            .Validate(
                options =>
                    options.OutboxWarningCount > 0
                    && options.OutboxUnhealthyCount > options.OutboxWarningCount
                    && options.DeadLetterWarningCount > 0,
                "Monitoring thresholds are invalid."
            )
            .ValidateOnStart();

        builder.Services.AddAuthorizationBuilder();

        builder
            .Services.AddIdentityApiEndpoints<ApplicationUser>()
            .AddRoles<IdentityRole>()
            .AddEntityFrameworkStores<ApplicationDbContext>();
        builder.Services.AddIdentitySecurity(builder.Configuration, builder.Environment);

        builder.Services.AddSingleton(TimeProvider.System);
        builder.Services.AddTransient<IIdentityService, IdentityService>();
        builder.Services.AddDataProtection();
        builder
            .Services.AddOptions<AdministratorInvitationOptions>()
            .Bind(builder.Configuration.GetSection(AdministratorInvitationOptions.SectionName))
            .Validate(
                options => options.LifetimeHours > 0,
                "Invitation lifetime must be positive."
            );
        builder.Services.AddSingleton<
            IAdministratorInvitationPolicy,
            AdministratorInvitationPolicy
        >();
        builder.Services.AddSingleton<ICache, InMemoryCache>();
        builder
            .Services.AddOptions<PayMongoOptions>()
            .Bind(builder.Configuration.GetSection(PayMongoOptions.SectionName));
        builder.Services.AddHttpClient<IPaymentGateway, PaymentGateway>(
            (provider, client) =>
            {
                var options = provider
                    .GetRequiredService<Microsoft.Extensions.Options.IOptions<PayMongoOptions>>()
                    .Value;
                client.BaseAddress = options.BaseUrl;
                client.Timeout = TimeSpan.FromSeconds(30);
            }
        );
        builder.Services.AddHttpClient<IPayoutProvider, PayoutProvider>(
            (provider, client) =>
            {
                var options = provider
                    .GetRequiredService<Microsoft.Extensions.Options.IOptions<PayMongoOptions>>()
                    .Value;
                client.BaseAddress = options.BaseUrl;
                client.Timeout = TimeSpan.FromSeconds(30);
            }
        );
        builder.Services.AddScoped<IPayoutAccountProtector, PayoutAccountProtector>();
        builder.Services.AddSingleton<IObjectNameGenerator, ObjectNameGenerator>();
        builder.Services.AddSingleton<
            IBrandingAssetContentInspector,
            BrandingAssetContentInspector
        >();
        var blobSection = builder.Configuration.GetSection(
            AzureBlobObjectStorageOptions.SectionName
        );
        if (blobSection.GetValue<bool>(nameof(AzureBlobObjectStorageOptions.Enabled)))
        {
            builder
                .Services.AddOptions<AzureBlobObjectStorageOptions>()
                .Bind(blobSection)
                .Validate(
                    options => string.IsNullOrWhiteSpace(options.PublicBaseUrl)
                        || IsHttpUri(options.PublicBaseUrl),
                    "Azure Blob public base URL is invalid."
                )
                .ValidateOnStart();
            builder.AddAzureBlobContainerClient(Services.ObjectStorageContainer);
            builder.Services.AddScoped<IObjectStorage, AzureBlobObjectStorage>();
        }
        else
        {
            builder.Services.AddScoped<IObjectStorage, ObjectStorage>();
        }
        if (
            builder.Configuration.GetValue<bool>(
                $"{DataProtectionStorageOptions.SectionName}:Enabled"
            )
        )
        {
            builder.AddKeyedAzureBlobContainerClient(Services.DataProtectionContainer);
        }
        builder.Services.AddSingleton<ICommissionReleaseLock>(
            new PostgresCommissionReleaseLock(connectionString)
        );
        builder.Services.AddSingleton<IWalletAdjustmentLock>(
            new PostgresWalletAdjustmentLock(connectionString)
        );
        builder.Services.AddScoped<BinaryPairingJob>();
        builder.Services.AddScoped<BinaryPairingScheduleProcessor>();
        builder.Services.AddHostedService<BinaryPairingWorker>();
        builder.Services.AddHostedService<PaidOrderCompensationWorker>();
        builder.Services.AddHostedService<ProviderReconciliationWorker>();
        builder.Services.AddHostedService<WebhookRetryWorker>();
        builder
            .Services.AddOptions<AdministratorInvitationExpiryOptions>()
            .Bind(
                builder.Configuration.GetSection(AdministratorInvitationExpiryOptions.SectionName)
            )
            .Validate(x => x.PollIntervalSeconds > 0, "Poll interval must be positive.")
            .Validate(
                x => x.OrganizationBatchSize is > 0 and <= 500,
                "Invalid organization batch size."
            )
            .Validate(
                x => x.InvitationBatchSize is > 0 and <= 500,
                "Invalid invitation batch size."
            )
            .ValidateOnStart();
        builder.Services.AddScoped<AdministratorInvitationExpiryJob>();
        builder.Services.AddHostedService<AdministratorInvitationExpiryWorker>();
        builder.Services.AddCommissionRelease(builder.Configuration);
        builder.Services.AddInventoryReservationCleanup(builder.Configuration);
        builder.Services.AddScoped<IAdministratorAccountService, AdministratorAccountService>();
        builder.Services.AddScoped<
            IApplicationAuthorizationService,
            ApplicationAuthorizationService
        >();
        builder.Services.AddScoped<
            IAdministratorInvitationTokenService,
            AdministratorInvitationTokenService
        >();
        builder
            .Services.AddHealthChecks()
            .AddCheck<DatabaseReadinessHealthCheck>("database", tags: ["ready"])
            .AddCheck<PayMongoHealthCheck>("paymongo", tags: ["ready", "external"])
            .AddCheck<SmtpHealthCheck>("smtp", tags: ["ready", "external"])
            .AddCheck<WorkerBacklogHealthCheck>("worker-backlog", tags: ["ready"]);
        builder.Services.AddHttpClient(
            PayMongoHealthCheck.ClientName,
            client =>
                client.BaseAddress =
                    builder
                        .Configuration.GetSection(PayMongoOptions.SectionName)
                        .Get<PayMongoOptions>()
                        ?.BaseUrl
                    ?? new Uri("https://api.paymongo.com/")
        );
    }

    private static bool IsHttpUri(string value) =>
        Uri.TryCreate(value, UriKind.Absolute, out var uri)
        && (uri.Scheme == Uri.UriSchemeHttps || uri.Scheme == Uri.UriSchemeHttp)
        && string.IsNullOrEmpty(uri.UserInfo);

}
