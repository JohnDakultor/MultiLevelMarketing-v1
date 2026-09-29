using modular_mlm.Application.Payouts.Queries.GetAdminPayoutAccounts.Models;
using modular_mlm.Domain.Payouts;

namespace modular_mlm.Application.Payouts.Queries.GetAdminPayoutAccounts;

public sealed record GetAdminPayoutAccountsQuery(
    Guid OrganizationId,
    int Page = 1,
    int PageSize = 20,
    PayoutVerificationStatus? Status = null
) : IRequest<AdminPayoutAccountsPageDto>, IOrganizationAdminRequest;
