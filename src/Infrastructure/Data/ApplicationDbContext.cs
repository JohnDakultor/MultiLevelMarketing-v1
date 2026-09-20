using System.Reflection;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using modular_mlm.Application.Common.Interfaces;
using modular_mlm.Domain.AuditLogs;
using modular_mlm.Domain.Catalog;
using modular_mlm.Domain.Commerce;
using modular_mlm.Domain.Compensation;
using modular_mlm.Domain.Identity;
using modular_mlm.Domain.Network;
using modular_mlm.Domain.Notifications;
using modular_mlm.Domain.Organizations;
using modular_mlm.Domain.Payments;
using modular_mlm.Domain.Payouts;
using modular_mlm.Domain.Referral;
using modular_mlm.Domain.Wallets;
using modular_mlm.Infrastructure.BackgroundJobs;
using modular_mlm.Infrastructure.Idempotency;
using modular_mlm.Infrastructure.Identity;
using modular_mlm.Infrastructure.Messaging;
using modular_mlm.Infrastructure.Notifications;

namespace modular_mlm.Infrastructure.Data;

public sealed class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
    : IdentityDbContext<ApplicationUser>(options),
        IApplicationDbContext
{
    public DbSet<Organization> Organizations => Set<Organization>();
    public DbSet<OrganizationDomain> OrganizationDomains => Set<OrganizationDomain>();
    public DbSet<WalletSettings> WalletSettings => Set<WalletSettings>();
    public DbSet<AdministratorInvitation> AdministratorInvitations =>
        Set<AdministratorInvitation>();
    public DbSet<CustomerProfile> CustomerProfiles => Set<CustomerProfile>();
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<Product> Products => Set<Product>();
    public DbSet<ProductVariant> ProductVariants => Set<ProductVariant>();
    public DbSet<ProductCommissionProfile> ProductCommissionProfiles =>
        Set<ProductCommissionProfile>();
    public DbSet<Cart> Carts => Set<Cart>();
    public DbSet<CartItem> CartItems => Set<CartItem>();
    public DbSet<Order> Orders => Set<Order>();
    public DbSet<OrderItem> OrderItems => Set<OrderItem>();
    public DbSet<OrderItemRefund> OrderItemRefunds => Set<OrderItemRefund>();
    public DbSet<Agent> Agents => Set<Agent>();
    public DbSet<PlacementClosure> PlacementClosures => Set<PlacementClosure>();
    public DbSet<CommissionPlan> CommissionPlans => Set<CommissionPlan>();
    public DbSet<BinaryVolumeEntry> BinaryVolumeEntries => Set<BinaryVolumeEntry>();
    public DbSet<BinaryVolumeBalance> BinaryVolumeBalances => Set<BinaryVolumeBalance>();
    public DbSet<CommissionTransaction> CommissionTransactions => Set<CommissionTransaction>();
    public DbSet<AgentWallet> AgentWallets => Set<AgentWallet>();
    public DbSet<WalletEntry> WalletEntries => Set<WalletEntry>();
    public DbSet<PayoutAccount> PayoutAccounts => Set<PayoutAccount>();
    public DbSet<PayoutRequest> PayoutRequests => Set<PayoutRequest>();
    public DbSet<ReferralAttribution> ReferralAttributions => Set<ReferralAttribution>();
    public DbSet<Payment> Payments => Set<Payment>();
    public DbSet<PaymentWebhookReceipt> PaymentWebhookReceipts => Set<PaymentWebhookReceipt>();
    public DbSet<PaymentRefund> PaymentRefunds => Set<PaymentRefund>();
    public DbSet<BinaryPairingRun> BinaryPairingRuns => Set<BinaryPairingRun>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();
    public DbSet<OutboxMessage> OutboxMessages => Set<OutboxMessage>();
    public DbSet<ProcessedMessage> ProcessedMessages => Set<ProcessedMessage>();
    public DbSet<InventoryAdjustment> InventoryAdjustments => Set<InventoryAdjustment>();
    public DbSet<InventoryReservation> InventoryReservations => Set<InventoryReservation>();
    public DbSet<WebhookProcessingAttempt> WebhookProcessingAttempts =>
        Set<WebhookProcessingAttempt>();
    public DbSet<Notification> Notifications => Set<Notification>();

    public DbSet<CustomerAddress> CustomerAddresses => Set<CustomerAddress>();
    public DbSet<IdempotencyRecord> IdempotencyRecords => Set<IdempotencyRecord>();
    public DbSet<NotificationDeliveryEnvelope> NotificationDeliveryEnvelopes =>
        Set<NotificationDeliveryEnvelope>();
    public DbSet<DurableBackgroundJob> DurableBackgroundJobs => Set<DurableBackgroundJob>();
    public DbSet<AuthenticationSession> AuthenticationSessions => Set<AuthenticationSession>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);
        builder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());
    }
}
