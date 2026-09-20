using modular_mlm.Application.Common.Exceptions;
using modular_mlm.Application.Common.Interfaces;

namespace modular_mlm.Application.Payouts.Commands.MakeDefaultPayoutAccount;

public sealed class MakeDefaultPayoutAccountCommandHandler(
    IUser currentUser,
    IApplicationDbContext db
) : IRequestHandler<MakeDefaultPayoutAccountCommand>
{
    public async Task Handle(
        MakeDefaultPayoutAccountCommand request,
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
        var accounts = await db
            .PayoutAccounts.Where(account =>
                account.OrganizationId == request.OrganizationId
                && account.AgentId == request.AgentId
            )
            .ToListAsync(cancellationToken);
        var selected = accounts.SingleOrDefault(account => account.Id == request.PayoutAccountId);
        if (selected is null)
            throw new KeyNotFoundException("Payout account was not found.");
        foreach (var account in accounts.Where(account => account.IsDefault))
            account.RemoveDefault();
        selected.MakeDefault();
        await db.SaveChangesAsync(cancellationToken);
    }
}
