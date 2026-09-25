using modular_mlm.Application.Compensation.Queries.GetBinaryVolumeLedger.Models;

namespace modular_mlm.Application.Compensation.Queries.GetBinaryVolumeLedger;

public sealed class GetBinaryVolumeLedgerQueryHandler(IApplicationDbContext db)
    : IRequestHandler<GetBinaryVolumeLedgerQuery, BinaryVolumeLedgerPageDto>
{
    public async Task<BinaryVolumeLedgerPageDto> Handle(
        GetBinaryVolumeLedgerQuery request,
        CancellationToken cancellationToken
    )
    {
        var query = db.BinaryVolumeEntries.AsNoTracking().Where(entry =>
            entry.OrganizationId == request.OrganizationId
            && entry.OwnerAgentId == request.AgentId
        );

        if (request.From.HasValue)
            query = query.Where(entry => entry.EffectiveAt >= request.From.Value);
        if (request.To.HasValue)
            query = query.Where(entry => entry.EffectiveAt < request.To.Value);
        if (request.Side.HasValue)
            query = query.Where(entry => entry.Side == request.Side.Value);
        if (request.EntryType.HasValue)
            query = query.Where(entry => entry.EntryType == request.EntryType.Value);

        var totalCount = await query.CountAsync(cancellationToken);
        var items = await query
            .OrderByDescending(entry => entry.EffectiveAt)
            .ThenByDescending(entry => entry.Id)
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(entry => new BinaryVolumeLedgerItemDto(
                entry.Id,
                entry.OwnerAgentId,
                entry.SourceAgentId,
                entry.SourceOrderItemId,
                entry.PairingRunId,
                entry.Side,
                entry.Volume,
                entry.EntryType,
                entry.EffectiveAt,
                entry.ReversalOfEntryId,
                entry.SourceOrderItemRefundId
            ))
            .ToListAsync(cancellationToken);

        return new BinaryVolumeLedgerPageDto(
            request.OrganizationId,
            request.AgentId,
            items,
            request.Page,
            request.PageSize,
            totalCount
        );
    }
}
