using modular_mlm.Application.Wallets.Queries.GetAdminWalletEntries.Models;

namespace modular_mlm.Application.Wallets.Queries.GetAdminWalletEntries;

public sealed class GetAdminWalletEntriesQueryHandler(IApplicationDbContext db)
    : IRequestHandler<GetAdminWalletEntriesQuery, AdminWalletEntriesPageDto>
{
    public async Task<AdminWalletEntriesPageDto> Handle(
        GetAdminWalletEntriesQuery request,
        CancellationToken cancellationToken
    )
    {
        var wallet = await (
            from candidate in db.AgentWallets.AsNoTracking()
            join agent in db.Agents.AsNoTracking()
                on new { candidate.AgentId, candidate.OrganizationId } equals new
                {
                    AgentId = agent.Id,
                    agent.OrganizationId,
                }
            where
                candidate.OrganizationId == request.OrganizationId
                && candidate.AgentId == request.AgentId
            select new
            {
                candidate.Id,
                candidate.Currency,
                agent.AgentCode,
            }
        ).SingleOrDefaultAsync(cancellationToken);
        if (wallet is null)
            throw new KeyNotFoundException("Agent wallet was not found in this organization.");

        var query = db.WalletEntries.AsNoTracking().Where(entry => entry.WalletId == wallet.Id);
        if (request.EntryType.HasValue)
            query = query.Where(entry => entry.Type == request.EntryType.Value);
        if (request.From.HasValue)
            query = query.Where(entry => entry.Created >= request.From.Value);
        if (request.To.HasValue)
            query = query.Where(entry => entry.Created < request.To.Value);

        var totalCount = await query.CountAsync(cancellationToken);
        var items = await query
            .OrderByDescending(entry => entry.Created)
            .ThenByDescending(entry => entry.Id)
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(entry => new AdminWalletEntryDto(
                entry.Id,
                entry.WalletId,
                entry.Type,
                entry.Amount,
                entry.SourceType,
                entry.SourceId,
                entry.AvailableAt,
                entry.ReversalOfEntryId,
                entry.ReleasedFromEntryId,
                entry.Created
            ))
            .ToListAsync(cancellationToken);

        return new AdminWalletEntriesPageDto(
            request.OrganizationId,
            request.AgentId,
            wallet.Id,
            wallet.AgentCode,
            wallet.Currency,
            items,
            request.Page,
            request.PageSize,
            totalCount
        );
    }
}
