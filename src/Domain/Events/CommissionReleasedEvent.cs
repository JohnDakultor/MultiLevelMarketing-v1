namespace modular_mlm.Domain.Events;

public sealed record CommissionReleasedEvent(
    Guid OrganizationId,
    Guid CommissionId,
    Guid BeneficiaryAgentId,
    DateTimeOffset AvailableAt
) : BaseEvent;
