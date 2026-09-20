namespace modular_mlm.Application.Catalog.Commands.CreateProductCommissionProfile;

public sealed record CreateProductCommissionProfileCommand(
    Guid OrganizationId,
    string Name,
    bool DirectSalesEligible,
    decimal? DirectSalesRateOverride,
    bool BinaryVolumeEligible,
    decimal? BinaryVolumeOverride,
    DateTimeOffset EffectiveFrom
) : IRequest<Guid>, IOrganizationAdminRequest;
