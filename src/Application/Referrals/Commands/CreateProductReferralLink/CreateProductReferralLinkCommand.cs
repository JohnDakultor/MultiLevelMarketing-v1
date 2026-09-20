using modular_mlm.Application.Common.Security;
using modular_mlm.Application.Referrals.Commands.CreateProductReferralLink.Models;
using modular_mlm.Domain.Constants;

namespace modular_mlm.Application.Referrals.Commands.CreateProductReferralLink;

[Authorize(Roles = Roles.Agent)]
public sealed record CreateProductReferralLinkCommand(Guid OrganizationId, Guid ProductId)
    : IRequest<ProductReferralLinkDto>,
        ICurrentAgentRequest;
