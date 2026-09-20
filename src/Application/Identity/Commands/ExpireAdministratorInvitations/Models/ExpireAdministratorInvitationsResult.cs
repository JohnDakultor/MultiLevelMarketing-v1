namespace modular_mlm.Application.Identity.Commands.ExpireAdministratorInvitations.Models;

public sealed record ExpireAdministratorInvitationsResult(
    int ExpiredCount,
    int RemainingCount,
    DateTimeOffset CompletedAt
);
