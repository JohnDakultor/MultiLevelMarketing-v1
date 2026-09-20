using modular_mlm.Application.Common.Interfaces;
using modular_mlm.Application.Network.Queries.GetDirectRecruits.Models;

namespace modular_mlm.Application.Network.Queries.GetDirectRecruits;

public sealed class GetDirectRecruitsQueryHandler(IApplicationDbContext db)
    : IRequestHandler<GetDirectRecruitsQuery, IReadOnlyList<DirectRecruitDto>>
{
    public async Task<IReadOnlyList<DirectRecruitDto>> Handle(
        GetDirectRecruitsQuery request,
        CancellationToken cancellationToken
    ) =>
        await db
            .Agents.AsNoTracking()
            .Where(x =>
                x.OrganizationId == request.OrganizationId && x.SponsorAgentId == request.AgentId
            )
            .OrderBy(x => x.JoinedAt)
            .ThenBy(x => x.Id)
            .Select(x => new DirectRecruitDto(
                x.Id,
                x.AgentCode,
                x.ReferralCode,
                x.Status,
                x.PlacementParentAgentId,
                x.PlacementSide,
                x.JoinedAt
            ))
            .ToListAsync(cancellationToken);
}
