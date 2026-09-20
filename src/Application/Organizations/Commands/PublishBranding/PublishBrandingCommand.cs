namespace modular_mlm.Application.Organizations.Commands.PublishBranding;

public sealed record PublishBrandingCommand(Guid OrganizationId)
    : IRequest,
        IOrganizationAdminRequest;
