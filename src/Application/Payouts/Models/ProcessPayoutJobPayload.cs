namespace modular_mlm.Application.Payouts.Models;

public sealed record ProcessPayoutJobPayload(
    Guid OrganizationId,
    Guid PayoutRequestId,
    string IdempotencyKey
);
