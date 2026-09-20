using modular_mlm.Application.Common.Interfaces;
using modular_mlm.Application.Referrals.Queries.GetAgentStorefront.Models;
using modular_mlm.Domain.Catalog;
using modular_mlm.Domain.Network;

namespace modular_mlm.Application.Referrals.Queries.GetAgentStorefront;

public sealed class GetAgentStorefrontQueryHandler(IApplicationDbContext db)
    : IRequestHandler<GetAgentStorefrontQuery, AgentStorefrontDto>
{
    public async Task<AgentStorefrontDto> Handle(
        GetAgentStorefrontQuery request,
        CancellationToken cancellationToken
    )
    {
        var agent = await db
            .Agents.AsNoTracking()
            .SingleOrDefaultAsync(
                x =>
                    x.OrganizationId == request.OrganizationId
                    && x.ReferralCode == request.ReferralCode.ToUpper(),
                cancellationToken
            );
        if (agent is null || agent.Status != AgentStatus.Active)
            throw new KeyNotFoundException("Active agent storefront was not found.");

        var productsQuery = db
            .Products.AsNoTracking()
            .Where(x =>
                x.OrganizationId == request.OrganizationId && x.Status == ProductStatus.Active
            );
        var totalCount = await productsQuery.CountAsync(cancellationToken);
        var products = await productsQuery
            .OrderBy(x => x.Name)
            .ThenBy(x => x.Id)
            .Skip((request.PageNumber - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(x => new StorefrontProductDto(
                x.Id,
                x.Name,
                x.Slug,
                x.Description,
                x.DefaultImageUrl,
                x.Variants.Min(variant => variant.Price),
                "/r/" + agent.ReferralCode + "/products/" + x.Slug
            ))
            .ToListAsync(cancellationToken);

        return new AgentStorefrontDto(
            agent.Id,
            agent.AgentCode,
            agent.ReferralCode,
            request.PageNumber,
            request.PageSize,
            totalCount,
            products
        );
    }
}
