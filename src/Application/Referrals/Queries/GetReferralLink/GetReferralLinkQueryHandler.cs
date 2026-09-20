using modular_mlm.Application.Common.Interfaces;
using modular_mlm.Application.Referrals.Queries.GetReferralLink.Models;
using modular_mlm.Domain.Catalog;
using modular_mlm.Domain.Network;

namespace modular_mlm.Application.Referrals.Queries.GetReferralLink;

public sealed class GetReferralLinkQueryHandler(IApplicationDbContext db)
    : IRequestHandler<GetReferralLinkQuery, ReferralLinkDto>
{
    public async Task<ReferralLinkDto> Handle(
        GetReferralLinkQuery request,
        CancellationToken cancellationToken
    )
    {
        var agent = await db
            .Agents.AsNoTracking()
            .SingleOrDefaultAsync(
                x => x.Id == request.AgentId && x.OrganizationId == request.OrganizationId,
                cancellationToken
            );
        if (agent is null || agent.Status != AgentStatus.Active)
            throw new InvalidOperationException("An active agent is required.");

        if (request.ProductId is null)
            return new ReferralLinkDto(
                agent.ReferralCode,
                null,
                $"/r/{Uri.EscapeDataString(agent.ReferralCode)}"
            );

        var product = await db
            .Products.AsNoTracking()
            .SingleOrDefaultAsync(
                x => x.Id == request.ProductId && x.OrganizationId == request.OrganizationId,
                cancellationToken
            );
        if (product is null || product.Status != ProductStatus.Active)
            throw new KeyNotFoundException("Active product was not found.");

        return new ReferralLinkDto(
            agent.ReferralCode,
            product.Id,
            $"/r/{Uri.EscapeDataString(agent.ReferralCode)}/products/{Uri.EscapeDataString(product.Slug)}"
        );
    }
}
