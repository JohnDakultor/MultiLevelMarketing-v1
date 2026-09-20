using Application.Wallets.Queries.GetWalletEntries.Models;

namespace modular_mlm.Application.Wallets.Queries.GetWalletEntries;

public sealed class GetWalletEntriesQueryHandler(IApplicationDbContext db)
    : IRequestHandler<GetWalletEntriesQuery, WalletEntriesPageDto>
{
    public async Task<WalletEntriesPageDto> Handle(
        GetWalletEntriesQuery request,
        CancellationToken cancellationToken
    )
    {
        var wallet = await db
            .AgentWallets.AsNoTracking()
            .SingleOrDefaultAsync(
                candidate =>
                    candidate.OrganizationId == request.OrganizationId
                    && candidate.AgentId == request.AgentId,
                cancellationToken
            );
        if (wallet is null)
            throw new KeyNotFoundException("Agent wallet was not found.");

        var query = db.WalletEntries.AsNoTracking().Where(entry => entry.WalletId == wallet.Id);

        if (request.EntryType.HasValue)
            query = query.Where(entry => entry.Type == request.EntryType.Value);
        if (request.From.HasValue)
            query = query.Where(entry => entry.Created >= request.From.Value);
        if (request.To.HasValue)
            query = query.Where(entry => entry.Created < request.To.Value);

        var totalCount = await query.CountAsync(cancellationToken);

        var pageItems = await query
            .OrderByDescending(entry => entry.Created)
            .ThenByDescending(entry => entry.Id)
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(entry => new WalletEntryDto(
                entry.Id,
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

        return new WalletEntriesPageDto(pageItems, request.Page, request.PageSize, totalCount);
    }
}
