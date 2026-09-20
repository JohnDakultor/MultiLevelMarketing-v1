namespace modular_mlm.Application.Commerce.Commands.ApplyReferralCode;

public sealed record ApplyReferralCodeCommand(Guid OrganizationId, string ReferralCode)
    : IRequest<CartDto>,
        ICustomerContextRequest;
