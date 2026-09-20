using modular_mlm.Application.Common.Exceptions;
using modular_mlm.Application.Common.Interfaces;
using modular_mlm.Domain.Payouts;

namespace modular_mlm.Application.Payouts.Commands.RegisterPayoutAccount;

public sealed class RegisterPayoutAccountCommandHandler(
    IUser currentUser,
    IApplicationDbContext db,
    IPayoutAccountProtector protector,
    IAdministratorAccountService administrators
) : IRequestHandler<RegisterPayoutAccountCommand, Guid>
{
    public async Task<Guid> Handle(
        RegisterPayoutAccountCommand request,
        CancellationToken cancellationToken
    )
    {
        var userId = currentUser.Id ?? throw new UnauthorizedAccessException();
        var ownsAgent = await db.Agents.AnyAsync(
            agent =>
                agent.Id == request.AgentId
                && agent.OrganizationId == request.OrganizationId
                && agent.UserId == userId,
            cancellationToken
        );
        if (
            !ownsAgent
            && !await administrators.CanManageOrganizationAsync(
                userId,
                request.OrganizationId,
                cancellationToken
            )
        )
            throw new ForbiddenAccessException();

        var masked =
            request.AccountNumber.Length <= 4
                ? request.AccountNumber
                : $"****{request.AccountNumber[^4..]}";
        var account = PayoutAccount.Register(
            request.OrganizationId,
            request.AgentId,
            request.Method,
            masked,
            protector.Protect(request.AccountName),
            protector.Protect(request.AccountNumber),
            request.BankCode,
            request.Rail
        );
        db.PayoutAccounts.Add(account);
        await db.SaveChangesAsync(cancellationToken);
        return account.Id;
    }
}
