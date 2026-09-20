using modular_mlm.Application.Common.Exceptions;
using modular_mlm.Application.Common.Interfaces;

namespace modular_mlm.Application.Payouts.Commands.SubmitPayoutAccountForVerification;

public sealed class SubmitPayoutAccountForVerificationCommandHandler(
    IUser currentUser,
    IApplicationDbContext db
) : IRequestHandler<SubmitPayoutAccountForVerificationCommand>
{
    public async Task Handle(
        SubmitPayoutAccountForVerificationCommand request,
        CancellationToken cancellationToken
    )
    {
        var userId = currentUser.Id ?? throw new UnauthorizedAccessException();
        if (
            !await db.Agents.AnyAsync(
                agent =>
                    agent.Id == request.AgentId
                    && agent.OrganizationId == request.OrganizationId
                    && agent.UserId == userId,
                cancellationToken
            )
        )
            throw new ForbiddenAccessException();
        var account = await db.PayoutAccounts.SingleOrDefaultAsync(
            candidate =>
                candidate.Id == request.PayoutAccountId
                && candidate.OrganizationId == request.OrganizationId
                && candidate.AgentId == request.AgentId,
            cancellationToken
        );
        if (account is null)
            throw new KeyNotFoundException("Payout account was not found.");
        account.SubmitForVerification();
        await db.SaveChangesAsync(cancellationToken);
    }
}
