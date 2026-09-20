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

namespace modular_mlm.Application.Common.Interfaces;

public interface IApplicationDbContext
{
    DbSet<Organization> Organizations { get; }
    DbSet<OrganizationDomain> OrganizationDomains { get; }
    DbSet<WalletSettings> WalletSettings { get; }
    DbSet<AdministratorInvitation> AdministratorInvitations { get; }
    DbSet<CustomerProfile> CustomerProfiles { get; }
    DbSet<Category> Categories { get; }
    DbSet<Product> Products { get; }
    DbSet<ProductVariant> ProductVariants { get; }
    DbSet<ProductCommissionProfile> ProductCommissionProfiles { get; }
    DbSet<Cart> Carts { get; }
    DbSet<CartItem> CartItems { get; }
    DbSet<Order> Orders { get; }
    DbSet<OrderItem> OrderItems { get; }
    DbSet<OrderItemRefund> OrderItemRefunds { get; }
    DbSet<Agent> Agents { get; }
    DbSet<PlacementClosure> PlacementClosures { get; }
    DbSet<CommissionPlan> CommissionPlans { get; }
    DbSet<BinaryVolumeEntry> BinaryVolumeEntries { get; }
    DbSet<BinaryVolumeBalance> BinaryVolumeBalances { get; }
    DbSet<CommissionTransaction> CommissionTransactions { get; }
    DbSet<AgentWallet> AgentWallets { get; }
    DbSet<WalletEntry> WalletEntries { get; }
    DbSet<PayoutAccount> PayoutAccounts { get; }
    DbSet<PayoutRequest> PayoutRequests { get; }
    DbSet<ReferralAttribution> ReferralAttributions { get; }
    DbSet<Payment> Payments { get; }
    DbSet<PaymentWebhookReceipt> PaymentWebhookReceipts { get; }
    DbSet<PaymentRefund> PaymentRefunds { get; }

    DbSet<BinaryPairingRun> BinaryPairingRuns { get; }
    DbSet<AuditLog> AuditLogs { get; }

    DbSet<CustomerAddress> CustomerAddresses { get; }

    DbSet<InventoryAdjustment> InventoryAdjustments { get; }
    DbSet<InventoryReservation> InventoryReservations { get; }
    DbSet<Notification> Notifications { get; }
    Task<int> SaveChangesAsync(CancellationToken cancellationToken);
}
