namespace modular_mlm.Application.Wallets.Commands.ReleasePendingCommissions.Models;

public sealed record ReleasePendingCommissionsResult(
    Guid OrganizationId,
    int CandidatesFound,
    int ReleasedCount,
    int IneligibleCount,
    int AlreadyReleasedCount,
    int FailureCount,
    TimeSpan? OldestPendingAge,
    TimeSpan Duration
);
