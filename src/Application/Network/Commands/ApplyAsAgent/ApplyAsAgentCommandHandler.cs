using modular_mlm.Application.Common.Interfaces;
using modular_mlm.Domain.Network;
using modular_mlm.Domain.Organizations;

namespace modular_mlm.Application.Network.Commands.ApplyAsAgent;

public sealed class ApplyAsAgentCommandHandler(
    IUser currentUser,
    IApplicationDbContext db,
    TimeProvider clock
) : IRequestHandler<ApplyAsAgentCommand, Guid>
{
    public async Task<Guid> Handle(ApplyAsAgentCommand request, CancellationToken cancellationToken)
    {
        var userId = currentUser.Id ?? throw new UnauthorizedAccessException();
        var organization = await db.Organizations.SingleOrDefaultAsync(
            candidate => candidate.Id == request.OrganizationId,
            cancellationToken
        );
        if (organization is null)
            throw new KeyNotFoundException("Organization was not found.");
        if (organization.Status != OrganizationStatus.Active)
            throw new InvalidOperationException(
                "Agent applications require an active Organization."
            );

        if (
            await db.Agents.AnyAsync(
                x => x.OrganizationId == request.OrganizationId && x.UserId == userId,
                cancellationToken
            )
        )
            throw new InvalidOperationException(
                "This user already has an agent profile for the organization."
            );

        if (request.SponsorAgentId is { } sponsorAgentId)
        {
            var sponsorIsActive = await db.Agents.AnyAsync(
                candidate =>
                    candidate.Id == sponsorAgentId
                    && candidate.OrganizationId == request.OrganizationId
                    && candidate.Status == AgentStatus.Active,
                cancellationToken
            );
            if (!sponsorIsActive)
                throw new KeyNotFoundException(
                    "An active Sponsor was not found in this Organization."
                );
        }

        var token = Guid.NewGuid().ToString("N")[..10].ToUpperInvariant();
        var agent = Agent.Apply(
            request.OrganizationId,
            userId,
            $"AG-{token}",
            token,
            clock.GetUtcNow(),
            request.SponsorAgentId
        );
        agent.SubmitForApproval();
        db.Agents.Add(agent);
        await db.SaveChangesAsync(cancellationToken);
        return agent.Id;
    }
}
