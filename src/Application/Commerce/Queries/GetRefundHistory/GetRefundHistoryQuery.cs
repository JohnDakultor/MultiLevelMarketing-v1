using modular_mlm.Application.Commerce.Queries.GetRefundHistory.Models;

namespace modular_mlm.Application.Commerce.Queries.GetRefundHistory;

public sealed record GetRefundHistoryQuery(
    Guid OrganizationId,
    Guid? OrderId = null,
    int Page = 1,
    int PageSize = 20
) : IRequest<IReadOnlyList<RefundHistoryItemDto>>, IOrganizationAdminRequest;
