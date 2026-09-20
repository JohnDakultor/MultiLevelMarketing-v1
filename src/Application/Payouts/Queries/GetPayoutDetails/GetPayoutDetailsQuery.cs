using modular_mlm.Application.Payouts.Queries.GetPayoutDetails.Models;

namespace modular_mlm.Application.Payouts.Queries.GetPayoutDetails;

public sealed record GetPayoutDetailsQuery(Guid OrganizationId, Guid PayoutRequestId)
    : IRequest<PayoutDetailsDto?>,
        IPayoutScopedRequest;
