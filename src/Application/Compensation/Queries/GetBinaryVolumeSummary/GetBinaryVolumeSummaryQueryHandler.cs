using modular_mlm.Application.Common.Interfaces;
using modular_mlm.Application.Compensation.Queries.GetBinaryVolumeSummary.Models;

namespace modular_mlm.Application.Compensation.Queries.GetBinaryVolumeSummary;

public sealed class GetBinaryVolumeSummaryQueryHandler(IApplicationDbContext db)
    : IRequestHandler<GetBinaryVolumeSummaryQuery, BinaryVolumeSummaryDto>
{
    public async Task<BinaryVolumeSummaryDto> Handle(
        GetBinaryVolumeSummaryQuery request,
        CancellationToken cancellationToken
    )
    {
        var balance = await db
            .BinaryVolumeBalances.AsNoTracking()
            .SingleOrDefaultAsync(
                candidate =>
                    candidate.OrganizationId == request.OrganizationId
                    && candidate.AgentId == request.AgentId,
                cancellationToken
            );
        return balance is null
            ? new BinaryVolumeSummaryDto(request.AgentId, 0m, 0m, 0m, 0m)
            : new BinaryVolumeSummaryDto(
                balance.AgentId,
                balance.LeftAvailable,
                balance.RightAvailable,
                balance.LeftLifetime,
                balance.RightLifetime
            );
    }
}
