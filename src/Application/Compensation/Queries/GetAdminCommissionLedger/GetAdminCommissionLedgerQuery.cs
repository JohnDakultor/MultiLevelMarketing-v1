using modular_mlm.Application.Compensation.Queries.GetAdminCommissionLedger.Model;

namespace modular_mlm.Application.Compensation.Queries.GetAdminCommissionLedger;

public sealed record GetAdminCommissionLedgerQuery(
    Guid OrganizationId,
    int Page,
    int PageSize,
    Guid? AgentId,
    string? CommissionType,
    string? Status,
    DateTime? From,
    DateTime? To,
    bool IncludeReversals
) : IRequest<AdminCommissionLedgerPageDto>, IOrganizationAdminRequest;
