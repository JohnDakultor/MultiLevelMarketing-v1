using modular_mlm.Application.Commerce.Queries.GetAgentProductSalesSummary.Models;
using modular_mlm.Application.Common.Security;
using modular_mlm.Domain.Constants;

namespace modular_mlm.Application.Commerce.Queries.GetAgentProductSalesSummary;

[Authorize(Roles = Roles.Agent)]
public sealed record GetAgentProductSalesSummaryQuery(
    Guid OrganizationId,
    int Page = 1,
    int PageSize = 20,
    DateTimeOffset? From = null,
    DateTimeOffset? To = null
) : IRequest<AgentProductSalesPageDto>, ICurrentAgentRequest;
