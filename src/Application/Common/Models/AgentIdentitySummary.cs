namespace modular_mlm.Application.Common.Models;

public sealed record AgentIdentitySummary(
    string UserId,
    string DisplayName,
    string Email,
    bool EmailConfirmed
);
