using modular_mlm.Application.Common.Interfaces;
using modular_mlm.Application.Referrals.Commands.CreateProductReferralLink.Models;
using modular_mlm.Domain.Catalog;
using modular_mlm.Domain.Network;

namespace modular_mlm.Application.Referrals.Commands.CreateProductReferralLink;

public sealed class CreateProductReferralLinkCommandHandler(IApplicationDbContext db, IUser user)
    : IRequestHandler<CreateProductReferralLinkCommand, ProductReferralLinkDto>
{
    public async Task<ProductReferralLinkDto> Handle(
        CreateProductReferralLinkCommand request,
        CancellationToken cancellationToken
    )
    {
        var userId = user.Id ?? throw new UnauthorizedAccessException();
        var agent =
            await db
                .Agents.AsNoTracking()
                .SingleOrDefaultAsync(
                    candidate =>
                        candidate.OrganizationId == request.OrganizationId
                        && candidate.UserId == userId,
                    cancellationToken
                )
            ?? throw new KeyNotFoundException("Current Agent profile was not found.");
        if (agent.Status != AgentStatus.Active)
            throw new InvalidOperationException("An active Agent is required.");
        var product =
            await db
                .Products.AsNoTracking()
                .SingleOrDefaultAsync(
                    candidate =>
                        candidate.OrganizationId == request.OrganizationId
                        && candidate.Id == request.ProductId,
                    cancellationToken
                )
            ?? throw new KeyNotFoundException("Product was not found.");
        if (product.Status != ProductStatus.Active)
            throw new InvalidOperationException("Only an active product can be shared.");
        var hostName = await db
            .OrganizationDomains.AsNoTracking()
            .Where(domain =>
                domain.OrganizationId == request.OrganizationId
                && domain.IsPrimary
                && domain.VerifiedAt != null
            )
            .Select(domain => domain.HostName)
            .SingleOrDefaultAsync(cancellationToken);
        var path =
            $"/r/{Uri.EscapeDataString(agent.ReferralCode)}/products/{Uri.EscapeDataString(product.Slug)}";
        var url = hostName is null ? path : $"https://{hostName}{path}";
        return new ProductReferralLinkDto(product.Id, product.Slug, agent.ReferralCode, url, url);
    }
}
